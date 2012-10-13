using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot
{
    class messaging
    {
        public void message_control(string[] line, string command, Interface ircbot, int nick_access, string nick)
        {
            switch (command)
            {
                case "message":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            add_message(nick, line, line[2], ircbot);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    break;
            }
        }
        private void add_message(string nick, string[] line, string channel, Interface ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\messaging\\messages.txt";
            char[] charS = new char[] { ' ' };
            string[] tmp = line[4].Split(charS, 2);
            string to_nick = tmp[0];
            string add_line = nick + "*" + to_nick + "*" + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt") + "*";
            bool found_nick = false;
            if (tmp.GetUpperBound(0) >= 1)
            {
                add_line += tmp[1];
                if (File.Exists(list_file))
                {
                    int counter = 0;
                    string[] old_file = System.IO.File.ReadAllLines(list_file);
                    string[] new_file = new string[old_file.GetUpperBound(0) + 2];
                    foreach (string file_line in old_file)
                    {
                        char[] charSeparator = new char[] { '*' };
                        string[] intro_nick = file_line.Split(charSeparator, 4);
                        if (nick.Equals(intro_nick[0]))
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
                if (channel != null)
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + nick + ", I will send your message as soon as I can.");
                }
                else
                {
                    ircbot.sendData("PRIVMSG", nick + " :I will send your message as soon as I can.");
                }
            }
        }

        public void find_message(string nick, Interface ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\messaging\\messages.txt";
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 1];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] intro_nick = file_line.Split(charSeparator, 4);
                    if (intro_nick.GetUpperBound(0) > 0)
                    {
                        if (nick.Equals(intro_nick[1]))
                        {
                            ircbot.sendData("PRIVMSG", nick + " :" + intro_nick[0] + " has left you a message on: " + intro_nick[2]);
                            ircbot.sendData("PRIVMSG", nick + " :\"" + intro_nick[3] + "\"");
                            ircbot.sendData("PRIVMSG", nick + " :If you would like to reply to them, please type .message " + intro_nick[0] + " <your_message>");
                        }
                        else
                        {
                            new_file[counter] = file_line;
                            counter++;
                        }
                    }
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
                // Read the file and display it line by line.
            }
        }
    }
}
