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
    public IPAddress[] server_ip;
    public string chans;
    public string chan_blacklist;
    public int port;
    public string nick;
    public string secondary_nicks;
    public string pass;
    public string email;
    public string name;
    public string owner;
    public string command;
    public string keep_logs;
    public string logs_path;
    public bool minimize_to_tray;
    public int spam_count_max;
    public int spam_threshold;
    public int spam_timout;
    public int max_message_length;
    public int user_level;
    public int voice_level;
    public int hop_level;
    public int op_level;
    public int sop_level;
    public int founder_level;
    public int owner_level;
    public List<List<string>> module_config;
    public List<List<string>> command_list;
}

namespace IRCBot
{
    public partial class Interface : Form
    {

        private string output = "";
        public string full_server_list = "";
        public string cur_dir = "";
        public List<string> queue_text = new List<string>();
        private System.Windows.Forms.Timer updateOutput = new System.Windows.Forms.Timer();
        private List<bot> bot_instances;

        public readonly object listLock = new object();

        private IRCConfig conf = new IRCConfig();
        private System.Windows.Forms.NotifyIcon MyNotifyIcon;
        delegate void SetTextCallback(string text);

        ContextMenu TrayMenu = new ContextMenu();
        ContextMenu TabMenu = new ContextMenu();

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
            conf.command_list = new List<List<string>>();
            
            cur_dir = Directory.GetCurrentDirectory();
            
            button1.Enabled = false;
            button1.Visible = false;

            updateOutput.Tick += UpdateOutput;
            Control control = new Control();
            control = tabControl1.Controls.Find("output_box_system", true)[0];
            RichTextBox output_box = (RichTextBox)control;
            output_box.LinkClicked += link_Click;

            MyNotifyIcon = new System.Windows.Forms.NotifyIcon();
            MyNotifyIcon.Visible = false;
            MyNotifyIcon.Icon = new System.Drawing.Icon(cur_dir + Path.DirectorySeparatorChar + "Bot.ico");
            MyNotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(MyNotifyIcon_MouseDoubleClick);
            MyNotifyIcon.ContextMenu = TrayMenu;

            MenuItem config_item = new MenuItem();
            config_item.Text = "Configuration";
            config_item.Click += new EventHandler(configurationToolStripMenuItem_Click);
            TrayMenu.MenuItems.Add(config_item);

            MenuItem about_item = new MenuItem();
            about_item.Text = "About";
            about_item.Click += new EventHandler(aboutToolStripMenuItem_Click);
            TrayMenu.MenuItems.Add(about_item);

            MenuItem exit_item = new MenuItem();
            exit_item.Text = "Exit";
            exit_item.Click += new EventHandler(exitToolStripMenuItem_Click);
            TrayMenu.MenuItems.Add(exit_item);

            this.Icon = new System.Drawing.Icon(cur_dir + Path.DirectorySeparatorChar + "Bot.ico");
            tabControl1.SelectedIndexChanged += tab_changed;
            connect();
        }

