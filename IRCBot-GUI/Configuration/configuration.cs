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
using IWshRuntimeLibrary;

namespace IRCBot_GUI
{
    public partial class configuration : Form
    {
        private Interface m_parent;
        public configuration(Interface frmctrl)
        {
            m_parent = frmctrl;
            this.Icon = new Icon(m_parent.GetType(), "Bot.ico");
            InitializeComponent();

            XmlDocument xmlDoc = new XmlDocument();
            if (System.IO.File.Exists(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml"))
            {
                xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            }
            else
            {
                XmlNode node = xmlDoc.CreateNode(XmlNodeType.Element, "global_settings", null);
                XmlNode nodeConfig = xmlDoc.CreateElement("config_path");
                nodeConfig.InnerText = "servers.xml";
                node.AppendChild(nodeConfig);
                XmlNode nodeStart = xmlDoc.CreateElement("auto_start");
                nodeStart.InnerText = "False";
                node.AppendChild(nodeStart);
                XmlNode nodeTray = xmlDoc.CreateElement("minimize_to_tray");
                nodeTray.InnerText = "False";
                node.AppendChild(nodeTray);
                xmlDoc.AppendChild(node);
                xmlDoc.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            }
            XmlNode list = xmlDoc.SelectSingleNode("/client_settings");

            if (list["auto_start"].InnerText == "True")
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
            server_config_box.Text = list["config_path"].InnerText;

            m_parent.controller.list_servers();
            foreach (string server in m_parent.controller.list_servers())
            {
                server_list.Items.Add(server);
            }

            server_list.SelectedIndexChanged += server_changed;
        }

        private void server_changed(Object sender, EventArgs e)
        {
            if (server_list.SelectedItem != null)
            {
                bool connected = m_parent.controller.bot_connected(server_list.SelectedItem.ToString());
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
            XmlNode node = xmlDoc.SelectSingleNode("/client_settings");
            node["config_path"].InnerText = server_config_box.Text;
            node["auto_start"].InnerText = windows_start_box.Checked.ToString();
            node["minimize_to_tray"].InnerText = minimize_to_tray.Checked.ToString();

            m_parent.irc_conf.minimize_to_tray = Convert.ToBoolean(node["minimize_to_tray"].InnerText);
            m_parent.irc_conf.auto_start = Convert.ToBoolean(node["auto_start"].InnerText);
            m_parent.irc_conf.config_path = node["config_path"].InnerText;
            xmlDoc.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            
            string startup_loc = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (windows_start_box.Checked.ToString() == "True")
            {
                if (!System.IO.File.Exists(startup_loc + "\\IRCBot.lnk"))
                {
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(startup_loc + "\\IRCBot.lnk");
                    shortcut.Description = "IRCBot";
                    shortcut.WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                    shortcut.IconLocation = System.Reflection.Assembly.GetExecutingAssembly().Location + ", 0";
                    shortcut.TargetPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    shortcut.Save();
                }
            }
            else
            {
                if (System.IO.File.Exists(startup_loc + "\\IRCBot.lnk"))
                {
                    System.IO.File.Delete(startup_loc + "\\IRCBot.lnk");
                }
            }
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void add_server_button_Click(object sender, EventArgs e)
        {
            server_list.SelectedIndexChanged -= server_changed;
            server_config add_server = new server_config(m_parent, this, null, true);
            add_server.ShowDialog();
            server_list.SelectedIndexChanged += server_changed;
        }

        private void edit_server_button_Click(object sender, EventArgs e)
        {
            server_list.SelectedIndexChanged -= server_changed;
            server_config edit_server = new server_config(m_parent, this, server_list.SelectedItem.ToString(), false);
            edit_server.ShowDialog();
            server_list.SelectedIndexChanged += server_changed;
        }

        private void delete_server_button_Click(object sender, EventArgs e)
        {
            server_list.SelectedIndexChanged -= server_changed;
            string server_name = server_list.SelectedItem.ToString();
            m_parent.controller.delete_server_xml(server_name);
            server_list.Items.RemoveAt(server_list.SelectedIndex);
            server_list.SelectedIndexChanged += server_changed;
            edit_server_button.Enabled = false;
            delete_server_button.Enabled = false;
            connect_button.Enabled = false;
        }

        private void connect_button_Click(object sender, EventArgs e)
        {
            if (server_list.SelectedItem != null)
            {
                if (connect_button.Text.Equals("Connect"))
                {
                    connect_button.Text = "Connecting...";
                    bool connected = false;
                    Bot.bot bot = m_parent.controller.get_bot_instance(server_list.SelectedItem.ToString());
                    if (bot == null)
                    {
                        connected = m_parent.connect(server_list.SelectedItem.ToString(), true);
                    }
                    else
                    {
                        connected = m_parent.controller.start_bot(server_list.SelectedItem.ToString());
                    }
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
                    bool disconnected = m_parent.controller.stop_bot(server_list.SelectedItem.ToString());
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

        public void del_from_list(string old_server_name, string new_server_name)
        {
            server_list.Items.Remove(old_server_name);
            server_list.Items.Add(new_server_name);
        }
    }
}
