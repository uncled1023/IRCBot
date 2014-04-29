using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Bot.Modules
{
    class fortune : Module
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
                                case "fortune":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        get_quote(line[2], ircbot);
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

        private static void get_quote(string channel, bot ircbot)
        {
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "fortune" + Path.DirectorySeparatorChar + "list.txt"))
            {
                string[] answer_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "fortune" + Path.DirectorySeparatorChar + "list.txt");
                int number_of_lines = answer_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    Random random = new Random();
                    int index = random.Next(0, number_of_lines);
                    line = answer_file[index];
                    if (!String.IsNullOrEmpty(line))
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + line);
                    }
                    else
                    {
                        ircbot.sendData("PRIVMSG", channel + " :No fortune for you!");
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :No fortune for you!");
                }
            }
            else
            {
                if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "fortune"))
                {
                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "fortune");
                }
                List<string> contents = new List<string>();
                contents.Add("You will find fortune soon.");
                File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "fortune" + Path.DirectorySeparatorChar + "list.txt", contents.ToArray());
                ircbot.sendData("PRIVMSG", channel + " :No fortune for you!");
            }
        }
    }
}
