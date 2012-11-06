using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using Microsoft.Win32;

namespace IRCBot
{
    public partial class configuration : Form
    {
        private Interface m_parent;
        public configuration(Interface frmctrl)
        {
            InitializeComponent();
            tabControl2.DrawItem += new DrawItemEventHandler(tabControl2_DrawItem);
            m_parent = frmctrl;

            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(m_parent.cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
            }
            else
            {
                XmlNode node = xmlDoc.CreateNode(XmlNodeType.Element, "connection_settings", null);
                XmlNode nodeCommand = xmlDoc.CreateElement("command_prefix");
                nodeCommand.InnerText = ".";
                node.AppendChild(nodeCommand);
                XmlNode nodeKeep = xmlDoc.CreateElement("keep_logs");
                nodeKeep.InnerText = "True";
                node.AppendChild(nodeKeep);
                XmlNode nodeLogs = xmlDoc.CreateElement("logs_path");
                nodeLogs.InnerText = m_parent.cur_dir + "\\logs\\";
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
                XmlNode nodeLevels;
                nodeLevels = xmlDoc.CreateElement("user_level");
                nodeLevels.InnerText = "1";
                node.AppendChild(nodeLevels);
                nodeLevels = xmlDoc.CreateElement("voice_level");
                nodeLevels.InnerText = "3";
                node.AppendChild(nodeLevels);
                nodeLevels = xmlDoc.CreateElement("hop_level");
                nodeLevels.InnerText = "6";
                node.AppendChild(nodeLevels);
                nodeLevels = xmlDoc.CreateElement("op_level");
                nodeLevels.InnerText = "7";
                node.AppendChild(nodeLevels);
                nodeLevels = xmlDoc.CreateElement("sop_level");
                nodeLevels.InnerText = "8";
                node.AppendChild(nodeLevels);
                nodeLevels = xmlDoc.CreateElement("founder_level");
                nodeLevels.InnerText = "9";
                node.AppendChild(nodeLevels);
                nodeLevels = xmlDoc.CreateElement("owner_level");
                nodeLevels.InnerText = "10";
                node.AppendChild(nodeLevels);
                xmlDoc.AppendChild(node);
                xmlDoc.Save(m_parent.cur_dir + "\\config\\config.xml");
                xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
            }
            XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/connection_settings");

            command_prefix_box.Text = list["command_prefix"].InnerText;
            spam_count_box.Text = list["spam_count"].InnerText;
            spam_threshold_box.Text = list["spam_threshold"].InnerText;
            spam_timeout_box.Text = list["spam_timeout"].InnerText;
            max_message_length_box.Text = list["max_message_length"].InnerText;
            user_level_box.Text = list["user_level"].InnerText;
            voice_level_box.Text = list["voice_level"].InnerText;
            hop_level_box.Text = list["hop_level"].InnerText;
            op_level_box.Text = list["op_level"].InnerText;
            sop_level_box.Text = list["sop_level"].InnerText;
            founder_level_box.Text = list["founder_level"].InnerText;
            owner_level_box.Text = list["owner_level"].InnerText;
            if (list["keep_logs"].InnerText == "True")
            {
                keep_logs_box.Checked = true;
            }
            else
            {
                keep_logs_box.Checked = false;
            }
            log_folder_box.Text = list["logs_path"].InnerText;
            if (list["start_with_windows"].InnerText == "True")
            {
                windows_start_box.Checked = true;
            }
            else
            {
                windows_start_box.Checked = false;
            }

            XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/connection_settings/server_list/server");
            foreach (XmlNode xn in xnList)
            {
                string server_name = xn["server_name"].InnerText;
                server_list.Items.Add(server_name);
            }

            server_list.SelectedIndexChanged += server_changed;

            xnList = xmlDoc.SelectNodes("/bot_settings/modules/module");
            foreach (XmlNode xn in xnList)
            {
                int element_num = 0;
                String module_name = xn["name"].InnerText;
                System.Windows.Forms.TabPage tabPage = new System.Windows.Forms.TabPage();

                tabPage.Location = new System.Drawing.Point(4, 22);
                tabPage.Name = module_name;
                tabPage.Padding = new System.Windows.Forms.Padding(3);
                tabPage.Size = new System.Drawing.Size(378, 293);
                tabPage.TabIndex = 0;
                tabPage.Text = module_name;
                tabPage.UseVisualStyleBackColor = true;
                tabControl2.Controls.Add(tabPage);

                bool checkbox_checked = false;
                if (xn["enabled"].InnerText == "True")
                {
                    checkbox_checked = true;
                }
                CheckBox myCheckboxEnabled = new CheckBox();
                myCheckboxEnabled.Name = "checkBox_" + module_name + "_enabled";
                myCheckboxEnabled.Checked = checkbox_checked;
                myCheckboxEnabled.Left = 200;
                myCheckboxEnabled.Top = 10 + (element_num * 25);
                myCheckboxEnabled.TabIndex = element_num + 1;
                myCheckboxEnabled.TabStop = true;
                tabPage.Controls.Add(myCheckboxEnabled);

                Label myLabelEnabled = new Label();
                myLabelEnabled.Name = "label_" + module_name + "_enabled";
                myLabelEnabled.Text = "Enabled";
                myLabelEnabled.Width = 180;
                myLabelEnabled.Height = 13;
                myLabelEnabled.Left = 8;
                myLabelEnabled.Top = 13 + (element_num * 25);
                tabPage.Controls.Add(myLabelEnabled);

                element_num++;
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
                                    TextBox myTextBox = new TextBox();
                                    myTextBox.Name = "textBox_" + module_name + "_" + options.Name + "_" + element_num.ToString();
                                    myTextBox.Text = options["value"].InnerText;
                                    myTextBox.TextAlign = HorizontalAlignment.Left;
                                    myTextBox.Width = 170;
                                    myTextBox.Height = 20;
                                    myTextBox.Left = 200;
                                    myTextBox.Top = 10 + (element_num * 25);
                                    myTextBox.TabIndex = element_num + 1;
                                    myTextBox.TabStop = true;
                                    tabPage.Controls.Add(myTextBox);

                                    Label myLabelText = new Label();
                                    myLabelText.Name = "label_" + module_name + "_" + options.Name + "_" + element_num.ToString();
                                    myLabelText.Text = options["label"].InnerText;
                                    myLabelText.Width = 180;
                                    myLabelText.Height = 13;
                                    myLabelText.Left = 8;
                                    myLabelText.Top = 13 + (element_num * 25);
                                    tabPage.Controls.Add(myLabelText);

                                    element_num++;
                                    break;
                                case "checkbox":
                                    checkbox_checked = false;
                                    if (options["checked"].InnerText == "True")
                                    {
                                        checkbox_checked = true;
                                    }
                                    CheckBox myCheckbox = new CheckBox();
                                    myCheckbox.Name = "checkBox_" + module_name + "_" + options.Name + "_" + element_num.ToString();
                                    myCheckbox.Checked = checkbox_checked;
                                    myCheckbox.Left = 200;
                                    myCheckbox.Top = 10 + (element_num * 25);
                                    myCheckbox.TabIndex = element_num + 1;
                                    myCheckbox.TabStop = true;
                                    tabPage.Controls.Add(myCheckbox);

                                    Label myLabelCheck = new Label();
                                    myLabelCheck.Name = "label_" + module_name + "_" + options.Name + "_" + element_num.ToString();
                                    myLabelCheck.Text = options["label"].InnerText;
                                    myLabelCheck.Width = 180;
                                    myLabelCheck.Height = 13;
                                    myLabelCheck.Left = 8;
                                    myLabelCheck.Top = 13 + (element_num * 25);
                                    tabPage.Controls.Add(myLabelCheck);

                                    element_num++;
                                    break;
                            }
                        }
                    }
                }
            }

            string list_file = m_parent.cur_dir + "\\config\\help.txt";
            if (File.Exists(list_file))
            {
                string[] file = System.IO.File.ReadAllLines(list_file);
                foreach (string file_line in file)
                {
                    string[] split = file_line.Split(':');
                    command_list.Items.Add(split[2]);
                }
            }
            command_list.SelectedIndexChanged += command_list_change;
            command_list.Sorted = true;
        }

        private void server_changed(Object sender, EventArgs e)
        {
            if (server_list.SelectedItem != null)
            {
                bool connected = m_parent.bot_connected(server_list.SelectedItem.ToString());
                if (connected == true)
                {
                    connect_button.Text = "Disconnect";
                }
                else
                {
                    connect_button.Text = "Connect";
                }
            }
        }

        private void command_list_change(Object sender, EventArgs e)
        {
            string list_file = m_parent.cur_dir + "\\config\\help.txt";
            if (File.Exists(list_file))
            {
                string[] file = System.IO.File.ReadAllLines(list_file);
                foreach (string file_line in file)
                {
                    string[] split = file_line.Split(':');
                    if (split.GetUpperBound(0) > 3)
                    {
                        if (split[2].Equals(command_list.SelectedItem))
                        {
                            command_name.Text = split[0];
                            command_arguments.Text = split[3];
                            command_description.Text = split[4];
                            command_access_level.Text = split[1];
                            break;
                        }
                    }
                }
            }
        }

        private void tabControl2_DrawItem(Object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Brush _textBrush;

            // Get the item from the collection.
            TabPage _tabPage = tabControl2.TabPages[e.Index];

            // Get the real bounds for the tab rectangle.
            Rectangle _tabBounds = tabControl2.GetTabRect(e.Index);

            if (e.State == DrawItemState.Selected)
            {

                // Draw a different background color, and don't paint a focus rectangle.
                _textBrush = new SolidBrush(Color.Black);
                g.FillRectangle(Brushes.Gray, e.Bounds);
            }
            else
            {
                _textBrush = new System.Drawing.SolidBrush(e.ForeColor);
                e.DrawBackground();
            }

            // Use our own font.
            Font _tabFont = new Font("Arial", (float)10.0, FontStyle.Bold, GraphicsUnit.Pixel);

            // Draw string. Center the text.
            StringFormat _stringFlags = new StringFormat();
            _stringFlags.Alignment = StringAlignment.Center;
            _stringFlags.LineAlignment = StringAlignment.Center;
            g.DrawString(_tabPage.Text, _tabFont, _textBrush, _tabBounds, new StringFormat(_stringFlags));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
            XmlNode node = xmlDoc.SelectSingleNode("/bot_settings/connection_settings");
            node["command_prefix"].InnerText = command_prefix_box.Text;
            node["keep_logs"].InnerText = keep_logs_box.Checked.ToString();
            node["logs_path"].InnerText = log_folder_box.Text;
            node["start_with_windows"].InnerText = windows_start_box.Checked.ToString();
            node["spam_count"].InnerText = spam_count_box.Text;
            node["spam_threshold"].InnerText = spam_threshold_box.Text;
            node["spam_timeout"].InnerText = spam_timeout_box.Text;
            node["max_message_length"].InnerText = max_message_length_box.Text;
            node["user_level"].InnerText = user_level_box.Text;
            node["voice_level"].InnerText = voice_level_box.Text;
            node["hop_level"].InnerText = hop_level_box.Text;
            node["op_level"].InnerText = op_level_box.Text;
            node["sop_level"].InnerText = sop_level_box.Text;
            node["founder_level"].InnerText = founder_level_box.Text;
            node["owner_level"].InnerText = owner_level_box.Text;

            XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/modules/module");
            foreach (XmlNode xn in xnList)
            {
                int element_num = 1;
                String module_name = xn["name"].InnerText;

                CheckBox enabled = (CheckBox)tabControl2.Controls.Find("checkBox_" + module_name + "_enabled", true)[0];
                xn["enabled"].InnerText = enabled.Checked.ToString();

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
                                    if (tabControl2.Controls.Find("textBox_" + options.Name + "_" + element_num.ToString(), true) != null)
                                    {
                                        TextBox textBox = (TextBox)tabControl2.Controls.Find("textBox_" + module_name + "_" + options.Name + "_" + element_num.ToString(), true)[0];
                                        options["value"].InnerText = textBox.Text;

                                        element_num++;
                                    }
                                    break;
                                case "checkbox":
                                    if (tabControl2.Controls.Find("checkBox_" + options.Name + "_" + element_num.ToString(), true) != null)
                                    {
                                        CheckBox checkBox = (CheckBox)tabControl2.Controls.Find("checkBox_" + module_name + "_" + options.Name + "_" + element_num.ToString(), true)[0];
                                        options["checked"].InnerText = checkBox.Checked.ToString();

                                        element_num++;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            xmlDoc.Save(m_parent.cur_dir + "\\config\\config.xml");

            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (windows_start_box.Checked.ToString() == "True")
            {
                rkApp.SetValue("IRCBot", Application.ExecutablePath.ToString());
            }
            else
            {
                rkApp.DeleteValue("IRCBot", false);
            }

            m_parent.update_conf();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void browse_button_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (Directory.Exists(folderBrowserDialog1.SelectedPath))
                {
                    log_folder_box.Text = folderBrowserDialog1.SelectedPath;
                }
                else
                {
                    DialogResult result = MessageBox.Show("The folder does not exist.  Would you like to create it?", "Folder does not Exist", MessageBoxButtons.YesNo);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        Directory.CreateDirectory(folderBrowserDialog1.SelectedPath);
                        log_folder_box.Text = folderBrowserDialog1.SelectedPath;
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string list_file = m_parent.cur_dir + "\\config\\help.txt";
            List<string> new_file = new List<string>();
            if (File.Exists(list_file))
            {
                string msg = "";
                string[] file = System.IO.File.ReadAllLines(list_file);
                foreach (string file_line in file)
                {
                    string[] split = file_line.Split(':');
                    if (split[2].Equals(command_list.SelectedItem))
                    {
                        msg = command_name.Text + ":" + command_access_level.Text + ":" + command_list.SelectedItem + ":" + command_arguments.Text + ":" + command_description.Text;
                    }
                    else
                    {
                        msg = file_line;
                    }
                    new_file.Add(msg);
                    msg = "";
                }
            }
            System.IO.File.WriteAllLines(list_file, new_file);
        }

        private void add_server_button_Click(object sender, EventArgs e)
        {
            server_list.SelectedIndexChanged -= server_changed;
            add_server add_server = new add_server(m_parent, this);
            add_server.ShowDialog();
            server_list.SelectedIndexChanged += server_changed;
        }

        private void edit_server_button_Click(object sender, EventArgs e)
        {
            server_list.SelectedIndexChanged -= server_changed;
            edit_server edit_server = new edit_server(m_parent, server_list.SelectedItem.ToString());
            edit_server.ShowDialog();
            server_list.SelectedIndexChanged += server_changed;
        }

        private void delete_server_button_Click(object sender, EventArgs e)
        {
            server_list.SelectedIndexChanged -= server_changed;
            string server_name = server_list.SelectedItem.ToString();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
            XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/connection_settings/server_list/server");
            foreach (XmlNode xn in ServerxnList)
            {
                string tmp_server = xn["server_name"].InnerText;
                if (tmp_server.Equals(server_name))
                {
                    xn.ParentNode.RemoveChild(xn);
                    break;
                }
            }
            xmlDoc.Save(m_parent.cur_dir + "\\config\\config.xml");
            server_list.Items.RemoveAt(server_list.SelectedIndex);
            m_parent.update_conf();
            server_list.SelectedIndexChanged += server_changed;
            bool connected = m_parent.bot_connected(server_name);
            if (connected == true)
            {
                bool disconnected = m_parent.end_connection(server_name);
            }
        }

        private void connect_button_Click(object sender, EventArgs e)
        {
            if (connect_button.Text.Equals("Connect"))
            {
                connect_button.Text = "Connecting...";
                bool connected = m_parent.start_connection(server_list.SelectedItem.ToString());
                if (connected == true)
                {
                    connect_button.Text = "Disconnect";
                }
                else
                {
                    MessageBox.Show("Could not connect");
                    connect_button.Text = "Connect";
                }
            }
            else
            {
                connect_button.Text = "Disconnecting...";
                bool disconnected = m_parent.end_connection(server_list.SelectedItem.ToString());
                if (disconnected == true)
                {
                    connect_button.Text = "Connect";
                }
                else
                {
                    MessageBox.Show("Could not disconnect");
                    connect_button.Text = "Disconnect";
                }
            }
        }

        private void server_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            edit_server_button.Enabled = true;
            delete_server_button.Enabled = true;
            connect_button.Enabled = true;
        }

        public void add_to_list(string server_name)
        {
            server_list.Items.Add(server_name);
        }
    }
}
