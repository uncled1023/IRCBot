using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IRCBot
{
    class seen
    {
        public void seen_control(string[] line, string command, Interface ircbot, int nick_access, string nick, StreamReader sr)
        {
            switch (command)
            {
                case "seen":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        display_seen(line[4], line[2], ircbot, sr);
                    }
                    break;
            }
        }

        private void display_seen(string nick, string channel, Interface ircbot, StreamReader sr)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = "#" + tab_name + ".log";
            bool nick_found = false;
            bool nick_here = false;

            ircbot.sendData("NAMES", channel);

            string name_line = sr.ReadLine();
            char[] charSeparator = new char[] { ' ' };
            string[] name_lines = name_line.Split(charSeparator, 5);
            string[] names_list = name_lines[4].Split(':');
            string[] names = names_list[1].Split(' ');
            for (int x = 0; x <= names.GetUpperBound(0); x++)
            {
                string new_name = names[x].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+');
                if (nick == new_name)
                {
                    nick_here = true;
                }
            }
            while (name_lines[4] != ":End of /NAMES list.")
            {
                name_line = sr.ReadLine();
                name_lines = name_line.Split(charSeparator, 5);
            }
            if (nick_here == true)
            {
                ircbot.sendData("PRIVMSG", channel + " :" + nick + " is right here!");
            }
            else
            {
                if (File.Exists(ircbot.cur_dir + "\\modules\\seen\\" + file_name))
                {
                    string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\seen\\" + file_name);
                    int number_of_lines = log_file.GetUpperBound(0) + 1;
                    if (number_of_lines > 0)
                    {
                        foreach (string line in log_file)
                        {
                            char[] sep = new char[] { '*' };
                            string[] new_line = line.Split(sep, 4);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
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
                                    ircbot.sendData("PRIVMSG", channel + " :I last saw " + nick + " " + difference.TrimEnd(',') + " ago " + new_line[3]);
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
        }

        public void add_seen(string nick, string channel, string[] line, Interface ircbot)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = "";
            if (channel.StartsWith("#"))
            {
                file_name = "#" + tab_name + ".log";
            }
            else
            {
                file_name = tab_name + ".log";
            }
            DateTime current_date = DateTime.Now;
            string msg = "";
            line[1] = line[1].ToLower();
            if (line[1].Equals("quit"))
            {
                msg = "Quitting";
            }
            else if (line[1].Equals("join"))
            {
                msg = "Joining " + channel;
            }
            else if (line[1].Equals("part"))
            {
                msg = "Leaving " + channel;
            }
            else if (line[1].Equals("kick"))
            {
                msg = "getting kicked from " + channel;
            }
            else if (line[1].Equals("mode"))
            {
                msg = "setting mode " + " in " + channel;
            }
            else if (line[1].Equals("privmsg"))
            {
                if (line.GetUpperBound(0) > 3)
                {
                    msg = "Saying: " + line[3].TrimStart(':') + " " + line[4];
                }
                else
                {
                    msg = "Saying: " + line[3].TrimStart(':');
                }
            }
            else
            {
                msg = "";
            }
            if (Directory.Exists(ircbot.cur_dir + "\\modules\\seen\\") == false)
            {
                Directory.CreateDirectory(ircbot.cur_dir + "\\modules\\seen");
            }
            if (File.Exists(ircbot.cur_dir + "\\modules\\seen\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\seen\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                string[] new_file = new string[number_of_lines];
                int index = 0;
                bool nick_found = false;
                if (number_of_lines > 0)
                {
                    foreach (string lines in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = lines.Split(sep, 4);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                new_file[index] = new_line[0] + "*" + new_line[1] + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg;
                                nick_found = true;
                            }
                            else
                            {
                                new_file[index] = lines;
                            }
                            index++;
                        }
                    }
                    if (nick_found == false)
                    {
                        StreamWriter log = File.AppendText(ircbot.cur_dir + "\\modules\\seen\\" + file_name);
                        log.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                        log.Close();
                    }
                    else
                    {
                        System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\seen\\" + file_name, new_file);
                    }
                }
                else
                {
                    StreamWriter log = File.AppendText(ircbot.cur_dir + "\\modules\\seen\\" + file_name);
                    log.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                    log.Close();
                }
            }
            else
            {
                StreamWriter log_file = File.CreateText(ircbot.cur_dir + "\\modules\\seen\\" + file_name);
                log_file.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                log_file.Close();
            }
        }
    }
}
