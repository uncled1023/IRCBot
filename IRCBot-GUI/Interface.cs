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
using System.Reflection;

namespace IRCBot_GUI
{
    public partial class Interface : Form
    {
        internal IRCBot.bot_controller controller;
        internal ClientConfig irc_conf = new ClientConfig();
        internal string cur_dir = Directory.GetCurrentDirectory();

        private System.Windows.Forms.Timer updateOutput = new System.Windows.Forms.Timer();
        private string output = "";
        private System.Windows.Forms.NotifyIcon MyNotifyIcon;

        delegate void SetTextCallback(string text);

        ContextMenu OutputMenu = new ContextMenu();
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
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

            InitializeComponent();

            button1.Enabled = false;
            button1.Visible = false;

            updateOutput.Tick += UpdateOutput;
            Control control = new Control();
            control = tabControl1.Controls.Find("output_box_system", true)[0];
            RichTextBox output_box = (RichTextBox)control;
            output_box.LinkClicked += link_Click;
            output_box.ContextMenu = OutputMenu;
            output_box.ContextMenu.Popup += ContextMenu_Popup;

            MyNotifyIcon = new System.Windows.Forms.NotifyIcon();
            MyNotifyIcon.Visible = false;
            MyNotifyIcon.Icon = new Icon(GetType(), "Bot.ico");
            MyNotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(MyNotifyIcon_MouseDoubleClick);
            MyNotifyIcon.ContextMenu = TrayMenu;

            MenuItem copy_item = new MenuItem();
            copy_item.Text = "Copy";
            copy_item.Click += new EventHandler(copyToolStripMenuItem_Click);
            OutputMenu.MenuItems.Add(copy_item);

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

            this.Icon = new Icon(GetType(), "Bot.ico");
            tabControl1.SelectedIndexChanged += tab_changed;
            start_client();
        }

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Resources";
            string assemblyPath = folderPath + Path.DirectorySeparatorChar + new AssemblyName(args.Name).Name + ".dll";
            if (File.Exists(assemblyPath) == false) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        void ContextMenu_Popup(object sender, EventArgs e)
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
            if (control.SelectionLength == 0)
            {
                control.ContextMenu.MenuItems[0].Enabled = false;
            }
            else
            {
                control.ContextMenu.MenuItems[0].Enabled = true;
            }
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
            updateOutput.Interval = 30;
            updateOutput.Start();

            // Load Bot Configuration
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNode list = xmlDoc.SelectSingleNode("/client_settings");
                irc_conf.minimize_to_tray = Convert.ToBoolean(list["minimize_to_tray"].InnerText);
                irc_conf.auto_start = Convert.ToBoolean(list["auto_start"].InnerText);
                irc_conf.config_path = list["config_path"].InnerText;
                if (irc_conf.minimize_to_tray == true)
                {
                    MyNotifyIcon.Visible = true;
                }
                else
                {
                    MyNotifyIcon.Visible = false;
                }
                int index = 0;
                connectToolStripMenuItem.Enabled = false;
                
