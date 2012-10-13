using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot
{
    class intro
    {
        public void intro_control(string[] line, string command, Interface ircbot, int nick_access, string nick)
        {
            switch (command)
            {
                case "intro":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
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
                    break;
                case "introdelete":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        // Delete Introduction
                        delete_intro(nick, line[2], ircbot);
                    }
                    break;
            }
        }

        public void check_intro(string nick, string channel, Interface ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\intro\\list.txt";
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

        private void add_intro(string nick, string channel, string[] line, Interface ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\intro\\list.txt";
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

        private void delete_intro(string nick, string channel, Interface ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\intro\\list.txt";
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
