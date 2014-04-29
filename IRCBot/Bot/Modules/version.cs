using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Bot.Modules
{
    class version : Module
    {
        private List<List<string>> version_list = new List<List<string>>();
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if (type.Equals("channel") && bot_command == true)
            {
                foreach (Command tmp_command in this.Commands)
                {
                    bool blocked = tmp_command.Blacklist.Contains(channel) || tmp_command.Blacklist.Contains(nick);
                    bool cmd_found = false;
                    bool spam_check = ircbot.get_spam_check(channel, nick, tmp_command.Spam_Check);
                    if (spam_check == true)
                    {
                        blocked = blocked || ircbot.get_spam_status(channel);
                    }
                    cmd_found = tmp_command.Triggers.Contains(command);
                    if (blocked == true && cmd_found == true)
                    {
                        ircbot.sendData("NOTICE", nick + " :I am currently too busy to process that.");
                    }
                    if (blocked == false && cmd_found == true)
                    {
                        foreach (string trigger in tmp_command.Triggers)
                        {
                            switch (trigger)
                            {
                                case "ver":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            ircbot.sendData("PRIVMSG", line[4].Trim() + " :\u0001VERSION\u0001");
                                            List<string> tmp_list = new List<string>();
                                            tmp_list.Add(line[4].Trim());
                                            tmp_list.Add(channel);
                                            version_list.Add(tmp_list);
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                        }
                                    }
                                    else
                                    {
                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            if (type.Equals("query"))
            {
                string version = ":\u0001VERSION\u0001";
                if (line[3] == version)
                {
                    ircbot.sendData("NOTICE", nick + " :\u0001VERSION IRCBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " on " + this.Options["bot_machine"] + "\u0001");
                }
            }
            if (type.Equals("line"))
            {
                string version_reply = ":\u0001VERSION";
                if (line.GetUpperBound(0) > 3)
                {
                    if (line[3].Equals(version_reply))
                    {
                        for (int x = 0; x < version_list.Count(); x++)
                        {
                            if (version_list[x][0].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                string response = "[" + nick + "] Using version: " + line[4].Replace("\u0001", "");
                                ircbot.sendData("PRIVMSG", version_list[x][1] + " :" + response);
                                version_list.RemoveAt(x);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
