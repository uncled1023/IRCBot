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
using System.Web;
using System.Text.RegularExpressions;

struct Board
{
    public string channel;
    public string cur_board;
    public string cur_thread;
    public string cur_reply_thread;
    public int cur_OP_num;
    public int cur_reply_num;
}

namespace IRCBot.Modules
{
    class _4chan : Module
    {
        private List<Board> Board_stats = new List<Board>();

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
                                    case "4chan":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] args = line[4].Split(' ');
                                                if (args.GetUpperBound(0) > 1)
                                                {
                                                    try
                                                    {
                                                        bool thread_id = false;
                                                        bool reply_id = false;
                                                        if (args[1].StartsWith("#"))
                                                        {
                                                            thread_id = true;
                                                            args[1] = args[1].TrimStart('#');
                                                        }
                                                        if (args[2].StartsWith("#"))
                                                        {
                                                            reply_id = true;
                                                            args[2] = args[2].TrimStart('#');
                                                        }
                                                        bool thread_found = get_reply(channel, ircbot, args[0], args[1], args[2], thread_id, reply_id);
                                                        if (!thread_found)
                                                        {
                                                            ircbot.sendData("PRIVMSG", channel + " :Could not find the thread specified");
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :Could not find the thread specified");
                                                    }
                                                }
                                                else if (args.GetUpperBound(0) > 0)
                                                {
                                                    try
                                                    {
                                                        bool thread_id = false;
                                                        if (args[1].StartsWith("#"))
                                                        {
                                                            thread_id = true;
                                                            args[1] = args[1].TrimStart('#');
                                                        }
                                                        bool thread_found = get_thread(channel, ircbot, args[0], args[1], thread_id);
                                                        if (!thread_found)
                                                        {
                                                            ircbot.sendData("PRIVMSG", channel + " :Could not find the thread specified");
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :Could not find the thread specified");
                                                    }
                                                }
                                                else
                                                {
                                                    bool thread_found = get_thread(channel, ircbot, args[0], "0", false);
                                                    if (!thread_found)
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :Could not find the board specified");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                string uri = "https://api.4chan.org/boards.json";
                                                WebClient chan = new WebClient();
                                                var json_data = string.Empty;
                                                json_data = chan.DownloadString(uri);
                                                XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json_data, "_4chan");
                                                XmlNodeList board_list = xmlDoc.SelectNodes("_4chan/boards");
                                                string msg = "";
                                                foreach (XmlNode tmp_board in board_list)
                                                {
                                                    msg += " /" + tmp_board["board"].InnerText + "/,";
                                                }
                                                if (!msg.Equals(string.Empty))
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :Boards Available:" + msg.TrimEnd(','));
                                                }
                                                else
                                                {
                                                    ircbot.sendData("PRIVMSG", channel + " :No Boards Available");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "next_thread":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool chan_found = false;
                                            string board = "";
                                            string thread = "";
                                            bool thread_id = false;
                                            for (int x = 0; x < Board_stats.Count(); x++)
                                            {
                                                if (Board_stats[x].channel.Equals(channel))
                                                {
                                                    chan_found = true;
                                                    board = Board_stats[x].cur_board;
                                                    thread = (Board_stats[x].cur_OP_num + 1).ToString();
                                                }
                                            }
                                            if (!chan_found)
                                            {
                                                string uri = "https://api.4chan.org/boards.json";
                                                WebClient chan = new WebClient();
                                                var json_data = string.Empty;
                                                json_data = chan.DownloadString(uri);
                                                XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json_data, "_4chan");
                                                XmlNodeList board_list = xmlDoc.SelectNodes("_4chan/boards");
                                                Random rand = new Random();
                                                int stop = rand.Next(board_list.Count);
                                                int index = 0;
                                                foreach(XmlNode tmp_board in board_list)
                                                {
                                                    if(stop == index)
                                                    {
                                                        board = tmp_board["board"].InnerText;
                                                        break;
                                                    }
                                                    index++;
                                                }
                                                thread = "0";
                                            }
                                            bool thread_found = get_thread(channel, ircbot, board, thread, thread_id);
                                            if (!thread_found)
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :Could not find the thread specified");
                                            }
                                        }
                                        else
                                        {
                                            ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "next_reply":
                                        if (spam_check == true)
                                        {
                                            ircbot.add_spam_count(channel);
                                        }
                                        if (nick_access >= command_access)
                                        {
                                            bool chan_found = false;
                                            string board = "";
                                            string thread = "";
                                            string reply = "";
                                            bool thread_id = false;
                                            bool reply_id = false;
                                            for (int x = 0; x < Board_stats.Count(); x++)
                                            {
                                                if (Board_stats[x].channel.Equals(channel))
                                                {
                                                    chan_found = true;
                                                    board = Board_stats[x].cur_board;
                                                    thread = Board_stats[x].cur_thread;
                                                    thread_id = true;
                                                    reply = (Board_stats[x].cur_reply_num + 1).ToString();
                                                }
                                            }
                                            if (!chan_found)
                                            {
                                                string uri = "https://api.4chan.org/boards.json";
                                                WebClient chan = new WebClient();
                                                var json_data = string.Empty;
                                                json_data = chan.DownloadString(uri);
                                                XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json_data, "_4chan");
                                                XmlNodeList board_list = xmlDoc.SelectNodes("_4chan/boards");
                                                Random rand = new Random();
                                                int stop = rand.Next(board_list.Count);
                                                int index = 0;
                                                foreach (XmlNode tmp_board in board_list)
                                                {
                                                    if (stop == index)
                                                    {
                                                        board = tmp_board["board"].InnerText;
                                                        break;
                                                    }
                                                    index++;
                                                }
                                                thread = "0";
                                                reply = "0";
                                            }
                                            bool thread_found = get_reply(channel, ircbot, board, thread, reply, thread_id, reply_id);
                                            if (!thread_found)
                                            {
                                                ircbot.sendData("PRIVMSG", channel + " :Could not find the thread specified");
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

        private bool get_thread(string channel, bot ircbot, string board, string thread, bool thread_id)
        {
            bool thread_found = false;
            string uri_board = "https://api.4chan.org/boards.json";
            WebClient chan_board = new WebClient();
            var json_data_board = string.Empty;
            json_data_board = chan_board.DownloadString(uri_board);
            XmlDocument xmlDoc_board = JsonConvert.DeserializeXmlNode(json_data_board, "_4chan");
            XmlNodeList board_list_board = xmlDoc_board.SelectNodes("_4chan/boards");
            int pages = 0;
            foreach (XmlNode tmp_board in board_list_board)
            {
                if (tmp_board["board"].InnerText.Equals(board))
                {
                    pages = Convert.ToInt32(tmp_board["pages"].InnerText);
                    break;
                }
            }
            for (int page_num = 0; page_num < pages; page_num++)
            {
                string uri = "https://api.4chan.org/" + board + "/" + page_num + ".json";
                WebClient chan = new WebClient();
                var json_data = string.Empty;
                json_data = chan.DownloadString(uri);
                XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json_data, "_" + board + "-" + thread);
                XmlNodeList post_list = xmlDoc.SelectNodes("_" + board + "-" + thread + "/threads/posts");
                int index = 0;
                foreach (XmlNode post in post_list)
                {
                    string tmp_post_num = post["no"].InnerText;
                    string tmp_replies = "";
                    try
                    {
                        tmp_replies = post["replies"].InnerText;
                    }
                    catch { }
                    if (((thread_id && tmp_post_num.Equals(thread)) || (!thread_id && index == Convert.ToInt32(thread))) && !tmp_replies.Equals(string.Empty))
                    {
                        bool chan_found = false;
                        for (int x = 0; x < Board_stats.Count(); x++)
                        {
                            if (Board_stats[x].channel.Equals(channel))
                            {
                                chan_found = true;
                                Board tmp_board = new Board();
                                tmp_board.channel = channel;
                                tmp_board.cur_board = board;
                                tmp_board.cur_OP_num = index;
                                tmp_board.cur_reply_num = 0;
                                tmp_board.cur_reply_thread = tmp_post_num;
                                tmp_board.cur_thread = tmp_post_num;
                                Board_stats.RemoveAt(x);
                                Board_stats.Add(tmp_board);
                                break;
                            }
                        }
                        if (!chan_found)
                        {
                            Board tmp_board = new Board();
                            tmp_board.channel = channel;
                            tmp_board.cur_board = board;
                            tmp_board.cur_OP_num = index;
                            tmp_board.cur_reply_num = 0;
                            tmp_board.cur_reply_thread = tmp_post_num;
                            tmp_board.cur_thread = tmp_post_num;
                            Board_stats.Add(tmp_board);
                        }
                        thread_found = true;
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

                        string quote = "<span class=\"quote\">(.*?)</span>";
                        post_message = Regex.Replace(post_message, quote, "$1");
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
                        ircbot.sendData("PRIVMSG", channel + " :http://boards.4chan.org/" + board + "/res/" + tmp_post_num);
                        break;
                    }
                    if (!tmp_replies.Equals(string.Empty))
                    {
                        index++;
                    }
                }
                if (thread_found)
                {
                    break;
                }
            }
            return thread_found;
        }

        private bool get_reply(string channel, bot ircbot, string board, string thread, string reply, bool thread_id, bool reply_id)
        {
            bool thread_found = false;
            string uri_board = "https://api.4chan.org/boards.json";
            WebClient chan_board = new WebClient();
            var json_data_board = string.Empty;
            json_data_board = chan_board.DownloadString(uri_board);
            XmlDocument xmlDoc_board = JsonConvert.DeserializeXmlNode(json_data_board, "_4chan");
            XmlNodeList board_list_board = xmlDoc_board.SelectNodes("_4chan/boards");
            int pages = 0;
            foreach (XmlNode tmp_board in board_list_board)
            {
                if (tmp_board["board"].InnerText.Equals(board))
                {
                    pages = Convert.ToInt32(tmp_board["pages"].InnerText);
                    break;
                }
            }
            for (int page_num = 0; page_num < pages; page_num++)
            {
                string uri = "https://api.4chan.org/" + board + "/" + page_num + ".json";
                WebClient chan = new WebClient();
                var json_data = string.Empty;
                json_data = chan.DownloadString(uri);
                XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(json_data, "_" + board + "-" + thread);
                XmlNodeList post_list = xmlDoc.SelectNodes("_" + board + "-" + thread + "/threads/posts");
                int index = 0;
                foreach (XmlNode post in post_list)
                {
                    string tmp_post_num = post["no"].InnerText;
                    if ((thread_id && tmp_post_num.Equals(thread)) || (!thread_id && index == Convert.ToInt32(thread)))
                    {
                        uri = "https://api.4chan.org/" + board + "/res/" + tmp_post_num + ".json";
                        chan = new WebClient();
                        json_data = string.Empty;
                        json_data = chan.DownloadString(uri);
                        XmlDocument xmlDoc2 = JsonConvert.DeserializeXmlNode(json_data, "_" + board + "-" + thread);
                        XmlNodeList post_list_thread = xmlDoc2.SelectNodes("_" + board + "-" + thread + "/posts");
                        int index_reply = 0;
                        foreach (XmlNode post_reply in post_list_thread)
                        {
                            string tmp_reply_num = post_reply["no"].InnerText;
                            if ((reply_id && tmp_reply_num.Equals(reply)) || (!reply_id && index_reply == Convert.ToInt32(reply)))
                            {
                                bool chan_found = false;
                                for (int x = 0; x < Board_stats.Count(); x++)
                                {
                                    if (Board_stats[x].channel.Equals(channel))
                                    {
                                        chan_found = true;
                                        Board tmp_board = Board_stats[x];
                                        tmp_board.channel = channel;
                                        tmp_board.cur_board = board;
                                        tmp_board.cur_OP_num = index;
                                        tmp_board.cur_reply_num = index_reply;
                                        tmp_board.cur_reply_thread = tmp_reply_num;
                                        tmp_board.cur_thread = tmp_post_num; ;
                                        Board_stats.RemoveAt(x);
                                        Board_stats.Add(tmp_board);
                                        break;
                                    }
                                }
                                if (!chan_found)
                                {
                                    Board tmp_board = new Board();
                                    tmp_board.channel = channel;
                                    tmp_board.cur_board = board;
                                    tmp_board.cur_OP_num = index;
                                    tmp_board.cur_reply_num = index_reply;
                                    tmp_board.cur_reply_thread = tmp_reply_num;
                                    tmp_board.cur_thread = tmp_post_num;
                                    Board_stats.Add(tmp_board);
                                }
                                thread_found = true;
                                string date = post_reply["now"].InnerText;
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
                                    post_name = post_reply["name"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    post_comment = post_reply["com"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    tripcode = post_reply["trip"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    ID = post_reply["id"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    email = post_reply["email"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    subject = post_reply["sub"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    replies = post_reply["replies"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    images = post_reply["images"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    image_ext = post_reply["ext"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    image_name = post_reply["tim"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    image_width = post_reply["w"].InnerText;
                                }
                                catch { }
                                try
                                {
                                    image_height = post_reply["h"].InnerText;
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

                                string quote = "<span class=\"quote\">(.*?)</span>";
                                post_message = Regex.Replace(post_message, quote, "$1");
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
                                ircbot.sendData("PRIVMSG", channel + " :http://boards.4chan.org/" + board + "/res/" + tmp_post_num + "#p" + tmp_reply_num);
                                break;
                            }
                            index_reply++;
                        }
                        index++;
                    }
                }
                if (thread_found)
                {
                    break;
                }
            }
            return thread_found;
        }
    }
}
