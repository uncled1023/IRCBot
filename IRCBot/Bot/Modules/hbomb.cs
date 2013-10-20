using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;

struct hbomb_info
{
    public System.Timers.Timer bomb_trigger;
    public bool bomb_locked;
    public string bomb_holder;
    public string previous_bomb_holder;
    public string bomb_channel;
    public string wire_color;
    public string[] wire_colors;
}

namespace Bot.Modules
{
    class hbomb : Module
    {
        private bot main;
        public List<hbomb_info> hbombs = new List<hbomb_info>();

        public override void control(bot ircbot, BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
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
                                    case "hbomb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int module_index = 0;
                                            foreach (Modules.Module module in ircbot.module_list)
                                            {
                                                string module_type = module.GetType().ToString();
                                                if (module_type.Equals("IRCBot.Modules.idle"))
                                                {
                                                    break;
                                                }
                                                else
                                                {
                                                    module_index++;
                                                }
                                            }
                                            Modules.idle idle;
                                            if (module_index < ircbot.module_list.Count())
                                            {
                                                idle = (Modules.idle)ircbot.module_list[module_index];
                                            }
                                            else
                                            {
                                                idle = new Modules.idle();
                                            }
                                            if (idle.check_idle(nick) == false)
                                            {
                                                bool hbomb_active = false;
                                                hbomb_info tmp_info = new hbomb_info();
                                                foreach (hbomb_info bomb in hbombs)
                                                {
                                                    if (bomb.bomb_channel.Equals(channel))
                                                    {
                                                        tmp_info = bomb;
                                                        hbomb_active = true;
                                                        break;
                                                    }
                                                }
                                                if (hbomb_active == false)
                                                {
                                                    tmp_info.bomb_locked = false;
                                                    tmp_info.bomb_trigger = new System.Timers.Timer();
                                                    tmp_info.wire_colors = ircbot.conf.module_config[module_id][3].Split(',');
                                                    tmp_info.bomb_channel = channel;

                                                    Random random_color = new Random();
                                                    int color_index = random_color.Next(0, tmp_info.wire_colors.GetUpperBound(0) + 1);
                                                    tmp_info.wire_color = tmp_info.wire_colors[color_index];

                                                    Random random = new Random();
                                                    int index = random.Next(10, 60);

                                                    tmp_info.bomb_trigger.Elapsed += (System, EventArgs) => activate_bomb(channel);
                                                    tmp_info.bomb_trigger.Interval = (index * 1000);
                                                    tmp_info.bomb_trigger.Enabled = true;
                                                    tmp_info.bomb_trigger.AutoReset = false;

                                                    main = ircbot;

                                                    tmp_info.previous_bomb_holder = nick;
                                                    tmp_info.bomb_holder = nick;

                                                    ircbot.sendData("PRIVMSG", channel + " :" + nick + " has started the timer!  If the bomb gets passed to you, type " + ircbot.conf.command + "pass <nick> to pass it to someone else, or type " + ircbot.conf.command + "defuse <color> to try to defuse it.");
                                                    string colors = "";
                                                    foreach (string wire in tmp_info.wire_colors)
                                                    {
                                                        colors += wire + ",";
                                                    }
                                                    ircbot.sendData("NOTICE", nick + " :You need to hurry and pass the bomb before it blows up!  Or you can try to defuse it yourself.  The colors are: " + colors.TrimEnd(','));
                                                    hbombs.Add(tmp_info);
                                                }
                                                else
                                                {
                                                    if (tmp_info.bomb_channel.Equals(channel))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :There is already a bomb counting down.");
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :There is already a bomb counting down in " + tmp_info.bomb_channel + ".");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :You can not start a HBomb when you are idle.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "pass":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            bool hbomb_active = false;
                                            hbomb_info tmp_info = new hbomb_info();
                                            foreach (hbomb_info bomb in hbombs)
                                            {
                                                if (bomb.bomb_channel.Equals(channel))
                                                {
                                                    tmp_info = bomb;
                                                    hbomb_active = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if (hbomb_active == true)
                                            {
                                                if (!tmp_info.bomb_locked)
                                                {
                                                    int module_index = 0;
                                                    foreach (Modules.Module module in ircbot.module_list)
                                                    {
                                                        string module_type = module.GetType().ToString();
                                                        if (module_type.Equals("IRCBot.Modules.idle"))
                                                        {
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            module_index++;
                                                        }
                                                    }
                                                    Modules.idle idle;
                                                    if (module_index < ircbot.module_list.Count())
                                                    {
                                                        idle = (Modules.idle)ircbot.module_list[module_index];
                                                    }
                                                    else
                                                    {
                                                        idle = new Modules.idle();
                                                    }
                                                    if (idle.check_idle(nick) == false)
                                                    {
                                                        if (tmp_info.bomb_holder.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            if (line.GetUpperBound(0) > 3)
                                                            {
                                                                if (line[4].Trim().Equals(ircbot.nick, StringComparison.InvariantCultureIgnoreCase))
                                                                {
                                                                    ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you can't pass it to me!");
                                                                }
                                                                else
                                                                {
                                                                    int user_access = ircbot.get_user_access(line[4].Trim(), channel);
                                                                    if (user_access > 0 && idle.check_idle(line[4].Trim()) == false)
                                                                    {
                                                                        pass_hbomb(line[4].Trim(), channel, nick, ircbot, conf, ref tmp_info, index);
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
                                                        ircbot.sendData("PRIVMSG", channel + " :You can not pass the HBomb when you are idle.");
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :You can not pass a locked bomb.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to pass!");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "set_bomb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            bool hbomb_active = false;
                                            hbomb_info tmp_info = new hbomb_info();
                                            foreach (hbomb_info bomb in hbombs)
                                            {
                                                if (bomb.bomb_channel.Equals(channel))
                                                {
                                                    tmp_info = bomb;
                                                    hbomb_active = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if (hbomb_active == true)
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    if (line[4].Trim().Equals(ircbot.nick, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you can't pass it to me!");
                                                    }
                                                    else
                                                    {
                                                        int user_access = ircbot.get_user_access(line[4].Trim(), channel);
                                                        if (user_access > 0)
                                                        {
                                                            pass_hbomb(line[4].Trim(), channel, nick, ircbot, conf, ref tmp_info, index);
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", channel + " :" + nick + ", that user isn't online!");
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
                                                ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to pass!");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "lock_bomb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            bool hbomb_active = false;
                                            hbomb_info tmp_info = new hbomb_info();
                                            foreach (hbomb_info bomb in hbombs)
                                            {
                                                if (bomb.bomb_channel.Equals(channel))
                                                {
                                                    tmp_info = bomb;
                                                    hbomb_active = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if (hbomb_active == true)
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    if (line[4].Trim().Equals(ircbot.nick, StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you can't pass it to me!");
                                                    }
                                                    else
                                                    {
                                                        int user_access = ircbot.get_user_access(line[4].Trim(), channel);
                                                        if (user_access > 0)
                                                        {
                                                            pass_hbomb(line[4].Trim(), channel, nick, ircbot, conf, ref tmp_info, index);
                                                            tmp_info.bomb_locked = true;
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", channel + " :" + nick + ", that user isn't online!");
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    tmp_info.bomb_locked = true;
                                                }
                                                hbombs[index] = tmp_info;
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to lock!");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "unlock_bomb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            bool hbomb_active = false;
                                            hbomb_info tmp_info = new hbomb_info();
                                            foreach (hbomb_info bomb in hbombs)
                                            {
                                                if (bomb.bomb_channel.Equals(channel))
                                                {
                                                    tmp_info = bomb;
                                                    hbomb_active = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if (hbomb_active == true)
                                            {
                                                tmp_info.bomb_locked = false;
                                                hbombs[index] = tmp_info;
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to unlock!");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "detonate":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool hbomb_active = false;
                                            foreach (hbomb_info bomb in hbombs)
                                            {
                                                if (bomb.bomb_channel.Equals(channel))
                                                {
                                                    hbomb_active = true;
                                                    break;
                                                }
                                            }
                                            if (hbomb_active == true)
                                            {
                                                main = ircbot;
                                                activate_bomb(channel);
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to blow up!");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "stop_bomb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            bool hbomb_active = false;
                                            hbomb_info tmp_info = new hbomb_info();
                                            foreach (hbomb_info bomb in hbombs)
                                            {
                                                if (bomb.bomb_channel.Equals(channel))
                                                {
                                                    tmp_info = bomb;
                                                    hbomb_active = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if (hbomb_active == true)
                                            {
                                                hbombs.RemoveAt(index);
                                                ircbot.sendData("PRIVMSG", channel + " :Bomb has been defused and thrown away.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to stop!");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "defuse":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            int index = 0;
                                            bool hbomb_active = false;
                                            hbomb_info tmp_info = new hbomb_info();
                                            foreach (hbomb_info bomb in hbombs)
                                            {
                                                if (bomb.bomb_channel.Equals(channel))
                                                {
                                                    tmp_info = bomb;
                                                    hbomb_active = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if (hbomb_active == true)
                                            {
                                                if (!tmp_info.bomb_locked)
                                                {
                                                    int module_index = 0;
                                                    foreach (Modules.Module module in ircbot.module_list)
                                                    {
                                                        string module_type = module.GetType().ToString();
                                                        if (module_type.Equals("IRCBot.Modules.idle"))
                                                        {
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            module_index++;
                                                        }
                                                    }
                                                    Modules.idle idle;
                                                    if (module_index < ircbot.module_list.Count())
                                                    {
                                                        idle = (Modules.idle)ircbot.module_list[module_index];
                                                    }
                                                    else
                                                    {
                                                        idle = new Modules.idle();
                                                    }
                                                    if (idle.check_idle(nick) == false)
                                                    {
                                                        if (tmp_info.bomb_holder.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            if (line.GetUpperBound(0) > 3)
                                                            {
                                                                if (line[4].Trim().Equals(tmp_info.wire_color, StringComparison.InvariantCultureIgnoreCase))
                                                                {
                                                                    ircbot.sendData("PRIVMSG", channel + " :You have successfully defused the bomb!");
                                                                    if (tmp_info.previous_bomb_holder.Equals(tmp_info.bomb_holder))
                                                                    {
                                                                    }
                                                                    else
                                                                    {
                                                                        ircbot.sendData("KICK", tmp_info.bomb_channel + " " + tmp_info.previous_bomb_holder + " :BOOM!!!");
                                                                    }
                                                                    hbombs.RemoveAt(index);
                                                                }
                                                                else
                                                                {
                                                                    main = ircbot;
                                                                    activate_bomb(channel);
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
                                                        ircbot.sendData("PRIVMSG", channel + " :You can not defuse the HBomb when you are idle.");
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :You can not defuse the HBomb when it is locked.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There isn't a bomb to defuse!");
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

        public void activate_bomb(string channel)
        {
            int index = 0;
            bool hbomb_active = false;
            hbomb_info tmp_info = new hbomb_info();
            foreach (hbomb_info bomb in hbombs)
            {
                if (bomb.bomb_channel.Equals(channel))
                {
                    tmp_info = bomb;
                    hbomb_active = true;
                    break;
                }
                index++;
            }
            if (hbomb_active)
            {
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :4,1/8,1!4,1" + Path.DirectorySeparatorChar + " 1,8 WARNING! Nuclear Strike Inbound  4,1/8,1!4,1" + Path.DirectorySeparatorChar + "");
                Thread.Sleep(1000);
                string msg = "";
                msg = "1,1.....15,1_.14,1-^^---....,15,1,--1,1.......";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "1,1.15,1_--14,1,.';,`.,';,.;;`;,.15,1--_1,1...";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "15,1<,.14,1;'`\".,;`..,;`*.,';`.15,1;'>)1,1.";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "15,1I.:;14,1.,`;~,`.;'`,.;'`,..15,1';`I1,1.";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "1,1.15,1._.14,1`'`..`';.,`';,`';,15,1._./1,1..";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "1,1....15,1```14,1--. . , ; .--15,1'''1,1.....";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "1,1..........4,1I1,1.8,1I7,1I1,1.8,1I4,1I1,1...........";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "1,1..........4,1I1,1.7,1I8,1I1,1.7,1I4,1I1,1...........";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "1,1.......,4,1-=4,1II7,1..I4,1.I=-,1,1........";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                msg = "1,1.......4,1`-=7,1#$8,1%&7,1%$#4,1=-'1,1........";
                main.sendData("PRIVMSG", tmp_info.bomb_channel + " :" + msg);
                if (tmp_info.bomb_holder != "")
                {
                    main.sendData("KICK", tmp_info.bomb_channel + " " + tmp_info.bomb_holder + " :BOOM!!!");
                }
                else
                {
                    main.sendData("KICK", tmp_info.bomb_channel + " " + tmp_info.previous_bomb_holder + " :BOOM!!!");
                }
                hbombs.RemoveAt(index);
            }
        }

        private void pass_hbomb(string pass_nick, string channel, string nick, bot ircbot, BotConfig conf, ref hbomb_info tmp_info, int index)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = ircbot.conf.server + "_#" + tab_name + ".log";
            bool nick_idle = true;

            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "seen" + Path.DirectorySeparatorChar + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string file_line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = file_line.Split(sep, 4);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(pass_nick) && new_line[1].Equals(channel))
                            {
                                DateTime current_date = DateTime.Now;
                                DateTime past_date = DateTime.Parse(new_line[2]);
                                double difference_second = 0;
                                difference_second = current_date.Subtract(past_date).TotalSeconds;
                                if (difference_second <= 600)
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
                tmp_info.bomb_holder = pass_nick;
                tmp_info.previous_bomb_holder = nick;
                hbombs[index] = tmp_info;
                ircbot.sendData("PRIVMSG", channel + " :" + nick + " passed the bomb to " + pass_nick);
                ircbot.sendData("NOTICE", pass_nick + " :You now have the bomb!  Type " + ircbot.conf.command + "pass <nick> to pass it to someone else, or type " + ircbot.conf.command + "defuse <color> to try to defuse it.");
                string colors = "";
                foreach (string wire in tmp_info.wire_colors)
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
