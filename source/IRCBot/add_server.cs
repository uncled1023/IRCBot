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
    public partial class add_server : Form
    {
        private Interface m_parent;
        private configuration config;
        public add_server(Interface frmctrl, configuration tmp_config)
        {
            InitializeComponent();
            m_parent = frmctrl;
            config = tmp_config;
        }

        private void add_server_button_Click(object sender, EventArgs e)
        {
            if (server_name_box.Text == "")
            {
                MessageBox.Show("A Server must be specified");
            }
            else if (port_box.Text == "")
            {
                MessageBox.Show("A port number must be specified");
            }
            else if (bot_name_box.Text == "")
            {
                MessageBox.Show("A name must be specified");
            }
            else if (bot_nick_box.Text == "")
            {
                MessageBox.Show("A nickname must be specified");
            }
            else
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
                XmlNode Serverxn = xmlDoc.SelectSingleNode("/bot_settings/connection_settings/server_list");
                XmlNode node = xmlDoc.CreateNode(XmlNodeType.Element, "server", null);
                XmlNode nodeInner;
                nodeInner = xmlDoc.CreateElement("server_name");
                nodeInner.InnerText = server_name_box.Text;
                node.AppendChild(nodeInner);
                nodeInner = xmlDoc.CreateElement("port");
                nodeInner.InnerText = port_box.Text;
                node.AppendChild(nodeInner);
                nodeInner = xmlDoc.CreateElement("chan_list");
                nodeInner.InnerText = channels_box.Text;
                node.AppendChild(nodeInner);
                nodeInner = xmlDoc.CreateElement("name");
                nodeInner.InnerText = bot_name_box.Text;
                node.AppendChild(nodeInner);
                nodeInner = xmlDoc.CreateElement("nick");
                nodeInner.InnerText = bot_nick_box.Text;
                node.AppendChild(nodeInner);
                nodeInner = xmlDoc.CreateElement("password");
                nodeInner.InnerText = password_box.Text;
                node.AppendChild(nodeInner);
                nodeInner = xmlDoc.CreateElement("email");
                nodeInner.InnerText = email_box.Text;
                node.AppendChild(nodeInner);
                nodeInner = xmlDoc.CreateElement("owner");
                nodeInner.InnerText = owner_nicks_box.Text;
                node.AppendChild(nodeInner);
                Serverxn.AppendChild(node);
                xmlDoc.Save(m_parent.cur_dir + "\\config\\config.xml");
                m_parent.update_conf();
                config.add_to_list(server_name_box.Text);
                this.Close();
            }
        }

        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
