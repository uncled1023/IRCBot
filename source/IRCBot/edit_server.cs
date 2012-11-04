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
    public partial class edit_server : Form
    {
        private Interface m_parent;
        private string server_name;
        public edit_server(Interface frmctrl, string tmp_server_name)
        {
            InitializeComponent();
            m_parent = frmctrl;
            server_name = tmp_server_name;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(m_parent.cur_dir + "\\config\\config.xml");
            XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/connection_settings/server_list/server");
            foreach (XmlNode xn in ServerxnList)
            {
                string tmp_server = xn["server_name"].InnerText;
                if (tmp_server.Equals(server_name))
                {
                    bot_name_box.Text = xn["name"].InnerText;
                    bot_nick_box.Text = xn["nick"].InnerText;
                    password_box.Text = xn["password"].InnerText;
                    email_box.Text = xn["email"].InnerText;
                    owner_nicks_box.Text = xn["owner"].InnerText;
                    port_box.Text = xn["port"].InnerText;
                    server_name_box.Text = xn["server_name"].InnerText;
                    channels_box.Text = xn["chan_list"].InnerText;
                    break;
                }
            }
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
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/connection_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(server_name))
                    {
                        xn["name"].InnerText = bot_name_box.Text;
                        xn["nick"].InnerText = bot_nick_box.Text;
                        xn["password"].InnerText = password_box.Text;
                        xn["email"].InnerText = email_box.Text;
                        xn["owner"].InnerText = owner_nicks_box.Text;
                        xn["port"].InnerText = port_box.Text;
                        xn["server_name"].InnerText = server_name_box.Text;
                        xn["chan_list"].InnerText = channels_box.Text;
                        break;
                    }
                }
                xmlDoc.Save(m_parent.cur_dir + "\\config\\config.xml");
                this.Close();
            }
        }

        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
