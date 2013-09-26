using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class about : Module
    {
        public override void control(bot ircbot, ref BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if (type.Equals("channel") || type.Equals("query") && bot_command == true)
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
                        bool spam_check = Convert.ToBoolean(tmp_command[8]);
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
                                    case "about":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            AboutBox1 about = new AboutBox1();
                                            string[] owners = conf.owner.TrimStart(',').TrimEnd(',').Split(',');
                                            string owner_num = " is";
                                            if (owners.GetUpperBound(0) > 0)
                                            {
                                                owner_num = "s are";
                                            }
                                            string response = "IRCBot";
                                            if (conf.module_config[module_id][3].Equals("True"))
                                            {
                                                response += " v" + about.AssemblyVersion + ".";
                                            }
                                            if (conf.module_config[module_id][4].Equals("True"))
                                            {
                                                response += " Created by Uncled1023.";
                                            }
                                            if (conf.module_config[module_id][5].Equals("True"))
                                            {
                                                if (owners.GetUpperBound(0) > 0)
                                                {
                                                    response += " My owner" + owner_num + " ";
                                                    if (owners.GetUpperBound(0) > 1)
                                                    {
                                                        int index = 0;
                                                        foreach (string owner in owners)
                                                        {
                                                            response += owner;
                                                            if (index == owners.GetUpperBound(0) - 1)
                                                            {
                                                                response += ", and ";
                                                            }
                                                            else if (index < owners.GetUpperBound(0))
                                                            {
                                                                response += ", ";
                                                            }
                                                            index++;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        response += conf.owner.TrimStart(',').TrimEnd(',').Replace(",", " and ");
                                                    }
                                                }
                                            }
                                            if (type.Equals("channel"))
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :" + response);
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + response);
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "uptime":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            DateTime now = new DateTime();
                                            now = DateTime.Now;
                                            int days = now.Subtract(ircbot.start_time).Days;
                                            int hours = now.Subtract(ircbot.start_time).Hours;
                                            int minutes = now.Subtract(ircbot.start_time).Minutes;
                                            int seconds = now.Subtract(ircbot.start_time).Seconds;
                                            string uptime = "";
                                            if (days > 0)
                                            {
                                                uptime += days + " days, ";
                                            }
                                            if (hours > 0)
                                            {
                                                uptime += hours + " hours, ";
                                            }
                                            if (minutes > 0)
                                            {
                                                uptime += minutes + " minutes, ";
                                            }
                                            if (seconds > 0)
                                            {
                                                uptime += seconds + " seconds, ";
                                            }
                                            if (type.Equals("channel"))
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :I have been online for " + uptime.Trim().TrimEnd(','));
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :I have been online for " + uptime.Trim().TrimEnd(','));
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
        }
    }
}
