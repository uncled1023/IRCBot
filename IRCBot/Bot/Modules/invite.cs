using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Modules
{
    class invite : Module
    {
        public override void control(bot ircbot, BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if (type.Equals("line") && line.GetUpperBound(0) >= 3 && line[1].Equals("invite", StringComparison.InvariantCultureIgnoreCase) && command.Equals(string.Empty))
            {
                if (channel.Equals(ircbot.nick))
                {
                    bool in_chan = false;
                    bool chan_allowed = true;
                    string request_chan = line[3].TrimStart(':');
                    foreach (string chan in ircbot.conf.chan_blacklist.Split(','))
                    {
                        if (chan.Equals(request_chan, StringComparison.InvariantCultureIgnoreCase))
                        {
                            chan_allowed = false;
                            break;
                        }
                    }
                    if (chan_allowed == true)
                    {
                        foreach (string in_channel in ircbot.channel_list)
                        {
                            if (request_chan.Equals(in_channel, StringComparison.InvariantCultureIgnoreCase))
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
                                    ircbot.sendData("NOTICE", owner_nick + " :" + nick + " has invited me to join " + request_chan);
                                    ircbot.sendData("NOTICE", owner_nick + " :If you would like to permanently add this channel, please type " + ircbot.conf.command + "addchanlist " + request_chan);
                                }
                            }
                            ircbot.sendData("JOIN", request_chan);
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :I am not allowed to join that channel.");
                    }
                }
            }
        }
    }
}
