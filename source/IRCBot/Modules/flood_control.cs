using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace IRCBot.Modules
{
    class flood_control : Module
    {
        private List<spam_check> spam_logs = new List<spam_check>();
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            int max_lines = Convert.ToInt32(ircbot.conf.module_config[module_id][3]);
            int check_timeout = Convert.ToInt32(ircbot.conf.module_config[module_id][4]);
            bool warn = Convert.ToBoolean(ircbot.conf.module_config[module_id][5]);
            string warn_msg = ircbot.conf.module_config[module_id][6];
            bool kick = Convert.ToBoolean(ircbot.conf.module_config[module_id][7]);
            string kick_msg = ircbot.conf.module_config[module_id][8];
            bool ban = Convert.ToBoolean(ircbot.conf.module_config[module_id][9]);
            string ban_msg = ircbot.conf.module_config[module_id][10];
            if (type.Equals("channel"))
            {
                bool nick_found = false;
                int cur_lines = 0;
                int index = 0;
                foreach (spam_check spam_log in spam_logs)
                {
                    if (spam_log.channel.Equals(channel) && spam_log.nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                    {
                        nick_found = true;
                        cur_lines = spam_log.lines;
                        break;
                    }
                    index++;
                }
                if (cur_lines >= max_lines)
                {
                    if (warn)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + warn_msg);
                    }
                    if (ban)
                    {
                        string target_host = ircbot.get_user_host(nick);
                        string tmp_ban = "*!*@" + target_host;
                        if (target_host.Equals(""))
                        {
                            tmp_ban = nick + "!*@*";
                        }
                        ircbot.sendData("MODE", line[2] + " +b " + tmp_ban + " :" + ban_msg);
                    }
                    if (kick)
                    {
                        ircbot.sendData("KICK", channel + " " + nick + " :" + kick_msg);
                    }
                    spam_logs[index].timer.Enabled = false;
                    spam_logs.RemoveAt(index);
                }
                else
                {
                    if (nick_found)
                    {
                        spam_logs[index].lines++;
                    }
                    else
                    {
                        spam_check tmp_spam = new spam_check();
                        tmp_spam.channel = channel;
                        tmp_spam.nick = nick;
                        tmp_spam.lines = 1;
                        Timer tmp_timer = new Timer();
                        tmp_timer.Interval = check_timeout;
                        tmp_timer.Elapsed += (sender, e) => spam_tick(sender, e, channel, nick);
                        tmp_timer.Enabled = true;
                        tmp_timer.AutoReset = false;
                        tmp_spam.timer = tmp_timer;
                        spam_logs.Add(tmp_spam);
                    }
                }
            }
        }

        private void spam_tick(object sender, EventArgs e, string channel, string nick)
        {
            int index = 0;
            bool nick_found = false;
            foreach (spam_check spam_log in spam_logs)
            {
                if (spam_log.channel.Equals(channel) && spam_log.nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    nick_found = true;
                    break;
                }
                index++;
            }
            if (nick_found)
            {
                spam_logs[index].timer.Enabled = false;
                spam_logs.RemoveAt(index);
            }
        }
    }

    class spam_check
    {
        public string channel;
        public string nick;
        public int lines;
        public Timer timer;
    }
}
