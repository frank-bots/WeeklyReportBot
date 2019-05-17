using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WRBot.Scraper;

namespace WRBot.Group {
    class Jobs {
        static string hintMsg = @"起床写周报啦!";
        public static void StartJobs (cqhttp.Cyan.Instance.CQApiClient cli, long target) {
            Task.Run (() => {
                while (true) {
                    var weekend = DateTime.UtcNow.Date;
                    weekend = weekend.AddHours (8).AddDays ((double) (7 - weekend.DayOfWeek)).AddHours (8);
                    Thread.Sleep (weekend - DateTime.UtcNow);
                    cli.SendTextAsync (
                        cqhttp.Cyan.Enums.MessageType.group_, target,
                        hintMsg
                    );
                    Thread.Sleep (DateTime.Now.AddHours (8) - DateTime.Now);
                    Task.Run (WRScraper.updateIndex);
                    var unsubmitted = (
                        from user in WRScraper.users.Keys where WRScraper.submittedUsers.Contains (user) select user
                    ).ToList ();
                    if (unsubmitted.Count > 0) {
                        string toSend = $"仍然有{unsubmitted.Count}名铁憨憨没有交周报。他们分别是:\n";
                        unsubmitted.ForEach ((s) => toSend += s + ',');
                        cli.SendTextAsync (
                            cqhttp.Cyan.Enums.MessageType.group_, target,
                            toSend
                        );
                    }
                }
            });
        }
        public static void Handle (Command cmd) {
            switch (cmd.operation) {
                case "status":
                    Task.Run (WRScraper.updateIndex);
                    cmd.endPoint.Item1.SendTextAsync (
                        cmd.endPoint.Item2,
                        $"{WRScraper.submittedUsers.Count}人已提交周报"
                    );
                    break;
            }
        }
    }
}