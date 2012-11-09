using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class idle : Module
    {
        private List<string> idle_list = new List<string>();

        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
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
                                    case "idle":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool nick_found = false;
                                            foreach (string idle_nick in idle_list)
                                            {
                                                if (idle_nick.Equals(nick))
                                                {
                                                    nick_found = true;
                                                    break;
                                                }
                                            }
                                            if (nick_found == false)
                                            {
                                                idle_list.Add(nick);
                                                ircbot.sendData("NOTICE", nick + " :You are now set as idle.  Type .deidle to come back.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You are already idle.  Type .deidle to come back.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "deidle":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool nick_found = false;
                                            foreach (string idle_nick in idle_list)
                                            {
                                                if (idle_nick.Equals(nick))
                                                {
                                                    nick_found = true;
                                                    break;
                                                }
                                            }
                                            if (nick_found == true)
                                            {
                                                idle_list.Remove(nick);
                                                ircbot.sendData("NOTICE", nick + " :Welcome back!");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You are already idle.  Type .deidle to come back.");
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

        public bool check_idle(string nick)
        {
            bool nick_found = false;
            foreach (string idle_nick in idle_list)
            {
                if (idle_nick.Equals(nick))
                {
                    nick_found = true;
                    break;
                }
            }
            return nick_found;
        }
    }
}
