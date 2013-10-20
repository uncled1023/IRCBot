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
        public override void control(bot ircbot, BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if (type.Equals("channel") && bot_command == true)
            {
                foreach (List<string> tmp_command in conf.command_list)
                {
                    if (module_name.Equals(tmp_command[0]))
                    {
                        string[] triggers = tmp_command[3].Split('|');
                        int command_access = Convert.ToInt32(tmp_command[5]);
                        string[] blacklist = tmp_command[6].Split(',');
                        bool blocked = false;
                        bool cmd_found = false;
                        bool spam_check = ircbot.get_spam_check(channel, nick, Convert.ToBoolean(tmp_command[8]));
                        foreach (string bl_chan in blacklist)
                        {
                            if (bl_chan.Equals(channel))
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (spam_check == true)
                        {
                            blocked = ircbot.get_spam_status(channel);
                        }
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(command))
                            {
                                cmd_found = true;
                                break;
                            }
                        }
                        if (blocked == true && cmd_found == true)
                        {
                            ircbot.sendData("NOTICE", nick + " :I am currently too busy to process that.");
                        }
                        if (blocked == false && cmd_found == true)
                        {
                            foreach (string trigger in triggers)
                            {
                                switch (trigger)
                                {
                                    case "ver":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
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
            }
            if (type.Equals("query"))
            {
                string version = ":\u0001VERSION\u0001";
                if (line[3] == version)
                {
                    ircbot.sendData("NOTICE", nick + " :\u0001VERSION IRCBot v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " on " + conf.module_config[module_id][3] + "\u0001");
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
