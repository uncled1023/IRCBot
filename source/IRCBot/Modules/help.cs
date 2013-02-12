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
                            if (ircbot.spam_activated == true)
                            {
                                blocked = true;
                            }
                        }
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(command))
                            {
                                cmd_found = true;
                                break;
                            }
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
                                            ircbot.spam_count++;
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
            bool more_info = false;
            foreach (List<string> tmp_command in conf.command_list)
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
                            if (line.GetUpperBound(0) > 3)
                            {
                                more_info = true;
                                search_term = line[4];
                                if (search_term.ToLower().Equals(trigger.ToLower()))
                                {
                                    ircbot.sendData("NOTICE", nick + " :" + tmp_command[1] + " | Usage: " + conf.command + trigger + " " + tmp_command[4] + " | Description: " + tmp_command[2]);
                                }
                            }
                            else
                            {
                                msg += " " + conf.command + trigger + ",";
                            }
                        }
                    }
                }
            }
            if (more_info == false)
            {
                if (msg != "")
                {
                    ircbot.sendData("NOTICE", nick + " :" + msg.TrimEnd(','));
                    msg = "";
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :No help information available.");
                }
                ircbot.sendData("NOTICE", nick + " :For more information about a specific command, type .help <command name>");
            }
        }
    }
}
