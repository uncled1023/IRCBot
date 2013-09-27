using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

struct poll_info
{
    public string channel;
    public string owner;
    public string question;
    public List<List<string>> answers;
    public List<List<string>> nick_responses;
}

namespace IRCBot.Modules
{
    class poll : Module
    {
        private List<poll_info> poll_list = new List<poll_info>();

        public override void control(bot ircbot, ref BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == true)
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
                                    case "poll":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool poll_active = false;
                                            foreach (poll_info tmp_poll in poll_list)
                                            {
                                                if (tmp_poll.channel.Equals(channel))
                                                {
                                                    poll_active = true;
                                                    break;
                                                }
                                            }
                                            if (poll_active == false)
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    poll_info temp_poll = new poll_info();
                                                    poll_active = true;
                                                    string[] lines = line[4].Split('|');
                                                    temp_poll.question = lines[0];
                                                    temp_poll.owner = nick;
                                                    temp_poll.channel = channel;
                                                    temp_poll.answers = new List<List<string>>();
                                                    temp_poll.nick_responses = new List<List<string>>();
                                                    for (int x = 1; x <= lines.GetUpperBound(0); x++)
                                                    {
                                                        List<string> tmp_list = new List<string>();
                                                        tmp_list.Add(lines[x]);
                                                        tmp_list.Add("0");
                                                        temp_poll.answers.Add(tmp_list);
                                                    }
                                                    ircbot.sendData("PRIVMSG", channel + " :Poll has been started by " + temp_poll.owner + ": " + temp_poll.question);
                                                    for (int x = 0; x < temp_poll.answers.Count(); x++)
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + (x + 1).ToString() + ") " + temp_poll.answers[x][0]);
                                                    }
                                                    ircbot.sendData("PRIVMSG", channel + " :To Vote, type " + ircbot.ircbot.irc_conf.command + "vote <answer_number>.  You may only vote once per poll.  You can change your vote by voting for a different answer.");
                                                    poll_list.Add(temp_poll);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There is currently a poll active right now.  To view the current results, type " + ircbot.ircbot.irc_conf.command + "results");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "addanswer":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool poll_active = false;
                                            poll_info cur_poll = new poll_info();
                                            foreach (poll_info tmp_poll in poll_list)
                                            {
                                                if (tmp_poll.channel.Equals(channel))
                                                {
                                                    cur_poll = tmp_poll;
                                                    poll_active = true;
                                                    break;
                                                }
                                            }
                                            if (poll_active == true)
                                            {
                                                if (cur_poll.owner.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    if (line.GetUpperBound(0) > 3)
                                                    {
                                                            List<string> tmp_list = new List<string>();
                                                            tmp_list.Add(line[4]);
                                                            tmp_list.Add("0");
                                                            cur_poll.answers.Add(tmp_list);
                                                            ircbot.sendData("PRIVMSG", channel + " :An Answer has been added to the poll.");
                                                            ircbot.sendData("PRIVMSG", channel + " :" + cur_poll.answers.Count().ToString() + ")" + line[4]);
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :You are not the poll owner.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There is currently no poll active right now");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delanswer":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool poll_active = false;
                                            poll_info cur_poll = new poll_info();
                                            foreach (poll_info tmp_poll in poll_list)
                                            {
                                                if (tmp_poll.channel.Equals(channel))
                                                {
                                                    cur_poll = tmp_poll;
                                                    poll_active = true;
                                                    break;
                                                }
                                            }
                                            if (poll_active == true)
                                            {
                                                if (cur_poll.owner.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    if (line.GetUpperBound(0) > 3)
                                                    {
                                                        for (int x = 0; x < cur_poll.answers.Count(); x++)
                                                        {
                                                            if (x == (Convert.ToInt32(line[4]) - 1))
                                                            {
                                                                ircbot.sendData("PRIVMSG", channel + " :Answer " + x.ToString() + " has been removed.");
                                                                cur_poll.answers.RemoveAt(x);
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :You are not the poll owner.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There is currently no poll active right now");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "stoppoll":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool poll_active = false;
                                            int index = 0;
                                            poll_info cur_poll = new poll_info();
                                            foreach (poll_info tmp_poll in poll_list)
                                            {
                                                if (tmp_poll.channel.Equals(channel))
                                                {
                                                    cur_poll = tmp_poll;
                                                    poll_active = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if (poll_active == true)
                                            {
                                                if (cur_poll.owner.Equals(nick, StringComparison.InvariantCultureIgnoreCase) || nick_access > Convert.ToInt32(ircbot.get_user_access(cur_poll.owner, channel)))
                                                {
                                                    poll_active = false;

                                                    ircbot.sendData("PRIVMSG", channel + " :Results of poll by " + cur_poll.owner + ": " + cur_poll.question);
                                                    for (int x = 0; x < cur_poll.answers.Count(); x++)
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + (x + 1).ToString() + ") " + cur_poll.answers[x][0] + " | " + cur_poll.answers[x][1] + " votes");
                                                    }
                                                    poll_list.RemoveAt(index);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :You are not the poll owner.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There is currently no poll active right now");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "results":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool poll_active = false;
                                            poll_info cur_poll = new poll_info();
                                            foreach (poll_info tmp_poll in poll_list)
                                            {
                                                if (tmp_poll.channel.Equals(channel))
                                                {
                                                    cur_poll = tmp_poll;
                                                    poll_active = true;
                                                    break;
                                                }
                                            }
                                            if (poll_active == true)
                                            {
                                                ircbot.sendData("NOTICE", nick + " :Poll by " + cur_poll.owner + ": " + cur_poll.question);
                                                for (int x = 0; x < cur_poll.answers.Count(); x++)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + (x + 1).ToString() + ") " + cur_poll.answers[x][0] + " | " + cur_poll.answers[x][1] + " votes");
                                                }
                                                ircbot.sendData("NOTICE", nick + " :To Vote, type .vote <answer_number>.  You may only vote once per poll.  You can change your vote by voting for a different answer.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :There is currently no poll active right now");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "vote":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                bool poll_active = false;
                                                poll_info cur_poll = new poll_info();
                                                foreach (poll_info tmp_poll in poll_list)
                                                {
                                                    if (tmp_poll.channel.Equals(channel))
                                                    {
                                                        cur_poll = tmp_poll;
                                                        poll_active = true;
                                                        break;
                                                    }
                                                }
                                                if (poll_active == true)
                                                {
                                                    try
                                                    {
                                                        int vote = Convert.ToInt32(line[4]);
                                                        if (vote > 0 && vote <= cur_poll.answers.Count())
                                                        {
                                                            bool nick_voted = false;
                                                            int index = 0;
                                                            string nick_host = ircbot.get_user_host(nick);
                                                            if (nick_host.Equals(""))
                                                            {
                                                                nick_host = nick;
                                                            }
                                                            for (int x = 0; x < cur_poll.nick_responses.Count(); x++)
                                                            {
                                                                if (cur_poll.nick_responses[x][0].Equals(nick_host))
                                                                {
                                                                    nick_voted = true;
                                                                    index = Convert.ToInt32(cur_poll.nick_responses[x][1]);
                                                                    cur_poll.nick_responses[x][1] = vote.ToString();
                                                                    cur_poll.answers[index - 1][1] = (Convert.ToInt32(cur_poll.answers[index - 1][1]) - 1).ToString();
                                                                    cur_poll.answers[vote - 1][1] = (Convert.ToInt32(cur_poll.answers[vote - 1][1]) + 1).ToString();
                                                                    break;
                                                                }
                                                            }
                                                            if (nick_voted == false)
                                                            {
                                                                List<string> tmp_list = new List<string>();
                                                                tmp_list.Add(nick_host);
                                                                tmp_list.Add(vote.ToString());
                                                                cur_poll.nick_responses.Add(tmp_list);
                                                                cur_poll.answers[vote - 1][1] = (Convert.ToInt32(cur_poll.answers[vote - 1][1]) + 1).ToString();
                                                            }
                                                            ircbot.sendData("PRIVMSG", channel + " :Thank you for voting for " + vote.ToString());
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", channel + " :You need to vote for a valid answer.");
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :You need to vote for a valid answer.");
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :There is currently no poll active right now");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
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
