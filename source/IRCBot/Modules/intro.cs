using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot.Modules
{
    class intro : Module
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
                                    case "intro":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                add_intro(nick, line[2], line, ircbot);
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
                                    case "introdelete":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            // Delete Introduction
                                            delete_intro(nick, line[2], ircbot);
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
        }

        public void check_intro(string nick, string channel, bot ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\intro\\" + ircbot.server_name + "_list.txt";
            if (File.Exists(list_file))
            {
                string line;

                // Read the file and display it line by line.
                System.IO.StreamReader file = new System.IO.StreamReader(list_file);
                while ((line = file.ReadLine()) != null)
                {
                    char[] charSeparator = new char[] { ':' };
                    string[] intro_nick = line.Split(charSeparator, 3);
                    if (nick.Equals(intro_nick[0]) && channel.Equals(intro_nick[1]))
                    {
                        string[] intro_line = intro_nick[2].Split('|');
                        int number_of_responses = intro_line.GetUpperBound(0) + 1;
                        Random random = new Random();
                        int index = random.Next(0, number_of_responses);
                        ircbot.sendData("PRIVMSG", channel + " : " + intro_line[index]);
                    }
                }
                file.Close();
            }
        }

        private void add_intro(string nick, string channel, string[] line, bot ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\intro\\" + ircbot.server_name + "_list.txt";
            string add_line = nick + ":" + channel + ":";
            bool found_nick = false;
            if (line.GetUpperBound(0) > 3)
            {
                for (int x = 4; x <= line.GetUpperBound(0); x++)
                {
                    add_line += line[x] + " ";
                }
                if (File.Exists(list_file))
                {
                    int counter = 0;
                    string[] old_file = System.IO.File.ReadAllLines(list_file);
                    string[] new_file = new string[old_file.GetUpperBound(0) + 2];
                    foreach (string file_line in old_file)
                    {
                        char[] charSeparator = new char[] { ':' };
                        string[] intro_nick = file_line.Split(charSeparator, 3);
                        if (nick.Equals(intro_nick[0]) && channel.Equals(intro_nick[1]))
                        {
                            new_file[counter] = add_line;
                            found_nick = true;
                        }
                        else
                        {
                            new_file[counter] = file_line;
                        }
                        counter++;
                    }
                    if (found_nick == false)
                    {
                        new_file[counter] = add_line;
                    }
                    System.IO.File.WriteAllLines(@list_file, new_file);
                }
                else
                {
                    System.IO.File.WriteAllText(@list_file, add_line);
                }
                ircbot.sendData("PRIVMSG", channel + " :Your introduction will be proclaimed as you wish.");
            }
        }

        private void delete_intro(string nick, string channel, bot ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\intro\\" + ircbot.server_name + "_list.txt";
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 1];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { ':' };
                    string[] intro_nick = file_line.Split(charSeparator, 3);
                    if (nick.Equals(intro_nick[0]) && channel.Equals(intro_nick[1]))
                    {
                    }
                    else
                    {
                        new_file[counter] = file_line;
                        counter++;
                    }
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
            }
            ircbot.sendData("PRIVMSG", channel + " :Your introduction has been removed.");
        }
    }
}
