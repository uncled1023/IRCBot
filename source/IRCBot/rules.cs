using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot
{
    class rules
    {
        public void rules_control(string[] line, string command, Interface ircbot, int nick_access, string nick)
        {
            switch (command)
            {
                case "rules":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        get_rules(nick, line[2], ircbot);
                    }
                    break;
                case "addrule":
                    break;
                case "delrule":
                    break;
            }
        }

        private void get_rules(string nick, string channel, Interface ircbot)
        {
            if (File.Exists(ircbot.cur_dir + "\\modules\\rules\\rules.txt"))
            {
                string[] answer_file = System.IO.File.ReadAllLines(ircbot.cur_dir + "\\modules\\rules\\rules.txt");
                int number_of_lines = answer_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    int index = 1;
                    foreach (string line in answer_file)
                    {
                        string[] split = line.Split('*');
                        if (split.GetUpperBound(0) > 0 && channel.Equals(split[0]))
                        {
                            ircbot.sendData("NOTICE", nick + " :Rule " + index + ") " + split[1]);
                            index++;
                        }
                    }
                    if (index == 1)
                    {
                        ircbot.sendData("NOTICE", nick + " :There are no Rules");
                    }
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :There are no Rules");
                }
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :There are no Rules");
            }
        }
    }
}
