using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace Bot.Modules
{
    class flood_control : Module
    {
        private List<spam_check> spam_logs = new List<spam_check>();
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            int max_lines = Convert.ToInt32(this.Options["max_lines"]);
            int check_timeout = Convert.ToInt32(this.Options["spam_timeout"]);
            bool warn = this.Options["warn"];
            string warn_msg = this.Options["warn_msg"];
            bool kick = this.Options["kick"];
            string kick_msg = this.Options["kick_msg"];
            bool ban = this.Options["ban"];
            string ban_msg = this.Options["ban_msg"];
            if (type.Equals("channel"))
            {
                bool nick_found = false;
                int cur_lines = 0;
                int index = 0;
                foreach (spam_check spam_log in spam_logs)
                {
                    if (spam_log.channel.Equals(channel) && spam_log.nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase) && !nick.Equals(ircbot.Nick, StringComparison.InvariantCultureIgnoreCase))
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
                        string target_host = ircbot.get_nick_host(nick);
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
                    else if (!nick.Equals(ircbot.Nick, StringComparison.InvariantCultureIgnoreCase))
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
