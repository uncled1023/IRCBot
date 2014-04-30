using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Bot.Modules
{
    class about : Module
    {
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == true)
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
                                case "about":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        string owner_num = " is";
                                        if (Conf.Owners.Count > 1)
                                        {
                                            owner_num = "s are";
                                        }
                                        string response = "IRCBot";
                                        if (this.Options["display_version"])
                                        {
                                            response += " v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".";
                                        }
                                        if (this.Options["display_creator"])
                                        {
                                            response += " Created by Uncled1023.";
                                        }
                                        if (this.Options["display_owner"])
                                        {
                                            if (Conf.Owners.Count >= 1)
                                            {
                                                response += " My owner" + owner_num + " ";
                                                if (Conf.Owners.Count > 1)
                                                {
                                                    int index = 1;
                                                    foreach (string owner in Conf.Owners)
                                                    {
                                                        response += owner;
                                                        if (index == Conf.Owners.Count - 1)
                                                        {
                                                            response += ", and ";
                                                        }
                                                        else if (index < Conf.Owners.Count)
                                                        {
                                                            response += ", ";
                                                        }
                                                        index++;
                                                    }
                                                }
                                                else
                                                {
                                                    response += Conf.Owners[0];
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
                                case "source":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (type.Equals("channel"))
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :https://github.com/uncled1023/IRCBot");
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :https://github.com/uncled1023/IRCBot");
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
                                    if (nick_access >= tmp_command.Access)
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
                                case "runtime":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        DateTime now = new DateTime();
                                        now = DateTime.Now;
                                        int days = now.Subtract(ircbot.controller.run_time).Days;
                                        int hours = now.Subtract(ircbot.controller.run_time).Hours;
                                        int minutes = now.Subtract(ircbot.controller.run_time).Minutes;
                                        int seconds = now.Subtract(ircbot.controller.run_time).Seconds;
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
                                            ircbot.sendData("PRIVMSG", channel + " :I have been running for " + uptime.Trim().TrimEnd(','));
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :I have been running for " + uptime.Trim().TrimEnd(','));
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
