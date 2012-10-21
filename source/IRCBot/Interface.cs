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

struct IRCConfig
{
    public string server;
    public string chans;
    public int port;
    public string nick;
    public string pass;
    public string email;
    public string name;
    public string owner;
    public string command;
    public string keep_logs;
    public string logs_path;
    public int spam_count_max;
    public int spam_threshold;
    public int spam_timout;
    public int max_message_length;
    public List<List<string>> module_config;
    public List<List<string>> command_access;
}

namespace IRCBot
{
    public partial class Interface : Form
    {
        TcpClient IRCConnection = null;
        IRCConfig config;
        NetworkStream ns = null;
        StreamReader sr = null;
        StreamWriter sw = null;

        private string output = "";
        public string cur_dir = "";
        private List<string> queue_text = new List<string>();
        public List<List<string>> nick_list = new List<List<string>>();
        public List<string> channel_list = new List<string>();
        private System.Windows.Forms.Timer updateOutput = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer checkRegisterationTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer Spam_Check_Timer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer Spam_Timer = new System.Windows.Forms.Timer();
        private bool spam_activated = false;
        public int spam_count = 0;
        private bool restart = false;
        private int restart_attempts = 0;
        public bool shouldRun = true;
        public bool first_run = true;

        private readonly object listLock = new object();

        private IRCConfig conf = new IRCConfig();

        // Load Modules //
        
        // Access Module
        access access = new access();
        // Owner Module
        owner owner = new owner();
        // Help Module
        help help = new help();
        // Rules Module
        rules rules = new rules();
        // Intro Message Module
        intro intro = new intro();
        // Quote Module
        quote quote = new quote();
        // Seen Module
        seen seen = new seen();
        // Weather Module
        weather weather = new weather();
        // Google Module
        google google = new google();
        // Urban Dictionary Module
        urban_dictionary ud = new urban_dictionary();
        // 8ball Module
        _8ball _8ball = new _8ball();
        // AI Module
        AI ai = new AI();
        // Messaging Module
        messaging message_module = new messaging();
        // Hbomb Module
        hbomb hbomb = new hbomb();
        // Ping Me Module
        pingme pingme = new pingme();
        // Fun Commands Module
        fun fun = new fun();
        // ChatBot Module
        chat chat = new chat();

        delegate void SetTextCallback(string text);

