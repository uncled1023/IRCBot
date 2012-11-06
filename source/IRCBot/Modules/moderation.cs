using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot
{
    class moderation
    {
        private System.Timers.Timer unban_trigger;
        private string unban_nick_string;
        private string unban_host_string;
        private string unban_channel_string;
        private bot main;

        public moderation()
        {
            unban_trigger = new System.Timers.Timer();
            unban_trigger.Elapsed += unban_nick;
        }

        public void moderation_control(string[] line, string command, bot ircbot, IRCConfig conf, int nick_access, string nick)
        {
            access access = new access();

            char[] charS = new char[] { ' ' };
            switch (command)
            {
                case "founder":
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +q " + line[4]);
                            access.set_access_list(line[4], line[2], conf.founder_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -q " + line[4]);
                            access.del_access_list(line[4], line[2], conf.founder_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +a " + line[4]);
                            access.set_access_list(line[4], line[2], conf.sop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +a " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :SOP " + line[2] + " add " + line[4]);
                            access.set_access_list(line[4], line[2], conf.sop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -a " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :SOP " + line[2] + " del " + line[4]);
                            access.del_access_list(line[4], line[2], conf.sop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -a " + line[4]);
                            access.del_access_list(line[4], line[2], conf.sop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +o " + line[4]);
                            access.set_access_list(line[4], line[2], conf.op_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +o " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :AOP " + line[2] + " add " + line[4]);
                            access.set_access_list(line[4], line[2], conf.op_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -o " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :AOP " + line[2] + " del " + line[4]);
                            access.del_access_list(line[4], line[2], conf.op_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -o " + line[4]);
                            access.del_access_list(line[4], line[2], conf.op_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " + " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :HOP " + line[2] + " add " + line[4]);
                            access.set_access_list(line[4], line[2], conf.hop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -h " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :HOP " + line[2] + " del " + line[4]);
                            access.del_access_list(line[4], line[2], conf.hop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +h " + line[4]);
                            access.set_access_list(line[4], line[2], conf.hop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -h " + line[4]);
                            access.del_access_list(line[4], line[2], conf.hop_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +v " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :VOP " + line[2] + " add " + line[4]);
                            access.set_access_list(line[4], line[2], conf.voice_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -v " + line[4]);
                            ircbot.sendData("PRIVMSG", "chanserv :VOP " + line[2] + " del " + line[4]);
                            access.del_access_list(line[4], line[2], conf.voice_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " +v " + line[4]);
                            access.set_access_list(line[4], line[2], conf.voice_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            ircbot.sendData("MODE", line[2] + " -v " + line[4]);
                            access.del_access_list(line[4], line[2], conf.voice_level.ToString(), ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                ircbot.sendData("MODE", line[2] + " " + new_line[0] + " :" + new_line[1]);
                            }
                            else
                            {
                                ircbot.sendData("MODE", line[2] + " " + new_line[0]);
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
                    if (nick_access >= ircbot.get_command_access(command))
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            string nicks = new_line[0].TrimStart(':');
                            string[] total_nicks = nicks.Split(',');
                            int sent_nick_access = ircbot.get_user_access(new_line[0].TrimStart(':'), line[2]);

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
                                    string target_host = ircbot.get_user_host(new_line[0]);
                                    string ban = "*!*@" + target_host;
                                    if (target_host.Equals("was"))
                                    {
                                        ban = nick + "!*@*";
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            int sent_nick_access = ircbot.get_user_access(new_line[0].TrimStart(':'), line[2]);

                            if (nick_access >= sent_nick_access)
                            {
                                string target_host = ircbot.get_user_host(new_line[0]);
                                string ban = "*!*@" + target_host;
                                if (target_host.Equals("was"))
                                {
                                    ban = nick + "!*@*";
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
                case "kb":
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            string nicks = new_line[0].TrimStart(':');
                            string[] total_nicks = nicks.Split(',');
                            int sent_nick_access = ircbot.get_user_access(new_line[0].TrimStart(':'), line[2]);

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
                                    string target_host = ircbot.get_user_host(new_line[0]);
                                    string ban = "*!*@" + target_host;
                                    if (target_host.Equals("was"))
                                    {
                                        ban = nick + "!*@*";
                                    }
                                    if (new_line.GetUpperBound(0) > 0)
                                    {
                                        ircbot.sendData("MODE", line[2] + " +b " + ban + " :" + new_line[1]);
                                        ircbot.sendData("KICK", line[2] + " " + new_line[0] + " :" + new_line[1]);
                                    }
                                    else
                                    {
                                        ircbot.sendData("MODE", line[2] + " +b " + ban + " :No Reason");
                                        ircbot.sendData("KICK", line[2] + " " + new_line[0] + " :No Reason");
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 3, StringSplitOptions.RemoveEmptyEntries);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                int time = Convert.ToInt32(new_line[0].TrimStart(':'));
                                string nicks = new_line[1];
                                string target_host = ircbot.get_user_host(new_line[1]);
                                string[] total_nicks = nicks.Split(',');
                                int sent_nick_access = ircbot.get_user_access(new_line[1], line[2]);

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
                                        if (target_host.Equals("was"))
                                        {
                                            ban = nick + "!*@*";
                                        }
                                        if (new_line.GetUpperBound(0) > 1)
                                        {
                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :" + new_line[2]);
                                        }
                                        else
                                        {
                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :No Reason");
                                        }

                                        unban_trigger.Interval = (Convert.ToInt32(new_line[0]) * 1000);
                                        unban_trigger.Enabled = true;
                                        unban_trigger.AutoReset = false;
                                        main = ircbot;
                                        unban_nick_string = new_line[1];
                                        unban_host_string = target_host;
                                        unban_channel_string = line[2];
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 3, StringSplitOptions.RemoveEmptyEntries);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                int time = Convert.ToInt32(new_line[0].TrimStart(':'));
                                string nicks = new_line[1];
                                string target_host = ircbot.get_user_host(new_line[1]);
                                string[] total_nicks = nicks.Split(',');
                                int sent_nick_access = ircbot.get_user_access(new_line[1], line[2]);

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
                                        if (target_host.Equals("was"))
                                        {
                                            ban = nick + "!*@*";
                                        }
                                        if (new_line.GetUpperBound(0) > 1)
                                        {
                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :" + new_line[2]);
                                            ircbot.sendData("KICK", line[2] + " " + new_line[1] + " :" + new_line[2]);
                                        }
                                        else
                                        {
                                            ircbot.sendData("MODE", line[2] + " +b " + ban + " :No Reason");
                                            ircbot.sendData("KICK", line[2] + " " + new_line[1] + " :No Reason");
                                        }

                                        unban_trigger.Interval = (Convert.ToInt32(new_line[0]) * 1000);
                                        unban_trigger.Enabled = true;
                                        unban_trigger.AutoReset = false;
                                        main = ircbot;
                                        unban_nick_string = new_line[1];
                                        unban_host_string = target_host;
                                        unban_channel_string = line[2];
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            string nicks = new_line[0].TrimStart(':');
                            string[] total_nicks = nicks.Split(',');
                            int sent_nick_access = ircbot.get_user_access(new_line[0].TrimStart(':'), line[2]);
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
                                    string target_host = ircbot.get_user_host(new_line[0]);
                                    if (new_line.GetUpperBound(0) > 0)
                                    {
                                        add_auto(new_line[0], line[2], target_host, "k", new_line[1], ircbot);
                                    }
                                    else
                                    {
                                        add_auto(new_line[0], line[2], target_host, "k", "No Reason", ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            string target_host = ircbot.get_user_host(new_line[0]);
                            string nicks = new_line[0].TrimStart(':');
                            string[] total_nicks = nicks.Split(',');
                            int sent_nick_access = ircbot.get_user_access(new_line[0].TrimStart(':'), line[2]);
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
                                        add_auto(new_line[0], line[2], target_host, "b", new_line[1], ircbot);
                                    }
                                    else
                                    {
                                        add_auto(new_line[0], line[2], target_host, "b", "No Reason", ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            string target_host = ircbot.get_user_host(new_line[0]);
                            string nicks = new_line[0].TrimStart(':');
                            string[] total_nicks = nicks.Split(',');
                            int sent_nick_access = ircbot.get_user_access(new_line[0].TrimStart(':'), line[2]);
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
                                        add_auto(new_line[0], line[2], target_host, "kb", new_line[1], ircbot);
                                    }
                                    else
                                    {
                                        add_auto(new_line[0], line[2], target_host, "kb", "No Reason", ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string target_host = ircbot.get_user_host(line[4]);
                            del_auto(line[4], line[2], target_host, "k", ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string target_host = ircbot.get_user_host(line[4]);
                            del_auto(line[4], line[2], target_host, "b", ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string target_host = ircbot.get_user_host(line[4]);
                            del_auto(line[4], line[2], target_host, "kb", ircbot);
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
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            string[] new_line = line[4].Split(charS, 2);
                            string nicks = new_line[0].TrimStart(':');
                            string[] total_nicks = nicks.Split(',');
                            int sent_nick_access = ircbot.get_user_access(new_line[0].TrimStart(':'), line[2]);
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
                                        ircbot.sendData("KICK", line[2] + " " + new_line[0] + " :" + new_line[1]);
                                    }
                                    else
                                    {
                                        ircbot.sendData("KICK", line[2] + " " + new_line[0] + " :No Reason");
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
                    ircbot.spam_count++;
                    if (nick_access >= ircbot.get_command_access(command))
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

        public void check_auto(string nick, string channel, string hostname, bot ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\auto_kb\\" + ircbot.server_name + "_list.txt";
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
                            if (hostname.Equals("was"))
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

        public void unban_nick(object sender, EventArgs e)
        {
            unban_trigger.Enabled = false;
            string ban = "*!*@" + unban_host_string;
            if (unban_host_string.Equals("was"))
            {
                ban = unban_nick_string + "!*@*";
            }
            main.sendData("MODE", unban_channel_string + " -b " + ban);
        }

        private void add_auto(string nick, string channel, string hostname, string type, string reason, bot ircbot)
        {
            string list_file = ircbot.cur_dir + "\\modules\\auto_kb\\" + ircbot.server_name + "_list.txt";
            string add_line = nick + "*" + hostname + "*" + channel + "*" + type + "*" + reason + "*" + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt");
            bool found_nick = false;
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 2];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] auto_nick = file_line.Split(charSeparator, 5);
                    if (nick.Equals(auto_nick[0]) && hostname.Equals(auto_nick[1]) && channel.Equals(auto_nick[2]) && type.Equals(auto_nick[3]))
                    {
                        new_file[counter] = add_line;
                        found_nick = true;
                    }
                    else
                    {
                        new_file[counter] = file_line;
                    }
                    counter++;
                }
                if (found_nick == false)
                {
                    new_file[counter] = add_line;
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
                string ban = "*!*@" + hostname;
                if (hostname.Equals("was"))
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
            string list_file = ircbot.cur_dir + "\\modules\\auto_kb\\" + ircbot.server_name + "_list.txt";
            bool found_nick = false;
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 2];
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
                        new_file[counter] = file_line;
                    }
                    counter++;
                }
                if (found_nick == false)
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + nick + " is not in the a" + type + " list.");
                }
                else
                {
                    System.IO.File.WriteAllLines(@list_file, new_file);
                    string ban = "*!*@" + hostname;
                    if (hostname.Equals("was"))
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
