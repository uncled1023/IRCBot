using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace IRCBot
{
    class AI
    {
        public void AI_Parse(string[] total_line, string channel, string nick, bot ircbot, IRCConfig conf, chat chat)
        {
            if (nick != conf.nick && chat.still_chatting == false)
            {
                try
                {
                    string[] file;
                    string list_file = ircbot.cur_dir + "\\modules\\AI\\dictionary.txt";
                    if (File.Exists(list_file))
                    {
                        file = System.IO.File.ReadAllLines(list_file);
                    }
                    else
                    {
                        file = null;
                    }

                    string line = total_line[3];
                    if (total_line.GetUpperBound(0) > 3)
                    {
                        line += " " + total_line[4];
                    }
                    line = line.Remove(0, 1);
                    string new_line = line.ToLowerInvariant();
                    bool triggered = false;
                    if (file.GetUpperBound(0) >= 0)
                    {
                        foreach (string tmp_line in file)
                        {
                            string file_line = tmp_line.Replace("<nick>", nick);
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
                                ircbot.spam_count++;
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
                                        string[] mod_tmp_line = new string[] { conf.nick, "0", channel, ":tb", events[y].Remove(0, 9) };
                                        moderation mod = new moderation();
                                        mod.moderation_control(mod_tmp_line, "tb", ircbot, conf, 10, conf.nick);
                                    }
                                    else if (events[y].StartsWith("<timekickban>") == true)
                                    {
                                        string[] mod_tmp_line = new string[] { conf.nick, "0", channel, ":tkb", events[y].Remove(0, 13) };
                                        moderation mod = new moderation();
                                        mod.moderation_control(mod_tmp_line, "tkb", ircbot, conf, 10, conf.nick);
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
