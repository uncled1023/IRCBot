using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace IRCBot
{
    class quote
    {
        public void quote_control(string[] line, string command, Interface ircbot, IRCConfig conf, int nick_access, string nick)
        {
            switch (command)
            {
                case "quote":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        get_quote(line[2], ircbot, conf);
                    }
                    break;
            }
        }

        public void add_quote(string nick, string channel, string[] line, Interface ircbot, IRCConfig conf)
        {
            if (!nick.Equals(conf.nick) && !line[3].Remove(0, 1).StartsWith(conf.command))
            {
                string tab_name = channel.TrimStart('#');
                string pattern = "[^a-zA-Z0-9]"; //regex pattern
                string new_tab_name = Regex.Replace(tab_name, pattern, "_");
                string[] server = conf.server.Split('.');
                string file_name = server[1] + "-#" + new_tab_name + ".log";
                if (Directory.Exists(ircbot.cur_dir + "\\modules\\quotes\\logs") == false)
                {
                    Directory.CreateDirectory(ircbot.cur_dir + "\\modules\\quotes\\logs");
                }
                StreamWriter log_file = File.AppendText(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name);
                log_file.WriteLine(line[3].Remove(0, 1) + " [" + nick + "]");
                log_file.Close();
            }
        }

        private void get_quote(string channel, Interface ircbot, IRCConfig conf)
        {
            string[] server = conf.server.Split('.');
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = server[1] + "-#" + tab_name + ".log";
            if (File.Exists(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\quotes\\logs\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    while (line == "")
                    {
                        Random random = new Random();
                        int index = random.Next(0, number_of_lines);
                        line = log_file[index];
                    }
                    ircbot.sendData("PRIVMSG", channel + " :" + line);
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
            }
        }
    }
}
