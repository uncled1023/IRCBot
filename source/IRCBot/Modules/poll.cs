using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class poll : Module
    {
        private bool poll_active = false;
        private string poll_owner = "";
        private string poll_question = "";
        private List<List<string>> poll_answers = new List<List<string>>();
        private List<List<string>> poll_nick_responses = new List<List<string>>();

        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
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
                            if (ircbot.spam_activated == true)
                            {
                                blocked = true;
                            }
                        }
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(command))
                            {
                                cmd_found = true;
                                break;
                            }
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (poll_active == false)
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    poll_active = true;
                                                    string[] lines = line[4].Split('|');
                                                    poll_question = lines[0];
                                                    poll_owner = nick;
                                                    for (int x = 1; x <= lines.GetUpperBound(0); x++)
                                                    {
                                                        List<string> tmp_list = new List<string>();
                                                        tmp_list.Add(lines[x]);
                                                        tmp_list.Add("0");
                                                        poll_answers.Add(tmp_list);
                                                    }
                                                    ircbot.sendData("PRIVMSG", channel + " :Poll has been started by " + poll_owner + ": " + poll_question);
                                                    for (int x = 0; x < poll_answers.Count(); x++)
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + (x + 1).ToString() + ") " + poll_answers[x][0]);
                                                    }
                                                    ircbot.sendData("PRIVMSG", channel + " :To Vote, type .vote <answer_number>.  You may only vote once per poll.  You can change your vote by voting for a different answer.");
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :There is currently a poll active right now");
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (poll_owner.Equals(nick))
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    if (poll_active == true)
                                                    {
                                                        List<string> tmp_list = new List<string>();
                                                        tmp_list.Add(line[4]);
                                                        tmp_list.Add("0");
                                                        poll_answers.Add(tmp_list);
                                                        ircbot.sendData("PRIVMSG", channel + " :An Answer has been added to the poll.");
                                                        ircbot.sendData("PRIVMSG", channel + " :" + poll_answers.Count().ToString() + ")" + line[4]);
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
                                                ircbot.sendData("PRIVMSG", channel + " :You are not the poll owner.");
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (poll_owner.Equals(nick))
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    if (poll_active == true)
                                                    {
                                                        for (int x = 0; x < poll_answers.Count(); x++)
                                                        {
                                                            if (x == Convert.ToInt32(line[4]))
                                                            {
                                                                ircbot.sendData("PRIVMSG", channel + " :Answer " + x.ToString() + " has been removed.");
                                                                poll_answers.RemoveAt(x);
                                                                break;
                                                            }
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
                                                ircbot.sendData("PRIVMSG", channel + " :You are not the poll owner.");
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (poll_active == true)
                                            {
                                                if (poll_owner.Equals(nick) || nick_access > Convert.ToInt32(ircbot.get_user_access(poll_owner, channel)))
                                                {
                                                    poll_active = false;

                                                    ircbot.sendData("PRIVMSG", channel + " :Results of poll by " + poll_owner + ": " + poll_question);
                                                    for (int x = 0; x < poll_answers.Count(); x++)
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + (x + 1).ToString() + ") " + poll_answers[x][0] + " | " + poll_answers[x][1] + " votes");
                                                    }
                                                    poll_answers.Clear();
                                                    poll_nick_responses.Clear();
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (poll_active == true)
                                            {
                                                ircbot.sendData("NOTICE", nick + " :Poll by " + poll_owner + ": " + poll_question);
                                                for (int x = 0; x < poll_answers.Count(); x++)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + (x + 1).ToString() + ") " + poll_answers[x][0] + " | " + poll_answers[x][1] + " votes");
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
                                            ircbot.spam_count++;
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                if (poll_active == true)
                                                {
                                                    try
                                                    {
                                                        int vote = Convert.ToInt32(line[4]);
                                                        if (vote > 0 && vote <= poll_answers.Count())
                                                        {
                                                            bool nick_voted = false;
                                                            int index = 0;
                                                            string nick_host = ircbot.get_user_host(nick);
                                                            if (nick_host.Equals(""))
                                                            {
                                                                nick_host = nick;
                                                            }
                                                            for (int x = 0; x < poll_nick_responses.Count(); x++)
                                                            {
                                                                if (poll_nick_responses[x][0].Equals(nick_host))
                                                                {
                                                                    nick_voted = true;
                                                                    index = Convert.ToInt32(poll_nick_responses[x][1]);
                                                                    poll_nick_responses[x][1] = vote.ToString();
                                                                    poll_answers[index - 1][1] = (Convert.ToInt32(poll_answers[index - 1][1]) - 1).ToString();
                                                                    poll_answers[vote - 1][1] = (Convert.ToInt32(poll_answers[vote - 1][1]) + 1).ToString();
                                                                    break;
                                                                }
                                                            }
                                                            if (nick_voted == false)
                                                            {
                                                                List<string> tmp_list = new List<string>();
                                                                tmp_list.Add(nick_host);
                                                                tmp_list.Add(vote.ToString());
                                                                poll_nick_responses.Add(tmp_list);
                                                                poll_answers[vote - 1][1] = (Convert.ToInt32(poll_answers[vote - 1][1]) + 1).ToString();
                                                            }
                                                            ircbot.sendData("PRIVMSG", channel + " :Thank you for voting for " + vote.ToString());
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", channel + " :You need to vote for a valid answer.");
                                                        }
                                                    }
                                                    catch (Exception ex)
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
