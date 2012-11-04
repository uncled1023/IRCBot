using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Management;

namespace IRCBot
{
    class bot
    {
        // Load Modules //

        // Access Module
        access access;
        // Owner Module
        owner owner;
        // Help Module
        help help;
        // Rules Module
        rules rules;
        // Intro Message Module
        intro intro;
        // Quote Module
        quote quote;
        // Seen Module
        seen seen;
        // Weather Module
        weather weather;
        // Google Module
        google google;
        // Urban Dictionary Module
        urban_dictionary ud;
        // 8ball Module
        _8ball _8ball;
        // AI Module
        AI ai;
        // Messaging Module
        messaging message_module;
        // Hbomb Module
        hbomb hbomb;
        // Ping Me Module
        pingme pingme;
        // Fun Commands Module
        fun fun;
        // ChatBot Module
        chat chat;
        // Poll Module
        poll poll;
        // Roll Call Module
        roll_call roll_call;
        // Version Response Module
        version version;

        TcpClient IRCConnection;
        IRCConfig config;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;

        private System.Windows.Forms.Timer checkRegisterationTimer;
        private System.Windows.Forms.Timer Spam_Check_Timer;
        private System.Windows.Forms.Timer Spam_Timer;
        private System.Windows.Forms.Timer check_cancel;
        private bool spam_activated;
        private bool restart;
        private int restart_attempts;

        public string server_name;
        public bool connected;
        public bool shouldRun;
        public bool first_run;
        public int spam_count;
        public List<List<string>> nick_list;
        public List<string> channel_list;
        public string cur_dir;
        public BackgroundWorker worker;

        Interface ircbot;
        public IRCConfig conf;

        public bot()
        {
            // Load Modules //

            // Access Module
            access = new access();
            // Owner Module
            owner = new owner();
            // Help Module
            help = new help();
            // Rules Module
            rules = new rules();
            // Intro Message Module
            intro = new intro();
            // Quote Module
            quote = new quote();
            // Seen Module
            seen = new seen();
            // Weather Module
            weather = new weather();
            // Google Module
            google = new google();
            // Urban Dictionary Module
            ud = new urban_dictionary();
            // 8ball Module
            _8ball = new _8ball();
            // AI Module
            ai = new AI();
            // Messaging Module
            message_module = new messaging();
            // Hbomb Module
            hbomb = new hbomb();
            // Ping Me Module
            pingme = new pingme();
            // Fun Commands Module
            fun = new fun();
            // ChatBot Module
            chat = new chat();
            // Poll Module
            poll = new poll();
            // Roll Call Module
            roll_call = new roll_call();
            // Version Response Module
            version = new version();

            IRCConnection = null;
            ns = null;
            sr = null;
            sw = null;

            checkRegisterationTimer = new System.Windows.Forms.Timer();
            Spam_Check_Timer = new System.Windows.Forms.Timer();
            Spam_Timer = new System.Windows.Forms.Timer();
            check_cancel = new System.Windows.Forms.Timer();
            connected = false;
            spam_activated = false;
            restart = false;
            restart_attempts = 0;
            server_name = "No_Server_Specified";
            worker = new BackgroundWorker();

            shouldRun = true;
            first_run = true;
            spam_count = 0;
            nick_list = new List<List<string>>();
            channel_list = new List<string>();
        }

