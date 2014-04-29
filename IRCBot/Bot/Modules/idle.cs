using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Modules
{
    class idle : Module
    {
        private List<string> idle_list = new List<string>();

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
                                case "idle":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (check_idle(nick) == false)
                                        {
                                            idle_list.Add(nick);
                                            ircbot.sendData("NOTICE", nick + " :You are now set as idle.  Type " + ircbot.Conf.Command + "deidle to come back.");
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You are already idle.  Type " + ircbot.Conf.Command + "deidle to come back.");
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
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (check_idle(nick) == true)
                                        {
                                            idle_list.Remove(nick);
                                            ircbot.sendData("NOTICE", nick + " :Welcome back!");
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You aren't idle.  Type " + ircbot.Conf.Command + "idle to be set idle.");
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

        public bool check_idle(string nick)
        {
            bool nick_found = false;
            foreach (string idle_nick in idle_list)
            {
                if (idle_nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    nick_found = true;
                    break;
                }
            }
            return nick_found;
        }
    }
}
