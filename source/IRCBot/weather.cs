using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace IRCBot
{
    class weather
    {
        public void weather_control(string[] line, string command, Interface ircbot, int nick_access, string nick)
        {
            switch (command)
            {
                case "w":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
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
                    break;
                case "weather":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
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
                    break;
                case "f":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            // Add introduction
                            get_forecast(line[4], line[2], ircbot);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    break;
                case "forecast":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            // Add introduction
                            get_forecast(line[4], line[2], ircbot);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    break;
            }
        }

        private void get_forecast(string term, string channel, Interface ircbot)
        {
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
                    ircbot.sendData("PRIVMSG", channel + " :Five day forecast for " + location);
                    foreach (XmlNode node in nodes)
                    {
                        foreach (XmlNode sub_node in node)
                        {
                            weekday = sub_node["date"].SelectSingleNode("weekday").InnerText;
                            highf = sub_node["high"].SelectSingleNode("fahrenheit").InnerText;
                            highc = sub_node["high"].SelectSingleNode("celsius").InnerText;
                            lowf = sub_node["low"].SelectSingleNode("fahrenheit").InnerText;
                            lowc = sub_node["low"].SelectSingleNode("celsius").InnerText;
                            conditions = sub_node["conditions"].InnerText;
                            ircbot.sendData("PRIVMSG", channel + " :" + weekday + ": " + conditions + " with a high of " + highf + " F (" + highc + " C) and a low of " + lowf + " F (" + lowc + " C).");
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

        private void get_weather(string term, string channel, Interface ircbot)
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
