using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot
{
    class _8ball
    {
        public void _8ball_control(string[] line, string command, bot ircbot, int nick_access, string nick)
        {
            switch (command)
            {
                case "8ball":
                    ircbot.spam_count++;
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            get_answer(line[2], ircbot);
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
            }
        }

        private void get_answer(string channel, bot ircbot)
        {
            if (File.Exists(ircbot.cur_dir + "\\modules\\8ball\\answers.txt"))
            {
                string[] answer_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\8ball\\answers.txt");
                int number_of_lines = answer_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    while (line == "")
                    {
                        Random random = new Random();
                        int index = random.Next(0, number_of_lines);
                        line = answer_file[index];
                    }
                    ircbot.sendData("PRIVMSG", channel + " :" + line);
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :I don't know!");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :I don't know!");
            }
        }
    }
}
