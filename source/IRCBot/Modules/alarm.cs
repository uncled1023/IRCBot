using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;

namespace IRCBot.Modules
{
    class alarm : Module
    {
        private List<System.Timers.Timer> alarms;
        private IRCConfig tmp_conf;

        public alarm()
        {
            alarms = new List<System.Timers.Timer>();
        }

        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            access access = new access();

            char[] charS = new char[] { ' ' };
            string module_name = ircbot.conf.module_config[module_id][0];
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == true)
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
                            blocked = ircbot.get_spam_status(channel, nick);
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
                                    case "alarm":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
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
                                                        if (ex[0].TrimStart(Convert.ToChar(conf.command)).Equals("alarm"))
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
                                                            tmp_conf = conf;
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
        }

        public void ring_alarm(object sender, EventArgs e, bot ircbot, string nick, string full_nick, int nick_access, string channel, string type, string msg)
        {
            IRCConfig conf = tmp_conf;
            System.Timers.Timer alarm_trigger = (System.Timers.Timer)sender;
            alarm_trigger.Enabled = false;
            if (msg.StartsWith(conf.command))
            {
                bool bot_command = true;
                string line = "";
                if (type.Equals("channel"))
                {
                    line = ":" + full_nick + " PRIVMSG " + channel + " :" + msg;
                }
                else
                {
                    line = ":" + full_nick + " PRIVMSG " + conf.nick + " :" + msg;
                }

                char[] charSplit = new char[] { ' ' };
                string[] ex = line.Split(charSplit, 5);
                string[] ignored_nicks = conf.ignore_list.Split(',');
                bool run_modules = true;
                foreach (string ignore_nick in ignored_nicks)
                {
                    if (ignore_nick.ToLower().Equals(nick))
                    {
                        run_modules = false;
                        break;
                    }
                }
                if (run_modules)
                {
                    //Run Enabled Modules
                    List<Modules.Module> tmp_module_list = new List<Modules.Module>();
                    tmp_module_list.AddRange(ircbot.module_list);
                    foreach (Modules.Module module in tmp_module_list)
                    {
                        int index = 0;
                        bool module_found = false;
                        string module_blacklist = "";
                        foreach (List<string> conf_module in conf.module_config)
                        {
                            if (module.ToString().Equals("IRCBot.Modules." + conf_module[0]))
                            {
                                module_blacklist = conf_module[2];
                                module_found = true;
                                break;
                            }
                            index++;
                        }
                        if (module_found == true)
                        {
                            char[] sepComma = new char[] { ',' };
                            char[] sepSpace = new char[] { ' ' };
                            string[] blacklist = module_blacklist.Split(sepComma, StringSplitOptions.RemoveEmptyEntries);
                            bool module_allowed = true;
                            foreach (string blacklist_node in blacklist)
                            {
                                string[] nodes = blacklist_node.Split(sepSpace, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string node in nodes)
                                {
                                    if (node.ToLower().Equals(nick) || node.ToLower().TrimStart('#').Equals(channel.ToLower().TrimStart('#')))
                                    {
                                        module_allowed = false;
                                        break;
                                    }
                                }
                                if (module_allowed == false)
                                {
                                    break;
                                }
                            }
                            if (module_allowed == true)
                            {
                                module.control(ircbot, ref conf, index, ex, ex[3].TrimStart(':').TrimStart(Convert.ToChar(conf.command)), nick_access, nick, channel, bot_command, type);
                            }
                        }
                    }
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", nick + " :ALARM: " + msg);
            }
        }
    }
}
