using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace IRCBot.Modules
{
    class search : Module
    {
        public override void control(bot ircbot, ref BotConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
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
                                    case "google":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                if (line[4].StartsWith("DCC SEND"))
                                                {
                                                    ircbot.sendData("PRIVMSG", line[2] + " :Invalid Search Term");
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        List<SearchResult> results = GoogleSearch(line[4]);
                                                        if (results.Count > 0)
                                                        {
                                                            foreach (SearchResult searchType in results)
                                                            {
                                                                ircbot.sendData("PRIVMSG", line[2] + " :" + searchType.title.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'").Replace("&amp;", "&") + ": " + searchType.content.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'").Replace("&amp;", "&"));

                                                                if (conf.module_config[module_id][3].Equals("True"))
                                                                {
                                                                    ircbot.sendData("PRIVMSG", line[2] + " :" + HttpUtility.UrlDecode(searchType.url));
                                                                }
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            ircbot.sendData("PRIVMSG", line[2] + " :No Results Found");
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        ircbot.sendData("PRIVMSG", line[2] + " :I can't search atm.");
                                                    }
                                                }
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

        public static List<SearchResult> GoogleSearch(string search_expression)
        {
            var url_template = "http://ajax.googleapis.com/ajax/services/search/web?v=1.0&rsz=large&safe=active&q={0}&start={1}";
            Uri search_url;
            var results_list = new List<SearchResult>();
            int[] offsets = { 0, 8, 16, 24, 32, 40, 48 };
            foreach (var offset in offsets)
            {
                search_url = new Uri(string.Format(url_template, search_expression, offset));
 
                var page = new WebClient().DownloadString(search_url);
 
                JObject o = (JObject)JsonConvert.DeserializeObject(page);
 
                var results_query =
                    from result in o["responseData"]["results"].Children()
                    select new SearchResult(
                        url: result.Value<string>("url").ToString(),
                        title: result.Value<string>("title").ToString(),
                        content: result.Value<string>("content").ToString(),
                        engine: SearchResult.FindingEngine.google
                        );
 
                foreach (var result in results_query)
                    results_list.Add(result);
            }
 
            return results_list;
        }
    }

    public class SearchResult
    {
        public string url;
        public string title;
        public string content;
        public FindingEngine engine;

        public enum FindingEngine { google, bing, google_and_bing };

        public SearchResult(string url, string title, string content, FindingEngine engine)
        {
            this.url = url;
            this.title = title;
            this.content = content;
            this.engine = engine;
        }
    }
}