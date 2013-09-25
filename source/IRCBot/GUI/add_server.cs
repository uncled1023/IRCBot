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
        private string server_name;
        private configuration old_configeration;
        private XmlDocument xmlDocModules = new XmlDocument();
        public add_server(Interface frmctrl, configuration config)
        {
            InitializeComponent();
            old_configeration = config;
            m_parent = frmctrl;
            user_level_box.Text = "1";
            voice_level_box.Text = "3";
            hop_level_box.Text = "6";
            op_level_box.Text = "7";
            sop_level_box.Text = "8";
            founder_level_box.Text = "9";
            owner_level_box.Text = "10";
            xmlDocModules.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + "Default" + Path.DirectorySeparatorChar + "modules.xml");
            XmlNodeList xnList = xmlDocModules.SelectNodes("/modules/module");
            foreach (XmlNode xnModules in xnList)
            {
                String module_name = xnModules["name"].InnerText;
                module_list.Items.Add(module_name);
                // Add commands to command list
                XmlNodeList optionList = xnModules.ChildNodes;
                foreach (XmlNode option in optionList)
                {
                    if (option.Name.Equals("commands"))
                    {
                        XmlNodeList Options = option.ChildNodes;
                        foreach (XmlNode options in Options)
                        {
                            command_list.Items.Add(options["name"].InnerText);
                        }
                    }
                }
            }
            xnList = xmlDocModules.SelectNodes("/modules/module");
            foreach (XmlNode xn_node in xnList)
            {
                XmlNodeList optionList = xn_node.ChildNodes;
                foreach (XmlNode option in optionList)
                {
                    if (option.Name.Equals("commands"))
                    {
                        XmlNodeList Options = option.ChildNodes;
                        foreach (XmlNode options in Options)
                        {
                            if (options["name"].InnerText.Equals(command_list.Items[0]))
                            {
                                command_label.Text = options["name"].InnerText;
                                command_name.Text = options["name"].InnerText;
                                command_triggers.Text = options["triggers"].InnerText;
                                command_arguments.Text = options["syntax"].InnerText;
                                command_description.Text = options["description"].InnerText;
                                command_access_level.Text = options["access_level"].InnerText;
                                channel_blacklist.Text = options["blacklist"].InnerText;
                                show_in_help.Checked = Convert.ToBoolean(options["show_help"].InnerText);
                                spam_counter.Checked = Convert.ToBoolean(options["spam_check"].InnerText);
                                break;
                            }
                        }
                    }
                }
            }
            module_list.SelectedIndex = 0;
            command_list.SelectedIndex = 0;
        }

        private void save_server_button_Click(object sender, EventArgs e)
        {
            if (server_name_box.Text == "")
            {
                MessageBox.Show("A Server Name must be specified");
            }
            else if (server_address_box.Text == "")
            {
                MessageBox.Show("A Server Address must be specified");
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
                server_name = server_name_box.Text;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList xnServerList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                bool server_found = false;
                foreach (XmlNode xn in xnServerList)
                {
                    if (server_name.Equals(xn["server_name"].InnerText, StringComparison.InvariantCultureIgnoreCase))
                    {
                        server_found = true;
                        break;
                    }
                }
                if (!server_found)
                {
                    XmlNode Serverxn = xmlDoc.SelectSingleNode("/bot_settings/server_list");
                    XmlNode node = xmlDoc.CreateElement("server");
                    XmlNode nodeName = xmlDoc.CreateElement("name");
                    nodeName.InnerText = bot_name_box.Text;
                    node.AppendChild(nodeName);
                    XmlNode nodeNick = xmlDoc.CreateElement("nick");
                    nodeNick.InnerText = bot_nick_box.Text;
                    node.AppendChild(nodeNick);
                    XmlNode nodeSecNick = xmlDoc.CreateElement("sec_nicks");
                    nodeSecNick.InnerText = sec_nicks.Text;
                    node.AppendChild(nodeSecNick);
                    XmlNode nodePassword = xmlDoc.CreateElement("password");
                    nodePassword.InnerText = password_box.Text;
                    node.AppendChild(nodePassword);
                    XmlNode nodeEmail = xmlDoc.CreateElement("email");
                    nodeEmail.InnerText = email_box.Text;
                    node.AppendChild(nodeEmail);
                    XmlNode nodeOwner = xmlDoc.CreateElement("owner");
                    nodeOwner.InnerText = owner_nicks_box.Text;
                    node.AppendChild(nodeOwner);
                    XmlNode nodePort = xmlDoc.CreateElement("port");
                    nodePort.InnerText = port_box.Text;
                    node.AppendChild(nodePort);
                    XmlNode nodeServer_Name = xmlDoc.CreateElement("server_name");
                    nodeServer_Name.InnerText = server_name_box.Text;
                    node.AppendChild(nodeServer_Name);
                    XmlNode nodeServer_Address = xmlDoc.CreateElement("server_address");
                    nodeServer_Address.InnerText = server_address_box.Text;
                    node.AppendChild(nodeServer_Address);
                    XmlNode nodeServer_Folder = xmlDoc.CreateElement("server_folder");
                    nodeServer_Folder.InnerText = server_name;
                    node.AppendChild(nodeServer_Folder);
                    XmlNode nodeChanList = xmlDoc.CreateElement("chan_list");
                    nodeChanList.InnerText = channels_box.Text;
                    node.AppendChild(nodeChanList);
                    XmlNode nodeChanBlacklist = xmlDoc.CreateElement("chan_blacklist");
                    nodeChanBlacklist.InnerText = channel_blacklist_box.Text;
                    node.AppendChild(nodeChanBlacklist);
                    XmlNode nodeignore_list = xmlDoc.CreateElement("ignore_list");
                    nodeignore_list.InnerText = ignore_list_box.Text;
                    node.AppendChild(nodeignore_list);
                    XmlNode nodeauto_connect = xmlDoc.CreateElement("auto_connect");
                    nodeauto_connect.InnerText = auto_connect.Checked.ToString();
                    node.AppendChild(nodeauto_connect);
                    XmlNode nodeuser_level = xmlDoc.CreateElement("user_level");
                    nodeuser_level.InnerText = user_level_box.Text;
                    node.AppendChild(nodeuser_level);
                    XmlNode nodevoice_level = xmlDoc.CreateElement("voice_level");
                    nodevoice_level.InnerText = voice_level_box.Text;
                    node.AppendChild(nodevoice_level);
                    XmlNode nodehop_level = xmlDoc.CreateElement("hop_level");
                    nodehop_level.InnerText = hop_level_box.Text;
                    node.AppendChild(nodehop_level);
                    XmlNode nodeop_level = xmlDoc.CreateElement("op_level");
                    nodeop_level.InnerText = op_level_box.Text;
                    node.AppendChild(nodeop_level);
                    XmlNode nodesop_level = xmlDoc.CreateElement("sop_level");
                    nodesop_level.InnerText = sop_level_box.Text;
                    node.AppendChild(nodesop_level);
                    XmlNode nodefounder_level = xmlDoc.CreateElement("founder_level");
                    nodefounder_level.InnerText = founder_level_box.Text;
                    node.AppendChild(nodefounder_level);
                    XmlNode nodeowner_level = xmlDoc.CreateElement("owner_level");
                    nodeowner_level.InnerText = owner_level_box.Text;
                    node.AppendChild(nodeowner_level);
                    Serverxn.AppendChild(node);
                    xmlDoc.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");

                    Directory.CreateDirectory(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_name);

                    XmlNodeList xnList = xmlDocModules.SelectNodes("/modules/module");
                    foreach (XmlNode xn in xnList)
                    {
                        int element_num = 0;
                        String module_name = xn["name"].InnerText;

                        if (module_list.SelectedItem.Equals(module_name))
                        {
                            CheckBox myCheckboxEnabled = (CheckBox)module_options.Controls.Find("checkBox_" + module_name + "_enabled", true)[0];
                            xn["enabled"].InnerText = myCheckboxEnabled.Checked.ToString();
                            element_num++;
                            xn["blacklist"].InnerText = module_options.Controls.Find("textBox_" + module_name + "_blacklist", true)[0].Text;

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
                                                TextBox myTextBox = (TextBox)module_options.Controls.Find("textBox_" + module_name + "_" + options.Name + "_" + element_num.ToString(), true)[0];
                                                options["value"].InnerText = myTextBox.Text;
                                                element_num++;
                                                break;
                                            case "checkbox":
                                                CheckBox myCheckbox = (CheckBox)module_options.Controls.Find("checkBox_" + module_name + "_" + options.Name + "_" + element_num.ToString(), true)[0];
                                                options["checked"].InnerText = myCheckbox.Checked.ToString();
                                                element_num++;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    xmlDocModules.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_name + Path.DirectorySeparatorChar + "modules.xml");
                    old_configeration.add_to_list(server_name);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("You must use a unique Server Name.");
                }
            }
        }

        private void cancel_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void command_list_change(Object sender, EventArgs e)
        {
            XmlNodeList xnList = xmlDocModules.SelectNodes("/modules/module");
            foreach (XmlNode xn in xnList)
            {
                XmlNodeList optionList = xn.ChildNodes;
                foreach (XmlNode option in optionList)
                {
                    if (option.Name.Equals("commands"))
                    {
                        XmlNodeList Options = option.ChildNodes;
                        foreach (XmlNode options in Options)
                        {
                            if (options["name"].InnerText.Equals(command_label.Text))
                            {
                                options["name"].InnerText = command_name.Text;
                                options["triggers"].InnerText = command_triggers.Text;
                                options["syntax"].InnerText = command_arguments.Text;
                                options["description"].InnerText = command_description.Text;
                                options["access_level"].InnerText = command_access_level.Text;
                                options["blacklist"].InnerText = channel_blacklist.Text;
                                options["show_help"].InnerText = show_in_help.Checked.ToString();
                                options["spam_check"].InnerText = spam_counter.Checked.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            xnList = xmlDocModules.SelectNodes("/modules/module");
            foreach (XmlNode xn in xnList)
            {
                XmlNodeList optionList = xn.ChildNodes;
                foreach (XmlNode option in optionList)
                {
                    if (option.Name.Equals("commands"))
                    {
                        XmlNodeList Options = option.ChildNodes;
                        foreach (XmlNode options in Options)
                        {
                            if (options["name"].InnerText.Equals(command_list.SelectedItem))
                            {
                                command_label.Text = options["name"].InnerText;
                                command_name.Text = options["name"].InnerText;
                                command_triggers.Text = options["triggers"].InnerText;
                                command_arguments.Text = options["syntax"].InnerText;
                                command_description.Text = options["description"].InnerText;
                                command_access_level.Text = options["access_level"].InnerText;
                                channel_blacklist.Text = options["blacklist"].InnerText;
                                show_in_help.Checked = Convert.ToBoolean(options["show_help"].InnerText);
                                spam_counter.Checked = Convert.ToBoolean(options["spam_check"].InnerText);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void module_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (module_options.Controls.Count > 0)
            {
                XmlNodeList xnList = xmlDocModules.SelectNodes("/modules/module");
                foreach (XmlNode xn in xnList)
                {
                    int element_num = 0;
                    String module_name = xn["name"].InnerText;

                    if (module_options.Controls.Find("checkBox_" + module_name + "_enabled", true).GetUpperBound(0) >= 0)
                    {
                        CheckBox myCheckboxEnabled = (CheckBox)module_options.Controls.Find("checkBox_" + module_name + "_enabled", true)[0];
                        xn["enabled"].InnerText = myCheckboxEnabled.Checked.ToString();
                        element_num++;
                        xn["blacklist"].InnerText = module_options.Controls.Find("textBox_" + module_name + "_blacklist", true)[0].Text;

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
                                            TextBox myTextBox = (TextBox)module_options.Controls.Find("textBox_" + module_name + "_" + options.Name + "_" + element_num.ToString(), true)[0];
                                            options["value"].InnerText = myTextBox.Text;
                                            element_num++;
                                            break;
                                        case "checkbox":
                                            CheckBox myCheckbox = (CheckBox)module_options.Controls.Find("checkBox_" + module_name + "_" + options.Name + "_" + element_num.ToString(), true)[0];
                                            options["checked"].InnerText = myCheckbox.Checked.ToString();
                                            element_num++;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            XmlNodeList xnList_2 = xmlDocModules.SelectNodes("/modules/module");
            foreach (XmlNode xn in xnList_2)
            {
                int element_num = 0;
                String module_name = xn["name"].InnerText;

                if (module_list.SelectedItem.Equals(module_name))
                {
                    module_options.Controls.Clear();
                    bool checkbox_checked = false;
                    if (xn["enabled"].InnerText == "True")
                    {
                        checkbox_checked = true;
                    }
                    CheckBox myCheckboxEnabled = new CheckBox();
                    myCheckboxEnabled.Name = "checkBox_" + module_name + "_enabled";
                    myCheckboxEnabled.Checked = checkbox_checked;
                    myCheckboxEnabled.Left = 185;
                    myCheckboxEnabled.Top = 10 + (element_num * 25);
                    myCheckboxEnabled.TabIndex = element_num + 1;
                    myCheckboxEnabled.TabStop = true;
                    module_options.Controls.Add(myCheckboxEnabled);

                    Label myLabelEnabled = new Label();
                    myLabelEnabled.Name = "label_" + module_name + "_enabled";
                    myLabelEnabled.Text = "Enabled";
                    myLabelEnabled.Width = 180;
                    myLabelEnabled.Height = 13;
                    myLabelEnabled.Left = 8;
                    myLabelEnabled.Top = 13 + (element_num * 25);
                    module_options.Controls.Add(myLabelEnabled);

                    element_num++;

                    TextBox Blacklist = new TextBox();
                    Blacklist.Name = "textBox_" + module_name + "_blacklist";
                    Blacklist.Text = xn["blacklist"].InnerText;
                    Blacklist.Width = 140;
                    Blacklist.Height = 20;
                    Blacklist.Left = 185;
                    Blacklist.Top = 10 + (element_num * 25);
                    Blacklist.TabIndex = element_num + 1;
                    Blacklist.TabStop = true;
                    module_options.Controls.Add(Blacklist);

                    Label myLabelBlacklist = new Label();
                    myLabelBlacklist.Name = "label_" + module_name + "_blacklist";
                    myLabelBlacklist.Text = "Blacklist";
                    myLabelBlacklist.Width = 180;
                    myLabelBlacklist.Height = 13;
                    myLabelBlacklist.Left = 8;
                    myLabelBlacklist.Top = 13 + (element_num * 25);
                    module_options.Controls.Add(myLabelBlacklist);
                    XmlNodeList optionList = xn.ChildNodes;

                    element_num++;
                    foreach (XmlNode option in optionList)
                    {
                        if (option.Name.Equals("commands"))
                        {
                            XmlNodeList Options = option.ChildNodes;
                            foreach (XmlNode options in Options)
                            {
                                command_list.Items.Add(options["name"].InnerText);
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
                                        TextBox myTextBox = new TextBox();
                                        myTextBox.Name = "textBox_" + module_name + "_" + options.Name + "_" + element_num.ToString();
                                        myTextBox.Text = options["value"].InnerText;
                                        myTextBox.TextAlign = HorizontalAlignment.Left;
                                        myTextBox.Width = 140;
                                        myTextBox.Height = 20;
                                        myTextBox.Left = 185;
                                        myTextBox.Top = 10 + (element_num * 25);
                                        myTextBox.TabIndex = element_num + 1;
                                        myTextBox.TabStop = true;
                                        module_options.Controls.Add(myTextBox);

                                        Label myLabelText = new Label();
                                        myLabelText.Name = "label_" + module_name + "_" + options.Name + "_" + element_num.ToString();
                                        myLabelText.Text = options["label"].InnerText;
                                        myLabelText.Width = 180;
                                        myLabelText.Height = 13;
                                        myLabelText.Left = 8;
                                        myLabelText.Top = 13 + (element_num * 25);
                                        module_options.Controls.Add(myLabelText);

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
                                        myCheckbox.Left = 185;
                                        myCheckbox.Top = 10 + (element_num * 25);
                                        myCheckbox.TabIndex = element_num + 1;
                                        myCheckbox.TabStop = true;
                                        module_options.Controls.Add(myCheckbox);

                                        Label myLabelCheck = new Label();
                                        myLabelCheck.Name = "label_" + module_name + "_" + options.Name + "_" + element_num.ToString();
                                        myLabelCheck.Text = options["label"].InnerText;
                                        myLabelCheck.Width = 180;
                                        myLabelCheck.Height = 13;
                                        myLabelCheck.Left = 8;
                                        myLabelCheck.Top = 13 + (element_num * 25);
                                        module_options.Controls.Add(myLabelCheck);

                                        element_num++;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
