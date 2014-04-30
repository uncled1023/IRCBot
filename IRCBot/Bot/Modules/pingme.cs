using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Modules
{
    class pingme : Module
    {
        private List<List<string>> ping_list = new List<List<string>>();
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
                                case "pingme":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                                        ircbot.sendData("PRIVMSG", nick + " :\u0001PING " + epoch.ToString() + "\u0001");
                                        List<string> tmp_list = new List<string>();
                                        string current_time = DateTime.Now.ToLongTimeString();
                                        tmp_list.Add(nick);
                                        tmp_list.Add(channel);
                                        tmp_list.Add(current_time);
                                        ping_list.Add(tmp_list);
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
            if (type.Equals("line") || type.Equals("query"))
            {
                check_ping(line, ircbot, nick);
            }
        }

        public void check_ping(string[] line, bot ircbot, string nick)
        {
            if (line.GetUpperBound(0) > 3)
            {
                if (line[3].Equals(":\u0001PING"))
                {
                    for (int x = 0; x < ping_list.Count(); x++)
                    {
                        if (ping_list[x][0].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                        {
                            DateTime current_time = DateTime.Now;
                            DateTime ping_time = Convert.ToDateTime(ping_list[x][2]);
                            TimeSpan dif_time = current_time.Subtract(ping_time);
                            string time_string = "";
                            if (dif_time.Days > 0)
                            {
                                time_string += dif_time.Days.ToString() + " Days, ";
                            }
                            if (dif_time.Hours > 0)
                            {
                                time_string += dif_time.Hours.ToString() + " Hours, ";
                            }
                            if (dif_time.Minutes > 0)
                            {
                                time_string += dif_time.Minutes.ToString() + " Minutes, ";
                            }
                            if (dif_time.Seconds > 0)
                            {
                                time_string += dif_time.Seconds.ToString() + " Seconds, ";
                            }
                            if (dif_time.Milliseconds > 0)
                            {
                                time_string += dif_time.Milliseconds.ToString() + " Milliseconds";
                            }
                            ircbot.sendData("PRIVMSG", ping_list[x][1] + " :" + nick + ", your ping is " + time_string.Trim().TrimEnd(','));
                            ping_list.RemoveAt(x);
                            break;
                        }
                    }
                }
            }
        }
    }
}
