using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;

namespace IRCBot.Modules
{
    class moderation : Module
    {
        private List<System.Timers.Timer> unban_triggers;
        private List<List<string>> ban_info;
        private bot main;

        public moderation()
        {
            unban_triggers = new List<System.Timers.Timer>();
            ban_info = new List<List<string>>();
        }

        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            access access = new access();

            char[] charS = new char[] { ' ' };
            string module_name = ircbot.conf.module_config[module_id][0];
            string disallowed_modes = ircbot.conf.module_config[module_id][3];
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
                            blocked = ircbot.get_spam_status(channel, nick);
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
                                    case "founder":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +q " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.founder_level.ToString(), ircbot);
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
                                    case "defounder":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -q " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.founder_level.ToString(), ircbot);
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
                                    case "sop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +a " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.sop_level.ToString(), ircbot);
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
                                    case "asop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +a " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :SOP " + line[2] + " add " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.sop_level.ToString(), ircbot);
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
                                    case "deasop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -a " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :SOP " + line[2] + " del " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.sop_level.ToString(), ircbot);
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
                                    case "desop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -a " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.sop_level.ToString(), ircbot);
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
                                    case "op":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +o " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.op_level.ToString(), ircbot);
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
                                    case "aop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +o " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :AOP " + line[2] + " add " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.op_level.ToString(), ircbot);
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
                                    case "deaop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -o " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :AOP " + line[2] + " del " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.op_level.ToString(), ircbot);
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
                                    case "deop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -o " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.op_level.ToString(), ircbot);
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
                                    case "ahop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +h " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :HOP " + line[2] + " add " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.hop_level.ToString(), ircbot);
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
                                    case "deahop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -h " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :HOP " + line[2] + " del " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.hop_level.ToString(), ircbot);
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
                                    case "hop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +h " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.hop_level.ToString(), ircbot);
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
                                    case "dehop":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -h " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.hop_level.ToString(), ircbot);
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
                                    case "avoice":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +v " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :VOP " + line[2] + " add " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.voice_level.ToString(), ircbot);
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
                                    case "deavoice":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -v " + line[4].ToLower());
                                                ircbot.sendData("PRIVMSG", "chanserv :VOP " + line[2] + " del " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.voice_level.ToString(), ircbot);
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
                                    case "voice":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " +v " + line[4].ToLower());
                                                access.set_access_list(line[4].ToLower(), line[2], conf.voice_level.ToString(), ircbot);
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
                                    case "devoice":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("MODE", line[2] + " -v " + line[4].ToLower());
                                                access.del_access_list(line[4].ToLower(), line[2], conf.voice_level.ToString(), ircbot);
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
                                    case "mode":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                char[] arr = new_line[0].ToCharArray();

                                                bool mode_allowed = true;
                                                bool positive = true;
                                                int mode_index = 0;
                                                foreach (char c in arr)
                                                {
                                                    if (!c.Equals('+') && !c.Equals('-'))
                                                    {
                                                        char[] modes_disallowed = disallowed_modes.ToCharArray();
                                                        foreach (char m in modes_disallowed)
                                                        {
                                                            if (m.Equals(c))
                                                            {
                                                                mode_allowed = false;
                                                                break;
                                                            }
                                                        }
                                                        if (mode_allowed == true)
                                                        {
                                                            if (c.Equals('q') || c.Equals('a') || c.Equals('o') || c.Equals('h') || c.Equals('v'))
                                                            {
                                                                int mode_access = ircbot.get_access_num(c.ToString(), true);
                                                                if (nick_access < mode_access)
                                                                {
                                                                    mode_allowed = false;
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                        if (mode_allowed)
                                                        {
                                                            string leading_cmd = "";
                                                            if (positive)
                                                            {
                                                                leading_cmd = "+";
                                                            }
                                                            else
                                                            {
                                                                leading_cmd = "-";
                                                            }
                                                            bool nick_needed = false;
                                                            if (new_line.GetUpperBound(0) > 0)
                                                            {
                                                                string[] nicks = new_line[1].Split(charS);
                                                                if (nicks.GetUpperBound(0) >= mode_index)
                                                                {
                                                                    nick_needed = true;
                                                                }
                                                            }
                                                            if (nick_needed)
                                                            {
                                                                string[] nicks = new_line[1].Split(charS);
                                                                ircbot.sendData("MODE", line[2] + " " + leading_cmd + c.ToString() + " :" + nicks[mode_index]);
                                                            }
                                                            else
                                                            {
                                                                ircbot.sendData("MODE", line[2] + " " + leading_cmd + c.ToString());
                                                            }
                                                        }
                                                        mode_index++;
                                                    }
                                                    else if (c.Equals('+'))
                                                    {
                                                        positive = true;
                                                    }
                                                    else if (c.Equals('-'))
                                                    {
                                                        positive = false;
                                                    }
                                                }
                                                if (!mode_allowed)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You do not have permission to use that command.");
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
                                    case "topic":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    ircbot.sendData("TOPIC", line[2] + " :" + new_line[0] + " " + new_line[1]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("TOPIC", line[2] + " :" + new_line[0]);
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
                                    case "invite":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                ircbot.sendData("INVITE", new_line[0] + " " + line[2]);
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
                                    case "b":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                string nicks = new_line[0].TrimStart(':');
                                                char[] charSep = new char[] { ',' };
                                                string[] total_nicks = new_line[0].Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);

                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (sent_nick_access == conf.owner_level)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't ban my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't ban me!");
                                                }
                                                else
                                                {
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        string target_host = ircbot.get_user_host(total_nicks[0]);
                                                        string ban = "*!*@" + target_host;
                                                        if (target_host.Equals(""))
                                                        {
                                                            ban = new_line[0] + "!*@*";
                                                        }
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :" + new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ub":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                char[] charSep = new char[] { ',' };
                                                string[] total_nicks = new_line[0].Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                int sent_nick_access = ircbot.get_user_access(total_nicks[0].TrimStart(':'), line[2]);

                                                if (nick_access >= sent_nick_access)
                                                {
                                                    string target_host = ircbot.get_user_host(total_nicks[0]);
                                                    string ban = "*!*@" + target_host;
                                                    if (target_host.Equals(""))
                                                    {
                                                        ban = new_line[0] + "!*@*";
                                                    }
                                                    if (new_line.GetUpperBound(0) > 0)
                                                    {
                                                        ircbot.sendData("MODE", line[2] + " -b " + ban + " :" + new_line[1]);
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("MODE", line[2] + " -b " + ban + " :No Reason");
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                    case "clearban":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            ircbot.sendData("PRIVMSG", "chanserv :clear " + channel + " bans");
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "kb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                string nicks = new_line[0].TrimStart(':');
                                                char[] charSep = new char[] { ',' };
                                                string[] total_nicks = nicks.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);

                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (sent_nick_access == conf.owner_level)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick-ban my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick-ban me!");
                                                }
                                                else
                                                {
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        string target_host = ircbot.get_user_host(total_nicks[0]);
                                                        string ban = "*!*@" + target_host;
                                                        if (target_host.Equals(""))
                                                        {
                                                            ban = new_line[0] + "!*@*";
                                                        }
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :" + new_line[1]);
                                                            ircbot.sendData("KICK", line[2] + " " + total_nicks[0] + " :" + new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :No Reason");
                                                            ircbot.sendData("KICK", line[2] + " " + total_nicks[0] + " :No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "tb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 3, StringSplitOptions.RemoveEmptyEntries);
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    int time = Convert.ToInt32(new_line[0].TrimStart(':'));
                                                    string nicks = new_line[1];
                                                    char[] charSep = new char[] { ',' };
                                                    string[] total_nicks = nicks.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                    string target_host = ircbot.get_user_host(total_nicks[0]);
                                                    int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);

                                                    bool tmp_me = false;
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_me = true;
                                                        }
                                                    }
                                                    if (sent_nick_access == conf.owner_level)
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :You can't ban my owner!");
                                                    }
                                                    else if (tmp_me == true)
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :You can't ban me!");
                                                    }
                                                    else
                                                    {
                                                        if (nick_access >= sent_nick_access)
                                                        {
                                                            string ban = "*!*@" + target_host;
                                                            if (target_host.Equals(""))
                                                            {
                                                                ban = total_nicks[0] + "!*@*";
                                                            }
                                                            if (new_line.GetUpperBound(0) > 1)
                                                            {
                                                                ircbot.sendData("MODE", line[2] + " +b " + ban + " :" + new_line[2]);
                                                            }
                                                            else
                                                            {
                                                                ircbot.sendData("MODE", line[2] + " +b " + ban + " :No Reason");
                                                            }
                                                            Timer unban_trigger = new Timer();
                                                            unban_trigger.Interval = (Convert.ToInt32(new_line[0]) * 1000);
                                                            unban_trigger.Enabled = true;
                                                            unban_trigger.AutoReset = false;
                                                            unban_trigger.Elapsed += (sender, e) => unban_nick(sender, e, total_nicks[0], target_host, line[2]);
                                                            unban_triggers.Add(unban_trigger);
                                                            main = ircbot;
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "tkb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 3, StringSplitOptions.RemoveEmptyEntries);
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    int time = Convert.ToInt32(new_line[0].TrimStart(':'));
                                                    string nicks = new_line[1];
                                                    char[] charSep = new char[] { ',' };
                                                    string[] total_nicks = nicks.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                    int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);
                                                    string target_host = ircbot.get_user_host(total_nicks[0]);

                                                    bool tmp_me = false;
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_me = true;
                                                        }
                                                    }
                                                    if (sent_nick_access == conf.owner_level)
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :You can't kick-ban my owner!");
                                                    }
                                                    else if (tmp_me == true)
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :You can't kick-ban me!");
                                                    }
                                                    else
                                                    {
                                                        if (nick_access >= sent_nick_access)
                                                        {
                                                            string ban = "*!*@" + target_host;
                                                            if (target_host.Equals(""))
                                                            {
                                                                ban = new_line[1] + "!*@*";
                                                            }
                                                            if (new_line.GetUpperBound(0) > 1)
                                                            {
                                                                ircbot.sendData("MODE", line[2] + " +b " + ban + " :" + new_line[2]);
                                                                ircbot.sendData("KICK", line[2] + " " + total_nicks[0] + " :" + new_line[2]);
                                                            }
                                                            else
                                                            {
                                                                ircbot.sendData("MODE", line[2] + " +b " + ban + " :No Reason");
                                                                ircbot.sendData("KICK", line[2] + " " + total_nicks[0] + " :No Reason");
                                                            }
                                                            System.Timers.Timer unban_trigger = new System.Timers.Timer();
                                                            unban_trigger.Interval = (Convert.ToInt32(new_line[0]) * 1000);
                                                            unban_trigger.Enabled = true;
                                                            unban_trigger.AutoReset = false;
                                                            unban_trigger.Elapsed += (sender, e) => unban_nick(sender, e, total_nicks[0], target_host, line[2]);
                                                            unban_triggers.Add(unban_trigger);
                                                            main = ircbot;
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ak":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                string nicks = new_line[0].TrimStart(':');
                                                char[] charSep = new char[] { ',' };
                                                string[] total_nicks = nicks.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (sent_nick_access == conf.owner_level)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick me!");
                                                }
                                                else
                                                {
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        string target_host = ircbot.get_user_host(total_nicks[0]);
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            add_auto(total_nicks[0], line[2], target_host, "k", new_line[1], ircbot);
                                                        }
                                                        else
                                                        {
                                                            add_auto(total_nicks[0], line[2], target_host, "k", "No Reason", ircbot);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ab":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                string target_host = ircbot.get_user_host(new_line[0]);
                                                string nicks = new_line[0].TrimStart(':');
                                                char[] charSep = new char[] { ',' };
                                                string[] total_nicks = nicks.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (sent_nick_access == conf.owner_level)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't ban my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't ban me!");
                                                }
                                                else
                                                {
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            add_auto(total_nicks[0], line[2], target_host, "b", new_line[1], ircbot);
                                                        }
                                                        else
                                                        {
                                                            add_auto(total_nicks[0], line[2], target_host, "b", "No Reason", ircbot);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "akb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                string target_host = ircbot.get_user_host(new_line[0]);
                                                string nicks = new_line[0].TrimStart(':');
                                                char[] charSep = new char[] { ',' };
                                                string[] total_nicks = nicks.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (sent_nick_access == conf.owner_level)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick-ban my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick-ban me!");
                                                }
                                                else
                                                {
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            add_auto(total_nicks[0], line[2], target_host, "kb", new_line[1], ircbot);
                                                        }
                                                        else
                                                        {
                                                            add_auto(total_nicks[0], line[2], target_host, "kb", "No Reason", ircbot);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "deak":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string target_host = ircbot.get_user_host(line[4].ToLower());
                                                del_auto(line[4].ToLower(), line[2], target_host, "k", ircbot);
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
                                    case "deab":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string target_host = ircbot.get_user_host(line[4].ToLower());
                                                del_auto(line[4].ToLower(), line[2], target_host, "b", ircbot);
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
                                    case "deakb":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string target_host = ircbot.get_user_host(line[4].ToLower());
                                                del_auto(line[4].ToLower(), line[2], target_host, "kb", ircbot);
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
                                    case "k":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].ToLower().Split(charS, 2);
                                                string nicks = new_line[0].TrimStart(':');
                                                char[] charSep = new char[] { ',' };
                                                string[] total_nicks = nicks.Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                int sent_nick_access = ircbot.get_user_access(total_nicks[0], line[2]);
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (sent_nick_access == conf.owner_level)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :You can't kick me!");
                                                }
                                                else
                                                {
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            ircbot.sendData("KICK", line[2] + " " + total_nicks[0] + " :" + new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("KICK", line[2] + " " + total_nicks[0] + " :No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
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
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "kme":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("KICK", line[2] + " " + nick + " :" + line[4]);
                                            }
                                            else
                                            {
                                                ircbot.sendData("KICK", line[2] + " " + nick + " :No Reason");
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            if (type.Equals("join"))
            {
                string[] user_info = line[0].Split('@');
                string nick_host = user_info[1];
                check_auto(nick, channel.TrimStart(':'), nick_host, ircbot);
            }
        }

        public void check_auto(string nick, string channel, string hostname, bot ircbot)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "auto_kb" + Path.DirectorySeparatorChar + ircbot.server_name + "_list.txt";
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 1];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] auto_nick = file_line.Split(charSeparator, 6);
                    if (auto_nick.GetUpperBound(0) > 0)
                    {
                        if ((nick.Equals(auto_nick[0]) == true || hostname.Equals(auto_nick[1])) && channel.Equals(auto_nick[2]))
                        {
                            string ban = "*!*@" + hostname;
                            if (hostname.Equals(""))
                            {
                                ban = nick + "!*@*";
                            }
                            if (auto_nick[4] == "")
                            {
                                auto_nick[4] = "Auto " + auto_nick[3];
                            }
                            if (auto_nick[3].Equals("k"))
                            {
                                ircbot.sendData("KICK", channel + " " + nick + " :" + auto_nick[4]);
                            }
                            else if (auto_nick[3].Equals("b"))
                            {
                                ircbot.sendData("MODE", channel + " +b " + ban + " :" + auto_nick[4]);
                            }
                            else if (auto_nick[3].Equals("kb"))
                            {
                                ircbot.sendData("MODE", channel + " +b " + ban + " :" + auto_nick[4]);
                                ircbot.sendData("KICK", channel + " " + nick + " :" + auto_nick[4]);
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                            new_file[counter] = file_line;
                            counter++;
                        }
                    }
                }
            }
        }

        public void unban_nick(object sender, EventArgs e, string nick, string host, string channel)
        {
            System.Timers.Timer unban_trigger = (System.Timers.Timer)sender;
            unban_trigger.Enabled = false;
            string ban = "*!*@" + host;
            if (host.Equals(""))
            {
                ban = nick + "!*@*";
            }
            main.sendData("MODE", channel + " -b " + ban);
        }

        private void add_auto(string nick, string channel, string hostname, string type, string reason, bot ircbot)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "auto_kb" + Path.DirectorySeparatorChar + ircbot.server_name + "_list.txt";
            string add_line = nick + "*" + hostname + "*" + channel + "*" + type + "*" + reason + "*" + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt");
            bool found_nick = false;
            if (File.Exists(list_file))
            {
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                List<string> new_file = new List<string>();
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] auto_nick = file_line.Split(charSeparator, 5);
                    if (nick.Equals(auto_nick[0]) && hostname.Equals(auto_nick[1]) && channel.Equals(auto_nick[2]) && type.Equals(auto_nick[3]))
                    {
                        new_file.Add(add_line);
                        found_nick = true;
                    }
                    else
                    {
                        new_file.Add(file_line);
                    }
                }
                if (found_nick == false)
                {
                    new_file.Add(add_line);
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
                string ban = "*!*@" + hostname;
                if (hostname.Equals(""))
                {
                    ban = nick + "!*@*";
                }
                if (type.Equals("k"))
                {
                    ircbot.sendData("KICK", channel + " " + nick + " :" + reason);
                }
                else if (type.Equals("b"))
                {
                    ircbot.sendData("MODE", channel + " +b " + ban + " :" + reason);
                }
                else if (type.Equals("kb"))
                {
                    ircbot.sendData("MODE", channel + " +b " + ban + " :" + reason);
                    ircbot.sendData("KICK", channel + " " + nick + " :" + reason);
                }
                else
                {
                }
            }
            else
            {
                System.IO.File.WriteAllText(@list_file, add_line);
            }
            ircbot.sendData("PRIVMSG", channel + " :" + nick + " has been added to the a" + type + " list.");
        }

        private void del_auto(string nick, string channel, string hostname, string type, bot ircbot)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "auto_kb" + Path.DirectorySeparatorChar + ircbot.server_name + "_list.txt";
            bool found_nick = false;
            if (File.Exists(list_file))
            {
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                List<string> new_file = new List<string>();
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] auto_nick = file_line.Split(charSeparator, 5);
                    if (nick.Equals(auto_nick[0]) && hostname.Equals(auto_nick[1]) && channel.Equals(auto_nick[2]) && type.Equals(auto_nick[3]))
                    {
                        found_nick = true;
                    }
                    else
                    {
                        new_file.Add(file_line);
                    }
                }
                if (found_nick == false)
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + nick + " is not in the a" + type + " list.");
                }
                else
                {
                    System.IO.File.WriteAllLines(@list_file, new_file);
                    string ban = "*!*@" + hostname;
                    if (hostname.Equals(""))
                    {
                        ban = nick + "!*@*";
                    }
                    if (type.Equals("b"))
                    {
                        ircbot.sendData("MODE", channel + " -b " + ban);
                    }
                    else if (type.Equals("kb"))
                    {
                        ircbot.sendData("MODE", channel + " -b " + ban);
                    }
                    else
                    {
                    }
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :" + nick + " is not in the a" + type + " list.");
            }
            if (found_nick == true)
            {
                ircbot.sendData("PRIVMSG", channel + " :" + nick + " has been removed from the a" + type + " list.");
            }
        }
    }
}
