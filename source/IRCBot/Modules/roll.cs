using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot
{
    class roll
    {
        public void roll_control(string[] line, string command, bot ircbot, IRCConfig conf, int conf_id, int nick_access, string nick, string channel)
        {
            switch (command)
            {
                case "roll":
                    ircbot.spam_count++;
                    if (nick_access >= ircbot.get_command_access(command))
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
                                if (num_dice > Convert.ToInt32(conf.module_config[conf_id][2]))
                                {
                                    num_dice = Convert.ToInt32(conf.module_config[conf_id][2]);
                                }
                                if (num_sides > Convert.ToInt32(conf.module_config[conf_id][3]))
                                {
                                    num_sides = Convert.ToInt32(conf.module_config[conf_id][3]);
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
                                if (num_dice > Convert.ToInt32(conf.module_config[conf_id][2]))
                                {
                                    num_dice = Convert.ToInt32(conf.module_config[conf_id][2]);
                                }
                                for (int x = 0; x < num_dice; x++)
                                {
                                    System.Threading.Thread.Sleep(100);
                                    Random random = new Random();
                                    int ran_num = random.Next(1, 7);
                                    bool num_found = false;
                                    foreach (List<int> num in roll_results)
                                    {
                                        if(num[0] == ran_num)
                                        {
                                            num[1]++;
                                            num_found = true;
                                            break;
                                        }
                                    }
                                    if(num_found == false)
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
