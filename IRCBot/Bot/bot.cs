using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
using System.Reflection;

namespace Bot
{
    public class bot
    {
        private BotConfig Conf;
        public BotConfig conf
        {
            get
            {
                return Conf;
            }

            internal set
            {
                Conf = value;
            }
        }

        private string Nick;
        public string nick
        {
            get
            {
                return Nick;
            }

            internal set
            {
                Nick = value;
            }
        }

        private List<List<string>> Nick_List;
        public List<List<string>> nick_list
        {
            get
            {
                return Nick_List;
            }

            internal set
            {
                Nick_List = value;
            }
        }

        internal TcpClient IRCConnection;
        internal NetworkStream ns;
        internal StreamReader sr;
        internal StreamWriter sw;

        internal System.Timers.Timer checkRegisterationTimer;
        internal System.Timers.Timer Spam_Check_Timer;
        internal System.Timers.Timer Spam_Threshold_Check;
        internal List<timer_info> Spam_Timers;
        internal List<string> data_queue;
        internal List<string> stream_queue;
        internal int disconnect_count = 0;

        internal bool restart;
        internal int restart_attempts;
        internal bool connected;
        internal bool connecting;
        internal bool disconnected;
        internal bool shouldRun;
        internal bool first_run;
        internal List<string> channel_list;
        internal string cur_dir;
        internal BackgroundWorker worker;
        internal BackgroundWorker irc_worker;
        internal BackgroundWorker save_stream_worker;
        internal BackgroundWorker check_connect_worker;
        internal List<Modules.Module> module_list;
        internal List<string> modules_loaded;
        internal List<string> modules_error;
        internal DateTime start_time;

        internal IRCBot.bot_controller controller;

        internal readonly object timerlock = new object();
        internal readonly object spamlock = new object();
        internal readonly object queuelock = new object();
        internal readonly object streamlock = new object();

        internal bot(IRCBot.bot_controller main, BotConfig new_conf)
        {
            controller = main;
            conf = new_conf;

            init_bot();
        }

        internal void start_bot()
        {
            init_bot();

            load_modules();

            worker.RunWorkerAsync();
        }

        private void delay_start(object delay_sender, DoWorkEventArgs delay_e, int delay)
        {
            BackgroundWorker bw = delay_sender as BackgroundWorker;
            Thread.Sleep(delay);
            start_bot();
        }

        internal void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                irc_worker.RunWorkerAsync();

