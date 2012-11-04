using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IRCBot
{
    class quote
    {
        public void quote_control(string[] line, string command, bot ircbot, IRCConfig conf, int conf_id, int nick_access, string nick)
        {
            switch (command)
            {
                case "quote":
                    ircbot.spam_count++;
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (conf.module_config[conf_id][2].Equals("True"))
                        {
                            if (line.GetUpperBound(0) > 3)
                            {
                                get_specific_quote(line[2], line[4], ircbot, conf);
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

        public void add_quote(string nick, string channel, string[] line, bot ircbot, IRCConfig conf)
        {
            if (!nick.Equals(conf.nick) && !line[3].Remove(0, 1).StartsWith(conf.command))
            {
                string tab_name = channel.TrimStart('#');
                string pattern = "[^a-zA-Z0-9]"; //regex pattern
                string new_tab_name = Regex.Replace(tab_name, pattern, "_");
                string file_name = ircbot.server_name + "_#" + new_tab_name + ".log";
                if (Directory.Exists(ircbot.cur_dir + "\\modules\\quotes\\logs") == false)
                {
                    Directory.CreateDirectory(ircbot.cur_dir + "\\modules\\quotes\\logs");
                }
                StreamWriter log_file = File.AppendText(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name);
                if (line.GetUpperBound(0) > 3)
                {
                    log_file.WriteLine(line[3].Remove(0, 1) + " " + line[4] + " [" + nick + "]");
                }
                else
                {
                    log_file.WriteLine(line[3].Remove(0, 1) + " [" + nick + "]");
                }
                log_file.Close();
            }
        }

        private void get_quote(string channel, bot ircbot, IRCConfig conf)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.server_name + "_#" + tab_name + ".log";
            if (File.Exists(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name);
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
                    ircbot.sendData("PRIVMSG", channel + " :" + line);
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
            if (File.Exists(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    string line_nick = "";
                    bool nick_found = false;
                    List<List<string>> quote_list = new List<List<string>>();
                    foreach (string file_line in log_file)
                    {
                        nick_found = false;
                        string[] tmp_line = file_line.Split('[');
                        line_nick = tmp_line[tmp_line.GetUpperBound(0)].TrimEnd(']');
                        for (int x = 0; x < quote_list.Count(); x++)
                        {
                            if(quote_list[x][0].Equals(line_nick.ToLower()))
                            {
                                nick_found = true;
                                quote_list[x].Add(file_line);
                                break;
                            }
                        }
                        if (nick_found == false)
                        {
                            List<string> tmp_list = new List<string>();
                            tmp_list.Add(line_nick.ToLower());
                            tmp_list.Add(file_line);
                            quote_list.Add(tmp_list);
                        }
                    }
                    line_nick = "";
                    line = "";
                    while (line == "")
                    {
                        nick_found = false;
                        int quote_index = 0;
                        for (int x = 0; x < quote_list.Count(); x++)
                        {
                            if (quote_list[x][0].Equals(nick.Trim().ToLower()))
                            {
                                nick_found = true;
                                quote_index = x;
                                break;
                            }
                        }
                        if (nick_found == true)
                        {
                            Random random = new Random();
                            number_of_lines = quote_list[quote_index].Count();
                            int index = random.Next(1, number_of_lines);
                            line = quote_list[quote_index][index];
                            line_nick = quote_list[quote_index][0];
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (nick_found == true)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + line);
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
