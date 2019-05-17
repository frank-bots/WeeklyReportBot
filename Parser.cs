using System;
using System.Collections.Generic;
using cqhttp.Cyan.Enums;
using cqhttp.Cyan.Instance;

namespace WRBot {
    public struct Command {
        public string operation;
        public (CQApiClient, (MessageType, long)) endPoint;
        public List<string> parameters;
    }
    class CommandErrorException : Exception { };
    class Parser {
        public static Command ParseCommand (string raw, string user) {
            try {
                if (raw.Split (' ') [0][0] != '/')
                    throw new CommandErrorException ();
            } catch {
                throw new CommandErrorException ();
            }
            Command ret = new Command ();
            string command = raw.Split (' ') [0].Substring (1);
            ret.operation = command;
            ret.parameters = new List<string> ();
            ret.parameters.Add (user);
            if (raw.TrimEnd ().Length == command.Length) return ret;
            raw = raw.Substring (command.Length + 1).Trim ();

            for (int i = 0; i < raw.Length;) {
                int x = -1;
                if (raw[0] != '"') raw = raw.Insert (0, " ");
                switch (raw[i]) {
                    case '"':
                        x = raw.Substring (i + 1).IndexOf ('"');
                        if (x == -1) throw new CommandErrorException ();
                        ret.parameters.Add (raw.Substring (i + 1, x - i));
                        raw = raw.Substring (x + 2).Trim ();
                        i = 0;
                        break;
                    case ' ':
                        x = raw.Substring (i + 1).Trim ().IndexOf (' ');
                        if (x == -1) {
                            ret.parameters.Add (raw.Substring (i + 1).Trim ());
                            i = raw.Length; //break,break!
                        } else {
                            ret.parameters.Add (raw.Substring (i + 1, x - i));
                            raw = raw.Substring (x + 1).Trim ();
                            i = 0;
                        }
                        break;
                }
            }
            return ret;
        }
    }
}