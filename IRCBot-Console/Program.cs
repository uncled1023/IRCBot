using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using IRCBot;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;
using System.Threading;
using System.Text.RegularExpressions;
using System.Net;
using System.ComponentModel;

namespace IRCBot_Console
{
    class Program
    {
        bot_controller controller;
        public readonly object listLock = new object();
        public readonly object errorlock = new object();

        private bool running = false;
        delegate void SetTextCallback(string text);

        static void Main(string[] args)
        {
            IRCBot_Console.Program prog = new Program();
            prog.run_bot();
        }

        public void run_bot()
        {
            running = true;
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

            if (File.Exists(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNode list = xmlDoc.SelectSingleNode("/client_settings");
                string config_path = list["config_path"].InnerText;
                if (!config_path.Trim().Equals(string.Empty))
                {
                    controller = new bot_controller(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + config_path.Trim());
                    string[] server_list = controller.list_servers();
                    int index = 0;
                    bool server_started = false;
                    foreach (string server in server_list)
                    {
                        server_started = controller.init_server(server, false);
                        if (server_started)
                        {
                            index++;
                        }
                    }
                    if (index == 0)
                    {
                        Console.WriteLine("No Servers Specified for Auto Start");
                    }
                }
                else
                {
                    Console.WriteLine("Missing Config File.");
                }
            }
            else
            {
                Console.WriteLine("Missing Config File.");
            }


            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(UpdateOutput);
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync();

            while (running)
            {
                string line = Console.ReadLine();
                char[] separator = new char[] { ' ' };
                string[] args = line.Split(separator, 2, StringSplitOptions.RemoveEmptyEntries);
                if(args.GetUpperBound(0) > 0 && args[0].StartsWith("/") && controller != null)
                {
                    string channel = "System";
                    string input_tmp = args[1];
                    char[] charSeparator = new char[] { ' ' };
                    string[] input = input_tmp.Split(charSeparator, 2);
                    string[] param;

                    if (input.GetUpperBound(0) > 0)
                    {
                        param = input[1].Split(' ');
                    }
                    else
                    {
                        param = null;
                    }
                    controller.run_command(args[0].TrimStart('/'), channel, input[0], param);
                }
                else
                {
                    string cmd = "";
                    string arg = "";
                    if (args.GetUpperBound(0) > 0)
                    {
                        cmd = args[0];
                        arg = args[1];
                    }
                    else
                    {
                        cmd = line;
                    }
                    string[] param = arg.Split(' ');
                    switch (cmd.ToLower())
                    {
                        case "exit":
                            running = false;
                            break;
                        default:
                            break;
                    }
                }
            }
            worker.CancelAsync();
        }

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Resources";
            string assemblyPath = folderPath + Path.DirectorySeparatorChar + new AssemblyName(args.Name).Name + ".dll";
            if (File.Exists(assemblyPath) == false) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        private void UpdateOutput(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            while (!bw.CancellationPending)
            {
                string text = string.Join("", controller.get_queue());
                string[] stringSeparators = new string[] { "\r\n" };
                string[] lines = text.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x <= lines.GetUpperBound(0); x++)
                {
                    UpdateOutput_final(lines[x]);
                }
                Thread.Sleep(30);
            }
        }

