using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using ExtensionMethods;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bot.Modules
{
    class github : Module
    {
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            char[] charS = new char[] { ' ' };
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
                                case "bug":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string title = "";
                                            string description = "";
                                            string[] split = line[4].Split('|');
                                            title = "[" + nick + "] " + split[0];
                                            if (split.GetUpperBound(0) > 0)
                                            {
                                                description = split[1];
                                            }
                                            List<string> label = new List<string>() { "bug" };
                                            string uri = "https://api.github.com/repos/" + this.Options["username"] + "/" + this.Options["repository"] + "/issues";
                                            string response = post_issue(ircbot, uri, title, description, this.Options["username"], label);
                                            if (response.Equals(""))
                                            {
                                                ircbot.sendData("NOTICE", nick + " :Issue Added Successfully");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + response);
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
                                case "request":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            string title = "";
                                            string description = "";
                                            string[] split = line[4].Split('|');
                                            title = "[" + nick + "] " + split[0];
                                            if (split.GetUpperBound(0) > 0)
                                            {
                                                description = split[1];
                                            }
                                            List<string> label = new List<string>() { "Feature Request" };
                                            string uri = "https://api.github.com/repos/" + this.Options["username"] + "/" + this.Options["repository"] + "/issues";
                                            string response = post_issue(ircbot, uri, title, description, this.Options["username"], label);
                                            if (response.Equals(""))
                                            {
                                                ircbot.sendData("NOTICE", nick + " :Feature Request Added Successfully");
                                            }
                                            else
                                            {
                                                ircbot.sendData("NOTICE", nick + " :" + response);
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
                            }
                        }
                    }
                }
            }
        }

        private string post_issue(bot ircbot, string uri, string title, string description, string assignee, List<string> labels)
        {
            Issues issue = new Issues { title = title, body = description, assignee = assignee, labels = labels.ToArray() };

            string jsonString = issue.ToJSON();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.Headers.Add("Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Options["username"] + ":" + this.Options["api"])));
            request.UserAgent = "IRCBot";
            byte[] postBytes = Encoding.ASCII.GetBytes(jsonString);
            // this is important - make sure you specify type this way
            request.ContentLength = postBytes.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            string reply = "";
            try
            {
                // grab te response and print it out to the console along with the status code
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (WebException ex)
            {
                reply = ex.Message;
            }
            return reply;
        } // end HttpPost 

        private string get_user_info(bot ircbot, string uri, string title, string description, string assignee, List<string> labels)
        {
            Issues issue = new Issues { title = title, body = description, assignee = assignee, labels = labels.ToArray() };

            string jsonString = issue.ToJSON();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.Headers.Add("Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Options["username"] + ":" + this.Options["api"])));
            request.UserAgent = "IRCBot";
            byte[] postBytes = Encoding.ASCII.GetBytes(jsonString);
            // this is important - make sure you specify type this way
            request.ContentLength = postBytes.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            string reply = "";
            try
            {
                // grab te response and print it out to the console along with the status code
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (WebException ex)
            {
                reply = ex.Message;
            }
            return reply;
        } // end HttpPost 
    }

    public class Issues
    {
        public string title { get; set; }
        public string body { get; set; }
        public string assignee { get; set; }
        public int? milestone { get; set; }
        public string[] labels { get; set; }
    }
}
