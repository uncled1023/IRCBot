using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot
{
    class rules
    {
        public void rules_control(string[] line, string command, bot ircbot, int nick_access, string nick)
        {
            switch (command)
            {
                case "rules":
                    ircbot.spam_count++;
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        get_rules(nick, line[2], ircbot);
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "addrule":
                    if (nick_access >= ircbot.get_command_access(command))
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
                    if (nick_access >= ircbot.get_command_access(command))
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

        private void add_rule(string rule, string nick, string channel, bot ircbot)
        {
            if (File.Exists(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt"))
            {
                List<string> rules_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt").ToList();
                rules_file.Add(channel + "*" + rule);
                System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt", rules_file);
            }
            else
            {
                List<string> rules_file = new List<string>();
                rules_file.Add(channel + "*" + rule);
                System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt", rules_file);
            }
        }

        private void del_rule(string rule_num, string nick, string channel, bot ircbot)
        {
            if (File.Exists(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt"))
            {
                List<string> rules_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt").ToList();
                int number_of_lines = rules_file.Count + 1;
                if (number_of_lines > 0)
                {
                    int index = 0;
                    bool rule_found = false;
                    foreach (string line in rules_file)
                    {
                        string[] split = line.Split('*');
                        if (split.GetUpperBound(0) > 0 && channel.Equals(split[0]) && (index + 1) == Convert.ToInt32(rule_num))
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
                        System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt", rules_file);
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
            if (File.Exists(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt"))
            {
                string[] answer_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_rules.txt");
                int number_of_lines = answer_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    int index = 1;
                    foreach (string line in answer_file)
                    {
                        string[] split = line.Split('*');
                        if (split.GetUpperBound(0) > 0 && channel.Equals(split[0]))
                        {
                            ircbot.sendData("NOTICE", nick + " :Rule " + index + ") " + split[1]);
                            index++;
                        }
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