                bool server_started = false;
                if (!irc_conf.config_path.Trim().Equals(string.Empty))
                {
                    controller = new IRCBot.bot_controller(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + irc_conf.config_path.Trim());
                    string[] server_list = controller.list_servers();
                    foreach (string server in server_list)
                    {
                        server_started = connect(server, false);
                        if (server_started)
                        {
                            connectToolStripMenuItem.Text = "Disconnect";
                            connectToolStripMenuItem.Enabled = true;
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
            bool server_initiated = controller.init_server(server_name, manual);
            if (server_initiated)
            {
                Control control = new Control();
                if (tabControl1.Controls.Find("output_box_system", true).GetUpperBound(0) >= 0)
                {
                    control = tabControl1.Controls.Find("output_box_system", true)[0];
                    RichTextBox output_box = (RichTextBox)control;
                    output_box.Name = "output_box_" + server_name + ":system";
                    control = tabControl1.Controls.Find("tabPage1", true)[0];
                    TabPage tabpage = (TabPage)control;
                    tabpage.Name = "tabPage:" + server_name + ":__:system";
                    tabpage.Text = server_name;
                }
                else if (tabControl1.Controls.Find("output_box_" + server_name + ":system", true).GetUpperBound(0) < 0)
                {
                    RichTextBox box = new RichTextBox();
                    box.Dock = System.Windows.Forms.DockStyle.Fill;
                    box.Location = new System.Drawing.Point(3, 3);
                    box.Name = "output_box_" + server_name + ":system";
                    box.BackColor = Color.White;
                    box.ReadOnly = true;
                    box.Text = "";
                    box.HideSelection = false;
                    box.MouseClick += new System.Windows.Forms.MouseEventHandler(output_box_system_MouseClick);
                    box.ContextMenu = OutputMenu;
                    box.ContextMenu.Popup += ContextMenu_Popup;
                    TabPage tabpage = new TabPage();
                    tabpage.Controls.Add(box);
                    tabpage.Location = new System.Drawing.Point(4, 22);
                    tabpage.Name = "tabPage:" + server_name + ":__:system";
                    tabpage.Padding = new System.Windows.Forms.Padding(3);
                    tabpage.Text = server_name;
                    tabpage.UseVisualStyleBackColor = true;
                    tabControl1.Controls.Add(tabpage);
                    tabControl1.Update();
                    box.LinkClicked += new LinkClickedEventHandler(link_Click);
                }
                else
                {
                }

                button1.Visible = true;
                button1.Enabled = true;
                server_initiated = true;
            }
            return server_initiated;
        }

        private void tab_changed(object sender, EventArgs e)
        {
            string nick = "";
            string server = "";
            string chan = "";
            string chan_name = "";
            char[] charSep = new char[] { ':' };
            string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 4);
            if (tab_name.GetUpperBound(0) > 0)
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
                if (controller.bot_connected(tab_name[1]) == true)
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
            string tmp_server = "No_Server_Specified";
            string channel = "";
            char[] charSep = new char[] { ':' };
            string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
            if (tab_name.GetUpperBound(0) > 0)
            {
                Bot.bot bot = controller.get_bot_instance(tab_name[1]);
                if (bot != null)
                {
                    tmp_server = bot.Conf.Server_Name;
                    string input_tmp = input_box.Text;
                    char[] charSeparator = new char[] { ' ' };
                    string[] input = input_tmp.Split(charSeparator, 2);
                    if (input[0].StartsWith("/") && !input[0].StartsWith("//"))
                    {
                        string[] new_tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 4);
                        channel = new_tab_name[3];
                        string[] args;

                        if (input.GetUpperBound(0) > 0)
                        {
                            args = input[1].Split(' ');
                        }
                        else
                        {
                            args = null;
                        }
                        controller.run_command(bot.Conf.Server_Name, channel, input[0].TrimStart('/'), args);
                    }
                    else
                    {
                        if (input[0].StartsWith("/"))
                        {
                            input[0] = input[0].TrimStart('/');
                            input[0] = "/" + input[0];
                        }
                        if (tabControl1.SelectedIndex == 0)
                        {
                            output = tmp_server + ":" + "No channel joined. Try /join #<channel>";
                            UpdateOutput_final(output);
                        }
                        else
                        {
                            if (input.GetUpperBound(0) > 0)
                            {
                                controller.send_data(bot.Conf.Server_Name, "PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0] + " " + input[1]);
                            }
                            else
                            {
                                controller.send_data(bot.Conf.Server_Name, "PRIVMSG", tabControl1.SelectedTab.Text + " :" + input[0]);
                            }
                        }
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
                Bot.bot bot = controller.get_bot_instance(tmp_server_name);

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

                if (tabControl1.Controls.Find("output_box_" + tmp_server_name + ":system", true).GetUpperBound(0) >= 0)
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
                                if (tab[1].Equals(bot.Conf.Server_Name))
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
                            if (channel.Equals(bot.Nick))
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
                            else if (channel.Equals("nickserv", StringComparison.InvariantCultureIgnoreCase) && nickname.Equals(bot.Nick))
                            {
                                channel = "System";
                                prefix = "<---";
                                pre_color = "#C73232";
                                postfix = "";
                                post_color = "#C73232";
                                nickname = "";
                            }
                            else if (channel.Equals("chanserv", StringComparison.InvariantCultureIgnoreCase) && nickname.Equals(bot.Nick))
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
                                    if (nickname.Equals(bot.Nick))
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
                            else if (channel.Equals(bot.Nick))
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
                        else if (tmp_lines[1].Equals("352") || tmp_lines[1].Equals("315") || tmp_lines[1].Equals("321") || tmp_lines[1].Equals("322") || tmp_lines[1].Equals("323"))
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
                        if (!text.Trim().Equals(string.Empty))
                        {
                            controller.log(text, bot, channel_line, date_stamp, time_stamp);
                        }
                    }
                }
            }
        }

        private void ThreadProcSafeOutput()
        {
            string text = string.Join("", controller.get_queue());
            string[] stringSeparators = new string[] { "\r\n" };
            string[] lines = text.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x <= lines.GetUpperBound(0); x++)
            {
                UpdateOutput_final(lines[x]);
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
                box.MouseClick += new System.Windows.Forms.MouseEventHandler(output_box_system_MouseClick);
                box.ContextMenu = OutputMenu;
                box.ContextMenu.Popup += ContextMenu_Popup;
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

        public void replace_tabs(string old_server_name, string new_server_name)
        {
            // disable tab change events (screws up updating)
            this.tabControl1.SelectedIndexChanged -= new System.EventHandler(this.tabControl1_MouseClick);
            this.tabControl1.MouseClick -= new System.Windows.Forms.MouseEventHandler(this.tabControl1_MouseClick);
            tabControl1.SelectedIndexChanged -= tab_changed;

            foreach (Control tab in tabControl1.Controls)
            {
                char[] charSep = new char[] { ':' };
                string[] channel = tab.Name.Split(charSep);
                if (channel[1].Equals(old_server_name))
                {
                    tab.Name = "tabPage:" + new_server_name + ":" + channel[2] + ":" + channel[3];
                    foreach (Control option_box in tab.Controls)
                    {
                        string[] parts = option_box.Name.Split(charSep);
                        string[] sec_parts = parts[0].Split('_');
                        if (sec_parts[sec_parts.GetUpperBound(0)].Equals(old_server_name))
                        {
                            string new_box = "";
                            for (int x = 0; x < sec_parts.GetUpperBound(0); x++ )
                            {
                                new_box += sec_parts[x] + "_";
                            }
                            option_box.Name = new_box + new_server_name + ":" + parts[1];
                        }
                    }
                }
            }

            // enable tab change events again.
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_MouseClick);
            this.tabControl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.tabControl1_MouseClick);
            tabControl1.SelectedIndexChanged += tab_changed;
        }

