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
                                    case "bug":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
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
                                                string uri = "https://api.github.com/repos/" + ircbot.conf.module_config[module_id][3] + "/" + ircbot.conf.module_config[module_id][5] + "/issues";
                                                string response = post_issue(ircbot, module_id, uri, title, description, ircbot.conf.module_config[module_id][3], label);
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
                                        if (nick_access >= command_access)
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
                                                string uri = "https://api.github.com/repos/" + ircbot.conf.module_config[module_id][3] + "/" + ircbot.conf.module_config[module_id][5] + "/issues";
                                                string response = post_issue(ircbot, module_id, uri, title, description, ircbot.conf.module_config[module_id][3], label);
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
        }

        private string post_issue(bot ircbot, int module_id, string uri, string title, string description, string assignee, List<string> labels)
        {
            Issues issue = new Issues { title = title, body = description, assignee = assignee, labels = labels.ToArray() };

            string jsonString = issue.ToJSON();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.Headers.Add("Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(ircbot.conf.module_config[module_id][3] + ":" + ircbot.conf.module_config[module_id][4])));
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

        private string get_user_info(bot ircbot, int module_id, string uri, string title, string description, string assignee, List<string> labels)
        {
            Issues issue = new Issues { title = title, body = description, assignee = assignee, labels = labels.ToArray() };

            string jsonString = issue.ToJSON();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "POST";
            request.Headers.Add("Authorization: Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(ircbot.conf.module_config[module_id][3] + ":" + ircbot.conf.module_config[module_id][4])));
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
