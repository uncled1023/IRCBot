using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using search.api;
using System.Net;

namespace IRCBot
{
    class google
    {
        public void google_control(string[] line, string command, Interface ircbot, int nick_access, string nick)
        {
            switch (command)
            {
                case "google":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            if (line[4].StartsWith("DCC SEND"))
                            {
                                ircbot.sendData("PRIVMSG", line[2] + " :Invalid Search Term");
                            }
                            else
                            {
                                ISearchResult searchClass = new GoogleSearch(line[4]);
                                try
                                {
                                    var list = searchClass.Search();
                                    if (list.Count > 0)
                                    {
                                        foreach (var searchType in list)
                                        {
                                            ircbot.sendData("PRIVMSG", line[2] + " :" + searchType.title.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'").Replace("&amp;", "&") + ": " + searchType.content.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'").Replace("&amp;", "&"));
                                            ircbot.sendData("PRIVMSG", line[2] + " :" + searchType.url);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        ircbot.sendData("PRIVMSG", line[2] + " :No Results Found");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.ToString());
                                }
                            }
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    break;
            }
        }
    }
}

namespace search.api
{
    public struct SearchType
    {
        public string url;
        public string title;
        public string content;
        public FindingEngine engine;
        public enum FindingEngine { Google };
    }

    public interface ISearchResult
    {
        SearchType.FindingEngine Engine { get; set; }
        string SearchExpression { get; set; }
        List<SearchType> Search();
    }

    public class GoogleSearch : ISearchResult
    {
        public GoogleSearch(string searchExpression)
        {
            this.Engine = SearchType.FindingEngine.Google;
            this.SearchExpression = searchExpression;
        }
        public SearchType.FindingEngine Engine { get; set; }
        public string SearchExpression { get; set; }

        public List<SearchType> Search()
        {
            const string urlTemplate = @"http://ajax.googleapis.com/ajax/services/search/web?v=1.0&rsz=large&safe=active&q={0}&start={1}";
            var resultsList = new List<SearchType>();
            int[] offsets = { 0, 8, 16, 24, 32, 40, 48 };
            foreach (var offset in offsets)
            {
                var searchUrl = new Uri(string.Format(urlTemplate, SearchExpression, offset));
                var page = new WebClient().DownloadString(searchUrl);
                var o = (JObject)JsonConvert.DeserializeObject(page);

                var resultsQuery =
                  from result in o["responseData"]["results"].Children()
                  select new SearchType
                  {
                      url = result.Value<string>("url").ToString(),
                      title = result.Value<string>("title").ToString(),
                      content = result.Value<string>("content").ToString(),
                      engine = this.Engine
                  };

                resultsList.AddRange(resultsQuery);
            }
            return resultsList;
        }
    }
}
