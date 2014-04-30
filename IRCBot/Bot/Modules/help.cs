using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Bot.Modules
{
    class help : Module
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
                                case "help":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        display_help(line, nick, channel, nick_access, ircbot, Conf);
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

        private static void display_help(string[] line, string nick, string channel, int access, bot ircbot, BotConfig Conf)
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
                        foreach (Module tmp_module in Conf.Modules)
                        {
                            if (index == mod_num)
                            {
                                foreach (Command tmp_command in tmp_module.Commands)
                                {
                                    int command_access = tmp_command.Access;
                                    bool show_help = tmp_command.Show_Help;
                                    if (show_help == true)
                                    {
                                        char[] trm = ircbot.Conf.Command.ToCharArray();
                                        search_term = new_line[1].Trim().TrimStart(trm);
                                        bool trigger_found = tmp_command.Triggers.Contains(search_term);
                                        if (access >= command_access && trigger_found)
                                        {
                                            string alt = "";
                                            foreach (string trigger in tmp_command.Triggers)
                                            {
                                                if (!search_term.Equals(trigger, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    alt += ircbot.Conf.Command + trigger + ", ";
                                                }
                                            }
                                            if (!String.IsNullOrEmpty(alt))
                                            {
                                                alt = " | Alternate Commands: " + alt.Trim().TrimEnd(',');
                                            }
                                            ircbot.sendData("NOTICE", nick + " :Name: " + tmp_command.Name + alt);
                                            ircbot.sendData("NOTICE", nick + " :Usage: " + ircbot.Conf.Command + search_term + " " + tmp_command.Syntax);
                                            ircbot.sendData("NOTICE", nick + " :Description: " + tmp_command.Description);
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
                        foreach (Module tmp_module in Conf.Modules)
                        {
                            if (tmp_module.Class_Name.Equals(new_line[0], StringComparison.InvariantCultureIgnoreCase))
                            {
                                foreach (Command tmp_command in tmp_module.Commands)
                                {
                                    int command_access = tmp_command.Access;
                                    if (tmp_command.Show_Help)
                                    {
                                        char[] trm = ircbot.Conf.Command.ToCharArray();
                                        search_term = new_line[1].Trim().TrimStart(trm);
                                        bool trigger_found = tmp_command.Triggers.Contains(search_term);
                                        if (access >= command_access && trigger_found)
                                        {
                                            string alt = "";
                                            foreach (string trigger in tmp_command.Triggers)
                                            {
                                                if (!search_term.Equals(trigger, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    alt += ircbot.Conf.Command + trigger + ", ";
                                                }
                                            }
                                            if (!String.IsNullOrEmpty(alt))
                                            {
                                                alt = " | Alternate Commands: " + alt.Trim().TrimEnd(',');
                                            }
                                            ircbot.sendData("NOTICE", nick + " :Name: " + tmp_command.Name + alt);
                                            ircbot.sendData("NOTICE", nick + " :Usage: " + ircbot.Conf.Command + search_term + " " + tmp_command.Syntax);
                                            ircbot.sendData("NOTICE", nick + " :Description: " + tmp_command.Description);
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
                        foreach (Module tmp_module in Conf.Modules)
                        {
                            if (index == mod_num)
                            {
                                module_name = tmp_module.Class_Name;
                                foreach (Command tmp_command in tmp_module.Commands)
                                {
                                    if (tmp_command.Show_Help)
                                    {
                                        cmds += " " + ircbot.Conf.Command + tmp_command.Triggers[0] + ",";
                                    }
                                }
                                break;
                            }
                            index++;
                        }
                    }
                    catch
                    {
                        foreach (Module tmp_module in Conf.Modules)
                        {
                            if (tmp_module.Class_Name.Equals(line[4], StringComparison.InvariantCultureIgnoreCase))
                            {
                                module_name = tmp_module.Class_Name;
                                foreach (Command tmp_command in tmp_module.Commands)
                                {
                                    if (tmp_command.Show_Help)
                                    {
                                        cmds += " " + ircbot.Conf.Command + tmp_command.Triggers[0] + ",";
                                    }
                                }
                                break;
                            }
                        }
                    }
                    if (!String.IsNullOrEmpty(cmds))
                    {
                        ircbot.sendData("NOTICE", nick + " :Commands for " + module_name + ":" + cmds.TrimEnd(','));
                        cmds = "";
                        ircbot.sendData("NOTICE", nick + " :For more information about a specific command, type " + ircbot.Conf.Command + "help " + module_name + " {command name}");
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
                foreach (Module tmp_module in Conf.Modules)
                {
                    mods += " [" + index + "]" + tmp_module.Class_Name + ",";
                    index++;
                }
                if (!String.IsNullOrEmpty(mods))
                {
                    ircbot.sendData("NOTICE", nick + " :" + mods.TrimEnd(','));
                    mods = "";
                    ircbot.sendData("NOTICE", nick + " :To view the commands for a specific module, type " + ircbot.Conf.Command + "help {module_name or module_number}");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :No help information available.");
                }
            }
        }
    }
}
