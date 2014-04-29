using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;

namespace Bot.Modules
{
    class alarm : Module
    {
        private List<System.Timers.Timer> alarms;
        private BotConfig tmp_conf;

        public alarm()
        {
            alarms = new List<System.Timers.Timer>();
        }

        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            char[] charS = new char[] { ' ' };
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
                                case "alarm":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] new_line = line[4].Split(charS, 2, StringSplitOptions.RemoveEmptyEntries);
                                            if (new_line.GetUpperBound(0) > 0)
                                            {
                                                bool int_allowed = true;
                                                int time = 0;
                                                try
                                                {
                                                    time = Convert.ToInt32(new_line[0]);
                                                    if ((time * 1000) <= 0)
                                                    {
                                                        int_allowed = false;
                                                    }
                                                }
                                                catch
                                                {
                                                    int_allowed = false;
                                                }
                                                if (int_allowed == true)
                                                {
                                                    char[] charSplit = new char[] { ' ' };
                                                    string[] ex = new_line[1].Split(charSplit);
                                                    if (ex[0].TrimStart(Convert.ToChar(ircbot.Conf.Command)).Equals("alarm"))
                                                    {
                                                        if (type.Equals("channel"))
                                                        {
                                                            ircbot.sendData("PRIVMSG", line[2] + " :Recursion is bad.");
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", nick + " :Recursion is bad.");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        tmp_conf = Conf;
                                                        Timer alarm_trigger = new Timer();
                                                        alarm_trigger.Interval = (time * 1000);
                                                        alarm_trigger.Enabled = true;
                                                        alarm_trigger.AutoReset = false;
                                                        alarm_trigger.Elapsed += (sender, e) => ring_alarm(sender, e, ircbot, nick, line[0], nick_access, channel, type, new_line[1]);
                                                        alarms.Add(alarm_trigger);

                                                        if (type.Equals("channel"))
                                                        {
                                                            ircbot.sendData("PRIVMSG", line[2] + " :Alarm added for " + new_line[0] + " seconds from now.");
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", nick + " :Alarm added for " + new_line[0] + " seconds from now.");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (type.Equals("channel"))
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", please pick a valid time.");
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", nick + " :" + nick + ", please pick a valid time.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (type.Equals("channel"))
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (type.Equals("channel"))
                                            {
                                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", nick + " :" + nick + ", you need to include more info.");
                                            }
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

        public void ring_alarm(object sender, EventArgs e, bot ircbot, string nick, string full_nick, int nick_access, string channel, string type, string msg)
        {
            BotConfig Conf = tmp_conf;
            System.Timers.Timer alarm_trigger = (System.Timers.Timer)sender;
            alarm_trigger.Enabled = false;
            if (msg.StartsWith(ircbot.Conf.Command))
            {
                string chan = "";
                if (type.Equals("channel"))
                {
                    chan = channel;
                }
                else
                {
                    chan = Conf.Nick;
                }

                char[] charSplit = new char[] { ' ' };
                string[] ex = msg.TrimStart(Convert.ToChar(Conf.Command)).Split(charSplit, 2);
                string[] args;
                if (ex.GetUpperBound(0) > 0)
                {
                    args = ex[1].Split(charSplit);
                }
                else
                {
                    args = null;
                }
                ircbot.controller.run_command(Conf.Server_Name, nick, chan, ex[0], args);
            }
            else
            {
                ircbot.sendData("PRIVMSG", nick + " :ALARM: " + msg);
            }
        }
    }
}