        private void MyNotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MyNotifyIcon.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        private void Interface_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && conf.minimize_to_tray == true)
            {
                ShowInTaskbar = false;
                this.WindowState = FormWindowState.Minimized;
                MyNotifyIcon.BalloonTipTitle = this.Text;
                MyNotifyIcon.BalloonTipText = "Working in the Background";
                MyNotifyIcon.ShowBalloonTip(500);
            }
            else if (WindowState == FormWindowState.Normal)
            {
                ShowInTaskbar = true;
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void connect()
        {
            cur_dir = Directory.GetCurrentDirectory();

            updateOutput.Interval = 100;
            updateOutput.Start();
            
            queue_text.Capacity = 1000;
            queue_text.Clear();

            // Load Bot Configuration
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            }
            else
            {
                XmlNode node = xmlDoc.CreateNode(XmlNodeType.Element, "global_settings", null);
                XmlNode nodeCommand = xmlDoc.CreateElement("command_prefix");
                nodeCommand.InnerText = ".";
                node.AppendChild(nodeCommand);
                XmlNode nodeKeep = xmlDoc.CreateElement("keep_logs");
                nodeKeep.InnerText = "True";
                node.AppendChild(nodeKeep);
                XmlNode nodeLogs = xmlDoc.CreateElement("logs_path");
                nodeLogs.InnerText = cur_dir + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + "";
                node.AppendChild(nodeLogs);
                XmlNode nodeStart = xmlDoc.CreateElement("start_with_windows");
                nodeStart.InnerText = "False";
                node.AppendChild(nodeStart);
                XmlNode nodeTray = xmlDoc.CreateElement("minimize_to_tray");
                nodeTray.InnerText = "False";
                node.AppendChild(nodeTray);
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
                xmlDoc.Save(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            }
            XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/global_settings");

            conf.command = list["command_prefix"].InnerText;
            conf.keep_logs = list["keep_logs"].InnerText;
            if (Directory.Exists(list["logs_path"].InnerText))
            {
                conf.logs_path = list["logs_path"].InnerText;
            }
            else
            {
                conf.logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
            }
            conf.spam_count_max = Convert.ToInt32(list["spam_count"].InnerText);
            conf.spam_threshold = Convert.ToInt32(list["spam_threshold"].InnerText);
            conf.spam_timout = Convert.ToInt32(list["spam_timeout"].InnerText);
            conf.max_message_length = Convert.ToInt32(list["max_message_length"].InnerText);
            conf.minimize_to_tray = Convert.ToBoolean(list["minimize_to_tray"].InnerText);
            if (conf.minimize_to_tray == true)
            {
                MyNotifyIcon.Visible = true;
            }
            else
            {
                MyNotifyIcon.Visible = false;
            }

            conf.module_config.Clear();
            XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
            full_server_list = "";
            foreach (XmlNode xn in xnList)
            {
                full_server_list += xn["server_name"].InnerText + ",";
            }
            full_server_list = full_server_list.TrimEnd(',');

            string[] tmp_server_list = full_server_list.Split(',');
            bot_instances = new List<bot>();
            int index = 0;
            connectToolStripMenuItem.Enabled = false;
            if (full_server_list != "")
            {
                connectToolStripMenuItem.Text = "Disconnect";
                connectToolStripMenuItem.Enabled = true;
                foreach (string server_name in tmp_server_list)
                {
                    XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                    foreach (XmlNode xn in ServerxnList)
                    {
                        string tmp_server = xn["server_name"].InnerText;
                        if (tmp_server.Equals(server_name))
                        {
                            conf.name = xn["name"].InnerText;
                            conf.nick = xn["nick"].InnerText;
                            conf.secondary_nicks = xn["sec_nicks"].InnerText;
                            conf.pass = xn["password"].InnerText;
                            conf.email = xn["email"].InnerText;
                            conf.owner = xn["owner"].InnerText;
                            conf.port = Convert.ToInt32(xn["port"].InnerText);
                            conf.server = xn["server_name"].InnerText;
                            conf.chans = xn["chan_list"].InnerText;
                            conf.chan_blacklist = xn["chan_blacklist"].InnerText;
                            conf.user_level = Convert.ToInt32(xn["user_level"].InnerText);
                            conf.voice_level = Convert.ToInt32(xn["voice_level"].InnerText);
                            conf.hop_level = Convert.ToInt32(xn["hop_level"].InnerText);
                            conf.op_level = Convert.ToInt32(xn["op_level"].InnerText);
                            conf.sop_level = Convert.ToInt32(xn["sop_level"].InnerText);
                            conf.founder_level = Convert.ToInt32(xn["founder_level"].InnerText);
                            conf.owner_level = Convert.ToInt32(xn["owner_level"].InnerText);


                            XmlDocument xmlDocModules = new XmlDocument();
                            if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml"))
                            {
                                xmlDocModules.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml");
                            }
                            else
                            {
                                Directory.CreateDirectory(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"]);
                                File.Copy(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + "Default" + Path.DirectorySeparatorChar + "modules.xml", cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"] + Path.DirectorySeparatorChar + "modules.xml");
                                xmlDocModules.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml");
                            }
                            xnList = xmlDocModules.SelectNodes("/modules/module");
                            foreach (XmlNode xn_module in xnList)
                            {
                                if (xn_module["enabled"].InnerText == "True")
                                {
                                    List<string> tmp_list = new List<string>();
                                    String module_name = xn_module["name"].InnerText;
                                    String class_name = xn_module["class_name"].InnerText;
                                    tmp_list.Add(class_name);
                                    tmp_list.Add(module_name);
                                    tmp_list.Add(xn_module["blacklist"].InnerText);

                                    XmlNodeList optionList = xn_module.ChildNodes;
                                    foreach (XmlNode option in optionList)
                                    {
                                        if (option.Name.Equals("commands"))
                                        {
                                            XmlNodeList Options = option.ChildNodes;
                                            foreach (XmlNode options in Options)
                                            {
                                                List<string> tmp2_list = new List<string>();
                                                tmp2_list.Add(class_name);
                                                tmp2_list.Add(options["name"].InnerText);
                                                tmp2_list.Add(options["description"].InnerText);
                                                tmp2_list.Add(options["triggers"].InnerText);
                                                tmp2_list.Add(options["syntax"].InnerText);
                                                tmp2_list.Add(options["access_level"].InnerText);
                                                tmp2_list.Add(options["blacklist"].InnerText);
                                                tmp2_list.Add(options["show_help"].InnerText);
                                                tmp2_list.Add(options["spam_check"].InnerText);
                                                conf.command_list.Add(tmp2_list);
                                            }
                                        }
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
                            }
                            break;
                        }
                    }
                    Control control = new Control();
                    string[] server = server_name.Split('.');
                    string tmp_server_name = "No_Server_Specified";
                    if (server.GetUpperBound(0) > 0)
                    {
                        tmp_server_name = server[1];
                    }
                    if (tabControl1.Controls.Find("output_box_system", true).GetUpperBound(0) >= 0)
                    {
                        control = tabControl1.Controls.Find("output_box_system", true)[0];
                        RichTextBox output_box = (RichTextBox)control;
                        output_box.Name = "output_box_" + tmp_server_name + ":system";
                        control = tabControl1.Controls.Find("tabPage1", true)[0];
                        TabPage tabpage = (TabPage)control;
                        tabpage.Name = "tabPage:" + tmp_server_name + ":__:system";
                        tabpage.Text = tmp_server_name;
                    }
                    else if (tabControl1.Controls.Find("output_box_" + tmp_server_name + ":system", true).GetUpperBound(0) < 0)
                    {
                        RichTextBox box = new RichTextBox();
                        box.Dock = System.Windows.Forms.DockStyle.Fill;
                        box.Location = new System.Drawing.Point(3, 3);
                        box.Name = "output_box_" + tmp_server_name + ":system";
                        box.Size = new System.Drawing.Size(826, 347);
                        box.ReadOnly = true;
                        box.Text = "";
                        TabPage tabpage = new TabPage();
                        tabpage.Controls.Add(box);
                        tabpage.Location = new System.Drawing.Point(4, 22);
                        tabpage.Name = "tabPage:" + tmp_server_name + ":__:system";
                        tabpage.Padding = new System.Windows.Forms.Padding(3);
                        tabpage.Size = new System.Drawing.Size(832, 353);
                        tabpage.Text = tmp_server_name;
                        tabpage.UseVisualStyleBackColor = true;
                        tabControl1.Controls.Add(tabpage);
                        tabControl1.GetControl(index).TabIndex = index;
                        tabControl1.Update();
                        box.LinkClicked += new LinkClickedEventHandler(link_Click);
                    }
                    else
                    {
                    }

                    try
                    {
                        conf.server_ip = Dns.GetHostAddresses(conf.server);
                    }
                    catch
                    {
                        conf.server_ip = null;
                    }

                    button1.Visible = true;
                    button1.Enabled = true;

                    bot bot_instance = new bot();
                    bot_instances.Add(bot_instance);
                    bot_instances[index].start_bot(this, conf);
                    index++;
                }
            }
            else
            {
                Control control = new Control();
                control = tabControl1.Controls.Find("output_box_system", true)[0];
                RichTextBox output_box = (RichTextBox)control;
                output_box.AppendText("No Servers Specified");
            }
        }

        private void tab_changed(object sender, EventArgs e)
        {
            string[] server_list = full_server_list.Split(',');
            foreach (string server_name in server_list)
            {
                string[] server = server_name.Split('.');
                string tmp_server_name = "No_Server_Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
            }
            int index = 0;
            string tmp_server = "No_Server_Specified";
            foreach (string server_name in server_list)
            {
                string[] server = server_name.Split('.');
                string tmp_server_name = "No_Server_Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
                if (tab_name[1].Equals(tmp_server_name))
                {
                    tmp_server = tmp_server_name;
                    break;
                }
                else
                {
                    index++;
                }
            }
            if (bot_instances[index].connected == true)
            {
                connectToolStripMenuItem.Text = "Disconnect";
                input_box.Enabled = true;
                send_button.Enabled = true;
            }
            else
            {
                connectToolStripMenuItem.Text = "Connect";
                input_box.Enabled = false;
                send_button.Enabled = false;
            }
        }

        protected void link_Click(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void send_button_Click(object sender, EventArgs e)
        {
            string[] server_list = full_server_list.Split(',');
            int index = 0;
            string tmp_server = "No_Server_Specified";
            foreach (string server_name in server_list)
            {
                string[] server = server_name.Split('.');
                string tmp_server_name = "No_Server_Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
                if (tab_name[1].Equals(tmp_server_name))
                {
                    tmp_server = tmp_server_name;
                    break;
                }
                else
                {
                    index++;
                }
            }
            string input_tmp = input_box.Text;
            char[] charSeparator = new char[] { ' ' };
            string[] input = input_tmp.Split(charSeparator, 2);
            if (input[0].StartsWith("/"))
            {
                if (input.GetUpperBound(0) > 0)
                {
                    bot_instances[index].sendData(input[0].TrimStart('/'), input[1]);
                }
                else
                {
                    bot_instances[index].sendData(input[0].TrimStart('/'), null);
                }
            }
            else
            {
                if (tabControl1.SelectedIndex == 0)
                {
                    output = Environment.NewLine + tmp_server + ":" + "No channel joined. Try /join #<channel>";

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
                        bot_instances[index].sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0] + " " + input[1]);
                    }
                    else
                    {
                        bot_instances[index].sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0]);
                    }
                }
            }
            input_box.Text = "";
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
                string channel = "System";
                string tab_name = "System";
                string message = text;
                string nickname = "";
                string pattern = "[^a-zA-Z0-9-_.+#]"; //regex pattern
                if (tabControl1.Controls.Find("output_box_" + tmp_server_name + ":system", true).GetUpperBound(0) >= 0)
                {
                    Control control = tabControl1.Controls.Find("output_box_" + tmp_server_name + ":system", true)[0];
                    char[] charSeparator = new char[] { ' ' };
                    string[] tmp_lines = text.Split(charSeparator, 4);
                    string time_stamp = DateTime.Now.ToString("hh:mm tt");
                    string date_stamp = DateTime.Now.ToString("yyyy-MM-dd");
                    string font_color = "#000000";
                    if (tmp_lines.GetUpperBound(0) > 1)
                    {
                        tmp_lines[1] = tmp_lines[1].ToLower();
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
                            if (channel.Equals(conf.nick))
                            {
                                channel = nickname;
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
                            }
                            else
                            {
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
                                if (nickname.ToLower().Equals(conf.nick.ToLower()))
                                {
                                    channel = "System";
                                }
                            }
                            else
                            {
                                channel = "System";
                            }
                            message = nickname + " has left " + tmp_lines[2];
                            nickname = "";
                            font_color = "#66361F";
                        }
                        else if (tmp_lines[1].Equals("quit"))
                        {
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            channel = "";
                            foreach (bot bot_instance in bot_instances)
                            {
                                int index = 0;
                                foreach (List<string> nicks in bot_instance.nick_list)
                                {
                                    int nick_index = 0;
                                    foreach (string nick in nicks)
                                    {
                                        string[] sep_nick = nick.Split(':');
                                        if (sep_nick.GetUpperBound(0) > 0)
                                        {
                                            if (sep_nick[1].Equals(nickname.ToLower()))
                                            {
                                                channel += "," + bot_instance.channel_list[index];
                                                bot_instance.nick_list[index].RemoveAt(nick_index);
                                                break;
                                            }
                                        }
                                        nick_index++;
                                    }
                                    index++;
                                }
                            }
                            channel = channel.TrimStart(',');
                            if (tmp_lines.GetUpperBound(0) > 2)
                            {
                                message = nickname + " has quit (" + tmp_lines[2].TrimStart(':') + " " + tmp_lines[3] + ")";
                            }
                            else
                            {
                                message = nickname + " has quit (" + tmp_lines[2].TrimStart(':') + ")";
                            }
                            nickname = "";
                            font_color = "#66361F";
                        }
                        else if (tmp_lines[1].Equals("nick"))
                        {
                            nickname = tmp_lines[2].TrimStart(':').ToLower();
                            channel = "";
                            foreach (bot bot_instance in bot_instances)
                            {
                                int index = 0;
                                foreach (List<string> nicks in bot_instance.nick_list)
                                {
                                    foreach (string nick in nicks)
                                    {
                                        string[] sep_nick = nick.Split(':');
                                        if (sep_nick.GetUpperBound(0) > 0)
                                        {
                                            if (sep_nick[1].Equals(nickname.ToLower()))
                                            {
                                                channel += "," + bot_instance.channel_list[index];
                                                break;
                                            }
                                        }
                                    }
                                    index++;
                                }
                            }
                            channel = channel.TrimStart(',');
                            message = tmp_lines[0].TrimStart(':').Split('!')[0] + " is now known as " + nickname;
                            nickname = "";
                            font_color = "#000000";
                        }
                        else if (tmp_lines[1].Equals("mode"))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                            }
                            else if (channel.Equals(conf.nick))
                            {
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
                            }
                            else if (channel.Equals(conf.nick))
                            {
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
                                        message = new_lines[1].TrimStart(':');
                                        nickname = "";
                                        font_color = "#B037B0";
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
                                        font_color = "000000";
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
                                    font_color = "000000";
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
                                font_color = "000000";
                            }
                        }
                    }
                    if (channel.Equals(""))
                    {
                        channel = "System";
                    }
                    string[] channels = channel.Split(',');
                    foreach (string channel_line in channels)
                    {
                        tab_name = Regex.Replace(channel_line, pattern, "_");
                        string[] nick = tmp_lines[0].Split('!');
                        if (channel != "System")
                        {
                            if (channel_line.StartsWith("#"))
                            {
                                if (tabControl1.Controls.Find("output_box_chan_" + tmp_server_name + ":" + channel_line, true).GetUpperBound(0) < 0)
                                {
                                    add_tab(tmp_server_name + ":" + channel_line);
                                }
                                control = tabControl1.Controls.Find("output_box_chan_" + tmp_server_name + ":" + channel_line, true)[0];
                            }
                            else
                            {
                                if (tabControl1.Controls.Find("output_box_user_" + tmp_server_name + ":" + channel_line, true).GetUpperBound(0) < 0)
                                {
                                    add_tab(tmp_server_name + ":" + channel_line);
                                }
                                control = tabControl1.Controls.Find("output_box_user_" + tmp_server_name + ":" + channel_line, true)[0];
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
                            file_name = tmp_server_name + "-" + tab_name + ".log";
                            if (conf.logs_path == "")
                            {
                                conf.logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
                            }
                            if (Directory.Exists(conf.logs_path))
                            {
                                StreamWriter log_file = File.AppendText(conf.logs_path + Path.DirectorySeparatorChar + file_name);
                                log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + text);
                                log_file.Close();
                            }
                            else
                            {
                                Directory.CreateDirectory(conf.logs_path);
                                StreamWriter log_file = File.AppendText(conf.logs_path + Path.DirectorySeparatorChar + file_name);
                                log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + text);
                                log_file.Close();
                            }
                        }
                    }
                }
            }
        }

        private void ThreadProcSafeOutput()
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
                    UpdateOutput_final(lines[x]);
                }
            }

        }

        private void add_tab(string tab_name)
        {
            if (tabControl1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(add_tab);
                this.Invoke(d, new object[] { tab_name });
            }
            else
            {
                char[] charSep = new char[] { ':' };
                string[] channel = tab_name.Split(charSep, 2);
                RichTextBox box = new RichTextBox();
                box.Dock = System.Windows.Forms.DockStyle.Fill;
                box.Location = new System.Drawing.Point(3, 3);
                if (channel[1].StartsWith("#"))
                {
                    box.Name = "output_box_chan_" + tab_name;
                }
                else
                {
                    box.Name = "output_box_user_" + tab_name;
                }
                box.Size = new System.Drawing.Size(826, 347);
                box.ReadOnly = true;
                box.Text = "";
                TabPage tabpage = new TabPage();
                tabpage.Controls.Add(box);
                tabpage.Location = new System.Drawing.Point(4, 22);
                if (channel[1].StartsWith("#"))
                {
                    tabpage.Name = "tabPage:" + channel[0] + ":_chan_:" + channel[1];
                }
                else
                {
                    tabpage.Name = "tabPage:" + channel[0] + ":_user_:" + channel[1];
                }
                tabpage.Padding = new System.Windows.Forms.Padding(3);
                tabpage.Size = new System.Drawing.Size(832, 353);
                tabpage.Text = channel[1];
                tabpage.UseVisualStyleBackColor = true;

                tabControl1.Controls.Add(tabpage);

                tabControl1.Update();

                string[][] tab_names = new string[tabControl1.TabPages.Count][];

                int index = 0;
                foreach (TabPage tab in tabControl1.TabPages)
                {
                    string[] tmp = new string[2] {
                        tab.Name,
                        index.ToString()
                    };
                    tab_names[index] = tmp;
                    index++;
                }

                Sort<string>(tab_names, 0);

                index = 1;
                for (int i = 0; i <= tab_names.GetUpperBound(0); i++)
                {
                    TabPage tmp = new TabPage();
                    tmp = tabControl1.TabPages[Convert.ToInt32(tab_names[i][1])];
                    tabControl1.TabPages.RemoveAt(Convert.ToInt32(tab_names[i][1]));
                    tabControl1.Controls.Add(tmp);
                    for (int x = index; x <= tab_names.GetUpperBound(0); x++)
                    {
                        if (Convert.ToInt32(tab_names[x][1]) > Convert.ToInt32(tab_names[i][1]))
                        {
                            tab_names[x][1] = (Convert.ToInt32(tab_names[x][1]) - 1).ToString();
                        }
                    }
                    index++;
                }

                tabControl1.Update();

                box.LinkClicked += new LinkClickedEventHandler(link_Click);

                tabControl1.SelectedIndex = tabControl1.Controls.Find(tabpage.Name, true)[0].TabIndex;
            }
        }

        private static void Sort<T>(T[][] data, int col)
        {
            Comparer<T> comparer = Comparer<T>.Default;
            Array.Sort<T[]>(data, (x, y) => comparer.Compare(x[col], y[col]));
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            updateOutput.Stop();
            foreach (bot bot_instance in bot_instances)
            {
                bot_instance.worker.CancelAsync();
            }
            this.Close();
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            configuration newForm = new configuration(this);
            newForm.ShowDialog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 about = new AboutBox1();
            about.ShowDialog();
        }

        private void input_box_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                string[] server_list = full_server_list.Split(',');
                int index = 0;
                string tmp_server = "No_Server_Specified";
                foreach (string server_name in server_list)
                {
                    string[] server = server_name.Split('.');
                    string tmp_server_name = "No_Server_Specified";
                    if (server.GetUpperBound(0) > 0)
                    {
                        tmp_server_name = server[1];
                    }
                    char[] charSep = new char[] { ':' };
                    string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
                    if (tab_name[1].Equals(tmp_server_name))
                    {
                        tmp_server = tmp_server_name;
                        break;
                    }
                    else
                    {
                        index++;
                    }
                }
                string input_tmp = input_box.Text;
                char[] charSeparator = new char[] { ' ' };
                string[] input = input_tmp.Split(charSeparator, 2);
                if (input[0].StartsWith("/"))
                {
                    if (input.GetUpperBound(0) > 0)
                    {
                        bot_instances[index].sendData(input[0].TrimStart('/'), input[1]);
                    }
                    else
                    {
                        bot_instances[index].sendData(input[0].TrimStart('/'), null);
                    }
                }
                else
                {
                    if (tabControl1.SelectedIndex == 0)
                    {
                        output = Environment.NewLine + tmp_server + ":" + "No channel joined. Try /join #<channel>";

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
                            bot_instances[index].sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0] + " " + input[1]);
                        }
                        else
                        {
                            bot_instances[index].sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0]);
                        }
                    }
                }
                input_box.Text = "";
            }
        }

        public void update_conf()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            foreach (bot bot_instance in bot_instances)
            {
                XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/global_settings");
                bot_instance.conf.command = list["command_prefix"].InnerText;
                bot_instance.conf.keep_logs = list["keep_logs"].InnerText;
                if (Directory.Exists(list["logs_path"].InnerText))
                {
                    bot_instance.conf.logs_path = list["logs_path"].InnerText;
                }
                else
                {
                    bot_instance.conf.logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
                }
                bot_instance.conf.max_message_length = Convert.ToInt32(list["max_message_length"].InnerText);
                bot_instance.conf.minimize_to_tray = Convert.ToBoolean(list["minimize_to_tray"].InnerText);

                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(bot_instance.conf.server))
                    {
                        bot_instance.conf.name = xn["name"].InnerText;
                        bot_instance.conf.nick = xn["nick"].InnerText;
                        bot_instance.conf.secondary_nicks = xn["sec_nicks"].InnerText;
                        bot_instance.conf.pass = xn["password"].InnerText;
                        bot_instance.conf.email = xn["email"].InnerText;
                        bot_instance.conf.owner = xn["owner"].InnerText;
                        bot_instance.conf.port = Convert.ToInt32(xn["port"].InnerText);
                        bot_instance.conf.server = xn["server_name"].InnerText;
                        bot_instance.conf.chans = xn["chan_list"].InnerText;
                        bot_instance.conf.chan_blacklist = xn["chan_blacklist"].InnerText;

                        bot_instance.conf.command_list.Clear();
                        bot_instance.conf.module_config.Clear();
                        XmlDocument xmlDocModules = new XmlDocument();
                        if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml"))
                        {
                            xmlDocModules.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml");
                        }
                        else
                        {
                            Directory.CreateDirectory(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"]);
                            File.Copy(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + "Default" + Path.DirectorySeparatorChar + "modules.xml", cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"] + Path.DirectorySeparatorChar + "modules.xml");
                            xmlDocModules.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml");
                        }
                        XmlNodeList xnList = xmlDocModules.SelectNodes("/modules/module");
                        foreach (XmlNode xn_module in xnList)
                        {
                            if (xn_module["enabled"].InnerText == "True")
                            {
                                List<string> tmp_list = new List<string>();
                                String module_name = xn_module["name"].InnerText;
                                String class_name = xn_module["class_name"].InnerText;
                                tmp_list.Add(class_name);
                                tmp_list.Add(module_name);
                                tmp_list.Add(xn_module["blacklist"].InnerText);

                                XmlNodeList optionList = xn_module.ChildNodes;
                                foreach (XmlNode option in optionList)
                                {
                                    if (option.Name.Equals("commands"))
                                    {
                                        XmlNodeList Options = option.ChildNodes;
                                        foreach (XmlNode options in Options)
                                        {
                                            List<string> tmp2_list = new List<string>();
                                            tmp2_list.Add(class_name);
                                            tmp2_list.Add(options["name"].InnerText);
                                            tmp2_list.Add(options["description"].InnerText);
                                            tmp2_list.Add(options["triggers"].InnerText);
                                            tmp2_list.Add(options["syntax"].InnerText);
                                            tmp2_list.Add(options["access_level"].InnerText);
                                            tmp2_list.Add(options["blacklist"].InnerText);
                                            tmp2_list.Add(options["show_help"].InnerText);
                                            tmp2_list.Add(options["spam_check"].InnerText);
                                            bot_instance.conf.command_list.Add(tmp2_list);
                                        }
                                    }
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
                                bot_instance.conf.module_config.Add(tmp_list);
                            }
                        }
                    }
                }
            }

            XmlNodeList xnList2 = xmlDoc.SelectNodes("/bot_settings/server_list/server");
            full_server_list = "";
            foreach (XmlNode xn in xnList2)
            {
                full_server_list += xn["server_name"].InnerText + ",";
            }
            full_server_list = full_server_list.TrimEnd(',');
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string[] server_list = full_server_list.Split(',');
            int index = 0;
            string tmp_server = "No_Server_Specified";
            string tmp_full_server = "No_Server_Specified";
            foreach (string server_name in server_list)
            {
                string[] server = server_name.Split('.');
                string tmp_server_name = "No_Server_Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
                if (tab_name[1].Equals(tmp_server_name))
                {
                    tmp_server = tmp_server_name;
                    tmp_full_server = server_name;
                    break;
                }
                else
                {
                    index++;
                }
            }
            string name = tabControl1.SelectedTab.Text;
            int selected_tab = tabControl1.SelectedIndex;
            bool chan_connected = false;
            int chan_index = 0;
            foreach (string channel in bot_instances[index].channel_list)
            {
                if (channel.Equals(name))
                {
                    chan_connected = true;
                    break;
                }
                chan_index++;
            }
            if (name.StartsWith("#") == true && bot_instances[index].connected == true && chan_connected == true)
            {
                bot_instances[index].channel_list.RemoveAt(chan_index);
                bot_instances[index].nick_list.RemoveAt(chan_index);
                bot_instances[index].sendData("PART", name);
            }

            if (name.Equals(tmp_server))
            {
                bool ended = false;

                while (!ended)
                {
                    ended = end_connection(tmp_full_server);
                }

                char[] charSep = new char[] { ':' };
                for (int x = tabControl1.SelectedIndex; x < tabControl1.TabPages.Count; x++)
                {
                    string[] tab_name = tabControl1.TabPages[x].Name.ToString().Split(charSep, 3);
                    if (tab_name[1].Equals(tmp_server))
                    {
                        if (tabControl1.TabPages.Count == 1)
                        {
                            break;
                        }
                        else
                        {
                            tabControl1.Controls.RemoveAt(x);
                            x--;
                        }
                    }
                }

                if (tabControl1.TabPages.Count == 1)
                {
                    tabControl1.SelectedIndex = 0;
                    Control control = tabControl1.TabPages[0].Controls[0];
                    RichTextBox output_box = (RichTextBox)control;
                    output_box.Name = "output_box_system";
                    output_box.Text = "No Server Connected";
                    control = tabControl1.TabPages[0];
                    TabPage tabpage = (TabPage)control;
                    tabpage.Name = "tabPage1";
                    tabpage.Text = "System";

                    button1.Visible = false;
                    button1.Enabled = false;
                    connectToolStripMenuItem.Text = "Connect";
                    connectToolStripMenuItem.Enabled = false;
                }
                bool server_found = false;
                foreach (string server_name in server_list)
                {
                    if (tmp_full_server.Equals(server_name))
                    {
                        server_found = true;
                        break;
                    }
                    else
                    {
                        server_found = false;
                        index++;
                    }
                }
                if (server_found == true && bot_instances.Count > index)
                {
                    bot_instances.RemoveAt(index);
                }
            }
            else
            {
                if (tabControl1.TabPages.Count == 1)
                {
                    tabControl1.SelectedIndex = 0;
                    Control control = tabControl1.TabPages[0].Controls[0];
                    RichTextBox output_box = (RichTextBox)control;
                    output_box.Name = "output_box_system";
                    output_box.Text = "No Server Connected";
                    control = tabControl1.TabPages[0];
                    TabPage tabpage = (TabPage)control;
                    tabpage.Name = "tabPage1";
                    tabpage.Text = "System";

                    button1.Visible = false;
                    button1.Enabled = false;
                    connectToolStripMenuItem.Text = "Connect";
                    connectToolStripMenuItem.Enabled = false;
                }
                else
                {
                    tabControl1.Controls.RemoveAt(selected_tab);
                    if (tabControl1.TabPages.Count <= selected_tab)
                    {
                        tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;
                    }
                    else
                    {
                        tabControl1.SelectedIndex = selected_tab;
                    }
                }
            }
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = 0;
            string tmp_server = "No_Server_Specified";
            string tmp_min_server = "No_Server_Specified";
            string[] server_list = full_server_list.Split(',');
            foreach (string server_name in server_list)
            {
                string[] server = server_name.Split('.');
                string tmp_server_name = "No_Server_Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
                if (tab_name[1].Equals(tmp_server_name))
                {
                    tmp_server = server_name;
                    tmp_min_server = tmp_server_name;
                    break;
                }
                else
                {
                    index++;
                }
            }
            if (connectToolStripMenuItem.Text.Equals("Disconnect"))
            {
                bot_instances[index].worker.CancelAsync();
                connectToolStripMenuItem.Text = "Connect";
                send_button.Enabled = false;
                input_box.Enabled = false;
            }
            else
            {
                bot_instances[index].worker.RunWorkerAsync(2000);
                connectToolStripMenuItem.Text = "Disconnect";
                send_button.Enabled = true;
                input_box.Enabled = true;
            }
        }

        private void Interface_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (bot bot_instance in bot_instances)
            {
                bot_instance.worker.CancelAsync();
            }
            MyNotifyIcon.Visible = false;
            updateOutput.Stop();
        }

        public bool start_connection(string start_server_name)
        {
            int index = 0;
            string[] tmp_server_list = full_server_list.Split(',');
            bool server_found = false;
            bool server_initiated = false;
            foreach (string server_name in tmp_server_list)
            {
                if (start_server_name.Equals(server_name))
                {
                    server_found = true;
                    break;
                }
                else
                {
                    server_found = false;
                    index++;
                }
            }
            if (server_found == true && bot_instances.Count > index)
            {
                if (bot_instances[index].connected == false)
                {
                    server_initiated = true;
                    bot_instances[index].worker.RunWorkerAsync(2000);
                    send_button.Enabled = true;
                    input_box.Enabled = true;
                }
            }
            else
            {
                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
                {
                    xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                }
                else
                {
                    XmlNode node = xmlDoc.CreateNode(XmlNodeType.Element, "global_settings", null);
                    XmlNode nodeCommand = xmlDoc.CreateElement("command_prefix");
                    nodeCommand.InnerText = ".";
                    node.AppendChild(nodeCommand);
                    XmlNode nodeKeep = xmlDoc.CreateElement("keep_logs");
                    nodeKeep.InnerText = "True";
                    node.AppendChild(nodeKeep);
                    XmlNode nodeLogs = xmlDoc.CreateElement("logs_path");
                    nodeLogs.InnerText = cur_dir + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + "";
                    node.AppendChild(nodeLogs);
                    XmlNode nodeStart = xmlDoc.CreateElement("start_with_windows");
                    nodeStart.InnerText = "False";
                    node.AppendChild(nodeStart);
                    XmlNode nodeTray = xmlDoc.CreateElement("minimize_to_tray");
                    nodeTray.InnerText = "False";
                    node.AppendChild(nodeTray);
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
                    xmlDoc.Save(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                    xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                }
                Control control = new Control();
                string[] server = start_server_name.Split('.');
                string tmp_server_name = "No_Server_Specified";
                if (server.GetUpperBound(0) > 0)
                {
                    tmp_server_name = server[1];
                }
                if (tabControl1.Controls.Find("output_box_system", true).GetUpperBound(0) >= 0)
                {
                    control = tabControl1.Controls.Find("output_box_system", true)[0];
                    RichTextBox output_box = (RichTextBox)control;
                    output_box.Name = "output_box_" + tmp_server_name + ":system";
                    control = tabControl1.Controls.Find("tabPage1", true)[0];
                    TabPage tabpage = (TabPage)control;
                    tabpage.Name = "tabPage:" + tmp_server_name + ":__:system";
                    tabpage.Text = tmp_server_name;
                }
                else if (tabControl1.Controls.Find("output_box_" + tmp_server_name + ":system", true).GetUpperBound(0) < 0)
                {
                    RichTextBox box = new RichTextBox();
                    box.Dock = System.Windows.Forms.DockStyle.Fill;
                    box.Location = new System.Drawing.Point(3, 3);
                    box.Name = "output_box_" + tmp_server_name + ":system";
                    box.Size = new System.Drawing.Size(826, 347);
                    box.ReadOnly = true;
                    box.Text = "";
                    TabPage tabpage = new TabPage();
                    tabpage.Controls.Add(box);
                    tabpage.Location = new System.Drawing.Point(4, 22);
                    tabpage.Name = "tabPage:" + tmp_server_name + ":__:system";
                    tabpage.Padding = new System.Windows.Forms.Padding(3);
                    tabpage.Size = new System.Drawing.Size(832, 353);
                    tabpage.Text = tmp_server_name;
                    tabpage.UseVisualStyleBackColor = true;
                    tabControl1.Controls.Add(tabpage);
                    tabControl1.GetControl(index).TabIndex = index;
                    tabControl1.Update();
                    box.LinkClicked += new LinkClickedEventHandler(link_Click);
                }
                else
                {
                }
                conf.command_list.Clear();
                conf.module_config.Clear();
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(start_server_name))
                    {
                        conf.name = xn["name"].InnerText;
                        conf.nick = xn["nick"].InnerText;
                        conf.secondary_nicks = xn["sec_nicks"].InnerText;
                        conf.pass = xn["password"].InnerText;
                        conf.email = xn["email"].InnerText;
                        conf.owner = xn["owner"].InnerText;
                        conf.port = Convert.ToInt32(xn["port"].InnerText);
                        conf.server = xn["server_name"].InnerText;
                        conf.chans = xn["chan_list"].InnerText;
                        conf.chan_blacklist = xn["chan_blacklist"].InnerText;
                        conf.user_level = Convert.ToInt32(xn["user_level"].InnerText);
                        conf.voice_level = Convert.ToInt32(xn["voice_level"].InnerText);
                        conf.hop_level = Convert.ToInt32(xn["hop_level"].InnerText);
                        conf.op_level = Convert.ToInt32(xn["op_level"].InnerText);
                        conf.sop_level = Convert.ToInt32(xn["sop_level"].InnerText);
                        conf.founder_level = Convert.ToInt32(xn["founder_level"].InnerText);
                        conf.owner_level = Convert.ToInt32(xn["owner_level"].InnerText);


                        XmlDocument xmlDocModules = new XmlDocument();
                        if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml"))
                        {
                            xmlDocModules.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml");
                        }
                        else
                        {
                            Directory.CreateDirectory(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"]);
                            File.Copy(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + "Default" + Path.DirectorySeparatorChar + "modules.xml", cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"] + Path.DirectorySeparatorChar + "modules.xml");
                            xmlDocModules.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText + Path.DirectorySeparatorChar + "modules.xml");
                        }
                        XmlNodeList xnList = xmlDocModules.SelectNodes("/modules/module");
                        foreach (XmlNode xn_module in xnList)
                        {
                            if (xn_module["enabled"].InnerText == "True")
                            {
                                List<string> tmp_list = new List<string>();
                                String module_name = xn_module["name"].InnerText;
                                String class_name = xn_module["class_name"].InnerText;
                                tmp_list.Add(class_name);
                                tmp_list.Add(module_name);
                                tmp_list.Add(xn_module["blacklist"].InnerText);

                                XmlNodeList optionList = xn_module.ChildNodes;
                                foreach (XmlNode option in optionList)
                                {
                                    if (option.Name.Equals("commands"))
                                    {
                                        XmlNodeList Options = option.ChildNodes;
                                        foreach (XmlNode options in Options)
                                        {
                                            List<string> tmp2_list = new List<string>();
                                            tmp2_list.Add(class_name);
                                            tmp2_list.Add(options["name"].InnerText);
                                            tmp2_list.Add(options["description"].InnerText);
                                            tmp2_list.Add(options["triggers"].InnerText);
                                            tmp2_list.Add(options["syntax"].InnerText);
                                            tmp2_list.Add(options["access_level"].InnerText);
                                            tmp2_list.Add(options["blacklist"].InnerText);
                                            tmp2_list.Add(options["show_help"].InnerText);
                                            tmp2_list.Add(options["spam_check"].InnerText);
                                            conf.command_list.Add(tmp2_list);
                                        }
                                    }
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
                        }
                        break;
                    }
                }

                button1.Visible = true;
                button1.Enabled = true;

                bot bot_instance = new bot();
                bot_instances.Add(bot_instance);
                bot_instances[bot_instances.Count - 1].start_bot(this, conf);
                server_initiated = true;
                connectToolStripMenuItem.Enabled = true;
                connectToolStripMenuItem.Text = "Disconnect";
                tabControl1.SelectedIndex = tabControl1.TabPages.IndexOfKey("tabPage:" + tmp_server_name + ":__:system");
            }
            return server_initiated;
        }

        public bool end_connection(string start_server_name)
        {
            int index = 0;
            string[] tmp_server_list = full_server_list.Split(',');
            bool server_found = false;
            bool server_initiated = false;
            foreach (string server_name in tmp_server_list)
            {
                if (start_server_name.Equals(server_name))
                {
                    server_found = true;
                    break;
                }
                else
                {
                    server_found = false;
                    index++;
                }
            }
            if (server_found == true)
            {
                if (bot_instances[index].connected == true)
                {
                    server_initiated = true;
                    bot_instances[index].disconnected = true;
                    bot_instances[index].worker.CancelAsync();
                    input_box.Enabled = false;
                    send_button.Enabled = false;
                }
            }
            return server_initiated;
        }

        public bool bot_connected(string input_server_name)
        {
            string[] server_list = full_server_list.Split(',');
            int index = 0;
            bool bot_found = false;
            foreach (string server_name in server_list)
            {
                if (input_server_name.Equals(server_name))
                {
                    bot_found = true;
                    break;
                }
                else
                {
                    index++;
                }
            }
            if (bot_found == true && bot_instances.Count > index)
            {
                return bot_instances[index].connected;
            }
            else
            {
                return false;
            }
        }

        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            input_box.Focus();
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                button1_Click(sender, e);
            }
        }

        private void tabControl1_MouseClick(object sender, EventArgs e)
        {
            input_box.Focus();
        }
    }
}