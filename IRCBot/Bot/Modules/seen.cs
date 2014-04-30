using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Bot.Modules
{
    class seen : Module
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
                                case "seen":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            display_seen(line[4].Trim(), line[2], ircbot);
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
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
            if (type.Equals("line") || type.Equals("channel") || type.Equals("invite") || type.Equals("join") || type.Equals("mode") || type.Equals("part") || type.Equals("quit") || type.Equals("nick"))
            {
                add_seen(nick.Trim(), channel, line, ircbot);
            }
        }

        public static DateTime get_seen_time(string nick, string channel, bot ircbot)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.Conf.Server_Name + "_#" + tab_name + ".log";
            DateTime past_date = new DateTime();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 4);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick, StringComparison.InvariantCultureIgnoreCase) && new_line[1].Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                            {
                                past_date = DateTime.Parse(new_line[2]);
                                break;
                            }
                        }
                    }
                }
            }
            return past_date;
        }

        private static void display_seen(string nick, string channel, bot ircbot)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.Conf.Server_Name + "_#" + tab_name + ".log";
            bool nick_found = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 4);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick, StringComparison.InvariantCultureIgnoreCase) && new_line[1].Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                            {
                                DateTime current_date = DateTime.Now;
                                DateTime past_date = DateTime.Parse(new_line[2]);
                                string difference_second = "00";
                                string difference_minute = "00";
                                string difference_hour = "00";
                                string difference_day = "00";
                                difference_second = current_date.Subtract(past_date).ToString("ss");
                                difference_minute = current_date.Subtract(past_date).ToString("mm");
                                difference_hour = current_date.Subtract(past_date).ToString("hh");
                                difference_day = current_date.Subtract(past_date).ToString("dd");
                                string difference = "";
                                if (difference_day != "00")
                                {
                                    difference += " " + difference_day + " days,";
                                }
                                if (difference_hour != "00")
                                {
                                    difference += " " + difference_hour + " hours,";
                                }
                                if (difference_minute != "00")
                                {
                                    difference += " " + difference_minute + " minutes,";
                                }
                                if (difference_second != "00")
                                {
                                    difference += " " + difference_second + " seconds,";
                                }
                                ircbot.sendData("PRIVMSG", channel + " :I last saw " + nick + " " + difference.Trim().TrimEnd(',') + " ago " + new_line[3]);
                                nick_found = true;
                                break;
                            }
                        }
                    }
                    if (nick_found == false)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :I have not seen " + nick);
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :I have not seen " + nick);
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :I have not seen " + nick);
            }            
        }

        public static void add_seen(string nick, string channel, string[] line, bot ircbot)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = "";
            if (channel.StartsWith("#"))
            {
                file_name = ircbot.Conf.Server_Name + "_#" + tab_name + ".log";
                DateTime current_date = DateTime.Now;
                string msg = "";
                line[1] = line[1];
                if (line[1].Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                {
                    msg = "quitting";
                }
                else if (line[1].Equals("join", StringComparison.InvariantCultureIgnoreCase))
                {
                    msg = "joining " + channel;
                }
                else if (line[1].Equals("part", StringComparison.InvariantCultureIgnoreCase))
                {
                    msg = "leaving " + channel;
                }
                else if (line[1].Equals("kick", StringComparison.InvariantCultureIgnoreCase))
                {
                    msg = "getting kicked from " + channel;
                }
                else if (line[1].Equals("mode", StringComparison.InvariantCultureIgnoreCase))
                {
                    msg = "setting mode " + " in " + channel;
                }
                else if (line[1].Equals("invite", StringComparison.InvariantCultureIgnoreCase))
                {
                    msg = "setting mode " + " in " + channel;
                }
                else if (line[1].Equals("privmsg", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (line.GetUpperBound(0) > 3)
                    {
                        msg = "saying: " + line[3].TrimStart(':') + " " + line[4];
                    }
                    else
                    {
                        msg = "saying: " + line[3].TrimStart(':');
                    }
                }
                else
                {
                    msg = "";
                }
                if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + "") == false)
                {
                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen");
                }
                if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name))
                {
                    string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name);
                    int number_of_lines = log_file.GetUpperBound(0) + 1;
                    List<string> new_file = new List<string>();
                    bool nick_found = false;
                    if (number_of_lines > 0)
                    {
                        foreach (string lines in log_file)
                        {
                            char[] sep = new char[] { '*' };
                            string[] new_line = lines.Split(sep, 4);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                if (new_line[0].Equals(nick, StringComparison.InvariantCultureIgnoreCase) && new_line[1].Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    new_file.Add(new_line[0] + "*" + new_line[1] + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                                    nick_found = true;
                                }
                                else
                                {
                                    new_file.Add(lines);
                                }
                            }
                        }
                        if (nick_found == false)
                        {
                            StreamWriter log = File.AppendText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name);
                            log.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                            log.Close();
                        }
                        else
                        {
                            System.IO.File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name, new_file);
                        }
                    }
                    else
                    {
                        StreamWriter log = File.AppendText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name);
                        log.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                        log.Close();
                    }
                }
                else
                {
                    StreamWriter log_file = File.CreateText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name);
                    log_file.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                    log_file.Close();
                }
            }
        }
    }
}
