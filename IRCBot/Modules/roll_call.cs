using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class roll_call : Module
    {
        public override void control(bot ircbot, ref BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
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
                                    case "rollcall":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                if (line[4].StartsWith("#"))
                                                {
                                                    channel = line[4];
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", nick + " :Please specify a valid channel");
                                                }
                                            }
                                            string nicks = "";
                                            for (int x = 0; x < ircbot.nick_list.Count(); x++)
                                            {
                                                if (ircbot.nick_list[x][0].Equals(channel))
                                                {
                                                    for (int i = 1; i < ircbot.nick_list[x].Count(); i++)
                                                    {
                                                        string[] split = ircbot.nick_list[x][i].Split(':');
                                                        if (split.GetUpperBound(0) > 0)
                                                        {
                                                            nicks += split[1] + ", ";
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                            ircbot.sendData("PRIVMSG", channel + " :" + conf.module_config[module_id][3] + ": " + nicks.Trim().TrimEnd(','));
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
