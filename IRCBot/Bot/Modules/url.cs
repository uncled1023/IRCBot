using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using Google.YouTube;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Web;

namespace Bot.Modules
{
    class url : Module
    {
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if (type.Equals("channel") && bot_command == false)
            {
                string text = "";
                if(line.GetUpperBound(0) > 3)
                {
                    text = line[3] + " " + line[4];
                }
                else
                {
                    text = line[3];
                }
                try
                {
                    Regex regex = new Regex("(((https?|ftp|file)://|www\\.)([A-Z0-9.\\-:]{1,})\\.[0-9A-Z?;~&#=\\-_\\./]{2,})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                    //get the first match
                    MatchCollection matches = regex.Matches(text);

                    foreach (Match match in matches)
                    {
                        string testMatch = match.Value.ToString();
                        if (!testMatch.Contains("://"))
                        {
                            testMatch = "http://" + testMatch;
                        }
                        Uri url = new Uri(testMatch);
                        System.Net.WebRequest req = System.Net.HttpWebRequest.Create(url);
                        req.Method = "HEAD";
                        using (System.Net.WebResponse resp = req.GetResponse())
                        {
                            string[] content_type = resp.ContentType.Split('/');
                            switch (content_type[0])
                            {
                                case "text":
                                    WebClient x = new WebClient();
                                    x.Encoding = Encoding.UTF8;
                                    string source = x.DownloadString(url.OriginalString);
                                    string title_regex = @"(?<=<title.*>)(.*?)(?=</title>)";
                                    Regex title_ex = new Regex(title_regex, RegexOptions.IgnoreCase);
                                    MatchCollection title_matches = title_ex.Matches(source);
                                    string title = title_matches[0].Value.Trim();
                                    if (url.OriginalString.Contains("youtube.com/watch?") && this.Options["parse_youtube"])
                                    {
                                        string YouTubeVideoID = ExtractYouTubeVideoIDFromUrl(url.OriginalString);
                                        Uri videoEntryUrl = new Uri(string.Format("https://gdata.youtube.com/feeds/api/videos/{0}", YouTubeVideoID));
                                        YouTubeRequestSettings settings = new YouTubeRequestSettings("YouTube Video Duration Sample App", developerKey);
                                        YouTubeRequest yt_request = new YouTubeRequest(settings);
                                        Video video = yt_request.Retrieve<Video>(videoEntryUrl);
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
                                        ircbot.sendData("PRIVMSG", channel + " :[Youtube] Title: " + HttpUtility.HtmlDecode(yt_title) + " | Length: " + total_duration.TrimEnd(' ') + " | Views: " + string.Format("{0:#,###0}", views) + " | Rated: " + Math.Round(rateavg, 2).ToString() + "/5.0 | Uploaded By: " + uploader + " on " + date.ToString("yyyy-MM-dd"));
                                    }
                                    else if (url.OriginalString.Contains("youtu.be") && this.Options["parse_youtube"])
                                    {
                                        string[] url_parsed = url.OriginalString.Split('/');
                                        string YouTubeVideoID = url_parsed[url_parsed.GetUpperBound(0)];
                                        Uri videoEntryUrl = new Uri(string.Format("https://gdata.youtube.com/feeds/api/videos/{0}", YouTubeVideoID));
                                        YouTubeRequestSettings settings = new YouTubeRequestSettings("YouTube Video Duration Sample App", developerKey);
                                        YouTubeRequest yt_request = new YouTubeRequest(settings);
                                        Video video = yt_request.Retrieve<Video>(videoEntryUrl);
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
                                        ircbot.sendData("PRIVMSG", channel + " :[Youtube] Title: " + HttpUtility.HtmlDecode(yt_title) + " | Length: " + total_duration.TrimEnd(' ') + " | Views: " + string.Format("{0:#,###0}", views) + " | Rated: " + Math.Round(rateavg, 2).ToString() + "/5.0 | Uploaded By: " + uploader + " on " + date.ToString("yyyy-MM-dd"));
                                    }
                                    else if ((url.OriginalString.Contains("boards.4chan.org") && url.Segments.GetUpperBound(0) > 2) && this.Options["parse_4chan"])
                                    {
                                        string board = url.Segments[1].TrimEnd('/');
                                        string uri = "https://a.4cdn.org/" + board + "/thread/" + url.Segments[3].TrimEnd('/') + ".json";
                                        WebClient chan = new WebClient();
                                        chan.Encoding = Encoding.UTF8;
                                        var json_data = string.Empty;
                                        json_data = chan.DownloadString(uri);
                                        XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json_data, board + "-" + url.Segments[3].TrimEnd('/'));
                                        XmlNodeList post_list = xmlDoc.SelectNodes(board + "-" + url.Segments[3].TrimEnd('/') + "/posts");
                                        string thread = "";
                                        if (!url.Fragment.Equals(string.Empty))
                                        {
                                            thread = url.Fragment.TrimStart('#').TrimStart('p');
                                        }
                                        else
                                        {
                                            thread = url.Segments[3].TrimEnd('/');
                                        }
                                        foreach (XmlNode post in post_list)
                                        {
                                            string post_num = post["no"].InnerText;
                                            if (post_num.Equals(thread))
                                            {
                                                string date = post["now"].InnerText;
                                                string parsed_date = date.Split('(')[0] + " " + date.Split(')')[1];
                                                DateTime post_date = DateTime.Parse(parsed_date);
                                                TimeSpan difference = DateTime.UtcNow - post_date;
                                                difference = difference.Subtract(TimeSpan.FromHours(4));
                                                string total_duration = "";
                                                TimeSpan t = TimeSpan.FromSeconds(difference.TotalSeconds);
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
                                                string post_name = "", post_comment = "", tripcode = "", ID = "", email = "", subject = "", replies = "", images = "", image_ext = "", image_name = "", image_width = "", image_height = "";
                                                try
                                                {
                                                    post_name = post["name"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    post_comment = post["com"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    tripcode = post["trip"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    ID = post["id"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    email = post["email"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    subject = post["sub"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    replies = post["replies"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    images = post["images"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    image_ext = post["ext"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    image_name = post["tim"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    image_width = post["w"].InnerText;
                                                }
                                                catch { }
                                                try
                                                {
                                                    image_height = post["h"].InnerText;
                                                }
                                                catch { }

                                                if (!ID.Trim().Equals(string.Empty))
                                                {
                                                    ID = "[" + ID + "]";
                                                }

                                                string quote = "<span class=\"(.*?)\">(.*?)</span>";
                                                subject = Regex.Replace(subject, quote, "$2");
                                                string post_message = "";
                                                if (!subject.Equals(string.Empty))
                                                {
                                                    post_message += "Subject: " + subject + " | ";
                                                }
                                                post_comment = Regex.Replace(post_comment, quote, "$2");
                                                string[] words = post_comment.Split(' ');
                                                if (words.GetUpperBound(0) > 15)
                                                {
                                                    post_message += " Comment: ";
                                                    for (int i = 0; i < 15; i++)
                                                    {
                                                        post_message += words[i] + " ";
                                                    }
                                                    post_message += "...";
                                                }
                                                else if (!post_comment.Equals(string.Empty))
                                                {
                                                    post_message += " Comment: " + post_comment;
                                                }

                                                string[] tmp_post = Regex.Split(post_message, "<br>");
                                                post_message = "";
                                                foreach (string tmp in tmp_post)
                                                {
                                                    if (!tmp.Trim().Equals(string.Empty))
                                                    {
                                                        post_message += HttpUtility.HtmlDecode(tmp) + " | ";
                                                    }
                                                }

                                                string image_url = "";
                                                if (!image_name.Equals(string.Empty))
                                                {
                                                    image_url = "http://images.4chan.org/" + board + "/src/" + image_name + image_ext;
                                                }

                                                if (!image_url.Equals(string.Empty))
                                                {
                                                    image_url = " | Posted Image: " + image_url + " (" + image_width + "x" + image_height + ")";
                                                }

                                                if (!replies.Equals(string.Empty))
                                                {
                                                    replies = " | Replies: " + replies;
                                                }

                                                if (!images.Equals(string.Empty))
                                                {
                                                    images = " | Images: " + images;
                                                }

                                                ircbot.sendData("PRIVMSG", channel + " :[4chan] /" + board + "/ | Posted by: " + post_name + tripcode + ID + " " + total_duration.Trim() + " ago" + replies + images + image_url);
                                                string re = @"<a [^>]+>(.*?)<\/a>(.*?)";
                                                if (!post_message.Equals(string.Empty))
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :" + Regex.Replace(post_message.Replace("<wbr>", "").Trim().TrimEnd('|').Trim(), re, "$1"));
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    else if (this.Options["parse_url"].Equals("True"))
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[URL] " + HttpUtility.HtmlDecode(title) + " (" + url.Host + ")");
                                    }
                                    break;
                                case "image":
                                    if (this.Options["parse_image"])
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[" + resp.ContentType + "] Size: " + ToFileSize(resp.ContentLength));
                                    }
                                    break;
                                case "video":
                                    if (this.Options["parse_video"])
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[Video] Type: " + content_type[1] + " | Size: " + ToFileSize(resp.ContentLength));
                                    }
                                    break;
                                case "application":
                                    if (this.Options["parse_app"])
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[Application] Type: " + content_type[1] + " | Size: " + ToFileSize(resp.ContentLength));
                                    }
                                    break;
                                case "audio":
                                    if (this.Options["parse_audio"])
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[Audio] Type: " + content_type[1] + " | Size: " + ToFileSize(resp.ContentLength));
                                    }
                                    break;
                                default:
                                    if (this.Options["parse_content"])
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[URL] " + HttpUtility.HtmlDecode(resp.ContentType) + " (" + url.Host + ")");
                                    }
                                    break;
                            }

                        }
                    }
                }
                catch
                {
                }
            }
        }

        public string ToFileSize(long source)
        {
            const int byteConversion = 1024;
            double bytes = Convert.ToDouble(source);

            if (bytes >= Math.Pow(byteConversion, 3)) //GB Range
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 3), 2), " GB");
            }
            else if (bytes >= Math.Pow(byteConversion, 2)) //MB Range
            {
                return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 2), 2), " MB");
            }
            else if (bytes >= byteConversion) //KB Range
            {
                return string.Concat(Math.Round(bytes / byteConversion, 2), " KB");
            }
            else //Bytes
            {
                return string.Concat(bytes, " Bytes");
            }
        }

        private readonly string developerKey = "AI39si6RqynrlYF5GRmMp01moUQiRUxdB3HPzHdD99sSH9wfMVvf6gosz00Mt--loK-zavQ2oXjpDnL9IAgCSp7sX-yuFA2usA";

        private string ExtractYouTubeVideoIDFromUrl(string youTubeUrl)
        {
            return Regex.Match(youTubeUrl, @"https?://www\.youtube\.com.*v=(?'VideoID'[^&]*)")
                .Groups["VideoID"]
                .Value;
        }
    }
}
