using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot
{
    class idle
    {
        private List<string> idle_list = new List<string>();
        public void idle_control(string[] line, string command, bot ircbot, IRCConfig conf, int access_level, string nick)
        {
            switch (command)
            {
                case "idle":
                    if (access_level >= ircbot.get_command_access(command))
                    {
                        bool nick_found = false;
                        foreach (string idle_nick in idle_list)
                        {
                            if(idle_nick.Equals(nick))
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
                    if (access_level >= ircbot.get_command_access(command))
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
