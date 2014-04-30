using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Bot.Modules
{
    class access : Module
    {
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
                                case "setaccess":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] parse = line[4].Split(' ');
                                            if (type.Equals("query") && parse.GetUpperBound(0) > 1)
                                            {
                                                if (Convert.ToInt32(parse[2]) <= nick_access)
                                                {
                                                    set_access_list(parse[0].Trim(), parse[1], parse[2], ircbot);
                                                    ircbot.sendData("NOTICE", nick + " :" + parse[0].Trim() + " has been added to access level " + parse[2]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You do not have permission to change their access.");
                                                }
                                            }
                                            else if (type.Equals("channel") && parse.GetUpperBound(0) > 0)
                                            {
                                                if (Convert.ToInt32(parse[1]) <= nick_access)
                                                {
                                                    set_access_list(parse[0].Trim(), channel, parse[1], ircbot);
                                                    ircbot.sendData("NOTICE", nick + " :" + parse[0].Trim() + " has been added to access level " + parse[1]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You do not have permission to change their access.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
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
                                case "delaccess":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] parse = line[4].Split(' ');
                                            if (type.Equals("query") && parse.GetUpperBound(0) > 1)
                                            {
                                                if (Convert.ToInt32(parse[2]) <= nick_access)
                                                {
                                                    del_access_list(parse[0].Trim(), parse[1], parse[2], ircbot);
                                                    ircbot.sendData("NOTICE", nick + " :" + parse[0].Trim() + " has been removed from access level " + parse[2]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You do not have permission to change their access.");
                                                }
                                            }
                                            else if (type.Equals("channel") && parse.GetUpperBound(0) > 0)
                                            {
                                                if (Convert.ToInt32(parse[1]) <= nick_access)
                                                {
                                                    del_access_list(parse[0].Trim(), channel, parse[1], ircbot);
                                                    ircbot.sendData("NOTICE", nick + " :" + parse[0].Trim() + " has been removed from access level " + parse[1]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You do not have permission to change their access.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
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
                                case "listaccess":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (type.Equals("query") && line.GetUpperBound(0) > 3)
                                        {
                                            list_access_list(nick, line[4], ircbot);
                                        }
                                        else if (type.Equals("channel"))
                                        {
                                            list_access_list(nick, channel, ircbot);
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
                                case "getaccess":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] new_line = line[4].Split(' ');
                                            if (new_line.GetUpperBound(0) > 0 && new_line[0].StartsWith("#"))
                                            {
                                                int viewed_access = ircbot.get_nick_access(new_line[1].Trim(), new_line[0].Trim());
                                                ircbot.sendData("NOTICE", nick + " :" + new_line[1].Trim() + " has access level " + viewed_access.ToString());
                                            }
                                            else if (type.Equals("channel"))
                                            {
                                                int viewed_access = ircbot.get_nick_access(line[4].Trim(), channel);
                                                ircbot.sendData("NOTICE", nick + " :" + line[4].Trim() + " has access level " + viewed_access.ToString());
                                            }
                                            else
                                            {
                                                int viewed_access = Conf.Default_Level;
                                                foreach (Channel_Info chan in Conf.Channel_List)
                                                {
                                                    int tmp_nick_access = ircbot.get_nick_access(line[4].Trim(), chan.Channel);
                                                    if (tmp_nick_access > viewed_access)
                                                    {
                                                        viewed_access = tmp_nick_access;
                                                    }
                                                }
                                                ircbot.sendData("NOTICE", nick + " :" + line[4].Trim() + " has access level " + viewed_access.ToString());
                                            }
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

        public void list_access_list(string nick, string channel, bot ircbot)
        {
            string file_name = ircbot.Conf.Server_Name + "_list.txt";

            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name);
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
                    if (!String.IsNullOrEmpty(access_msg))
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

        public void set_access_list(string nick, string channel, string access, bot ircbot)
        {
            string file_name = ircbot.Conf.Server_Name + "_list.txt";

            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + "") == false)
            {
                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access");
            }
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                List<string> new_file = new List<string>();
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
                            if (new_line[0].Trim().Equals(nick, StringComparison.InvariantCultureIgnoreCase) && new_line[1].Trim().Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                            {
                                string[] tmp_line = new_line[2].Trim().Split(',');
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
                                    if (String.IsNullOrEmpty(new_line[2].Trim()))
                                    {
                                        new_file.Add(new_line[0].Trim() + "*" + new_line[1].Trim() + "*" + access);
                                    }
                                    else
                                    {
                                        new_file.Add(new_line[0].Trim() + "*" + new_line[1].Trim() + "*" + new_line[2].Trim() + "," + access);
                                    }
                                }
                                nick_found = true;
                            }
                            else
                            {
                                new_file.Add(lines);
                            }
                            index++;
                        }
                    }
                    if (nick_found == false)
                    {
                        StreamWriter log = File.AppendText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name);
                        log.WriteLine(nick + "*" + channel + "*" + access);
                        log.Close();
                    }
                    else
                    {
                        System.IO.File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name, new_file);
                    }
                }
                else
                {
                    StreamWriter log = File.AppendText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name);
                    log.WriteLine(nick + "*" + channel + "*" + access);
                    log.Close();
                }
            }
            else
            {
                StreamWriter log_file = File.CreateText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name);
                log_file.WriteLine(nick.Trim() + "*" + channel + "*" + access.Trim());
                log_file.Close();
            }
        }

        public List<int> get_access_list(string nick, string channel, bot ircbot)
        {
            string file_name = ircbot.Conf.Server_Name + "_list.txt";
            List<int> access = new List<int>();

            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 1)
                        {
                            if (channel != null)
                            {
                                if (new_line[0].Trim().Equals(nick, StringComparison.InvariantCultureIgnoreCase) && new_line[1].Trim().Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    try
                                    {
                                        access.Add(Convert.ToInt32(new_line[2]));
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            return access;
        }

        public void del_access_list(string nick, string channel, string access, bot ircbot)
        {
            string file_name = ircbot.Conf.Server_Name + "_list.txt";

            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + "") == false)
            {
                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access");
            }
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                List<string> new_file = new List<string>();
                if (number_of_lines > 0)
                {
                    foreach (string lines in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = lines.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Trim().Equals(nick, StringComparison.InvariantCultureIgnoreCase) && new_line[1].Trim().Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                            {
                                string[] tmp_line = new_line[2].Trim().Split(',');
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
                                if (String.IsNullOrEmpty(new_access.TrimStart(',').TrimEnd(',')))
                                {
                                    new_file.Add(new_line[0].Trim() + "*" + new_line[1].Trim() + "*" + new_access.TrimStart(',').TrimEnd(','));
                                }
                            }
                            else
                            {
                                new_file.Add(lines);
                            }
                        }
                    }
                    System.IO.File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "access" + Path.DirectorySeparatorChar + file_name, new_file);
                }
            }
        }
    }
}
