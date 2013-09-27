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
    public partial class Interface : Form
    {
        public ClientConfig irc_conf = new ClientConfig();
        public string cur_dir = "";
        public List<string> queue_text = new List<string>();
        public DateTime run_time = new DateTime();
        private System.Windows.Forms.Timer updateOutput = new System.Windows.Forms.Timer();

        public readonly object listLock = new object();
        public readonly object errorlock = new object();

        private string output = "";
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
            run_time = DateTime.Now;
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
            start_client();
        }

        private void MyNotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MyNotifyIcon.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        private void Interface_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && irc_conf.minimize_to_tray == true)
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
                MyNotifyIcon.Visible = true;
            }
        }

        private void start_client()
        {
            cur_dir = Directory.GetCurrentDirectory();

            updateOutput.Interval = 30;
            updateOutput.Start();

            queue_text.Capacity = 1000;
            queue_text.Clear();

            // Load Bot Configuration
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/global_settings");
                irc_conf.command = list["command_prefix"].InnerText;
                irc_conf.keep_logs = list["keep_logs"].InnerText;
                if (Directory.Exists(list["logs_path"].InnerText))
                {
                    irc_conf.logs_path = list["logs_path"].InnerText;
                }
                else
                {
                    irc_conf.logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
                }
                irc_conf.spam_count_max = Convert.ToInt32(list["spam_count"].InnerText);
                irc_conf.spam_threshold = Convert.ToInt32(list["spam_threshold"].InnerText);
                irc_conf.spam_timout = Convert.ToInt32(list["spam_timeout"].InnerText);
                irc_conf.max_message_length = Convert.ToInt32(list["max_message_length"].InnerText);
                irc_conf.minimize_to_tray = Convert.ToBoolean(list["minimize_to_tray"].InnerText);
                if (irc_conf.minimize_to_tray == true)
                {
                    MyNotifyIcon.Visible = true;
                }
                else
                {
                    MyNotifyIcon.Visible = false;
                }

                irc_conf.bot_instances = new List<bot>();
                int index = 0;
                connectToolStripMenuItem.Enabled = false;
                
                connectToolStripMenuItem.Text = "Disconnect";
                connectToolStripMenuItem.Enabled = true;
                bool server_started = false;
                XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in xnList)
                {
                    server_started = connect(xn["server_name"].InnerText, false);
                    if (server_started)
                    {
                        index++;
                    }
                }
                if (index == 0)
                {
                    Control control = new Control();
                    control = tabControl1.Controls.Find("output_box_system", true)[0];
                    RichTextBox output_box = (RichTextBox)control;
                    output_box.AppendText("No Servers Specified for Auto Start");
                }
            }
            else
            {
                Control control = new Control();
                control = tabControl1.Controls.Find("output_box_system", true)[0];
                RichTextBox output_box = (RichTextBox)control;
                output_box.AppendText("Missing Config File.");
            }
        }

        public bool connect(string server_name, bool manual)
        {
            bool server_initiated = false;
            int index = 0;
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in xnList)
                {
                    if (xn["server_name"].InnerText.Equals(server_name))
                    {
                        BotConfig bot_conf = new BotConfig();
                        bot_conf.module_config = new List<List<string>>();
                        bot_conf.command_list = new List<List<string>>();
                        bot_conf.spam_check = new List<spam_info>();
                        bot_conf.module_config.Clear();
                        bot_conf.command_list.Clear();
                        bot_conf.name = xn["name"].InnerText;
                        bot_conf.nick = xn["nick"].InnerText;
                        bot_conf.secondary_nicks = xn["sec_nicks"].InnerText;
                        bot_conf.pass = xn["password"].InnerText;
                        bot_conf.email = xn["email"].InnerText;
                        bot_conf.owner = xn["owner"].InnerText;
                        bot_conf.port = Convert.ToInt32(xn["port"].InnerText);
                        bot_conf.server = xn["server_name"].InnerText;
                        bot_conf.server_address = xn["server_address"].InnerText;
                        bot_conf.chans = xn["chan_list"].InnerText;
                        bot_conf.chan_blacklist = xn["chan_blacklist"].InnerText;
                        bot_conf.ignore_list = xn["ignore_list"].InnerText;
                        bot_conf.user_level = Convert.ToInt32(xn["user_level"].InnerText);
                        bot_conf.voice_level = Convert.ToInt32(xn["voice_level"].InnerText);
                        bot_conf.hop_level = Convert.ToInt32(xn["hop_level"].InnerText);
                        bot_conf.op_level = Convert.ToInt32(xn["op_level"].InnerText);
                        bot_conf.sop_level = Convert.ToInt32(xn["sop_level"].InnerText);
                        bot_conf.founder_level = Convert.ToInt32(xn["founder_level"].InnerText);
                        bot_conf.owner_level = Convert.ToInt32(xn["owner_level"].InnerText);
                        bot_conf.auto_connect = Convert.ToBoolean(xn["auto_connect"].InnerText);
                        bot_conf.default_level = Math.Min(bot_conf.user_level, Math.Min(bot_conf.voice_level, Math.Min(bot_conf.hop_level, Math.Min(bot_conf.op_level, Math.Min(bot_conf.sop_level, Math.Min(bot_conf.founder_level, bot_conf.owner_level)))))) - 1;

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
                                            bot_conf.command_list.Add(tmp2_list);
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
                                bot_conf.module_config.Add(tmp_list);
                            }
                        }
                        if (bot_conf.auto_connect || manual)
                        {
                            Control control = new Control();
                            if (tabControl1.Controls.Find("output_box_system", true).GetUpperBound(0) >= 0)
                            {
                                control = tabControl1.Controls.Find("output_box_system", true)[0];
                                RichTextBox output_box = (RichTextBox)control;
                                output_box.Name = "output_box_" + bot_conf.server + ":system";
                                control = tabControl1.Controls.Find("tabPage1", true)[0];
                                TabPage tabpage = (TabPage)control;
                                tabpage.Name = "tabPage:" + bot_conf.server + ":__:system";
                                tabpage.Text = bot_conf.server;
                            }
                            else if (tabControl1.Controls.Find("output_box_" + bot_conf.server + ":system", true).GetUpperBound(0) < 0)
                            {
                                RichTextBox box = new RichTextBox();
                                box.Dock = System.Windows.Forms.DockStyle.Fill;
                                box.Location = new System.Drawing.Point(3, 3);
                                box.Name = "output_box_" + bot_conf.server + ":system";
                                box.BackColor = Color.White;
                                box.ReadOnly = true;
                                box.Text = "";
                                box.HideSelection = false;
                                TabPage tabpage = new TabPage();
                                tabpage.Controls.Add(box);
                                tabpage.Location = new System.Drawing.Point(4, 22);
                                tabpage.Name = "tabPage:" + bot_conf.server + ":__:system";
                                tabpage.Padding = new System.Windows.Forms.Padding(3);
                                tabpage.Text = bot_conf.server;
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
                                bot_conf.server_ip = Dns.GetHostAddresses(bot_conf.server_address);
                            }
                            catch
                            {
                                bot_conf.server_ip = null;
                            }

                            button1.Visible = true;
                            button1.Enabled = true;

                            bot bot_instance = new bot();
                            bot_instance.conf = bot_conf;
                            bot_instance.worker.WorkerSupportsCancellation = true;
                            bot_instance.start_bot(this);
                            irc_conf.bot_instances.Add(bot_instance);
                            server_initiated = true;
                        }
                        break;
                    }
                }
            }
            return server_initiated;
        }

        private void tab_changed(object sender, EventArgs e)
        {
            int index = 0;
            string nick = "";
            string server = "";
            string chan = "";
            string chan_name = "";
            foreach (bot bot in irc_conf.bot_instances)
            {
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 4);
                if (tab_name[1].Equals(bot.conf.server))
                {
                    nick = bot.nick;
                    chan = tab_name[2];
                    chan_name = tab_name[3];
                    server = bot.conf.server;
                    break;
                }
                else
                {
                    index++;
                }
            }
            if (index < irc_conf.bot_instances.Count())
            {
                string title = "IRCBot: ";
                if (chan.Equals("__"))
                {
                    title += nick + " @ " + server;
                }
                else if (chan.Equals("_chan_"))
                {
                    title += nick + " @ " + server + " / " + chan_name;
                }
                else if (chan.Equals("_user_"))
                {
                    title += "Dialog with " + chan_name + " @ " + server;
                }
                else
                {
                    title = "IRCBot";
                }
                this.Text = title;
                if (irc_conf.bot_instances[index].connected == true)
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
        }

        protected void link_Click(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void gui_cmd_send()
        {
            int index = 0;
            string tmp_server = "No_Server_Specified";
            string channel = "";
            string msg = "";
            foreach (bot bot in irc_conf.bot_instances)
            {
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
                if (tab_name[1].Equals(bot.conf.server))
                {
                    tmp_server = bot.conf.server;

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
                bool bot_command = true;
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 4);
                channel = tab_name[3];
                string type = "channel";
                if (tab_name[2].Equals("_user_"))
                {
                    type = "query";
                }

                if (input.GetUpperBound(0) > 0)
                {
                    msg = irc_conf.command + input[0].TrimStart('/') + " " + input[1];
                }
                else
                {
                    msg = irc_conf.command + input[0].TrimStart('/');
                }
                string line = ":" + irc_conf.bot_instances[index].nick + " PRIVMSG " + channel + " :" + msg;
                string[] ex = line.Split(charSeparator, 5);
                //Run Enabled Modules
                List<Modules.Module> tmp_module_list = new List<Modules.Module>();
                tmp_module_list.AddRange(irc_conf.bot_instances[index].module_list);
                int module_index = 0;
                foreach (Modules.Module module in tmp_module_list)
                {
                    module_index = 0;
                    bool module_found = false;
                    string module_blacklist = "";
                    foreach (List<string> conf_module in irc_conf.bot_instances[index].conf.module_config)
                    {
                        if (module.ToString().Equals("IRCBot.Modules." + conf_module[0]))
                        {
                            module_blacklist = conf_module[2];
                            module_found = true;
                            break;
                        }
                        module_index++;
                    }
                    if (module_found == true)
                    {
                        module.control(irc_conf.bot_instances[index], ref irc_conf.bot_instances[index].conf, module_index, ex, ex[3].TrimStart(':').TrimStart(Convert.ToChar(irc_conf.command)), irc_conf.bot_instances[index].conf.owner_level, irc_conf.bot_instances[index].nick, channel, bot_command, type);
                    }
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
                        irc_conf.bot_instances[index].sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0] + " " + input[1]);
                    }
                    else
                    {
                        irc_conf.bot_instances[index].sendData("PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0]);
                    }
                }
            }
        }

        private void send_button_Click(object sender, EventArgs e)
        {
            gui_cmd_send();
            input_box.Text = "";
        }

        private void UpdateOutput(object sender, EventArgs e)
        {
            Thread updateOutputThread = null;
            updateOutputThread = new Thread(new ThreadStart(this.ThreadProcSafeOutput));
            updateOutputThread.Name = "updateOutput";
            updateOutputThread.Start();
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
                string pattern = "[^a-zA-Z0-9-_.+#]"; //regex pattern
                bool instance_found = false;
                bot tmp_bot = new bot();
                foreach (bot bot in irc_conf.bot_instances)
                {
                    if (bot.conf.server.Equals(tmp_server_name))
                    {
                        instance_found = true;
                        tmp_bot = bot;
                    }
                }

                string channel = "System";
                string message = text;
                string nickname = "";
                string prefix = "---";
                string postfix = "---";
                string nick_sep = " ";
                string nick_color = "#C73232";
                string pre_color = "#C73232";
                string post_color = "#C73232";
                string sep_color = "#C73232";
                string message_color = "#000000";

                bool display_message = true;

                if (tabControl1.Controls.Find("output_box_" + tmp_server_name + ":system", true).GetUpperBound(0) >= 0 && instance_found)
                {
                    Control control = tabControl1.Controls.Find("output_box_" + tmp_server_name + ":system", true)[0];
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
                            if (!nickname.ToLower().Equals("nickserv") && !nickname.ToLower().Equals("chanserv"))
                            {
                                char[] charSep = new char[] { ':' };
                                string[] tab = tabControl1.SelectedTab.Name.ToString().Split(charSep, 4);
                                if (tab[1].Equals(tmp_bot.conf.server))
                                {
                                    if (!tab[3].Equals("system"))
                                    {
                                        channel = tab[3];
                                    }
                                }
                            }
                            prefix = "---";
                            pre_color = "#57799E";
                            postfix = "---";
                            post_color = "#57799E";
                            nick_sep = " ";
                            sep_color = "#57799E";
                            nick_color = "#57799E";
                            message_color = "#B037B0";
                        }
                        else if (tmp_lines[1].Equals("privmsg", StringComparison.InvariantCultureIgnoreCase))
                        {
                            channel = tmp_lines[2].TrimStart(':');
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            message = tmp_lines[3].Remove(0, 1);
                            nick_sep = " ";
                            sep_color = "#C73232";
                            nick_color = "#57799E";
                            message_color = "#000000";
                            if (channel.Equals(tmp_bot.nick))
                            {
                                if (nickname.Equals("nickserv", StringComparison.InvariantCultureIgnoreCase) || nickname.Equals("chanserv", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    channel = "System";
                                    prefix = "";
                                    pre_color = "#C73232";
                                    postfix = "--->";
                                    post_color = "#C73232";
                                    nickname = "";
                                }
                                else
                                {
                                    channel = nickname;
                                    prefix = "";
                                    pre_color = "#C73232";
                                    postfix = " --->";
                                    post_color = "#C73232";
                                }
                            }
                            else if (channel.Equals("nickserv", StringComparison.InvariantCultureIgnoreCase) && nickname.Equals(tmp_bot.nick))
                            {
                                channel = "System";
                                prefix = "<---";
                                pre_color = "#C73232";
                                postfix = "";
                                post_color = "#C73232";
                                nickname = "";
                            }
                            else if (channel.Equals("chanserv", StringComparison.InvariantCultureIgnoreCase) && nickname.Equals(tmp_bot.nick))
                            {
                                channel = "System";
                                prefix = "<---";
                                pre_color = "#C73232";
                                postfix = "";
                                post_color = "#C73232";
                                nickname = "";
                            }
                            else
                            {
                                prefix = "";
                                pre_color = "#C73232";
                                postfix = " --->";
                                post_color = "#C73232";
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
                                    pre_color = "#BF4299";
                                    postfix = ">";
                                    post_color = "#94359A";
                                    Regex reg = new Regex(ctcp_pattern);
                                    message = nickname + " " + reg.Replace(message, "$2");
                                    nickname = "";
                                }
                                else
                                {
                                    if (nickname.Equals(tmp_bot.nick))
                                    {
                                        prefix = "<<";
                                        pre_color = "#DC5C63";
                                        postfix = "<<";
                                        post_color = "#B43964";
                                        nickname = "";
                                    }
                                    else
                                    {
                                        prefix = ">>";
                                        pre_color = "#DC5C63";
                                        postfix = ">>";
                                        post_color = "#B43964";
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
                            pre_color = "#3DCC3D";
                            postfix = "";
                            post_color = "#3DCC3D";
                            nick_color = "#57799E";
                            message_color = "#3DCC3D";
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
                            pre_color = "#66361F";
                            postfix = "";
                            post_color = "#66361F";
                            nick_color = "#57799E";
                            message_color = "#8F3902";
                            if (channel.StartsWith("#"))
                            {
                                if (nickname.Equals(tmp_bot.nick))
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
                            int index = 0;
                            foreach (List<string> nicks in tmp_bot.nick_list)
                            {
                                int nick_index = 0;
                                foreach (string nick in nicks)
                                {
                                    string[] sep_nick = nick.Split(':');
                                    if (sep_nick.GetUpperBound(0) > 0)
                                    {
                                        if (sep_nick[1].Equals(nickname, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            channel += "," + nicks[0];
                                            tmp_bot.nick_list[index].RemoveAt(nick_index);
                                            break;
                                        }
                                    }
                                    nick_index++;
                                }
                                index++;
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
                            pre_color = "#66361F";
                            postfix = "";
                            post_color = "#66361F";
                            nick_color = "#57799E";
                            message_color = "#8F3902";
                        }
                        else if (tmp_lines[1].Equals("nick", StringComparison.InvariantCultureIgnoreCase))
                        {
                            nickname = tmp_lines[2].TrimStart(':');
                            channel = "";
                            int index = 0;
                            foreach (List<string> nicks in tmp_bot.nick_list)
                            {
                                foreach (string nick in nicks)
                                {
                                    string[] sep_nick = nick.Split(':');
                                    if (sep_nick.GetUpperBound(0) > 0)
                                    {
                                        if (sep_nick[1].Equals(nickname, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            channel += "," + nicks[0];
                                            break;
                                        }
                                    }
                                }
                                index++;
                            }
                            channel = channel.TrimStart(',');
                            message = tmp_lines[0].TrimStart(':').Split('!')[0] + " is now known as " + nickname;
                            prefix = "--";
                            pre_color = "#C73232";
                            postfix = "--";
                            post_color = "#C73232";
                            nickname = "*";
                            nick_color = "#57799E";
                            message_color = "#000000";
                        }
                        else if (tmp_lines[1].Equals("mode", StringComparison.InvariantCultureIgnoreCase))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                            }
                            else if (channel.Equals(tmp_bot.nick))
                            {
                                channel = "System";
                            }
                            if (nickname.Equals(tmp_bot.nick))
                            {
                                display_message = false;
                            }
                            else
                            {
                                message = "has set Mode " + tmp_lines[3];
                            }
                            prefix = ">> ";
                            pre_color = "#1E90FF";
                            postfix = "";
                            post_color = "#1E90FF";
                            nick_color = "#1E90FF";
                            message_color = "#1E90FF";
                        }
                        else if (tmp_lines[1].Equals("topic", StringComparison.InvariantCultureIgnoreCase))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            message = "has set Topic " + tmp_lines[3];
                            prefix = "";
                            pre_color = "#1E90FF";
                            postfix = "";
                            post_color = "#C73232";
                            nick_color = "#57799E";
                            message_color = "#000000";
                        }
                        else if (tmp_lines[1].Equals("kick", StringComparison.InvariantCultureIgnoreCase))
                        {
                            channel = tmp_lines[2];
                            nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                            if (channel.StartsWith("#"))
                            {
                            }
                            else if (channel.Equals(tmp_bot.nick))
                            {
                                channel = "System";
                            }
                            message = "has kicked " + tmp_lines[3].Replace(':', '(') + ")";
                            prefix = "";
                            pre_color = "#1E90FF";
                            postfix = "";
                            post_color = "#C73232";
                            nick_color = "#57799E";
                            message_color = "#C73232";
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
                                if (tmp_lines[2].Equals(tmp_bot.nick) && tmp_lines[3].StartsWith("#") && new_lines.GetUpperBound(0) > 0)
                                {
                                    if (new_lines[1] != ":End of /NAMES list.")
                                    {
                                        channel = new_lines[0];
                                        nickname = tmp_lines[0].TrimStart(':').Split('!')[0];
                                        message = new_lines[1].TrimStart(':');
                                        message_color = "#B037B0";
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
                    string[] channels = channel.Split(',');
                    foreach (string channel_line in channels)
                    {
                        string tab_name = Regex.Replace(channel_line, pattern, "_");
                        if (nickname.Equals(string.Empty) && message.Equals(string.Empty))
                        {
                            display_message = false;
                        }
                        if (display_message)
                        {
                            string[] nick = tmp_lines[0].Split('!');
                            if (!channel_line.Equals("System"))
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
                            if (output_box.Lines.Length > 500)
                            {
                                Regex reg = new Regex(@"(\[\\cf1)(.*?)(\n)", RegexOptions.IgnoreCase);
                                MatchCollection matches = reg.Matches(output_box.Rtf);
                                if (matches.Count > 0)
                                {
                                    output_box.Rtf = output_box.Rtf.Replace(matches[0].Value, "");
                                }
                            }
                            int cur_sel = output_box.Text.Length + 1;
                            int prefix_length = prefix.Length;
                            int postfix_length = postfix.Length;
                            int sep_length = nick_sep.Length;
                            int nickname_length = nickname.Length;
                            int message_length = message.Length;
                            int timestamp_length = time_stamp.Length;
                            string line = "[" + time_stamp + "] " + prefix + nickname + postfix + nick_sep + message;
                            int line_length = line.Length;

                            Color preColor;
                            preColor = System.Drawing.ColorTranslator.FromHtml(pre_color);
                            Color postColor;
                            postColor = System.Drawing.ColorTranslator.FromHtml(post_color);
                            Color sepColor;
                            sepColor = System.Drawing.ColorTranslator.FromHtml(sep_color);
                            Color nickColor;
                            nickColor = System.Drawing.ColorTranslator.FromHtml(nick_color);
                            Color msgColor;
                            msgColor = System.Drawing.ColorTranslator.FromHtml(message_color);

                            //Append line to end of outbox
                            output_box.AppendText(line + Environment.NewLine);

                            //timstamp coloring
                            output_box.SelectionStart = cur_sel;
                            output_box.SelectionLength = timestamp_length + 2;
                            output_box.SelectionColor = Color.Black;

                            cur_sel += timestamp_length + 2;

                            if (prefix_length > 0)
                            {
                                //Prefix Coloring
                                output_box.SelectionStart = cur_sel;
                                output_box.SelectionLength = prefix_length;
                                output_box.SelectionColor = preColor;

                                cur_sel += prefix_length;
                            }

                            if (nickname_length > 0)
                            {
                                //Nick Coloring
                                output_box.SelectionStart = cur_sel;
                                output_box.SelectionLength = nickname_length;
                                output_box.SelectionColor = nickColor;

                                cur_sel += nickname_length;
                            }

                            if (postfix_length > 0)
                            {
                                //Postfix Coloring
                                output_box.SelectionStart = cur_sel;
                                output_box.SelectionLength = postfix_length;
                                output_box.SelectionColor = postColor;

                                cur_sel += postfix_length;
                            }

                            if (sep_length > 0)
                            {
                                //Separator Coloring
                                output_box.SelectionStart = cur_sel;
                                output_box.SelectionLength = sep_length;
                                output_box.SelectionColor = sepColor;

                                cur_sel += sep_length;
                            }

                            if (message_length > 0)
                            {
                                //message coloring
                                output_box.SelectionStart = cur_sel;
                                output_box.SelectionLength = message_length;
                                output_box.SelectionColor = msgColor;
                            }

                            output_box.SelectionStart = output_box.Text.Length;
                            output_box.ScrollToCaret();
                        }
                        if (irc_conf.keep_logs.Equals("True"))
                        {
                            string file_name = "";
                            file_name = tmp_server_name + "-" + tab_name + ".log";
                            if (irc_conf.logs_path == "")
                            {
                                irc_conf.logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
                            }
                            if (Directory.Exists(irc_conf.logs_path))
                            {
                                StreamWriter log_file = File.AppendText(irc_conf.logs_path + Path.DirectorySeparatorChar + file_name);
                                log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + text);
                                log_file.Close();
                            }
                            else
                            {
                                Directory.CreateDirectory(cur_dir + Path.DirectorySeparatorChar + "logs");
                                StreamWriter log_file = File.AppendText(cur_dir + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + file_name);
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
                // disable tab change events (screws up updating)
                this.tabControl1.SelectedIndexChanged -= new System.EventHandler(this.tabControl1_MouseClick);
                this.tabControl1.MouseClick -= new System.Windows.Forms.MouseEventHandler(this.tabControl1_MouseClick);
                tabControl1.SelectedIndexChanged -= tab_changed;

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
                box.BackColor = Color.White;
                box.ReadOnly = true;
                box.Text = "";
                box.LinkClicked += new LinkClickedEventHandler(link_Click);
                box.HideSelection = false;

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
                tabpage.Text = channel[1];
                tabpage.UseVisualStyleBackColor = true;

                tabControl1.Controls.Add(tabpage);

                tabControl1.Update();

                string[][] tab_names = new string[tabControl1.TabPages.Count][];
                int index = 0;
                foreach (TabPage tab in tabControl1.TabPages)
                {
                    string[] tmp = new string[3] {
                        tab.Name,
                        index.ToString(),
                        tab.Text
                    };
                    tab_names[index] = tmp;
                    index++;
                }

                Sort<string>(tab_names, 0);

                TabControl tmp_tabcontrol = new TabControl();
                foreach (Control page in tabControl1.TabPages)
                {
                    tmp_tabcontrol.Controls.Add(page);
                }

                tabControl1.Controls.Clear();

                int tab_index = 0;
                index = 0;
                foreach (string[] page in tab_names)
                {
                    tabControl1.Controls.Add(tmp_tabcontrol.Controls.Find(page[0], true)[0]);
                    if (page[0].Equals(tabpage.Name))
                    {
                        tab_index = index;
                    }
                    index++;
                }

                tabControl1.Update();

                // enable tab change events again.
                this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_MouseClick);
                this.tabControl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.tabControl1_MouseClick);
                tabControl1.SelectedIndexChanged += tab_changed;

                tabControl1.SelectedIndex = tab_index;
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
            foreach (bot bot_instance in irc_conf.bot_instances)
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
                e.Handled = true;
                gui_cmd_send();
                input_box.Text = "";
            }
        }

        public void update_conf()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");

            XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/global_settings");
            irc_conf.command = list["command_prefix"].InnerText;
            irc_conf.keep_logs = list["keep_logs"].InnerText;
            if (Directory.Exists(list["logs_path"].InnerText))
            {
                irc_conf.logs_path = list["logs_path"].InnerText;
            }
            else
            {
                irc_conf.logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
            }
            irc_conf.max_message_length = Convert.ToInt32(list["max_message_length"].InnerText);
            irc_conf.minimize_to_tray = Convert.ToBoolean(list["minimize_to_tray"].InnerText);

            foreach (bot bot_instance in irc_conf.bot_instances)
            {
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
                        bot_instance.conf.server_address = xn["server_address"].InnerText;
                        bot_instance.conf.chans = xn["chan_list"].InnerText;
                        bot_instance.conf.chan_blacklist = xn["chan_blacklist"].InnerText;
                        bot_instance.conf.ignore_list = xn["ignore_list"].InnerText;

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
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int index = 0;
            string tmp_server = "No_Server_Specified";
            foreach (bot bot in irc_conf.bot_instances)
            {
                char[] charSep = new char[] { ':' };
                string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
                if (tab_name[1].Equals(bot.conf.server))
                {
                    tmp_server = bot.conf.server;
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
            if (index < irc_conf.bot_instances.Count())
            {
                foreach (string channel in irc_conf.bot_instances[index].channel_list)
                {
                    if (channel.Equals(name))
                    {
                        chan_connected = true;
                        break;
                    }
                    chan_index++;
                }
            }
            if (name.StartsWith("#") == true && irc_conf.bot_instances[index].connected == true && chan_connected == true)
            {
                irc_conf.bot_instances[index].channel_list.RemoveAt(chan_index);
                irc_conf.bot_instances[index].nick_list.RemoveAt(chan_index);
                irc_conf.bot_instances[index].sendData("PART", name);
            }

            if (name.Equals(tmp_server))
            {
                bool ended = false;

                if (chan_connected == true)
                {
                    while (!ended)
                    {
                        ended = end_connection(tmp_server);
                    }
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
                            tabControl1.TabPages.RemoveAt(x);
                            tabControl1.Update();
                            x--;
                        }
                    }
                }

                if (tabControl1.TabPages.Count == 0)
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
                index = 0;
                foreach (bot bot in irc_conf.bot_instances)
                {
                    if (tmp_server.Equals(bot.conf.server))
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
                if (server_found == true && irc_conf.bot_instances.Count > index)
                {
                    irc_conf.bot_instances.RemoveAt(index);
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
            char[] charSep = new char[] { ':' };
            string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
            foreach (bot bot in irc_conf.bot_instances)
            {
                if (tab_name[1].Equals(bot.conf.server))
                {
                    break;
                }
                else
                {
                    index++;
                }
            }
            if (connectToolStripMenuItem.Text.Equals("Disconnect"))
            {
                if (irc_conf.bot_instances[index].worker.IsBusy)
                {
                    bool closed = end_connection(tab_name[1]);
                    if (closed)
                    {
                        connectToolStripMenuItem.Text = "Connect";
                        send_button.Enabled = false;
                        input_box.Enabled = false;
                    }
                }
            }
            else
            {
                if (!irc_conf.bot_instances[index].worker.IsBusy)
                {
                    bool started = start_connection(tab_name[1]);
                    if (started)
                    {
                        connectToolStripMenuItem.Text = "Disconnect";
                        send_button.Enabled = true;
                        input_box.Enabled = true;
                    }
                }
            }
        }

        private void Interface_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (bot bot_instance in irc_conf.bot_instances)
            {
                bot_instance.worker.CancelAsync();
            }
            MyNotifyIcon.Visible = false;
            updateOutput.Stop();
        }

        public bool start_connection(string start_server_name)
        {
            int index = 0;
            bool server_found = false;
            bool server_initiated = false;
            foreach (bot bot in irc_conf.bot_instances)
            {
                if (bot.connected == false && bot.conf.server.Equals(start_server_name))
                {
                    server_found = true;
                    break;
                }
                index++;
            }
            if (server_found == true)
            {
                server_initiated = true;
                irc_conf.bot_instances[index].worker.RunWorkerAsync(2000);
                send_button.Enabled = true;
                input_box.Enabled = true;
            }
            else
            {
                server_initiated = connect(start_server_name, true);
            }
            return server_initiated;
        }

        public bool end_connection(string start_server_name)
        {
            bool server_terminated = false;
            int index = 0;
            foreach (bot bot in irc_conf.bot_instances)
            {
                if ((bot.connected == true || bot.connecting == true) && bot.conf.server.Equals(start_server_name))
                {
                    server_terminated = true;
                    bot.disconnected = true;
                    bot.restart = false;
                    bot.worker.CancelAsync();
                    input_box.Enabled = false;
                    send_button.Enabled = false;
                    bot.worker.CancelAsync();
                    break;
                }
                index++;
            }
            return server_terminated;
        }

        public bool bot_connected(string input_server_name)
        {
            foreach (bot bot in irc_conf.bot_instances)
            {
                if (bot.connected == true && bot.conf.server.Equals(input_server_name))
                {
                    return bot.connected;
                }
            }
            return false;
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

        public void log_error(Exception ex)
        {
            string errorMessage =
                "Unhandled Exception:\n\n" +
                ex.Message + "\n\n" +
                ex.GetType() +
                "\n\nStack Trace:\n" +
                ex.StackTrace;

            string file_name = "";
            string logs_path = "";
            file_name = "Errors.log";
            string time_stamp = DateTime.Now.ToString("hh:mm tt");
            string date_stamp = DateTime.Now.ToString("yyyy-MM-dd");
            string cur_dir = Directory.GetCurrentDirectory();
            logs_path = irc_conf.logs_path;

            if (Directory.Exists(logs_path + Path.DirectorySeparatorChar + "errors"))
            {
                StreamWriter log_file = File.AppendText(logs_path + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + errorMessage);
                log_file.Close();
            }
            else
            {
                Directory.CreateDirectory(logs_path + Path.DirectorySeparatorChar + "errors");
                StreamWriter log_file = File.AppendText(logs_path + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + errorMessage);
                log_file.Close();
            }
        }

        private void output_box_system_MouseClick(object sender, MouseEventArgs e)
        {
            input_box.Focus();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (input_box.SelectionLength > 0)
            {
                input_box.Cut();
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            char[] charSep = new char[] { ':' };
            string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 4);
            RichTextBox control = new RichTextBox();
            if (tab_name[2].Equals("__"))
            {
                control = (RichTextBox)tabControl1.Controls.Find("output_box_" + tab_name[1] + ":system", true)[0];
            }
            if (tab_name[2].Equals("_chan_"))
            {
                control = (RichTextBox)tabControl1.Controls.Find("output_box_chan_" + tab_name[1] + ":" + tab_name[3], true)[0];
            }
            else if (tab_name[2].Equals("_user_"))
            {
                control = (RichTextBox)tabControl1.Controls.Find("output_box_user_" + tab_name[1] + ":" + tab_name[3], true)[0];
            }
            else
            {
            }
            if (control.SelectionLength > 0)
            {
                control.Copy();
            }
            else if (input_box.SelectionLength > 0)
            {
                input_box.Copy();
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (input_box.Focused)
            {
                input_box.Paste();
            }
        }

        private void Interface_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
            {
                copyToolStripMenuItem_Click(sender, e);
            }
            else if (e.KeyCode == Keys.X && e.Modifiers == Keys.Control)
            {
                cutToolStripMenuItem_Click(sender, e);
            }
            else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                pasteToolStripMenuItem_Click(sender, e);
            }
        }
    }

    public class ClientConfig
    {
        public string command { get; set; }
        public string keep_logs { get; set; }
        public string logs_path { get; set; }
        public bool minimize_to_tray { get; set; }
        public int spam_count_max { get; set; }
        public int spam_threshold { get; set; }
        public int spam_timout { get; set; }
        public int max_message_length { get; set; }
        public List<bot> bot_instances { get; set; }
    }
}