        public void start_bot(Interface main, IRCConfig tmp_conf)
        {
            ircbot = main;
            conf = tmp_conf;
            string[] tmp_server = conf.server.Split('.');
            if (tmp_server.GetUpperBound(0) > 0)
            {
                server_name = tmp_server[1];
            }
            cur_dir = ircbot.cur_dir;

            Spam_Check_Timer.Tick += new EventHandler(spam_tick);
            Spam_Check_Timer.Interval = conf.spam_threshold;
            Spam_Check_Timer.Start();

            checkRegisterationTimer.Tick += new EventHandler(checkRegistration);
            checkRegisterationTimer.Interval = 60000;

            Spam_Timer.Tick += new EventHandler(spam_deactivate);

            check_cancel.Tick += new EventHandler(cancel_tick);
            check_cancel.Interval = 500;
            check_cancel.Start();

            BackgroundWorker work = new BackgroundWorker();
            work.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            work.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            work.WorkerSupportsCancellation = true;

            worker = work;
            worker.RunWorkerAsync(2000);
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            Spam_Timer.Interval = conf.spam_timout;
            nick_list.Clear();
            first_run = true;

            IRCBot(bw);
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            connected = false;
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (ns != null)
                ns.Close();
            if (IRCConnection != null)
                IRCConnection.Close();
            checkRegisterationTimer.Stop();

            if (restart == true)
            {
                string output = Environment.NewLine + server_name + ":" + "Restart Attempt " + restart_attempts + " [" + Math.Pow(2, Convert.ToDouble(restart_attempts)) + " Seconds Delay]";
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
                start_bot(ircbot, conf);
            }
            else
            {
                if (server_name.Equals("No_Server_Specified"))
                {
                    string output = Environment.NewLine + server_name + ":" + "Please add a server to connect to.";
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                restart_attempts = 0;
            }
        }

        public void IRCBot(BackgroundWorker bw)
        {
            if (restart == true)
            {
                Thread.Sleep(Convert.ToInt32(Math.Pow(2, Convert.ToDouble(restart_attempts))) * 1000);
            }
            config = conf;
            try
            {
                connected = true;
                IRCConnection = new TcpClient(conf.server, conf.port);
                restart = false;
            }
            catch
            {
                connected = false;
                string output = Environment.NewLine + server_name + ":" + "Connection Error";

                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
                restart = true;
                restart_attempts++;
            }

            if (restart == false)
            {
                try
                {
                    connected = true;
                    ns = IRCConnection.GetStream();
                    sr = new StreamReader(ns);
                    sw = new StreamWriter(ns);
                    if (conf.pass != "")
                    {
                        sendData("PASS", config.pass);
                    }
                    if (conf.email != "")
                    {
                        sendData("USER", config.nick + " " + conf.email + " " + conf.email + " :" + config.name);
                    }
                    else
                    {
                        sendData("USER", config.nick + " default_host default_server :" + config.name);
                    }
                    sendData("NICK", config.nick);
                    IRCWork();
                }
                catch (Exception ex)
                {
                    string output =  Environment.NewLine + server_name + ":" + ex.ToString().Replace("\r\n", " ");

                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                finally
                {
                    connected = false;
                    if (sr != null)
                        sr.Close();
                    if (sw != null)
                        sw.Close();
                    if (ns != null)
                        ns.Close();
                    if (IRCConnection != null)
                        IRCConnection.Close();
                    if (bw.CancellationPending != true)
                    {
                        restart = true;
                        restart_attempts++;
                    }
                }
            }
        }

        public void sendData(string cmd, string param)
        {
            if (sw != null)
            {
                if (param == null)
                {
                    sw.WriteLine(cmd);
                    sw.Flush();
                    string output =  Environment.NewLine + server_name + ":" + ":" + conf.nick + " " + cmd;

                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                else
                {
                    char[] separator = new char[] { ':' };
                    string[] message = param.Split(separator, 2);
                    if (message.GetUpperBound(0) > 0)
                    {
                        string first = cmd + " " + message[0];
                        string second = message[1];
                        if ((first.Length + 1 + second.Length) > conf.max_message_length)
                        {
                            string msg = "";
                            string[] par = second.Split(' ');
                            foreach (string word in par)
                            {
                                if ((first.Length + msg.Length + word.Length + 1) < conf.max_message_length)
                                {
                                    msg += " " + word;
                                }
                                else
                                {
                                    msg = msg.Remove(0, 1);
                                    sw.WriteLine(first + ":" + msg);
                                    sw.Flush();
                                    string output =  Environment.NewLine + server_name + ":" + ":" + conf.nick + " " + first + ":" + msg;
                                    lock (ircbot.listLock)
                                    {
                                        if (ircbot.queue_text.Count >= 1000)
                                        {
                                            ircbot.queue_text.RemoveAt(0);
                                        }
                                        ircbot.queue_text.Add(output);
                                    }
                                    msg = " " + word;
                                }
                            }
                            if (msg.Trim() != "")
                            {
                                msg = msg.Remove(0, 1);
                                sw.WriteLine(first + ":" + msg);
                                sw.Flush();
                                string output =  Environment.NewLine + server_name + ":" + ":" + conf.nick + " " + first + ":" + msg;
                                lock (ircbot.listLock)
                                {
                                    if (ircbot.queue_text.Count >= 1000)
                                    {
                                        ircbot.queue_text.RemoveAt(0);
                                    }
                                    ircbot.queue_text.Add(output);
                                }
                            }
                        }
                        else
                        {
                            sw.WriteLine(cmd + " " + param);
                            sw.Flush();
                            string output =  Environment.NewLine + server_name + ":" + ":" + conf.nick + " " + cmd + " " + param;

                            lock (ircbot.listLock)
                            {
                                if (ircbot.queue_text.Count >= 1000)
                                {
                                    ircbot.queue_text.RemoveAt(0);
                                }
                                ircbot.queue_text.Add(output);
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine(cmd + " " + param);
                        sw.Flush();
                        string output =  Environment.NewLine + server_name + ":" + ":" + conf.nick + " " + cmd + " " + param;

                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }
                    }
                }
            }
        }

        public void IRCWork()
        {
            string[] ex;
            string data;

            joinChannels();
            checkRegisterationTimer.Start();
            first_run = false;
            while (shouldRun)
            {
                restart = false;
                restart_attempts = 0;
                Thread.Sleep(30);
                data = sr.ReadLine();

                string output = Environment.NewLine + server_name + ":" + data;

                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }

                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5, StringSplitOptions.RemoveEmptyEntries);

                if (ex[0] == "PING")
                {
                    sendData("PONG", ex[1]);
                }

                if (ex.GetUpperBound(0) > 3)
                {
                    if (ex[3] == ":Password" && ex[4].StartsWith("accepted"))
                    {
                        checkRegisterationTimer.Stop();
                    }
                }

                // Ping Me Module
                for (int x = 0; x < conf.module_config.Count(); x++)
                {
                    if (conf.module_config[x][0].Equals("Ping Me"))
                    {
                        if (conf.module_config[x][1].Equals("True"))
                        {
                            pingme.check_ping(ex, this);
                        }
                        break;
                    }
                }

                string[] user_info = ex[0].Split('@');
                string[] name = user_info[0].Split('!');
                if (name.GetUpperBound(0) > 0)
                {
                    string nick = name[0].TrimStart(':');
                    string nick_host = user_info[1];
                    string channel = ex[2];

                    // Seen Module
                    for (int x = 0; x < conf.module_config.Count(); x++)
                    {
                        if (conf.module_config[x][0].Equals("Seen"))
                        {
                            if (conf.module_config[x][1].Equals("True"))
                            {
                                seen.add_seen(nick, channel, ex, this);
                            }
                            break;
                        }
                    }

                    if (spam_activated == false)
                    {
                        // On Message Events events
                        if (ex[1].ToLower() == "privmsg")
                        {
                            if (ex.GetUpperBound(0) >= 3) // If valid Input
                            {
                                bool bot_command = false;
                                string command = ex[3]; //grab the command sent
                                command = command.ToLower();
                                string msg_type = command.TrimStart(':');
                                if (msg_type.StartsWith(conf.command) == true)
                                {
                                    bot_command = true;
                                    command = command.Remove(0, 2);
                                }

                                if (bot_command == true) // Starts with the bots command deliminator (aka a command)
                                {

                                    if (ex[2].StartsWith("#") == true) // From Channel
                                    {
                                        // Get the nicks access from channel and access list
                                        int nick_access = get_user_access(nick, channel);

                                        // Access Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Access"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    access.access_control(ex, command, this, conf, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Moderation Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Moderation"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    moderation mod = new moderation();
                                                    mod.moderation_control(ex, command, this, conf, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Owner Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Owner"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    owner.owner_control(ex, command, this, ref conf, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Help Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Help"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    help.help_control(ex, command, this, conf, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Rules Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Rules"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    rules.rules_control(ex, command, this, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Messaging Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Messaging"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    message_module.message_control(ex, command, this, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Intro Message Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Intro"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    intro.intro_control(ex, command, this, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Quote Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Quote"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    quote.quote_control(ex, command, this, conf, x, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Seen Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Seen"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    seen.seen_control(ex, command, this, nick_access, nick, sr);
                                                }
                                                break;
                                            }
                                        }

                                        // Weather Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Weather"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    weather.weather_control(ex, command, this, conf, x, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Google Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Google"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    google.google_control(ex, command, this, conf, x, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Urban Dictionary Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Urban Dictionary"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    ud.ud_control(ex, command, this, conf, x, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // 8ball Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("8ball"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    _8ball._8ball_control(ex, command, this, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // hbomb Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("HBomb"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    hbomb.hbomb_control(ex, command, this, nick_access, nick, channel, conf);
                                                } break;
                                            }
                                        }

                                        // Ping Me Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Ping Me"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    pingme.pingme_control(ex, command, this, nick_access, nick, channel);
                                                }
                                                break;
                                            }
                                        }

                                        // Fun Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Fun"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    fun.fun_control(ex, command, this, nick_access, nick, channel);
                                                }
                                                break;
                                            }
                                        }

                                        // Poll Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Poll"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    poll.poll_control(ex, command, this, nick_access, nick, channel);
                                                }
                                                break;
                                            }
                                        }

