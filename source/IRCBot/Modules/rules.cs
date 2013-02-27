using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IRCBot.Modules
{
    class rules : Module
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
                                    case "rules":
                                        if (spam_check == true)
                                        {
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
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
        }

        private void add_rule(string rule, string nick, string channel, bot ircbot)
        {
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            string tab_name = Regex.Replace(channel, pattern, "_");
            if (File.Exists(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt"))
            {
                List<string> rules_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt").ToList();
                rules_file.Add(channel + "*" + rule);
                System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt", rules_file);
            }
            else
            {
                List<string> rules_file = new List<string>();
                rules_file.Add(channel + "*" + rule);
                System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt", rules_file);
            }
            ircbot.sendData("PRIVMSG", channel + " :Rule added successfully");
        }

        private void del_rule(string rule_num, string nick, string channel, bot ircbot)
        {
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            string tab_name = Regex.Replace(channel, pattern, "_");
            if (File.Exists(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt"))
            {
                List<string> rules_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt").ToList();
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
                        System.IO.File.WriteAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt", rules_file);
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
            if (File.Exists(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt"))
            {
                string[] answer_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\rules\\" + ircbot.server_name + "_" + tab_name + "_rules.txt");
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
