using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot
{
    class roll_call
    {
        public void roll_call_control(string[] line, string command, bot ircbot, IRCConfig conf, int conf_id, int nick_access, string channel, string nick)
        {
            switch (command)
            {
                case "rollcall":
                    ircbot.spam_count++;
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            if (line[4].StartsWith("#"))
                            {
                                channel = line[4];
                            }
                            else
                            {
                                ircbot.sendData("PRIVMSG", nick + " :Please specify a valid channel");
                            }
                        }
                        string nicks = "";
                        for (int x = 0; x < ircbot.nick_list.Count(); x++)
                        {
                            if (ircbot.nick_list[x][0].Equals(channel))
                            {
                                for (int i = 1; i < ircbot.nick_list[x].Count(); i++)
                                {
                                    string[] split = ircbot.nick_list[x][i].Split(':');
                                    if (split.GetUpperBound(0) > 0)
                                    {
                                        nicks += split[1] + ", ";
                                    }
                                }
                                break;
                            }
                        }
                        ircbot.sendData("PRIVMSG", channel + " :" + conf.module_config[conf_id][2] + ": " + nicks.Trim().TrimEnd(','));
                    }
                    break;
            }
        }
    }
}
