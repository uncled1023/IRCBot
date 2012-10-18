using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;

namespace IRCBot
{
    class hbomb
    {
        private System.Timers.Timer bomb_trigger = new System.Timers.Timer();
        private bool hbomb_active = false;
        private string bomb_holder = "";
        private string previous_bomb_holder = "";
        private string bomb_channel = "";
        private string wire_color = "";
        private string[] wire_colors = new string[] { "Black", "Blue", "Yellow", "Red", "Green", "White" };
        private Interface main;

        public hbomb()
        {
            bomb_trigger.Elapsed += activate_bomb;
        }

        public void hbomb_control(string[] line, string command, Interface ircbot, int nick_access, string nick, string channel, IRCConfig conf)
        {
            switch (command)
            {
                case "hbomb":
                    if (nick_access >= 1)
                    {
                        if (hbomb_active == false)
                        {
                            ircbot.spam_count++;
                            hbomb_active = true;
                            bomb_channel = channel;

                            Random random_color = new Random();
                            int color_index = random_color.Next(0, wire_colors.GetUpperBound(0) + 1);
                            wire_color = wire_colors[color_index];

                            Random random = new Random();
                            int index = random.Next(10, 60);

                            bomb_trigger.Interval = (index * 1000);
                            bomb_trigger.Enabled = true;
                            bomb_trigger.AutoReset = false;

                            main = ircbot;

                            previous_bomb_holder = nick;
                            bomb_holder = nick;

                            ircbot.sendData("PRIVMSG", channel + " :" + nick + " has started the timer!  If the bomb get's passed to you, type .pass <nick> to pass it to someone else, or type .defuse <color> to try to defuse it.");
                            string colors = "";
                            foreach (string wire in wire_colors)
                            {
                                colors += wire + ",";
                            }
                            ircbot.sendData("NOTICE", nick + " :You need to hurry and pass the bomb before it blows up!  Or you can try to defuse it yourself.  The colors are: " + colors.TrimEnd(','));
                        }
                        else
                        {
                            if (bomb_channel.Equals(channel))
                            {
                                ircbot.sendData("PRIVMSG", channel + " :There is already a bomb counting down.");
                            }
                            else
                            {
                                ircbot.sendData("PRIVMSG", line[2] + " :There is already a bomb counting down in " + bomb_channel + ".");
                            }
                        }
                    }
                    break;
                case "pass":
                    if (nick_access >= 1)
                    {
                        if (hbomb_active == true)
                        {
                            if (bomb_holder.Equals(nick))
                            {
                                ircbot.spam_count++;
                                if (line.GetUpperBound(0) > 3)
                                {
                                    if (line[4].TrimEnd(' ').Equals(conf.nick))
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you can't pass it to me!");
                                    }
                                    else
                                    {
                                        int user_access = ircbot.get_user_access(line[4].TrimEnd(' '), channel);
                                        if (user_access > 0)
                                        {
                                            pass_hbomb(line[4].TrimEnd(' '), channel, nick, ircbot);
                                        }
                                        else
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you can't pass to them!");
                                        }
                                    }
                                }
                                else
                                {
                                    ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you need to pass the bomb to someone.");
                                }
                            }
                            else
                            {
                                ircbot.sendData("PRIVMSG", channel + " :You don't have the bomb!");
                            }
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to pass!");
                        }
                    }
                    break;
                case "defuse":
                    if (nick_access >= 1)
                    {
                        if (hbomb_active == true)
                        {
                            if (bomb_holder.Equals(nick))
                            {
                                ircbot.spam_count++;
                                if (line.GetUpperBound(0) > 3)
                                {
                                    if (line[4].ToLower().Equals(wire_color.ToLower()))
                                    {
                                        bomb_trigger.Enabled = false;
                                        hbomb_active = false;
                                        ircbot.sendData("PRIVMSG", channel + " :You have successfully defused the bomb!");
                                        if (previous_bomb_holder.Equals(bomb_holder))
                                        {
                                        }
                                        else
                                        {
                                            ircbot.sendData("KICK", bomb_channel + " " + previous_bomb_holder + " :BOOM!!!");
                                        }
                                    }
                                    else
                                    {
                                        main = ircbot;
                                        activate_bomb(this, new EventArgs());
                                    }
                                }
                                else
                                {
                                    ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you need to cut a wire.");
                                }
                            }
                            else
                            {
                                ircbot.sendData("PRIVMSG", channel + " :You don't have the bomb!");
                            }
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to pass!");
                        }
                    }
                    break;
            }
        }

        public void activate_bomb(object sender, EventArgs e)
        {
            bomb_trigger.Enabled = false;
            hbomb_active = false;
            main.sendData("PRIVMSG", bomb_channel + " :4,1/8,1!4,1\\ 1,8 WARNING! Nuclear Strike Inbound  4,1/8,1!4,1\\");
            Thread.Sleep(1000);
            if(bomb_holder != "")
            {
                main.sendData("KICK", bomb_channel + " " + bomb_holder + " :BOOM!!!");
            }
            else
            {
                main.sendData("KICK", bomb_channel + " " + previous_bomb_holder + " :BOOM!!!");
            }
        }

        private void pass_hbomb(string pass_nick, string channel, string nick, Interface ircbot)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = "#" + tab_name + ".log";
            bool nick_idle = true;

            if (File.Exists(ircbot.cur_dir + "\\modules\\seen\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\seen\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string file_line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = file_line.Split(sep, 4);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                DateTime current_date = DateTime.Now;
                                DateTime past_date = DateTime.Parse(new_line[2]);
                                double difference_second = 0;
                                difference_second = current_date.Subtract(past_date).TotalSeconds;
                                if (difference_second <= 500)
                                {
                                    nick_idle = false;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            if (nick_idle == false)
            {
                bomb_holder = pass_nick;
                previous_bomb_holder = nick;
                ircbot.sendData("PRIVMSG", channel + " :" + nick + " passed the bomb to " + pass_nick);
                ircbot.sendData("NOTICE", pass_nick + " :You now have the bomb!  Type .pass <nick> to pass it to someone else, or type .defuse <color> to try to defuse it.");
                string colors = "";
                foreach (string wire in wire_colors)
                {
                    colors += wire + ",";
                }
                ircbot.sendData("NOTICE", pass_nick + " :The colors of the wires are: " + colors);
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :Dang, you missed them! (Idle)");
            }
        }
    }
}
