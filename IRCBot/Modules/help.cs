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

        private void display_help(string[] line, string nick, string channel, int access, bot ircbot, BotConfig conf)
        {
            string search_term = "";
            string cmds = "";
            if (line.GetUpperBound(0) > 3)
            {
                string[] new_line = line[4].Split(' ');
                if (new_line.GetUpperBound(0) > 0)
                {
                    try
                    {
                        int mod_num = Convert.ToInt32(new_line[0]);
                        int index = 1;
                        foreach (List<string> tmp_module in conf.module_config)
                        {
                            if (index == mod_num)
                            {
                                string module_name = tmp_module[0];
                                foreach (List<string> tmp_command in conf.command_list)
                                {
                                    if (module_name.Equals(tmp_command[0], StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        string[] triggers = tmp_command[3].Split('|');
                                        int command_access = Convert.ToInt32(tmp_command[5]);
                                        bool show_help = Convert.ToBoolean(tmp_command[7]);
                                        if (show_help == true)
                                        {
                                            char[] trm = ircbot.conf.command.ToCharArray();
                                            search_term = new_line[1].Trim().TrimStart(trm);
                                            bool trigger_found = false;
                                            foreach (string trigger in triggers)
                                            {
                                                if (search_term.Equals(trigger, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    trigger_found = true;
                                                    break;
                                                }
                                            }
                                            if (access >= Convert.ToInt32(command_access) && trigger_found)
                                            {
                                                string alt = "";
                                                foreach (string trigger in triggers)
                                                {
                                                    if (!search_term.Equals(trigger, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        alt += ircbot.conf.command + trigger + ", ";
                                                    }
                                                }
                                                if (!alt.Equals(string.Empty))
                                                {
                                                    alt = " | Alternate Commands: " + alt.Trim().TrimEnd(',');
                                                }
                                                ircbot.sendData("NOTICE", nick + " :Name: " + tmp_command[1] + alt);
                                                ircbot.sendData("NOTICE", nick + " :Usage: " + ircbot.conf.command + search_term + " " + tmp_command[4]);
                                                ircbot.sendData("NOTICE", nick + " :Description: " + tmp_command[2]);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                            index++;
                        }
                    }
                    catch
                    {
                        foreach (List<string> tmp_module in conf.module_config)
                        {
                            if (tmp_module[0].Equals(new_line[0], StringComparison.InvariantCultureIgnoreCase))
                            {
                                string module_name = tmp_module[0];
                                foreach (List<string> tmp_command in conf.command_list)
                                {
                                    if (module_name.Equals(tmp_command[0], StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        string[] triggers = tmp_command[3].Split('|');
                                        int command_access = Convert.ToInt32(tmp_command[5]);
                                        bool show_help = Convert.ToBoolean(tmp_command[7]);
                                        if (show_help == true)
                                        {
                                            char[] trm = ircbot.conf.command.ToCharArray();
                                            search_term = new_line[1].Trim().TrimStart(trm);
                                            bool trigger_found = false;
                                            foreach (string trigger in triggers)
                                            {
                                                if (search_term.Equals(trigger, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    trigger_found = true;
                                                    break;
                                                }
                                            }
                                            if (access >= Convert.ToInt32(command_access) && trigger_found)
                                            {
                                                string alt = "";
                                                foreach (string trigger in triggers)
                                                {
                                                    if (!search_term.Equals(trigger, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        alt += ircbot.conf.command + trigger + ", ";
                                                    }
                                                }
                                                if(!alt.Equals(string.Empty))
                                                {
                                                    alt = " | Alternate Commands: " + alt.Trim().TrimEnd(',');
                                                }
                                                ircbot.sendData("NOTICE", nick + " :Name: " + tmp_command[1] + alt);
                                                ircbot.sendData("NOTICE", nick + " :Usage: " + ircbot.conf.command + search_term + " " + tmp_command[4]);
                                                ircbot.sendData("NOTICE", nick + " :Description: " + tmp_command[2]);
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    string module_name = "";
                    try
                    {
                        int mod_num = Convert.ToInt32(line[4]);
                        int index = 1;
                        foreach (List<string> tmp_module in conf.module_config)
                        {
                            if (index == mod_num)
                            {
                                module_name = tmp_module[0];
                                foreach (List<string> tmp_command in conf.command_list)
                                {
                                    if (module_name.Equals(tmp_command[0], StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        string[] triggers = tmp_command[3].Split('|');
                                        int command_access = Convert.ToInt32(tmp_command[5]);
                                        bool show_help = Convert.ToBoolean(tmp_command[7]);
                                        if (show_help == true)
                                        {
                                            if (triggers.GetUpperBound(0) >= 0)
                                            {
                                                cmds += " " + ircbot.conf.command + triggers[0] + ",";
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                            index++;
                        }
                    }
                    catch
                    {
                        foreach (List<string> tmp_module in conf.module_config)
                        {
                            if (tmp_module[0].Equals(line[4], StringComparison.InvariantCultureIgnoreCase))
                            {
                                module_name = tmp_module[0];
                                foreach (List<string> tmp_command in conf.command_list)
                                {
                                    if (module_name.Equals(tmp_command[0], StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        string[] triggers = tmp_command[3].Split('|');
                                        int command_access = Convert.ToInt32(tmp_command[5]);
                                        bool show_help = Convert.ToBoolean(tmp_command[7]);
                                        if (show_help == true)
                                        {
                                            if (triggers.GetUpperBound(0) >= 0)
                                            {
                                                cmds += " " + ircbot.conf.command + triggers[0] + ",";
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    if (cmds != "")
                    {
                        ircbot.sendData("NOTICE", nick + " :Commands for " + module_name + ":" + cmds.TrimEnd(','));
                        cmds = "";
                        ircbot.sendData("NOTICE", nick + " :For more information about a specific command, type " + ircbot.conf.command + "help " + module_name + " {command name}");
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :There are no commands for " + module_name);
                    }
                }
            }
            else
            {
                string mods = "Modules Available:";
                int index = 1;
                foreach (List<string> tmp_module in conf.module_config)
                {
                    string module_name = tmp_module[0];
                    mods += " [" + index + "]" + module_name + ",";
                    index++;
                }
                if (mods != "")
                {
                    ircbot.sendData("NOTICE", nick + " :" + mods.TrimEnd(','));
                    mods = "";
                    ircbot.sendData("NOTICE", nick + " :To view the commands for a specific module, type " + ircbot.conf.command + "help {module_name or module_number}");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :No help information available.");
                }
            }
        }
    }
}
