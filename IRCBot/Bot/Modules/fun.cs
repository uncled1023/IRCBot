using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Modules
{
    class fun : Module
    {
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if (type.Equals("channel") && bot_command == true)
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
                                case "love":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
