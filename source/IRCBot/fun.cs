using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot
{
    class fun
    {
        private System.Timers.Timer timer = new System.Timers.Timer();

        public fun()
        {
        }

        public void fun_control(string[] line, string command, Interface ircbot, int nick_access, string nick, string channel)
        {
            switch (command)
            {
                case "love":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
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
                    break;
                case "hug":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
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
                    break;
                case "slap":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
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
                    break;
                case "bots":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :Reporting in!");
                    }
                    break;
            }
        }
    }
}
