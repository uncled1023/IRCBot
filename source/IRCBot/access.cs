using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace IRCBot
{
    class access
    {
        public void access_control(string[] line, string command, Interface ircbot, IRCConfig conf, int access_level, string nick)
        {
            switch (command)
            {
                case "access":
                    if (access_level >= 7)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] parse = line[4].Split(' ');
                            if (parse.GetUpperBound(0) > 1)
                            {
                                if (Convert.ToInt32(parse[2]) <= access_level)
                                {
                                    set_access_list(parse[0], parse[1], parse[2], ircbot);
                                }
                                else
                                {
                                    ircbot.sendData("NOTICE", nick + " :You do not have permission to change their access.");
                                }
                            }
                            else if (parse.GetUpperBound(0) > 0)
                            {
                                if (Convert.ToInt32(parse[1]) <= access_level)
                                {
                                    set_access_list(parse[0], line[2], parse[1], ircbot);
                                }
                                else
                                {
                                    ircbot.sendData("NOTICE", nick + " :You do not have permission to change their access.");
                                }
                            }
                            else
                            {
                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
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
                case "delaccess":
                    if (access_level >= 7)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] parse = line[4].Split(' ');
                            if (parse.GetUpperBound(0) > 1)
                            {
                                del_access_list(parse[0], parse[1], parse[2], ircbot);
                            }
                            else if (parse.GetUpperBound(0) > 0)
                            {
                                del_access_list(parse[0], line[2], parse[1], ircbot);
                            }
                            else
                            {
                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
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
                case "listaccess":
                    if (access_level >= 7)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            list_access_list(nick, line[4], ircbot);
                        }
                        else
                        {
                            list_access_list(nick, line[2], ircbot);
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
            }
        }

        public void list_access_list(string nick, string channel, Interface ircbot)
        {
            string file_name = "list.txt";

            if (File.Exists(ircbot.cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string access_msg = "";
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[1].Equals(channel))
                            {
                                access_msg += " | " + new_line[0] + ": " + new_line[2];
                            }
                        }
                    }
                    if (access_msg != "")
                    {
                        ircbot.sendData("NOTICE", nick + " :" + access_msg.Trim().TrimStart('|').Trim());
                        ircbot.sendData("NOTICE", nick + " :End of Access List");
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :No users in Access List.");
                    }
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :No users in Access List.");
                }
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :No users in Access List.");
            }
        }

        public void set_access_list(string nick, string channel, string access, Interface ircbot)
        {
            string file_name = "list.txt";
            DateTime current_date = DateTime.Now;

            if (Directory.Exists(ircbot.cur_dir + "\\modules\\access\\") == false)
            {
                Directory.CreateDirectory(ircbot.cur_dir + "\\modules\\access");
            }
            if (File.Exists(ircbot.cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                string[] new_file = new string[number_of_lines];
                int index = 0;
                bool nick_found = false;
                if (number_of_lines > 0)
                {
                    foreach (string lines in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = lines.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                string[] tmp_line = new_line[2].Split(',');
                                bool access_found = false;
                                foreach (string line in tmp_line)
                                {
                                    if (line.Equals(access))
                                    {
                                        access_found = true;
                                    }
                                }
                                if (access_found == false)
                                {
                                    if (new_line[2].Equals(""))
                                    {
                                        new_file[index] = new_line[0] + "*" + new_line[1] + "*" + access;
                                    }
                                    else
                                    {
                                        new_file[index] = new_line[0] + "*" + new_line[1] + "*" + new_line[2] + "," + access;
                                    }
                                }
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
                        StreamWriter log = File.AppendText(ircbot.cur_dir + "\\modules\\access\\" + file_name);
                        log.WriteLine(nick + "*" + channel + "*" + access);
                        log.Close();
                    }
                    else
                    {
                        System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\access\\" + file_name, new_file);
                    }
                }
                else
                {
                    StreamWriter log = File.AppendText(ircbot.cur_dir + "\\modules\\access\\" + file_name);
                    log.WriteLine(nick + "*" + channel + "*" + access);
                    log.Close();
                }
            }
            else
            {
                StreamWriter log_file = File.CreateText(ircbot.cur_dir + "\\modules\\access\\" + file_name);
                log_file.WriteLine(nick + "*" + channel + "*" + access);
                log_file.Close();
            }

            for (int x = 0; x < ircbot.nick_list.Count(); x++)
            {
                if (ircbot.nick_list[x][0].Equals(channel))
                {
                    for (int i = 1; i < ircbot.nick_list[x].Count(); i++)
                    {
                        string[] split = ircbot.nick_list[x][i].Split(':');
                        if (split[1].Equals(nick))
                        {
                            int old_access = Convert.ToInt32(split[0]);
                            int new_access = Convert.ToInt32(access);
                            if (old_access > new_access)
                            {
                                new_access = old_access;
                            }
                            ircbot.nick_list[x][i] = new_access.ToString() + ":" + nick;
                            break;
                        }
                    }
                }
            }
        }

        public string get_access_list(string nick, string channel, Interface ircbot)
        {
            string file_name = "list.txt";
            string access = "";

            if (File.Exists(ircbot.cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                access = new_line[2];
                                break;
                            }
                        }
                    }
                }
            }
            return access;
        }

        public void del_access_list(string nick, string channel, string access, Interface ircbot)
        {
            string file_name = "list.txt";
            DateTime current_date = DateTime.Now;

            if (Directory.Exists(ircbot.cur_dir + "\\modules\\access\\") == false)
            {
                Directory.CreateDirectory(ircbot.cur_dir + "\\modules\\access");
            }
            if (File.Exists(ircbot.cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                string[] new_file = new string[number_of_lines];
                int index = 0;
                if (number_of_lines > 0)
                {
                    foreach (string lines in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = lines.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                string[] tmp_line = new_line[2].Split(',');
                                bool access_found = false;
                                string new_access = "";
                                foreach (string line in tmp_line)
                                {
                                    if (line.Equals(access))
                                    {
                                    }
                                    else
                                    {
                                        new_access += "," + line;
                                    }
                                }
                                if (access_found == false)
                                {
                                    if (new_access.TrimStart(',').TrimEnd(',') != "")
                                    {
                                        new_file[index] = new_line[0] + "*" + new_line[1] + "*" + new_access.TrimStart(',').TrimEnd(',');
                                        index++;
                                    }
                                }
                            }
                            else
                            {
                                new_file[index] = lines;
                                index++;
                            }
                        }
                    }
                    System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\access\\" + file_name, new_file);
                }
            }

            for (int x = 0; x < ircbot.nick_list.Count(); x++)
            {
                if (ircbot.nick_list[x][0].Equals(channel))
                {
                    for (int i = 1; i < ircbot.nick_list[x].Count(); i++)
                    {
                        string[] split = ircbot.nick_list[x][i].Split(':');
                        if (split[1].Equals(nick))
                        {
                            int new_access = ircbot.get_user_access(nick, channel);
                            ircbot.nick_list[x][i] = new_access.ToString() + ":" + nick;
                            break;
                        }
                    }
                }
            }
        }
    }
}
