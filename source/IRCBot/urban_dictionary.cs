using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;

namespace IRCBot
{
    class urban_dictionary
    {
        public void ud_control(string[] line, string command, Interface ircbot, IRCConfig conf, int conf_id, int nick_access, string nick)
        {
            switch (command)
            {
                case "ud":
                    ircbot.spam_count++;
                    if (nick_access >= 1)
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            // Get Urban Dictionary Info
                            get_ud(line[4], line[2], ircbot, conf, conf_id);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    break;
            }
        }

        private void get_ud(string search, string channel, Interface ircbot, IRCConfig conf, int conf_id)
        {
            string URL = "http://www.urbandictionary.com/define.php?term=";
            List<KeyValuePair<string, string>> ret = new List<KeyValuePair<string, string>>();
            try
            {
                string c = new WebClient().DownloadString(URL + System.Web.HttpUtility.UrlEncode(search));
                MatchCollection mc = Regex.Matches(c, "<div class=\"definition\">(.*?)</div>");
                MatchCollection mc2 = Regex.Matches(c, "<td class='word'>(.*?)</td>", RegexOptions.Singleline);
                if (mc.Count <= mc2.Count)
                {
                    for (int i = 0; i < mc.Count; i++)
                    {
                        ret.Add(new KeyValuePair<string, string>(System.Web.HttpUtility.HtmlDecode(mc2[i].Groups[1].Value.Trim()), System.Web.HttpUtility.HtmlDecode(mc[i].Groups[1].Value.Trim())));
                    }
                }
            }
            catch (Exception ex)
            {
                ircbot.sendData("PRIVMSG", channel + " :UrbanDictionary/Search Error: " + ex);
            }
            if (ret.Count > 0)
            {
                foreach (KeyValuePair<string, string> pair in ret)
                {
                    string def = pair.Value;
                    string term = pair.Key;
                    while (def.IndexOf("<a href") != -1)
                    {
                        int start_strip = def.IndexOf("<a href");
                        string tmp_def = def.Substring(start_strip);
                        int end_strip = tmp_def.IndexOf(">");
                        def = def.Remove(start_strip, end_strip + 1);
                    }
                    def = def.Replace("</a>", "").Replace("\n", "").Replace("\r", "").Replace("<strong class=\"highlight\">", "").Replace("</strong>", "");
                    while (term.IndexOf("<a href") != -1)
                    {
                        int start_strip = term.IndexOf("<a href");
                        string tmp_def = term.Substring(start_strip);
                        int end_strip = tmp_def.IndexOf(">");
                        term = term.Remove(start_strip, end_strip + 1);
                    }
                    term = term.Replace("</a>", "").Replace("\n", "").Replace("\r", "").Replace("<strong class=\"highlight\">", "").Replace("</strong>", "");
                    string[] strSep = new string[] { "<br/><br/>" };
                    string[] definition = def.Split(strSep, StringSplitOptions.RemoveEmptyEntries);
                    string[] strSep2 = new string[] { "<br/>" };
                    string[] fin_def = definition[0].Split(strSep2, StringSplitOptions.RemoveEmptyEntries);
                    ircbot.sendData("PRIVMSG", channel + " :" + term + ": " + fin_def[0]);
                    for (int x = 1; fin_def.GetUpperBound(0) >= x; x++)
                    {
                        ircbot.sendData("PRIVMSG", channel + " :" + fin_def[x]);
                    }
                    break;
                }
                if (conf.module_config[conf_id][2].Equals("True"))
                {
                    ircbot.sendData("PRIVMSG", channel + " :" + URL + System.Web.HttpUtility.UrlEncode(search));
                }
            }
            else
            {
                ircbot.sendData("PRIVMSG", channel + " :No Results Found.");
            }
        }
    }
}
