using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class fun : Module
    {
        private System.Timers.Timer timer = new System.Timers.Timer();

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
                                    case "love":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                Random random = new Random();
                                                int ran_num = random.Next(0, 3);
                                                switch (ran_num)
                                                {
                                                    case 0:
                                                        ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION Gently makes love to " + line[4] + "\u0001");
                                                        break;
                                                    case 1:
                                                        ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION sings a love ballad to " + line[4] + "\u0001");
                                                        break;
                                                    case 2:
                                                        ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION slowly sneaks up behind " + line[4] + "\u0001");
                                                        ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION squeezes " + line[4] + " tightly\u0001");
                                                        ircbot.sendData("PRIVMSG", line[4] + " :I'll give you some more later tonight.  ;)");
                                                        break;
                                                    case 3:
                                                        ircbot.sendData("PRIVMSG", channel + " :I love you " + line[4] + "!  Sooo much!");
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "hug":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION hugs " + line[4] + "\u0001");
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION hugs " + nick + "\u0001");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "slap":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION slaps " + line[4] + " with a large trout\u0001");
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION slaps " + nick + " with a large trout\u0001");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "bots":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :Reporting in!");
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "br":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :HUEHUEHUE");
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "net":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :Sure is enterprise quality in here");
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
}
