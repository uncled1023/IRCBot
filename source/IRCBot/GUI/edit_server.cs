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
        private string server_module_folder;
        private XmlDocument xmlDocModules = new XmlDocument();
        public edit_server(Interface frmctrl, string tmp_server_name)
        {
            InitializeComponent();
            m_parent = frmctrl;
            server_name = tmp_server_name;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
            foreach (XmlNode xn in ServerxnList)
            {
                string tmp_server = xn["server_name"].InnerText;
                if (tmp_server.Equals(server_name))
                {
                    bot_name_box.Text = xn["name"].InnerText;
                    bot_nick_box.Text = xn["nick"].InnerText;
                    sec_nicks.Text = xn["sec_nicks"].InnerText;
                    password_box.Text = xn["password"].InnerText;
                    email_box.Text = xn["email"].InnerText;
                    owner_nicks_box.Text = xn["owner"].InnerText;
                    port_box.Text = xn["port"].InnerText;
                    server_name_box.Text = xn["server_name"].InnerText;
                    server_module_folder = xn["server_folder"].InnerText;
                    channels_box.Text = xn["chan_list"].InnerText;
                    channel_blacklist_box.Text = xn["chan_blacklist"].InnerText;
                    ignore_list_box.Text = xn["ignore_list"].InnerText;
                    user_level_box.Text = xn["user_level"].InnerText;
                    voice_level_box.Text = xn["voice_level"].InnerText;
                    hop_level_box.Text = xn["hop_level"].InnerText;
                    op_level_box.Text = xn["op_level"].InnerText;
                    sop_level_box.Text = xn["sop_level"].InnerText;
                    founder_level_box.Text = xn["founder_level"].InnerText;
                    owner_level_box.Text = xn["owner_level"].InnerText;

                    if (File.Exists(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_module_folder + Path.DirectorySeparatorChar + "modules.xml"))
                    {
                        xmlDocModules.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_module_folder + Path.DirectorySeparatorChar + "modules.xml");
                    }
                    else
                    {
                        Directory.CreateDirectory(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_module_folder);
                        File.Copy(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + "Default" + Path.DirectorySeparatorChar + "modules.xml", m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_module_folder + Path.DirectorySeparatorChar + "modules.xml");
                        xmlDocModules.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_module_folder + Path.DirectorySeparatorChar + "modules.xml");
                    }
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
            }
        }

        private void save_server_button_Click(object sender, EventArgs e)
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
                xmlDoc.Load(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                XmlNodeList ServerxnList = xmlDoc.SelectNodes("/bot_settings/server_list/server");
                foreach (XmlNode xn in ServerxnList)
                {
                    string tmp_server = xn["server_name"].InnerText;
                    if (tmp_server.Equals(server_name))
                    {
                        xn["name"].InnerText = bot_name_box.Text;
                        xn["nick"].InnerText = bot_nick_box.Text;
                        xn["sec_nicks"].InnerText = sec_nicks.Text;
                        xn["password"].InnerText = password_box.Text;
                        xn["email"].InnerText = email_box.Text;
                        xn["owner"].InnerText = owner_nicks_box.Text;
                        xn["port"].InnerText = port_box.Text;
                        xn["server_name"].InnerText = server_name_box.Text;
                        xn["chan_list"].InnerText = channels_box.Text;
                        xn["chan_blacklist"].InnerText = channel_blacklist_box.Text;
                        xn["ignore_list"].InnerText = ignore_list_box.Text;
                        xn["user_level"].InnerText = user_level_box.Text;
                        xn["voice_level"].InnerText = voice_level_box.Text;
                        xn["hop_level"].InnerText = hop_level_box.Text;
                        xn["op_level"].InnerText = op_level_box.Text;
                        xn["sop_level"].InnerText = sop_level_box.Text;
                        xn["founder_level"].InnerText = founder_level_box.Text;
                        xn["owner_level"].InnerText = owner_level_box.Text;
                        break;
                    }
                }
                xmlDoc.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");

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
                xmlDocModules.Save(m_parent.cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "Module_Config" + Path.DirectorySeparatorChar + server_module_folder + Path.DirectorySeparatorChar + "modules.xml");
                m_parent.update_conf();
                this.Close();
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

                    element_num++;
                    XmlNodeList optionList = xn.ChildNodes;
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
