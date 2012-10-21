using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot
{
    class pingme
    {
        private List<List<string>> ping_list = new List<List<string>>();
        public void pingme_control(string[] line, string command, Interface ircbot, int nick_access, string nick, string channel)
        {
            switch (command)
            {
                case "pingme":
                    ircbot.spam_count++;
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        ircbot.sendData("PING", nick);
                        ping_list.Add(new List<string>());
                        int index = ping_list.Count() - 1;
                        string current_time = DateTime.Now.ToLongTimeString();
                        ping_list[index].Add(nick);
                        ping_list[index].Add(channel);
                        ping_list[index].Add(current_time);
                    }
                    break;
            }
        }

        public void check_ping(string[] line, Interface ircbot)
        {
            if (line.GetUpperBound(0) > 2)
            {
                if (line[1].ToLower().Equals("pong"))
                {
                    string nick = line[3].TrimStart(':');
                    for (int x = 0; x < ping_list.Count(); x++)
                    {
                        if (ping_list[x][0].Equals(nick))
                        {
                            DateTime current_time = DateTime.Now;
                            DateTime ping_time = Convert.ToDateTime(ping_list[x][2]);
                            string dif_time = current_time.Subtract(ping_time).ToString();
                            ircbot.sendData("PRIVMSG", ping_list[x][1] + " :" + nick + ", your ping is " + dif_time);
                            ping_list.RemoveAt(x);
                            break;
                        }
                    }
                }
            }
        }
    }
}
