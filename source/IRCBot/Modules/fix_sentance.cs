using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IRCBot.Modules
{
    class fix_sentance : Module
    {
        private List<List<string>> nick_logs = new List<List<string>>();
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if (type.Equals("channel") && bot_command == false)
            {
                string full_line = "";
                if (line.GetUpperBound(0) > 3)
                {
                    full_line = line[3].TrimStart(':') + " " + line[4];
                }
                else
                {
                    full_line = line[3].TrimStart(':');
                }
                Regex reg = new Regex(@"((^s\/)(([^\/]*)+|([^\/]*)+\/\/([^\/]*)+)\/(([^\/]*)+|([^\/]*)+\/\/([^\/]*)+)\/(([g|I]+)|$))", RegexOptions.None);
                MatchCollection matches = reg.Matches(full_line);
                if (matches.Count > 0)
                {
                    bool nick_found = false;
                    string past_line = "";
                    foreach (List<string> nick_log in nick_logs)
                    {
                        if (nick_log[0].Equals(channel) && nick_log[1].Equals(nick))
                        {
                            nick_found = true;
                            past_line = nick_log[2];
                            break;
                        }
                    }
                    if (nick_found)
                    {
                        string[] parts = matches[0].ToString().Split('/');
                        string pattern = parts[1];
                        string replace = parts[2];
                        string order = "";
                        if (parts.GetUpperBound(0) > 2)
                        {
                            order = parts[3];
                        }

                        RegexOptions reg_options = new RegexOptions();
                        reg_options = RegexOptions.Singleline;
                        bool recurse = false;
                        foreach (char c in order.ToCharArray())
                        {
                            if (c.Equals('I'))
                            {
                                reg_options = RegexOptions.IgnoreCase;
                            }
                            else if (c.Equals('g'))
                            {
                                recurse = true;
                            }
                        }

                        Regex line_reg = new Regex(pattern, reg_options);
                        string result = "";
                        if (recurse)
                        {
                            result = line_reg.Replace(past_line, replace);
                        }
                        else
                        {
                            result = line_reg.Replace(past_line, replace, 1);
                        }
                        for (int x = 0; x < nick_logs.Count(); x++)
                        {
                            if (nick_logs[x][0].Equals(channel) && nick_logs[x][1].Equals(nick))
                            {
                                nick_logs[x][2] = full_line;
                                break;
                            }
                        }
                        ircbot.sendData("PRIVMSG", channel + " :[" + nick + "] " + result);
                    }
                    else
                    {
                        ircbot.sendData("PRIVMSG", channel + " :I don't remember what you said earlier.");
                    }
                }
                else
                {
                    bool nick_found = false;
                    for (int x = 0; x < nick_logs.Count(); x++)
                    {
                        if (nick_logs[x][0].Equals(channel) && nick_logs[x][1].Equals(nick))
                        {
                            nick_found = true;
                            nick_logs[x][2] = full_line;
                            break;
                        }
                    }
                    if (!nick_found)
                    {
                        List<string> tmp_list = new List<string>();
                        tmp_list.Add(channel);
                        tmp_list.Add(nick);
                        tmp_list.Add(full_line);
                        nick_logs.Add(tmp_list);
                    }
                }
            }
        }
    }
}
