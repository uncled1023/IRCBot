using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IRCBot.Modules
{
    class seen : Module
    {
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
                            if (ircbot.spam_activated == true)
                            {
                                blocked = true;
                            }
                        }
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(command))
                            {
                                cmd_found = true;
                                break;
                            }
                        }
                        if (blocked == false && cmd_found == true)
                        {
                            foreach (string trigger in triggers)
                            {
                                switch (trigger)
                                {
                                    case "seen":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                display_seen(line[4].ToLower(), line[2], ircbot);
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
            }
            if (type.Equals("line") || type.Equals("channel") || type.Equals("join") || type.Equals("mode") || type.Equals("part") || type.Equals("quit") || type.Equals("nick"))
            {
                add_seen(nick, channel, line, ircbot);
            }
        }

        private void display_seen(string nick, string channel, bot ircbot)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.server_name + "_#" + tab_name + ".log";
            bool nick_found = false;
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
                            if (new_line[0].ToLower().Equals(nick) && new_line[1].Equals(channel))
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

        public void add_seen(string nick, string channel, string[] line, bot ircbot)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = "";
            if (channel.StartsWith("#"))
            {
                file_name = ircbot.server_name + "_#" + tab_name + ".log";
                DateTime current_date = DateTime.Now;
                string msg = "";
                line[1] = line[1].ToLower();
                if (line[1].Equals("quit"))
                {
                    msg = "quitting";
                }
                else if (line[1].Equals("join"))
                {
                    msg = "joining " + channel;
                }
                else if (line[1].Equals("part"))
                {
                    msg = "leaving " + channel;
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
                if (Directory.Exists(ircbot.cur_dir + "\\modules\\seen\\") == false)
                {
                    Directory.CreateDirectory(ircbot.cur_dir + "\\modules\\seen");
                }
                if (File.Exists(ircbot.cur_dir + "\\modules\\seen\\" + file_name))
                {
                    string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\seen\\" + file_name);
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
                                if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
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
}
