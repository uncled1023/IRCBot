using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot.Modules
{
    class help : Module
    {
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
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
                                    case "help":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            display_help(line, nick, line[2], nick_access, ircbot, conf);
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

        private void display_help(string[] line, string nick, string channel, int access, bot ircbot, IRCConfig conf)
        {
            string search_term = "";
            string msg = "";
            if (line.GetUpperBound(0) > 3)
            {
                string[] new_line = line[4].Split(' ');
                if (new_line.GetUpperBound(0) > 0)
                {
                    foreach (List<string> tmp_command in conf.command_list)
                    {
                        if (new_line[0].Equals(tmp_command[0]))
                        {
                            string[] triggers = tmp_command[3].Split('|');
                            int command_access = Convert.ToInt32(tmp_command[5]);
                            bool show_help = Convert.ToBoolean(tmp_command[7]);
                            if (show_help == true)
                            {
                                foreach (string trigger in triggers)
                                {
                                    if (access >= Convert.ToInt32(command_access))
                                    {
                                        search_term = new_line[1];
                                        if (search_term.ToLower().Equals(trigger.ToLower()))
                                        {
                                            ircbot.sendData("NOTICE", nick + " :" + tmp_command[1] + " | Usage: " + conf.command + trigger + " " + tmp_command[4] + " | Description: " + tmp_command[2]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    string module_name = "";
                    foreach (List<string> module in conf.module_config)
                    {
                        if (module[0].Equals(line[4]))
                        {
                            module_name = module[1];
                        }
                    }
                    foreach (List<string> tmp_command in conf.command_list)
                    {
                        if (line[4].Equals(tmp_command[0]))
                        {
                            string[] triggers = tmp_command[3].Split('|');
                            int command_access = Convert.ToInt32(tmp_command[5]);
                            bool show_help = Convert.ToBoolean(tmp_command[7]);
                            if (show_help == true)
                            {
                                foreach (string trigger in triggers)
                                {
                                    if (access >= Convert.ToInt32(command_access))
                                    {
                                        msg += " " + conf.command + trigger + ",";
                                    }
                                }
                            }
                        }
                    }
                    if (msg != "")
                    {
                        ircbot.sendData("NOTICE", nick + " :Commands for " + line[4] + ":" + msg.TrimEnd(','));
                        msg = "";
                        ircbot.sendData("NOTICE", nick + " :For more information about a specific command, type " + conf.command + "help {module} {command name}");
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :There are no commands for " + line[4]);
                    }
                }
            }
            else
            {
                msg += "Modules Available:";
                foreach (List<string> tmp_module in conf.module_config)
                {
                    string module_name = tmp_module[0];
                    int commands = 0;
                    foreach (List<string> tmp_command in conf.command_list)
                    {
                        if (module_name.Equals(tmp_command[0]))
                        {
                            string[] triggers = tmp_command[3].Split('|');
                            int command_access = Convert.ToInt32(tmp_command[5]);
                            bool show_help = Convert.ToBoolean(tmp_command[7]);
                            if (show_help == true)
                            {
                                foreach (string trigger in triggers)
                                {
                                    if (access >= Convert.ToInt32(command_access))
                                    {
                                        commands++;
                                    }
                                }
                            }
                        }
                    }
                    if (commands >= 1)
                    {
                        msg += " " + module_name;
                        msg += " [" + commands.ToString() + "]" + ",";
                    }
                }
                if (msg != "")
                {
                    ircbot.sendData("NOTICE", nick + " :" + msg.TrimEnd(','));
                    msg = "";
                    ircbot.sendData("NOTICE", nick + " :To view the commands for a specific module, type " + conf.command + "help {module}");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :No help information available.");
                }
            }
        }
    }
}
