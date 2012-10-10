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
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using search.api;

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
}

namespace IRCBot
{
    public partial class Form1 : Form
    {
        TcpClient IRCConnection = null;
        IRCConfig config;
        NetworkStream ns = null;
        StreamReader sr = null;
        StreamWriter sw = null;

        private string output = "";
        private string cur_dir = "";
        private List<string> queue_text = new List<string>();
        private System.Windows.Forms.Timer updateOutput = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer checkRegisterationTimer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer Spam_Check_Timer = new System.Windows.Forms.Timer();
        private System.Windows.Forms.Timer Spam_Timer = new System.Windows.Forms.Timer();
        private bool spam_activated = false;
        private int spam_count = 0;
        private bool restart = false;
        private int restart_attempts = 0;
        bool shouldRun = true;
        string[] file;

        private readonly object listLock = new object();

        private IRCConfig conf = new IRCConfig();

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

        public Form1()
        {
            InitializeComponent();

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
            string list_file = cur_dir + "\\modules\\AI\\dictionary.txt";
            if (File.Exists(list_file))
            {
                file = System.IO.File.ReadAllLines(list_file);
            }

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
                nodeServer.InnerText = "";
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
                xmlDoc.AppendChild(node);
                xmlDoc.Save(cur_dir + "\\config\\config.xml");
                xmlDoc.Load(cur_dir + "\\config\\config.xml");
            }
            XmlNode list = xmlDoc.SelectSingleNode("connection_settings");

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

            Spam_Check_Timer.Interval = conf.spam_threshold;
            Spam_Check_Timer.Start();

            Spam_Timer.Interval = conf.spam_timout;

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
            Control control = new Control();
            control = tabControl1.Controls.Find("output_box_system", true)[0];
            RichTextBox output_box = (RichTextBox)control;
            if (restart == true)
            {
                output_box.AppendText(Environment.NewLine + "Restart Attempt " + restart_attempts + " [" + Math.Pow(2, Convert.ToDouble(restart_attempts)) + " Seconds Delay]" + Environment.NewLine);
                connect();
            }
            else
            {
                restart_attempts = 0;
                output_box.AppendText(Environment.NewLine + "Exited" + Environment.NewLine);
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
                    sendData("PASS", config.pass);
                    sendData("USER", config.nick + " inb4u.com " + " inb4u.com" + " :" + config.name);
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

        public void IRCWork()
        {
            string[] ex;
            string data;

            joinChannels();
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
                ex = data.Split(charSeparator, 5);
                
                if (ex[0] == "PING")
                {
                    sendData("PONG", ex[1]);
                }

                if (spam_activated == false)
                {
                    shouldRun = parse_message(ex);
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
                        checkRegisterationTimer.Stop();
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
            string[] channels = conf.chans.Split(',');
            for (int x = 0; x <= channels.GetUpperBound(0); x++)
            {
                sendData("JOIN", channels[x]);
            }
        }

        private void checkRegistration(object sender, EventArgs e)
        {
            checkRegisterationTimer.Stop();
            register_nick(conf.nick, conf.pass, conf.email);
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

        private void identify()
        {
            sendData("PRIVMSG", "NickServ :Identify " + conf.pass);
        }

        private string get_user_host(string nick)
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
                while (name_line[6] != "/WHOIS")
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
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
                if (name_line.GetUpperBound(0) > 5)
                {
                    while (tmp_line[6] != "WHOWAS")
                    {
                        line = sr.ReadLine();
                        tmp_line = line.Split(charSeparator);
                    }
                }
            }
            else
            {
                access = name_line[5];
                while (name_line[6] != "/WHOIS")
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator);
                }
            }
            return access;
        }

        private int get_user_access(string nick, string channel)
        {
            string access = "";
            string user_access = "";
            access = get_access_list(nick, channel);
            if (access == "")
            {
                sendData("NAMES", channel);

                string line = sr.ReadLine();
                char[] charSeparator = new char[] { ' ' };
                string[] name_line = line.Split(charSeparator, 5);
                string[] names_list = name_line[4].Split(':');
                string[] names = names_list[1].Split(' ');
                for (int x = 0; x <= names.GetUpperBound(0); x++)
                {
                    if (nick == names[x].Remove(0, 1))
                    {
                        user_access = names[x].Remove(1);
                    }
                }
                while (name_line[4] != ":End of /NAMES list.")
                {
                    line = sr.ReadLine();
                    name_line = line.Split(charSeparator, 5);
                }
                if (user_access.Contains('~'))
                {
                    access = "9";
                }
                else if (user_access.Contains('&'))
                {
                    access = "8";
                }
                else if (user_access.Contains('@'))
                {
                    access = "7";
                }
                else if (user_access.Contains('%'))
                {
                    access = "4";
                }
                else if (user_access.Contains('+'))
                {
                    access = "3";
                }
                else if (user_access != "")
                {
                    access = "1";
                }
                else
                {
                    access = "0";
                }
            }
            string[] owners = conf.owner.Split(','); // Get list of owners
            for (int x = 0; x <= owners.GetUpperBound(0); x++)
            {
                if (nick.Equals(owners[x]))
                {
                    access += "," + "10";
                }
            }
            string[] tmp_access = access.Split(',');
            int access_num = 0;
            foreach (string access_line in tmp_access)
            {
                if (Convert.ToInt32(access_line) > access_num)
                {
                    access_num = Convert.ToInt32(access_line);
                }
            }
            return access_num;
        }

