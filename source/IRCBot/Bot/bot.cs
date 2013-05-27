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
using System.Net.NetworkInformation;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Management;
using System.Reflection;

namespace IRCBot
{
    class bot
    {
        TcpClient IRCConnection;
        IRCConfig config;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;

        public System.Windows.Forms.Timer checkRegisterationTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer Spam_Check_Timer;
        private System.Windows.Forms.Timer Spam_Timer;
        private System.Windows.Forms.Timer check_cancel;
        private bool restart;
        private int restart_attempts;
        private bool bot_identified;
        private List<string> data_queue = new List<string>();
        private List<string> stream_queue = new List<string>();

        public readonly object queuelock = new object();
        public readonly object streamlock = new object();

        public string server_name;
        public bool connected;
        public bool disconnected;
        public bool shouldRun;
        public bool first_run;
        public int spam_count;
        public bool spam_activated;
        public List<List<string>> nick_list;
        public List<string> channel_list;
        public string cur_dir;
        public BackgroundWorker worker;
        public List<Modules.Module> module_list = new List<Modules.Module>();
        public List<string> modules_loaded = new List<string>();
        public List<string> modules_error = new List<string>();
        public DateTime start_time = new DateTime();

        public Interface ircbot;
        public IRCConfig conf;

        public bot()
        {
            IRCConnection = null;
            ns = null;
            sr = null;
            sw = null;

            bot_identified = false;
            checkRegisterationTimer = new System.Windows.Forms.Timer();
            Spam_Check_Timer = new System.Windows.Forms.Timer();
            Spam_Timer = new System.Windows.Forms.Timer();
            check_cancel = new System.Windows.Forms.Timer();
            connected = false;
            disconnected = true;
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
            start_time = DateTime.Now;
            ircbot = main;
            conf = tmp_conf;
            string[] tmp_server = conf.server.Split('.');
            if (tmp_server.GetUpperBound(0) > 0)
            {
                server_name = tmp_server[1];
            }
            cur_dir = ircbot.cur_dir;

            load_modules();

            Spam_Check_Timer.Tick += new EventHandler(spam_tick);
            Spam_Check_Timer.Interval = conf.spam_threshold;
            Spam_Check_Timer.Start();

            checkRegisterationTimer.Tick += new EventHandler(checkRegistration);
            checkRegisterationTimer.Interval = 120000;
            checkRegisterationTimer.Enabled = true;

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

        public void restart_server()
        {
            start_time = DateTime.Now;
            string[] tmp_server = conf.server.Split('.');
            if (tmp_server.GetUpperBound(0) > 0)
            {
                server_name = tmp_server[1];
            }
            cur_dir = ircbot.cur_dir;

            Spam_Check_Timer.Interval = conf.spam_threshold;
            Spam_Check_Timer.Start();

            checkRegisterationTimer.Interval = 120000;
            checkRegisterationTimer.Enabled = true;

            check_cancel.Interval = 500;
            check_cancel.Start();

            BackgroundWorker work = new BackgroundWorker();
            work.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            work.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            work.WorkerSupportsCancellation = true;

            worker = work;
            worker.RunWorkerAsync(2000);
        }

        public bool check_connection()
        {
            bool conn_open = true;
            if (IRCConnection.Client.Connected == true)
            {
                if (IRCConnection.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if (IRCConnection.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        // Client disconnected
                        conn_open = false;
                    }
                }
            }
            else
            {
                conn_open = false;
            }

            Socket s = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

            try
            {
                s.Connect(conf.server_ip[0], conf.port);
            }
            catch (Exception ex)
            {
                conn_open = false;
            }

            return conn_open;
        }
       
        public void load_modules()
        {
            module_list.Clear();
            modules_loaded.Clear();
            modules_error.Clear();
            foreach (List<string> module in conf.module_config)
            {
                string module_name = module[1];
                string class_name = module[0];
                //create the class base on string
                //note : include the namespace and class name (namespace=IRCBot.Modules, class name=<class_name>)
                Assembly a = Assembly.Load("IRCBot");
                Type t = a.GetType("IRCBot.Modules." + class_name);

                //check to see if the class is instantiated or not
                if (t != null)
                {
                    Modules.Module new_module = (Modules.Module)Activator.CreateInstance(t);
                    module_list.Add(new_module);
                    modules_loaded.Add(module_name);
                }
                else
                {
                    modules_error.Add(module_name);
                }
            }
            if (modules_loaded.Count > 0)
            {
                string msg = "";
                foreach (string module_name in modules_loaded)
                {
                    msg += ", " + module_name;
                }
                string output = Environment.NewLine + server_name + ":Loaded Modules: " + msg.TrimStart(',').Trim();
                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
            }
            if (modules_error.Count > 0)
            {
                string msg = "";
                foreach (string module_name in modules_loaded)
                {
                    msg += ", " + module_name;
                }
                string output = Environment.NewLine + server_name + ":Error Loading Modules: " + msg.TrimStart(',').Trim();
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

        public bool load_module(string class_name)
        {
            bool module_found = false;
            bool module_loaded = false;
            foreach (Modules.Module module in module_list)
            {
                if (module.ToString().Equals("IRCBot.Modules." + class_name))
                {
                    module_found = true;
                    break;
                }
            }
            if (module_found == false)
            {
                //create the class base on string
                //note : include the namespace and class name (namespace=IRCBot.Modules, class name=<class_name>)
                Assembly a = Assembly.Load("IRCBot");
                Type t = a.GetType("IRCBot.Modules." + class_name);

                //check to see if the class is instantiated or not
                if (t != null)
                {
                    Modules.Module new_module = (Modules.Module)Activator.CreateInstance(t);
                    module_list.Add(new_module);
                    module_loaded = true;
                }
            }
            return module_loaded;
        }

        public bool unload_module(string class_name)
        {
            bool module_found = false;
            int index = 0;
            foreach (Modules.Module module in module_list)
            {
                if (module.ToString().Equals("IRCBot.Modules." + class_name))
                {
                    module_list.RemoveAt(index);
                    module_found = true;
                    break;
                }
                index++;
            }
            return module_found;
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            Spam_Timer.Interval = conf.spam_timout;
            nick_list.Clear();
            channel_list.Clear();
            first_run = true;

            IRCBot(bw);
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            disconnected = true;
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
                restart_server();
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
            restart = false;
            config = conf;
            try
            {
                connected = true;
                IRCConnection = new TcpClient(conf.server, conf.port);
            }
            catch
            {
                restart = true;
                restart_attempts++;
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
            }

            if (restart == false)
            {
                try
                {
                    ns = IRCConnection.GetStream();
                    sr = new StreamReader(ns);
                    sw = new StreamWriter(ns);
                    sw.AutoFlush = true;
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

                    IRCWork(bw);
                }
                catch (Exception ex)
                {
                    restart = true;
                    restart_attempts++;
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
                }
            }
        }

        public void sendData(string cmd, string param)
        {
            if (sw != null)
            {
                if (cmd.ToLower().Equals("msg"))
                {
                    cmd = "PRIVMSG";
                }
                if (param == null)
                {
                    sw.WriteLine(cmd);
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

        private void save_stream(Object sender, EventArgs e)
        {
            while (shouldRun)
            {
                try
                {
                    string line = sr.ReadLine();
                    lock (streamlock)
                    {
                        string output = Environment.NewLine + server_name + ":" + line;
                        lock (ircbot.listLock)
                        {
                            if (ircbot.queue_text.Count >= 1000)
                            {
                                ircbot.queue_text.RemoveAt(0);
                            }
                            ircbot.queue_text.Add(output);
                        }

                        data_queue.Add(line);
                        stream_queue.Add(line);
                    }
                }
                catch (Exception ex)
                {
                    lock (streamlock)
                    {
                        stream_queue.Add(ex.Message);
                    }
                    shouldRun = false;
                }
            }
        }

        public string read_stream_queue()
        {
            string response = "";
            lock (streamlock)
            {
                if (stream_queue.Count > 0)
                {
                    response = stream_queue[0];
                    stream_queue.RemoveAt(0);
                }
            }
            return response;
        }

        public string read_queue()
        {
            string response = "";
            lock (streamlock)
            {
                if (data_queue.Count > 0)
                {
                    response = data_queue[0];
                    data_queue.RemoveAt(0);
                }
            }
            return response;
        }

        public void initiate_nick()
        {
            sendData("NICK", config.nick);
            Thread.Sleep(30);
            string line = "test";
            bool nick_accepted = true;
            while (!line.Equals(""))
            {
                if (line.Equals("Nickname is already in use."))
                {
                    nick_accepted = false;
                    string[] nicks = conf.secondary_nicks.Split(',');
                    foreach (string nick in nicks)
                    {
                        sendData("NICK", config.nick);
                        Thread.Sleep(30);
                        line = read_queue();
                        if (!line.Equals("Nickname is already in use."))
                        {
                            nick_accepted = true;
                            conf.nick = nick;
                            ircbot.Name = nick;
                            break;
                        }
                    }
                }
                while (nick_accepted == false)
                {
                    Random rand = new Random();
                    string nick_rand = "Guest" + rand.Next(100000).ToString();
                    sendData("NICK", nick_rand);
                    Thread.Sleep(30);
                    line = read_queue();
                    if (!line.Equals("Nickname is already in use."))
                    {
                        nick_accepted = true;
                        conf.nick = nick_rand;
                        ircbot.Name = nick_rand;
                    }
                }
                line = read_queue();
            }
        }

        public void IRCWork(BackgroundWorker bw)
        {
            shouldRun = true;
            connected = true;
            disconnected = false;

            BackgroundWorker work = new BackgroundWorker();
            work.DoWork += (sender, e) => save_stream(sender, e);
            work.RunWorkerAsync(2000);

            Thread.Sleep(100);

            string data = "";

            checkRegisterationTimer.Enabled = true;

            Thread.Sleep(100);

            initiate_nick();

            bool tmp_connected = false;
            while (tmp_connected == false)
            {
                Thread.Sleep(30);
                string line = read_queue();
                char[] charSeparator = new char[] { ' ' };
                string[] ex = line.Split(charSeparator, 5, StringSplitOptions.RemoveEmptyEntries);

                if (ex.GetUpperBound(0) >= 0)
                {
                    if (ex[0] == "PING")
                    {
                        sendData("PONG", ex[1]);
                    }
                }

                string[] new_line = line.Split(charSeparator, 5);
                if (new_line.GetUpperBound(0) > 3)
                {
                    if (new_line[3] == ":End" && new_line[4] == "of /MOTD command.")
                    {
                        tmp_connected = true;
                    }
                }
            }

            Thread.Sleep(100);

            identify();

            Thread.Sleep(100);

            joinChannels();

            first_run = false;
            connected = true;
            disconnected = false;
            restart_attempts = 0;
            while (shouldRun)
            {
                Thread.Sleep(30);
                data = read_stream_queue();
                if (data != "")
                {
                    parse_stream(data);
                }
                if (bw.CancellationPending == false)
                {
                    bool is_connected = check_connection();
                    if (is_connected == false)
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
                        shouldRun = false;
                        restart = true;
                    }
                }
            }
        }

        public void identify()
        {
            if (conf.pass != "")
            {
                sendData("PRIVMSG", "NickServ :Identify " + conf.pass);
                string line = read_queue();
                char[] charSeparator = new char[] { ' ' };
                string[] name_line = line.Split(charSeparator, 5);
                while (name_line.GetUpperBound(0) <= 3)
                {
                    line = read_queue();
                    name_line = line.Split(charSeparator, 5);
                }
                while (bot_identified == false)
                {
                    if (name_line[3] == ":Password" && name_line[4].StartsWith("accepted"))
                    {
                        checkRegisterationTimer.Enabled = false;
                        bot_identified = true;
                    }
                    else if (name_line[3] == ":Your" && name_line[4].StartsWith("nick"))
                    {
                        bot_identified = true;
                    }
                    else
                    {
                        line = read_queue();
                        name_line = line.Split(charSeparator, 5);
                        while (name_line.GetUpperBound(0) <= 3)
                        {
                            line = read_queue();
                            name_line = line.Split(charSeparator, 5);
                        }
                    }
                }
            }
        }

        private void joinChannels()
        {
            // Joins all the channels in the channel list
            if (conf.chans != "")
            {
                string[] channels = conf.chans.Split(',');
                foreach (string channel in channels)
                {
                    bool chan_blocked = false;
                    string[] channels_blacklist = conf.chan_blacklist.Split(',');
                    for (int i = 0; i <= channels_blacklist.GetUpperBound(0); i++)
                    {
                        if (channel.Equals(channels_blacklist[i]))
                        {
                            chan_blocked = true;
                        }
                    }
                    if (chan_blocked == false)
                    {
                        sendData("JOIN", channel);
                        char[] charSeparator = new char[] { ':' };
                        char[] Separator = new char[] { ' ' };
                        bool channel_found = false;
                        List<string> tmp_list = new List<string>();
                        tmp_list.Add(channel.TrimStart(':'));
                        string line = "";
                        line = read_queue();
                        while (line == "")
                        {
                            line = read_queue();
                        }
                        string[] name_line = line.Split(Separator, 5);
                        while (name_line.GetUpperBound(0) <= 1)
                        {
                            line = read_queue();
                            name_line = line.Split(Separator, 5);
                        }
                        while (name_line[1] != "JOIN" && !line.Contains("Cannot join channel"))
                        {
                            line = read_queue();
                            name_line = line.Split(Separator, 5);
                            while (name_line.GetUpperBound(0) <= 1)
                            {
                                line = read_queue();
                                name_line = line.Split(Separator, 5);
                            }
                        }
                        if (!line.Contains("Cannot join channel"))
                        {
                            name_line = line.Split(Separator, 5);
                            while (name_line.GetUpperBound(0) <= 3)
                            {
                                line = read_queue();
                                name_line = line.Split(Separator, 5);
                            }
                            while (name_line[3] != "=" && name_line[3] != "@" && name_line[3] != "*")
                            {
                                line = read_queue();
                                name_line = line.Split(Separator, 5);
                                while (name_line.GetUpperBound(0) <= 3)
                                {
                                    line = read_queue();
                                    name_line = line.Split(Separator, 5);
                                }
                            }
                            tmp_list.Add(name_line[3]);
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
                                        names = names_list[1].Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                                        for (int i = 0; i <= names.GetUpperBound(0); i++)
                                        {
                                            char[] arr = names[i].ToCharArray();
                                            int user_access = conf.user_level;
                                            foreach (char c in arr)
                                            {
                                                if (c.Equals('~') || c.Equals('&') || c.Equals('@') || c.Equals('%') || c.Equals('+'))
                                                {
                                                    int tmp_access = get_access_num(c.ToString(), false);
                                                    if (tmp_access > user_access)
                                                    {
                                                        user_access = tmp_access;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            tmp_list.Add(user_access + ":" + names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+').ToLower());
                                        }
                                    }
                                    line = read_queue();
                                    name_line = line.Split(Separator, 5);
                                    while (name_line.GetUpperBound(0) <= 3)
                                    {
                                        line = read_queue();
                                        name_line = line.Split(Separator, 5);
                                    }
                                }
                                if (channel_found == true)
                                {
                                    nick_list.Add(tmp_list);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void parse_stream(string data_line)
        {
            string[] ex;
            string type = "base";
            int nick_access = conf.user_level;
            string nick = "";
            string channel = "";
            string nick_host = "";
            bool bot_command = false;
            string command = "";
            restart = false;
            restart_attempts = 0;
            char[] charSeparator = new char[] { ' ' };
            ex = data_line.Split(charSeparator, 5, StringSplitOptions.RemoveEmptyEntries);

            if (ex[0] == "PING")
            {
                sendData("PONG", ex[1]);
            }

            string[] user_info = ex[0].Split('@');
            string[] name = user_info[0].Split('!');
            if (name.GetUpperBound(0) > 0)
            {
                nick = name[0].ToLower().TrimStart(':');
                nick_host = user_info[1];
                channel = ex[2].TrimStart(':');

                type = "line";
                // On Message Events events
                if (ex[1].ToLower() == "privmsg")
                {
                    if (ex.GetUpperBound(0) >= 3) // If valid Input
                    {
                        command = ex[3]; //grab the command sent
                        command = command.ToLower();
                        string msg_type = command.TrimStart(':');
                        if (msg_type.StartsWith(conf.command) == true)
                        {
                            bot_command = true;
                            command = command.Remove(0, 2);
                        }

                        if (ex[2].StartsWith("#") == true) // From Channel
                        {
                            nick_access = get_user_access(nick, channel);
                            type = "channel";
                        }
                        else // From Query
                        {
                            nick_access = get_user_access(nick, null);
                            type = "query";
                        }
                    }
                }

                // On JOIN events
                if (ex[1].ToLower() == "join")
                {
                    type = "join";
                    bool chan_found = false;
                    foreach (string tmp_channel in channel_list)
                    {
                        if (channel.Equals(tmp_channel))
                        {
                            chan_found = true;
                            break;
                        }
                    }
                    if (chan_found == false)
                    {
                        channel_list.Add(channel);
                    }
                    chan_found = false;
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        if (nick_list[x][0].Equals(channel.TrimStart(':')))
                        {
                            bool nick_found = false;
                            chan_found = true;
                            int new_access = get_user_access(nick, channel.TrimStart(':'));
                            for (int i = 2; i < nick_list[x].Count(); i++)
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
                                nick_list[x].Add("1:" + nick);
                            }
                        }
                    }
                    if (chan_found == false)
                    {
                        bool channel_found = false;
                        List<string> tmp_list = new List<string>();
                        tmp_list.Add(channel.TrimStart(':'));
                        string line = "";
                        if (sw != null)
                        {
                            sw.WriteLine("NAMES " + channel.TrimStart(':'));
                            line = read_queue();
                            while (line == "")
                            {
                                line = read_queue();
                            }
                            char[] Separator = new char[] { ' ' };
                            string[] name_line = line.Split(Separator, 5);
                            while (name_line.GetUpperBound(0) <= 3)
                            {
                                line = read_queue();
                                name_line = line.Split(Separator, 5);
                            }
                            while (name_line[3] != "=" && name_line[3] != "@" && name_line[3] != "*")
                            {
                                line = read_queue();
                                name_line = line.Split(charSeparator, 5);
                                while (name_line.GetUpperBound(0) <= 3)
                                {
                                    line = read_queue();
                                    name_line = line.Split(charSeparator, 5);
                                }
                            }
                            tmp_list.Add(name_line[3]);
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
                                            char[] arr = names[i].ToCharArray();
                                            int user_access = conf.user_level;
                                            foreach (char c in arr)
                                            {
                                                if (c.Equals('~') || c.Equals('&') || c.Equals('@') || c.Equals('%') || c.Equals('+'))
                                                {
                                                    int tmp_access = get_access_num(c.ToString(), false);
                                                    if (tmp_access > user_access)
                                                    {
                                                        user_access = tmp_access;
                                                    }
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            tmp_list.Add(user_access + ":" + names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+').ToLower());
                                        }
                                    }
                                    line = read_queue();
                                    name_line = line.Split(charSeparator, 5);
                                    while (name_line.GetUpperBound(0) <= 3)
                                    {
                                        line = read_queue();
                                        name_line = line.Split(charSeparator, 5);
                                    }
                                }
                                if (channel_found == true)
                                {
                                    nick_list.Add(tmp_list);
                                }
                            }
                        }
                    }
                }

                // On user QUIT events
                if (ex[1].ToLower() == "quit")
                {
                    type = "quit";
                }

                // On user PART events
                if (ex[1].ToLower() == "part")
                {
                    type = "part";
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        if (nick_list[x][0].Equals(ex[2]))
                        {
                            for (int i = 2; i < nick_list[x].Count(); i++)
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

                // On user KICK events
                if (ex[1].ToLower() == "kick")
                {
                    type = "kick";
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        if (nick_list[x][0].Equals(ex[2]))
                        {
                            if (ex[3].ToLower().Equals(conf.nick.ToLower()))
                            {
                                nick_list.RemoveAt(x);
                                channel_list.RemoveAt(x);
                            }
                            else
                            {
                                for (int i = 2; i < nick_list[x].Count(); i++)
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
                }

                // On user Nick Change events
                if (ex[1].ToLower() == "nick")
                {
                    type = "nick";
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        for (int i = 2; i < nick_list[x].Count(); i++)
                        {
                            string[] split = nick_list[x][i].Split(':');
                            if (split.GetUpperBound(0) > 0)
                            {
                                if (split[1].Equals(nick))
                                {
                                    nick_list[x][i] = split[0] + ":" + ex[2].TrimStart(':').ToLower();
                                    break;
                                }
                            }
                        }
                    }
                }

                // On ChanServ Mode Change
                if (ex[1].ToLower() == "mode")
                {
                    type = "mode";
                    if (ex.GetUpperBound(0) > 3)
                    {
                        char[] arr = ex[3].TrimStart('-').TrimStart('+').ToCharArray();
                        bool user_mode = false;
                        foreach (char c in arr)
                        {
                            if (c.Equals('q') || c.Equals('a') || c.Equals('o') || c.Equals('h') || c.Equals('v'))
                            {
                                user_mode = true;
                                break;
                            }
                        }
                        if (user_mode == true)
                        {
                            for (int x = 0; x < nick_list.Count(); x++)
                            {
                                if (nick_list[x][0].Equals(ex[2]))
                                {
                                    bool nick_found = false;
                                    string[] new_nick = ex[4].Split(charSeparator, StringSplitOptions.RemoveEmptyEntries);
                                    for (int y = 0; y <= new_nick.GetUpperBound(0); y++)
                                    {
                                        int new_access = conf.user_level;
                                        if (ex[3].StartsWith("-"))
                                        {
                                            for (int i = 2; i < nick_list[x].Count(); i++)
                                            {
                                                string[] split = nick_list[x][i].Split(':');
                                                if (split.GetUpperBound(0) > 0)
                                                {
                                                    if (split[1].Equals(new_nick[y].ToLower()))
                                                    {
                                                        nick_list[x][i] = get_user_op(new_nick[y].ToLower(), channel).ToString() + ":" + new_nick[y].ToLower();
                                                        break;
                                                    }
                                                }
                                            }
                                            new_access = get_user_access(new_nick[y].ToLower(), channel);
                                        }
                                        else
                                        {
                                            int tmp_access = 0;
                                            char[] tmp_arr = ex[3].TrimStart('-').TrimStart('+').ToCharArray();
                                            foreach (char c in arr)
                                            {
                                                if (c.Equals('q') || c.Equals('a') || c.Equals('o') || c.Equals('h') || c.Equals('v'))
                                                {
                                                    tmp_access = get_access_num(c.ToString(), true);
                                                    if (tmp_access > new_access)
                                                    {
                                                        new_access = tmp_access;
                                                    }
                                                }
                                            }
                                        }
                                        for (int i = 2; i < nick_list[x].Count(); i++)
                                        {
                                            string[] split = nick_list[x][i].Split(':');
                                            if (split.GetUpperBound(0) > 0)
                                            {
                                                if (split[1].Equals(new_nick[y].ToLower()))
                                                {
                                                    nick_found = true;
                                                    nick_list[x][i] = new_access.ToString() + ":" + new_nick[y].ToLower();
                                                    break;
                                                }
                                            }
                                        }
                                        if (nick_found == false)
                                        {
                                            nick_list[x].Add(new_access.ToString() + ":" + new_nick[y].ToLower());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Run Enabled Modules
            List<Modules.Module> tmp_module_list = new List<Modules.Module>();
            tmp_module_list.AddRange(module_list);
            foreach (Modules.Module module in tmp_module_list)
            {
                int index = 0;
                bool module_found = false;
                string module_blacklist = "";
                foreach (List<string> conf_module in conf.module_config)
                {
                    if (module.ToString().Equals("IRCBot.Modules." + conf_module[0]))
                    {
                        module_blacklist = conf_module[2];
                        module_found = true;
                        break;
                    }
                    index++;
                }
                if (module_found == true)
                {
                    char[] sepComma = new char[] { ',' };
                    char[] sepSpace = new char[] { ' ' };
                    string[] blacklist = module_blacklist.Split(sepComma, StringSplitOptions.RemoveEmptyEntries);
                    bool module_allowed = true;
                    foreach (string blacklist_node in blacklist)
                    {
                        string[] nodes = blacklist_node.Split(sepSpace, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string node in nodes)
                        {
                            if (node.ToLower().Equals(nick) || node.ToLower().TrimStart('#').Equals(channel.ToLower().TrimStart('#')))
                            {
                                module_allowed = false;
                                break;
                            }
                        }
                        if (module_allowed == false)
                        {
                            break;
                        }
                    }
                    if (module_allowed == true)
                    {
                        BackgroundWorker work = new BackgroundWorker();
                        work.DoWork += (sender, e) => backgroundWorker_RunModule(sender, e, module, index, ex, command, nick_access, nick, channel, bot_command, type);
                        work.RunWorkerAsync(2000);
                    }
                }
            }
        }

        private void backgroundWorker_RunModule(object sender, DoWorkEventArgs e, Modules.Module module, int index, string[] ex, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            module.control(this, ref conf, index, ex, command, nick_access, nick, channel, bot_command, type);
        }

        private void checkRegistration(object sender, EventArgs e)
        {
            if (connected == true)
            {
                checkRegisterationTimer.Enabled = false;
                if (conf.nick != "" && conf.pass != "" && conf.email != "")
                {
                    register_nick(conf.nick, conf.pass, conf.email);
                }
                else
                {
                    string output = Environment.NewLine + server_name + ":You are missing an username and/or password.  Please add those to the server configuration so I can register this nick.";

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
                checkRegisterationTimer.Enabled = false;
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

        public string get_user_host(string nick)
        {
            string access = "";
            string line = "";
            //sendData("ISON", nick);
            //line = sr.ReadLine();
            string[] new_nick = nick.Split(' ');
            if (sw != null)
            {
                sw.WriteLine("USERHOST " + new_nick[0]);
                line = read_queue();
                while (line == "")
                {
                    line = read_queue();
                }
                char[] charSeparator = new char[] { ' ' };
                string[] name_line = line.Split(charSeparator);
                while (name_line[2] != conf.nick || name_line.GetUpperBound(0) < 3)
                {
                    line = read_queue();
                    name_line = line.Split(charSeparator);
                }
                if (name_line[3] != ":")
                {
                    string[] strSplit = new string[] { "=+" };
                    string[] who_split = name_line[3].TrimStart(':').Split(strSplit, StringSplitOptions.RemoveEmptyEntries);
                    if (who_split.GetUpperBound(0) > 0)
                    {
                        string[] hostname = who_split[1].Split('@');
                        if (hostname.GetUpperBound(0) > 0)
                        {
                            access = hostname[1];
                        }
                    }
                }
            }
            return access;
        }

        public int get_access_num(string type, bool letter_mode)
        {
            int access = 0;
            if (type.Equals("~") || (type.Equals("q") && letter_mode == true))
            {
                access = conf.founder_level;
            }
            else if (type.Equals("&") || (type.Equals("a") && letter_mode == true))
            {
                access = conf.sop_level;
            }
            else if (type.Equals("@") || (type.Equals("o") && letter_mode == true))
            {
                access = conf.op_level;
            }
            else if (type.Equals("%") || (type.Equals("h") && letter_mode == true))
            {
                access = conf.hop_level;
            }
            else if (type.Equals("+") || (type.Equals("v") && letter_mode == true))
            {
                access = conf.voice_level;
            }
            else
            {
                access = conf.user_level;
            }
            return access;
        }

        public bool get_user_ident(string nick)
        {
            bool identified = false;
            if (sw != null)
            {
                string line = "";
                sw.WriteLine("PRIVMSG nickserv :STATUS " + nick);
                line = read_queue();
                while (line == "")
                {
                    line = read_queue();
                }
                char[] charSeparator = new char[] { ' ' };
                string[] name_line = line.Split(charSeparator, 5);
                while (name_line.GetUpperBound(0) < 4)
                {
                    line = read_queue();
                    name_line = line.Split(charSeparator, 5);
                }
                while (name_line[3] != ":STATUS")
                {
                    line = read_queue();
                    name_line = line.Split(charSeparator, 5);
                    while (name_line.GetUpperBound(0) < 4)
                    {
                        line = read_queue();
                        name_line = line.Split(charSeparator, 5);
                    }
                }
                if (name_line[4].ToLower().StartsWith(nick + " 3"))
                {
                    identified = true;
                }
            }
            return identified;
        }

        public int get_user_op(string nick, string channel)
        {
            int new_access = conf.user_level;
            bool nick_found = false;
            string line = "";
            if (sw != null)
            {
                sw.WriteLine("NAMES " + channel.TrimStart(':'));
                line = read_queue();
                while (line == "")
                {
                    line = read_queue();
                }
                char[] charSeparator = new char[] { ' ' };
                char[] Separator = new char[] { ' ' };
                string[] name_line = line.Split(Separator, 5);
                while (name_line.GetUpperBound(0) <= 3)
                {
                    line = read_queue();
                    name_line = line.Split(Separator, 5);
                }
                while (name_line[3] != "=" && name_line[3] != "@" && name_line[3] != "*")
                {
                    line = read_queue();
                    name_line = line.Split(charSeparator, 5);
                    while (name_line.GetUpperBound(0) <= 3)
                    {
                        line = read_queue();
                        name_line = line.Split(charSeparator, 5);
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
                                if (names[i].ToLower().TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+') == nick)
                                {
                                    char[] arr = names[i].ToCharArray();
                                    foreach (char c in arr)
                                    {
                                        if (c.Equals('~') || c.Equals('&') || c.Equals('@') || c.Equals('%') || c.Equals('+'))
                                        {
                                            int tmp_access = get_access_num(c.ToString(), false);
                                            if (tmp_access > new_access)
                                            {
                                                new_access = tmp_access;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    nick_found = true;
                                    break;
                                }
                            }
                        }
                        if (nick_found == false)
                        {
                            line = read_queue();
                            name_line = line.Split(charSeparator, 5);
                            while (name_line.GetUpperBound(0) <= 3)
                            {
                                line = read_queue();
                                name_line = line.Split(charSeparator, 5);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return new_access;
        }

        public int get_user_access(string nick, string channel)
        {
            int access_num = 0;
            try
            {
                string access = "0";
                string tmp_custom_access = "";
                bool user_identified = get_user_ident(nick);
                if (user_identified == true)
                {
                    for (int x = 0; x < conf.module_config.Count(); x++)
                    {
                        if (conf.module_config[x][0].Equals("access"))
                        {
                            if (conf.module_config[x][2].Equals("True"))
                            {
                                if (channel != null)
                                {
                                    Modules.access acc = new Modules.access();
                                    tmp_custom_access = acc.get_access_list(nick, channel, this);
                                    access = tmp_custom_access;
                                }
                            }
                            break;
                        }
                    }
                }
                for (int x = 0; x < nick_list.Count(); x++)
                {
                    if (nick_list[x][0].Equals(channel) || channel == null)
                    {
                        for (int i = 2; i < nick_list[x].Count(); i++)
                        {
                            string[] lists = nick_list[x][i].Split(':');
                            if (lists.GetUpperBound(0) > 0)
                            {
                                if (lists[1].ToLower().Equals(nick))
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
                        if (nick.Equals(owners[x].ToLower()))
                        {
                            access += "," + conf.owner_level.ToString();
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
