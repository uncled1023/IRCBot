using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Bot.Modules
{
    class rules : Module
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
                                case "rules":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        get_rules(nick, line[2], ircbot);
                                    }
                                    else
                                    {
                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                    }
                                    break;
                                case "addrule":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            add_rule(line[4], nick, line[2], ircbot);
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
                                case "delrule":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            del_rule(line[4], nick, line[2], ircbot);
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

        private void add_rule(string rule, string nick, string channel, bot ircbot)
        {
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            string tab_name = Regex.Replace(channel, pattern, "_");
            if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules"))
            {
                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules");
            }
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt"))
            {
                List<string> rules_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt").ToList();
                rules_file.Add(rule);
                System.IO.File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt", rules_file);
            }
            else
            {
                List<string> rules_file = new List<string>();
                rules_file.Add(rule);
                System.IO.File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt", rules_file);
            }
            ircbot.sendData("PRIVMSG", channel + " :Rule added successfully");
        }

        private void del_rule(string rule_num, string nick, string channel, bot ircbot)
        {
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            string tab_name = Regex.Replace(channel, pattern, "_");
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt"))
            {
                List<string> rules_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt").ToList();
                int number_of_lines = rules_file.Count + 1;
                if (number_of_lines > 0)
                {
                    int index = 0;
                    bool rule_found = false;
                    foreach (string line in rules_file)
                    {
                        string[] split = line.Split('*');
                        if ((index + 1) == Convert.ToInt32(rule_num))
                        {
                            rules_file.RemoveAt(index);
                            rule_found = true;
                            break;
                        }
                        index++;
                    }
                    if (rule_found == false)
                    {
                        ircbot.sendData("NOTICE", nick + " :No Rules to Delete!");
                    }
                    else
                    {
                        System.IO.File.WriteAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt", rules_file);
                    }
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :No Rules to Delete!");
                }
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :There are no Rules");
            }
        }

        private void get_rules(string nick, string channel, bot ircbot)
        {
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            string tab_name = Regex.Replace(channel, pattern, "_");
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt"))
            {
                string[] answer_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "rules" + Path.DirectorySeparatorChar + ircbot.Conf.Server_Name + "_" + tab_name + "_rules.txt");
                int number_of_lines = answer_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    int index = 1;
                    foreach (string line in answer_file)
                    {
                        ircbot.sendData("NOTICE", nick + " :Rule " + index + ") " + line);
                        index++;
                    }
                    if (index == 1)
                    {
                        ircbot.sendData("NOTICE", nick + " :There are no Rules");
                    }
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :There are no Rules");
                }
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :There are no Rules");
            }
        }
    }
}
