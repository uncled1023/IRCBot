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
using System.Xml.Linq;
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
            m_parent = frmctrl;

            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
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
                nodeLogs.InnerText = m_parent.cur_dir + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar + "";
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
                xmlDoc.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            }
            XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/global_settings");

            command_prefix_box.Text = list["command_prefix"].InnerText;
            spam_count_box.Text = list["spam_count"].InnerText;
            spam_threshold_box.Text = list["spam_threshold"].InnerText;
            spam_timeout_box.Text = list["spam_timeout"].InnerText;
            max_message_length_box.Text = list["max_message_length"].InnerText;
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
            if (list["minimize_to_tray"].InnerText == "True")
            {
                minimize_to_tray.Checked = true;
            }
            else
            {
                minimize_to_tray.Checked = false;
            }

            XmlNodeList xnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
            foreach (XmlNode xn in xnList)
            {
                string server_name = xn["server_name"].InnerText;
                server_list.Items.Add(server_name);
            }

            server_list.SelectedIndexChanged += server_changed;
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

        private void button2_Click(object sender, EventArgs e)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            XmlNode node = xmlDoc.SelectSingleNode("/bot_settings/global_settings");
            node["command_prefix"].InnerText = command_prefix_box.Text;
            node["keep_logs"].InnerText = keep_logs_box.Checked.ToString();
            node["logs_path"].InnerText = log_folder_box.Text;
            node["start_with_windows"].InnerText = windows_start_box.Checked.ToString();
            node["minimize_to_tray"].InnerText = minimize_to_tray.Checked.ToString();
            node["spam_count"].InnerText = spam_count_box.Text;
            node["spam_threshold"].InnerText = spam_threshold_box.Text;
            node["spam_timeout"].InnerText = spam_timeout_box.Text;
            node["max_message_length"].InnerText = max_message_length_box.Text;

            xmlDoc.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");

            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE" + Path.DirectorySeparatorChar + "Microsoft" + Path.DirectorySeparatorChar + "Windows" + Path.DirectorySeparatorChar + "CurrentVersion" + Path.DirectorySeparatorChar + "Run", true);
            if (windows_start_box.Checked.ToString() == "True")
            {
                rkApp.SetValue("IRCBot", "\"" + Application.ExecutablePath.ToString() + "\"");
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
            xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
            foreach (XmlNode xn in ServerxnList)
            {
                string tmp_server = xn["server_name"].InnerText;
                if (tmp_server.Equals(server_name))
                {
                    xn.ParentNode.RemoveChild(xn);
                    Directory.Delete(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + xn["server_folder"].InnerText, true);
                    break;
                }
            }
            xmlDoc.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
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
            if (server_list.SelectedItem != null)
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
