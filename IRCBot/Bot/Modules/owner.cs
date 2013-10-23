using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace Bot.Modules
{
    class owner : Module
    {
        System.Timers.Timer invalid_pass_timeout = new System.Timers.Timer();
        public override void control(bot ircbot, BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            char[] charS = new char[] { ' ' };
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
                        bool spam_check = ircbot.get_spam_check(channel, nick, Convert.ToBoolean(tmp_command[8]));
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
                                    case "owner":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                if (line[4].Equals(conf.pass))
                                                {
                                                    add_owner(nick, ircbot, conf);
                                                    ircbot.sendData("NOTICE", nick + " :You are now identified as an owner!");
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :Invalid Password");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "addowner":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                add_owner(line[4], ircbot, conf);
                                                ircbot.sendData("NOTICE", nick + " :" + line[4] + " has been added as an owner.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delowner":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                del_owner(line[4], ircbot, conf);
                                                ircbot.sendData("NOTICE", nick + " :" + line[4] + " has been removed as an owner.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :You need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "nick":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ircbot.sendData("NICK", line[4]);
                                                ircbot.nick = line[4];
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "id":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            ircbot.sendData("PRIVMSG", "NickServ :Identify " + conf.pass);
                                            ircbot.sendData("NOTICE", nick + " :I have identified.");
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "join":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] chan_list = line[4].Replace(' ', ',').Split(',');
                                                foreach (string chan in chan_list)
                                                {
                                                    ircbot.sendData("JOIN", chan);
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "part":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] channels = line[4].Split(' ');
                                                bool chan_found = false;
                                                foreach (string tmp_chan in channels)
                                                {
                                                    bool part_chan = false;
                                                    int index = 0;
                                                    foreach (string chan in ircbot.channel_list)
                                                    {
                                                        if (chan.Equals(tmp_chan))
                                                        {
                                                            part_chan = true;
                                                            chan_found = true;
                                                            break;
                                                        }
                                                        index++;
                                                    }
                                                    if (part_chan == true)
                                                    {
                                                        ircbot.sendData("PART", tmp_chan);
                                                        ircbot.nick_list.RemoveAt(index);
                                                        ircbot.channel_list.RemoveAt(index);
                                                    }
                                                }
                                                if (chan_found == false)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :I am not in that channel.");
                                                }
                                            }
                                            else if (type.Equals("channel"))
                                            {
                                                bool part_chan = false;
                                                int index = 0;
                                                foreach (string chan in ircbot.channel_list)
                                                {
                                                    if (chan.Equals(channel))
                                                    {
                                                        part_chan = true;
                                                        break;
                                                    }
                                                    index++;
                                                }
                                                if (part_chan == true)
                                                {
                                                    ircbot.sendData("PART", channel);
                                                    ircbot.nick_list.RemoveAt(index);
                                                    ircbot.channel_list.RemoveAt(index);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :I am not in that channel.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "say":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                if (new_line[0].StartsWith("#") == true && new_line.GetUpperBound(0) > 0)
                                                {
                                                    ircbot.sendData("PRIVMSG", new_line[0] + " :" + new_line[1]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :" + line[4]);
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "action":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                if (new_line[0].StartsWith("#") == true && new_line.GetUpperBound(0) > 0)
                                                {
                                                    ircbot.sendData("PRIVMSG", new_line[0] + " :\u0001ACTION " + new_line[1] + "\u0001");
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :\u0001ACTION " + line[4] + "\u0001");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "query":
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
                                                    ircbot.sendData("PRIVMSG", new_line[0] + " :" + new_line[1]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "quit":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) <= 3)
                                            {
                                                ircbot.sendData("QUIT", "Leaving");
                                            }
                                            else
                                            {
                                                ircbot.sendData("QUIT", ":" + line[4]); //if the command is quit, send the QUIT command to the server with a quit message
                                            }
                                            ircbot.worker.CancelAsync();
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "quitall":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            foreach (var bot in ircbot.controller.bot_instances)
                                            {
                                                if (line.GetUpperBound(0) <= 3)
                                                {
                                                    bot.sendData("QUIT", "Leaving");
                                                }
                                                else
                                                {
                                                    bot.sendData("QUIT", ":" + line[4]);
                                                }
                                                bot.worker.CancelAsync();
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "cycle":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            ircbot.restart = true;
                                            ircbot.worker.CancelAsync();
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "cycleall":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            foreach (var bot in ircbot.controller.bot_instances)
                                            {
                                                bot.restart = true;
                                                bot.worker.CancelAsync();
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "exit":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            //ircbot.controller.Exit();
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "restart":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            System.Diagnostics.Process.Start(Assembly.GetEntryAssembly().Location); // to start new instance of application
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ignore":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                ignore(line[4], nick, ircbot, conf);
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "unignore":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                unignore(line[4], nick, ircbot, conf);
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ignorecmd":
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
                                                    ignorecmd(new_line[0], new_line[1], nick, ircbot, conf);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "unignorecmd":
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
                                                    unignorecmd(new_line[0], new_line[1], nick, ircbot, conf);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ignoremodule":
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
                                                    ignoremodule(new_line[0], new_line[1], nick, ircbot, conf);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "unignoremodule":
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
                                                    unignoremodule(new_line[0], new_line[1], nick, ircbot, conf);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "blacklist":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                add_blacklist(line[4], nick, ircbot, conf);
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "unblacklist":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                unblacklist(line[4], nick, ircbot, conf);
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "update":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            ircbot.controller.update_conf();
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "modules":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            string msg = "";
                                            foreach (Module module in ircbot.module_list)
                                            {
                                                msg += ", " + module.ToString().Remove(0, 15);
                                            }
                                            if (msg != "")
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + msg.TrimStart(',').Trim());
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :No Modules are loaded.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "loadmodule":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                bool module_found = ircbot.load_module(line[4]);
                                                if (module_found == true)
                                                {
                                                    if (type.Equals("channel"))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :Module Loaded Successfully");
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :Module Loaded Successfully");
                                                    }
                                                }
                                                else
                                                {
                                                    if (type.Equals("channel"))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :Error loading Module");
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :Error loading Module");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (type.Equals("channel"))
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you need to include more info.");
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delmodule":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {

                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                bool module_found = ircbot.unload_module(line[4]);
                                                if (module_found == true)
                                                {
                                                    if (type.Equals("channel"))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :Module Unloaded Successfully");
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :Module Unloaded Successfully");
                                                    }
                                                }
                                                else
                                                {
                                                    if (type.Equals("channel"))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :No Module found");
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :No Module found");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (type.Equals("channel"))
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you need to include more info.");
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "addchan":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                bool in_chan = false;
                                                bool chan_allowed = true;
                                                foreach (string chan in ircbot.conf.chan_blacklist.Split(','))
                                                {
                                                    if (chan.Equals(line[4]))
                                                    {
                                                        chan_allowed = false;
                                                        break;
                                                    }
                                                }
                                                if (chan_allowed == true)
                                                {
                                                    foreach (string in_channel in ircbot.channel_list)
                                                    {
                                                        if (line[4].Equals(in_channel))
                                                        {
                                                            ircbot.sendData("NOTICE", nick + " :I'm already in that channel!");
                                                            in_chan = true;
                                                            break;
                                                        }
                                                    }
                                                    if (in_chan == false)
                                                    {
                                                        if (nick_access != conf.owner_level)
                                                        {
                                                            string[] owners = conf.owner.Split(',');
                                                            foreach (string owner_nick in owners)
                                                            {
                                                                ircbot.sendData("NOTICE", owner_nick + " :" + nick + " has invited me to join " + line[4]);
                                                                ircbot.sendData("NOTICE", owner_nick + " :If you would like to permanently add this channel, please type " + ircbot.conf.command + "addchanlist " + line[4]);
                                                            }
                                                        }
                                                        ircbot.sendData("JOIN", line[4]);
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :I am not allowed to join that channel.");
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "addchanlist":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                bool in_chan = false;
                                                foreach (string in_channel in ircbot.channel_list)
                                                {
                                                    if (line[4].Equals(in_channel))
                                                    {
                                                        in_chan = true;
                                                        break;
                                                    }
                                                }
                                                if (in_chan == false)
                                                {
                                                    ircbot.sendData("JOIN", line[4]);
                                                }
                                                add_channel_list(line[4], ircbot, conf);
                                                ircbot.sendData("NOTICE", nick + " :" + line[4] + " successfully added to auto-join list.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delchanlist":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                del_channel_list(line[4], ircbot, conf);
                                                ircbot.sendData("NOTICE", nick + " :" + line[4] + " successfully removed from auto-join list.");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "channels":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool chan_found = false;
                                            string chan_list = "";
                                            foreach (string in_channel in ircbot.channel_list)
                                            {
                                                if (conf.module_config[module_id][3].Equals("False") || nick_access == conf.owner_level) // if "hide 
                                                {
                                                    chan_list += in_channel + ", ";
                                                    chan_found = true;
                                                }
                                                else
                                                {
                                                    foreach (List<string> chan_types in ircbot.nick_list)
                                                    {
                                                        if (chan_types[0].Equals(in_channel))
                                                        {
                                                            if (chan_types[1].Equals("="))
                                                            {
                                                                chan_list += in_channel + ", ";
                                                                chan_found = true;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            if (chan_found == true)
                                            {
                                                if (type.Equals("channel"))
                                                {
                                                    if (nick_access == conf.owner_level)
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :I am currently in the following channels: " + chan_list.Trim().TrimEnd(','));
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :I am currently in the following channels: " + chan_list.Trim().TrimEnd(','));
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :I am currently in the following channels: " + chan_list.Trim().TrimEnd(','));
                                                }
                                            }
                                            else
                                            {
                                                if (type.Equals("channel"))
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :I'm in no channels.");
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :I'm in no channels.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "servers":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            string server_list = "";
                                            foreach (bot bot in ircbot.controller.bot_instances)
                                            {
                                                if (bot.connected)
                                                {
                                                    server_list += bot.conf.server_address + ", ";
                                                }
                                            }
                                            if (type.Equals("channel"))
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :I am currently in the following servers: " + server_list.Trim().TrimEnd(','));
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :I am currently in the following servers: " + server_list.Trim().TrimEnd(','));
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "conf":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                BotConfig myObjectType = new BotConfig();
                                                var fields = myObjectType.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                                                switch (line[4])
                                                {
                                                    case "module_config":
                                                        foreach (System.Reflection.FieldInfo info in fields)
                                                        {
                                                            if (info.Name.Replace("k__BackingField", "").Equals("<module_config>"))
                                                            {
                                                                List<List<string>> tmp_list = (List<List<string>>)info.GetValue(conf);
                                                                int index = 0;
                                                                foreach (List<string> list in tmp_list)
                                                                {
                                                                    string msg = "";
                                                                    msg += "Class: " + list[0] + " | ";
                                                                    msg += "Name: " + list[1] + " | ";
                                                                    msg += "Blacklist: " + list[2] + " | ";
                                                                    for (int x = 3; x < list.Count(); x++)
                                                                    {
                                                                        msg += list[x] + ", ";
                                                                    }
                                                                    ircbot.sendData("NOTICE", nick + " :" + msg.Trim().TrimEnd('|').TrimEnd(',').Trim());
                                                                    index++;
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        ircbot.sendData("NOTICE", nick + " :" + nick + ", I do not understand your request.");
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                BotConfig myObjectType = new BotConfig();
                                                var fields = myObjectType.GetType().GetFields(BindingFlags.NonPublic| BindingFlags.Public | BindingFlags.Instance);
                                                foreach (var info in fields)
                                                {
                                                    if(info.GetValue(conf) == null)
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": NULL");
                                                    }
                                                    else if (info.GetValue(conf).ToString().Equals("System.Net.IPAddress[]"))
                                                    {
                                                        System.Net.IPAddress[] tmp_list = (System.Net.IPAddress[])info.GetValue(conf);
                                                        int index = 0;
                                                        foreach (System.Net.IPAddress list in tmp_list)
                                                        {
                                                            ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + "[" + index.ToString() + "]: " + list.ToString());
                                                            index++;
                                                        }
                                                    }
                                                    else if (info.Name.Replace("k__BackingField", "").Equals("<command_list>"))
                                                    {
                                                        List<List<string>> tmp_list = (List<List<string>>)info.GetValue(conf);
                                                        ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": " + tmp_list.Count().ToString());
                                                    }
                                                    else if (info.Name.Replace("k__BackingField", "").Equals("<module_config>"))
                                                    {
                                                        List<List<string>> tmp_list = (List<List<string>>)info.GetValue(conf);
                                                        ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": " + tmp_list.Count().ToString());
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": " + info.GetValue(conf).ToString());
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "resources":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                                            float totalBytesOfMemoryUsed = (currentProcess.WorkingSet64 / 1024f) / 1024f;
                                            PerformanceCounter pc1 = new PerformanceCounter();
                                            pc1.CategoryName = "Processor";
                                            pc1.CounterName = "% Processor Time";
                                            pc1.InstanceName = "_Total";
                                            pc1.NextValue();
                                            Thread.Sleep(1000);
                                            float totalCPUUsage = pc1.NextValue();
                                            if (type.Equals("channel"))
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :CPU: " + totalCPUUsage + "%");
                                                ircbot.sendData("PRIVMSG", channel + " :RAM: " + totalBytesOfMemoryUsed + "MB");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :CPU: " + totalCPUUsage + "%");
                                                ircbot.sendData("NOTICE", nick + " :RAM: " + totalBytesOfMemoryUsed + "MB");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "clear":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (type.Equals("channel"))
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    foreach (List<string> tmp_nick in ircbot.nick_list)
                                                    {
                                                        if (tmp_nick[0].Equals(line[4]))
                                                        {
                                                            for (int i = 1; i < tmp_nick.Count(); i++)
                                                            {
                                                                string[] split = tmp_nick[i].Split(':');
                                                                if (split.GetUpperBound(0) > 0)
                                                                {
                                                                    if (split[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase) || split[1].Equals(ircbot.nick, StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                    }
                                                                    else
                                                                    {
                                                                        ircbot.sendData("KICK", line[4] + " :" + split[1]);
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    foreach (List<string> tmp_nick in ircbot.nick_list)
                                                    {
                                                        if (tmp_nick[0].Equals(channel))
                                                        {
                                                            for (int i = 1; i < tmp_nick.Count(); i++)
                                                            {
                                                                string[] split = tmp_nick[i].Split(':');
                                                                if (split.GetUpperBound(0) > 0)
                                                                {
                                                                    if (!split[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase) && !split[1].Equals(ircbot.nick, StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                        ircbot.sendData("KICK", channel + " :" + split[1]);
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    foreach (List<string> tmp_nick in ircbot.nick_list)
                                                    {
                                                        if (tmp_nick[0].Equals(line[4]))
                                                        {
                                                            for (int i = 1; i < tmp_nick.Count(); i++)
                                                            {
                                                                string[] split = tmp_nick[i].Split(':');
                                                                if (split.GetUpperBound(0) > 0)
                                                                {
                                                                    if (split[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase) || split[1].Equals(ircbot.nick, StringComparison.InvariantCultureIgnoreCase))
                                                                    {
                                                                    }
                                                                    else
                                                                    {
                                                                        ircbot.sendData("KICK", line[4] + " :" + split[1]);
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + nick + ", you need to include more info.");
                                                }
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

        private void add_blacklist(string channel, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            bool added = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string new_blacklist = xn["chan_blacklist"].InnerText + "," + channel;
                        xn["chan_blacklist"].InnerText = new_blacklist.TrimStart(',');
                        added = true;
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                if (added)
                {
                    conf.chan_blacklist += "," + channel;
                    conf.chan_blacklist = conf.chan_blacklist.TrimStart(',');
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + channel + " successfully added to the blacklist.");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :" + channel + " was not added to the blacklist.");
                }
            }
        }

        private void unblacklist(string channel, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            bool removed = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string new_blacklist = "";
                        conf.chan_blacklist = "";
                        foreach (string list_blacklist in xn["chan_blacklist"].InnerText.Split(','))
                        {
                            if (!list_blacklist.Equals(channel))
                            {
                                new_blacklist += list_blacklist + ",";
                                conf.ignore_list += list_blacklist + ",";
                            }
                            else
                            {
                                removed = true;
                            }
                        }
                        xn["chan_blacklist"].InnerText = new_blacklist.TrimEnd(',');
                        conf.chan_blacklist = conf.chan_blacklist.TrimEnd(',');
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                if (removed)
                {
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + channel + " successfully removed from the blacklist.");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :" + channel + " was not removed from the blacklist.");
                }
            }
        }

        private void ignorecmd(string cmd, string ignore_nick, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                bool cmd_found_file = false;
                bool cmd_found_conf = false;
                XmlNode Serverxn = xmlDoc.SelectSingleNode("/modules");
                XmlNodeList ServerxnList = Serverxn.ChildNodes;
                foreach (XmlNode xn in ServerxnList)
                {
                    XmlNodeList cmd_nodes = xn.SelectNodes("commands");
                    foreach (XmlNode cmd_node in cmd_nodes)
                    {
                        string[] triggers = cmd_node["triggers"].InnerText.Split('|');
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(cmd))
                            {
                                string old_ignore = cmd_node["blacklist"].InnerText;
                                string new_ignore = old_ignore + "," + ignore_nick;
                                cmd_node["blacklist"].InnerText = new_ignore.TrimStart(',');
                                cmd_found_file = true;
                                break;
                            }
                        }
                    }
                    break;
                }
                foreach (List<string> tmp_command in conf.command_list)
                {
                    string[] triggers = tmp_command[3].Split('|');
                    foreach (string trigger in triggers)
                    {
                        if (trigger.Equals(cmd))
                        {
                            tmp_command[6] += "," + ignore_nick;
                            cmd_found_conf = true;
                            break;
                        }
                    }
                }
                if (cmd_found_file && cmd_found_conf)
                {
                    xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " added successfully to the " + ircbot.conf.command + cmd + " ignore list!");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :" + ircbot.conf.command + cmd + " does not exist.");
                }
            }
        }

        private void unignorecmd(string cmd, string ignore_nick, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                bool cmd_found_file = false;
                bool cmd_found_conf = false;
                XmlNode Serverxn = xmlDoc.SelectSingleNode("/modules");
                XmlNodeList ServerxnList = Serverxn.ChildNodes;
                foreach (XmlNode xn in ServerxnList)
                {
                    XmlNodeList cmd_nodes = xn.SelectNodes("commands");
                    foreach (XmlNode cmd_node in cmd_nodes)
                    {
                        string[] triggers = cmd_node["triggers"].InnerText.Split('|');
                        foreach (string trigger in triggers)
                        {
                            if (trigger.Equals(cmd))
                            {
                                string new_ignore = "";
                                foreach (string list_ignore in cmd_node["blacklist"].InnerText.Split(','))
                                {
                                    if (!list_ignore.Equals(ignore_nick, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        new_ignore += list_ignore + ",";
                                    }
                                }
                                cmd_node["blacklist"].InnerText = new_ignore.TrimEnd(',');
                                cmd_found_file = true;
                                break;
                            }
                        }
                    }
                    break;
                }
                foreach (List<string> tmp_command in conf.command_list)
                {
                    string[] triggers = tmp_command[3].Split('|');
                    foreach (string trigger in triggers)
                    {
                        if (trigger.Equals(cmd))
                        {
                            string new_ignore = "";
                            foreach (string list_ignore in tmp_command[6].Split(','))
                            {
                                if (!list_ignore.Equals(ignore_nick, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    new_ignore += list_ignore + ",";
                                }
                            }
                            tmp_command[6] = new_ignore.TrimEnd(',');
                            cmd_found_conf = true;
                            break;
                        }
                    }
                }
                if (cmd_found_file && cmd_found_conf)
                {
                    xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " removed successfully from the " + ircbot.conf.command + cmd + " ignore list!");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :" + ircbot.conf.command + cmd + " does not exist.");
                }
            }
        }

        private void ignoremodule(string module, string ignore_nick, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                bool module_found_file = false;
                bool module_found_conf = false;
                XmlNode Serverxn = xmlDoc.SelectSingleNode("/modules");
                XmlNodeList ServerxnList = Serverxn.ChildNodes;
                foreach (XmlNode xn in ServerxnList)
                {
                    if (xn["class_name"].InnerText.Equals(module))
                    {
                        string old_ignore = xn["blacklist"].InnerText;
                        string new_ignore = old_ignore + "," + ignore_nick;
                        xn["blacklist"].InnerText = new_ignore.TrimStart(',');
                        module_found_file = true;
                        break;
                    }
                }
                foreach (List<string> conf_module in conf.module_config)
                {
                    if (module.ToString().Equals(conf_module[0]))
                    {
                        conf_module[2] += "," + ignore_nick;
                        module_found_conf = true;
                        break;
                    }
                }
                if (module_found_file && module_found_conf)
                {
                    xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " added successfully to the " + module + " module ignore list!");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :Module " + module + " does not exist.");
                }
            }
        }

        private void unignoremodule(string module, string ignore_nick, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                bool module_found_file = false;
                bool module_found_conf = false;
                XmlNode Serverxn = xmlDoc.SelectSingleNode("/modules");
                XmlNodeList ServerxnList = Serverxn.ChildNodes;
                foreach (XmlNode xn in ServerxnList)
                {
                    if (xn["class_name"].InnerText.Equals(module))
                    {
                        string new_ignore = "";
                        foreach (string list_ignore in xn["blacklist"].InnerText.Split(','))
                        {
                            if (!list_ignore.Equals(ignore_nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                new_ignore += list_ignore + ",";
                            }
                        }
                        xn["blacklist"].InnerText = new_ignore.TrimEnd(',');
                        module_found_file = true;
                        break;
                    }
                }
                foreach (List<string> conf_module in conf.module_config)
                {
                    if (module.ToString().Equals(conf_module[0]))
                    {
                        conf_module[2] += "," + ignore_nick;
                        module_found_conf = true;
                        break;
                    }
                }
                if (module_found_file && module_found_conf)
                {
                    xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + ircbot.conf.server + Path.DirectorySeparatorChar + "modules.xml");
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " removed successfully from the " + module + " module ignore list!");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :Module " + module + " does not exist.");
                }
            }
        }

        private void ignore(string ignore_nick, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            bool added = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string new_ignore = xn["ignore_list"].InnerText + "," + ignore_nick;
                        xn["ignore_list"].InnerText = new_ignore.TrimStart(',');
                        added = true;
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                if (added)
                {
                    conf.ignore_list += "," + nick;
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " successfully added to the ignore list!");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " was not added to the ignore list.");
                }
            }
        }

        private void unignore(string ignore_nick, string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            bool removed = false;
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string new_ignore = "";
                        conf.ignore_list = "";
                        foreach (string list_ignore in xn["ignore_list"].InnerText.Split(','))
                        {
                            if (!list_ignore.Equals(ignore_nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                new_ignore += list_ignore + ",";
                                conf.ignore_list += list_ignore + ",";
                            }
                            else
                            {
                                removed = true;
                            }
                        }
                        xn["ignore_list"].InnerText = new_ignore.TrimEnd(',');
                        conf.ignore_list = conf.ignore_list.TrimEnd(',');
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                if (removed)
                {
                    ircbot.controller.update_conf();
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " successfully removed from the ignore list!");
                }
                else
                {
                    ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " was not removed from the ignore list.");
                }
            }
        }

        private void add_channel_list(string channel, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string new_channel = xn["chan_list"].InnerText + "," + channel;
                        xn["chan_list"].InnerText = new_channel;
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                conf.chans += "," + channel;
            }
        }

        private void del_channel_list(string channel, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string new_channel = "";
                        conf.chans = "";
                        foreach (string list_chan in xn["chan_list"].InnerText.Split(','))
                        {
                            if (!list_chan.Equals(channel))
                            {
                                new_channel += list_chan + ",";
                                conf.chans += list_chan + ",";
                            }
                        }
                        xn["chan_list"].InnerText = new_channel.TrimEnd(',');
                        conf.chans = conf.chans.TrimEnd(',');
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            }
        }

        private void add_owner(string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string new_owner = xn["owner"].InnerText + "," + nick;
                        xn["owner"].InnerText = new_owner;
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                conf.owner += "," + nick;
            }
        }

        private void del_owner(string nick, bot ircbot, BotConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string new_owner = "";
            if (File.Exists(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(conf.server))
                    {
                        string[] new_owner_tmp = xn["owner"].InnerText.Split(',');
                        for (int x = 0; x <= new_owner_tmp.GetUpperBound(0); x++)
                        {
                            if (new_owner_tmp[x].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                            }
                            else
                            {
                                new_owner += new_owner_tmp[x] + ",";
                            }
                        }
                        xn["owner"].InnerText = new_owner.TrimEnd(',');
                        break;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                conf.owner = new_owner.TrimEnd(',');
            }
        }
    }
}
