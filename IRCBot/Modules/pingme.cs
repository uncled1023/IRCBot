using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class pingme : Module
    {
        private List<List<string>> ping_list = new List<List<string>>();
        public override void control(bot ircbot, ref BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
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
                        bool spam_check = ircbot.get_spam_check(channel, nick, Convert.ToBoolean(tmp_command[8]));
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
                            blocked = ircbot.get_spam_status(channel);
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
                                    case "pingme":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
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
