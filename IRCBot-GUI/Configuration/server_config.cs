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

namespace IRCBot_GUI
{
    public partial class server_config : Form
    {
        private Interface m_parent;
        private configuration config_form;
        private string server_name;
        private string old_server_name;
        private string module_path;
        private XmlDocument xmlModules = new XmlDocument();
        private bool server_add;

        public server_config(Interface frmctrl, configuration config, string tmp_server_name, bool add_server)
        {
            m_parent = frmctrl;
            this.Icon = new Icon(m_parent.GetType(), "Bot.ico");
            InitializeComponent();
            config_form = config;
            server_name = tmp_server_name;
            old_server_name = tmp_server_name;
            server_add = add_server;

            XmlNode xn;
            if (server_add)
            {
                xmlModules = m_parent.controller.get_module_xml(null);
                xn = m_parent.controller.get_server_xml(null);
            }
            else
            {
                xmlModules = m_parent.controller.get_module_xml(server_name);
                xn = m_parent.controller.get_server_xml(server_name);
            }

            bot_name_box.Text = xn["name"].InnerText;
            bot_nick_box.Text = xn["nick"].InnerText;
            sec_nicks.Text = xn["sec_nicks"].InnerText;
            password_box.Text = xn["password"].InnerText;
            email_box.Text = xn["email"].InnerText;
            owner_nicks_box.Text = xn["owner"].InnerText;
            port_box.Text = xn["port"].InnerText;
            server_name_box.Text = xn["server_name"].InnerText;
            server_address_box.Text = xn["server_address"].InnerText;
            module_path = xn["module_path"].InnerText;
            channels_box.Text = xn["chan_list"].InnerText;
            channel_blacklist_box.Text = xn["chan_blacklist"].InnerText;
            ignore_list_box.Text = xn["ignore_list"].InnerText;
            auto_connect.Checked = Convert.ToBoolean(xn["auto_connect"].InnerText);
            user_level_box.Text = xn["user_level"].InnerText;
            voice_level_box.Text = xn["voice_level"].InnerText;
            hop_level_box.Text = xn["hop_level"].InnerText;
            op_level_box.Text = xn["op_level"].InnerText;
            sop_level_box.Text = xn["sop_level"].InnerText;
            founder_level_box.Text = xn["founder_level"].InnerText;
            owner_level_box.Text = xn["owner_level"].InnerText;
            command_prefix_box.Text = xn["command_prefix"].InnerText;
            spam_enable.Checked = Convert.ToBoolean(xn["spam_enable"].InnerText);
            spam_ignore.Text = xn["spam_ignore"].InnerText;
            spam_count_box.Text = xn["spam_count"].InnerText;
            spam_threshold_box.Text = xn["spam_threshold"].InnerText;
            spam_timeout_box.Text = xn["spam_timeout"].InnerText;
            max_message_length_box.Text = xn["max_message_length"].InnerText;
            keep_logs_box.Checked = Convert.ToBoolean(xn["keep_logs"].InnerText);
            log_folder_box.Text = xn["logs_path"].InnerText;

            XmlNode xnNode = xmlModules.SelectSingleNode("/modules");
            XmlNodeList xnList = xnNode.ChildNodes;
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
            xnNode = xmlModules.SelectSingleNode("/modules");
            xnList = xnNode.ChildNodes;
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
                MessageBox.Show("A Server must be specified");
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
                XmlNode xn_server;
                if (server_add)
                {
                    xn_server = m_parent.controller.get_server_xml(null);
                }
                else
                {
                    xn_server = m_parent.controller.get_server_xml(old_server_name);
                }
                if (xn_server != null)
                {
                    xn_server["name"].InnerText = bot_name_box.Text;
                    xn_server["nick"].InnerText = bot_nick_box.Text;
                    xn_server["sec_nicks"].InnerText = sec_nicks.Text;
                    xn_server["password"].InnerText = password_box.Text;
                    xn_server["email"].InnerText = email_box.Text;
                    xn_server["owner"].InnerText = owner_nicks_box.Text;
                    xn_server["port"].InnerText = port_box.Text;
                    xn_server["server_name"].InnerText = server_name_box.Text;
                    xn_server["server_address"].InnerText = server_address_box.Text;
                    xn_server["chan_list"].InnerText = channels_box.Text;
                    xn_server["chan_blacklist"].InnerText = channel_blacklist_box.Text;
                    xn_server["ignore_list"].InnerText = ignore_list_box.Text;
                    xn_server["auto_connect"].InnerText = auto_connect.Checked.ToString();
                    xn_server["user_level"].InnerText = user_level_box.Text;
                    xn_server["voice_level"].InnerText = voice_level_box.Text;
                    xn_server["hop_level"].InnerText = hop_level_box.Text;
                    xn_server["op_level"].InnerText = op_level_box.Text;
                    xn_server["sop_level"].InnerText = sop_level_box.Text;
                    xn_server["founder_level"].InnerText = founder_level_box.Text;
                    xn_server["owner_level"].InnerText = owner_level_box.Text;
                    xn_server["command_prefix"].InnerText = command_prefix_box.Text;
                    xn_server["spam_enable"].InnerText = spam_enable.Checked.ToString();
                    xn_server["spam_ignore"].InnerText = spam_ignore.Text;
                    xn_server["spam_count"].InnerText = spam_count_box.Text;
                    xn_server["spam_threshold"].InnerText = spam_threshold_box.Text;
                    xn_server["spam_timeout"].InnerText = spam_timeout_box.Text;
                    xn_server["max_message_length"].InnerText = max_message_length_box.Text;
                    xn_server["keep_logs"].InnerText = keep_logs_box.Checked.ToString();
                    xn_server["logs_path"].InnerText = log_folder_box.Text;
                    xn_server["module_path"].InnerText = "Modules" + Path.DirectorySeparatorChar + server_name + Path.DirectorySeparatorChar + "modules.xml";

                    if (server_add)
                    {
                        m_parent.controller.add_server_xml(xn_server);
                    }
                    else
                    {
                        m_parent.controller.save_server_xml(old_server_name, xn_server);
                    }

                    XmlNodeList xnList = xmlModules.SelectSingleNode("/modules").ChildNodes;
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

                    xnList = xmlModules.SelectSingleNode("/modules").ChildNodes;
                    bool cmd_found = false;
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
                                        cmd_found = true;
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
                            if (cmd_found)
                            {
                                break;
                            }
                        }
                    }