                while (!worker.CancellationPending && irc_worker.IsBusy)
                {
                    Thread.Sleep(100);
                }
                if (irc_worker.IsBusy)
                {
                    irc_worker.CancelAsync();
                }
            }
            catch (Exception ex)
            {
                restart = true;

                lock (controller.errorlock)
                {
                    controller.log_error(ex, conf.logs_path);
                }
            }
            e.Cancel = true;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (restart == true)
            {
                string output = Environment.NewLine + conf.server + ":" + "Restart Attempt " + restart_attempts + " [" + Math.Pow(2, Convert.ToDouble(restart_attempts)) + " Seconds Delay]";
                lock (controller.listLock)
                {
                    if (controller.queue_text.Count >= 1000)
                    {
                        controller.queue_text.RemoveAt(0);
                    }
                    controller.queue_text.Add(output);
                }

                stop_timers();
                disconnect();

                int delay = Convert.ToInt32(Math.Pow(2, Convert.ToDouble(restart_attempts))) * 1000;
                restart_attempts++;
                BackgroundWorker work = new BackgroundWorker();
                work.DoWork += (delay_sender, delay_e) => delay_start(delay_sender, delay_e, delay);
                work.RunWorkerAsync(2000);
            }
            else
            {
                if (conf.server.Equals("No_Server_Specified"))
                {
                    string output = Environment.NewLine + conf.server + ":" + "Please add a server to connect to.";
                    lock (controller.listLock)
                    {
                        if (controller.queue_text.Count >= 1000)
                        {
                            controller.queue_text.RemoveAt(0);
                        }
                        controller.queue_text.Add(output);
                    }
                }

                stop_timers();
                disconnect();

                restart_attempts = 0;
            }
        }

        internal void IRCBot(object sender, DoWorkEventArgs e)
        {
            start_timers();

            connected = connect();

            if (connected)
            {
                IRCWork();
            }
            else
            {
                restart = true;
            }
        }

        internal void IRCWork()
        {
            save_stream_worker.RunWorkerAsync();
            check_connect_worker.RunWorkerAsync();
            
            shouldRun = true;
            string data = "";
            
            bool ghost_sent = false;
            string main_nick = nick;
            int cur_alt_nick = 0;
            char[] sep = new char[] { ',' };
            string[] nicks = conf.secondary_nicks.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            int bot_state = 0;
            int pre_state = 0;
            int next_state = 1;
            string line = "";
            char[] charSeparator = new char[] { ' ' };
            string[] ex = new string[2];
            while (shouldRun)
            {
                Thread.Sleep(30);
                switch (bot_state)
                {
                    case 0: // set nick state
                        sendData("NICK", nick);
                        bot_state = next_state;
                        break;
                    case 1: // wait until end of MOTD
                        line = read_queue();
                        if (line.Contains("Nickname is already in use."))
                        {
                            pre_state = bot_state;
                            bot_state = 2;
                        }
                        if (line.Contains("Erroneous Nickname:"))
                        {
                            pre_state = bot_state;
                            bot_state = 2;
                        }
                        else if (line.Contains("Ghost with your nick has been killed.") && ghost_sent)
                        {
                            nick = main_nick;
                            next_state = bot_state;
                            bot_state = 0;
                        }
                        else if (line.Contains("Access Denied.") && ghost_sent)
                        {
                            pre_state = bot_state;
                            bot_state = 2;
                        }
                        else if (line.Contains("End of /MOTD") || line.Contains("End of message of the day."))
                        {
                            bot_state = 4; // move to identify state
                        }
                        else if (line.Contains("Your nick") && line.Contains("isn't registered"))
                        {
                            bot_state = 6;
                        }
                        else if (line.Contains("This nick is awaiting an e-mail verification code before completing registration."))
                        {
                            checkRegisterationTimer.Stop();
                            bot_state = 6;
                        }
                        else
                        {
                        }
                        ex = line.Split(charSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (ex.GetUpperBound(0) > 0)
                        {
                            if (ex[0] == "PING")
                            {
                                sendData("PONG", ex[1]);
                            }
                        }
                        break;
                    case 2: // nick taken state
                        if (nicks.GetUpperBound(0) < cur_alt_nick)
                        {
                            nick = nicks[cur_alt_nick];
                            cur_alt_nick++;
                        }
                        else
                        {
                            Random rand = new Random();
                            string nick_rand = "Guest" + rand.Next(100000).ToString();
                            nick = nick_rand;
                        }
                        if (!ghost_sent)
                        {
                            next_state = 3;
                        }
                        else
                        {
                            next_state = pre_state;
                        }
                        bot_state = 0;
                        break;
                    case 3: // ghost state
                        ghost_sent = true;
                        if (!conf.pass.Equals(string.Empty))
                        {
                            sendData("PRIVMSG", "NickServ :ghost " + main_nick + " " + conf.pass);
                        }
                        bot_state = pre_state;
                        break;
                    case 4: // identify state
                        if (!conf.pass.Equals(string.Empty))
                        {
                            sendData("PRIVMSG", "NickServ :Identify " + conf.pass);
                            pre_state = bot_state;
                            bot_state = 5;
                        }
                        else
                        {
                            pre_state = bot_state;
                            bot_state = 6;
                        }
                        break;
                    case 5: // wait 
                        line = read_queue();
                        if (line.Contains("Nickname is already in use."))
                        {
                            pre_state = bot_state;
                            bot_state = 2;
                        }
                        if (line.Contains("Erroneous Nickname:"))
                        {
                            pre_state = bot_state;
                            bot_state = 2;
                        }
                        else if (line.Contains("Ghost with your nick has been killed.") && ghost_sent)
                        {
                            nick = main_nick;
                            next_state = pre_state;
                            bot_state = 0;
                        }
                        else if (line.Contains("Access Denied.") && ghost_sent)
                        {
                            pre_state = bot_state;
                            bot_state = 2;
                        }
                        else if (line.Contains("Password accepted"))
                        {
                            checkRegisterationTimer.Stop();
                            bot_state = 6;
                        }
                        else if (line.Contains("Your nick") && line.Contains("isn't registered"))
                        {
                            bot_state = 6;
                        }
                        else if (line.Contains("No such nick/channel"))
                        {
                            bot_state = 6;
                        }
                        else if (line.Contains("This nick is awaiting an e-mail verification code before completing registration."))
                        {
                            checkRegisterationTimer.Stop();
                            bot_state = 6;
                        }
                        else
                        {
                        }
                        ex = line.Split(charSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (ex.GetUpperBound(0) > 1)
                        {
                            if (ex[0] == "PING")
                            {
                                sendData("PONG", ex[1]);
                            }
                        }
                        break;
                    case 6:
                        joinChannels();
                        bot_state = -1; // go to default case
                        break;
                    default:
                        connected = true;
                        disconnected = false;
                        restart_attempts = 0;
                        data = read_stream_queue();
                        if (data != "")
                        {
                            parse_stream(data.Trim());
                        }
                        break;
                }
                if (worker.CancellationPending || irc_worker.CancellationPending)
                {
                    shouldRun = false;
                }
            }

            connected = false;
            disconnected = true;
        }

        private void init_bot()
        {
            IRCConnection = null;
            ns = null;
            sr = null;
            sw = null;

            disconnected = true;
            connected = false;
            connecting = false;
            shouldRun = true;
            first_run = true;
            restart = false;
            start_time = DateTime.Now;
            cur_dir = controller.cur_dir;
            nick = conf.nick;
            nick_list = new List<List<string>>();
            channel_list = new List<string>();
            module_list = new List<Modules.Module>();
            modules_loaded = new List<string>();
            modules_error = new List<string>();
            data_queue = new List<string>();
            stream_queue = new List<string>();


            Spam_Timers = new List<timer_info>();

            Spam_Check_Timer = new System.Timers.Timer();
            Spam_Check_Timer.Elapsed += spam_tick;
            Spam_Check_Timer.Interval = conf.spam_threshold;

            Spam_Threshold_Check = new System.Timers.Timer();
            Spam_Threshold_Check.Elapsed += spam_check;
            Spam_Threshold_Check.Interval = 100;

            checkRegisterationTimer = new System.Timers.Timer();
            checkRegisterationTimer.Elapsed += checkRegistration;
            checkRegisterationTimer.Interval = 120000;

            save_stream_worker = new BackgroundWorker();
            save_stream_worker.DoWork += new DoWorkEventHandler(save_stream);
            save_stream_worker.WorkerSupportsCancellation = true;

            check_connect_worker = new BackgroundWorker();
            check_connect_worker.DoWork += new DoWorkEventHandler(check_connection);
            check_connect_worker.WorkerSupportsCancellation = true;

            irc_worker = new BackgroundWorker();
            irc_worker.DoWork += new DoWorkEventHandler(IRCBot);
            irc_worker.WorkerSupportsCancellation = true;

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            worker.WorkerSupportsCancellation = true;
        }

        private bool connect()
        {
            connecting = true;
            bool bot_connected = false;
            try
            {
                IRCConnection = new TcpClient(conf.server_address, conf.port);
                IRCConnection.NoDelay = true;
                ns = IRCConnection.GetStream();
                sr = new StreamReader(ns, Encoding.UTF8);
                sw = new StreamWriter(ns, Encoding.UTF8);
                sw.AutoFlush = true;

                if (conf.pass != "")
                {
                    sendData("PASS", conf.pass);
                }
                if (conf.email != "")
                {
                    sendData("USER", nick + " " + conf.email + " " + conf.email + " :" + conf.name);
                }
                else
                {
                    sendData("USER", nick + " default_host default_server :" + conf.name);
                }
                disconnected = false;
                bot_connected = true;
            }
            catch (Exception ex)
            {
                lock (controller.errorlock)
                {
                    controller.log_error(ex, conf.logs_path);
                }
            }
            return bot_connected;
        }

        private void disconnect()
        {
            if (irc_worker.IsBusy)
            {
                irc_worker.CancelAsync();
            }

            save_stream_worker.CancelAsync();
            check_connect_worker.CancelAsync();

            disconnected = true;
            connected = false;
            connecting = false;
            shouldRun = false;

            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (ns != null)
                ns.Close();
            if (IRCConnection != null)
                IRCConnection.Close();

            string output = Environment.NewLine + conf.server + ":" + "Disconnected";

            lock (controller.listLock)
            {
                if (controller.queue_text.Count >= 1000)
                {
                    controller.queue_text.RemoveAt(0);
                }
                controller.queue_text.Add(output);
            }
        }

        internal void check_connection(object sender, DoWorkEventArgs e)
        {
            bool is_connected = true;
            BackgroundWorker bw = sender as BackgroundWorker;
            while (shouldRun && is_connected && !bw.CancellationPending)
            {
                Thread.Sleep(1000); 
                is_connected = NetworkInterface.GetIsNetworkAvailable();

                if (is_connected && shouldRun)
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        s.Connect(conf.server_address, conf.port);
                    }
                    catch
                    {
                        is_connected = false;
                    }
                }

                if (!is_connected)
                {
                    disconnect_count++;
                }
                else
                {
                    disconnect_count = 0;
                }

                if (disconnect_count >= 5 || !shouldRun)
                {
                    disconnect_count = 0;
                    restart = true;
                    shouldRun = false;
                }
            }
        }
       
        internal void load_modules()
        {
            module_list.Clear();
            modules_loaded.Clear();
            modules_error.Clear();
            foreach (List<string> module in conf.module_config)
            {
                bool module_loaded = false;
                string module_name = module[1];
                string class_name = module[0];

                module_loaded = load_module(class_name);

                //check to see if the class is instantiated or not
                if (module_loaded)
                {
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
                string output = Environment.NewLine + conf.server + ":Loaded Modules: " + msg.TrimStart(',').Trim();
                lock (controller.listLock)
                {
                    if (controller.queue_text.Count >= 1000)
                    {
                        controller.queue_text.RemoveAt(0);
                    }
                    controller.queue_text.Add(output);
                }
            }
            if (modules_error.Count > 0)
            {
                string msg = "";
                foreach (string module_name in modules_error)
                {
                    msg += ", " + module_name;
                }
                string output = Environment.NewLine + conf.server + ":Error Loading Modules: " + msg.TrimStart(',').Trim();
                lock (controller.listLock)
                {
                    if (controller.queue_text.Count >= 1000)
                    {
                        controller.queue_text.RemoveAt(0);
                    }
                    controller.queue_text.Add(output);
                }
            }
        }

        internal bool load_module(string class_name)
        {
            bool module_found = false;
            bool module_loaded = false;
            foreach (Modules.Module module in module_list)
            {
                if (module.ToString().Equals("Bot.Modules." + class_name))
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
                Type t = a.GetType("Bot.Modules." + class_name);

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

        internal bool unload_module(string class_name)
        {
            bool module_found = false;
            int index = 0;
            foreach (Modules.Module module in module_list)
            {
                if (module.ToString().Equals("Bot.Modules." + class_name))
                {
                    module_list.RemoveAt(index);
                    module_found = true;
                    break;
                }
                index++;
            }
            return module_found;
        }

        internal Modules.Module get_module(string class_name)
        {
            foreach (Modules.Module module in module_list)
            {
                if (module.ToString().Equals("Bot.Modules." + class_name))
                {
                    return module;
                }
            }
            return null;
        }

        private void start_timers()
        {
            checkRegisterationTimer.Start();

            Spam_Check_Timer.Start();

            Spam_Threshold_Check.Start();

            foreach (timer_info timer in Spam_Timers)
            {
                timer.spam_timer.Start();
            }

        }

        private void stop_timers()
        {
            checkRegisterationTimer.Stop();
            checkRegisterationTimer.Dispose();

            Spam_Check_Timer.Stop();
            Spam_Check_Timer.Dispose();

            Spam_Threshold_Check.Stop();
            Spam_Threshold_Check.Dispose();

            foreach (timer_info timer in Spam_Timers)
            {
                timer.spam_timer.Stop();
                timer.spam_timer.Dispose();
            }
        }

        internal void parse_stream(string data_line)
        {
            string[] ex;
            string type = "base";
            int nick_access = conf.user_level;
            string line_nick = "";
            string channel = "";
            string nick_host = "";
            bool bot_command = false;
            string command = "";
            restart = false;
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
                line_nick = name[0].TrimStart(':');
                nick_host = user_info[1];
                channel = ex[2].TrimStart(':');

                type = "line";
                // On Message Events events
                if (ex[1].Equals("privmsg", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (ex.GetUpperBound(0) >= 3) // If valid Input
                    {
                        command = ex[3].ToLower(); //grab the command sent
                        string msg_type = command.TrimStart(':');
                        if (msg_type.StartsWith(conf.command) == true)
                        {
                            bot_command = true;
                            command = command.Remove(0, 2);
                        }

                        if (ex[2].StartsWith("#") == true) // From Channel
                        {
                            nick_access = get_user_access(line_nick, channel);
                            type = "channel";
                        }
                        else // From Query
                        {
                            nick_access = get_user_access(line_nick, null);
                            type = "query";
                        }
                    }
                }

                // On JOIN events
                if (ex[1].Equals("join", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "join";
                    bool chan_found = false;
                    foreach (string tmp_channel in channel_list)
                    {
                        if (channel.Equals(tmp_channel, StringComparison.InvariantCultureIgnoreCase))
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
                        if (nick_list[x][0].Equals(channel.TrimStart(':'), StringComparison.InvariantCultureIgnoreCase))
                        {
                            bool nick_found = false;
                            chan_found = true;
                            int new_access = get_access_num(line_nick, false);
                            for (int i = 2; i < nick_list[x].Count(); i++)
                            {
                                string[] split = nick_list[x][i].Split(':');
                                if (split.GetUpperBound(0) > 0)
                                {
                                    if (split[1].Equals(line_nick, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        nick_found = true;
                                        int old_access = Convert.ToInt32(split[0]);
                                        bool identified = get_user_ident(line_nick);
                                        if (identified == true)
                                        {
                                            if (old_access > new_access)
                                            {
                                                new_access = old_access;
                                            }
                                        }
                                        nick_list[x][i] = new_access.ToString() + ":" + line_nick;
                                        break;
                                    }
                                }
                            }
                            if (nick_found == false)
                            {
                                nick_list[x].Add(new_access + ":" + line_nick);
                            }
                        }
                    }
                    if (chan_found == false)
                    {
                        bool channel_found = false;
                        List<string> tmp_list = new List<string>();
                        tmp_list.Add(channel.TrimStart(':'));
                        string line = "";
                        lock (queuelock)
                        {
                            sendData("WHO", channel.TrimStart(':'));
                            line = read_queue();
                            while (!line.Contains("352 " + nick + " " + channel))
                            {
                                line = read_queue();
                            }
                            tmp_list.Add(channel);
                            while (!line.Contains("315 " + nick + " " + channel + " :End of /WHO list."))
                            {
                                if (line.Contains("352 " + nick + " " + channel))
                                {
                                    channel_found = true;
                                    char[] sep = new char[] { ' ' };
                                    string[] name_line = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                                    char[] arr = name_line[8].ToCharArray();
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
                                    }
                                    tmp_list.Add(user_access + ":" + name_line[7].TrimStart('~'));
                                }
                                line = read_queue();
                            }
                            if (channel_found == true)
                            {
                                nick_list.Add(tmp_list);
                            }
                        }
                    }
                }

                // On user QUIT events
                if (ex[1].Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "quit";
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        for (int i = 2; i < nick_list[x].Count(); i++)
                        {
                            string[] split = nick_list[x][i].Split(':');
                            if (split[1].Equals(line_nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                nick_list[x].RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                // On user PART events
                if (ex[1].Equals("part", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "part";
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        if (nick_list[x][0].Equals(ex[2], StringComparison.InvariantCultureIgnoreCase))
                        {
                            for (int i = 2; i < nick_list[x].Count(); i++)
                            {
                                string[] split = nick_list[x][i].Split(':');
                                if (split[1].Equals(line_nick, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    nick_list[x].RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }

                // On user KICK events
                if (ex[1].Equals("kick", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "kick";
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        if (nick_list[x][0].Equals(ex[2], StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (ex[3].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                nick_list.RemoveAt(x);
                                channel_list.RemoveAt(x);
                            }
                            else
                            {
                                for (int i = 2; i < nick_list[x].Count(); i++)
                                {
                                    string[] split = nick_list[x][i].Split(':');
                                    if (split[1].Equals(line_nick, StringComparison.InvariantCultureIgnoreCase))
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
                if (ex[1].Equals("nick", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "nick";
                    for (int x = 0; x < nick_list.Count(); x++)
                    {
                        for (int i = 2; i < nick_list[x].Count(); i++)
                        {
                            string[] split = nick_list[x][i].Split(':');
                            if (split.GetUpperBound(0) > 0)
                            {
                                if (split[1].Equals(line_nick, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    nick_list[x][i] = split[0] + ":" + ex[2].TrimStart(':');
                                    break;
                                }
                            }
                        }
                    }
                }

                // On ChanServ Mode Change
                if (ex[1].Equals("mode", StringComparison.InvariantCultureIgnoreCase))
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
                                if (nick_list[x][0].Equals(ex[2], StringComparison.InvariantCultureIgnoreCase))
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
                                                    if (split[1].Equals(new_nick[y], StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        nick_list[x][i] = get_user_op(new_nick[y], channel).ToString() + ":" + new_nick[y];
                                                        break;
                                                    }
                                                }
                                            }
                                            new_access = get_user_access(new_nick[y], channel);
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
                                                if (split[1].Equals(new_nick[y], StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    nick_found = true;
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

            string[] ignored_nicks = conf.ignore_list.Split(',');
            bool run_modules = true;
            foreach (string ignore_nick in ignored_nicks)
            {
                if (ignore_nick.Equals(line_nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    run_modules = false;
                    break;
                }
            }
            if (run_modules)
            {
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
                        if (module.ToString().Equals("Bot.Modules." + conf_module[0]))
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
                                if (node.Equals(line_nick, StringComparison.InvariantCultureIgnoreCase) || node.TrimStart('#').Equals(channel.TrimStart('#'), StringComparison.InvariantCultureIgnoreCase))
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
                            work.DoWork += (sender, e) => backgroundWorker_RunModule(sender, e, this, module, index, ex, command, nick_access, line_nick, channel, bot_command, type);
                            work.RunWorkerAsync(2000);
                        }
                    }
                }
            }
        }

        private void backgroundWorker_RunModule(object sender, DoWorkEventArgs e, bot parent, Modules.Module module, int index, string[] ex, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            module.control(parent, conf, index, ex, command, nick_access, nick, channel, bot_command, type);
        }

        internal void sendData(string cmd, string param)
        {
            bool display_output = true;
            cmd = cmd.ToLower();
            if (sw != null)
            {
                if (cmd.Equals("msg", StringComparison.InvariantCultureIgnoreCase))
                {
                    cmd = "PRIVMSG";
                }
                if (cmd.Equals("join") || cmd.Equals("part") || cmd.Equals("quit") || cmd.Equals("kick") || cmd.Equals("nick") || cmd.Equals("notice"))
                {
                    display_output = false;
                }
                if (param == null)
                {
                    sw.WriteLine(cmd);
                    string output = Environment.NewLine + conf.server + ":" + ":" + nick + " " + cmd;

                    if (display_output)
                    {
                        lock (controller.listLock)
                        {
                            if (controller.queue_text.Count >= 1000)
                            {
                                controller.queue_text.RemoveAt(0);
                            }
                            controller.queue_text.Add(output);
                        }
                    }
                }
                else
                {
                    char[] separator = new char[] { ':' };
                    param = param.Replace(Environment.NewLine, " ");
                    string[] message = param.Split(separator, 2);
                    if (message.GetUpperBound(0) > 0)
                    {
                        string first = cmd + " " + message[0];
                        string second = message[1];
                        string[] stringSeparators = new string[] { "\n" };
                        string[] lines = second.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                        for (int x = 0; x <= lines.GetUpperBound(0); x++)
                        {
                            if ((first.Length + 1 + lines[x].Length) > conf.max_message_length)
                            {
                                string msg = "";
                                string[] par = lines[x].Split(' ');
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
                                        string output = Environment.NewLine + conf.server + ":" + ":" + nick + " " + first + ":" + msg;
                                        if (display_output)
                                        {
                                            lock (controller.listLock)
                                            {
                                                if (controller.queue_text.Count >= 1000)
                                                {
                                                    controller.queue_text.RemoveAt(0);
                                                }
                                                controller.queue_text.Add(output);
                                            }
                                        }
                                        msg = " " + word;
                                    }
                                }
                                if (msg.Trim() != "")
                                {
                                    msg = msg.Remove(0, 1);
                                    sw.WriteLine(first + ":" + msg);
                                    string output = Environment.NewLine + conf.server + ":" + ":" + nick + " " + first + ":" + msg;
                                    if (display_output)
                                    {
                                        lock (controller.listLock)
                                        {
                                            if (controller.queue_text.Count >= 1000)
                                            {
                                                controller.queue_text.RemoveAt(0);
                                            }
                                            controller.queue_text.Add(output);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                sw.WriteLine(first + ":" + lines[x]);
                                string output = Environment.NewLine + conf.server + ":" + ":" + nick + " " + first + ":" + lines[x];

                                if (display_output)
                                {
                                    lock (controller.listLock)
                                    {
                                        if (controller.queue_text.Count >= 1000)
                                        {
                                            controller.queue_text.RemoveAt(0);
                                        }
                                        controller.queue_text.Add(output);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string[] stringSeparators = new string[] { "\n" };
                        string[] lines = param.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                        for (int x = 0; x <= lines.GetUpperBound(0); x++)
                        {
                            sw.WriteLine(cmd + " " + lines[x]);
                            string output = Environment.NewLine + conf.server + ":" + ":" + nick + " " + cmd + " " + lines[x];

                            if (display_output)
                            {
                                lock (controller.listLock)
                                {
                                    if (controller.queue_text.Count >= 1000)
                                    {
                                        controller.queue_text.RemoveAt(0);
                                    }
                                    controller.queue_text.Add(output);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void save_stream(Object sender, EventArgs e)
        {
            int blank_count = 0;
            while (shouldRun && !worker.CancellationPending)
            {
                try
                {
                    if (sr != null)
                    {
                        string line = sr.ReadLine();

                        if (line.Equals(string.Empty))
                        {
                            blank_count++;
                        }
                        else
                        {
                            lock (streamlock)
                            {
                                string output = Environment.NewLine + conf.server + ":" + line;
                                lock (controller.listLock)
                                {
                                    if (controller.queue_text.Count >= 1000)
                                    {
                                        controller.queue_text.RemoveAt(0);
                                    }
                                    controller.queue_text.Add(output);
                                }

                                data_queue.Add(line);
                                stream_queue.Add(line);
                            }
                        }
                        if (blank_count >= 5)
                        {
                            restart = true;
                            shouldRun = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (controller.errorlock)
                    {
                        controller.log_error(ex, conf.logs_path);
                    }
                    restart = true;
                    shouldRun = false;
                }
            }
        }

        internal string read_stream_queue()
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
            if (response == null)
            {
                response = "";
            }
            return response;
        }

        internal string read_queue()
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
            if (response == null)
            {
                response = "";
            }
            return response;
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
                        if (channel.Equals(channels_blacklist[i], StringComparison.InvariantCultureIgnoreCase))
                        {
                            chan_blocked = true;
                            break;
                        }
                    }
                    if (chan_blocked == false)
                    {
                        sendData("JOIN", channel);
                    }
                }
            }
        }

        private void checkRegistration(object sender, EventArgs e)
        {
            if (connected == true)
            {
                checkRegisterationTimer.Enabled = false;
                if (nick != "" && conf.pass != "" && conf.email != "")
                {
                    register_nick(conf.pass, conf.email);
                }
                else
                {
                    string output = Environment.NewLine + conf.server + ":You are missing an username and/or password.  Please add those to the server configuration so I can register this nick.";

                    lock (controller.listLock)
                    {
                        if (controller.queue_text.Count >= 1000)
                        {
                            controller.queue_text.RemoveAt(0);
                        }
                        controller.queue_text.Add(output);
                    }
                }
            }
        }

        private void spam_tick(object sender, EventArgs e)
        {
            lock (spamlock)
            {
                List<int> spam_index = new List<int>();
                int index = 0;
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.count < conf.spam_count_max)
                    {
                        spam_index.Add(index);
                    }
                    index++;
                }
                foreach (int x in spam_index)
                {
                    if (conf.spam_check.Count() <= x)
                    {
                        conf.spam_check.RemoveAt(x);
                    }
                }
            }
        }

        private void spam_check(object sender, EventArgs e)
        {
            lock (spamlock)
            {
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.count > conf.spam_count_max)
                    {
                        if (!spam.activated)
                        {
                            System.Timers.Timer new_timer = new System.Timers.Timer();
                            new_timer.Interval = conf.spam_timeout;
                            new_timer.Elapsed += (new_sender, new_e) => spam_deactivate(new_sender, new_e, spam.channel);
                            new_timer.Enabled = true;
                            timer_info tmp_timer = new timer_info();
                            tmp_timer.channel = spam.channel;
                            tmp_timer.spam_timer = new_timer;
                            lock (timerlock)
                            {
                                Spam_Timers.Add(tmp_timer);
                            }
                            spam.activated = true;
                            spam.count++;
                            Spam_Timers[Spam_Timers.Count - 1].spam_timer.Start();
                        }
                    }
                }
            }
        }

        internal void spam_deactivate(object sender, EventArgs e, string channel)
        {
            lock (spamlock)
            {
                int index = 0;
                bool spam_found = false;
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.activated && spam.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        spam_found = true;
                        break;
                    }
                    index++;
                }
                if (spam_found)
                {
                    conf.spam_check.RemoveAt(index);
                }
            }
            lock (timerlock)
            {
                int index = 0;
                bool spam_found = false;
                foreach (timer_info spam in Spam_Timers)
                {
                    if (spam.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        spam_found = true;
                        break;
                    }
                    index++;
                }
                if (spam_found)
                {
                    Spam_Timers[index].spam_timer.Stop();
                    Spam_Timers.RemoveAt(index);
                }
            }
        }

        internal void add_spam_count(string channel)
        {
            lock (spamlock)
            {
                bool spam_added = false;
                bool spam_found = false;
                int index = 0;
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (spam.count > conf.spam_count_max + 1)
                        {
                            spam_added = true;
                        }
                        spam_found = true;
                        break;
                    }
                    index++;
                }
                if (!spam_added && spam_found)
                {
                    conf.spam_check[index].count++;
                }
                else if (!spam_found)
                {
                    spam_info new_spam = new spam_info();
                    new_spam.channel = channel;
                    new_spam.activated = false;
                    new_spam.count = 1;
                    conf.spam_check.Add(new_spam);
                }
            }
        }

        internal bool get_spam_status(string channel)
        {
            bool active = false;
            lock (spamlock)
            {
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (spam.activated)
                        {
                            active = true;
                        }
                        break;
                    }
                }
            }
            return active;
        }

        internal bool get_spam_check(string channel, string nick, bool enabled)
        {
            bool active = conf.spam_enable;
            if (active)
            {
                foreach (string part in conf.spam_ignore.Split(','))
                {
                    if (part.Equals(channel, StringComparison.InvariantCultureIgnoreCase) || part.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                    {
                        active = false;
                    }
                }
            }
            return active;
        }

        private void register_nick(string password, string email)
        {
            sendData("PRIVMSG", "NickServ :register " + password + " " + email);
        }

        internal string get_user_host(string tmp_nick)
        {
            string access = "";
            string line = "";
            //sendData("ISON", nick);
            //line = sr.ReadLine();
            string[] new_nick = tmp_nick.Split(' ');
            lock (queuelock)
            {
                sendData("USERHOST ", new_nick[0]);
                line = read_queue();
                while (line == "")
                {
                    line = read_queue();
                }
                char[] charSeparator = new char[] { ' ' };
                string[] name_line = line.Split(charSeparator);

                while (name_line.GetUpperBound(0) < 3 || name_line[2] != nick)
                {
                    line = read_queue();
                    name_line = line.Split(charSeparator);
                }
                if (name_line[3] != ":")
                {
                    string[] strSplit = new string[] { "=" };
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

        internal int get_access_num(string type, bool letter_mode)
        {
            int access = conf.default_level;
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

        internal bool get_user_auto(string type, string channel, string tmp_nick)
        {
            bool auto = true;
            string line = "";
            lock (queuelock)
            {
                sendData("PRIVMSG", "chanserv :" + type + " " + channel + " list " + tmp_nick);
                line = read_queue();
                bool cont = true;
                while (cont)
                {
                    if (line.Contains("No matching entries on " + channel + " " + type + " list.") || line.Contains(channel + " " + type + " list is empty."))
                    {
                        auto = false;
                        cont = false;
                    }
                    else if (line.Contains(type + " list for " + channel))
                    {
                        auto = true;
                        cont = false;
                    }
                    else
                    {
                        cont = true;
                        line = read_queue();
                    }
                }
            }
            return auto;
        }

        internal bool get_user_ident(string tmp_nick)
        {
            bool identified = false;
            string line = "";
            lock (queuelock)
            {
                sendData("PRIVMSG", "nickserv :STATUS " + tmp_nick);
                line = read_queue();
                while (!line.Contains(":STATUS"))
                {
                    line = read_queue();
                }
                if (line.Contains(":STATUS " + tmp_nick + " 3"))
                {
                    identified = true;
                }
            }
            return identified;
        }

        internal int get_user_op(string tmp_nick, string channel)
        {
            int new_access = conf.default_level;
            string line = "";
            lock (queuelock)
            {
                sendData("WHO", channel.TrimStart(':'));
                line = read_queue();
                while (!line.Contains("352 " + nick + " " + channel))
                {
                    line = read_queue();
                }
                char[] Separator = new char[] { ' ' };
                while (!line.Contains("315 " + nick + " " + channel + ":End of /WHO list."))
                {
                    if (line.Contains("352 " + nick + " " + channel))
                    {
                        string[] name_line = line.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                        if (name_line.GetUpperBound(0) >= 8)
                        {
                            if (name_line[7].TrimStart('~').Equals(tmp_nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                bool char_found = false;
                                char[] arr = name_line[8].ToCharArray();
                                foreach (char c in arr)
                                {
                                    if (c.Equals('~') || c.Equals('&') || c.Equals('@') || c.Equals('%') || c.Equals('+'))
                                    {
                                        int tmp_access = get_access_num(c.ToString(), false);
                                        char_found = true;
                                        if (tmp_access > new_access)
                                        {
                                            new_access = tmp_access;
                                        }
                                    }
                                }
                                if (!char_found)
                                {
                                    new_access = conf.user_level;
                                }
                                break;
                            }
                        }
                    }
                    line = read_queue();
                }
            }
            return new_access;
        }

        internal int get_user_access(string tmp_nick, string channel)
        {
            int access_num = conf.default_level;
            try
            {
                string access = access_num.ToString();
                string tmp_custom_access = "";
                if (tmp_nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    access = conf.owner_level.ToString();
                }
                bool user_identified = get_user_ident(tmp_nick);
                if (user_identified == true)
                {
                    for (int x = 0; x < conf.module_config.Count(); x++)
                    {
                        if (conf.module_config[x][0].Equals("access"))
                        {
                            bool chan_allowed = true;
                            foreach (string blacklist in conf.module_config[x][2].Split(','))
                            {
                                if (blacklist.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    chan_allowed = false;
                                    break;
                                }
                            }
                            if (chan_allowed)
                            {
                                if (channel != null)
                                {
                                    Modules.access acc = new Modules.access();
                                    tmp_custom_access = acc.get_access_list(tmp_nick, channel, this);
                                    access = tmp_custom_access;
                                }
                            }
                            break;
                        }
                    }
                }
                for (int x = 0; x < nick_list.Count(); x++)
                {
                    if (nick_list[x][0].Equals(channel, StringComparison.InvariantCultureIgnoreCase) || channel == null)
                    {
                        for (int i = 2; i < nick_list[x].Count(); i++)
                        {
                            string[] lists = nick_list[x][i].Split(':');
                            if (lists.GetUpperBound(0) > 0)
                            {
                                if (lists[1].Equals(tmp_nick, StringComparison.InvariantCultureIgnoreCase))
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
                        if (tmp_nick.Equals(owners[x], StringComparison.InvariantCultureIgnoreCase))
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
                if (access_num == conf.default_level && channel != null)
                {
                    bool nick_found = false;
                    access_num = get_user_op(tmp_nick, channel);
                    if (access_num != conf.default_level)
                    {
                        for (int x = 0; x < nick_list.Count(); x++)
                        {
                            if (nick_list[x][0].Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                            {
                                for (int i = 2; i < nick_list[x].Count(); i++)
                                {
                                    string[] lists = nick_list[x][i].Split(':');
                                    if (lists.GetUpperBound(0) > 0)
                                    {
                                        if (lists[1].Equals(tmp_nick, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            nick_found = true;
                                            string new_nick = access_num.ToString();
                                            for (int z = 1; z < lists.Count(); z++)
                                            {
                                                new_nick += ":" + lists[z];
                                            }
                                            nick_list[x][i] = new_nick;
                                            break;
                                        }
                                    }
                                }
                                if (nick_found == false)
                                {
                                    nick_list[x].Add(access_num + ":" + tmp_nick);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lock (controller.errorlock)
                {
                    controller.log_error(ex, conf.logs_path);
                }
            }
            return access_num;
        }
    }

    public class BotConfig
    {
        private string Server;
        public string server 
        { 
            get
            {
                return Server;
            }

            internal set
            {
                Server = value;
            } 
        }

        private string Server_Address;
        public string server_address
        {
            get
            {
                return Server_Address;
            }

            internal set
            {
                Server_Address = value;
            }
        }

        private IPAddress[] Server_IP;
        public IPAddress[] server_ip
        {
            get
            {
                return Server_IP;
            }

            internal set
            {
                Server_IP = value;
            }
        }

        private string Chans;
        public string chans
        {
            get
            {
                return Chans;
            }

            internal set
            {
                Chans = value;
            }
        }

        private string Chan_Blacklist;
        public string chan_blacklist
        {
            get
            {
                return Chan_Blacklist;
            }

            internal set
            {
                Chan_Blacklist = value;
            }
        }

        private string Ignore_List;
        public string ignore_list
        {
            get
            {
                return Ignore_List;
            }

            internal set
            {
                Ignore_List = value;
            }
        }

        private int Port;
        public int port
        {
            get
            {
                return Port;
            }

            internal set
            {
                Port = value;
            }
        }

        private string Nick;
        public string nick
        {
            get
            {
                return Nick;
            }

            internal set
            {
                Nick = value;
            }
        }

        private string Secondary_Nicks;
        public string secondary_nicks
        {
            get
            {
                return Secondary_Nicks;
            }

            internal set
            {
                Secondary_Nicks = value;
            }
        }

        private string Pass;
        public string pass
        {
            get
            {
                return Pass;
            }

            internal set
            {
                Pass = value;
            }
        }

        private string Email;
        public string email
        {
            get
            {
                return Email;
            }

            internal set
            {
                Email = value;
            }
        }

        private string Name;
        public string name
        {
            get
            {
                return Name;
            }

            internal set
            {
                Name = value;
            }
        }

        private string Owner;
        public string owner
        {
            get
            {
                return Owner;
            }

            internal set
            {
                Owner = value;
            }
        }

        private int Default_Level;
        public int default_level
        {
            get
            {
                return Default_Level;
            }

            internal set
            {
                Default_Level = value;
            }
        }

        private int User_Level;
        public int user_level
        {
            get
            {
                return User_Level;
            }

            internal set
            {
                User_Level = value;
            }
        }

        private int Voice_Level;
        public int voice_level
        {
            get
            {
                return Voice_Level;
            }

            internal set
            {
                Voice_Level = value;
            }
        }

        private int Hop_Level;
        public int hop_level
        {
            get
            {
                return Hop_Level;
            }

            internal set
            {
                Hop_Level = value;
            }
        }

        private int Op_Level;
        public int op_level
        {
            get
            {
                return Op_Level;
            }

            internal set
            {
                Op_Level = value;
            }
        }

        private int Sop_Level;
        public int sop_level
        {
            get
            {
                return Sop_Level;
            }

            internal set
            {
                Sop_Level = value;
            }
        }

        private int Founder_Level;
        public int founder_level
        {
            get
            {
                return Founder_Level;
            }

            internal set
            {
                Founder_Level = value;
            }
        }

        private int Owner_Level;
        public int owner_level
        {
            get
            {
                return Owner_Level;
            }

            internal set
            {
                Owner_Level = value;
            }
        }

        private bool Auto_Connect;
        public bool auto_connect
        {
            get
            {
                return Auto_Connect;
            }

            internal set
            {
                Auto_Connect = value;
            }
        }

        private string Command;
        public string command
        {
            get
            {
                return Command;
            }

            internal set
            {
                Command = value;
            }
        }

        private bool Spam_Enable;
        public bool spam_enable
        {
            get
            {
                return Spam_Enable;
            }

            internal set
            {
                Spam_Enable = value;
            }
        }

        private string Spam_Ignore;
        public string spam_ignore
        {
            get
            {
                return Spam_Ignore;
            }

            internal set
            {
                Spam_Ignore = value;
            }
        }

        private int Spam_Count_Max;
        public int spam_count_max
        {
            get
            {
                return Spam_Count_Max;
            }

            internal set
            {
                Spam_Count_Max = value;
            }
        }

        private int Spam_Threshold;
        public int spam_threshold
        {
            get
            {
                return Spam_Threshold;
            }

            internal set
            {
                Spam_Threshold = value;
            }
        }

        private int Spam_Timeout;
        public int spam_timeout
        {
            get
            {
                return Spam_Timeout;
            }

            internal set
            {
                Spam_Timeout = value;
            }
        }

        private int Max_Message_Length;
        public int max_message_length
        {
            get
            {
                return Max_Message_Length;
            }

            internal set
            {
                Max_Message_Length = value;
            }
        }

        private string Keep_Logs;
        public string keep_logs
        {
            get
            {
                return Keep_Logs;
            }

            internal set
            {
                Keep_Logs = value;
            }
        }

        private string Logs_Path;
        public string logs_path
        {
            get
            {
                return Logs_Path;
            }

            internal set
            {
                Logs_Path = value;
            }
        }

        private List<List<string>> Module_Config;
        public List<List<string>> module_config
        {
            get
            {
                return Module_Config;
            }

            internal set
            {
                Module_Config = value;
            }
        }

        private List<List<string>> Command_List;
        public List<List<string>> command_list
        {
            get
            {
                return Command_List;
            }

            internal set
            {
                Command_List = value;
            }
        }

        private List<spam_info> Spam_Check;
        public List<spam_info> spam_check
        {
            get
            {
                return Spam_Check;
            }

            internal set
            {
                Spam_Check = value;
            }
        }
    }
}

public class spam_info
{
    private string Channel;
    public string channel
    {
        get
        {
            return Channel;
        }

        internal set
        {
            Channel = value;
        }
    }

    private int Count;
    public int count
    {
        get
        {
            return Count;
        }

        internal set
        {
            Count = value;
        }
    }

    private bool Activated;
    public bool activated
    {
        get
        {
            return Activated;
        }

        internal set
        {
            Activated = value;
        }
    }
}

public class timer_info
{
    private string Channel;
    public string channel
    {
        get
        {
            return Channel;
        }

        internal set
        {
            Channel = value;
        }
    }

    private System.Timers.Timer Spam_Timer;
    public System.Timers.Timer spam_timer
    {
        get
        {
            return Spam_Timer;
        }

        internal set
        {
            Spam_Timer = value;
        }
    }

}