        private static void Sort<T>(T[][] data, int col)
        {
            Comparer<T> comparer = Comparer<T>.Default;
            Array.Sort<T[]>(data, (x, y) => comparer.Compare(x[col], y[col]));
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Bot.bot bot_instance in controller.bot_instances)
            {
                controller.stop_bot(bot_instance.Conf.Server_Name);
            }
            updateOutput.Stop();
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

        private void button1_Click(object sender, EventArgs e)
        {
            string tmp_server = "No_Server_Specified";
            char[] charSep = new char[] { ':' };
            string[] tab_name = tabControl1.SelectedTab.Name.Split(charSep, 3);
            string name = tabControl1.SelectedTab.Text;
            int selected_tab = tabControl1.SelectedIndex;
            if (tab_name.GetUpperBound(0) > 1)
            {
                if (name.StartsWith("#") == true && controller.bot_connected(tab_name[1]) == true)
                {
                    controller.run_command(tab_name[1], name, "part", new string[] { name });
                }
            }
            tmp_server = tab_name[1];
            if (name.Equals(tmp_server))
            {
                bool ended = false;

                while (!ended)
                {
                    ended = controller.stop_bot(tmp_server);
                }

                for (int x = tabControl1.SelectedIndex; x < tabControl1.TabPages.Count; x++)
                {
                    string[] new_tab_name = tabControl1.TabPages[x].Name.Split(charSep, 3);
                    if (new_tab_name[1].Equals(tmp_server))
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

                if (tabControl1.TabPages.Count == 1)
                {
                    string[] names = tabControl1.TabPages[0].Name.Split(charSep);
                    if (names[1].Equals(tmp_server))
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
                }
                controller.remove_bot_instance(tmp_server);
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
            char[] charSep = new char[] { ':' };
            string[] tab_name = tabControl1.SelectedTab.Name.ToString().Split(charSep, 3);
            if (connectToolStripMenuItem.Text.Equals("Disconnect"))
            {
                bool closed = controller.stop_bot(tab_name[1]);
                if (closed)
                {
                    connectToolStripMenuItem.Text = "Connect";
                    send_button.Enabled = false;
                    input_box.Enabled = false;
                }
            }
            else
            {
                bool started = controller.start_bot(tab_name[1]);
                if (started)
                {
                    connectToolStripMenuItem.Text = "Disconnect";
                    send_button.Enabled = true;
                    input_box.Enabled = true;
                }
            }
        }

        private void Interface_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (controller.bot_instances != null)
            {
                foreach (Bot.bot bot_instance in controller.bot_instances)
                {
                    controller.stop_bot(bot_instance.Conf.Server_Name);
                }
            }
            MyNotifyIcon.Visible = false;
            updateOutput.Stop();
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
        public string config_path { get; set; }
        public bool auto_start { get; set; }
        public bool minimize_to_tray { get; set; }
    }
}
