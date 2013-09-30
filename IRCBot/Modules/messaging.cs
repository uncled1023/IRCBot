using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IRCBot.Modules
{
    class messaging : Module
    {
        public override void control(bot ircbot, ref BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
            if (type.Equals("channel") || type.Equals("query") && bot_command == true)
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
                                    case "message":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            if (type.Equals("channel"))
                                            {
                                                add_message(nick, line, line[2], ircbot, module_id);
                                            }
                                            else
                                            {
                                                add_message(nick, line, null, ircbot, module_id);
                                            }
                                        }
                                        else
                                        {
                                            if (type.Equals("channel"))
                                            {
                                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "anonmessage":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            if (type.Equals("channel"))
                                            {
                                                add_anonmessage(nick, line, line[2], ircbot, module_id);
                                            }
                                            else
                                            {
                                                add_anonmessage(nick, line, null, ircbot, module_id);
                                            }
                                        }
                                        else
                                        {
                                            if (type.Equals("channel"))
                                            {
                                                ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            if (type.Equals("channel") || type.Equals("query") || type.Equals("join") || type.Equals("mode"))
            {
                find_message(nick, ircbot);
            }
        }

        private void add_message(string nick, string[] line, string channel, bot ircbot, int module_id)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "messaging" + Path.DirectorySeparatorChar + ircbot.conf.server + "_messages.txt";
            char[] charS = new char[] { ' ' };
            string[] tmp = line[4].Split(charS, 2);
            string to_nick = tmp[0];
            string add_line = nick + "*Reg*" + to_nick + "*" + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt") + "*";
            bool added_nick = false;
            if (tmp.GetUpperBound(0) >= 1)
            {
                add_line += tmp[1];
                if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "messaging"))
                {
                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "messaging");
                }
                if (File.Exists(list_file))
                {
                    string[] old_file = System.IO.File.ReadAllLines(list_file);
                    List<string> new_file = new List<string>();
                    int num_msg = 0;
                    foreach (string file_line in old_file)
                    {
                        char[] charSeparator = new char[] { '*' };
                        string[] intro_nick = file_line.Split(charSeparator, 4);
                        if (nick.Equals(intro_nick[0], StringComparison.InvariantCultureIgnoreCase) && to_nick.Equals(intro_nick[2], StringComparison.InvariantCultureIgnoreCase))
                        {
                            num_msg++;
                        }
                        new_file.Add(file_line);
                    }
                    if (Convert.ToInt32(ircbot.conf.module_config[module_id][3]) > num_msg)
                    {
                        new_file.Add(add_line);
                        added_nick = true;
                    }
                    System.IO.File.WriteAllLines(@list_file, new_file);
                }
                else
                {
                    System.IO.File.WriteAllText(@list_file, add_line);
                    added_nick = true;
                }
                if (added_nick)
                {
                    ircbot.sendData("NOTICE", nick + " :I will send your message as soon as I can.");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :You have reached the maximum number of messages you are able to send to " + to_nick + ".  Please try again after they have read them.");
                }
            }
        }

        private void add_anonmessage(string nick, string[] line, string channel, bot ircbot, int module_id)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "messaging" + Path.DirectorySeparatorChar + ircbot.conf.server + "_messages.txt";
            char[] charS = new char[] { ' ' };
            string[] tmp = line[4].Split(charS, 2);
            string to_nick = tmp[0];
            string add_line = nick + "*Anon*" + to_nick + "*" + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt") + "*";
            bool added_nick = false;
            if (tmp.GetUpperBound(0) >= 1)
            {
                add_line += tmp[1];
                if (!Directory.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "messaging"))
                {
                    Directory.CreateDirectory(ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "messaging");
                }
                if (File.Exists(list_file))
                {
                    string[] old_file = System.IO.File.ReadAllLines(list_file);
                    List<string> new_file = new List<string>();
                    int num_msg = 0;
                    foreach (string file_line in old_file)
                    {
                        char[] charSeparator = new char[] { '*' };
                        string[] intro_nick = file_line.Split(charSeparator, 4);
                        if (nick.Equals(intro_nick[0], StringComparison.InvariantCultureIgnoreCase) && to_nick.Equals(intro_nick[1], StringComparison.InvariantCultureIgnoreCase))
                        {
                            num_msg++;
                        }
                        new_file.Add(file_line);
                    }
                    if (Convert.ToInt32(ircbot.conf.module_config[module_id][3]) > num_msg)
                    {
                        new_file.Add(add_line);
                        added_nick = true;
                    }
                    System.IO.File.WriteAllLines(@list_file, new_file);
                }
                else
                {
                    System.IO.File.WriteAllText(@list_file, add_line);
                    added_nick = true;
                }
                if (added_nick)
                {
                    ircbot.sendData("NOTICE", nick + " :I will send your message as soon as I can.");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :You have reached the maximum number of messages you are able to send to " + to_nick + ".  Please try again after they have read them.");
                }
            }
        }

        public void find_message(string nick, bot ircbot)
        {
            string list_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "messaging" + Path.DirectorySeparatorChar + ircbot.conf.server + "_messages.txt";
            if (File.Exists(list_file))
            {
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                List<string> new_file = new List<string>();
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] intro_nick = file_line.Split(charSeparator, 5);
                    if (intro_nick.GetUpperBound(0) > 3)
                    {
                        if (nick.Equals(intro_nick[2], StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (intro_nick[1].Equals("Reg"))
                            {
                                ircbot.sendData("PRIVMSG", nick + " :" + intro_nick[0] + " has left you a message on: " + intro_nick[3]);
                                ircbot.sendData("PRIVMSG", nick + " :\"" + intro_nick[4] + "\"");
                                ircbot.sendData("PRIVMSG", nick + " :If you would like to reply to them, please type .message " + intro_nick[0] + " <your_message>");
                            }
                            else if (intro_nick[1].Equals("Anon"))
                            {
                                ircbot.sendData("PRIVMSG", nick + " :" + "An anonymous sender has left you a message on: " + intro_nick[3]);
                                ircbot.sendData("PRIVMSG", nick + " :\"" + intro_nick[4] + "\"");
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                            new_file.Add(file_line);
                        }
                    }
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
                // Read the file and display it line by line.
            }
        }
    }
}
