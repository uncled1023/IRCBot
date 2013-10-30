using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Bot.Modules
{
    class response : Module
    {
        public override void control(bot ircbot, BotConfig Conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.Conf.Module_Config[module_id][0];
            if (type.Equals("channel") || type.Equals("query") && bot_command == true)
            {
                foreach (List<string> tmp_command in Conf.Command_List)
                {
                    if (module_name.Equals(tmp_command[0]))
                    {
                        string[] triggers = tmp_command[3].Split('|');
                        int command_access = Convert.ToInt32(tmp_command[5]);
                        string[] blacklist = tmp_command[6].Split(',');
                        bool blocked = false;
                        bool cmd_found = false;
                        bool spam_check = ircbot.get_spam_check(channel, nick, Convert.ToBoolean(tmp_command[8]));
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
                            blocked = ircbot.get_spam_status(channel);
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
                                    case "delresponse":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                try
                                                {
                                                    bool response_found = false;
                                                    int del_num = Convert.ToInt32(line[4]);
                                                    string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt";
                                                    if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "") == false)
                                                    {
                                                        Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response");
                                                    }
                                                    if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt"))
                                                    {
                                                        string[] file = System.IO.File.ReadAllLines(list_file);

                                                        if (file.GetUpperBound(0) >= 0)
                                                        {
                                                            List<string> new_file = new List<string>();
                                                            int index = 1;
                                                            foreach (string tmp_new_line in file)
                                                            {
                                                                if (index == Convert.ToInt32(line[4]))
                                                                {
                                                                    ircbot.sendData("NOTICE", nick + " :Response removed successfully.");
                                                                    response_found = true;
                                                                }
                                                                else
                                                                {
                                                                    new_file.Add(tmp_new_line);
                                                                }
                                                                index++;
                                                            }
                                                            System.IO.File.WriteAllLines(@list_file, new_file);
                                                        }
                                                        if(!response_found)
                                                        {
                                                            ircbot.sendData("NOTICE", nick + " :Unable to delete desired response.");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        StreamWriter log_file = File.CreateText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt");
                                                        log_file.Close();
                                                    }
                                                }
                                                catch
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :Please specify a valid number.");
                                                }
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
                                    case "listresponse":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt";
                                            if (Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "") == false)
                                            {
                                                Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response");
                                            }
                                            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt"))
                                            {
                                                string[] file = System.IO.File.ReadAllLines(list_file);

                                                if (file.GetUpperBound(0) >= 0)
                                                {
                                                    int index = 1;
                                                    foreach (string tmp_new_line in file)
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :[" + index + "] " + tmp_new_line);
                                                        Thread.Sleep(100);
                                                        index++;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                StreamWriter log_file = File.CreateText(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt");
                                                log_file.Close();
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
            if (type.Equals("channel") && !bot_command)
            {
                if (!nick.Equals(ircbot.Nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] file;
                    string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response" + Path.DirectorySeparatorChar + "dictionary.txt";
                    if (File.Exists(list_file))
                    {
                        file = System.IO.File.ReadAllLines(list_file);

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
                                char[] split_type = new char[] { ':' };
                                char[] trigger_split = new char[] { '*' };
                                char[] triggered_split = new char[] { '&' };
                                string[] split = tmp_new_line.Split(split_type, 3);
                                string[] channels = split[0].Split(',');
                                string[] triggers = split[1].Split('|');
                                string[] responses = split[2].Split('|');
                                bool response_allowed = false;
                                foreach (string chan in channels)
                                {
                                    if (chan.Equals(channel, StringComparison.InvariantCultureIgnoreCase) || chan.Equals(nick, StringComparison.InvariantCultureIgnoreCase) || chan.Equals("<all>"))
                                    {
                                        response_allowed = true;
                                    }
                                    if(chan.Equals("!" + nick, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        response_allowed = false;
                                        break;
                                    }
                                    if (chan.Equals("!" + channel, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        response_allowed = false;
                                        break;
                                    }
                                }
                                if (response_allowed)
                                {
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
                                        string file_line = responses[index].Replace("<nick>", nick);
                                        file_line = file_line.Replace("<me>", Conf.Nick);
                                        file_line = file_line.Replace("<chan>", channel);
                                        string[] events = file_line.Split(triggered_split, StringSplitOptions.RemoveEmptyEntries);
                                        for (int y = 0; y <= events.GetUpperBound(0); y++)
                                        {
                                            if (events[y].StartsWith("<cmd>") == true)
                                            {
                                                char[] charSplit = new char[] { ' ' };
                                                string[] ex = events[y].Remove(0, 5).Split(charSplit, 2);
                                                string[] args;
                                                if (ex.GetUpperBound(0) > 0)
                                                {
                                                    args = ex[1].Split(charSplit);
                                                }
                                                else
                                                {
                                                    args = null;
                                                }
                                                ircbot.controller.run_command(Conf.Server_Name, channel, ex[0], args);
                                            }
                                            else if (events[y].StartsWith("<delay>") == true)
                                            {
                                                Thread.Sleep(Convert.ToInt32(events[y].Remove(0, 7)));
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
                    }
                    else
                    {
                        if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response"))
                        {
                            Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "Response");
                        }
                        File.Create(list_file);
                    }
                }
            }
        }
    }
}
