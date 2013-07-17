using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace IRCBot.Modules
{
    class response : Module
    {
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if (type.Equals("channel") || type.Equals("query") && bot_command == true)
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
                            blocked = ircbot.get_spam_status(channel, nick);
                        }
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(command))
                            {
                                cmd_found = true;
                                break;
                            }
                        }
                        if (blocked == true && cmd_found == true)
                        {
                            ircbot.sendData("NOTICE", nick + " :I am currently too busy to process that.");
                        }
                        if (blocked == false && cmd_found == true)
                        {
                            foreach (string trigger in triggers)
                            {
                                switch (trigger)
                                {
                                    case "addresponse":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt";
                                                if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "") == false)
                                                {
                                                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response");
                                                }
                                                if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt"))
                                                {
                                                    StreamWriter log = File.AppendText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt");
                                                    log.WriteLine(line[4]);
                                                    log.Close();
                                                }
                                                else
                                                {
                                                    StreamWriter log_file = File.CreateText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt");
                                                    log_file.WriteLine(line[4]);
                                                    log_file.Close();
                                                }
                                                ircbot.sendData("PRIVMSG", channel + " :Response added successfully");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
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
            if (type.Equals("channel") && bot_command == false)
            {
                if (nick != conf.nick)
                {
                    try
                    {
                        string[] file;
                        string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt";
                        if (File.Exists(list_file))
                        {
                            file = System.IO.File.ReadAllLines(list_file);
                        }
                        else
                        {
                            file = null;
                        }

                        string tmp_line = line[3];
                        if (line.GetUpperBound(0) > 3)
                        {
                            tmp_line += " " + line[4];
                        }
                        tmp_line = tmp_line.Remove(0, 1);
                        string new_line = tmp_line.ToLowerInvariant();
                        bool triggered = false;
                        if (file.GetUpperBound(0) >= 0)
                        {
                            foreach (string tmp_new_line in file)
                            {
                                string file_line = tmp_new_line.Replace("<nick>", nick);
                                file_line = file_line.Replace("<me>", conf.nick);
                                char[] split_type = new char[] { ':' };
                                char[] trigger_split = new char[] { '*' };
                                char[] triggered_split = new char[] { '&' };
                                string[] split = file_line.Split(split_type, 2);
                                string[] triggers = split[0].Split('|');
                                string[] responses = split[1].Split('|');
                                int index = 0;
                                for (int x = 0; x <= triggers.GetUpperBound(0); x++)
                                {
                                    string[] terms = triggers[x].Split(trigger_split, StringSplitOptions.RemoveEmptyEntries);
                                    for (int y = 0; y <= terms.GetUpperBound(0); y++)
                                    {
                                        triggered = false;
                                        terms[y] = terms[y].ToLowerInvariant();
                                        if (triggers[x].StartsWith("*") == false && triggers[x].EndsWith("*") == false && terms.GetUpperBound(0) == 0)
                                        {
                                            if (new_line.Equals(terms[y]) == true)
                                            {
                                                triggered = true;
                                            }
                                            else
                                            {
                                                triggered = false;
                                                break;
                                            }
                                        }
                                        else if (triggers[x].StartsWith("*") == false && y == 0)
                                        {
                                            if (new_line.StartsWith(terms[y]) == true && index <= new_line.IndexOf(terms[y]))
                                            {
                                                triggered = true;
                                                index = new_line.IndexOf(terms[y]);
                                            }
                                            else
                                            {
                                                triggered = false;
                                                break;
                                            }
                                        }
                                        else if (triggers[x].EndsWith("*") == false && y == terms.GetUpperBound(0))
                                        {
                                            if (new_line.EndsWith(terms[y]) == true && index <= new_line.IndexOf(terms[y]))
                                            {
                                                triggered = true;
                                                index = new_line.IndexOf(terms[y]);
                                            }
                                            else
                                            {
                                                triggered = false;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (new_line.Contains(terms[y]) == true && index <= new_line.IndexOf(terms[y]))
                                            {
                                                triggered = true;
                                                index = new_line.IndexOf(terms[y]);
                                            }
                                            else
                                            {
                                                triggered = false;
                                                break;
                                            }
                                        }
                                    }
                                    if (triggered == true)
                                    {
                                        break;
                                    }
                                }
                                if (triggered == true)
                                {
                                    ircbot.add_spam_count(channel);
                                    int number_of_responses = responses.GetUpperBound(0) + 1;
                                    Random random = new Random();
                                    index = random.Next(0, number_of_responses);
                                    string[] events = responses[index].Split(triggered_split, StringSplitOptions.RemoveEmptyEntries);
                                    for (int y = 0; y <= events.GetUpperBound(0); y++)
                                    {
                                        if (events[y].StartsWith("<action>") == true)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION " + events[y].Remove(0, 8) + "\u0001");
                                        }
                                        else if (events[y].StartsWith("<delay>") == true)
                                        {
                                            Thread.Sleep(Convert.ToInt32(events[y].Remove(0, 7)));
                                        }
                                        else if (events[y].StartsWith("<part>") == true)
                                        {
                                            ircbot.sendData("PART", channel);
                                        }
                                        else if (events[y].StartsWith("<join>") == true)
                                        {
                                            ircbot.sendData("JOIN", channel);
                                        }
                                        else if (events[y].StartsWith("<kick>") == true)
                                        {
                                            if (events[y].Length > 6)
                                            {
                                                ircbot.sendData("KICK", channel + " " + nick + " :" + events[y].Remove(0, 6));
                                            }
                                            else
                                            {
                                                ircbot.sendData("KICK", channel + " " + nick + " :No Reason");
                                            }
                                        }
                                        else if (events[y].StartsWith("<ban>") == true)
                                        {
                                            string target_host = ircbot.get_user_host(nick);
                                            string ban = "*!*@" + target_host;
                                            if (target_host.Equals("was"))
                                            {
                                                ban = nick + "!*@*";
                                            }
                                            if (events[y].Length > 6)
                                            {
                                                ircbot.sendData("MODE", channel + " +b " + ban + " :" + events[y].Remove(0, 6));
                                            }
                                            else
                                            {
                                                ircbot.sendData("MODE", channel + " +b " + ban + " :No Reason");
                                            }
                                        }
                                        else if (events[y].StartsWith("<kickban>") == true)
                                        {
                                            string target_host = ircbot.get_user_host(nick);
                                            string ban = "*!*@" + target_host;
                                            if (target_host.Equals("was"))
                                            {
                                                ban = nick + "!*@*";
                                            }
                                            if (events[y].Length > 6)
                                            {
                                                ircbot.sendData("MODE", channel + " +b " + ban + " :" + events[y].Remove(0, 6));
                                                ircbot.sendData("KICK", channel + " " + nick + " :" + events[y].Remove(0, 6));
                                            }
                                            else
                                            {
                                                ircbot.sendData("MODE", channel + " +b " + ban + " :No Reason");
                                                ircbot.sendData("KICK", channel + " " + nick + " :No Reason");
                                            }
                                        }
                                        else if (events[y].StartsWith("<timeban>") == true)
                                        {
                                            string[] mod_line = new string[] { conf.nick, "0", channel, ":tb", events[y].Remove(0, 9) };
                                            Modules.moderation mod = new Modules.moderation();
                                            mod.control(ircbot, ref conf, module_id, mod_line, "tb", conf.owner_level, nick, channel, true, "channel");
                                        }
                                        else if (events[y].StartsWith("<timekickban>") == true)
                                        {
                                            string[] mod_line = new string[] { conf.nick, "0", channel, ":tkb", events[y].Remove(0, 13) };
                                            Modules.moderation mod = new Modules.moderation();
                                            mod.control(ircbot, ref conf, module_id, mod_line, "tkb", conf.owner_level, nick, channel, true, "channel");
                                        }
                                        else
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :" + events[y]);
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + ex.ToString());
                    }
                }
            }
        }
    }
}