        //inner enum used only internally
        [Flags]
        private enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F,
            NoHeaps = 0x40000000
        }
        //inner struct used only internally
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            const int MAX_PATH = 260;
            internal UInt32 dwSize;
            internal UInt32 cntUsage;
            internal UInt32 th32ProcessID;
            internal IntPtr th32DefaultHeapID;
            internal UInt32 th32ModuleID;
            internal UInt32 cntThreads;
            internal UInt32 th32ParentProcessID;
            internal Int32 pcPriClassBase;
            internal UInt32 dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string szExeFile;
        }

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr CreateToolhelp32Snapshot([In]UInt32 dwFlags, [In]UInt32 th32ProcessID);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32First([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern bool Process32Next([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle([In] IntPtr hObject);

        [DllImport("user32.dll")]
        static extern Int32 FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public Int32 dwFlags;
            public UInt32 uCount;
            public Int32 dwTimeout;
        }

        public Interface()
        {
            InitializeComponent();

            conf.module_config = new List<List<string>>();
            conf.command_access = new List<List<string>>();
            
            cur_dir = Directory.GetCurrentDirectory();

            string list_file = cur_dir + "\\config\\help.txt";
            if (File.Exists(list_file))
            {
                string[] file = System.IO.File.ReadAllLines(list_file);
                foreach (string file_line in file)
                {
                    List<string> tmp_list = new List<string>();
                    string[] split = file_line.Split(':');
                    string command_access = split[1];
                    string command = split[2];
                    tmp_list.Add(command);
                    tmp_list.Add(command_access);
                    conf.command_access.Add(tmp_list);
                }
            }

            updateOutput.Tick += new EventHandler(UpdateOutput);
            Spam_Check_Timer.Tick += new EventHandler(spam_tick);
            checkRegisterationTimer.Tick += new EventHandler(checkRegistration);
            Spam_Timer.Tick += new EventHandler(spam_deactivate);
            Control control = new Control();
            control = tabControl1.Controls.Find("output_box_system", true)[0];
            RichTextBox output_box = (RichTextBox)control;
            output_box.LinkClicked += new LinkClickedEventHandler(link_Click);
            connect();
        }

        private void connect()
        {
            connectToolStripMenuItem.Text = "Disconnect";
            cur_dir = Directory.GetCurrentDirectory();

            updateOutput.Interval = 100;
            updateOutput.Start();

            checkRegisterationTimer.Interval = 60000;
            checkRegisterationTimer.Start();
            
            queue_text.Capacity = 1000;
            queue_text.Clear();

            // Load Bot Configuration
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(cur_dir + "\\config\\config.xml");
            }
            else
            {
                XmlNode node = xmlDoc.CreateNode(XmlNodeType.Element, "connection_settings", null);
                XmlNode nodeName = xmlDoc.CreateElement("name");
                nodeName.InnerText = "IRCBot";
                node.AppendChild(nodeName);
                XmlNode nodeNick = xmlDoc.CreateElement("nick");
                nodeNick.InnerText = "IRCBot";
                node.AppendChild(nodeNick);
                XmlNode nodePass = xmlDoc.CreateElement("password");
                nodePass.InnerText = "";
                node.AppendChild(nodePass);
                XmlNode nodeEmail = xmlDoc.CreateElement("email");
                nodeEmail.InnerText = "";
                node.AppendChild(nodeEmail);
                XmlNode nodeOwner = xmlDoc.CreateElement("owner");
                nodeOwner.InnerText = "";
                node.AppendChild(nodeOwner);
                XmlNode nodePort = xmlDoc.CreateElement("port");
                nodePort.InnerText = "6667";
                node.AppendChild(nodePort);
                XmlNode nodeServer = xmlDoc.CreateElement("server");
                nodeServer.InnerText = "irc";
                node.AppendChild(nodeServer);
                XmlNode nodeChan = xmlDoc.CreateElement("chan_list");
                nodeChan.InnerText = "";
                node.AppendChild(nodeChan);
                XmlNode nodeCommand = xmlDoc.CreateElement("command_prefix");
                nodeCommand.InnerText = ".";
                node.AppendChild(nodeCommand);
                XmlNode nodeKeep = xmlDoc.CreateElement("keep_logs");
                nodeKeep.InnerText = "True";
                node.AppendChild(nodeKeep);
                XmlNode nodeLogs = xmlDoc.CreateElement("logs_path");
                nodeLogs.InnerText = cur_dir + "\\logs\\";
                node.AppendChild(nodeLogs);
                XmlNode nodeStart = xmlDoc.CreateElement("start_with_windows");
                nodeStart.InnerText = "True";
                node.AppendChild(nodeStart);
                XmlNode nodeSpamCount = xmlDoc.CreateElement("spam_count");
                nodeSpamCount.InnerText = "5";
                node.AppendChild(nodeSpamCount);
                XmlNode nodeSpamThreshold = xmlDoc.CreateElement("spam_threshold");
                nodeSpamThreshold.InnerText = "1000";
                node.AppendChild(nodeSpamThreshold);
                XmlNode nodeSpamTime = xmlDoc.CreateElement("spam_timeout");
                nodeSpamTime.InnerText = "10000";
                node.AppendChild(nodeSpamTime);
                XmlNode nodeSpamMaxMsgLength = xmlDoc.CreateElement("max_message_length");
                nodeSpamMaxMsgLength.InnerText = "450";
                node.AppendChild(nodeSpamMaxMsgLength);
                xmlDoc.AppendChild(node);
                xmlDoc.Save(cur_dir + "\\config\\config.xml");
                xmlDoc.Load(cur_dir + "\\config\\config.xml");
            }
            XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/connection_settings");

            conf.name = list["name"].InnerText;
            conf.nick = list["nick"].InnerText;
            conf.pass = list["password"].InnerText;
            conf.email = list["email"].InnerText;
            conf.owner = list["owner"].InnerText;
            conf.port = Convert.ToInt32(list["port"].InnerText);
            conf.server = list["server"].InnerText;
            conf.chans = list["chan_list"].InnerText;
            conf.command = list["command_prefix"].InnerText;
            conf.keep_logs = list["keep_logs"].InnerText;
            conf.logs_path = list["logs_path"].InnerText;
            conf.spam_count_max = Convert.ToInt32(list["spam_count"].InnerText);
            conf.spam_threshold = Convert.ToInt32(list["spam_threshold"].InnerText);
            conf.spam_timout = Convert.ToInt32(list["spam_timeout"].InnerText);
            conf.max_message_length = Convert.ToInt32(list["max_message_length"].InnerText);

            conf.module_config.Clear();
            XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/modules/module");
            foreach (XmlNode xn in xnList)
            {
                List<string> tmp_list = new List<string>();
                String module_name = xn["name"].InnerText;
                tmp_list.Add(module_name);
                tmp_list.Add(xn["enabled"].InnerText);

                XmlNodeList optionList = xn.ChildNodes;
                foreach (XmlNode option in optionList)
                {
                    if (option.Name.Equals("options"))
                    {
                        XmlNodeList Options = option.ChildNodes;
                        foreach (XmlNode options in Options)
                        {
                            switch (options["type"].InnerText)
                            {
                                case "textbox":
                                        tmp_list.Add(options["value"].InnerText);
                                    break;
                                case "checkbox":
                                        tmp_list.Add(options["checked"].InnerText);
                                    break;
                            }
                        }
                    }
                }
                conf.module_config.Add(tmp_list);
            }

            Spam_Check_Timer.Interval = conf.spam_threshold;
            Spam_Check_Timer.Start();

            Spam_Timer.Interval = conf.spam_timout;
            string[] server_list = conf.server.Split(',');
            foreach (string server_name in server_list)
            {
                Control control = new Control();
                string[] server = conf.server.Split('.');
                string tmp_server_name = "No Server Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
                if (tabControl1.Controls.Find("output_box_system", true).GetUpperBound(0) >= 0)
                {
                    control = tabControl1.Controls.Find("output_box_system", true)[0];
                    RichTextBox output_box = (RichTextBox)control;
                    output_box.Name = "output_box_" + tmp_server_name + "_system";
                    control = tabControl1.Controls.Find("tabPage1", true)[0];
                    TabPage tabpage = (TabPage)control;
                    tabpage.Name = "tabPage_" + tmp_server_name + "_system";
                    tabpage.Text = tmp_server_name;
                }
                else if (tabControl1.Controls.Find("output_box_" + tmp_server_name + "_system", true).GetUpperBound(0) < 0)
                {
                    RichTextBox box = new RichTextBox();
                    box.Dock = System.Windows.Forms.DockStyle.Fill;
                    box.Location = new System.Drawing.Point(3, 3);
                    box.Name = "output_box_" + tmp_server_name + "_system";
                    box.Size = new System.Drawing.Size(826, 347);
                    box.TabIndex = 0;
                    box.ReadOnly = true;
                    box.Text = "";
                    TabPage tabpage = new TabPage();
                    tabpage.Controls.Add(box);
                    tabpage.Location = new System.Drawing.Point(4, 22);
                    tabpage.Name = "tabPage_user_" + tmp_server_name + "_system";
                    tabpage.Padding = new System.Windows.Forms.Padding(3);
                    tabpage.Size = new System.Drawing.Size(832, 353);
                    tabpage.TabIndex = 0;
                    tabpage.Text = tmp_server_name;
                    tabpage.UseVisualStyleBackColor = true;
                    tabControl1.Controls.Add(tabpage);
                    tabControl1.Update();
                    box.LinkClicked += new LinkClickedEventHandler(link_Click);
                }
                else
                {
                }
            }

            nick_list.Clear();
            first_run = true;
            this.backgroundWorker1.RunWorkerAsync(2000);
        }

        protected void link_Click(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void send_button_Click(object sender, EventArgs e)
        {
            string input_tmp = input_box.Text;
            char[] charSeparator = new char[] { ' ' };
            string[] input = input_tmp.Split(charSeparator, 2);
            if (input[0].StartsWith("/"))
            {
                if (input.GetUpperBound(0) > 0)
                {
                    sendData(input[0].TrimStart('/'), input[1]);
                }
                else
                {
                    sendData(input[0].TrimStart('/'), null);
                }
            }
            else
            {
                if (tabControl1.SelectedIndex == 0)
                {
                    output = Environment.NewLine + "No channel joined. Try /join #<channel>";

                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
                    }
                }
                else
                {
                    if (input.GetUpperBound(0) > 0)
                    {
                        sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0] + " " + input[1]);
                    }
                    else
                    {
                        sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0]);
                    }
                }
            }
            input_box.Text = "";
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            
            IRCBot(bw);
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string[] server = conf.server.Split('.');
            string tmp_server_name = "No Server Specified";
            if (server.GetUpperBound(0) > 0)
            {
                tmp_server_name = server[1];
            }
            else
            {
                restart = false;
            }
            Control control = new Control();
            control = tabControl1.Controls.Find("output_box_" + tmp_server_name + "_system", true)[0];
            RichTextBox output_box = (RichTextBox)control;
            if (restart == true)
            {
                output_box.AppendText(Environment.NewLine + "Restart Attempt " + restart_attempts + " [" + Math.Pow(2, Convert.ToDouble(restart_attempts)) + " Seconds Delay]" + Environment.NewLine);
                connect();
            }
            else
            {
                if (tmp_server_name == "No Server Specified")
                {
                    output_box.AppendText(Environment.NewLine + "Please add a server to connect to." + Environment.NewLine);
                }
                restart_attempts = 0;
                output_box.AppendText(Environment.NewLine + "Exited" + Environment.NewLine);
                connectToolStripMenuItem.Text = "Connect";
            }
        }

        public void IRCBot(BackgroundWorker bw)
        {
            if (restart == true)
            {
                Thread.Sleep(Convert.ToInt32(Math.Pow(2, Convert.ToDouble(restart_attempts))) * 1000);
            }
            this.config = conf;
            try
            {
                IRCConnection = new TcpClient(config.server, config.port);
                restart = false;
            }
            catch
            {
                output = Environment.NewLine + "Connection Error";

                lock (listLock)
                {
                    if (queue_text.Count >= 1000)
                    {
                        queue_text.RemoveAt(0);
                    }
                    queue_text.Add(output);
                }
                restart = true;
                restart_attempts++;
            }

            if (restart == false)
            {
                try
                {
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
                catch
                {
                    output = Environment.NewLine + "Communication error";

                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
                    }
                }
                finally
                {
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
                    output = Environment.NewLine + ":" + conf.nick + " " + cmd;

                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
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
                                    output = Environment.NewLine + ":" + conf.nick + " " + first + ":" + msg;
                                    lock (listLock)
                                    {
                                        if (queue_text.Count >= 1000)
                                        {
                                            queue_text.RemoveAt(0);
                                        }
                                        queue_text.Add(output);
                                    }
                                    msg = " " + word;
                                }
                            }
                            if (msg.Trim() != "")
                            {
                                msg = msg.Remove(0, 1);
                                sw.WriteLine(first + ":" + msg);
                                sw.Flush();
                                output = Environment.NewLine + ":" + conf.nick + " " + first + ":" + msg;
                                lock (listLock)
                                {
                                    if (queue_text.Count >= 1000)
                                    {
                                        queue_text.RemoveAt(0);
                                    }
                                    queue_text.Add(output);
                                }
                            }
                        }
                        else
                        {
                            sw.WriteLine(cmd + " " + param);
                            sw.Flush();
                            output = Environment.NewLine + ":" + conf.nick + " " + cmd + " " + param;

                            lock (listLock)
                            {
                                if (queue_text.Count >= 1000)
                                {
                                    queue_text.RemoveAt(0);
                                }
                                queue_text.Add(output);
                            }
                        }
                    }
                    else
                    {
                        sw.WriteLine(cmd + " " + param);
                        sw.Flush();
                        output = Environment.NewLine + ":" + conf.nick + " " + cmd + " " + param;

                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
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
            first_run = false;
            while (shouldRun)
            {
                restart = false;
                restart_attempts = 0;
                Thread.Sleep(20);
                data = sr.ReadLine();

                output = Environment.NewLine + data;

                lock (listLock)
                {
                    if (queue_text.Count >= 1000)
                    {
                        queue_text.RemoveAt(0);
                    }
                    queue_text.Add(output);
                }

                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5, StringSplitOptions.RemoveEmptyEntries);
                
                if (ex[0] == "PING")
                {
                    sendData("PONG", ex[1]);
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
                                    char[] Separator = new char[] { ' ' };
                                    string[] name_line = line.Split(Separator, 5);
                                    while (name_line.GetUpperBound(0) <= 3)
                                    {
                                        output = Environment.NewLine + line;
                                        lock (listLock)
                                        {
                                            if (queue_text.Count >= 1000)
                                            {
                                                queue_text.RemoveAt(0);
                                            }
                                            queue_text.Add(output);
                                        }
                                        line = sr.ReadLine();
                                        name_line = line.Split(Separator, 5);
                                    }
                                    while (name_line[3] != "=")
                                    {
                                        output = Environment.NewLine + line;
                                        lock (listLock)
                                        {
                                            if (queue_text.Count >= 1000)
                                            {
                                                queue_text.RemoveAt(0);
                                            }
                                            queue_text.Add(output);
                                        }
                                        line = sr.ReadLine();
                                        name_line = line.Split(charSeparator, 5);
                                        while (name_line.GetUpperBound(0) <= 3)
                                        {
                                            output = Environment.NewLine + line;
                                            lock (listLock)
                                            {
                                                if (queue_text.Count >= 1000)
                                                {
                                                    queue_text.RemoveAt(0);
                                                }
                                                queue_text.Add(output);
                                            }
                                            line = sr.ReadLine();
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
                                                names = names_list[1].Split(' ');
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
                                            while (name_line.GetUpperBound(0) <= 3)
                                            {
                                                output = Environment.NewLine + line;
                                                lock (listLock)
                                                {
                                                    if (queue_text.Count >= 1000)
                                                    {
                                                        queue_text.RemoveAt(0);
                                                    }
                                                    queue_text.Add(output);
                                                }
                                                line = sr.ReadLine();
                                                name_line = line.Split(charSeparator, 5);
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
                            while (name_line.GetUpperBound(0) <= 3)
                            {
                                output = Environment.NewLine + line;
                                lock (listLock)
                                {
                                    if (queue_text.Count >= 1000)
                                    {
                                        queue_text.RemoveAt(0);
                                    }
                                    queue_text.Add(output);
                                }
                                line = sr.ReadLine();
                                name_line = line.Split(Separator, 5);
                            }
                            while (name_line[3] != "=")
                            {
                                output = Environment.NewLine + line;
                                lock (listLock)
                                {
                                    if (queue_text.Count >= 1000)
                                    {
                                        queue_text.RemoveAt(0);
                                    }
                                    queue_text.Add(output);
                                }
                                line = sr.ReadLine();
                                name_line = line.Split(charSeparator, 5);
                                while (name_line.GetUpperBound(0) <= 3)
                                {
                                    output = Environment.NewLine + line;
                                    lock (listLock)
                                    {
                                        if (queue_text.Count >= 1000)
                                        {
                                            queue_text.RemoveAt(0);
                                        }
                                        queue_text.Add(output);
                                    }
                                    line = sr.ReadLine();
                                    name_line = line.Split(charSeparator, 5);
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
                                        names = names_list[1].Split(' ');
                                        for (int i = 0; i <= names.GetUpperBound(0); i++)
                                        {
                                            int user_access = get_access_num(names[i].Remove(1));
                                            tmp_list.Add(user_access + ":" + names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+'));
                                        }
                                    }
                                    line = sr.ReadLine();
                                    name_line = line.Split(charSeparator, 5);
                                    while (name_line.GetUpperBound(0) <= 3)
                                    {
                                        output = Environment.NewLine + line;
                                        lock (listLock)
                                        {
                                            if (queue_text.Count >= 1000)
                                            {
                                                queue_text.RemoveAt(0);
                                            }
                                            queue_text.Add(output);
                                        }
                                        line = sr.ReadLine();
                                        name_line = line.Split(charSeparator, 5);
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
            bool connected = false;
            while (connected == false)
            {
                Thread.Sleep(30);
                string line = sr.ReadLine();
                char[] charSeparator = new char[] { ' ' };
                string[] new_line = line.Split(charSeparator, 5);
                output = Environment.NewLine + line;
                lock (listLock)
                {
                    if (queue_text.Count >= 1000)
                    {
                        queue_text.RemoveAt(0);
                    }
                    queue_text.Add(output);
                }
                if (new_line.GetUpperBound(0) > 3)
                {
                    if (new_line[3] == ":End" && new_line[4] == "of /MOTD command.")
                    {
                        connected = true;
                    }
                }
            }
            identify();
            bool joined = false;
            while (joined == false)
            {
                Thread.Sleep(30);
                string line = sr.ReadLine();
                char[] charSeparator = new char[] { ' ' };
                string[] new_line = line.Split(charSeparator, 5);
                output = Environment.NewLine + line;
                lock (listLock)
                {
                    if (queue_text.Count >= 1000)
                    {
                        queue_text.RemoveAt(0);
                    }
                    queue_text.Add(output);
                }
                if (new_line.GetUpperBound(0) > 3)
                {
                    if (new_line[3] == ":Password" && new_line[4] == "accepted - you are now recognized.")
                    {
                        checkRegisterationTimer.Stop();
                        joined = true;
                    }
                }
            }
            // Joins all the channels in the channel list
            if (conf.chans != "")
            {
                string[] channels = conf.chans.Split(',');
                for (int x = 0; x <= channels.GetUpperBound(0); x++)
                {
                    sendData("JOIN", "#" + channels[x].TrimStart('#'));
                    string line = sr.ReadLine();
                    output = Environment.NewLine + line;
                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
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
                        output = Environment.NewLine + line;
                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator, 5);
                    }
                    while (name_line[3] != "=")
                    {
                        output = Environment.NewLine + line;
                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator, 5);
                        while (name_line.GetUpperBound(0) < 3)
                        {
                            output = Environment.NewLine + line;
                            lock (listLock)
                            {
                                if (queue_text.Count >= 1000)
                                {
                                    queue_text.RemoveAt(0);
                                }
                                queue_text.Add(output);
                            }
                            line = sr.ReadLine();
                            name_line = line.Split(charSeparator, 5);
                        }
                    }
                    string[] names_list = name_line[4].Split(':');
                    string[] names = names_list[1].Split(' ');
                    while (name_line[4] != ":End of /NAMES list.")
                    {
                        channel_found = true;
                        names_list = name_line[4].Split(':');
                        names = names_list[1].Split(' ');
                        for (int i = 0; i <= names.GetUpperBound(0); i++)
                        {
                            int user_access = get_access_num(names[i].Remove(1));
                            tmp_list.Add(user_access + ":" + names[i].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+'));
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator, 5);
                        output = Environment.NewLine + line;
                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
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
            char[] charSeparator = new char[] { ' ' };
            string[] name_line = line.Split(charSeparator);
            while (name_line[2] != conf.nick || name_line[3] != new_nick[0])
            {
                line = sr.ReadLine();
                name_line = line.Split(charSeparator);
            }
            if (name_line[5] == "such")
            {
                while (name_line.GetUpperBound(0) < 6)
                {
                    output = Environment.NewLine + line;
                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
                    }
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                }
                while (name_line[6] != "/WHOIS")
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                    while (name_line.GetUpperBound(0) < 6)
                    {
                        output = Environment.NewLine + line;
                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator);
                    }
                }
                sendData("WHOWAS", new_nick[0]);
                line = sr.ReadLine();
                string[] tmp_line = line.Split(charSeparator);
                while (tmp_line[3].Equals(":Server"))
                {
                    sendData("WHOWAS", new_nick[0]);
                    line = sr.ReadLine();
                    tmp_line = line.Split(charSeparator);
                }
                while (tmp_line[2] != conf.nick || tmp_line[3] != new_nick[0])
                {
                    line = sr.ReadLine();
                    tmp_line = line.Split(charSeparator);
                }
                access = tmp_line[5];
                while (name_line.GetUpperBound(0) < 6)
                {
                    output = Environment.NewLine + line;
                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
                    }
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                }
                while (tmp_line[6] != "WHOWAS")
                {
                    line = sr.ReadLine();
                    tmp_line = line.Split(charSeparator);
                    while (name_line.GetUpperBound(0) < 6)
                    {
                        output = Environment.NewLine + line;
                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
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
                    output = Environment.NewLine + line;
                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
                    }
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                }
                while (name_line[6] != "/WHOIS")
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                    while (name_line.GetUpperBound(0) < 6)
                    {
                        output = Environment.NewLine + line;
                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
                        }
                        line = sr.ReadLine();
                        name_line = line.Split(charSeparator);
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
            while (name_line.GetUpperBound(0) < 4)
            {
                output = Environment.NewLine + line;
                lock (listLock)
                {
                    if (queue_text.Count >= 1000)
                    {
                        queue_text.RemoveAt(0);
                    }
                    queue_text.Add(output);
                }
                line = sr.ReadLine();
                name_line = line.Split(charSeparator, 5);
            }
            while (name_line[4] != ":End of /WHOIS list.")
            {
                if(name_line[4].Equals(":has identified for this nick"))
                {
                    identified = true;
                }
                line = sr.ReadLine();
                name_line = line.Split(charSeparator, 5);
                while (name_line.GetUpperBound(0) < 4)
                {
                    output = Environment.NewLine + line;
                    lock (listLock)
                    {
                        if (queue_text.Count >= 1000)
                        {
                            queue_text.RemoveAt(0);
                        }
                        queue_text.Add(output);
                    }
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator, 5);
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
                    if (conf.module_config[x][0].Equals("Moderation"))
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
                MessageBox.Show(ex.ToString());
            }
            return access_num;
        }

        private void UpdateOutput(object sender, EventArgs e)
        {
            try
            {
                Thread updateOutputThread = null;
                updateOutputThread = new Thread(new ThreadStart(this.ThreadProcSafeOutput));
                updateOutputThread.Name = "updateOutput";
                updateOutputThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void UpdateOutput_final(string text)
        {
            
            RichTextBox output_box = new RichTextBox();
            output_box = output_box_system;
            if (output_box.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(UpdateOutput_final);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                try
                {
                    string[] server = conf.server.Split('.');
                    string tmp_server_name = "No Server Specified";
                    if (server.GetUpperBound(0) > 0)
                    {
                        tmp_server_name = server[1];
                    }
                    string channel = "System";
                    string tab_name = "System";
                    string message = "";
                    string nickname = "";
                    string pattern = "[^a-zA-Z0-9]"; //regex pattern
                    Control control = tabControl1.Controls.Find("output_box_" + tmp_server_name + "_system", true)[0];
                    char[] charSeparator = new char[] { ' ' };
                    string[] tmp_lines = text.Split(charSeparator, 4);
                    string time_stamp = DateTime.Now.ToString("hh:mm tt");
                    string date_stamp = DateTime.Now.ToString("yyyy-MM-dd");
                    string font_color = "#000000";
                    tmp_lines[1] = tmp_lines[1].ToLower();
                    if (tmp_lines.GetUpperBound(0) > 1)
                    {
                        if (tmp_lines[1].Equals("notice"))
                        {
                            channel = tmp_lines[3].TrimStart(':').TrimStart('[');
                            string[] tmp_msg = channel.Split(charSeparator, 2);
                            if (channel.StartsWith("#"))
                            {
                                tab_name = tmp_msg[0].TrimStart('#').TrimEnd(']');
                                channel = "#" + tab_name;
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
                                tab_name = "System";
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
                            nickname = "--<" + tmp_lines[0].TrimStart(':').Split('!')[0] + ">--  ";
                            font_color = "#B037B0";
                        }
                        else if (tmp_lines[1].Equals("privmsg"))
                        {
                            channel = tmp_lines[2].TrimStart(':');
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                                tab_name = channel.TrimStart('#');
                            }
                            else if (channel.Equals(conf.nick))
                            {
                                tab_name = nickname;
                                channel = nickname;
                            }
                            else if (nickname.Equals(conf.nick))
                            {
                                tab_name = channel;
                            }
                            message = tmp_lines[3].Remove(0, 1);
                            nickname = "<" + nickname + ">  ";
                            font_color = "#000000";
                        }
                        else if (tmp_lines[1].Equals("join"))
                        {
                            channel = tmp_lines[2].TrimStart(':');
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                                tab_name = channel.TrimStart('#');
                            }
                            else
                            {
                                tab_name = "System";
                                channel = "System";
                            }
                            message = nickname + " has joined " + channel;
                            nickname = "";
                            font_color = "#3DCC3D";
                        }
                        else if (tmp_lines[1].Equals("part"))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                                tab_name = channel.TrimStart('#');
                            }
                            else
                            {
                                tab_name = "System";
                                channel = "System";
                            }
                            message = nickname + " has left " + channel;
                            nickname = "";
                            font_color = "#66361F";
                        }
                        else if (tmp_lines[1].Equals("mode"))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                                tab_name = channel.TrimStart('#');
                            }
                            else if (channel.Equals(conf.nick))
                            {
                                tab_name = "System";
                                channel = "System";
                            }
                            if (nickname.Equals(conf.nick))
                            {
                                message = "";
                            }
                            else
                            {
                                message = nickname + " has set Mode " + tmp_lines[3];
                            }
                            nickname = "";
                            font_color = "#000000";
                        }
                        else if (tmp_lines[1].Equals("topic"))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            tab_name = channel.TrimStart('#');
                            message = nickname + " has set Topic " + tmp_lines[3];
                            nickname = "";
                            font_color = "#000000";
                        }
                        else if (tmp_lines[1].Equals("kick"))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                                tab_name = channel.TrimStart('#');
                            }
                            else if (channel.Equals(conf.nick))
                            {
                                tab_name = "System";
                                channel = "System";
                            }
                            message = nickname + " has kicked " + tmp_lines[3].Replace(':', '(') + ")";
                            nickname = "";
                            font_color = "#C73232";
                        }
                        else
                        {
                            if (tmp_lines.GetUpperBound(0) > 2)
                            {
                                string[] new_lines = tmp_lines[3].Split(charSeparator, 2);
                                if (tmp_lines[2].Equals(conf.nick) && tmp_lines[3].StartsWith("#") && new_lines.GetUpperBound(0) > 0)
                                {
                                    if (new_lines[1] != ":End of /NAMES list.")
                                    {
                                        channel = new_lines[0];
                                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                                        tab_name = channel.TrimStart('#');
                                        message = "Topic for " + channel + " is: " + new_lines[1].TrimStart(':');
                                        nickname = "";
                                        font_color = "#B037B0";
                                    }
                                    else
                                    {
                                        channel = "System";
                                        tab_name = "System";
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
                                        font_color = "000000";
                                    }
                                }
                                else
                                {
                                    channel = "System";
                                    tab_name = "System";
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
                                    font_color = "000000";
                                }
                            }
                            else
                            {
                                channel = "System";
                                tab_name = "System";
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
                                font_color = "000000";
                            }
                        }
                        tab_name = Regex.Replace(tab_name, pattern, "_");
                        string[] nick = tmp_lines[0].Split('!');
                        if (channel != "System")
                        {
                            if (channel.StartsWith("#"))
                            {
                                if (tabControl1.Controls.Find("output_box_chan_" + tmp_server_name + "_" + tab_name, true).GetUpperBound(0) < 0)
                                {
                                    add_tab(channel);
                                }
                                control = tabControl1.Controls.Find("output_box_chan_" + tmp_server_name + "_" + tab_name, true)[0];
                            }
                            else
                            {
                                if (tabControl1.Controls.Find("output_box_user_" + tmp_server_name + "_" + tab_name, true).GetUpperBound(0) < 0)
                                {
                                    add_tab(channel);
                                }
                                control = tabControl1.Controls.Find("output_box_user_" + tmp_server_name + "_" + tab_name, true)[0];
                            }
                        }
                    }
                    output_box = (RichTextBox)control;
                    if (nickname != "" || message != "")
                    {
                        int before_length = output_box.Text.Length + 1;
                        int nickname_length = nickname.Length;
                        int message_length = message.Length;
                        int timestamp_length = time_stamp.Length;
                        string line = "[" + time_stamp + "] " + nickname + message;
                        int line_length = line.Length;
                        Color actColor;
                        actColor = System.Drawing.ColorTranslator.FromHtml(font_color);
                        output_box.AppendText(line + Environment.NewLine);
                        //timstamp coloring
                        output_box.SelectionStart = before_length;
                        output_box.SelectionLength = timestamp_length + 2;
                        output_box.SelectionColor = Color.Black;
                        if (nickname_length > 0)
                        {
                            //nick coloring
                            output_box.SelectionStart = before_length + timestamp_length + 2;
                            output_box.SelectionLength = nickname_length - 1;
                            output_box.SelectionColor = System.Drawing.ColorTranslator.FromHtml("#C73232");
                        }
                        //message coloring
                        output_box.SelectionStart = before_length + timestamp_length + 2 + nickname_length;
                        output_box.SelectionLength = message_length;
                        output_box.SelectionColor = actColor;

                        output_box.SelectionStart = output_box.Text.Length;
                        output_box.ScrollToCaret();
                    }
                    this.Text = conf.name;
                    if (conf.keep_logs.Equals("True"))
                    {
                        string file_name = "";
                        if (channel.StartsWith("#"))
                        {
                            file_name = tmp_server_name + "-#" + tab_name + ".log";
                        }
                        else
                        {
                            file_name = tmp_server_name + "-" + tab_name + ".log";
                        }
                        if (conf.logs_path == "")
                        {
                            conf.logs_path = cur_dir + "\\logs";
                        }
                        if (Directory.Exists(conf.logs_path))
                        {
                            StreamWriter log_file = File.AppendText(conf.logs_path + "\\" + file_name);
                            log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + text);
                            log_file.Close();
                        }
                        else
                        {
                            Directory.CreateDirectory(conf.logs_path);
                            StreamWriter log_file = File.AppendText(conf.logs_path + "\\" + file_name);
                            log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + text);
                            log_file.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void ThreadProcSafeOutput()
        {
            try
            {
                if (queue_text.Count > 0)
                {
                    string text = "";
                    lock (listLock)
                    {
                        text = string.Join("", queue_text.ToArray());
                        queue_text.Clear();
                    }
                    string[] stringSeparators = new string[] { "\r\n" };
                    string[] lines = text.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    for (int x = 0; x <= lines.GetUpperBound(0); x++)
                    {
                        try
                        {
                            UpdateOutput_final(lines[x]);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void add_tab(string channel)
        {
            if (tabControl1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(add_tab);
                this.Invoke(d, new object[] { channel });
            }
            else
            {
                string[] server = conf.server.Split('.');
                string tmp_server_name = "No Server Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
                string pattern = "[^a-zA-Z0-9]"; //regex pattern
                string tab_name = channel.TrimStart('#');
                tab_name = Regex.Replace(tab_name, pattern, "_");
                RichTextBox box = new RichTextBox();
                box.Dock = System.Windows.Forms.DockStyle.Fill;
                box.Location = new System.Drawing.Point(3, 3);
                if (channel.StartsWith("#"))
                {
                    box.Name = "output_box_chan_" + tmp_server_name + "_" + tab_name;
                }
                else
                {
                    box.Name = "output_box_user_" + tmp_server_name + "_" + tab_name;
                }
                box.Size = new System.Drawing.Size(826, 347);
                box.TabIndex = 0;
                box.ReadOnly = true;
                box.Text = "";
                TabPage tabpage = new TabPage();
                tabpage.Controls.Add(box);
                tabpage.Location = new System.Drawing.Point(4, 22);
                if (channel.StartsWith("#"))
                {
                    tabpage.Name = "tabPage_chan_" + tmp_server_name + "_" + tab_name;
                }
                else
                {
                    tabpage.Name = "tabPage_user_" + tmp_server_name + "_" + tab_name;
                }
                tabpage.Padding = new System.Windows.Forms.Padding(3);
                tabpage.Size = new System.Drawing.Size(832, 353);
                tabpage.TabIndex = 0;
                tabpage.Text = channel;
                tabpage.UseVisualStyleBackColor = true;
                tabControl1.Controls.Add(tabpage);
                tabControl1.Update();
                box.LinkClicked += new LinkClickedEventHandler(link_Click);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sendData("QUIT", "Leaving");
            this.Close();
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            configuration newForm = new configuration(this);
            newForm.ShowDialog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void input_box_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                string input_tmp = input_box.Text;
                char[] charSeparator = new char[] { ' ' };
                string[] input = input_tmp.Split(charSeparator, 2);
                if (input[0].StartsWith("/"))
                {
                    if (input.GetUpperBound(0) > 0)
                    {
                        sendData(input[0].TrimStart('/'), input[1]);
                    }
                    else
                    {
                        sendData(input[0].TrimStart('/'), null);
                    }
                }
                else
                {
                    if (tabControl1.SelectedIndex == 0)
                    {
                        output = Environment.NewLine + "No channel joined. Try /join #<channel>";

                        lock (listLock)
                        {
                            if (queue_text.Count >= 1000)
                            {
                                queue_text.RemoveAt(0);
                            }
                            queue_text.Add(output);
                        }
                    }
                    else
                    {
                        if (input.GetUpperBound(0) > 0)
                        {
                            sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0] + " " + input[1]);
                        }
                        else
                        {
                            sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0]);
                        }
                    }
                }
                input_box.Text = "";
            }
        }

        public void update_conf()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(cur_dir + "\\config\\config.xml");
            XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/connection_settings");

            conf.name = list["name"].InnerText;
            conf.nick = list["nick"].InnerText;
            conf.pass = list["password"].InnerText;
            conf.email = list["email"].InnerText;
            conf.owner = list["owner"].InnerText;
            conf.port = Convert.ToInt32(list["port"].InnerText);
            conf.server = list["server"].InnerText;
            conf.chans = list["chan_list"].InnerText;
            conf.command = list["command_prefix"].InnerText;
            conf.keep_logs = list["keep_logs"].InnerText;
            conf.logs_path = list["logs_path"].InnerText;
            conf.max_message_length = Convert.ToInt32(list["max_message_length"].InnerText);

            conf.module_config.Clear();
            XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/modules/module");
            foreach (XmlNode xn in xnList)
            {
                List<string> tmp_list = new List<string>();
                String module_name = xn["name"].InnerText;
                tmp_list.Add(module_name);
                tmp_list.Add(xn["enabled"].InnerText);

                XmlNodeList optionList = xn.ChildNodes;
                foreach (XmlNode option in optionList)
                {
                    if (option.Name.Equals("options"))
                    {
                        XmlNodeList Options = option.ChildNodes;
                        foreach (XmlNode options in Options)
                        {
                            switch (options["type"].InnerText)
                            {
                                case "textbox":
                                    tmp_list.Add(options["value"].InnerText);
                                    break;
                                case "checkbox":
                                    tmp_list.Add(options["checked"].InnerText);
                                    break;
                            }
                        }
                    }
                }
                conf.module_config.Add(tmp_list);
            }

            conf.command_access.Clear();
            string list_file = cur_dir + "\\config\\help.txt";
            if (File.Exists(list_file))
            {
                string[] file = System.IO.File.ReadAllLines(list_file);
                foreach (string file_line in file)
                {
                    List<string> tmp_list2 = new List<string>();
                    string[] split = file_line.Split(':');
                    string command_access = split[1];
                    string command = split[2];
                    tmp_list2.Add(command);
                    tmp_list2.Add(command_access);
                    conf.command_access.Add(tmp_list2);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string name = tabControl1.SelectedTab.Text;
            int selected_tab = tabControl1.SelectedIndex;
            if (name.StartsWith("#") == true)
            {
                sendData("PART", name);
            }
            tabControl1.TabPages.RemoveAt(selected_tab);
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (connectToolStripMenuItem.Text.Equals("Disconnect"))
            {
                this.backgroundWorker1.CancelAsync();
                output = Environment.NewLine + "Disconnected";

                lock (listLock)
                {
                    if (queue_text.Count >= 1000)
                    {
                        queue_text.RemoveAt(0);
                    }
                    queue_text.Add(output);
                }
                output = "";
                if (sr != null)
                    sr.Close();
                if (sw != null)
                    sw.Close();
                if (ns != null)
                    ns.Close();
                if (IRCConnection != null)
                    IRCConnection.Close();
                connectToolStripMenuItem.Text = "Connect";
                Thread.Sleep(50);
                updateOutput.Stop();
            }
            else
            {
                connect();
            }
        }
    }
}