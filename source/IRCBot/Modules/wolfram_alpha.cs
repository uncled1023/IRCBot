using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml.Linq;
using System.Xml;

namespace IRCBot.Modules
{
    class wolfram_alpha : Module
    {
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
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
                                    case "wa":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Get Urban Dictionary Info
                                                get_wa(line[4], line[2], ircbot, conf, module_id);
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

        private void get_wa(string search, string channel, bot ircbot, IRCConfig conf, int conf_id)
        {
            string URL = "http://api.wolframalpha.com/v2/query?input=" + System.Web.HttpUtility.UrlEncode(search) + "&appid=" + conf.module_config[conf_id][3] + "&format=plaintext";
            XmlNodeList xnList = null;
            try
            {
                string results = new WebClient().DownloadString(URL);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(results);
                xnList = xmlDoc.SelectNodes("/queryresult/pod");
            }
            catch
            {
                ircbot.sendData("PRIVMSG", channel + " :Could not fetch results");
            }
            if (xnList.Count > 1)
            {
                ircbot.sendData("PRIVMSG", channel + " :Result for: " + xnList[0]["subpod"]["plaintext"].InnerText);
                ircbot.sendData("PRIVMSG", channel + " :" + xnList[1]["subpod"]["plaintext"].InnerText);
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :No Results Found.");
            }
        }
    }
}
