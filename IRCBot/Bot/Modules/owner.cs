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
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            char[] charS = new char[] { ' ' };
            if (type.Equals("channel") && bot_command == true)
            {
                foreach (Command tmp_command in this.Commands)
                {
                    bool blocked = tmp_command.Blacklist.Contains(channel) || tmp_command.Blacklist.Contains(nick);
                    bool cmd_found = false;
                    bool spam_check = ircbot.get_spam_check(channel, nick, tmp_command.Spam_Check);
                    if (spam_check == true)
                    {
                        blocked = blocked || ircbot.get_spam_status(channel);
                    }
                    cmd_found = tmp_command.Triggers.Contains(command);
                    if (blocked == true && cmd_found == true)
                    {
                        ircbot.sendData("NOTICE", nick + " :I am currently too busy to process that.");
                    }
                    if (blocked == false && cmd_found == true)
                    {
                        foreach (string trigger in tmp_command.Triggers)
                        {
                            switch (trigger)
                            {
                                case "owner":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            if (line[4].Equals(Conf.Pass))
                                            {
                                                add_owner(nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            add_owner(line[4], ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            del_owner(line[4], ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            ircbot.sendData("NICK", line[4]);
                                            ircbot.Nick = line[4];
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        ircbot.sendData("PRIVMSG", "NickServ :Identify " + Conf.Pass);
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] channels = line[4].Split(' ');
                                            bool chan_found = false;
                                            foreach (string tmp_chan in channels)
                                            {
                                                Channel_Info chan_info = ircbot.get_chan_info(tmp_chan);
                                                if (chan_info != null)
                                                {
                                                    chan_found = true;
                                                    ircbot.sendData("PART", tmp_chan);
                                                    ircbot.del_chan_info(tmp_chan);
                                                }
                                            }
                                            if (chan_found == false)
                                            {
                                                ircbot.sendData("NOTICE", nick + " :I am not in that channel.");
                                            }
                                        }
                                        else if (type.Equals("channel"))
                                        {
                                            Channel_Info chan_info = ircbot.get_chan_info(channel);
                                            if (chan_info != null)
                                            {
                                                ircbot.sendData("PART", channel);
                                                ircbot.del_chan_info(channel);
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        Environment.Exit(0);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        System.Diagnostics.Process.Start(Assembly.GetEntryAssembly().Location); // to start new instance of application
                                        Environment.Exit(0);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            ignore(line[4], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            unignore(line[4], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] new_line = line[4].Split(charS, 2);
                                            if (new_line.GetUpperBound(0) > 0)
                                            {
                                                ignorecmd(new_line[0], new_line[1], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] new_line = line[4].Split(charS, 2);
                                            if (new_line.GetUpperBound(0) > 0)
                                            {
                                                unignorecmd(new_line[0], new_line[1], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] new_line = line[4].Split(charS, 2);
                                            if (new_line.GetUpperBound(0) > 0)
                                            {
                                                ignoremodule(new_line[0], new_line[1], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string[] new_line = line[4].Split(charS, 2);
                                            if (new_line.GetUpperBound(0) > 0)
                                            {
                                                unignoremodule(new_line[0], new_line[1], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            add_blacklist(line[4], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            unblacklist(line[4], nick, ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        string msg = "";
                                        foreach (Module module in ircbot.Conf.Modules)
                                        {
                                            msg += ", " + module.ToString().Remove(0, 12);
                                        }
                                        if (!String.IsNullOrEmpty(msg))
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            bool chan_allowed = true;
                                            foreach (string chan in ircbot.Conf.Chan_Blacklist.Split(','))
                                            {
                                                if (chan.Equals(line[4]))
                                                {
                                                    chan_allowed = false;
                                                    break;
                                                }
                                            }
                                            if (chan_allowed == true)
                                            {
                                                Channel_Info chan_info = ircbot.get_chan_info(line[4]);
                                                if (chan_info != null)
                                                {
                                                    if (nick_access != Conf.Owner_Level)
                                                    {
                                                        string[] owners = Conf.Owner.Split(',');
                                                        foreach (string owner_nick in owners)
                                                        {
                                                            ircbot.sendData("NOTICE", owner_nick + " :" + nick + " has invited me to join " + line[4]);
                                                            ircbot.sendData("NOTICE", owner_nick + " :If you would like to permanently add this channel, please type " + ircbot.Conf.Command + "addchanlist " + line[4]);
                                                        }
                                                    }
                                                    ircbot.sendData("JOIN", line[4]);
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :I'm already in that channel!");
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            Channel_Info chan_info = ircbot.get_chan_info(line[4]);
                                            if (chan_info != null)
                                            {
                                                ircbot.sendData("JOIN", line[4]);
                                            }
                                            add_channel_list(line[4], ircbot, Conf);
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            del_channel_list(line[4], ircbot, Conf);
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
                                case "nicklist":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (type.Equals("channel"))
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                bool nick_found = false;
                                                string nick_list = "";
                                                Channel_Info chan_info = ircbot.get_chan_info(line[4]);
                                                if (chan_info != null)
                                                {
                                                    foreach (Nick_Info nick_info in chan_info.Nicks)
                                                    {
                                                        nick_list += nick_info.Nick + ", ";
                                                        nick_found = true;
                                                    }
                                                }
                                                if (nick_found)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + line[4] + ": " + nick_list.Trim().TrimEnd(','));
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :No nicks in " + line[4]);
                                                }
                                            }
                                            else
                                            {
                                                bool nick_found = false;
                                                string nick_list = "";
                                                Channel_Info chan_info = ircbot.get_chan_info(channel);
                                                if (chan_info != null)
                                                {
                                                    foreach (Nick_Info nick_info in chan_info.Nicks)
                                                    {
                                                        nick_list += nick_info.Nick + ", ";
                                                        nick_found = true;
                                                    }
                                                }
                                                if (nick_found)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + channel + ": " + nick_list.Trim().TrimEnd(','));
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :No nicks in " + channel);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                bool nick_found = false;
                                                string nick_list = "";
                                                Channel_Info chan_info = ircbot.get_chan_info(line[4]);
                                                if (chan_info != null)
                                                {
                                                    foreach (Nick_Info nick_info in chan_info.Nicks)
                                                    {
                                                        nick_list += nick_info.Nick + ", ";
                                                        nick_found = true;
                                                    }
                                                }
                                                if (nick_found)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + line[4] + ": " + nick_list.Trim().TrimEnd(','));
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :No nicks in " + line[4]);
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
                                case "channels":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        bool chan_found = false;
                                        string chan_list = "";
                                        foreach (Channel_Info chan_info in ircbot.Conf.Channel_List)
                                        {
                                            if (!this.Options["hide_private"] || nick_access == Conf.Owner_Level) // if "hide on private/secret" is false
                                            {
                                                chan_list += chan_info.Channel + ", ";
                                                chan_found = true;
                                            }
                                            else
                                            {
                                                if (chan_info.Show || ircbot.get_nick_info(nick, chan_info.Channel) != null)
                                                {
                                                    chan_list += chan_info.Channel + ", ";
                                                    chan_found = true;
                                                }
                                            }
                                        }
                                        if (chan_found == true)
                                        {
                                            if (type.Equals("channel"))
                                            {
                                                if (nick_access == Conf.Owner_Level)
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        string server_list = "";
                                        foreach (bot bot in ircbot.controller.bot_instances)
                                        {
                                            if (bot.connected)
                                            {
                                                server_list += bot.Conf.Server_Address + ", ";
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
                                    if (nick_access >= tmp_command.Access)
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
                                                            List<List<string>> tmp_list = (List<List<string>>)info.GetValue(Conf);
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
                                            var fields = myObjectType.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                                            foreach (var info in fields)
                                            {
                                                if (info.GetValue(Conf) == null)
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": NULL");
                                                }
                                                else if (info.GetValue(Conf).ToString().Equals("System.Net.IPAddress[]"))
                                                {
                                                    System.Net.IPAddress[] tmp_list = (System.Net.IPAddress[])info.GetValue(Conf);
                                                    int index = 0;
                                                    foreach (System.Net.IPAddress list in tmp_list)
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + "[" + index.ToString() + "]: " + list.ToString());
                                                        index++;
                                                    }
                                                }
                                                else if (info.Name.Replace("k__BackingField", "").Equals("<command_list>"))
                                                {
                                                    List<List<string>> tmp_list = (List<List<string>>)info.GetValue(Conf);
                                                    ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": " + tmp_list.Count().ToString());
                                                }
                                                else if (info.Name.Replace("k__BackingField", "").Equals("<module_config>"))
                                                {
                                                    List<List<string>> tmp_list = (List<List<string>>)info.GetValue(Conf);
                                                    ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": " + tmp_list.Count().ToString());
                                                }
                                                else
                                                {
                                                    ircbot.sendData("NOTICE", nick + " :" + info.Name.Replace("k__BackingField", "") + ": " + info.GetValue(Conf).ToString());
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
                                    if (nick_access >= tmp_command.Access)
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
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (type.Equals("channel"))
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                Channel_Info chan_info = ircbot.get_chan_info(line[4]);
                                                if (chan_info != null)
                                                {
                                                    foreach (Nick_Info nick_info in chan_info.Nicks)
                                                    {
                                                        if (!nick_info.Nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase) && !nick_info.Nick.Equals(ircbot.Nick, StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            ircbot.sendData("KICK", line[4] + " :" + nick_info.Nick);
                                                            Thread.Sleep(100);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Channel_Info chan_info = ircbot.get_chan_info(channel);
                                                if (chan_info != null)
                                                {
                                                    foreach (Nick_Info nick_info in chan_info.Nicks)
                                                    {
                                                        if (!nick_info.Nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase) && !nick_info.Nick.Equals(ircbot.Nick, StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            ircbot.sendData("KICK", line[4] + " :" + nick_info.Nick);
                                                            Thread.Sleep(100);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                Channel_Info chan_info = ircbot.get_chan_info(line[4]);
                                                if (chan_info != null)
                                                {
                                                    foreach (Nick_Info nick_info in chan_info.Nicks)
                                                    {
                                                        if (!nick_info.Nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase) && !nick_info.Nick.Equals(ircbot.Nick, StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            ircbot.sendData("KICK", line[4] + " :" + nick_info.Nick);
                                                            Thread.Sleep(100);
                                                        }
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

        private static void add_blacklist(string channel, string nick, bot ircbot, BotConfig Conf)
        {
            XmlNode node = ircbot.controller.get_server_xml(Conf.Server_Name);
            string new_blacklist = node["chan_blacklist"].InnerText + "," + channel;
            node["chan_blacklist"].InnerText = new_blacklist.TrimStart(',');
            bool added = ircbot.controller.save_server_xml(Conf.Server_Name, node);
            if (added)
            {
                Conf.Chan_Blacklist += "," + channel;
                Conf.Chan_Blacklist = Conf.Chan_Blacklist.TrimStart(',');
                ircbot.controller.update_conf();
                ircbot.sendData("NOTICE", nick + " :" + channel + " successfully added to the blacklist.");
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :" + channel + " was not added to the blacklist.");
            }
        }

        private static void unblacklist(string channel, string nick, bot ircbot, BotConfig Conf)
        {
            XmlNode node = ircbot.controller.get_server_xml(Conf.Server_Name);
            string new_blacklist = "";
            Conf.Chan_Blacklist = "";
            bool removed = false;
            foreach (string list_blacklist in node["chan_blacklist"].InnerText.Split(','))
            {
                if (!list_blacklist.Equals(channel))
                {
                    new_blacklist += list_blacklist + ",";
                    Conf.Ignore_List += list_blacklist + ",";
                }
                else
                {
                    removed = true;
                }
            }
            node["chan_blacklist"].InnerText = new_blacklist.TrimEnd(',');
            Conf.Chan_Blacklist = Conf.Chan_Blacklist.TrimEnd(',');
            ircbot.controller.save_server_xml(Conf.Server_Name, node);
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

        private static void ignorecmd(string cmd, string ignore_nick, string nick, bot ircbot, BotConfig Conf)
        {
            XmlDocument xmlDoc = ircbot.controller.get_module_xml(Conf.Server_Name);            
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
            foreach (Module tmp_module in Conf.Modules)
            {
                foreach (Command tmp_command in tmp_module.Commands)
                {
                    if (tmp_command.Triggers.Contains(cmd))
                    {
                        tmp_command.Blacklist.Add(ignore_nick);
                        cmd_found_conf = true;
                        break;
                    }
                }
            }
            if (cmd_found_file && cmd_found_conf)
            {
                ircbot.controller.save_module_xml(Conf.Server_Name, xmlDoc);
                ircbot.controller.update_conf();
                ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " added successfully to the " + ircbot.Conf.Command + cmd + " ignore list!");
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :" + ircbot.Conf.Command + cmd + " does not exist.");
            }
        }

        private static void unignorecmd(string cmd, string ignore_nick, string nick, bot ircbot, BotConfig Conf)
        {
            XmlDocument xmlDoc = ircbot.controller.get_module_xml(Conf.Server_Name);   
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
            foreach (Module tmp_module in Conf.Modules)
            {
                foreach (Command tmp_command in tmp_module.Commands)
                {
                    if (tmp_command.Triggers.Contains(cmd))
                    {
                        tmp_command.Blacklist.Remove(ignore_nick);
                        cmd_found_conf = true;
                        break;
                    }
                }
            }
            if (cmd_found_file && cmd_found_conf)
            {
                ircbot.controller.save_module_xml(Conf.Server_Name, xmlDoc);
                ircbot.controller.update_conf();
                ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " removed successfully from the " + ircbot.Conf.Command + cmd + " ignore list!");
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :" + ircbot.Conf.Command + cmd + " does not exist.");
            }
        }

        private static void ignoremodule(string module, string ignore_nick, string nick, bot ircbot, BotConfig Conf)
        {
            XmlDocument xmlDoc = ircbot.controller.get_module_xml(Conf.Server_Name);
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
            foreach (Module tmp_module in Conf.Modules)
            {
                if (tmp_module.Class_Name.Equals(module))
                {
                    tmp_module.Blacklist.Add(ignore_nick);
                    module_found_conf = true;
                    break;
                }
            }
            if (module_found_file && module_found_conf)
            {
                ircbot.controller.save_module_xml(Conf.Server_Name, xmlDoc);
                ircbot.controller.update_conf();
                ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " added successfully to the " + module + " module ignore list!");
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :Module " + module + " does not exist.");
            }
        }

        private static void unignoremodule(string module, string ignore_nick, string nick, bot ircbot, BotConfig Conf)
        {
            XmlDocument xmlDoc = ircbot.controller.get_module_xml(Conf.Server_Name);   
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
            foreach (Module tmp_module in Conf.Modules)
            {
                if (tmp_module.Class_Name.Equals(module))
                {
                    tmp_module.Blacklist.Remove(ignore_nick);
                    module_found_conf = true;
                    break;
                }
            }
            if (module_found_file && module_found_conf)
            {
                ircbot.controller.save_module_xml(Conf.Server_Name, xmlDoc);
                ircbot.controller.update_conf();
                ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " removed successfully from the " + module + " module ignore list!");
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :Module " + module + " does not exist.");
            }
        }

        private static void ignore(string ignore_nick, string nick, bot ircbot, BotConfig Conf)
        {
            bool added = false;
            XmlNode xn = ircbot.controller.get_server_xml(Conf.Server_Name);
            string new_ignore = xn["ignore_list"].InnerText + "," + ignore_nick;
            xn["ignore_list"].InnerText = new_ignore.TrimStart(',');
            added = ircbot.controller.save_server_xml(Conf.Server_Name, xn);
            if (added)
            {
                Conf.Ignore_List += "," + nick;
                ircbot.controller.update_conf();
                ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " successfully added to the ignore list!");
            }
            else
            {
                ircbot.sendData("NOTICE", nick + " :" + ignore_nick + " was not added to the ignore list.");
            }
        }

        private static void unignore(string ignore_nick, string nick, bot ircbot, BotConfig Conf)
        {
            bool removed = false;
            XmlNode xn = ircbot.controller.get_server_xml(Conf.Server_Name);
            string new_ignore = "";
            Conf.Ignore_List = "";
            foreach (string list_ignore in xn["ignore_list"].InnerText.Split(','))
            {
                if (!list_ignore.Equals(ignore_nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    new_ignore += list_ignore + ",";
                    Conf.Ignore_List += list_ignore + ",";
                }
                else
                {
                    removed = true;
                }
            }
            xn["ignore_list"].InnerText = new_ignore.TrimEnd(',');
            Conf.Ignore_List = Conf.Ignore_List.TrimEnd(',');
            ircbot.controller.save_server_xml(Conf.Server_Name, xn);
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

        private static void add_channel_list(string channel, bot ircbot, BotConfig Conf)
        {
            XmlNode xn = ircbot.controller.get_server_xml(Conf.Server_Name);
            string new_channel = xn["chan_list"].InnerText + "," + channel;
            xn["chan_list"].InnerText = new_channel;
            Conf.Chans += "," + channel;
            ircbot.controller.save_server_xml(Conf.Server_Name, xn);
        }

        private static void del_channel_list(string channel, bot ircbot, BotConfig Conf)
        {
            XmlNode xn = ircbot.controller.get_server_xml(Conf.Server_Name);
            string new_channel = "";
            Conf.Chans = "";
            foreach (string list_chan in xn["chan_list"].InnerText.Split(','))
            {
                if (!list_chan.Equals(channel))
                {
                    new_channel += list_chan + ",";
                    Conf.Chans += list_chan + ",";
                }
            }
            xn["chan_list"].InnerText = new_channel.TrimEnd(',');
            Conf.Chans = Conf.Chans.TrimEnd(',');
            ircbot.controller.save_server_xml(Conf.Server_Name, xn);
        }

        private static void add_owner(string nick, bot ircbot, BotConfig Conf)
        {
            XmlNode xn = ircbot.controller.get_server_xml(Conf.Server_Name);
            string new_owner = xn["owner"].InnerText + "," + nick;
            xn["owner"].InnerText = new_owner;
            Conf.Owner += "," + nick;
            ircbot.controller.save_server_xml(Conf.Server_Name, xn);
        }

        private static void del_owner(string nick, bot ircbot, BotConfig Conf)
        {
            string new_owner = "";
            XmlNode xn = ircbot.controller.get_server_xml(Conf.Server_Name);
            string[] new_owner_tmp = xn["owner"].InnerText.Split(',');
            for (int x = 0; x <= new_owner_tmp.GetUpperBound(0); x++)
            {
                if (!new_owner_tmp[x].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    new_owner += new_owner_tmp[x] + ",";
                }
            }
            xn["owner"].InnerText = new_owner.TrimEnd(',');
            Conf.Owner = new_owner.TrimEnd(',');
            ircbot.controller.save_server_xml(Conf.Server_Name, xn);
        }
    }
}
