using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using Google.YouTube;
using Google.GData.YouTube;
using Google.GData.Client;
using Google.GData.Extensions;

namespace Bot.Modules
{
    class search : Module
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
                                case "google":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            if (line[4].StartsWith("DCC SEND"))
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :Invalid Search Term");
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
                                                            ircbot.sendData("PRIVMSG", channel + " :" + searchType.title.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'").Replace("&amp;", "&") + ": " + searchType.content.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'").Replace("&amp;", "&"));

                                                            if (this.Options["show_url"])
                                                            {
                                                                ircbot.sendData("PRIVMSG", channel + " :" + System.Web.HttpUtility.UrlDecode(searchType.url));
                                                            }
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :No Results Found");
                                                    }
                                                }
                                                catch
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :Sorry, Google isn't responding to me right now.  Try again later.");
                                                }
                                            }
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
                                case "youtube":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            Feed<Video> results = YoutubeSearch(line[4]);
                                            if (results.TotalResults > 0)
                                            {
                                                foreach (Video video in results.Entries)
                                                {
                                                    int duration = int.Parse(video.Contents.First().Duration);
                                                    string yt_title = video.Title;
                                                    int views = video.ViewCount;
                                                    double rateavg = video.RatingAverage;
                                                    string uploader = video.Uploader;
                                                    DateTime date = video.Updated;
                                                    string total_duration = "";
                                                    TimeSpan t = TimeSpan.FromSeconds(duration);
                                                    if (t.Hours > 0)
                                                    {
                                                        total_duration += t.Hours.ToString() + "h ";
                                                    }
                                                    if (t.Minutes > 0)
                                                    {
                                                        total_duration += t.Minutes.ToString() + "m ";
                                                    }
                                                    if (t.Seconds > 0)
                                                    {
                                                        total_duration += t.Seconds.ToString() + "s ";
                                                    }
                                                    ircbot.sendData("PRIVMSG", channel + " :[Youtube] Title: " + System.Web.HttpUtility.HtmlDecode(yt_title) + " | Length: " + total_duration.TrimEnd(' ') + " | Views: " + string.Format("{0:#,###0}", views) + " | Rated: " + Math.Round(rateavg, 2).ToString() + "/5.0 | Uploaded By: " + uploader + " on " + date.ToString("yyyy-MM-dd"));
                                                        
                                                    if (this.Options["show_url"])
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + video.WatchPage.ToString());
                                                    }
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :No Results Found");
                                            }
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

        public static Feed<Video> YoutubeSearch(string search_expression)
        {
            string developerKey = "AI39si6RqynrlYF5GRmMp01moUQiRUxdB3HPzHdD99sSH9wfMVvf6gosz00Mt--loK-zavQ2oXjpDnL9IAgCSp7sX-yuFA2usA";
            YouTubeQuery query = new YouTubeQuery(YouTubeQuery.DefaultVideoUri);
            query.OrderBy = "relevance";
            query.Query = search_expression;
            query.SafeSearch = YouTubeQuery.SafeSearchValues.None;

            YouTubeRequestSettings settings = new YouTubeRequestSettings("YouTube Video Duration Sample App", developerKey);
            YouTubeRequest yt_request = new YouTubeRequest(settings);
            Feed<Video> results = yt_request.Get<Video>(query);
            return results;
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
                WebClient web = new WebClient();
                web.Encoding = Encoding.UTF8;
                var page = web.DownloadString(search_url);
 
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