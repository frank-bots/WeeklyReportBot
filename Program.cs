using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using cqhttp.Cyan.ApiCall.Requests;
using cqhttp.Cyan.Events.CQEvents;
using cqhttp.Cyan.Events.CQEvents.Base;
using cqhttp.Cyan.Events.CQResponses;
using cqhttp.Cyan.Instance;
using cqhttp.Cyan.Messages;
using cqhttp.Cyan.Utils;

namespace WRBot {
    class Program {
        static List<string> privateCommands = new List<string> { "help", "init", "get_wr", "submit" };
        public static string getText (Message m) {
            string ret = "";
            foreach (var i in m.data) {
                if (i.type == "text")
                    ret += i.data["text"];
            }
            return ret;
        }
        static string helpMsg = @"此bot为交互式bot，调用相应的命令会有相应的指引。请大胆使用，enjoy。
支持的命令: 
/help 展示此消息
/init 设置登陆周报平台所需的credentials
/get_wr 获取本周大家的周报
/submit 提交自己的周报(only sunday)
/status 在群里就能使用, 查看有多少人已经提交周报
bot(应该)会在每周日早八点在XDSEC 2018群里提醒大家写周报, 也(应该)会在周日下午四点公布尚未提交周报的人";
        static void Main (string[] args) {
            cqhttp.Cyan.Logger.LogLevel = cqhttp.Cyan.Enums.Verbosity.INFO;
            CQApiClient cli = new CQHTTPClient (
                accessUrl: "http://127.0.0.1:5700",
                listen_port : 210
            );
            Group.Jobs.StartJobs (cli, 00000000);
            Private.Operation.LoadData ();
            cli.OnEvent += (client, e) => {
                if (e is MessageEvent) {
                    string text_message = getText ((e as MessageEvent).message);
                    Command cmd;
                    try {
                        cmd = Parser.ParseCommand (text_message, (e as MessageEvent).sender.nickname);
                    } catch { return new EmptyResponse (); }
                    if (e is GroupMessageEvent) {
                        GroupMessageEvent ge = (e as GroupMessageEvent);
                        cmd.endPoint = (client, (ge.messageType, ge.group_id));
                        if (privateCommands.IndexOf (cmd.operation) != -1) {
                            client.SendTextAsync (
                                cmd.endPoint.Item2, "请私聊调用这项功能"
                            );
                            client.SendTextAsync (
                                cqhttp.Cyan.Enums.MessageType.private_,
                                ge.sender.user_id, helpMsg
                            );
                        } else {
                            Group.Jobs.Handle (cmd);
                        }
                    } else if (e is PrivateMessageEvent) {
                        PrivateMessageEvent pe = (e as PrivateMessageEvent);
                        cmd.endPoint = (cli, (pe.messageType, pe.sender_id));
                        if (cmd.operation == "help") {
                            cli.SendTextAsync (cmd.endPoint.Item2, helpMsg);
                            return new EmptyResponse ();
                        }
                        try {
                            Private.Operation.Handle (cmd).Wait ();
                        } catch (AggregateException ee) {
                            throw ee.InnerException;
                        }
                    }
                }
                return new EmptyResponse ();
            };
            Console.ReadLine ();
        }
    }
}