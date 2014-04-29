using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Bot.Modules
{
    class _8ball : Module
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
                                case "8ball":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
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
                    }
                }
            }
        }

        private void get_answer(string channel, bot ircbot)
        {
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "8ball" + Path.DirectorySeparatorChar + "answers.txt"))
            {
                string[] answer_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "8ball" + Path.DirectorySeparatorChar + "answers.txt");
                int number_of_lines = answer_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    Random random = new Random();
                    int index = random.Next(0, number_of_lines);
                    line = answer_file[index];
                    if (!line.Equals(string.Empty))
                    {
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
            else
            {
                if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "8ball"))
                {
                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "8ball");
                }
                List<string> contents = new List<string>();
                contents.Add("Yes");
                contents.Add("No");
                contents.Add("Maybe");
                File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "8ball" + Path.DirectorySeparatorChar + "answers.txt", contents.ToArray());
                ircbot.sendData("PRIVMSG", channel + " :I don't know!");
            }
        }
    }
}
