using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace Bot.Modules
{
    class weather : Module
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
                                case "weather":
                                    if (this.Options["allow_weather"])
                                    {
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= tmp_command.Access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                get_weather(line[4], channel, ircbot);
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
                                    }
                                    break;
                                case "forecast":
                                    if (this.Options["allow_forecast"])
                                    {
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= tmp_command.Access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                get_forecast(line[4], channel, ircbot, Convert.ToInt32(this.Options["forecast_days"]));
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
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private static void get_forecast(string term, string channel, bot ircbot, int days)
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
            if (location != ", " && !String.IsNullOrEmpty(location))
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

        private static void get_weather(string term, string channel, bot ircbot)
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