        private void UpdateOutput_final(string text)
        {
            char[] text_sep = new char[] { ':' };
            string[] server = text.Split(text_sep, 2);
            string tmp_server_name = server[0];
            if (server.GetUpperBound(0) > 0)
            {
                tmp_server_name = server[0];
                text = server[1];
            }
            else
            {
                tmp_server_name = server[0];
                text = "";
            }
            Bot.bot bot = controller.get_bot_instance(tmp_server_name);

            string message = text;
            bool display_message = true;
            string channel = "System";
            string nickname = "";
            string prefix = "---";
            string postfix = "---";
            string nick_sep = " ";

            if (bot != null)
            {
                char[] charSeparator = new char[] { ' ' };
                string[] tmp_lines = text.Split(charSeparator, 4);
                string time_stamp = DateTime.Now.ToString("hh:mm:ss tt");
                string date_stamp = DateTime.Now.ToString("yyyy-MM-dd");
                if (tmp_lines.GetUpperBound(0) > 1)
                {
                    tmp_lines[1] = tmp_lines[1];
                    if (tmp_lines[1].Equals("notice", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channel = tmp_lines[3].TrimStart(':').TrimStart('[');
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        string[] tmp_msg = channel.Split(charSeparator, 2);
                        if (channel.StartsWith("#"))
                        {
                            channel = "#" + tmp_msg[0].TrimStart('#').TrimEnd(']');
                            if (tmp_msg.GetUpperBound(0) > 0)
                            {
                                message = tmp_msg[1];
                            }
                            else
                            {
                                message = tmp_msg[0];
                            }
                        }
                        else
                        {
                            channel = "System";
                            if (tmp_msg.GetUpperBound(0) > 0)
                            {
                                message = tmp_msg[0] + " " + tmp_msg[1];
                            }
                            else
                            {
                                message = tmp_msg[0];
                            }
                        }
                        prefix = "---";
                        postfix = "---";
                        nick_sep = " ";
                    }
                    else if (tmp_lines[1].Equals("privmsg", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channel = tmp_lines[2].TrimStart(':');
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        message = tmp_lines[3].Remove(0, 1);
                        nick_sep = " ";
                        if (channel.Equals(bot.Nick))
                        {
                            if (nickname.Equals("nickserv", StringComparison.InvariantCultureIgnoreCase) || nickname.Equals("chanserv", StringComparison.InvariantCultureIgnoreCase))
                            {
                                channel = "System";
                                prefix = "";
                                postfix = "--->";
                                nickname = "";
                            }
                            else
                            {
                                channel = nickname;
                                prefix = "";
                                postfix = " --->";
                            }
                        }
                        else if (channel.Equals("nickserv", StringComparison.InvariantCultureIgnoreCase) && nickname.Equals(bot.Nick))
                        {
                            channel = "System";
                            prefix = "<---";
                            postfix = "";
                            nickname = "";
                        }
                        else if (channel.Equals("chanserv", StringComparison.InvariantCultureIgnoreCase) && nickname.Equals(bot.Nick))
                        {
                            channel = "System";
                            prefix = "<---";
                            postfix = "";
                            nickname = "";
                        }
                        else
                        {
                            prefix = "";
                            postfix = " --->";
                        }
                        string ctcp_pattern = "^(\u0001(.*?)\u0001)*$";
                        Match match = Regex.Match(message, ctcp_pattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            ctcp_pattern = "^(\u0001ACTION(.*?)\u0001)*$";
                            Match tmp_match = Regex.Match(message, ctcp_pattern, RegexOptions.IgnoreCase);
                            if (tmp_match.Success)
                            {
                                prefix = ">";
                                postfix = ">";
                                Regex reg = new Regex(ctcp_pattern);
                                message = nickname + " " + reg.Replace(message, "$2");
                                nickname = "";
                            }
                            else
                            {
                                if (nickname.Equals(bot.Nick))
                                {
                                    prefix = "<<";
                                    postfix = "<<";
                                    nickname = "";
                                }
                                else
                                {
                                    prefix = ">>";
                                    postfix = ">>";
                                    nickname = "";
                                }
                                channel = "System";
                            }
                        }
                    }
                    else if (tmp_lines[1].Equals("join", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channel = tmp_lines[2].TrimStart(':');
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        message = "has joined " + channel;
                        prefix = "--->> ";
                        postfix = "";
                        if (channel.StartsWith("#"))
                        {
                        }
                        else
                        {
                            channel = "System";
                        }
                    }
                    else if (tmp_lines[1].Equals("part", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channel = tmp_lines[2];
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        message = "has left " + tmp_lines[2];
                        prefix = "<<--- ";
                        postfix = "";
                        if (channel.StartsWith("#"))
                        {
                            if (nickname.Equals(bot.Nick))
                            {
                                display_message = false;
                            }
                        }
                        else
                        {
                            channel = "System";
                        }
                    }
                    else if (tmp_lines[1].Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                    {
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        channel = "";
                        foreach (Bot.Channel_Info chan_info in bot.Conf.Channel_List)
                        {
                            foreach (var nick_info in chan_info.Nicks)
                            {
                                if (nick_info.Nick.Equals(nickname, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    channel += "," + chan_info.Channel;
                                    break;
                                }
                            }
                        }
                        channel = channel.TrimStart(',');
                        if (tmp_lines.GetUpperBound(0) > 2)
                        {
                            message = "has quit (" + tmp_lines[2].TrimStart(':') + " " + tmp_lines[3] + ")";
                        }
                        else
                        {
                            message = "has quit (" + tmp_lines[2].TrimStart(':') + ")";
                        }
                        prefix = "<<--- ";
                        postfix = "";
                    }
                    else if (tmp_lines[1].Equals("nick", StringComparison.InvariantCultureIgnoreCase))
                    {
                        nickname = tmp_lines[2].TrimStart(':');
                        channel = "";
                        foreach (Bot.Channel_Info chan_info in bot.Conf.Channel_List)
                        {
                            foreach (var nick_info in chan_info.Nicks)
                            {
                                if (nick_info.Nick.Equals(nickname, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    channel += "," + chan_info.Channel;
                                    break;
                                }
                            }
                        }
                        channel = channel.TrimStart(',');
                        message = tmp_lines[0].TrimStart(':').Split('!')[0] + " is now known as " + nickname;
                        prefix = "--";
                        postfix = "--";
                        nickname = "*";
                    }
                    else if (tmp_lines[1].Equals("mode", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channel = tmp_lines[2];
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        if (channel.StartsWith("#"))
                        {
                        }
                        else if (channel.Equals(bot.Nick))
                        {
                            channel = "System";
                        }
                        if (nickname.Equals(bot.Nick))
                        {
                            display_message = false;
                        }
                        else
                        {
                            message = "has set Mode " + tmp_lines[3];
                        }
                        prefix = ">> ";
                        postfix = "";
                    }
                    else if (tmp_lines[1].Equals("topic", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channel = tmp_lines[2];
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        message = "has set Topic " + tmp_lines[3];
                        prefix = "";
                        postfix = "";
                    }
                    else if (tmp_lines[1].Equals("kick", StringComparison.InvariantCultureIgnoreCase))
                    {
                        channel = tmp_lines[2];
                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                        if (channel.StartsWith("#"))
                        {
                        }
                        else if (channel.Equals(bot.Nick))
                        {
                            channel = "System";
                        }
                        message = "has kicked " + tmp_lines[3].Replace(':', '(') + ")";
                        prefix = "";
                        postfix = "";
                    }
                    else if (tmp_lines[1].Equals("352"))
                    {
                        display_message = false;
                    }
                    else
                    {
                        if (tmp_lines.GetUpperBound(0) > 2)
                        {
                            string[] new_lines = tmp_lines[3].Split(charSeparator, 2);
                            if (tmp_lines[2].Equals(bot.Nick) && tmp_lines[3].StartsWith("#") && new_lines.GetUpperBound(0) > 0)
                            {
                                if (new_lines[1] != ":End of /NAMES list.")
                                {
                                    channel = new_lines[0];
                                    nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                                    message = new_lines[1].TrimStart(':');
                                }
                                else
                                {
                                    channel = "System";
                                    nickname = tmp_lines[0].TrimStart(':').Split('!')[0] + ": ";
                                    charSeparator = new char[] { ':' };
                                    string[] tmp_msg = text.Split(charSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                                    if (tmp_msg.GetUpperBound(0) > 0)
                                    {
                                        message = tmp_msg[1];
                                    }
                                    else
                                    {
                                        message = tmp_msg[0];
                                    }
                                }
                            }
                            else
                            {
                                channel = "System";
                                nickname = tmp_lines[0].TrimStart(':').Split('!')[0] + ": ";
                                charSeparator = new char[] { ':' };
                                string[] tmp_msg = text.Split(charSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                                if (tmp_msg.GetUpperBound(0) > 0)
                                {
                                    message = tmp_msg[1];
                                }
                                else
                                {
                                    message = tmp_msg[0];
                                }
                            }
                        }
                        else
                        {
                            channel = "System";
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0] + ": ";
                            charSeparator = new char[] { ':' };
                            string[] tmp_msg = text.Split(charSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                            if (tmp_msg.GetUpperBound(0) > 0)
                            {
                                message = tmp_msg[1];
                            }
                            else
                            {
                                message = tmp_msg[0];
                            }
                        }
                    }
                }
                if (channel.Equals(""))
                {
                    channel = "System";
                }
                if (nickname.Equals(string.Empty) && message.Equals(string.Empty))
                {
                    display_message = false;
                }
                if (display_message)
                {
                    string line = "[" + time_stamp + "] [" + bot.Conf.Server_Name + "] [" + channel + "] " + prefix + nickname + postfix + nick_sep + message;
                    int line_length = line.Length;

                    //Append line to end of outbox
                    Console.WriteLine(line);
                }
                if (!text.Trim().Equals(string.Empty))
                {
                    string[] channels = channel.Split(',');
                    foreach (string channel_line in channels)
                    {
                        controller.log(text, bot, channel_line, date_stamp, time_stamp);
                    }
                }
            }
        }
    }
}
