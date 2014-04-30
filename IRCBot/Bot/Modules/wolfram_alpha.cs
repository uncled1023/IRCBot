using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml.Linq;
using System.Xml;

namespace Bot.Modules
{
    class wolfram_alpha : Module
    {
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == true)
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
                                case "wa":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            // Get Urban Dictionary Info
                                            get_wa(line[4], line[2], ircbot, Conf);
                                        }
                                        else
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :" + nick + ", you need to include more info.");
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

        private void get_wa(string search, string channel, bot ircbot, BotConfig Conf)
        {
            string URL = "http://api.wolframalpha.com/v2/query?input=" + System.Web.HttpUtility.UrlEncode(search) + "&appid=" + this.Options["API"] + "&format=plaintext";
            XmlNodeList xnList = null;
            try
            {
                WebClient web = new WebClient();
                web.Encoding = Encoding.UTF8;
                string results = web.DownloadString(URL);
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
