using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Bot.Modules
{
    class intro : Module
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
                                case "intro":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            // Add introduction
                                            string char_limit = this.Options["max_char"];
                                            add_intro(nick, line[2], line, ircbot, Convert.ToInt32(char_limit));
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
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
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
            if (type.Equals("join"))
            {
                check_intro(nick, channel.TrimStart(':'), ircbot);
            }
        }

        public static void check_intro(string nick, string channel, bot ircbot)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "intro" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_list.txt";
            if (File.Exists(list_file))
            {
                string line;

                // Read the file and display it line by line.
                System.IO.StreamReader file = new System.IO.StreamReader(list_file);
                while ((line = file.ReadLine()) != null)
                {
                    char[] charSeparator = new char[] { ':' };
                    string[] intro_nick = line.Split(charSeparator, 3);
                    if (nick.Equals(intro_nick[0], StringComparison.InvariantCultureIgnoreCase) && channel.Equals(intro_nick[1]))
                    {
                        string[] intro_line = intro_nick[2].Split('|');
                        int number_of_responses = intro_line.GetUpperBound(0) + 1;
                        Random random = new Random();
                        int index = random.Next(0, number_of_responses);
                        ircbot.sendData("PRIVMSG", channel + " :\u200B" + intro_line[index]);
                    }
                }
                file.Close();
            }
        }

        private static void add_intro(string nick, string channel, string[] line, bot ircbot, int char_limit)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "intro" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_list.txt";
            string add_line = nick + ":" + channel + ":";
            bool found_nick = false;
            if (line.GetUpperBound(0) > 3)
            {
                int intro_length = line[4].Length;
                if (intro_length <= char_limit)
                {
                    add_line += line[4] + " ";
                    if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "intro"))
                    {
                        Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "intro");
                    }
                    if (File.Exists(list_file))
                    {
                        string[] old_file = System.IO.File.ReadAllLines(list_file);
                        List<string> new_file = new List<string>();
                        foreach (string file_line in old_file)
                        {
                            char[] charSeparator = new char[] { ':' };
                            string[] intro_nick = file_line.Split(charSeparator, 3);
                            if (nick.Equals(intro_nick[0], StringComparison.InvariantCultureIgnoreCase) && channel.Equals(intro_nick[1]))
                            {
                                new_file.Add(add_line);
                                found_nick = true;
                            }
                            else
                            {
                                new_file.Add(file_line);
                            }
                        }
                        if (found_nick == false)
                        {
                            new_file.Add(add_line);
                        }
                        System.IO.File.WriteAllLines(@list_file, new_file);
                    }
                    else
                    {
                        System.IO.File.WriteAllText(@list_file, add_line);
                    }
                    ircbot.sendData("NOTICE", nick + " :Your introduction is as follows: " + line[4]);
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :Your introduction is too long.  The max length is " + char_limit.ToString() + " characters.");
                }
            }
        }

        private static void delete_intro(string nick, string channel, bot ircbot)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "intro" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_list.txt";
            if (File.Exists(list_file))
            {
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                List<string> new_file = new List<string>();
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { ':' };
                    string[] intro_nick = file_line.Split(charSeparator, 3);
                    if (nick.Equals(intro_nick[0], StringComparison.InvariantCultureIgnoreCase) && channel.Equals(intro_nick[1]))
                    {
                    }
                    else
                    {
                        new_file.Add(file_line);
                    }
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
            }
            ircbot.sendData("PRIVMSG", channel + " :Your introduction has been removed.");
        }
    }
}
