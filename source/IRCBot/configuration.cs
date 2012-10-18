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

            m_parent = frmctrl;

            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(m_parent.cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
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
                xmlDoc.AppendChild(node);
                xmlDoc.Save(m_parent.cur_dir + "\\config\\config.xml");
                xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
            }
            XmlNode list = xmlDoc.SelectSingleNode("connection_settings");

            bot_name_box.Text = list["name"].InnerText;
            bot_nick_box.Text = list["nick"].InnerText;
            password_box.Text = list["password"].InnerText;
            email_box.Text = list["email"].InnerText;
            owner_nicks_box.Text = list["owner"].InnerText;
            port_box.Text = list["port"].InnerText;
            server_name_box.Text = list["server"].InnerText;
            channels_box.Text = list["chan_list"].InnerText;
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

            XmlNodeList xnList = xmlDoc.SelectNodes("/modules/module");
            foreach (XmlNode xn in xnList)
            {
                String module_name = xn["name"].InnerText;
                module_list.Items.Add(module_name);
            }
            module_list.SelectedValueChanged += new EventHandler(this.module_changed);
        }

        private void module_changed(object sender, EventArgs e)
        {
            ComboBox selected_control = (ComboBox)sender;
            string value = selected_control.Text.ToString();
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(m_parent.cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
                XmlNodeList xnList = xmlDoc.SelectNodes("/modules/module");
                foreach (XmlNode xn in xnList)
                {
                    String module_name = xn["name"].InnerText;
                    if (value.Equals(module_name))
                    {
                    }
                }
            }
            else
            {
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (bot_nick_box.Equals(""))
            {
                MessageBox.Show("Your Bot must have a nickname");
            }
            else if (server_name_box.Equals(""))
            {
                MessageBox.Show("You must specify a Server Address");
            }
            else if (port_box.Equals(""))
            {
                MessageBox.Show("You must specify a port number");
            }
            else
            {
                XmlDocument xmlDoc2 = new XmlDocument();
                xmlDoc2.Load(m_parent.cur_dir + "\\config\\config.xml");
                XmlNodeList xnList = xmlDoc2.SelectNodes("connection_settings");
                foreach (XmlNode xn in xnList)
                {
                    xn.RemoveAll();
                }
                xmlDoc2.Save(m_parent.cur_dir + "\\config\\config.xml");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
                XmlNode node = xmlDoc.SelectSingleNode("connection_settings");
                XmlNode nodeName = xmlDoc.CreateElement("name");
                nodeName.InnerText = bot_name_box.Text;
                node.AppendChild(nodeName);
                XmlNode nodeNick = xmlDoc.CreateElement("nick");
                nodeNick.InnerText = bot_nick_box.Text;
                node.AppendChild(nodeNick);
                XmlNode nodePass = xmlDoc.CreateElement("password");
                nodePass.InnerText = password_box.Text;
                node.AppendChild(nodePass);
                XmlNode nodeEmail = xmlDoc.CreateElement("email");
                nodeEmail.InnerText = email_box.Text;
                node.AppendChild(nodeEmail);
                XmlNode nodeOwner = xmlDoc.CreateElement("owner");
                nodeOwner.InnerText = owner_nicks_box.Text;
                node.AppendChild(nodeOwner);
                XmlNode nodePort = xmlDoc.CreateElement("port");
                nodePort.InnerText = port_box.Text;
                node.AppendChild(nodePort);
                XmlNode nodeServer = xmlDoc.CreateElement("server");
                nodeServer.InnerText = server_name_box.Text;
                node.AppendChild(nodeServer);
                XmlNode nodeChan = xmlDoc.CreateElement("chan_list");
                nodeChan.InnerText = channels_box.Text;
                node.AppendChild(nodeChan);
                XmlNode nodeCommand = xmlDoc.CreateElement("command_prefix");
                nodeCommand.InnerText = command_prefix_box.Text;
                node.AppendChild(nodeCommand);
                XmlNode nodeKeep = xmlDoc.CreateElement("keep_logs");
                nodeKeep.InnerText = keep_logs_box.Checked.ToString();
                node.AppendChild(nodeKeep);
                XmlNode nodeLogs = xmlDoc.CreateElement("logs_path");
                nodeLogs.InnerText = log_folder_box.Text;
                node.AppendChild(nodeLogs);
                XmlNode nodeStart = xmlDoc.CreateElement("start_with_windows");
                nodeStart.InnerText = windows_start_box.Checked.ToString();
                node.AppendChild(nodeStart);
                XmlNode nodeSpamCount = xmlDoc.CreateElement("spam_count");
                nodeSpamCount.InnerText = spam_count_box.Text;
                node.AppendChild(nodeSpamCount);
                XmlNode nodeSpamThreshold = xmlDoc.CreateElement("spam_threshold");
                nodeSpamThreshold.InnerText = spam_threshold_box.Text;
                node.AppendChild(nodeSpamThreshold);
                XmlNode nodeSpamTime = xmlDoc.CreateElement("spam_timeout");
                nodeSpamTime.InnerText = spam_timeout_box.Text;
                node.AppendChild(nodeSpamTime);
                XmlNode nodeSpamMaxMsgLength = xmlDoc.CreateElement("max_message_length");
                nodeSpamMaxMsgLength.InnerText = max_message_length_box.Text;
                node.AppendChild(nodeSpamMaxMsgLength);
                xmlDoc.AppendChild(node);
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
    }
}
