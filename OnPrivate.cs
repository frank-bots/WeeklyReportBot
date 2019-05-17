using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using cqhttp.Cyan.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WRBot.Scraper;

namespace WRBot.Private {
    class Operation {
        //"/help", "/init", "/get_wr", "/submit"
        static JObject credentials = new JObject ();
        static Dictionary < long, (string, string) > cache = new Dictionary < long, (string, string) > ();
        static Dictionary<long, HttpClient> loggedIn = new Dictionary<long, HttpClient> ();
        public static void LoadData () {
            credentials = JObject.Parse (File.ReadAllText ("creds.json"));
        }
        static void PersistData (long user_id, string username, string password) {
            var entry = new JObject ();
            entry["username"] = username;
            entry["password"] = password;
            credentials[user_id.ToString ()] = entry;
            cache[user_id] = (username, password);
            File.WriteAllText ("creds.json", credentials.ToString (Formatting.None));
        }
        static (string, string) getCredentials (long user_id) {
            if (cache.ContainsKey (user_id))
                return cache[user_id];
            if (credentials.ContainsKey (user_id.ToString ())) {
                var temp = credentials[user_id.ToString ()];
                cache[user_id] = (temp["username"].ToObject<string> (), temp["password"].ToObject<string> ());
                return cache[user_id];
            } else throw new System.UnauthorizedAccessException ();
        }
        static async Task<bool> ensureLoggedIn (long user_id) {
            if (loggedIn.ContainsKey (user_id) == false) {
                (string, string) cred = ("", "");
                loggedIn[user_id] = new HttpClient ();
                try {
                    cred = getCredentials (user_id);
                } catch (System.UnauthorizedAccessException) {
                    return false;
                }
                var resp = await loggedIn[user_id].PostAsync ("http://wr.xdsec.club/login", new FormUrlEncodedContent (
                    new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string> ("username", cred.Item1),
                        new KeyValuePair<string, string> ("passwd", cred.Item2),
                        new KeyValuePair<string, string> ("login", "Login")
                    }
                ));
                string content = await resp.Content.ReadAsStringAsync ();
                if (content.Contains ("wrong!")) {
                    loggedIn[user_id].Dispose ();
                    loggedIn.Remove (user_id);
                    return false;
                }
                return true;
            }
            return true;
        }
        public async static Task Handle (Command cmd) {
            switch (cmd.operation) {
                case "init":
                    await cmd.endPoint.Item1.SendTextAsync (
                        cmd.endPoint.Item2,
                        "请输入登陆所使用的昵称"
                    );
                    Dialogue d1 = new Dialogue ();
                    string username = "", password;
                    d1["BEGIN"] = (c, m) => {
                        username = Program.getText (m);
                        c.SendTextAsync (cmd.endPoint.Item2, "请输入token");
                        return "SET_TOKEN";
                    };
                    d1["SET_TOKEN"] = (c, m) => {
                        password = Program.getText (m);
                        PersistData (cmd.endPoint.Item2.Item2, username, password);
                        c.SendTextAsync (cmd.endPoint.Item2, $"设置完成,用户名:{username},token:{password}");
                        return "DONE";
                    };
                    throw new InvokeDialogueException (d1);
                case "get_wr":
                    await WRScraper.updateIndex ();
                    if (await ensureLoggedIn (cmd.endPoint.Item2.Item2)) {
                        string ret = "目前有: ";
                        WRScraper.submittedUsers.ForEach ((s) => ret += s + ',');
                        ret += "已经提交了周报\n你要看谁的？";
                        await cmd.endPoint.Item1.SendTextAsync (
                            cmd.endPoint.Item2,
                            ret
                        );
                        Dialogue d2 = new Dialogue ();
                        d2["BEGIN"] = (c, m) => {
                            try {
                                c.SendTextAsync (
                                    cmd.endPoint.Item2,
                                    WRScraper.getWRFor (loggedIn[cmd.endPoint.Item2.Item2], Program.getText (m)).Result
                                );
                            } catch (System.IndexOutOfRangeException) {
                                c.SendTextAsync (
                                    cmd.endPoint.Item2,
                                    "用户名输入错误"
                                );
                            }
                            return "DONE";
                        };
                        throw new InvokeDialogueException (d2);
                    }
                    await cmd.endPoint.Item1.SendTextAsync (
                        cmd.endPoint.Item2,
                        "请重新进行/init"
                    );
                    break;
                case "submit":
                    if (System.DateTime.UtcNow.AddHours (8).DayOfWeek != System.DayOfWeek.Sunday) {
                        await cmd.endPoint.Item1.SendTextAsync (
                            cmd.endPoint.Item2,
                            "only sunday"
                        );
                        break;
                    }
                    if (loggedIn.ContainsKey (cmd.endPoint.Item2.Item2) == false) {
                        await cmd.endPoint.Item1.SendTextAsync (
                            cmd.endPoint.Item2,
                            "请重新进行/init"
                        );
                        break;
                    }
                    Dialogue d3 = new Dialogue ();
                    await cmd.endPoint.Item1.SendTextAsync (
                        cmd.endPoint.Item2,
                        "请发送周报的内容:"
                    );
                    d3["BEGIN"] = (c, m) => {
                        WRScraper.submitWR (
                            loggedIn[cmd.endPoint.Item2.Item2],
                            Program.getText (m)
                        ).Wait ();
                        c.SendTextAsync (cmd.endPoint.Item2, "已提交");
                        var sup = WRScraper.updateIndex ();
                        return "DONE";
                    };
                    throw new InvokeDialogueException (d3);
            }
        }
    }
}