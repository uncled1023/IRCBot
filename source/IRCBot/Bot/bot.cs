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
    public class bot
    {
        TcpClient IRCConnection;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;

        public System.Windows.Forms.Timer checkRegisterationTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer Spam_Check_Timer;
        private System.Windows.Forms.Timer Spam_Threshold_Check;
        private List<timer_info> Spam_Timers;
        private System.Windows.Forms.Timer check_cancel;
        private spam_info spam_list = new spam_info();
        private List<string> data_queue = new List<string>();
        private List<string> stream_queue = new List<string>();

        public readonly object queuelock = new object();
        public readonly object streamlock = new object();
        
        public bool restart;
        public int restart_attempts;
        public bool connected;
        public bool connecting;
        public bool disconnected;
        public bool shouldRun;
        public bool first_run;
        public List<List<string>> nick_list;
        public List<string> channel_list;
        public string cur_dir;
        public string nick;
        public BackgroundWorker worker;
        public List<Modules.Module> module_list = new List<Modules.Module>();
        public List<string> modules_loaded = new List<string>();
        public List<string> modules_error = new List<string>();
        public DateTime start_time = new DateTime();

        public Interface ircbot;
        public BotConfig conf = new BotConfig();

        public readonly object timerlock = new object();
        public readonly object spamlock = new object();

        public bot()
        {
            IRCConnection = null;
            ns = null;
            sr = null;
            sw = null;

            checkRegisterationTimer = new System.Windows.Forms.Timer();
            Spam_Check_Timer = new System.Windows.Forms.Timer();
            Spam_Threshold_Check = new System.Windows.Forms.Timer();
            Spam_Timers = new List<timer_info>();
            check_cancel = new System.Windows.Forms.Timer();
            connected = false;
            connecting = false;
            disconnected = true;
            restart = false;
            restart_attempts = 0;
            worker = new BackgroundWorker();

            shouldRun = true;
            first_run = true;
            nick_list = new List<List<string>>();
            channel_list = new List<string>();
        }

        public void start_bot(Interface main)
        {
            connecting = true;
            start_time = DateTime.Now;
            ircbot = main;
            cur_dir = ircbot.cur_dir;
            nick = conf.nick;

            load_modules();

            Spam_Check_Timer.Tick += new EventHandler(spam_tick);
            Spam_Check_Timer.Interval = ircbot.irc_conf.spam_threshold;
            Spam_Check_Timer.Start();

            Spam_Threshold_Check.Tick += new EventHandler(spam_check);
            Spam_Threshold_Check.Interval = 100;
            Spam_Threshold_Check.Start();

            checkRegisterationTimer.Tick += new EventHandler(checkRegistration);
            checkRegisterationTimer.Interval = 120000;
            checkRegisterationTimer.Enabled = true;

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
            checkRegisterationTimer = new System.Windows.Forms.Timer();
            Spam_Check_Timer = new System.Windows.Forms.Timer();
            Spam_Threshold_Check = new System.Windows.Forms.Timer();
            Spam_Timers = new List<timer_info>();
            check_cancel = new System.Windows.Forms.Timer();
            connected = false;
            connecting = true;
            disconnected = true;
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            shouldRun = true;
            first_run = true;
            nick_list = new List<List<string>>();
            channel_list = new List<string>();

            connecting = true;
            cur_dir = ircbot.cur_dir;
            nick = conf.nick;

            Spam_Check_Timer.Tick += new EventHandler(spam_tick);
            Spam_Check_Timer.Interval = ircbot.irc_conf.spam_threshold;
            Spam_Check_Timer.Start();

            Spam_Threshold_Check.Tick += new EventHandler(spam_check);
            Spam_Threshold_Check.Interval = 100;
            Spam_Threshold_Check.Start();

            checkRegisterationTimer.Tick += new EventHandler(checkRegistration);
            checkRegisterationTimer.Interval = 120000;
            checkRegisterationTimer.Enabled = true;

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

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        bool IsConnectedToInternet()
        {
            bool a;
            int xs;
            a = InternetGetConnectedState(out xs, 0);
            return a;
        }

        public void check_connection(object sender, DoWorkEventArgs e)
        {
            bool is_connected = true;
            while (shouldRun && is_connected)
            {
                Thread.Sleep(1000);
                is_connected = IsConnectedToInternet();

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

                if (!is_connected || !shouldRun)
                {
                    check_cancel.Stop();
                    connected = false;
                    connecting = false;
                    if (sr != null)
                        sr.Close();
                    if (sw != null)
                        sw.Close();
                    if (ns != null)
                        ns.Close();
                    if (IRCConnection != null)
                        IRCConnection.Close();
                    restart = true;
                    restart_attempts++;
                    shouldRun = false;
                }
            }
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
                string output = Environment.NewLine + conf.server + ":Loaded Modules: " + msg.TrimStart(',').Trim();
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
                string output = Environment.NewLine + conf.server + ":Error Loading Modules: " + msg.TrimStart(',').Trim();
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
            connecting = false;
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (ns != null)
                ns.Close();
            if (IRCConnection != null)
                IRCConnection.Close();

            checkRegisterationTimer.Stop();
            Spam_Check_Timer.Stop();
            Spam_Threshold_Check.Stop();
            check_cancel.Stop();

            if (restart == true)
            {
                string output = Environment.NewLine + conf.server + ":" + "Restart Attempt " + restart_attempts + " [" + Math.Pow(2, Convert.ToDouble(restart_attempts)) + " Seconds Delay]";
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
                if (conf.server.Equals("No_Server_Specified"))
                {
                    string output = Environment.NewLine + conf.server + ":" + "Please add a server to connect to.";
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
            try
            {
                connected = true;
                connecting = false;
                IRCConnection = new TcpClient(conf.server_address, conf.port);
            }
            catch (Exception ex)
            {
                restart = true;
                restart_attempts++;
                connected = false;
                connecting = false;

                lock (ircbot.errorlock)
                {
                    ircbot.log_error(ex);
                }
            }

            if (restart == false)
            {
                BackgroundWorker work = new BackgroundWorker();
                work.WorkerSupportsCancellation = true;
                work.DoWork += (sender, e) => save_stream(sender, e);

                BackgroundWorker check_connect = new BackgroundWorker();
                check_connect.WorkerSupportsCancellation = true;
                check_connect.DoWork += (sender, e) => check_connection(sender, e);

                try
                {
                    IRCConnection.NoDelay = true;
                    ns = IRCConnection.GetStream();
                    sr = new StreamReader(ns);
                    sw = new StreamWriter(ns);
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

                    work.RunWorkerAsync(2000);
                    check_connect.RunWorkerAsync(2000);

                    IRCWork(bw);
                }
                catch (Exception ex)
                {
                    restart = true;
                    restart_attempts++;

                    lock (ircbot.errorlock)
                    {
                        ircbot.log_error(ex);
                    }
                }
                finally
                {
                    work.CancelAsync();
                    check_connect.CancelAsync();
                    connecting = false;
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
                            if ((first.Length + 1 + lines[x].Length) > ircbot.irc_conf.max_message_length)
                            {
                                string msg = "";
                                string[] par = lines[x].Split(' ');
                                foreach (string word in par)
                                {
                                    if ((first.Length + msg.Length + word.Length + 1) < ircbot.irc_conf.max_message_length)
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
                                            lock (ircbot.listLock)
                                            {
                                                if (ircbot.queue_text.Count >= 1000)
                                                {
                                                    ircbot.queue_text.RemoveAt(0);
                                                }
                                                ircbot.queue_text.Add(output);
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
                            else
                            {
                                sw.WriteLine(first + ":" + lines[x]);
                                string output = Environment.NewLine + conf.server + ":" + ":" + nick + " " + first + ":" + lines[x];

                                if (display_output)
                                {
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

        private void save_stream(Object sender, EventArgs e)
        {
            while (shouldRun)
            {
                try
                {
                    if (sr != null)
                    {
                        string line = sr.ReadLine();
                        lock (streamlock)
                        {
                            string output = Environment.NewLine + conf.server + ":" + line;
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
                }
                catch (Exception ex)
                {
                    lock (ircbot.errorlock)
                    {
                        ircbot.log_error(ex);
                    }
                    restart = true;
                    restart_attempts++;
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
            if (response == null)
            {
                response = "";
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
            if (response == null)
            {
                response = "";
            }
            return response;
        }

        public void IRCWork(BackgroundWorker bw)
        {
            shouldRun = true;
            connected = true;
            connecting = false;
            disconnected = false;

            string data = "";

            checkRegisterationTimer.Enabled = true;

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
                            checkRegisterationTimer.Enabled = true;
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
                        if (main_nick.Equals(nick) || (!main_nick.Equals(nick) && nicks.GetUpperBound(0) <= cur_alt_nick))
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
                        sendData("PRIVMSG", "NickServ :ghost " + main_nick + " " + conf.pass);
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
                            checkRegisterationTimer.Enabled = false;
                            bot_state = 6;
                        }
                        else if (line.Contains("Your nick") && line.Contains("isn't registered"))
                        {
                            checkRegisterationTimer.Enabled = true;
                            bot_state = 6;
                        }
                        else if (line.Contains("No such nick/channel"))
                        {
                            checkRegisterationTimer.Enabled = true;
                            bot_state = 6;
                        }
                        else if (line.Contains("This nick is awaiting an e-mail verification code before completing registration."))
                        {
                            checkRegisterationTimer.Enabled = false;
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
                if (bw.CancellationPending)
                {
                    shouldRun = false;
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
                nick = name[0].TrimStart(':');
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
                        if (msg_type.StartsWith(ircbot.irc_conf.command) == true)
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
                            int new_access = get_access_num(nick, false);
                            for (int i = 2; i < nick_list[x].Count(); i++)
                            {
                                string[] split = nick_list[x][i].Split(':');
                                if (split.GetUpperBound(0) > 0)
                                {
                                    if (split[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
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
                                nick_list[x].Add(new_access + ":" + nick);
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
                            sw.WriteLine("WHO " + channel.TrimStart(':'));
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
                            while (name_line[1] != "352")
                            {
                                line = read_queue();
                                name_line = line.Split(charSeparator, 5);
                                while (name_line.GetUpperBound(0) <= 3)
                                {
                                    line = read_queue();
                                    name_line = line.Split(charSeparator, 5);
                                }
                            }
                            tmp_list.Add(name_line[3]); string[] name_info = name_line[4].Split(':');
                            if (name_info.GetUpperBound(0) > 0)
                            {
                                char[] sep = new char[] { ' ' };
                                string[] info = name_info[0].Split(sep, StringSplitOptions.RemoveEmptyEntries);
                                while (name_line[4] != ":End of /WHO list.")
                                {
                                    if (name_line[1] == "352")
                                    {
                                        channel_found = true;
                                        char[] arr = info[info.GetUpperBound(0)].ToCharArray();
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
                                        tmp_list.Add(user_access + ":" + info[info.GetUpperBound(0) - 1].TrimStart('~'));
                                    }
                                    line = read_queue();
                                    name_line = line.Split(charSeparator, 5);
                                    while (name_line.GetUpperBound(0) <= 3)
                                    {
                                        line = read_queue();
                                        name_line = line.Split(charSeparator, 5);
                                    }
                                    name_info = name_line[4].Split(':');
                                    info = name_info[0].Split(sep, StringSplitOptions.RemoveEmptyEntries);
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
                if (ex[1].Equals("quit", StringComparison.InvariantCultureIgnoreCase))
                {
                    type = "quit";
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
                                if (split[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
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
                                    if (split[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
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
                                if (split[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
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
                if (ignore_nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
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
                                if (node.Equals(nick, StringComparison.InvariantCultureIgnoreCase) || node.TrimStart('#').Equals(channel.TrimStart('#'), StringComparison.InvariantCultureIgnoreCase))
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
                if (nick != "" && conf.pass != "" && conf.email != "")
                {
                    register_nick(conf.pass, conf.email);
                }
                else
                {
                    string output = Environment.NewLine + conf.server + ":You are missing an username and/or password.  Please add those to the server configuration so I can register this nick.";

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
                shouldRun = false;
                connected = false;
                connecting = false;
                if (sr != null)
                    sr.Close();
                if (sw != null)
                    sw.Close();
                if (ns != null)
                    ns.Close();
                if (IRCConnection != null)
                    IRCConnection.Close();
                checkRegisterationTimer.Enabled = false;
                string output = Environment.NewLine + conf.server + ":" + "Disconnected";

                lock (ircbot.listLock)
                {
                    if (ircbot.queue_text.Count >= 1000)
                    {
                        ircbot.queue_text.RemoveAt(0);
                    }
                    ircbot.queue_text.Add(output);
                }
                check_cancel.Stop();
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
                    if (spam.spam_count < ircbot.irc_conf.spam_count_max)
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
                    if (spam.spam_count > ircbot.irc_conf.spam_count_max)
                    {
                        if (!spam.spam_activated)
                        {
                            System.Windows.Forms.Timer new_timer = new System.Windows.Forms.Timer();
                            new_timer.Interval = ircbot.irc_conf.spam_timout;
                            new_timer.Tick += (new_sender, new_e) => spam_deactivate(new_sender, new_e, spam.spam_channel);
                            new_timer.Enabled = true;
                            timer_info tmp_timer = new timer_info();
                            tmp_timer.spam_channel = spam.spam_channel;
                            tmp_timer.spam_timer = new_timer;
                            lock (timerlock)
                            {
                                Spam_Timers.Add(tmp_timer);
                            }
                            spam.spam_activated = true;
                            spam.spam_count++;
                            Spam_Timers[Spam_Timers.Count - 1].spam_timer.Start();
                        }
                    }
                }
            }
        }

        public void spam_deactivate(object sender, EventArgs e, string channel)
        {
            lock (spamlock)
            {
                int index = 0;
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.spam_activated && spam.spam_channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }
                    index++;
                }
                conf.spam_check.RemoveAt(index);
            }
            lock (timerlock)
            {
                int index = 0;
                foreach (timer_info spam in Spam_Timers)
                {
                    if (spam.spam_channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        break;
                    }
                    index++;
                }
                Spam_Timers[index].spam_timer.Stop();
                Spam_Timers.RemoveAt(index);
            }
        }

        public void add_spam_count(string channel)
        {
            lock (spamlock)
            {
                bool spam_added = false;
                bool spam_found = false;
                int index = 0;
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.spam_channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (spam.spam_count > ircbot.irc_conf.spam_count_max + 1)
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
                    conf.spam_check[index].spam_count++;
                }
                else if (!spam_found)
                {
                    spam_info new_spam = new spam_info();
                    new_spam.spam_channel = channel;
                    new_spam.spam_activated = false;
                    new_spam.spam_count = 1;
                    conf.spam_check.Add(new_spam);
                }
            }
        }

        public bool get_spam_status(string channel)
        {
            bool active = false;
            lock (spamlock)
            {
                foreach (spam_info spam in conf.spam_check)
                {
                    if (spam.spam_channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (spam.spam_activated)
                        {
                            active = true;
                        }
                        break;
                    }
                }
            }
            return active;
        }

        private void register_nick(string password, string email)
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

                while (name_line.GetUpperBound(0) < 3 || name_line[2] != nick)
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
                if (name_line[4].StartsWith(nick + " 3", StringComparison.InvariantCultureIgnoreCase))
                {
                    identified = true;
                }
            }
            return identified;
        }

        public int get_user_op(string nick, string channel)
        {
            int new_access = conf.default_level;
            bool nick_found = false;
            string line = "";
            if (sw != null)
            {
                sw.WriteLine("WHO " + channel.TrimStart(':'));
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
                while (name_line[1] != "352")
                {
                    line = read_queue();
                    name_line = line.Split(charSeparator, 5);
                    while (name_line.GetUpperBound(0) <= 3)
                    {
                        line = read_queue();
                        name_line = line.Split(charSeparator, 5);
                    }
                }
                string[] name_info = name_line[4].Split(':');
                if (name_info.GetUpperBound(0) > 0)
                {
                    char[] sep = new char[] { ' ' };
                    string[] info = name_info[0].Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    while (name_line[4] != ":End of /WHO list.")
                    {
                        if (info.GetUpperBound(0) - 1 >= 0)
                        {
                            if (info[info.GetUpperBound(0) - 1].TrimStart('~').Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                bool char_found = false;
                                char[] arr = info[info.GetUpperBound(0)].ToCharArray();
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
                                nick_found = true;
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
                            name_info = name_line[4].Split(':');
                            info = name_info[0].Split(sep, StringSplitOptions.RemoveEmptyEntries);
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
            int access_num = conf.default_level;
            try
            {
                string access = access_num.ToString();
                string tmp_custom_access = "";
                if (nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                {
                    access = conf.owner_level.ToString();
                }
                bool user_identified = get_user_ident(nick);
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
                    if (nick_list[x][0].Equals(channel, StringComparison.InvariantCultureIgnoreCase) || channel == null)
                    {
                        for (int i = 2; i < nick_list[x].Count(); i++)
                        {
                            string[] lists = nick_list[x][i].Split(':');
                            if (lists.GetUpperBound(0) > 0)
                            {
                                if (lists[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
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
                        if (nick.Equals(owners[x], StringComparison.InvariantCultureIgnoreCase))
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
                    access_num = get_user_op(nick, channel);
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
                                        if (lists[1].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
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
                                    nick_list[x].Add(access_num + ":" + nick);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lock (ircbot.errorlock)
                {
                    ircbot.log_error(ex);
                }
            }
            return access_num;
        }
    }

    public class BotConfig
    {
        public string server { get; set; }
        public string server_address { get; set; }
        public IPAddress[] server_ip { get; set; }
        public string chans { get; set; }
        public string chan_blacklist { get; set; }
        public string ignore_list { get; set; }
        public int port { get; set; }
        public string nick { get; set; }
        public string secondary_nicks { get; set; }
        public string pass { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public string owner { get; set; }
        public int default_level { get; set; }
        public int user_level { get; set; }
        public int voice_level { get; set; }
        public int hop_level { get; set; }
        public int op_level { get; set; }
        public int sop_level { get; set; }
        public int founder_level { get; set; }
        public int owner_level { get; set; }
        public bool auto_connect { get; set; }
        public List<List<string>> module_config { get; set; }
        public List<List<string>> command_list { get; set; }
        public List<spam_info> spam_check { get; set; }
    }
}

public class spam_info
{
    public string spam_channel { get; set; }
    public int spam_count { get; set; }
    public bool spam_activated { get; set; }
}

public class timer_info
{
    public string spam_channel { get; set; }
    public System.Windows.Forms.Timer spam_timer { get; set; }
}
