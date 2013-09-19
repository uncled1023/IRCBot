using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class roll : Module
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
                                    case "roll":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Trim().Split(' ');
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    int num_dice = 0;
                                                    int num_sides = 0;
                                                    List<List<int>> roll_results = new List<List<int>>();
                                                    try
                                                    {
                                                        num_dice = Convert.ToInt32(new_line[0].Trim());
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        if (ex != null)
                                                        {
                                                            num_dice = 1;
                                                        }
                                                    }
                                                    try
                                                    {
                                                        num_sides = Convert.ToInt32(new_line[1].Trim());
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        if (ex != null)
                                                        {
                                                            num_sides = 6;
                                                        }
                                                    }
                                                    if (num_dice > Convert.ToInt32(conf.module_config[module_id][3]))
                                                    {
                                                        num_dice = Convert.ToInt32(conf.module_config[module_id][3]);
                                                    }
                                                    if (num_sides > Convert.ToInt32(conf.module_config[module_id][4]))
                                                    {
                                                        num_sides = Convert.ToInt32(conf.module_config[module_id][4]);
                                                    }
                                                    for (int x = 0; x < num_dice; x++)
                                                    {
                                                        System.Threading.Thread.Sleep(100);
                                                        Random random = new Random();
                                                        int ran_num = random.Next(1, num_sides + 1);
                                                        bool num_found = false;
                                                        foreach (List<int> num in roll_results)
                                                        {
                                                            if (num[0] == ran_num)
                                                            {
                                                                num[1]++;
                                                                num_found = true;
                                                                break;
                                                            }
                                                        }
                                                        if (num_found == false)
                                                        {
                                                            List<int> tmp = new List<int>();
                                                            tmp.Add(ran_num);
                                                            tmp.Add(1);
                                                            roll_results.Add(tmp);
                                                        }
                                                    }
                                                    string msg = "";
                                                    foreach (List<int> num in roll_results)
                                                    {
                                                        msg += num[1] + " [" + num[0] + "'s] | ";
                                                    }
                                                    ircbot.sendData("PRIVMSG", channel + " :Rolling " + num_dice + " " + num_sides + "-sided dice: " + msg.Trim().TrimEnd('|').Trim());
                                                }
                                                else
                                                {
                                                    int num_dice = 0;
                                                    List<List<int>> roll_results = new List<List<int>>();
                                                    try
                                                    {
                                                        num_dice = Convert.ToInt32(new_line[0]);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        if (ex != null)
                                                        {
                                                            num_dice = 1;
                                                        }
                                                    }
                                                    if (num_dice > Convert.ToInt32(conf.module_config[module_id][3]))
                                                    {
                                                        num_dice = Convert.ToInt32(conf.module_config[module_id][3]);
                                                    }
                                                    for (int x = 0; x < num_dice; x++)
                                                    {
                                                        System.Threading.Thread.Sleep(100);
                                                        Random random = new Random();
                                                        int ran_num = random.Next(1, 7);
                                                        bool num_found = false;
                                                        foreach (List<int> num in roll_results)
                                                        {
                                                            if (num[0] == ran_num)
                                                            {
                                                                num[1]++;
                                                                num_found = true;
                                                                break;
                                                            }
                                                        }
                                                        if (num_found == false)
                                                        {
                                                            List<int> tmp = new List<int>();
                                                            tmp.Add(ran_num);
                                                            tmp.Add(1);
                                                            roll_results.Add(tmp);
                                                        }
                                                    }
                                                    string msg = "";
                                                    foreach (List<int> num in roll_results)
                                                    {
                                                        msg += num[1] + " [" + num[0] + "'s] | ";
                                                    }
                                                    ircbot.sendData("PRIVMSG", channel + " :Rolling " + num_dice + " 6-sided dice: " + msg.Trim().TrimEnd('|').Trim());
                                                }
                                            }
                                            else
                                            {
                                                Random random = new Random();
                                                int ran_num = random.Next(1, 7);
                                                ircbot.sendData("PRIVMSG", channel + " :Rolling 1 6-sided dice: " + ran_num.ToString());
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
    }
}
