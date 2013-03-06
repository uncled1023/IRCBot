using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot.Modules
{
    class logging : Module
    {
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == true)
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
                                    case "last":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] args = line[4].Split(' ');
                                                if (type.Equals("channel"))
                                                {
                                                    if (args.GetUpperBound(0) > 1)
                                                    {
                                                        int n;
                                                        bool isNumeric = int.TryParse(args[2], out n);
                                                        if (isNumeric)
                                                        {
                                                            display_log_nick_num(args[1], Convert.ToInt32(args[2]), channel, args[0], ircbot, conf);
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to specify a valid number.");
                                                        }
                                                    }
                                                    else if (args.GetUpperBound(0) > 0)
                                                    {
                                                        int n;
                                                        bool isNumeric = int.TryParse(args[1], out n);
                                                        if (isNumeric)
                                                        {
                                                            display_log_number(Convert.ToInt32(args[1]), channel, args[0], ircbot, conf);
                                                        }
                                                        else
                                                        {
                                                            display_log_nick(args[1], channel, args[0], ircbot, conf);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        display_log(channel, line[4], ircbot, conf);
                                                    }
                                                }
                                                else
                                                {
                                                    if (args.GetUpperBound(0) > 0)
                                                    {
                                                        display_log_nick(args[1], nick, args[0], ircbot, conf);
                                                    }
                                                    else
                                                    {
                                                        display_log(nick, line[4], ircbot, conf);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                display_last_log(channel, ircbot, conf);
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
            if (type.Equals("query") && bot_command == true)
            {
                bool command_valid = false;
                foreach (List<string> tmp_command in conf.command_list)
                {
                    string[] triggers = tmp_command[3].Split('|');
                    foreach (string trigger in triggers)
                    {
                        if (command.Equals(trigger))
                        {
                            command_valid = true;
                            break;
                        }
                    }
                    if (command_valid == true)
                    {
                        break;
                    }
                }
                if (command_valid == true)
                {
                    add_log(nick, "a private message", line, ircbot);
                }
            }
            if (type.Equals("channel") && bot_command == true)
            {
                bool command_valid = false;
                foreach (List<string> tmp_command in conf.command_list)
                {
                    string[] triggers = tmp_command[3].Split('|');
                    foreach (string trigger in triggers)
                    {
                        if (command.Equals(trigger))
                        {
                            command_valid = true;
                            break;
                        }
                    }
                    if (command_valid == true)
                    {
                        break;
                    }
                }
                if (command_valid == true)
                {
                    add_log(nick, channel, line, ircbot);
                }
            }
        }

        private void display_last_log(string channel, bot ircbot, IRCConfig conf)
        {
            string file_name = ircbot.server_name + ".log";
            bool cmd_found = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string parameters = "";
                    string date = "";
                    string inside = "";
                    string nick = "";
                    string command = "";
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 5);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[4] != "")
                            {
                                parameters = " with the following argument: " + new_line[4];
                            }
                            else
                            {
                                parameters = "";
                            }
                            date = new_line[2];
                            inside = new_line[1];
                            nick = new_line[0];
                            command = new_line[3];
                            cmd_found = true;
                        }
                    }
                    if (cmd_found == true)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :The last command used was " + conf.command + command + " by " + nick + " on " + date + " in " + inside + parameters);
                    }
                    else
                    {
                        ircbot.sendData("PRIVMSG", channel + " :No commands have been used");
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :No commands have been used");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :No commands have been used");
            }
        }

        private void display_log(string channel, string command, bot ircbot, IRCConfig conf)
        {
            string file_name = ircbot.server_name + ".log";
            bool cmd_found = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    int num_uses = 0;
                    string parameters = "";
                    string date = "";
                    string inside = "";
                    string nick = "";
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 5);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[3].Equals(command))
                            {
                                if (new_line[4] != "")
                                {
                                    parameters = " with the following argument: " + new_line[4];
                                }
                                else
                                {
                                    parameters = "";
                                }
                                date = new_line[2];
                                inside = new_line[1];
                                nick = new_line[0];
                                num_uses++;
                                cmd_found = true;
                            }
                        }
                    }
                    if (cmd_found == true)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + conf.command + command + " has been used " + num_uses + " times.");
                        ircbot.sendData("PRIVMSG", channel + " :It was last used by " + nick + " on " + date + " in " + inside + parameters);
                    }
                    else
                    {
                        string new_command = "";
                        foreach (string line in log_file)
                        {
                            char[] sep = new char[] { '*' };
                            string[] new_line = line.Split(sep, 5);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                if (new_line[0].Equals(command))
                                {
                                    if (new_line[4] != "")
                                    {
                                        parameters = " with the following argument: " + new_line[4];
                                    }
                                    else
                                    {
                                        parameters = "";
                                    }
                                    date = new_line[2];
                                    inside = new_line[1];
                                    nick = new_line[0];
                                    new_command = new_line[3];
                                    num_uses++;
                                    cmd_found = true;
                                }
                            }
                        }
                        if (cmd_found == true)
                        {
                            ircbot.sendData("PRIVMSG", channel + " :" + nick + " has used " + num_uses + " commands.");
                            ircbot.sendData("PRIVMSG", channel + " :The last command they used was " + conf.command + new_command + " on " + date + " in " + inside + parameters);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", channel + " :No results found");
                        }
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :No results found");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :No results found");
            }
        }

        private void display_log_number(int number, string channel, string command, bot ircbot, IRCConfig conf)
        {
            string file_name = ircbot.server_name + ".log";
            bool cmd_found = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                List<List<string>> command_list = new List<List<string>>();
                if (number_of_lines > 0)
                {
                    int num_uses = 0;
                    string parameters = "";
                    string date = "";
                    string inside = "";
                    string nick = "";
                    foreach (string line in log_file)
                    {
                        List<string> tmp_list = new List<string>();
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 5);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[3].Equals(command))
                            {
                                tmp_list.Add(new_line[0]);
                                tmp_list.Add(new_line[1]);
                                tmp_list.Add(new_line[2]);
                                tmp_list.Add(new_line[3]);
                                tmp_list.Add(new_line[4]);
                                command_list.Add(tmp_list);
                                num_uses++;
                                cmd_found = true;
                            }
                        }
                    }
                    if (cmd_found == true)
                    {
                        if (number < num_uses && number >= 0)
                        {
                            if (command_list[number][4] != "")
                            {
                                parameters = " with the following argument: " + command_list[number][4];
                            }
                            else
                            {
                                parameters = "";
                            }
                            date = command_list[number][2];
                            inside = command_list[number][1];
                            nick = command_list[number][0];
                            ircbot.sendData("PRIVMSG", channel + " :" + conf.command + command + " was used by " + nick + " on " + date + " in " + inside + parameters);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", channel + " :The command has not been used that many times");
                        }
                    }
                    else
                    {
                        foreach (string line in log_file)
                        {
                            List<string> tmp_list = new List<string>();
                            char[] sep = new char[] { '*' };
                            string[] new_line = line.Split(sep, 5);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                if (new_line[0].Equals(command))
                                {
                                    tmp_list.Add(new_line[0]);
                                    tmp_list.Add(new_line[1]);
                                    tmp_list.Add(new_line[2]);
                                    tmp_list.Add(new_line[3]);
                                    tmp_list.Add(new_line[4]);
                                    command_list.Add(tmp_list);
                                    num_uses++;
                                    cmd_found = true;
                                }
                            }
                        }
                        if (number < num_uses && number >= 0)
                        {
                            if (command_list[number][4] != "")
                            {
                                parameters = " with the following argument: " + command_list[number][4];
                            }
                            else
                            {
                                parameters = "";
                            }
                            date = command_list[number][2];
                            inside = command_list[number][1];
                            nick = command_list[number][0];
                            ircbot.sendData("PRIVMSG", channel + " :" + nick + " used " + conf.command + command_list[number][3] + " on " + date + " in " + inside + parameters);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", channel + " :The command has not been used that many times");
                        }
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + conf.command + command + " has not been used");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :" + conf.command + command + " has not been used");
            }
        }

        private void display_log_nick(string nick, string channel, string command, bot ircbot, IRCConfig conf)
        {
            string file_name = ircbot.server_name + ".log";
            bool cmd_found = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    int num_uses = 0;
                    string parameters = "";
                    string date = "";
                    string inside = "";
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 5);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[3].Equals(command))
                            {
                                if (new_line[4] != "")
                                {
                                    parameters = " with the following argument: " + new_line[4];
                                }
                                else
                                {
                                    parameters = "";
                                }
                                date = new_line[2];
                                inside = new_line[1];
                                num_uses++;
                                cmd_found = true;
                            }
                        }
                    }
                    if (cmd_found == true)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + nick + " has used " + conf.command + command + " " + num_uses + " times.");
                        ircbot.sendData("PRIVMSG", channel + " :They last used " + conf.command + command + " on " + date + " in " + inside + parameters);
                    }
                    else
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + nick + " has not used " + conf.command + command);
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + nick + " has not used any commands");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :" + nick + " has not used any commands");
            }
        }

        private void display_log_nick_num(string nick, int number, string channel, string command, bot ircbot, IRCConfig conf)
        {
            string file_name = ircbot.server_name + ".log";
            bool cmd_found = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                List<List<string>> command_list = new List<List<string>>();
                if (number_of_lines > 0)
                {
                    int num_uses = 0;
                    string parameters = "";
                    string date = "";
                    string inside = "";
                    foreach (string line in log_file)
                    {
                        List<string> tmp_list = new List<string>();
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 5);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[3].Equals(command))
                            {
                                tmp_list.Add(new_line[0]);
                                tmp_list.Add(new_line[1]);
                                tmp_list.Add(new_line[2]);
                                tmp_list.Add(new_line[3]);
                                tmp_list.Add(new_line[4]);
                                command_list.Add(tmp_list);
                                num_uses++;
                                cmd_found = true;
                            }
                        }
                    }
                    if (cmd_found == true)
                    {
                        if (number < num_uses && number >= 0)
                        {
                            if (command_list[number][4] != "")
                            {
                                parameters = " with the following argument: " + command_list[number][4];
                            }
                            else
                            {
                                parameters = "";
                            }
                            date = command_list[number][2];
                            inside = command_list[number][1];
                            nick = command_list[number][0];
                            ircbot.sendData("PRIVMSG", channel + " :" + nick + " used " + conf.command + command + " on " + date + " in " + inside + parameters);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", channel + " :" + nick + " has not used " + conf.command + command + " that many times");
                        }
                    }
                    else
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + nick + " has not used " + conf.command + command);
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + nick + " has not used any commands");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :" + nick + " has not used any commands");
            }
        }

        public void add_log(string nick, string channel, string[] line, bot ircbot)
        {
            string file_name = "";
            string msg = "";
            file_name = ircbot.server_name + ".log";
            DateTime current_date = DateTime.Now;
            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + "") == false)
            {
                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging");
            }
            if (line.GetUpperBound(0) > 3)
            {
                msg = line[4];
            }
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name))
            {
                StreamWriter log = File.AppendText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name);
                log.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + line[3].TrimStart(':').TrimStart('.') + "*" + msg);
                log.Close();
            }
            else
            {
                StreamWriter log_file = File.CreateText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "logging" + Path.DirectorySeparatorChar + file_name);
                log_file.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + line[3].TrimStart(':').TrimStart('.') + "*" + msg);
                log_file.Close();
            }
        }
    }
}
