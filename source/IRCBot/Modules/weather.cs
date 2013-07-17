using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace IRCBot.Modules
{
    class weather : Module
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
                                    case "weather":
                                        if (conf.module_config[module_id][3].Equals("True"))
                                        {
                                            if (spam_check == true)
                                            {
                                                lock (ircbot.spamlock)
                                                {
                                                    bool spam_added = false;
                                                    int index = 0;
                                                    foreach (spam_info spam in conf.spam_check)
                                                    {
                                                        if (spam.spam_channel.Equals(channel))
                                                        {
                                                            spam_added = true;
                                                            index++;
                                                            break;
                                                        }
                                                    }
                                                    if (spam_added)
                                                    {
                                                        conf.spam_check[index].spam_count++;
                                                    }
                                                    else
                                                    {
                                                        spam_info new_spam = new spam_info();
                                                        new_spam.spam_channel = channel;
                                                        new_spam.spam_activated = false;
                                                        new_spam.spam_count = 1;
                                                        conf.spam_check.Add(new_spam);
                                                    }
                                                }
                                            }
                                            if (nick_access >= command_access)
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    // Add introduction
                                                    get_weather(line[4], line[2], ircbot);
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
                                        }
                                        break;
                                    case "forecast":
                                        if (conf.module_config[module_id][4].Equals("True"))
                                        {
                                            if (spam_check == true)
                                            {
                                                lock (ircbot.spamlock)
                                                {
                                                    bool spam_added = false;
                                                    int index = 0;
                                                    foreach (spam_info spam in conf.spam_check)
                                                    {
                                                        if (spam.spam_channel.Equals(channel))
                                                        {
                                                            spam_added = true;
                                                            index++;
                                                            break;
                                                        }
                                                    }
                                                    if (spam_added)
                                                    {
                                                        conf.spam_check[index].spam_count++;
                                                    }
                                                    else
                                                    {
                                                        spam_info new_spam = new spam_info();
                                                        new_spam.spam_channel = channel;
                                                        new_spam.spam_activated = false;
                                                        new_spam.spam_count = 1;
                                                        conf.spam_check.Add(new_spam);
                                                    }
                                                }
                                            }
                                            if (nick_access >= command_access)
                                            {
                                                if (line.GetUpperBound(0) > 3)
                                                {
                                                    // Add introduction
                                                    get_forecast(line[4], line[2], ircbot, Convert.ToInt32(conf.module_config[module_id][5]));
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
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void get_forecast(string term, string channel, bot ircbot, int days)
        {
            if (days > 5)
            {
                days = 5;
            }
            if (days < 1)
            {
                days = 1;
            }
            XmlDocument doc2 = new XmlDocument();

            // Load data  
            doc2.Load("http://api.wunderground.com/auto/wui/geo/WXCurrentObXML/index.xml?query=" + term);

            // Get forecast with XPath  
            XmlNodeList nodes2 = doc2.SelectNodes("/current_observation");

            string location = "";
            if (nodes2.Count > 0)
            {
                foreach (XmlNode node2 in nodes2)
                {
                    XmlNodeList sub_node2 = doc2.SelectNodes("/current_observation/display_location");
                    foreach (XmlNode xn2 in sub_node2)
                    {
                        location = xn2["full"].InnerText;
                    }
                }
            }

            XmlDocument doc = new XmlDocument();

            // Load data  
            doc.Load("http://api.wunderground.com/auto/wui/geo/ForecastXML/index.xml?query=" + term);

            // Get forecast with XPath  
            XmlNodeList nodes = doc.SelectNodes("/forecast/simpleforecast");

            string weekday = "";
            string highf = "";
            string lowf = "";
            string highc = "";
            string lowc = "";
            string conditions = "";
            if (location != ", " && location != "")
            {
                if (nodes.Count > 0)
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + days + " day forecast for " + location);
                    int index = 0;
                    foreach (XmlNode node in nodes)
                    {
                        foreach (XmlNode sub_node in node)
                        {
                            if (index <= days)
                            {
                                weekday = sub_node["date"].SelectSingleNode("weekday").InnerText;
                                highf = sub_node["high"].SelectSingleNode("fahrenheit").InnerText;
                                highc = sub_node["high"].SelectSingleNode("celsius").InnerText;
                                lowf = sub_node["low"].SelectSingleNode("fahrenheit").InnerText;
                                lowc = sub_node["low"].SelectSingleNode("celsius").InnerText;
                                conditions = sub_node["conditions"].InnerText;
                                ircbot.sendData("PRIVMSG", channel + " :" + weekday + ": " + conditions + " with a high of " + highf + " F (" + highc + " C) and a low of " + lowf + " F (" + lowc + " C).");
                            }
                            index++;
                        }
                    }
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :No weather information available");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :No weather information available");
            }
        }

        private void get_weather(string term, string channel, bot ircbot)
        {
            XmlDocument doc = new XmlDocument();

            // Load data  
            doc.Load("http://api.wunderground.com/auto/wui/geo/WXCurrentObXML/index.xml?query=" + term);

            // Get forecast with XPath  
            XmlNodeList nodes = doc.SelectNodes("/current_observation");

            string location = "";
            string temp = "";
            string weather = "";
            string humidity = "";
            string wind = "";
            string wind_dir = "";
            string wind_mph = "";
            if (nodes.Count > 0)
            {
                foreach (XmlNode node in nodes)
                {
                    XmlNodeList sub_node = doc.SelectNodes("/current_observation/display_location");
                    foreach (XmlNode xn in sub_node)
                    {
                        location = xn["full"].InnerText;
                    }
                    temp = node["temperature_string"].InnerText;
                    weather = node["weather"].InnerText;
                    humidity = node["relative_humidity"].InnerText;
                    wind = node["wind_string"].InnerText;
                    wind_dir = node["wind_dir"].InnerText;
                    wind_mph = node["wind_mph"].InnerText;
                }
                if (location != ", ")
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + location + " is currently " + weather + " with a temperature of " + temp + ".  The humidity is " + humidity + " with winds blowing " + wind_dir + " at " + wind_mph + " mph.");
                }
                else
                {
                    ircbot.sendData("PRIVMSG", channel + " :No weather information available");
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :No weather information available");
            }
        }
    }
}