                                        // Roll Call Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Roll Call"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    roll_call.roll_call_control(ex, command, this, conf, x, nick_access, channel, nick);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    else // From Query
                                    {
                                        // Get the nicks access from channel and access list
                                        channel = null;
                                        int nick_access = get_user_access(nick, channel);

                                        // Access Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Access"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    access.access_control(ex, command, this, conf, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Owner Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Owner"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    owner.owner_control(ex, command, this, ref conf, nick_access, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Messaging Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Messaging"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    message_module.message_control(ex, command, this, nick);
                                                }
                                                break;
                                            }
                                        }

                                        // Roll Call Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Roll Call"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    roll_call.roll_call_control(ex, command, this, conf, x, nick_access, channel, nick);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                                else // All other text
                                {
                                    if (ex[2].StartsWith("#") == true) // From Channel
                                    {
                                        // ABan/AKick Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Moderation"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    moderation mod = new moderation();
                                                    mod.check_auto(nick, channel, nick_host, this);
                                                }
                                                break;
                                            }
                                        }

                                        // Quote Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Quote"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    quote.add_quote(nick, channel, ex, this, conf);
                                                }
                                                break;
                                            }
                                        }

                                        // Response Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Response"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    ai.AI_Parse(ex, channel, nick, this, conf, chat);
                                                }
                                                break;
                                            }
                                        }

                                        // Messaging Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Messaging"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    message_module.find_message(nick, this);
                                                }
                                                break;
                                            }
                                        }

                                        // Chat Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Chat"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    chat.chat_control(ex, this, conf, nick, channel);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                    else // From Query
                                    {
                                        // Messaging Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Messaging"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    message_module.find_message(nick, this);
                                                }
                                                break;
                                            }
                                        }

                                        // Response Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Response"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    ai.AI_Parse(ex, nick, nick, this, conf, chat);
                                                }
                                                break;
                                            }
                                        }

                                        // Version Response Module
                                        for (int x = 0; x < conf.module_config.Count(); x++)
                                        {
                                            if (conf.module_config[x][0].Equals("Version Response"))
                                            {
                                                if (conf.module_config[x][1].Equals("True"))
                                                {
                                                    version.version_control(ex, this, conf, x, nick);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // On JOIN events
                    if (ex[1].ToLower() == "join")
                    {
                        bool chan_found = false;
                        for (int x = 0; x < nick_list.Count(); x++)
                        {
                            if (nick_list[x][0].Equals(channel.TrimStart(':')))
                            {
                                bool nick_found = false;
                                int new_access = get_user_access(nick, channel.TrimStart(':'));
                                for (int i = 1; i < nick_list[x].Count(); i++)
                                {
                                    string[] split = nick_list[x][i].Split(':');
                                    if (split.GetUpperBound(0) > 0)
                                    {
                                        if (split[1].Equals(nick))
                                        {
                                            nick_found = true;
                                            int old_access = Convert.ToInt32(split[0]);
                                            bool identified = get_user_ident(nick);
                                            if (identified == true)
                                            {
                                                if (old_access > new_access)
                                                {
                                                    new_access = old_access;
                                                }
                                            }
                                            nick_list[x][i] = new_access.ToString() + ":" + nick;
                                            break;
                                        }
                                    }
                                }
                                if (nick_found == false)
                                {
                                    sendData("NAMES", channel.TrimStart(':'));
                                    string line = sr.ReadLine();
                                    output = Environment.NewLine + server_name + ":" + line;
                                    lock (ircbot.listLock)
                                    {
                                        if (ircbot.queue_text.Count >= 1000)
                                        {
                                            ircbot.queue_text.RemoveAt(0);
                                        }
                                        ircbot.queue_text.Add(output);
                                    }
                                    char[] Separator = new char[] { ' ' };
                                    string[] name_line = line.Split(Separator, 5);
                                    while (name_line.GetUpperBound(0) <= 3)
                                    {
                                        line = sr.ReadLine();
                                        name_line = line.Split(Separator, 5);
                                        output = Environment.NewLine + server_name + ":" + line;
                                        lock (ircbot.listLock)
                                        {
                                            if (ircbot.queue_text.Count >= 1000)
                                            {
                                                ircbot.queue_text.RemoveAt(0);
                                            }
                                            ircbot.queue_text.Add(output);
                                        }
                                    }
                                    while (name_line[3] != "=")
                                    {
                                        line = sr.ReadLine();
                                        name_line = line.Split(charSeparator, 5);
                                        output = Environment.NewLine + server_name + ":" + line;
                                        lock (ircbot.listLock)
                                        {
                                            if (ircbot.queue_text.Count >= 1000)
                                            {
                                                ircbot.queue_text.RemoveAt(0);
                                            }
                                            ircbot.queue_text.Add(output);
                                        }
                                        while (name_line.GetUpperBound(0) <= 3)
                                        {
                                            line = sr.ReadLine();
                                            name_line = line.Split(charSeparator, 5);
                                            output = Environment.NewLine + server_name + ":" + line;
                                            lock (ircbot.listLock)
                                            {
                                                if (ircbot.queue_text.Count >= 1000)
                                                {
                                                    ircbot.queue_text.RemoveAt(0);
                                                }
                                                ircbot.queue_text.Add(output);
                                            }
                                        }
                                    }
                                    string[] names_list = name_line[4].Split(':');
                                    if (names_list.GetUpperBound(0) > 0)
                                    {
                                        string[] names = names_list[1].Split(' ');
                                        while (name_line[4] != ":End of /NAMES list.")
                                        {
                                            names_list = name_line[4].Split(':');
                                            if (names_list.GetUpperBound(0) > 0)
                                            {
                                                char[] charSep = new char[] { ' ' };
                                                names = names_list[1].Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                                for (int i = 0; i <= names.GetUpperBound(0); i++)
                                                {
                                                    if (names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+') == nick)
                                                    {
                                                        nick_found = true;
                                                        int user_access = get_access_num(names[i].Remove(1));
                                                        nick_list[x].Add(user_access + ":" + names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+'));
                                                        break;
                                                    }
                                                }
                                            }
                                            line = sr.ReadLine();
                                            name_line = line.Split(charSeparator, 5);
                                            output = Environment.NewLine + server_name + ":" + line;
                                            lock (ircbot.listLock)
                                            {
                                                if (ircbot.queue_text.Count >= 1000)
                                                {
                                                    ircbot.queue_text.RemoveAt(0);
                                                }
                                                ircbot.queue_text.Add(output);
                                            }
                                            while (name_line.GetUpperBound(0) <= 3)
                                            {
                                                line = sr.ReadLine();
                                                name_line = line.Split(charSeparator, 5);
                                                output = Environment.NewLine + server_name + ":" + line;
                                                lock (ircbot.listLock)
                                                {
                                                    if (ircbot.queue_text.Count >= 1000)
                                                    {
                                                        ircbot.queue_text.RemoveAt(0);
                                                    }
                                                    ircbot.queue_text.Add(output);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (chan_found == false)
                        {
                            bool channel_found = false;
                            List<string> tmp_list = new List<string>();
                            tmp_list.Add(channel.TrimStart(':'));
                            sendData("NAMES", channel.TrimStart(':'));
                            string line = sr.ReadLine();
                            char[] Separator = new char[] { ' ' };
                            string[] name_line = line.Split(Separator, 5);
                            output = Environment.NewLine + server_name + ":" + line;
                            lock (ircbot.listLock)
                            {
                                if (ircbot.queue_text.Count >= 1000)
                                {
                                    ircbot.queue_text.RemoveAt(0);
                                }
                                ircbot.queue_text.Add(output);
                            }
                            while (name_line.GetUpperBound(0) <= 3)
                            {
                                line = sr.ReadLine();
                                name_line = line.Split(Separator, 5);
                                output = Environment.NewLine + server_name + ":" + line;
                                lock (ircbot.listLock)
                                {
                                    if (ircbot.queue_text.Count >= 1000)
                                    {
                                        ircbot.queue_text.RemoveAt(0);
                                    }
                                    ircbot.queue_text.Add(output);
                                }
                            }
                            while (name_line[3] != "=")
                            {
                                line = sr.ReadLine();
                                name_line = line.Split(charSeparator, 5);
                                output = Environment.NewLine + server_name + ":" + line;
                                lock (ircbot.listLock)
                                {
                                    if (ircbot.queue_text.Count >= 1000)
                                    {
                                        ircbot.queue_text.RemoveAt(0);
                                    }
                                    ircbot.queue_text.Add(output);
                                }
                                while (name_line.GetUpperBound(0) <= 3)
                                {
                                    line = sr.ReadLine();
                                    name_line = line.Split(charSeparator, 5);
                                    output = Environment.NewLine + server_name + ":" + line;
                                    lock (ircbot.listLock)
                                    {
                                        if (ircbot.queue_text.Count >= 1000)
                                        {
                                            ircbot.queue_text.RemoveAt(0);
                                        }
                                        ircbot.queue_text.Add(output);
                                    }
                                }
                            }
                            string[] names_list = name_line[4].Split(':');
                            if (names_list.GetUpperBound(0) > 0)
                            {
                                string[] names = names_list[1].Split(' ');
                                while (name_line[4] != ":End of /NAMES list.")
                                {
                                    channel_found = true;
                                    names_list = name_line[4].Split(':');
                                    if (names_list.GetUpperBound(0) > 0)
                                    {
                                        char[] charSep = new char[] { ' ' };
                                        names = names_list[1].Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                                        for (int i = 0; i <= names.GetUpperBound(0); i++)
                                        {
                                            int user_access = get_access_num(names[i].Remove(1));
                                            tmp_list.Add(user_access + ":" + names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+'));
                                        }
                                    }
                                    line = sr.ReadLine();
                                    name_line = line.Split(charSeparator, 5);
                                    output = Environment.NewLine + server_name + ":" + line;
                                    lock (ircbot.listLock)
                                    {
                                        if (ircbot.queue_text.Count >= 1000)
                                        {
                                            ircbot.queue_text.RemoveAt(0);
                                        }
                                        ircbot.queue_text.Add(output);
                                    }
                                    while (name_line.GetUpperBound(0) <= 3)
                                    {
                                        line = sr.ReadLine();
                                        name_line = line.Split(charSeparator, 5);
                                        output = Environment.NewLine + server_name + ":" + line;
                                        lock (ircbot.listLock)
                                        {
                                            if (ircbot.queue_text.Count >= 1000)
                                            {
                                                ircbot.queue_text.RemoveAt(0);
                                            }
                                            ircbot.queue_text.Add(output);
                                        }
                                    }
                                }
                                if (channel_found == true)
                                {
                                    for (int i = 0; i < nick_list.Count(); i++)
                                    {
                                        if (nick_list[i][0].Equals(channel.TrimStart(':')))
                                        {
                                            nick_list.RemoveAt(i);
                                        }
                                    }
                                    nick_list.Add(tmp_list);
                                }
                            }
                        }

                        // Intro Message Module
                        for (int x = 0; x < conf.module_config.Count(); x++)
                        {
                            if (conf.module_config[x][0].Equals("Intro"))
                            {
                                if (conf.module_config[x][1].Equals("True"))
                                {
                                    intro.check_intro(nick, channel.TrimStart(':'), this);
                                }
                                break;
                            }
                        }

                        // Messaging Module
                        for (int x = 0; x < conf.module_config.Count(); x++)
                        {
                            if (conf.module_config[x][0].Equals("Messaging"))
                            {
                                if (conf.module_config[x][1].Equals("True"))
                                {
                                    message_module.find_message(nick, this);
                                }
                                break;
                            }
                        }

                        // ABan/AKick Module
                        for (int x = 0; x < conf.module_config.Count(); x++)
                        {
                            if (conf.module_config[x][0].Equals("Moderation"))
                            {
                                if (conf.module_config[x][1].Equals("True"))
                                {
                                    moderation mod = new moderation();
                                    mod.check_auto(nick, channel.TrimStart(':'), nick_host, this);
                                }
                                break;
                            }
                        }
                    }

                    // On user QUIT events
                    if (ex[1].ToLower() == "quit")
                    {
                        for (int x = 0; x < nick_list.Count(); x++)
                        {
                            for (int i = 1; i < nick_list[x].Count(); i++)
                            {
                                string[] split = nick_list[x][i].Split(':');
                                if (split[1].Equals(nick))
                                {
                                    nick_list[x].RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }

                    // On user PART events
                    if (ex[1].ToLower() == "part")
                    {
                        for (int x = 0; x < nick_list.Count(); x++)
                        {
                            if (nick_list[x][0].Equals(ex[2]))
                            {
                                for (int i = 1; i < nick_list[x].Count(); i++)
                                {
                                    string[] split = nick_list[x][i].Split(':');
                                    if (split[1].Equals(nick))
                                    {
                                        nick_list[x].RemoveAt(i);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // On user Nick Change events
                    if (ex[1].ToLower() == "nick")
                    {
                        for (int x = 0; x < nick_list.Count(); x++)
                        {
                            for (int i = 1; i < nick_list[x].Count(); i++)
                            {
                                string[] split = nick_list[x][i].Split(':');
                                if (split.GetUpperBound(0) > 0)
                                {
                                    if (split[1].Equals(nick))
                                    {
                                        int old_access = Convert.ToInt32(split[0]);
                                        int new_access = get_user_access(ex[2].TrimStart(':'), nick_list[x][0]);
                                        if (old_access > new_access)
                                        {
                                            new_access = old_access;
                                        }
                                        nick_list[x][i] = new_access.ToString() + ":" + ex[2].TrimStart(':');
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // On ChanServ Mode Change
                    if (ex[1].ToLower() == "mode")
                    {
                        if (ex.GetUpperBound(0) > 3)
                        {
                            if (ex[3].TrimStart('-').TrimStart('+').ToLower() == "o" || ex[3].TrimStart('-').TrimStart('+').ToLower() == "v" || ex[3].TrimStart('-').TrimStart('+').ToLower() == "h" || ex[3].TrimStart('-').TrimStart('+').ToLower() == "q" || ex[3].TrimStart('-').TrimStart('+').ToLower() == "a")
                            {
                                for (int x = 0; x < nick_list.Count(); x++)
                                {
                                    if (nick_list[x][0].Equals(ex[2]))
                                    {
                                        bool nick_found = false;
                                        string[] new_nick = ex[4].Split(charSeparator, 2);
                                        for (int y = 0; y <= new_nick.GetUpperBound(0); y++)
                                        {
                                            int new_access = get_user_access(new_nick[y], ex[2]);
                                            bool identified = get_user_ident(new_nick[y]);
                                            for (int i = 1; i < nick_list[x].Count(); i++)
                                            {
                                                string[] split = nick_list[x][i].Split(':');
                                                if (split.GetUpperBound(0) > 0)
                                                {
                                                    if (split[1].Equals(new_nick[y]))
                                                    {
                                                        nick_found = true;
                                                        if (split[0] != "")
                                                        {
                                                            int old_access = Convert.ToInt32(split[0]);
                                                            if (identified == true)
                                                            {
                                                                if (old_access > new_access)
                                                                {
                                                                    new_access = old_access;
                                                                }
                                                            }
                                                        }
                                                        nick_list[x][i] = new_access.ToString() + ":" + new_nick[y];
                                                        break;
                                                    }
                                                }
                                            }
                                            if (nick_found == false)
                                            {
                                                nick_list[x].Add(new_access.ToString() + ":" + new_nick[y]);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void joinChannels()
        {
            bool tmp_connected = false;
            while (tmp_connected == false)
            {
                Thread.Sleep(30);
                string line = sr.ReadLine();
                char[] charSeparator = new char[] { ' ' };
                string[] ex = line.Split(charSeparator, 5, StringSplitOptions.RemoveEmptyEntries);

                if (ex[0] == "PING")
                {
                    sendData("PONG", ex[1]);
                }
                string[] new_line = line.Split(charSeparator, 5);
                string output =  Environment.NewLine + server_name + ":" + line;
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
                if (new_line.GetUpperBound(0) > 3)
                {
                    if (new_line[3] == ":End" && new_line[4] == "of /MOTD command.")
                    {
                        tmp_connected = true;
                    }
                }
            }
            identify();
            // Joins all the channels in the channel list
            if (conf.chans != "")
            {
                string[] channels = conf.chans.Split(',');
                for (int x = 0; x <= channels.GetUpperBound(0); x++)
                {
                    sendData("JOIN", "#" + channels[x].TrimStart('#'));
                    string line = sr.ReadLine();
                    string output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                    bool channel_found = false;
                    for (int i = 0; i < channel_list.Count(); i++)
                    {
                        if (channel_list[i].Equals(channels[x]))
                        {
                            channel_found = true;
                            break;
                        }
                    }
                    if (channel_found == false)
                    {
                        channel_list.Add(channels[x]);
                    }
                    channel_found = false;
                    List<string> tmp_list = new List<string>();
                    tmp_list.Add(channels[x]);
                    char[] charSeparator = new char[] { ' ' };
                    string[] name_line = line.Split(charSeparator, 5);
                    while (name_line.GetUpperBound(0) < 3)
                    {
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator, 5);
                        output = Environment.NewLine + server_name + ":" + line;
                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }
                    }
                    while (name_line[3] != "=")
                    {
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator, 5);
                        output = Environment.NewLine + server_name + ":" + line;
                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }
                        while (name_line.GetUpperBound(0) < 3)
                        {
                            line = sr.ReadLine();
                            name_line = line.Split(charSeparator, 5);
                            output = Environment.NewLine + server_name + ":" + line;
                            lock (ircbot.listLock)
                            {
                                if (ircbot.queue_text.Count >= 1000)
                                {
                                    ircbot.queue_text.RemoveAt(0);
                                }
                                ircbot.queue_text.Add(output);
                            }
                        }
                    }
                    string[] names_list = name_line[4].Split(':');
                    string[] names = names_list[1].Split(' ');
                    while (name_line[4] != ":End of /NAMES list.")
                    {
                        channel_found = true;
                        names_list = name_line[4].Split(':');
                        char[] charSep = new char[] { ' ' };
                        names = names_list[1].Split(charSep, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i <= names.GetUpperBound(0); i++)
                        {
                            int user_access = get_access_num(names[i].Remove(1));
                            tmp_list.Add(user_access + ":" + names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+'));
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator, 5);
                        output = Environment.NewLine + server_name + ":" + line;
                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }
                    }
                    if (channel_found == true)
                    {
                        for (int i = 0; i < nick_list.Count(); i++)
                        {
                            if (nick_list[i][0].Equals(channels[x]))
                            {
                                nick_list.RemoveAt(i);
                            }
                        }
                        nick_list.Add(tmp_list);
                    }
                }
            }
        }

        private void checkRegistration(object sender, EventArgs e)
        {
            checkRegisterationTimer.Stop();
            if (conf.nick != "" && conf.pass != "" && conf.email != "")
            {
                register_nick(conf.nick, conf.pass, conf.email);
            }
        }

        private void cancel_tick(object sender, EventArgs e)
        {
            if (worker.CancellationPending == true)
            {
                connected = false;
                if (sr != null)
                    sr.Close();
                if (sw != null)
                    sw.Close();
                if (ns != null)
                    ns.Close();
                if (IRCConnection != null)
                    IRCConnection.Close();
                checkRegisterationTimer.Stop();
                string output = Environment.NewLine + server_name + ":" + "Disconnected";

                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
            }
        }

        private void spam_tick(object sender, EventArgs e)
        {
            if (spam_count > conf.spam_count_max)
            {
                spam_count = 0;
                spam_activated = true;
                Spam_Timer.Start();
            }
        }

        private void spam_deactivate(object sender, EventArgs e)
        {
            spam_activated = false;
            Spam_Timer.Stop();
        }

        private void register_nick(string nick, string password, string email)
        {
            sendData("PRIVMSG", "NickServ :register " + password + " " + email);
        }

        public void identify()
        {
            if (conf.pass != "")
            {
                sendData("PRIVMSG", "NickServ :Identify " + conf.pass);
            }
        }

        public int get_command_access(string command)
        {
            int access = 0;
            for (int x = 0; x < conf.command_access.Count(); x++)
            {
                if (conf.command_access[x][0].Equals(command))
                {
                    access = Convert.ToInt32(conf.command_access[x][1]);
                    break;
                }
            }
            return access;
        }

        public string get_user_host(string nick)
        {
            string access = "";
            string line = "";
            //sendData("ISON", nick);
            //line = sr.ReadLine();
            string[] new_nick = nick.Split(' ');
            sendData("WHOIS", new_nick[0]);
            line = sr.ReadLine();
            string output = Environment.NewLine + server_name + ":" + line;
            lock (ircbot.listLock)
            {
                if (ircbot.queue_text.Count >= 1000)
                {
                    ircbot.queue_text.RemoveAt(0);
                }
                ircbot.queue_text.Add(output);
            }
            char[] charSeparator = new char[] { ' ' };
            string[] name_line = line.Split(charSeparator);
            while (name_line[2] != conf.nick || name_line[3] != new_nick[0])
            {
                line = sr.ReadLine();
                name_line = line.Split(charSeparator);
                output = Environment.NewLine + server_name + ":" + line;
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
            }
            if (name_line[5] == "such")
            {
                while (name_line.GetUpperBound(0) < 6)
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                    output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                while (name_line[6] != "/WHOIS")
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                    while (name_line.GetUpperBound(0) < 6)
                    {
                        output =  Environment.NewLine + server_name + ":" + line;
                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator);
                    }
                }
                sendData("WHOWAS", new_nick[0]);
                line = sr.ReadLine();
                string[] tmp_line = line.Split(charSeparator);
                output = Environment.NewLine + server_name + ":" + line;
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
                while (tmp_line[3].Equals(":Server"))
                {
                    sendData("WHOWAS", new_nick[0]);
                    line = sr.ReadLine();
                    tmp_line = line.Split(charSeparator);
                    output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                while (tmp_line[2] != conf.nick || tmp_line[3] != new_nick[0])
                {
                    line = sr.ReadLine();
                    tmp_line = line.Split(charSeparator);
                    output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                access = tmp_line[5];
                while (name_line.GetUpperBound(0) < 6)
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                    output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                while (tmp_line[6] != "WHOWAS")
                {
                    line = sr.ReadLine();
                    tmp_line = line.Split(charSeparator);
                    while (name_line.GetUpperBound(0) < 6)
                    {
                        output =  Environment.NewLine + server_name + ":" + line;
                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator);
                    }
                }
            }
            else
            {
                access = name_line[5];
                while (name_line.GetUpperBound(0) < 6)
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                    output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
                while (name_line[6] != "/WHOIS")
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                    output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                    while (name_line.GetUpperBound(0) < 6)
                    {
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator);
                        output = Environment.NewLine + server_name + ":" + line;
                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }
                    }
                }
            }
            return access;
        }

        public int get_access_num(string type)
        {
            int access = 0;
            if (type.Equals("~"))
            {
                access = 9;
            }
            else if (type.Equals("&"))
            {
                access = 8;
            }
            else if (type.Equals("@"))
            {
                access = 7;
            }
            else if (type.Equals("%"))
            {
                access = 6;
            }
            else if (type.Equals("+"))
            {
                access = 3;
            }
            else
            {
                access = 1;
            }
            return access;
        }

        public bool get_user_ident(string nick)
        {
            bool identified = false;
            sendData("WHOIS", nick);
            string line = sr.ReadLine();
            char[] charSeparator = new char[] { ' ' };
            string[] name_line = line.Split(charSeparator, 5);
            string output = Environment.NewLine + server_name + ":" + line;
            lock (ircbot.listLock)
            {
                if (ircbot.queue_text.Count >= 1000)
                {
                    ircbot.queue_text.RemoveAt(0);
                }
                ircbot.queue_text.Add(output);
            }
            while (name_line.GetUpperBound(0) < 4)
            {
                line = sr.ReadLine();
                name_line = line.Split(charSeparator, 5);
                output = Environment.NewLine + server_name + ":" + line;
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
            }
            while (name_line[4] != ":End of /WHOIS list.")
            {
                if (name_line[4].Equals(":has identified for this nick") || name_line[4].Equals(":is a registered nick"))
                {
                    identified = true;
                }
                line = sr.ReadLine();
                name_line = line.Split(charSeparator, 5);
                output = Environment.NewLine + server_name + ":" + line;
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
                while (name_line.GetUpperBound(0) < 4)
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator, 5);
                    output = Environment.NewLine + server_name + ":" + line;
                    lock (ircbot.listLock)
                    {
                        if (ircbot.queue_text.Count >= 1000)
                        {
                            ircbot.queue_text.RemoveAt(0);
                        }
                        ircbot.queue_text.Add(output);
                    }
                }
            }
            return identified;
        }

        public int get_user_access(string nick, string channel)
        {
            int access_num = 0;
            try
            {
                string access = "0";
                string tmp_custom_access = "";
                bool user_identified = get_user_ident(nick);
                for (int x = 0; x < conf.module_config.Count(); x++)
                {
                    if (conf.module_config[x][0].Equals("Access"))
                    {
                        if (conf.module_config[x][1].Equals("True"))
                        {
                            if (channel != null)
                            {
                                access acc = new access();
                                tmp_custom_access = acc.get_access_list(nick, channel, this);
                                if (user_identified == true)
                                {
                                    access = tmp_custom_access;
                                }
                            }
                        }
                        break;
                    }
                }
                for (int x = 0; x < nick_list.Count(); x++)
                {
                    if (nick_list[x][0].Equals(channel) || channel == null)
                    {
                        for (int i = 1; i < nick_list[x].Count(); i++)
                        {
                            string[] lists = nick_list[x][i].Split(':');
                            if (lists.GetUpperBound(0) > 0)
                            {
                                if (lists[1].Equals(nick))
                                {
                                    access += "," + lists[0];
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                if (user_identified == true)
                {
                    string[] owners = conf.owner.Split(','); // Get list of owners
                    for (int x = 0; x <= owners.GetUpperBound(0); x++)
                    {
                        if (nick.Equals(owners[x]))
                        {
                            access += ",10";
                        }
                    }
                }
                string[] tmp_access = access.TrimStart(',').TrimEnd(',').Split(',');
                foreach (string access_line in tmp_access)
                {
                    if (access_line != "")
                    {
                        if (Convert.ToInt32(access_line) > access_num)
                        {
                            access_num = Convert.ToInt32(access_line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string output = Environment.NewLine + server_name + ":" + ex.ToString();
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
            }
            return access_num;
        }
    }
}