                    if (server_add)
                    {
                        m_parent.controller.add_module_xml(server_name, xmlModules);
                        config_form.add_to_list(server_name);
                    }
                    else
                    {
                        if (old_server_name.Equals(server_name))
                        {
                            m_parent.controller.save_module_xml(server_name, xmlModules);
                            m_parent.controller.update_conf(server_name);
                        }
                        else
                        {
                            m_parent.controller.delete_module_xml(module_path);
                            m_parent.controller.add_module_xml(server_name, xmlModules);
                            m_parent.controller.update_conf(old_server_name, server_name);
                            config_form.del_from_list(old_server_name, server_name);
                            m_parent.replace_tabs(old_server_name, server_name);
                        }
                    }
                    this.Close();
                }
            }
        }

        private void cancel_button_Click(object sender, EventArgs e)
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

        private void command_list_change(Object sender, EventArgs e)
        {
            XmlNodeList xnList = xmlModules.SelectSingleNode("/modules").ChildNodes;
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
            xnList = xmlModules.SelectSingleNode("/modules").ChildNodes;
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
                XmlNodeList xnList = xmlModules.SelectSingleNode("/modules").ChildNodes;
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
            XmlNodeList xnList_2 = xmlModules.SelectSingleNode("/modules").ChildNodes;
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
