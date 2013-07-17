using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IRCBot.Modules
{
    class quote : Module
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
                                    case "quote":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (conf.module_config[module_id][3].Equals("True"))
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    get_specific_quote(line[2], line[4].ToLower(), ircbot, conf);
                                                }
                                                else
                                                {
                                                    get_quote(line[2], ircbot, conf);
                                                }
                                            }
                                            else
                                            {
                                                get_quote(line[2], ircbot, conf);
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
            if (type.Equals("channel") && bot_command == false && nick != conf.nick)
            {
                add_quote(nick, channel, line, ircbot, conf);
            }
        }

        public void add_quote(string nick, string channel, string[] line, bot ircbot, IRCConfig conf)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            string new_tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.server_name + "_#" + new_tab_name + ".log";
            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "quotes" + Path.DirectorySeparatorChar + "logs") == false)
            {
                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "quotes" + Path.DirectorySeparatorChar + "logs");
            }
            StreamWriter log_file = File.AppendText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "quotes" + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + file_name);
            if (line.GetUpperBound(0) > 3)
            {
                log_file.WriteLine(nick + "*" + line[3].Remove(0, 1) + " " + line[4]);
            }
            else
            {
                log_file.WriteLine(nick + "*" + line[3].Remove(0, 1));
            }
            log_file.Close();
        }

        private void get_quote(string channel, bot ircbot, IRCConfig conf)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.server_name + "_#" + tab_name + ".log";
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "quotes" + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "quotes" + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    while (line == "")
                    {
                        Random random = new Random();
                        int index = random.Next(0, number_of_lines);
                        line = log_file[index];
                    }
                    char[] charSep = new char[] { '*' };
                    string[] lines = line.Split(charSep, 2);
                    ircbot.sendData("PRIVMSG", channel + " :" + lines[1] + " [" + lines[0] + "]");
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
            }
        }

        private void get_specific_quote(string channel, string nick, bot ircbot, IRCConfig conf)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.server_name + "_#" + tab_name + ".log";
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "quotes" + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "quotes" + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    string line_nick = "";
                    bool nick_found = false;
                    List<string> quote_list = new List<string>();
                    foreach (string file_line in log_file)
                    {
                        char[] charSep = new char[] { '*' };
                        string[] tmp_line = file_line.Split(charSep, 2);
                        line_nick = tmp_line[0];
                        if (nick.Trim().ToLower().Equals(line_nick.Trim().ToLower()))
                        {
                            nick_found = true;
                            quote_list.Add(file_line);
                        }
                    }
                    line_nick = "";
                    line = "";
                    if (nick_found == true)
                    {
                        while (line == "")
                        {
                            Random random = new Random();
                            number_of_lines = quote_list.Count();
                            int index = random.Next(1, number_of_lines + 1);
                            line = quote_list[index - 1];
                        }
                        char[] charSep = new char[] { '*' };
                        string[] lines = line.Split(charSep, 2);
                        ircbot.sendData("PRIVMSG", channel + " :" + lines[1] + " [" + lines[0] + "]");
                    }
                    else
                    {
                        ircbot.sendData("PRIVMSG", channel + " :There are currently no logs for " + nick);
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
            }
        }
    }
}