        private bool parse_message(string[] line)
        {
            bool nick_owner = false;
            bool access_needed = false;
            int nick_access = 0;

            // On Message Events events
            if (line[1] == "PRIVMSG")
            {
                if (line.GetUpperBound(0) >= 3) // If valid Input
                {
                    string[] owners = conf.owner.Split(','); // Get list of owners
                    string[] user_info = line[0].Split('@');
                    string[] nick = user_info[0].Split('!');
                    if (nick.GetUpperBound(0) > 0)
                    {
                        for (int x = 0; x <= owners.GetUpperBound(0); x++)
                        {
                            if (nick[0].TrimStart(':') == owners[x])
                            {
                                nick_owner = true;
                            }
                        }

                        string command = line[3]; //grab the command sent
                        command = command.ToLower();
                        string msg_type = command.TrimStart(':');
                        if (msg_type.StartsWith(conf.command) == true)
                        {
                            access_needed = true;
                            command = command.Remove(0, 2);
                        }

                        if (access_needed == true)
                        {
                            if (line[2].StartsWith("#") == true) // From Channel
                            {
                                char[] charS = new char[] { ' ' };
                                // Owner Commands
                                switch (command)
                                {
                                    case "access":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] parse = line[4].Split(' ');
                                                if (parse.GetUpperBound(0) > 0)
                                                {
                                                    set_access_list(parse[0], line[2], parse[1]);
                                                }
                                                else
                                                {
                                                    sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "listaccess":
                                        if (nick_owner == true)
                                        {
                                            list_access_list(nick[0].TrimStart(':'), line[2]);
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delaccess":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] parse = line[4].Split(' ');
                                                if (parse.GetUpperBound(0) > 0)
                                                {
                                                    del_access_list(parse[0], line[2], parse[1]);
                                                }
                                                else
                                                {
                                                    sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "addowner":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                add_owner(line[4]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "delowner":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                del_owner(line[4]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "id":
                                        if (nick_owner == true)
                                        {
                                            identify();
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "join":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                line[4].Replace(' ', ',');
                                                sendData("JOIN", line[4]); //if the command is !join send the "JOIN" command to the server with the parameters set by the user
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "part":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                line[4].Replace(' ', ',');
                                                sendData("PART", line[4]); //if the command is !join send the "JOIN" command to the server with the parameters set by the user
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "say":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + line[4]); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "quit":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) <= 3)
                                            {
                                                sendData("QUIT", "Leaving");
                                            }
                                            else
                                            {
                                                sendData("QUIT", ":" + line[4]); //if the command is quit, send the QUIT command to the server with a quit message
                                            }
                                            shouldRun = false; //turn shouldRun to false - the server will stop sending us data so trying to read it will not work and result in an error. This stops the loop from running and we will close off the connections properly
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "founder":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 9)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " +q " + line[4]);
                                                set_access_list(line[4], line[2], "9");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "defounder":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 9)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " -q " + line[4]);
                                                del_access_list(line[4], line[2], "9");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "sop":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 8)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " +a " + line[4]);
                                                set_access_list(line[4], line[2], "8");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "desop":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 8)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " -a " + line[4]);
                                                del_access_list(line[4], line[2], "8");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "op":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " +o " + line[4]);
                                                set_access_list(line[4], line[2], "7");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "deop":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " -o " + line[4]);
                                                del_access_list(line[4], line[2], "7");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "mode":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    sendData("MODE", line[2] + " " + new_line[0] + " :" + new_line[1]);
                                                }
                                                else
                                                {
                                                    sendData("MODE", line[2] + " " + new_line[0]);
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "topic":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                if (new_line.GetUpperBound(0) > 0)
                                                {
                                                    sendData("TOPIC", line[2] + " :" + new_line[0] + " " + new_line[1]);
                                                }
                                                else
                                                {
                                                    sendData("TOPIC", line[2] + " :" + new_line[0]);
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "invite":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                sendData("INVITE", new_line[0] + " " + line[2]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "b":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 6)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                bool tmp_owner = false;
                                                string nicks = new_line[0].TrimStart(':');
                                                string[] total_nicks = nicks.Split(',');
                                                for (int x = 0; x <= owners.GetUpperBound(0); x++)
                                                {
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(owners[x], StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_owner = true;
                                                        }
                                                    }
                                                }
                                                if (tmp_owner == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't ban my owner!");
                                                }
                                                else if (new_line[0].Equals(conf.nick))
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't ban me!");
                                                }
                                                else
                                                {
                                                    int sent_nick_access = get_user_access(new_line[0].TrimStart(':'), line[2]);
                                                    bool allowed = false;
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        allowed = true;
                                                    }
                                                    if (allowed == true)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            sendData("MODE", line[2] + " +b " + new_line[0] + " :" + new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            sendData("MODE", line[2] + " +b " + new_line[0] + " :No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "kb":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 6)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                bool tmp_owner = false;
                                                string nicks = new_line[0].TrimStart(':');
                                                string[] total_nicks = nicks.Split(',');
                                                for (int x = 0; x <= owners.GetUpperBound(0); x++)
                                                {
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(owners[x], StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_owner = true;
                                                        }
                                                    }
                                                }
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (tmp_owner == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick-ban my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick-ban me!");
                                                }
                                                else
                                                {
                                                    int sent_nick_access = get_user_access(new_line[0].TrimStart(':'), line[2]);
                                                    bool allowed = false;
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        allowed = true;
                                                    }
                                                    if(allowed == true)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            sendData("MODE", line[2] + " +b " + new_line[0] + " :" + new_line[1]);
                                                            sendData("KICK", line[2] + " " + new_line[0] + " :" + new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            sendData("MODE", line[2] + " +b " + new_line[0] + " :No Reason");
                                                            sendData("KICK", line[2] + " " + new_line[0] + " :No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ak":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                bool tmp_owner = false;
                                                string nicks = new_line[0].TrimStart(':');
                                                string[] total_nicks = nicks.Split(',');
                                                for (int x = 0; x <= owners.GetUpperBound(0); x++)
                                                {
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(owners[x], StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_owner = true;
                                                        }
                                                    }
                                                }
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (tmp_owner == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick me!");
                                                }
                                                else
                                                {
                                                    int sent_nick_access = get_user_access(new_line[0].TrimStart(':'), line[2]);
                                                    bool allowed = false;
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        allowed = true;
                                                    }
                                                    if (allowed == true)
                                                    {
                                                        string target_host = get_user_host(new_line[0]);
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            add_auto(new_line[0], line[2], target_host, "k", new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            add_auto(new_line[0], line[2], target_host, "k", "No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "ab":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                string target_host = get_user_host(new_line[0]);
                                                bool tmp_owner = false;
                                                string nicks = new_line[0].TrimStart(':');
                                                string[] total_nicks = nicks.Split(',');
                                                for (int x = 0; x <= owners.GetUpperBound(0); x++)
                                                {
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(owners[x], StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_owner = true;
                                                        }
                                                    }
                                                }
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (tmp_owner == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't ban my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't ban me!");
                                                }
                                                else
                                                {
                                                    int sent_nick_access = get_user_access(new_line[0].TrimStart(':'), line[2]);
                                                    bool allowed = false;
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        allowed = true;
                                                    }
                                                    if (allowed == true)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            add_auto(new_line[0], line[2], target_host, "b", new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            add_auto(new_line[0], line[2], target_host, "b", "No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "akb":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                string target_host = get_user_host(new_line[0]);
                                                bool tmp_owner = false;
                                                string nicks = new_line[0].TrimStart(':');
                                                string[] total_nicks = nicks.Split(',');
                                                for (int x = 0; x <= owners.GetUpperBound(0); x++)
                                                {
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(owners[x], StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_owner = true;
                                                        }
                                                    }
                                                }
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (tmp_owner == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick-ban my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick-ban me!");
                                                }
                                                else
                                                {
                                                    int sent_nick_access = get_user_access(new_line[0].TrimStart(':'), line[2]);
                                                    bool allowed = false;
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        allowed = true;
                                                    }
                                                    if (allowed == true)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            add_auto(new_line[0], line[2], target_host, "kb", new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            add_auto(new_line[0], line[2], target_host, "kb", "No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "deak":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string target_host = get_user_host(line[4]);
                                                del_auto(line[4], line[2], target_host, "k");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "deab":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string target_host = get_user_host(line[4]);
                                                del_auto(line[4], line[2], target_host, "b");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "deakb":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 7)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string target_host = get_user_host(line[4]);
                                                del_auto(line[4], line[2], target_host, "kb");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "hop":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 4)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " +h " + line[4]);
                                                set_access_list(line[4], line[2], "4");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "dehop":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 4)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " -h " + line[4]);
                                                del_access_list(line[4], line[2], "4");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "k":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 5)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] new_line = line[4].Split(charS, 2);
                                                bool tmp_owner = false;
                                                string nicks = new_line[0].TrimStart(':');
                                                string[] total_nicks = nicks.Split(',');
                                                for (int x = 0; x <= owners.GetUpperBound(0); x++)
                                                {
                                                    for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                    {
                                                        if (total_nicks[y].Equals(owners[x], StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            tmp_owner = true;
                                                        }
                                                    }
                                                }
                                                bool tmp_me = false;
                                                for (int y = 0; y <= total_nicks.GetUpperBound(0); y++)
                                                {
                                                    if (total_nicks[y].Equals(conf.name, StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        tmp_me = true;
                                                    }
                                                }
                                                if (tmp_owner == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick my owner!");
                                                }
                                                else if (tmp_me == true)
                                                {
                                                    sendData("PRIVMSG", line[2] + " :You can't kick me!");
                                                }
                                                else
                                                {
                                                    int sent_nick_access = get_user_access(new_line[0].TrimStart(':'), line[2]);
                                                    bool allowed = false;
                                                    if (nick_access >= sent_nick_access)
                                                    {
                                                        allowed = true;
                                                    }
                                                    if (allowed == true)
                                                    {
                                                        if (new_line.GetUpperBound(0) > 0)
                                                        {
                                                            sendData("KICK", line[2] + " " + new_line[0] + " :" + new_line[1]);
                                                        }
                                                        else
                                                        {
                                                            sendData("KICK", line[2] + " " + new_line[0] + " :No Reason");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "voice":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        // Voice Commands
                                        if (nick_access >= 3)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " +v " + line[4]);
                                                set_access_list(line[4], line[2], "3");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "devoice":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        // Voice Commands
                                        if (nick_access >= 3)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("MODE", line[2] + " -v " + line[4]);
                                                del_access_list(line[4], line[2], "3");
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        else
                                        {
                                            sendData("NOTICE", nick[0].TrimStart(':') + " :You do not have permission to use that command.");
                                        }
                                        break;
                                    case "help":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            help(line, nick[0].TrimStart(':'), line[2], nick_access);
                                        }
                                        break;
                                    case "intro":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                add_intro(nick[0].TrimStart(':'), line[2], line);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "introdelete":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            // Delete Introduction
                                            delete_intro(nick[0].TrimStart(':'), line[2]);
                                        }
                                        break;
                                    case "message":
                                        spam_count++;
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            add_message(nick[0].TrimStart(':'), line, line[2]);
                                        }
                                        else
                                        {
                                            sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                        }
                                        break;
                                    case "kme":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                sendData("KICK", line[2] + " " + nick[0].TrimStart(':') + " :" + line[4]);
                                            }
                                            else
                                            {
                                                sendData("KICK", line[2] + " " + nick[0].TrimStart(':') + " :No Reason");
                                            }
                                        }
                                        break;
                                    case "quote":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            get_quote(line[2]);
                                        }
                                        break;
                                    case "seen":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                seen(line[4], line[2]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "w":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                get_weather(line[4], line[2]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "weather":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                get_weather(line[4], line[2]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "f":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                get_forecast(line[4], line[2]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "forecast":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                // Add introduction
                                                get_forecast(line[4], line[2]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "google":
                                        spam_count++;
                                        nick_access = get_user_access(nick[0].TrimStart(':'), line[2]);
                                        if (nick_access >= 1)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                if (line[4].StartsWith("DCC SEND"))
                                                {
                                                    sendData("PRIVMSG", line[2] + " :Invalid Search Term");
                                                }
                                                else
                                                {
                                                    ISearchResult searchClass = new GoogleSearch(line[4]);
                                                    try
                                                    {
                                                        var list = searchClass.Search();
                                                        if (list.Count > 0)
                                                        {
                                                            foreach (var searchType in list)
                                                            {
                                                                sendData("PRIVMSG", line[2] + " :" + searchType.title.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'") + ": " + searchType.content.Replace("<b>", "").Replace("</b>", "").Replace("&quot;", "\"").Replace("&#39", "'"));
                                                                sendData("PRIVMSG", line[2] + " :" + searchType.url);
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            sendData("PRIVMSG", line[2] + " :No Results Found");
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        MessageBox.Show(ex.ToString());
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                }
                            }
                            else // From Query
                            {
                                switch (command)
                                {
                                    case "access":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                string[] parse = line[4].Split(' ');
                                                if (parse.GetUpperBound(0) > 1)
                                                {
                                                    set_access_list(parse[0], parse[1], parse[2]);
                                                }
                                                else
                                                {
                                                    sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                                }
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "addowner":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                add_owner(line[4]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "delowner":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                del_owner(line[4]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "id":
                                        if (nick_owner == true)
                                        {
                                            identify();
                                        }
                                        break;
                                    case "join":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                line[4].Replace(' ', ':');
                                                sendData("JOIN", line[4]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "part":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                line[4].Replace(' ', ':');
                                                sendData("PART", line[4]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[1] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "say":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) > 3)
                                            {
                                                char[] charS = new char[] { ' ' };
                                                string[] new_line = line[4].Split(charS, 2);
                                                sendData("PRIVMSG", new_line[0] + " :" + new_line[new_line.GetUpperBound(0)]);
                                            }
                                            else
                                            {
                                                sendData("PRIVMSG", line[2] + " :" + nick[0].TrimStart(':') + ", you need to include more info.");
                                            }
                                        }
                                        break;
                                    case "quit":
                                        if (nick_owner == true)
                                        {
                                            if (line.GetUpperBound(0) <= 3)
                                            {
                                                sendData("QUIT", "Leaving");
                                            }
                                            else
                                            {
                                                sendData("QUIT", ":" + line[4]);
                                            }
                                            shouldRun = false; //turn shouldRun to false - the server will stop sending us data so trying to read it will not work and result in an error. This stops the loop from running and we will close off the connections properly
                                        }
                                        break;
                                    case "message":
                                        spam_count++;
                                        if (line.GetUpperBound(0) > 3)
                                        {
                                            add_message(nick[0].TrimStart(':'), line, null);
                                        }
                                        else
                                        {
                                            sendData("PRIVMSG", nick[0].TrimStart(':') + " :You need to include more info.");
                                        }
                                        break;
                                    case "owner":
                                        if (line[4].Equals(conf.pass))
                                        {
                                            add_owner(nick[0].TrimStart(':'));
                                            sendData("PRIVMSG", nick[0].TrimStart(':') + " :Identified Successfully!");
                                        }
                                        break;
                                }
                            }
                        }
                        else // Not a command
                        {
                            if (line[2].StartsWith("#") == true) // On a channel
                            {
                                // Messaging Module
                                find_message(nick[0].TrimStart(':'));

                                // AI Module
                                if (nick[0].TrimStart(':') != conf.nick)
                                {
                                    AI(line, line[2], nick[0].TrimStart(':'));
                                }
                            }
                            else // Other
                            {
                            }
                        }
                    }
                }
            }

            // On JOIN events
            if (line[1] == "JOIN")
            {
                string nick = "";
                string channel = "";
                string[] nick_tmp = line[0].TrimStart(':').Split('!');
                nick = nick_tmp[0];
                channel = line[2].TrimStart(':');

                // Intro Message Module
                check_intro(nick, channel);

                // Messaging Module
                find_message(nick);

                // ABan/AKick Module
                string[] nick_host = line[0].Split('@');
                if (nick_host.GetUpperBound(0) > 0)
                {
                    check_auto(nick, channel, nick_host[1]);
                }
            }
            return shouldRun;
        }

        private void list_access_list(string nick, string channel)
        {
            string file_name = "list.txt";

            if (File.Exists(cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[1].Equals(channel))
                            {
                                sendData("NOTICE", nick + " :" + new_line[0] + ": " + new_line[2]);
                            }
                        }
                    }
                    sendData("NOTICE", nick + " :End of Access List");
                }
                else
                {
                    sendData("NOTICE", nick + " :No users in Access List.");
                }
            }
            else
            {
                sendData("NOTICE", nick + " :No users in Access List.");
            }
        }

        private string get_access_list(string nick, string channel)
        {
            string file_name = "list.txt";
            string access = "";

            if (File.Exists(cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    foreach (string line in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = line.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                access = new_line[2];
                                break;
                            }
                        }
                    }
                }
            }
            return access;
        }

        private void set_access_list(string nick, string channel, string access)
        {
            string file_name = "list.txt";
            DateTime current_date = DateTime.Now;

            if (Directory.Exists(cur_dir + "\\modules\\access\\") == false)
            {
                Directory.CreateDirectory(cur_dir + "\\modules\\access");
            }
            if (File.Exists(cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                string[] new_file = new string[number_of_lines];
                int index = 0;
                bool nick_found = false;
                if (number_of_lines > 0)
                {
                    foreach (string lines in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = lines.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                string[] tmp_line = new_line[2].Split(',');
                                bool access_found = false;
                                foreach (string line in tmp_line)
                                {
                                    if(line.Equals(access))
                                    {
                                        access_found = true;
                                    }
                                }
                                if (access_found == false)
                                {
                                    if (new_line[2].Equals(""))
                                    {
                                        new_file[index] = new_line[0] + "*" + new_line[1] + "*" + access;
                                    }
                                    else
                                    {
                                        new_file[index] = new_line[0] + "*" + new_line[1] + "*" + new_line[2] + "," + access;
                                    }
                                }
                                nick_found = true;
                            }
                            else
                            {
                                new_file[index] = lines;
                            }
                            index++;
                        }
                    }
                    if (nick_found == false)
                    {
                        StreamWriter log = File.AppendText(cur_dir + "\\modules\\access\\" + file_name);
                        log.WriteLine(nick + "*" + channel + "*" + access);
                        log.Close();
                    }
                    else
                    {
                        System.IO.File.WriteAllLines(cur_dir + "\\modules\\access\\" + file_name, new_file);
                    }
                }
                else
                {
                    StreamWriter log = File.AppendText(cur_dir + "\\modules\\access\\" + file_name);
                    log.WriteLine(nick + "*" + channel + "*" + access);
                    log.Close();
                }
            }
            else
            {
                StreamWriter log_file = File.CreateText(cur_dir + "\\modules\\access\\" + file_name);
                log_file.WriteLine(nick + "*" + channel + "*" + access);
                log_file.Close();
            }
        }

        private void del_access_list(string nick, string channel, string access)
        {
            string file_name = "list.txt";
            DateTime current_date = DateTime.Now;

            if (Directory.Exists(cur_dir + "\\modules\\access\\") == false)
            {
                Directory.CreateDirectory(cur_dir + "\\modules\\access");
            }
            if (File.Exists(cur_dir + "\\modules\\access\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(cur_dir + "\\modules\\access\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                string[] new_file = new string[number_of_lines];
                int index = 0;
                if (number_of_lines > 0)
                {
                    foreach (string lines in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = lines.Split(sep, 3);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                string[] tmp_line = new_line[2].Split(',');
                                bool access_found = false;
                                string new_access = "";
                                foreach (string line in tmp_line)
                                {
                                    if (line.Equals(access))
                                    {
                                    }
                                    else
                                    {
                                        new_access += "," + line;
                                    }
                                }
                                if (access_found == false)
                                {
                                    if (new_access.TrimStart(',').TrimEnd(',') != "")
                                    {
                                        new_file[index] = new_line[0] + "*" + new_line[1] + "*" + new_access.TrimStart(',').TrimEnd(',');
                                        index++;
                                    }
                                }
                            }
                            else
                            {
                                new_file[index] = lines;
                                index++;
                            }
                        }
                    }
                    System.IO.File.WriteAllLines(cur_dir + "\\modules\\access\\" + file_name, new_file);
                }
            }
        }

        private void get_forecast(string term, string channel)
        {
            XmlDocument doc2 = new XmlDocument();

            // Load data  
            doc2.Load("http://api.wunderground.com/auto/wui/geo/WXCurrentObXML/index.xml?query=" + term);

            // Get forecast with XPath  
            XmlNodeList nodes2 = doc2.SelectNodes("/current_observation");

            string location = "";
            if (nodes2.Count > 0)
            {
                foreach (XmlNode node2 in nodes2)
                {
                    XmlNodeList sub_node2 = doc2.SelectNodes("/current_observation/display_location");
                    foreach (XmlNode xn2 in sub_node2)
                    {
                        location = xn2["full"].InnerText;
                    }
                }
            }

            XmlDocument doc = new XmlDocument();

            // Load data  
            doc.Load("http://api.wunderground.com/auto/wui/geo/ForecastXML/index.xml?query=" + term);

            // Get forecast with XPath  
            XmlNodeList nodes = doc.SelectNodes("/forecast/simpleforecast");

            string weekday = "";
            string highf = "";
            string lowf = "";
            string highc = "";
            string lowc = "";
            string conditions = "";
            if (location != ", " && location != "")
            {
                if (nodes.Count > 0)
                {
                    sendData("PRIVMSG", channel + " :Five day forecast for " + location);
                    foreach (XmlNode node in nodes)
                    {
                        foreach (XmlNode sub_node in node)
                        {
                            weekday = sub_node["date"].SelectSingleNode("weekday").InnerText;
                            highf = sub_node["high"].SelectSingleNode("fahrenheit").InnerText;
                            highc = sub_node["high"].SelectSingleNode("celsius").InnerText;
                            lowf = sub_node["low"].SelectSingleNode("fahrenheit").InnerText;
                            lowc = sub_node["low"].SelectSingleNode("celsius").InnerText;
                            conditions = sub_node["conditions"].InnerText;
                            sendData("PRIVMSG", channel + " :" + weekday + ": " + conditions + " with a high of " + highf + " F (" + highc + " C) and a low of " + lowf + " F (" + lowc + " C).");
                        }
                    }
                }
                else
                {
                    sendData("PRIVMSG", channel + " :No weather information available");
                }
            }
            else
            {
                sendData("PRIVMSG", channel + " :No weather information available");
            }
        }

        private void get_weather(string term, string channel)
        {
            XmlDocument doc = new XmlDocument();

            // Load data  
            doc.Load("http://api.wunderground.com/auto/wui/geo/WXCurrentObXML/index.xml?query=" + term);

            // Get forecast with XPath  
            XmlNodeList nodes = doc.SelectNodes("/current_observation");

            string location = "";
            string temp = "";
            string weather = "";
            string humidity = "";
            string wind = "";
            string wind_dir = "";
            string wind_mph = "";
            if (nodes.Count > 0)
            {
                foreach (XmlNode node in nodes)
                {
                    XmlNodeList sub_node = doc.SelectNodes("/current_observation/display_location");
                    foreach (XmlNode xn in sub_node)
                    {
                        location = xn["full"].InnerText;
                    }
                    temp = node["temperature_string"].InnerText;
                    weather = node["weather"].InnerText;
                    humidity = node["relative_humidity"].InnerText;
                    wind = node["wind_string"].InnerText;
                    wind_dir = node["wind_dir"].InnerText;
                    wind_mph = node["wind_mph"].InnerText;
                }
                if (location != ", ")
                {
                    sendData("PRIVMSG", channel + " :" + location + " is currently " + weather + " with a temperature of " + temp + ".  The humidity is " + humidity + " with winds blowing " + wind_dir + " at " + wind_mph + " mph.");
                }
                else
                {
                    sendData("PRIVMSG", channel + " :No weather information available");
                }
            }
            else
            {
                sendData("PRIVMSG", channel + " :No weather information available");
            }
        }

        private void seen(string nick, string channel)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = "#" + tab_name + ".log";
            bool nick_found = false;
            bool nick_here = false;

            sendData("NAMES", channel);

            string name_line = sr.ReadLine();
            char[] charSeparator = new char[] { ' ' };
            string[] name_lines = name_line.Split(charSeparator, 5);
            string[] names_list = name_lines[4].Split(':');
            string[] names = names_list[1].Split(' ');
            for (int x = 0; x <= names.GetUpperBound(0); x++)
            {
                string new_name = names[x].TrimStart('~').TrimStart('&').TrimStart('@').TrimStart('%').TrimStart('+');
                if (nick == new_name)
                {
                    nick_here = true;
                }
            }
            while (name_lines[4] != ":End of /NAMES list.")
            {
                name_line = sr.ReadLine();
                name_lines = name_line.Split(charSeparator, 5);
            }
            if (nick_here == true)
            {
                sendData("PRIVMSG", channel + " :" + nick + " is right here!");
            }
            else
            {
                if (File.Exists(cur_dir + "\\modules\\seen\\" + file_name))
                {
                    string[] log_file = System.IO.File.ReadAllLines(cur_dir + "\\modules\\seen\\" + file_name);
                    int number_of_lines = log_file.GetUpperBound(0) + 1;
                    if (number_of_lines > 0)
                    {
                        foreach (string line in log_file)
                        {
                            char[] sep = new char[] { '*' };
                            string[] new_line = line.Split(sep, 4);
                            if (new_line.GetUpperBound(0) > 0)
                            {
                                if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                                {
                                    DateTime current_date = DateTime.Now;
                                    DateTime past_date = DateTime.Parse(new_line[2]);
                                    string difference_second = "00";
                                    string difference_minute = "00";
                                    string difference_hour = "00";
                                    string difference_day = "00";
                                    difference_second = current_date.Subtract(past_date).ToString("ss");
                                    difference_minute = current_date.Subtract(past_date).ToString("mm");
                                    difference_hour = current_date.Subtract(past_date).ToString("hh");
                                    difference_day = current_date.Subtract(past_date).ToString("dd");
                                    string difference = "";
                                    if (difference_day != "00")
                                    {
                                        difference += " " + difference_day + " days,";
                                    }
                                    if (difference_hour != "00")
                                    {
                                        difference += " " + difference_hour + " hours,";
                                    }
                                    if (difference_minute != "00")
                                    {
                                        difference += " " + difference_minute + " minutes,";
                                    }
                                    if (difference_second != "00")
                                    {
                                        difference += " " + difference_second + " seconds,";
                                    }
                                    sendData("PRIVMSG", channel + " :I last saw " + nick + " " + difference.TrimEnd(',') + " ago " + new_line[3]);
                                    nick_found = true;
                                    break;
                                }
                            }
                        }
                        if (nick_found == false)
                        {
                            sendData("PRIVMSG", channel + " :I have not seen " + nick);
                        }
                    }
                    else
                    {
                        sendData("PRIVMSG", channel + " :I have not seen " + nick);
                    }
                }
                else
                {
                    sendData("PRIVMSG", channel + " :I have not seen " + nick);
                }
            }
        }

        private void add_seen(string nick, string channel, string[] line)
        {
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = "#" + tab_name + ".log";
            DateTime current_date = DateTime.Now;
            string msg = "";
            line[1] = line[1].ToLower();
            if (line[1].Equals("quit"))
            {
                msg = "Quitting";
            }
            else if (line[1].Equals("join"))
            {
                msg = "Joining " + channel;
            }
            else if (line[1].Equals("part"))
            {
                msg = "Leaving " + channel;
            }
            else if (line[1].Equals("kick"))
            {
                msg = "getting kicked from " + channel;
            }
            else if (line[1].Equals("mode"))
            {
                msg = "setting mode " + " in " + channel;
            }
            else if (line[1].Equals("privmsg"))
            {
                if (line.GetUpperBound(0) > 3)
                {
                    msg = "Saying: " + line[3].TrimStart(':') + " " + line[4];
                }
                else
                {
                    msg = "Saying: " + line[3].TrimStart(':');
                }
            }
            else
            {
                msg = "";
            }
            if (Directory.Exists(cur_dir + "\\modules\\seen\\") == false)
            {
                Directory.CreateDirectory(cur_dir + "\\modules\\seen");
            }
            if (File.Exists(cur_dir + "\\modules\\seen\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(cur_dir + "\\modules\\seen\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                string[] new_file = new string[number_of_lines];
                int index = 0;
                bool nick_found = false;
                if (number_of_lines > 0)
                {
                    foreach (string lines in log_file)
                    {
                        char[] sep = new char[] { '*' };
                        string[] new_line = lines.Split(sep, 4);
                        if (new_line.GetUpperBound(0) > 0)
                        {
                            if (new_line[0].Equals(nick) && new_line[1].Equals(channel))
                            {
                                new_file[index] = new_line[0] + "*" + new_line[1] + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg;
                                nick_found = true;
                            }
                            else
                            {
                                new_file[index] = lines;
                            }
                            index++;
                        }
                    }
                    if (nick_found == false)
                    {
                        StreamWriter log = File.AppendText(cur_dir + "\\modules\\seen\\" + file_name);
                        log.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                        log.Close();
                    }
                    else
                    {
                        System.IO.File.WriteAllLines(cur_dir + "\\modules\\seen\\" + file_name, new_file);
                    }
                }
                else
                {
                    StreamWriter log = File.AppendText(cur_dir + "\\modules\\seen\\" + file_name);
                    log.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                    log.Close();
                }
            }
            else
            {
                StreamWriter log_file = File.CreateText(cur_dir + "\\modules\\seen\\" + file_name);
                log_file.WriteLine(nick + "*" + channel + "*" + current_date.ToString("yyyy-MM-dd HH:mm:ss") + "*" + msg);
                log_file.Close();
            }
        }

        private void get_quote(string channel)
        {
            string[] server = conf.server.Split('.');
            string tab_name = channel.TrimStart('#');
            string pattern = "[^a-zA-Z0-9]"; //regex pattern
            tab_name = Regex.Replace(tab_name, pattern, "_");
            string file_name = server[1] + "-#" + tab_name + ".log";
            if (File.Exists(cur_dir + "\\modules\\quotes\\logs\\" + file_name))
            {
                string[] log_file = System.IO.File.ReadAllLines(cur_dir + "\\modules\\quotes\\logs\\" + file_name);
                int number_of_lines = log_file.GetUpperBound(0) + 1;
                if (number_of_lines > 0)
                {
                    string line = "";
                    while (line == "")
                    {
                        Random random = new Random();
                        int index = random.Next(0, number_of_lines);
                        line = log_file[index];
                    }
                    sendData("PRIVMSG", channel + " :" + line);
                }
                else
                {
                    sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
                }
            }
            else
            {
                sendData("PRIVMSG", channel + " :There are currently no logs for " + channel);
            }
        }

        private void check_auto(string nick, string channel, string hostname)
        {
            string list_file = cur_dir + "\\modules\\auto_kb\\list.txt";
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 1];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] auto_nick = file_line.Split(charSeparator, 6);
                    if (auto_nick.GetUpperBound(0) > 0)
                    {
                        if ((nick.Equals(auto_nick[0]) == true || hostname.Equals(auto_nick[1])) && channel.Equals(auto_nick[2]))
                        {
                            string ban = "*!*@" + hostname;
                            if (hostname.Equals("was"))
                            {
                                ban = nick + "!*@*";
                            }
                            if(auto_nick[4] == "")
                            {
                                auto_nick[4] = "Auto " + auto_nick[3];
                            }
                            if (auto_nick[3].Equals("k"))
                            {
                                sendData("KICK", channel + " " + nick + " :" + auto_nick[4]);
                            }
                            else if (auto_nick[3].Equals("b"))
                            {
                                sendData("MODE", channel + " +b " + ban + " :" + auto_nick[4]);
                            }
                            else if (auto_nick[3].Equals("kb"))
                            {
                                sendData("MODE", channel + " +b " + ban + " :" + auto_nick[4]);
                                sendData("KICK", channel + " " + nick + " :" + auto_nick[4]);
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                            new_file[counter] = file_line;
                            counter++;
                        }
                    }
                }
            }
        }

        private void add_auto(string nick, string channel, string hostname, string type, string reason)
        {
            string list_file = cur_dir + "\\modules\\auto_kb\\list.txt";
            string add_line = nick + "*" + hostname + "*" + channel + "*" + type + "*" + reason + "*" + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt");
            bool found_nick = false;
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 2];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] auto_nick = file_line.Split(charSeparator, 5);
                    if (nick.Equals(auto_nick[0]) && hostname.Equals(auto_nick[1]) && channel.Equals(auto_nick[2]) && type.Equals(auto_nick[3]))
                    {
                        new_file[counter] = add_line;
                        found_nick = true;
                    }
                    else
                    {
                        new_file[counter] = file_line;
                    }
                    counter++;
                }
                if (found_nick == false)
                {
                    new_file[counter] = add_line;
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
                string ban = "*!*@" + hostname;
                if (hostname.Equals("was"))
                {
                    ban = nick + "!*@*";
                }
                if (type.Equals("k"))
                {
                    sendData("KICK", channel + " " + nick + " :" + reason);
                }
                else if (type.Equals("b"))
                {
                    sendData("MODE", channel + " +b " + ban + " :" + reason);
                }
                else if (type.Equals("kb"))
                {
                    sendData("MODE", channel + " +b " + ban + " :" + reason);
                    sendData("KICK", channel + " " + nick + " :" + reason);
                }
                else
                {
                }
            }
            else
            {
                System.IO.File.WriteAllText(@list_file, add_line);
            }
            sendData("PRIVMSG", channel + " :" + nick + " has been added to the a" + type + " list.");
        }

        private void del_auto(string nick, string channel, string hostname, string type)
        {
            string list_file = cur_dir + "\\modules\\auto_kb\\list.txt";
            bool found_nick = false;
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 2];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] auto_nick = file_line.Split(charSeparator, 5);
                    if (nick.Equals(auto_nick[0]) && hostname.Equals(auto_nick[1]) && channel.Equals(auto_nick[2]) && type.Equals(auto_nick[3]))
                    {
                        found_nick = true;
                    }
                    else
                    {
                        new_file[counter] = file_line;
                    }
                    counter++;
                }
                if (found_nick == false)
                {
                    sendData("PRIVMSG", channel + " :" + nick + " is not in the a" + type + " list.");
                }
                else
                {
                    System.IO.File.WriteAllLines(@list_file, new_file);
                    string ban = "*!*@" + hostname;
                    if (hostname.Equals("was"))
                    {
                        ban = nick + "!*@*";
                    }
                    if (type.Equals("b"))
                    {
                        sendData("MODE", channel + " -b " + ban);
                    }
                    else if (type.Equals("kb"))
                    {
                        sendData("MODE", channel + " -b " + ban);
                    }
                    else
                    {
                    }
                }
            }
            else
            {
                sendData("PRIVMSG", channel + " :" + nick + " is not in the a" + type + " list.");
            }
            if (found_nick == true)
            {
                sendData("PRIVMSG", channel + " :" + nick + " has been removed from the a" + type + " list.");
            }
        }

        private void help(string[] line, string nick, string channel, int access)
        {
            string search_term = "";
            string list_file = cur_dir + "\\config\\help.txt";
            if (File.Exists(list_file))
            {
                string msg = "";
                string[] file = System.IO.File.ReadAllLines(list_file);
                int previous_access = 0;
                foreach (string file_line in file)
                {
                    string[] split = file_line.Split(':');
                    if (access >= Convert.ToInt32(split[1]))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            search_term = line[4];
                            string[] new_line = split[2].Split(' ');
                            if (search_term.Contains(new_line[0]))
                            {
                                sendData("NOTICE", nick + " :" + split[0] + " | Usage: " + conf.command + split[2] + " | Description: " + split[3]);
                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(split[1]) != previous_access)
                            {
                                previous_access = Convert.ToInt32(split[1]);
                                sendData("NOTICE", nick + " :" + msg.TrimEnd(','));
                                msg = "";
                                msg += " " + conf.command + split[2] + ",";
                            }
                            else
                            {
                                msg += " " + conf.command + split[2] + ",";
                            }
                        }
                    }
                }
                if (msg != "")
                {
                    sendData("NOTICE", nick + " :" + msg.TrimEnd(','));
                    msg = "";
                }
                if (line.GetUpperBound(0) <= 3)
                {
                    sendData("NOTICE", nick + " :For more information about a specific command, type .help <command name>");
                }
            }
        }

        private void AI(string[] total_line, string channel, string nick)
        {
            try
            {
                string line = total_line[3];
                if (total_line.GetUpperBound(0) > 3)
                {
                    line += " " + total_line[4];
                }
                line = line.Remove(0, 1);
                string new_line = line.ToLowerInvariant();
                bool triggered = false;
                if (file.GetUpperBound(0) >= 0)
                {
                    foreach (string tmp_line in file)
                    {
                        string file_line = tmp_line.Replace("<nick>", nick);
                        file_line = file_line.Replace("<me>", conf.nick);
                        char[] split_type = new char[] { ':' };
                        char[] trigger_split = new char[] { '*' };
                        char[] triggered_split = new char[] { '&' };
                        string[] split = file_line.Split(split_type, 2);
                        string[] triggers = split[0].Split('|');
                        string[] responses = split[1].Split('|');
                        int index = 0;
                        for (int x = 0; x <= triggers.GetUpperBound(0); x++)
                        {
                            string[] terms = triggers[x].Split(trigger_split, StringSplitOptions.RemoveEmptyEntries);
                            for (int y = 0; y <= terms.GetUpperBound(0); y++)
                            {
                                triggered = false;
                                terms[y] = terms[y].ToLowerInvariant();
                                if (triggers[x].StartsWith("*") == false && triggers[x].EndsWith("*") == false && terms.GetUpperBound(0) == 0)
                                {
                                    if (new_line.Equals(terms[y]) == true)
                                    {
                                        triggered = true;
                                    }
                                    else
                                    {
                                        triggered = false;
                                        break;
                                    }
                                }
                                else if (triggers[x].StartsWith("*") == false && y == 0)
                                {
                                    if (new_line.StartsWith(terms[y]) == true && index <= new_line.IndexOf(terms[y]))
                                    {
                                        triggered = true;
                                        index = new_line.IndexOf(terms[y]);
                                    }
                                    else
                                    {
                                        triggered = false;
                                        break;
                                    }
                                }
                                else if (triggers[x].EndsWith("*") == false && y == terms.GetUpperBound(0))
                                {
                                    if (new_line.EndsWith(terms[y]) == true && index <= new_line.IndexOf(terms[y]))
                                    {
                                        triggered = true;
                                        index = new_line.IndexOf(terms[y]);
                                    }
                                    else
                                    {
                                        triggered = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (new_line.Contains(terms[y]) == true && index <= new_line.IndexOf(terms[y]))
                                    {
                                        triggered = true;
                                        index = new_line.IndexOf(terms[y]);
                                    }
                                    else
                                    {
                                        triggered = false;
                                        break;
                                    }
                                }
                            }
                            if (triggered == true)
                            {
                                break;
                            }
                        }
                        if (triggered == true)
                        {
                            spam_count++;
                            int number_of_responses = responses.GetUpperBound(0) + 1;
                            Random random = new Random();
                            index = random.Next(0, number_of_responses);
                            string[] events = responses[index].Split(triggered_split, StringSplitOptions.RemoveEmptyEntries);
                            for (int y = 0; y <= events.GetUpperBound(0); y++)
                            {
                                if (events[y].StartsWith("<action>") == true)
                                {
                                    sendData("PRIVMSG", channel + " :\u0001ACTION " + events[y].Remove(0, 8) + "\u0001");
                                }
                                else if (events[y].StartsWith("<delay>") == true)
                                {
                                    Thread.Sleep(Convert.ToInt32(events[y].Remove(0, 7)));
                                }
                                else if (events[y].StartsWith("<part>") == true)
                                {
                                    sendData("PART", channel);
                                }
                                else if (events[y].StartsWith("<join>") == true)
                                {
                                    sendData("JOIN", channel);
                                }
                                else if (events[y].StartsWith("<kick>") == true)
                                {
                                    if (events[y].Length > 6)
                                    {
                                        sendData("KICK", channel + " " + nick + " :" + events[y].Remove(0, 6));
                                    }
                                    else
                                    {
                                        sendData("KICK", channel + " " + nick + " :No Reason");
                                    }
                                }
                                else
                                {
                                    sendData("PRIVMSG", channel + " :" + events[y]);
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void add_owner(string nick)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(cur_dir + "\\config\\config.xml");
                XmlNode list = xmlDoc.SelectSingleNode("connection_settings");
                foreach (XmlNode node in list)
                {
                    if (node.Name == "owner")
                    {
                        string new_owner = node.InnerText + "," + nick;
                        node.InnerText = new_owner;
                    }
                }
                xmlDoc.Save(cur_dir + "\\config\\config.xml");
                conf.owner += "," + nick;
            }
        }

        private void del_owner(string nick)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string new_owner = "";
            if (File.Exists(cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(cur_dir + "\\config\\config.xml");
                XmlNode list = xmlDoc.SelectSingleNode("connection_settings");
                foreach (XmlNode node in list)
                {
                    if (node.Name == "owner")
                    {
                        string[] new_owner_tmp = node.InnerText.Split(',');
                        for (int x = 0; x <= new_owner_tmp.GetUpperBound(0); x++)
                        {
                            if (new_owner_tmp[x].Equals(nick))
                            {
                            }
                            else
                            {
                                new_owner += new_owner_tmp[x] + ",";
                            }
                        }
                        node.InnerText = new_owner.TrimEnd(',');
                    }
                }
                xmlDoc.Save(cur_dir + "\\config\\config.xml");
                conf.owner = new_owner.TrimEnd(',');
            }
        }

        private void add_message(string nick, string[] line, string channel)
        {
            string list_file = cur_dir + "\\modules\\messaging\\messages.txt";
            char[] charS = new char[] { ' ' };
            string[] tmp = line[4].Split(charS, 2);
            string to_nick = tmp[0];
            string add_line = nick + "*" + to_nick + "*" + DateTime.Now.ToString("MMMM d, yyyy h:mm:ss tt") + "*";
            bool found_nick = false;
            if (tmp.GetUpperBound(0) >= 1)
            {
                add_line += tmp[1];
                if (File.Exists(list_file))
                {
                    int counter = 0;
                    string[] old_file = System.IO.File.ReadAllLines(list_file);
                    string[] new_file = new string[old_file.GetUpperBound(0) + 2];
                    foreach (string file_line in old_file)
                    {
                        char[] charSeparator = new char[] { '*' };
                        string[] intro_nick = file_line.Split(charSeparator, 4);
                        if (nick.Equals(intro_nick[0]))
                        {
                            new_file[counter] = add_line;
                            found_nick = true;
                        }
                        else
                        {
                            new_file[counter] = file_line;
                        }
                        counter++;
                    }
                    if (found_nick == false)
                    {
                        new_file[counter] = add_line;
                    }
                    System.IO.File.WriteAllLines(@list_file, new_file);
                }
                else
                {
                    System.IO.File.WriteAllText(@list_file, add_line);
                }
                if (channel != null)
                {
                    sendData("PRIVMSG", channel + " :" + nick + ", I will send your message as soon as I can.");
                }
                else
                {
                    sendData("PRIVMSG", nick + " :I will send your message as soon as I can.");
                }
            }
        }

        private void find_message(string nick)
        {
            string list_file = cur_dir + "\\modules\\messaging\\messages.txt";
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 1];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { '*' };
                    string[] intro_nick = file_line.Split(charSeparator, 4);
                    if (intro_nick.GetUpperBound(0) > 0)
                    {
                        if (nick.Equals(intro_nick[1]))
                        {
                            sendData("PRIVMSG", nick + " :" + intro_nick[0] + " has left you a message on: " + intro_nick[2]);
                            sendData("PRIVMSG", nick + " :\"" + intro_nick[3] + "\"");
                            sendData("PRIVMSG", nick + " :If you would like to reply to him, please type .message " + nick + " <your_message>");
                        }
                        else
                        {
                            new_file[counter] = file_line;
                            counter++;
                        }
                    }
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
                // Read the file and display it line by line.
            }
        }

        private void check_intro(string nick, string channel)
        {
            string list_file = cur_dir + "\\modules\\intro\\list.txt";
            if (File.Exists(list_file))
            {
                string line;

                // Read the file and display it line by line.
                System.IO.StreamReader file = new System.IO.StreamReader(list_file);
                while ((line = file.ReadLine()) != null)
                {
                    char[] charSeparator = new char[] { ':' };
                    string[] intro_nick = line.Split(charSeparator, 3);
                    if (nick.Equals(intro_nick[0]) && channel.Equals(intro_nick[1]))
                    {
                        string[] intro_line = intro_nick[2].Split('|');
                        int number_of_responses = intro_line.GetUpperBound(0) + 1;
                        Random random = new Random();
                        int index = random.Next(0, number_of_responses);
                        sendData("PRIVMSG", channel + " : " + intro_line[index]);
                    }
                }
                file.Close();
            }
        }

        private void add_intro(string nick, string channel, string[] line)
        {
            string list_file = cur_dir + "\\modules\\intro\\list.txt";
            string add_line = nick + ":" + channel + ":";
            bool found_nick = false;
            if (line.GetUpperBound(0) > 3)
            {
                for (int x = 4; x <= line.GetUpperBound(0); x++)
                {
                    add_line += line[x] + " ";
                }
                if (File.Exists(list_file))
                {
                    int counter = 0;
                    string[] old_file = System.IO.File.ReadAllLines(list_file);
                    string[] new_file = new string[old_file.GetUpperBound(0) + 2];
                    foreach (string file_line in old_file)
                    {
                        char[] charSeparator = new char[] { ':' };
                        string[] intro_nick = file_line.Split(charSeparator, 3);
                        if (nick.Equals(intro_nick[0]) && channel.Equals(intro_nick[1]))
                        {
                            new_file[counter] = add_line;
                            found_nick = true;
                        }
                        else
                        {
                            new_file[counter] = file_line;
                        }
                        counter++;
                    }
                    if (found_nick == false)
                    {
                        new_file[counter] = add_line;
                    }
                    System.IO.File.WriteAllLines(@list_file, new_file);
                }
                else
                {
                    System.IO.File.WriteAllText(@list_file, add_line);
                }
                sendData("PRIVMSG", channel + " :Your introduction will be proclaimed as you wish.");
            }
        }

        private void delete_intro(string nick, string channel)
        {
            string list_file = cur_dir + "\\modules\\intro\\list.txt";
            if (File.Exists(list_file))
            {
                int counter = 0;
                string[] old_file = System.IO.File.ReadAllLines(list_file);
                string[] new_file = new string[old_file.GetUpperBound(0) + 1];
                foreach (string file_line in old_file)
                {
                    char[] charSeparator = new char[] { ':' };
                    string[] intro_nick = file_line.Split(charSeparator, 3);
                    if (nick.Equals(intro_nick[0]) && channel.Equals(intro_nick[1]))
                    {
                    }
                    else
                    {
                        new_file[counter] = file_line;
                        counter++;
                    }
                }
                System.IO.File.WriteAllLines(@list_file, new_file);
            }
            sendData("PRIVMSG", channel + " :Your introduction has been removed.");
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
                    string channel = "System";
                    string tab_name = "System";
                    string message = "";
                    string nickname = "";
                    string pattern = "[^a-zA-Z0-9]"; //regex pattern
                    Control control = tabControl1.Controls.Find("output_box_system", true)[0];
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
                                if (!nickname.Equals(conf.nick) && !tmp_lines[3].Remove(0, 1).StartsWith("."))
                                {
                                    string new_tab_name = Regex.Replace(tab_name, pattern, "_");
                                    string[] server = conf.server.Split('.');
                                    string file_name = server[1] + "-#" + new_tab_name + ".log";
                                    if (Directory.Exists(cur_dir + "\\modules\\quotes\\logs") == false)
                                    {
                                        Directory.CreateDirectory(cur_dir + "\\modules\\quotes\\logs");
                                    }
                                    StreamWriter log_file = File.AppendText(cur_dir + "\\modules\\quotes\\logs\\" + file_name);
                                    log_file.WriteLine(tmp_lines[3].Remove(0, 1) + " [" + nickname + "]");
                                    log_file.Close();
                                }
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
                            // Last Seen Module
                            add_seen(tmp_lines[0].TrimStart(':').Split('!')[0], channel, tmp_lines);
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
                            // Last Seen Module
                            add_seen(tmp_lines[0].TrimStart(':').Split('!')[0], channel, tmp_lines);
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
                            // Last Seen Module
                            add_seen(tmp_lines[0].TrimStart(':').Split('!')[0], channel, tmp_lines);
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
                            message = nickname + " has set Mode " + tmp_lines[3];
                            nickname = "";
                            // Last Seen Module
                            add_seen(tmp_lines[0].TrimStart(':').Split('!')[0], channel, tmp_lines);
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
                            // Last Seen Module
                            string[] new_nick = tmp_lines[3].Split(' ');
                            add_seen(new_nick[0], channel, tmp_lines);
                            font_color = "#C73232";
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
                        tab_name = Regex.Replace(tab_name, pattern, "_");
                        string[] nick = tmp_lines[0].Split('!');
                        if (tabControl1.Controls.Find("output_box_" + tab_name, true).GetUpperBound(0) >= 0)
                        {
                            control = tabControl1.Controls.Find("output_box_" + tab_name, true)[0];
                        }
                        else
                        {
                            add_tab(channel);
                            control = tabControl1.Controls.Find("output_box_" + tab_name, true)[0];
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
                        string[] server = conf.server.Split('.');
                        string file_name = server[1] + "-#" + tab_name + ".log";
                        if (conf.logs_path == "")
                        {
                            conf.logs_path = cur_dir + "\\logs";
                        }
                        if (Directory.Exists(conf.logs_path))
                        {
                            StreamWriter log_file = File.AppendText(conf.logs_path + "\\" + file_name);
                            log_file.WriteLine(text);
                            log_file.Close();
                        }
                        else
                        {
                            Directory.CreateDirectory(conf.logs_path);
                            StreamWriter log_file = File.AppendText(conf.logs_path + "\\" + file_name);
                            log_file.WriteLine(text);
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
                string pattern = "[^a-zA-Z0-9]"; //regex pattern
                string tab_name = channel.TrimStart('#');
                tab_name = Regex.Replace(tab_name, pattern, "_");
                RichTextBox box = new RichTextBox();
                box.Dock = System.Windows.Forms.DockStyle.Fill;
                box.Location = new System.Drawing.Point(3, 3);
                box.Name = "output_box_" + tab_name;
                box.Size = new System.Drawing.Size(826, 347);
                box.TabIndex = 0;
                box.ReadOnly = true;
                box.Text = "";
                TabPage tabpage = new TabPage();
                tabpage.Controls.Add(box);
                tabpage.Location = new System.Drawing.Point(4, 22);
                tabpage.Name = "tabPage_" + tab_name;
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
            XmlNode list = xmlDoc.SelectSingleNode("connection_settings");

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

namespace search.api
{
    public struct SearchType
    {
        public string url;
        public string title;
        public string content;
        public FindingEngine engine;
        public enum FindingEngine { Google };
    }

    public interface ISearchResult
    {
        SearchType.FindingEngine Engine { get; set; }
        string SearchExpression { get; set; }
        List<SearchType> Search();
    }

    public class GoogleSearch : ISearchResult
    {
        public GoogleSearch(string searchExpression)
        {
            this.Engine = SearchType.FindingEngine.Google;
            this.SearchExpression = searchExpression;
        }
        public SearchType.FindingEngine Engine { get; set; }
        public string SearchExpression { get; set; }

        public List<SearchType> Search()
        {
            const string urlTemplate = @"http://ajax.googleapis.com/ajax/services/search/web?v=1.0&rsz=large&safe=active&q={0}&start={1}";
            var resultsList = new List<SearchType>();
            int[] offsets = { 0, 8, 16, 24, 32, 40, 48 };
            foreach (var offset in offsets)
            {
                var searchUrl = new Uri(string.Format(urlTemplate, SearchExpression, offset));
                var page = new WebClient().DownloadString(searchUrl);
                var o = (JObject)JsonConvert.DeserializeObject(page);

                var resultsQuery =
                  from result in o["responseData"]["results"].Children()
                  select new SearchType
                  {
                      url = result.Value<string>("url").ToString(),
                      title = result.Value<string>("title").ToString(),
                      content = result.Value<string>("content").ToString(),
                      engine = this.Engine
                  };

                resultsList.AddRange(resultsQuery);
            }
            return resultsList;
        }
    }
}
