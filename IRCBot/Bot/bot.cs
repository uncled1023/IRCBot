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
        private BotConfig conf;
        public BotConfig Conf
        {
            get
            {
                return conf;
            }

            internal set
            {
                conf = value;
            }
        }

        private string nick;
        public string Nick
        {
            get
            {
                return nick;
            }

            internal set
            {
                nick = value;
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
        internal string cur_dir;
        internal BackgroundWorker worker;
        internal BackgroundWorker irc_worker;
        internal BackgroundWorker save_stream_worker;
        internal BackgroundWorker check_connect_worker;
        internal List<string> modules_loaded;
        internal List<string> modules_disabled;
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
            Conf = new_conf;

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
                    controller.log_error(ex, Conf.Logs_Path);
                }
            }
            e.Cancel = true;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (restart == true)
            {
                string output = Environment.NewLine + Conf.Server_Name + ":" + "Restart Attempt " + restart_attempts + " [" + Math.Pow(2, Convert.ToDouble(restart_attempts)) + " Seconds Delay]";
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
                if (Conf.Server_Name.Equals("No_Server_Specified"))
                {
                    string output = Environment.NewLine + Conf.Server_Name + ":" + "Please add a server to connect to.";
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
            check_connect_worker.RunWorkerAsync();

            shouldRun = true;
            string data = "";
            
            bool ghost_sent = false;
            string main_nick = nick;
            int cur_alt_nick = 0;
            char[] sep = new char[] { ',' };
            string[] nicks = Conf.Secondary_Nicks.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            int bot_state = 0;
            int pre_state = 0;
            int next_state = 1;
            string line = "";
            char[] charSeparator = new char[] { ' ' };
            string[] ex = new string[2];

            while (shouldRun)
            {
                Thread.Sleep(10);
                line = read_queue();
                switch (bot_state)
                {
                    case 0: // Identify with the server
                        sendData("NICK", nick);
                        bot_state = next_state;
                        break;
                    case 1: // wait until end of MOTD
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
                        else if (line.Contains("You have not registered"))
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
                        if (!String.IsNullOrEmpty(Conf.Pass))
                        {
                            sendData("PRIVMSG", "NickServ :ghost " + main_nick + " " + Conf.Pass);
                        }
                        bot_state = pre_state;
                        break;
                    case 4: // identify state
                        if (!Conf.Pass.Equals(string.Empty))
                        {
                            sendData("PRIVMSG", "NickServ :Identify " + Conf.Pass);
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
                        else if (line.Contains("You have not registered"))
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
                        break;
                    case 6:
                        Task join_channels = new Task(() => joinChannels());
                        join_channels.Start();
                        bot_state = -1; // go to default case
                        break;
                    default:
                        connected = true;
                        disconnected = false;
                        restart_attempts = 0;
                        data = read_stream_queue();
                        if (!String.IsNullOrEmpty(data.Trim()))
                        {
                            string parse_string = data.Trim();
                            parse_stream(parse_string);
                        }
                        break;
                }
                ex = line.Split(charSeparator, 2, StringSplitOptions.RemoveEmptyEntries);
                if (ex.GetUpperBound(0) > 0)
                {
                    if (ex[0] == "PING")
                    {
                        sendData("PONG", ex[1]);
                    }
                }
                /*

                if (!sent_ping && DateTime.Now.Subtract(start).TotalSeconds > 30)
                {
                    seconds_start = Convert.ToInt64((start.ToUniversalTime() - epoch).TotalSeconds);
                    start = DateTime.Now;
                    sendData("PING", seconds_start.ToString());
                    sent_ping = true;
                }

                if (sent_ping)
                {
                    if (DateTime.Now.Subtract(start).TotalSeconds > 15)
                    {
                        shouldRun = false;
                    }
                    else
                    {
                        if (ex.GetUpperBound(0) > 0)
                        {
                            string[] newex = ex[1].Split(charSeparator, StringSplitOptions.RemoveEmptyEntries);
                            if (newex.GetUpperBound(0) > 1)
                            {
                                if (newex[0] == "PONG" && newex[2].Equals(":" + seconds_start.ToString()))
                                {
                                    start = DateTime.Now;
                                    sent_ping = false;
                                }
                            }
                        }
                    }   
                }
                */
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
            restart = false;
            start_time = DateTime.Now;
            cur_dir = controller.cur_dir;
            nick = Conf.Nick;
            modules_loaded = new List<string>();
            modules_disabled = new List<string>();
            modules_error = new List<string>();
            data_queue = new List<string>();
            stream_queue = new List<string>();


            Spam_Timers = new List<timer_info>();

            Spam_Check_Timer = new System.Timers.Timer();
            Spam_Check_Timer.Elapsed += spam_tick;
            Spam_Check_Timer.Interval = Conf.Spam_Threshold;

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
                IRCConnection = new TcpClient(Conf.Server_Address, Conf.Port);
                IRCConnection.NoDelay = true;
                ns = IRCConnection.GetStream();
                sr = new StreamReader(ns, Encoding.UTF8);
                sw = new StreamWriter(ns, Encoding.UTF8);
                sw.AutoFlush = true;

                // Register with Server
                sendData("NICK", nick);
                Thread.Sleep(30);
                sendData("USER", nick + " * * :" + Conf.Name);

                // Start the stream readers
                save_stream_worker.RunWorkerAsync();

                disconnected = false;
                bot_connected = true;
            }
            catch (Exception ex)
            {
                lock (controller.errorlock)
                {
                    controller.log_error(ex, Conf.Logs_Path);
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

            string output = Environment.NewLine + Conf.Server_Name + ":" + "Disconnected";

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
                        s.Connect(Conf.Server_Address, Conf.Port);
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
            Conf.Modules.Clear();
            modules_loaded.Clear();
            modules_disabled.Clear();
            modules_error.Clear();
            List<string> module_list = controller.get_module_list(Conf.Server_Name);
            foreach (string module_class_name in module_list)
            {
                bool module_loaded = false;

                module_loaded = load_module(module_class_name);

                //check to see if the class is instantiated or not
                if (!module_loaded)
                {
                    modules_error.Add(module_class_name);
                }
            }
            if (modules_loaded.Count > 0)
            {
                string msg = "";
                foreach (string module_name in modules_loaded)
                {
                    msg += ", " + module_name;
                }
                string output = Environment.NewLine + Conf.Server_Name + ":Loaded Modules: " + msg.TrimStart(',').Trim();
                lock (controller.listLock)
                {
                    if (controller.queue_text.Count >= 1000)
                    {
                        controller.queue_text.RemoveAt(0);
                    }
                    controller.queue_text.Add(output);
                }
            }
            if (modules_disabled.Count > 0)
            {
                string msg = "";
                foreach (string module_name in modules_disabled)
                {
                    msg += ", " + module_name;
                }
                string output = Environment.NewLine + Conf.Server_Name + ":Disabled Modules: " + msg.TrimStart(',').Trim();
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
                string output = Environment.NewLine + Conf.Server_Name + ":Error Loading Modules: " + msg.TrimStart(',').Trim();
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
            bool module_loaded = false;
            foreach (Modules.Module module in Conf.Modules)
            {
                if (module.Loaded && module.Class_Name.Equals(class_name))
                {
                    module_loaded = true;
                    break;
                }
            }
            if (!module_loaded)
            {
                Modules.Module module = controller.get_module_conf(Conf.Server_Name, class_name);
                if (module.Enabled)
                {
                    //create the class base on string
                    //note : include the namespace and class name (namespace=Bot.Modules, class name=<class_name>)
                    Assembly a = Assembly.Load("IRCBot");
                    Type t = a.GetType("Bot.Modules." + class_name);

                    //check to see if the class is instantiated or not
                    if (t != null)
                    {
                        Modules.Module new_module = (Modules.Module)Activator.CreateInstance(t);
                        new_module.Loaded = true;
                        new_module.Blacklist = module.Blacklist;
                        new_module.Class_Name = module.Class_Name;
                        new_module.Commands = module.Commands;
                        new_module.Enabled = module.Enabled;
                        new_module.Name = module.Name;
                        new_module.Options = module.Options;
                        Conf.Modules.Add(new_module);

                        modules_loaded.Add(module.Name);
                        module_loaded = true;
                    }
                }
                else
                {
                    modules_disabled.Add(module.Name);
                    module_loaded = true;
                }
            }
            return module_loaded;
        }

        internal bool unload_module(string class_name)
        {
            bool module_found = false;
            int index = 0;
            foreach (Modules.Module module in Conf.Modules)
            {
                if (module.ToString().Equals("Bot.Modules." + class_name))
                {
                    Conf.Modules.RemoveAt(index);
                    module_found = true;
                    break;
                }
                index++;
            }
            return module_found;
        }

        internal Modules.Module get_module(string class_name)
        {
            foreach (Modules.Module module in Conf.Modules)
            {
                if (module.Class_Name.Equals(class_name))
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
                timer.Spam_Timer.Start();
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
                timer.Spam_Timer.Stop();
                timer.Spam_Timer.Dispose();
            }
        }

        internal void parse_stream(string data_line)
        {
            string[] ex;
            string type = "base";
            int nick_access = Conf.User_Level;
            string line_nick = "";
            string channel = "";
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
                channel = ex[2].TrimStart(':');

                type = "line";
                // On Message Events events
                if (ex[1].Equals("privmsg", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (ex.GetUpperBound(0) >= 3) // If valid Input
                    {
                        command = ex[3].ToLower(); //grab the command sent
                        string msg_type = command.TrimStart(':');
                        if (msg_type.StartsWith(Conf.Command) == true)
                        {
                            bot_command = true;
                            command = command.Remove(0, 2);
                        }

                        if (ex[2].StartsWith("#") == true) // From Channel
                        {
                            nick_access = get_nick_access(line_nick, channel);
                            type = "channel";
                        }
                        else // From Query
                        {
                            foreach (Channel_Info chan in Conf.Channel_List)
                            {
                                int tmp_nick_access = get_nick_access(line_nick, chan.Channel);
                                if (tmp_nick_access > nick_access)
                                {
                                    nick_access = tmp_nick_access;
                                }
                            }
                            channel = line_nick;
                            type = "query";
                        }
                    }
                }

                // On Invite events
                if (ex[1].Equals("invite", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "invite";
                }

                // On JOIN events
                if (ex[1].Equals("join", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "join";
                    Nick_Info info = get_nick_info(line_nick, channel);
                    if (info == null)
                    {
                        Channel_Info chan_info = get_chan_info(channel);
                        if (chan_info == null)
                        {
                            add_chan_info(channel);
                        }
                        info = new Nick_Info();
                        info.Nick = line_nick;
                        info.Access = get_nick_chan_access(line_nick, channel);
                        add_nick_info(info, channel);
                    }
                }

                // On user QUIT events
                if (ex[1].Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "quit";
                    if (line_nick.Equals(Nick, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Conf.Channel_List.Clear();
                    }
                    else
                    {
                        foreach (Channel_Info chan_info in Conf.Channel_List)
                        {
                            Nick_Info info = get_nick_info(line_nick, chan_info.Channel);
                            if (info != null)
                            {
                                del_nick_info(info, chan_info.Channel);
                            }
                        }
                    }
                }

                // On user PART events
                if (ex[1].Equals("part", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "part";
                    if (line_nick.Equals(Nick, StringComparison.InvariantCultureIgnoreCase))
                    {
                        del_chan_info(channel);
                    }
                    else
                    {
                        Nick_Info info = get_nick_info(line_nick, channel);
                        if (info != null)
                        {
                            del_nick_info(info, channel);
                        }
                    }
                }

                // On user KICK events
                if (ex[1].Equals("kick", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "kick";
                    if (ex[3].Equals(Nick, StringComparison.InvariantCultureIgnoreCase))
                    {
                        del_chan_info(channel);
                    }
                    else
                    {
                        Nick_Info info = get_nick_info(ex[3], channel);
                        if (info != null)
                        {
                            del_nick_info(info, channel);
                        }
                    }
                }

                // On user Nick Change events
                if (ex[1].Equals("nick", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "nick";
                    foreach (Channel_Info chan_info in Conf.Channel_List)
                    {
                        foreach (Nick_Info nick_info in chan_info.Nicks)
                        {
                            if (nick_info.Nick.Equals(line_nick))
                            {
                                nick_info.Nick = channel;
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
                        char[] arr = ex[3].ToCharArray();
                        string[] change_nicks = ex[4].Split(charSeparator, StringSplitOptions.RemoveEmptyEntries);
                        int nick_index = 0;
                        bool add_sub = true;
                        foreach (char c in arr)
                        {
                            if (c.Equals('+'))
                            {
                                add_sub = true;
                            }
                            else if (c.Equals('-'))
                            {
                                add_sub = false;
                            }
                            else if (c.Equals('q') || c.Equals('a') || c.Equals('o') || c.Equals('h') || c.Equals('v'))
                            {
                                Nick_Info nick_info = get_nick_info(change_nicks[nick_index], channel);
                                if (nick_info != null)
                                {
                                    int change_access = get_access_num(c.ToString(), true);
                                    if (add_sub)
                                    {
                                        if (change_access > nick_info.Access)
                                        {
                                            nick_info.Access = change_access;
                                        }
                                    }
                                    else
                                    {
                                        nick_info.Access = get_nick_chan_access(change_nicks[nick_index], channel);
                                    }
                                }
                                else
                                {
                                    Nick_Info info = new Nick_Info();
                                    info.Nick = line_nick;
                                    if (add_sub)
                                    {
                                        info.Access = get_access_num(c.ToString(), true);
                                    }
                                    else
                                    {
                                        info.Access = get_nick_chan_access(change_nicks[nick_index], channel);
                                    }
                                    add_nick_info(info, channel);
                                }
                                nick_index++;
                            }
                            else if (c.Equals('p') || c.Equals('s'))
                            {
                                Channel_Info chan_info = get_chan_info(channel);
                                if (chan_info != null)
                                {
                                    if (add_sub)
                                    {
                                        chan_info.Show = false;
                                    }
                                    else
                                    {
                                        chan_info.Show = true;
                                    }
                                }
                            }
                        }
                    }
                    else if(ex.GetUpperBound(0) > 2)
                    {
                        char[] arr = ex[3].ToCharArray();
                        string mod_nick = ex[2];
                        bool add_sub = true;
                        foreach (char c in arr)
                        {
                            if (c.Equals('+'))
                            {
                                add_sub = true;
                            }
                            else if (c.Equals('-'))
                            {
                                add_sub = false;
                            }
                            else if (c.Equals('q') || c.Equals('a') || c.Equals('o') || c.Equals('h') || c.Equals('v'))
                            {
                                Nick_Info nick_info = get_nick_info(mod_nick, channel);
                                if (nick_info != null)
                                {
                                    int change_access = get_access_num(c.ToString(), true);
                                    if (add_sub)
                                    {
                                        if (change_access > nick_info.Access)
                                        {
                                            nick_info.Access = change_access;
                                        }
                                    }
                                    else
                                    {
                                        nick_info.Access = get_nick_chan_access(mod_nick, channel);
                                    }
                                }
                                else
                                {
                                    Nick_Info info = new Nick_Info();
                                    info.Nick = line_nick;
                                    if (add_sub)
                                    {
                                        info.Access = get_access_num(c.ToString(), true);
                                    }
                                    else
                                    {
                                        info.Access = get_nick_chan_access(mod_nick, channel);
                                    }
                                    add_nick_info(info, channel);
                                }
                            }
                        }
                    }
                }
            }

            string[] ignored_nicks = Conf.Ignore_List.Split(',');
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
                foreach (Modules.Module module in Conf.Modules)
                {
                    bool module_allowed = !module.Blacklist.Contains(line_nick) && !module.Blacklist.Contains(channel);
                    if (module_allowed == true)
                    {
                        Task.Factory.StartNew(() => RunModule(this, module, ex, command, nick_access, line_nick, channel, bot_command, type), TaskCreationOptions.LongRunning);
                    }
                }
            }
        }

        private void RunModule(bot parent, Modules.Module module, string[] ex, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            module.control(parent, Conf, ex, command, nick_access, nick, channel, bot_command, type);
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
                if (param == null)
                {
                    sw.WriteLine(cmd + Environment.NewLine);
                    string output = Environment.NewLine + Conf.Server_Name + ":" + ":" + nick + " " + cmd;

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
                        string[] stringSeparators = new string[] { Environment.NewLine };
                        string[] lines = second.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                        for (int x = 0; x <= lines.GetUpperBound(0); x++)
                        {
                            if ((first.Length + 1 + lines[x].Length) > Conf.Max_Message_Length)
                            {
                                string msg = "";
                                string[] par = lines[x].Split(' ');
                                foreach (string word in par)
                                {
                                    if ((first.Length + msg.Length + word.Length + 1) < Conf.Max_Message_Length)
                                    {
                                        msg += " " + word;
                                    }
                                    else
                                    {
                                        msg = msg.Remove(0, 1);
                                        sw.WriteLine(first + ":" + msg + Environment.NewLine);
                                        string output = Environment.NewLine + Conf.Server_Name + ":" + ":" + nick + " " + first + ":" + msg;
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
                                if (!String.IsNullOrEmpty(msg.Trim()))
                                {
                                    msg = msg.Remove(0, 1);
                                    sw.WriteLine(first + ":" + msg + Environment.NewLine);
                                    string output = Environment.NewLine + Conf.Server_Name + ":" + ":" + nick + " " + first + ":" + msg;
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
                                sw.WriteLine(first + ":" + lines[x] + Environment.NewLine);
                                string output = Environment.NewLine + Conf.Server_Name + ":" + ":" + nick + " " + first + ":" + lines[x];

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
                        string[] stringSeparators = new string[] { Environment.NewLine };
                        string[] lines = param.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                        for (int x = 0; x <= lines.GetUpperBound(0); x++)
                        {
                            sw.WriteLine(cmd + " " + lines[x] + Environment.NewLine);
                            string output = Environment.NewLine + Conf.Server_Name + ":" + ":" + nick + " " + cmd + " " + lines[x];

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

                        if (String.IsNullOrEmpty(line))
                        {
                            blank_count++;
                        }
                        else
                        {
                            lock (streamlock)
                            {
                                string output = Environment.NewLine + Conf.Server_Name + ":" + line;
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
                        controller.log_error(ex, Conf.Logs_Path);
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
            if (!String.IsNullOrEmpty(Conf.Chans))
            {
                string[] channels = Conf.Chans.Split(',');
                foreach (string channel in channels)
                {
                    bool chan_blocked = false;
                    string[] channels_blacklist = Conf.Chan_Blacklist.Split(',');
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
                    Thread.Sleep(5000);
                }
            }
        }

        private void checkRegistration(object sender, EventArgs e)
        {
            if (connected == true)
            {
                checkRegisterationTimer.Enabled = false;
                if (!String.IsNullOrEmpty(nick) && !String.IsNullOrEmpty(Conf.Pass) && !String.IsNullOrEmpty(Conf.Email))
                {
                    register_nick(Conf.Pass, Conf.Email);
                }
                else
                {
                    string output = Environment.NewLine + Conf.Server_Name + ":You are missing an username and/or password.  Please add those to the server configuration so I can register this nick.";

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
                foreach (spam_info spam in Conf.Spam_Check)
                {
                    if (spam.Count < Conf.Spam_Count_Max)
                    {
                        spam_index.Add(index);
                    }
                    index++;
                }
                foreach (int x in spam_index)
                {
                    if (Conf.Spam_Check.Count() <= x)
                    {
                        Conf.Spam_Check.RemoveAt(x);
                    }
                }
            }
        }

        private void spam_check(object sender, EventArgs e)
        {
            lock (spamlock)
            {
                foreach (spam_info spam in Conf.Spam_Check)
                {
                    if (spam.Count > Conf.Spam_Count_Max)
                    {
                        if (!spam.Activated)
                        {
                            System.Timers.Timer new_timer = new System.Timers.Timer();
                            new_timer.Interval = Conf.Spam_Timeout;
                            new_timer.Elapsed += (new_sender, new_e) => spam_deactivate(new_sender, new_e, spam.Channel);
                            new_timer.Enabled = true;
                            timer_info tmp_timer = new timer_info();
                            tmp_timer.Channel = spam.Channel;
                            tmp_timer.Spam_Timer = new_timer;
                            lock (timerlock)
                            {
                                Spam_Timers.Add(tmp_timer);
                            }
                            spam.Activated = true;
                            spam.Count++;
                            Spam_Timers[Spam_Timers.Count - 1].Spam_Timer.Start();
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
                foreach (spam_info spam in Conf.Spam_Check)
                {
                    if (spam.Activated && spam.Channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        spam_found = true;
                        break;
                    }
                    index++;
                }
                if (spam_found)
                {
                    Conf.Spam_Check.RemoveAt(index);
                }
            }
            lock (timerlock)
            {
                int index = 0;
                bool spam_found = false;
                foreach (timer_info spam in Spam_Timers)
                {
                    if (spam.Channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        spam_found = true;
                        break;
                    }
                    index++;
                }
                if (spam_found)
                {
                    Spam_Timers[index].Spam_Timer.Stop();
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
                foreach (spam_info spam in Conf.Spam_Check)
                {
                    if (spam.Channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (spam.Count > Conf.Spam_Count_Max + 1)
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
                    Conf.Spam_Check[index].Count++;
                }
                else if (!spam_found)
                {
                    spam_info new_spam = new spam_info();
                    new_spam.Channel = channel;
                    new_spam.Activated = false;
                    new_spam.Count = 1;
                    Conf.Spam_Check.Add(new_spam);
                }
            }
        }

        internal bool get_spam_status(string channel)
        {
            bool active = false;
            lock (spamlock)
            {
                foreach (spam_info spam in Conf.Spam_Check)
                {
                    if (spam.Channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (spam.Activated)
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
            bool active = Conf.Spam_Enable;
            if (active && enabled)
            {
                foreach (string part in Conf.Spam_Ignore.Split(','))
                {
                    if (part.Equals(channel, StringComparison.InvariantCultureIgnoreCase) || part.Equals(nick, StringComparison.InvariantCultureIgnoreCase) || part.Equals(Nick))
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

        internal string get_nick_host(string tmp_nick)
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
                DateTime start = DateTime.Now;

                while (DateTime.Now.Subtract(start).TotalSeconds < 15 && String.IsNullOrEmpty(line))
                {
                    if (line.Contains("Please wait a while and try again."))
                    {
                        Thread.Sleep(1000);
                        sendData("USERHOST ", new_nick[0]);
                    }
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
            int access = Conf.Default_Level;
            if (type.Equals("~") || (type.Equals("q") && letter_mode == true))
            {
                access = Conf.Founder_Level;
            }
            else if (type.Equals("&") || (type.Equals("a") && letter_mode == true))
            {
                access = Conf.Sop_Level;
            }
            else if (type.Equals("@") || (type.Equals("o") && letter_mode == true))
            {
                access = Conf.Op_Level;
            }
            else if (type.Equals("%") || (type.Equals("h") && letter_mode == true))
            {
                access = Conf.Hop_Level;
            }
            else if (type.Equals("+") || (type.Equals("v") && letter_mode == true))
            {
                access = Conf.Voice_Level;
            }
            else
            {
                access = Conf.User_Level;
            }
            return access;
        }

        internal bool get_nick_auto(string type, string channel, string tmp_nick)
        {
            bool auto = true;
            string line = "";
            lock (queuelock)
            {
                sendData("PRIVMSG", "chanserv :" + type + " " + channel + " list " + tmp_nick);
                line = read_queue();
                bool cont = true;
                DateTime start = DateTime.Now;

                while (DateTime.Now.Subtract(start).TotalSeconds < 15 && cont)
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
                    else if (line.Contains("Please wait a while and try again."))
                    {
                        Thread.Sleep(1000);
                        sendData("PRIVMSG", "chanserv :" + type + " " + channel + " list " + tmp_nick);
                        cont = true;
                        line = read_queue();
                    }
                    else if (line.Contains("No such nick/channel"))
                    {
                        auto = false;
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

        internal bool get_nick_ident(string tmp_nick)
        {
            bool identified = false;
            string line = "";
            lock (queuelock)
            {
                sendData("PRIVMSG", "nickserv :STATUS " + tmp_nick);
                line = read_queue();
                DateTime start = DateTime.Now;

                while (DateTime.Now.Subtract(start).TotalSeconds < 15 && !line.Contains(":STATUS") && !line.Contains("No such nick/channel"))
                {
                    if (line.Contains("Please wait a while and try again."))
                    {
                        Thread.Sleep(1000);
                        sendData("PRIVMSG", "nickserv :STATUS " + tmp_nick);
                    }
                    line = read_queue();
                }
                if (line.Contains(":STATUS " + tmp_nick + " 3"))
                {
                    identified = true;
                }
            }
            return identified;
        }

        internal int get_nick_chan_access(string tmp_nick, string channel)
        {
            int new_access = Conf.Default_Level;
            string line = "";
            lock (queuelock)
            {
                sendData("WHO", channel.TrimStart(':'));
                line = read_queue();
                DateTime start = DateTime.Now;

                while (DateTime.Now.Subtract(start).TotalSeconds < 15 && !line.Contains("352 " + nick + " " + channel))
                {
                    if (line.Contains("Please wait a while and try again."))
                    {
                        Thread.Sleep(1000);
                        sendData("WHO", channel.TrimStart(':'));
                    }
                    line = read_queue();
                }
                char[] Separator = new char[] { ' ' };
                start = DateTime.Now;

                while (DateTime.Now.Subtract(start).TotalSeconds < 15 && !line.Contains("315 " + nick + " " + channel + " :End of /WHO list."))
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
                                    new_access = Conf.User_Level;
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

        internal int get_nick_access(string tmp_nick, string channel)
        {
            List<int> access_num = new List<int>();
            int top_access = Conf.Default_Level;
            access_num.Add(Conf.Default_Level);
            try
            {
                Nick_Info nick_info = get_nick_info(tmp_nick, channel);
                if (nick_info != null)
                {
                    access_num.Add(nick_info.Access);
                }
                else
                {
                    Nick_Info new_nick = new Nick_Info();
                    new_nick.Nick = tmp_nick;
                    new_nick.Access = Conf.Default_Level;
                    new_nick.Identified = false;
                    add_nick_info(new_nick, channel);
                    nick_info = new_nick;
                }
                if (nick_info.Nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    access_num.Add(Conf.Owner_Level);
                }
                if (!nick_info.Identified)
                {
                    nick_info.Identified = get_nick_ident(nick_info.Nick);
                }
                if (nick_info.Identified == true)
                {
                    foreach (Modules.Module module in Conf.Modules)
                    {
                        if (module.Class_Name.Equals("access"))
                        {
                            bool chan_allowed = true;
                            foreach (string blacklist in module.Blacklist)
                            {
                                if (blacklist.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    chan_allowed = false;
                                    break;
                                }
                            }
                            if (chan_allowed)
                            {
                                Modules.access acc = new Modules.access();
                                access_num.AddRange(acc.get_access_list(tmp_nick, channel, this));
                            }
                            break;
                        }
                    }
                }
                if (nick_info.Identified == true)
                {
                    foreach (string owner in Conf.Owners)
                    {
                        if (tmp_nick.Equals(owner, StringComparison.InvariantCultureIgnoreCase))
                        {
                            access_num.Add(Conf.Owner_Level);
                        }
                    }
                }
                foreach (int access in access_num)
                {
                    if (access > top_access)
                    {
                        top_access = access;
                    }
                }
                if (top_access == Conf.Default_Level && channel != null)
                {
                    top_access = get_nick_chan_access(tmp_nick, channel);
                    if (top_access != Conf.Default_Level)
                    {
                        nick_info.Access = top_access;
                    }
                }
            }
            catch (Exception ex)
            {
                lock (controller.errorlock)
                {
                    controller.log_error(ex, Conf.Logs_Path);
                }
            }
            return top_access;
        }

        internal Nick_Info get_nick_info(string nick, string channel)
        {
            Nick_Info nick_info = null;
            Channel_Info chan = get_chan_info(channel);
            if (chan != null)
            {
                foreach (Nick_Info info in chan.Nicks)
                {
                    if (info.Nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                    {
                        nick_info = info;
                        break;
                    }
                }
            }
            return nick_info;
        }

        internal void add_nick_info(Nick_Info nick, string channel)
        {
            Channel_Info chan = get_chan_info(channel);
            if (chan != null)
            {
                chan.Nicks.Add(nick);
            }
            else
            {
                add_chan_info(channel);
                chan = get_chan_info(channel);
                chan.Nicks.Add(nick);
            }
        }

        internal void del_nick_info(Nick_Info nick, string channel)
        {
            Channel_Info chan = get_chan_info(channel);
            if (chan != null)
            {
                chan.Nicks.Remove(nick);
            }
        }

        internal Channel_Info get_chan_info(string channel)
        {
            Channel_Info chan_info = null;
            foreach (Channel_Info chan in Conf.Channel_List)
            {
                if (chan.Channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                {
                    chan_info = chan;
                    break;
                }
            }
            return chan_info;
        }

        internal void add_chan_info(string channel, bool show = true)
        {
            Channel_Info chan_info = new Channel_Info();
            chan_info.Channel = channel;
            chan_info.Nicks = new List<Nick_Info>();
            chan_info.Show = get_chan_access(channel);
            Conf.Channel_List.Add(chan_info);
        }

        internal void del_chan_info(string channel)
        {
            Channel_Info chan = get_chan_info(channel);
            if (chan != null)
            {
                Conf.Channel_List.Remove(chan);
            }
        }

        internal bool get_chan_access(string channel)
        {
            bool display_chan = true;
            string line = "";
            lock (queuelock)
            {
                sendData("MODE", channel.TrimStart(':'));
                line = read_queue();
                DateTime start = DateTime.Now;

                while (DateTime.Now.Subtract(start).Seconds < 15 && !line.Contains("324 " + nick + " " + channel))
                {
                    Thread.Sleep(100);
                    line = read_queue();
                }
                char[] Separator = new char[] { ' ' };
                string[] name_line = line.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                if (name_line.GetUpperBound(0) >= 4)
                {
                    char[] arr = name_line[4].TrimStart('+').ToCharArray();
                    foreach (char c in arr)
                    {
                        if (c.Equals('p') || c.Equals('s'))
                        {
                            display_chan = false;
                            break;
                        }
                    }
                }
            }
            return display_chan;
        }


    }

    public class BotConfig
    {
        private string server_name;
        public string Server_Name 
        { 
            get
            {
                return server_name;
            }

            internal set
            {
                server_name = value;
            } 
        }

        private string server_address;
        public string Server_Address
        {
            get
            {
                return server_address;
            }

            internal set
            {
                server_address = value;
            }
        }

        private IPAddress[] server_ip;
        public IPAddress[] Server_IP
        {
            get
            {
                return server_ip;
            }

            internal set
            {
                server_ip = value;
            }
        }

        private string chans;
        public string Chans
        {
            get
            {
                return chans;
            }

            internal set
            {
                chans = value;
            }
        }

        private string chan_blacklist;
        public string Chan_Blacklist
        {
            get
            {
                return chan_blacklist;
            }

            internal set
            {
                chan_blacklist = value;
            }
        }

        private string ignore_list;
        public string Ignore_List
        {
            get
            {
                return ignore_list;
            }

            internal set
            {
                ignore_list = value;
            }
        }

        private int port;
        public int Port
        {
            get
            {
                return port;
            }

            internal set
            {
                port = value;
            }
        }

        private string nick;
        public string Nick
        {
            get
            {
                return nick;
            }

            internal set
            {
                nick = value;
            }
        }

        private string secondary_nicks;
        public string Secondary_Nicks
        {
            get
            {
                return secondary_nicks;
            }

            internal set
            {
                secondary_nicks = value;
            }
        }

        private string pass;
        public string Pass
        {
            get
            {
                return pass;
            }

            internal set
            {
                pass = value;
            }
        }

        private string email;
        public string Email
        {
            get
            {
                return email;
            }

            internal set
            {
                email = value;
            }
        }

        private string name;
        public string Name
        {
            get
            {
                return name;
            }

            internal set
            {
                name = value;
            }
        }

        private List<string> owners;
        public List<string> Owners
        {
            get
            {
                return owners;
            }

            internal set
            {
                owners = value;
            }
        }

        private int default_level;
        public int Default_Level
        {
            get
            {
                return default_level;
            }

            internal set
            {
                default_level = value;
            }
        }

        private int user_level;
        public int User_Level
        {
            get
            {
                return user_level;
            }

            internal set
            {
                user_level = value;
            }
        }

        private int voice_level;
        public int Voice_Level
        {
            get
            {
                return voice_level;
            }

            internal set
            {
                voice_level = value;
            }
        }

        private int hop_level;
        public int Hop_Level
        {
            get
            {
                return hop_level;
            }

            internal set
            {
                hop_level = value;
            }
        }

        private int op_level;
        public int Op_Level
        {
            get
            {
                return op_level;
            }

            internal set
            {
                op_level = value;
            }
        }

        private int sop_level;
        public int Sop_Level
        {
            get
            {
                return sop_level;
            }

            internal set
            {
                sop_level = value;
            }
        }

        private int founder_level;
        public int Founder_Level
        {
            get
            {
                return founder_level;
            }

            internal set
            {
                founder_level = value;
            }
        }

        private int owner_level;
        public int Owner_Level
        {
            get
            {
                return owner_level;
            }

            internal set
            {
                owner_level = value;
            }
        }

        private bool auto_connect;
        public bool Auto_Connect
        {
            get
            {
                return auto_connect;
            }

            internal set
            {
                auto_connect = value;
            }
        }

        private string command;
        public string Command
        {
            get
            {
                return command;
            }

            internal set
            {
                command = value;
            }
        }

        private bool spam_enable;
        public bool Spam_Enable
        {
            get
            {
                return spam_enable;
            }

            internal set
            {
                spam_enable = value;
            }
        }

        private string spam_ignore;
        public string Spam_Ignore
        {
            get
            {
                return spam_ignore;
            }

            internal set
            {
                spam_ignore = value;
            }
        }

        private int spam_count_max;
        public int Spam_Count_Max
        {
            get
            {
                return spam_count_max;
            }

            internal set
            {
                spam_count_max = value;
            }
        }

        private int spam_threshold;
        public int Spam_Threshold
        {
            get
            {
                return spam_threshold;
            }

            internal set
            {
                spam_threshold = value;
            }
        }

        private int spam_timeout;
        public int Spam_Timeout
        {
            get
            {
                return spam_timeout;
            }

            internal set
            {
                spam_timeout = value;
            }
        }

        private int max_message_length;
        public int Max_Message_Length
        {
            get
            {
                return max_message_length;
            }

            internal set
            {
                max_message_length = value;
            }
        }

        private string keep_logs;
        public string Keep_Logs
        {
            get
            {
                return keep_logs;
            }

            internal set
            {
                keep_logs = value;
            }
        }

        private string logs_path;
        public string Logs_Path
        {
            get
            {
                return logs_path;
            }

            internal set
            {
                logs_path = value;
            }
        }

        private long max_log_size;
        public long Max_Log_Size
        {
            get
            {
                return max_log_size;
            }

            internal set
            {
                max_log_size = value;
            }
        }

        private int max_log_number;
        public int Max_Log_Number
        {
            get
            {
                return max_log_number;
            }

            internal set
            {
                max_log_number = value;
            }
        }

        private List<Modules.Module> modules;
        public List<Modules.Module> Modules
        {
            get
            {
                return modules;
            }

            internal set
            {
                modules = value;
            }
        }

        private List<spam_info> spam_check;
        public List<spam_info> Spam_Check
        {
            get
            {
                return spam_check;
            }

            internal set
            {
                spam_check = value;
            }
        }
        
        private List<Channel_Info> channel_list;
        public List<Channel_Info> Channel_List
        {
            get
            {
                return channel_list;
            }

            internal set
            {
                channel_list = value;
            }
        }
    }

    public class Nick_Info
    {
        private string nick;
        public string Nick
        {
            get
            {
                return nick;
            }

            internal set
            {
                nick = value;
            }
        }

        private int access;
        public int Access
        {
            get
            {
                return access;
            }

            internal set
            {
                access = value;
            }
        }

        private bool identified;
        public bool Identified
        {
            get
            {
                return identified;
            }

            internal set
            {
                identified = value;
            }
        }
    }

    public class Channel_Info
    {
        public Channel_Info()
        {
            nicks = new List<Nick_Info>();
        }
        private string channel;
        public string Channel
        {
            get
            {
                return channel;
            }

            internal set
            {
                channel = value;
            }
        }

        private bool show;
        public bool Show
        {
            get
            {
                return show;
            }

            internal set
            {
                show = value;
            }
        }

        private List<Nick_Info> nicks;
        public List<Nick_Info> Nicks
        {
            get
            {
                return nicks;
            }

            internal set
            {
                nicks = value;
            }
        }
    }

    public class spam_info
    {
        private string channel;
        public string Channel
        {
            get
            {
                return channel;
            }

            internal set
            {
                channel = value;
            }
        }

        private int count;
        public int Count
        {
            get
            {
                return count;
            }

            internal set
            {
                count = value;
            }
        }

        private bool activated;
        public bool Activated
        {
            get
            {
                return activated;
            }

            internal set
            {
                activated = value;
            }
        }
    }

    public class timer_info
    {
        private string channel;
        public string Channel
        {
            get
            {
                return channel;
            }

            internal set
            {
                channel = value;
            }
        }

        private System.Timers.Timer spam_timer;
        public System.Timers.Timer Spam_Timer
        {
            get
            {
                return spam_timer;
            }

            internal set
            {
                spam_timer = value;
            }
        }

    }
}
