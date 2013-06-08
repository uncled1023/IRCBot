using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Drawing;
using Google.YouTube;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Web;

namespace IRCBot.Modules
{
    class url : Module
    {
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            string module_name = ircbot.conf.module_config[module_id][0];
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
                    Regex regex = new Regex("((http://|www\\.)([A-Z0-9.-:]{1,})\\.[0-9A-Z?;~&#=\\-_\\./]{2,})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                        var request = WebRequest.Create(url);
                        request.Timeout = 5000;
                        using (var response = request.GetResponse())
                        {
                            string[] content_type = response.ContentType.Split('/');
                            switch (content_type[0])
                            {
                                case "text":
                                    WebClient x = new WebClient();
                                    string source = x.DownloadString(url.OriginalString);
                                    string title_regex = @"(?<=<title.*>)([\s\S]*)(?=</title>)";
                                    Regex title_ex = new Regex(title_regex, RegexOptions.IgnoreCase);
                                    string title = title_ex.Match(source).Value.Trim();
                                    if (url.OriginalString.Contains("youtube.com/watch?") && ircbot.conf.module_config[module_id][4].Equals("True"))
                                    {
                                        string YouTubeVideoID = ExtractYouTubeVideoIDFromUrl(url.OriginalString);
                                        Uri videoEntryUrl = new Uri(string.Format("http://gdata.youtube.com/feeds/api/videos/{0}", YouTubeVideoID));
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
                                        ircbot.sendData("PRIVMSG", channel + " :[Youtube] Title: " + HttpUtility.HtmlDecode(yt_title) + " | Length: " + total_duration.TrimEnd(' ') + " | Views: " + views.ToString() + " | Rated: " + Math.Round(rateavg, 2).ToString() + "/5.0 | Uploaded By: " + uploader + " on " + date.ToString("yyyy-MM-dd"));
                                    }
                                    else if ((url.OriginalString.Contains("boards.4chan.org") && url.Segments.GetUpperBound(0) > 2) && ircbot.conf.module_config[module_id][5].Equals("True"))
                                    {
                                        string board = url.Segments[1].TrimEnd('/');
                                        string uri = "https://api.4chan.org/" + board + "/res/" + url.Segments[3] + ".json";
                                        WebClient chan = new WebClient();
                                        var json_data = string.Empty;
                                        json_data = chan.DownloadString(uri);
                                        XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json_data, board + "-" + url.Segments[3]);
                                        XmlNodeList post_list = xmlDoc.SelectNodes(board + "-" + url.Segments[3] + "/posts");
                                        string thread = "";
                                        if (!url.Fragment.Equals(string.Empty))
                                        {
                                            thread = url.Fragment.TrimStart('#').TrimStart('p');
                                        }
                                        else
                                        {
                                            thread = url.Segments[3];
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

                                                string post_message = "";
                                                if (!subject.Equals(string.Empty))
                                                {
                                                    post_message += "Subject: " + subject + " | ";
                                                }
                                                string[] words = post_comment.Split(' ');
                                                if (words.GetUpperBound(0) > 10)
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
                                                    ircbot.sendData("PRIVMSG", channel + " :" + Regex.Replace(post_message.Trim().TrimEnd('|').Trim(), re, "$1"));
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    else if (ircbot.conf.module_config[module_id][3].Equals("True"))
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[URL] " + HttpUtility.HtmlDecode(title) + " (" + url.Host.ToLower() + ")");
                                    }
                                    break;
                                case "image":
                                    if (ircbot.conf.module_config[module_id][6].Equals("True"))
                                    {
                                        Image _image = null;
                                        _image = DownloadImage(url.OriginalString);
                                        if (_image != null)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :[" + response.ContentType + "] Size: " + ToFileSize(response.ContentLength) + " | Width: " + _image.Width.ToString() + "px | Height: " + _image.Height.ToString() + "px");
                                        }
                                    }
                                    break;
                                case "video":
                                    if (ircbot.conf.module_config[module_id][7].Equals("True"))
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[Video] Type: " + content_type[1] + " | Size: " + ToFileSize(response.ContentLength));
                                    }
                                    break;
                                case "application":
                                    if (ircbot.conf.module_config[module_id][8].Equals("True"))
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[Application] Type: " + content_type[1] + " | Size: " + ToFileSize(response.ContentLength));
                                    }
                                    break;
                                case "audio":
                                    if (ircbot.conf.module_config[module_id][9].Equals("True"))
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[Audio] Type: " + content_type[1] + " | Size: " + ToFileSize(response.ContentLength));
                                    }
                                    break;
                                default:
                                    if (ircbot.conf.module_config[module_id][10].Equals("True"))
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :[URL] " + HttpUtility.HtmlDecode(response.ContentType) + " (" + url.Host.ToLower() + ")");
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

        public Image DownloadImage(string _URL)
        {
            Image _tmpImage = null;

            try
            {
                // Open a connection
                System.Net.HttpWebRequest _HttpWebRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(_URL);

                _HttpWebRequest.AllowWriteStreamBuffering = true;

                // You can also specify additional header values like the user agent or the referer: (Optional)
                _HttpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1)";
                _HttpWebRequest.Referer = "http://www.google.com/";

                // set timeout for 20 seconds (Optional)
                _HttpWebRequest.Timeout = 20000;

                // Request response:
                System.Net.WebResponse _WebResponse = _HttpWebRequest.GetResponse();

                // Open data stream:
                System.IO.Stream _WebStream = _WebResponse.GetResponseStream();

                // convert webstream to image
                _tmpImage = Image.FromStream(_WebStream);

                // Cleanup
                _WebResponse.Close();
                _WebResponse.Close();
            }
            catch (Exception _Exception)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}", _Exception.ToString());
                return null;
            }

            return _tmpImage;
        }

        private readonly string developerKey = "AI39si6RqynrlYF5GRmMp01moUQiRUxdB3HPzHdD99sSH9wfMVvf6gosz00Mt--loK-zavQ2oXjpDnL9IAgCSp7sX-yuFA2usA";

        private string ExtractYouTubeVideoIDFromUrl(string youTubeUrl)
        {
            return Regex.Match(youTubeUrl, @"http://www\.youtube\.com.*v=(?'VideoID'[^&]*)")
                .Groups["VideoID"]
                .Value;
        }
    }
}
