using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Reflection;
using System.Net;
using System.Text.RegularExpressions;
using Bot;

namespace IRCBot
{
    public class bot_controller
    {
        public List<bot> bot_instances;
        public DateTime run_time;
        public readonly object outputLock = new object();

        private XmlDocument servers = new XmlDocument();
        private XmlDocument default_servers = new XmlDocument();
        private XmlDocument default_module = new XmlDocument();

        internal readonly object listLock = new object();
        internal readonly object errorlock = new object();

        internal List<string> queue_text;
        internal string cur_dir;
        internal string servers_config_path;

        public bot_controller(string server_config_path)
        {
            bot_instances = new List<bot>();
            cur_dir = Directory.GetCurrentDirectory();
            servers_config_path = server_config_path;
            run_time = DateTime.Now;
            queue_text = new List<string>();
            queue_text.Capacity = 1000;
            queue_text.Clear();

            using (Stream ServersStream = this.GetType().Assembly.GetManifestResourceStream("IRCBot.lib.Config.servers.xml"))
            {
                using (XmlReader servers_reader = XmlReader.Create(ServersStream))
                {
                    default_servers.Load(servers_reader);
                }
            }

            using (Stream ModulesStream = this.GetType().Assembly.GetManifestResourceStream("IRCBot.lib.Config.modules.xml"))
            {
                using (XmlReader module_reader = XmlReader.Create(ModulesStream))
                {
                    default_module.Load(module_reader);
                }
            }
            
            if (File.Exists(servers_config_path))
            {
                XmlDocument tmp_servers = new XmlDocument();
                tmp_servers.Load(servers_config_path);
                XmlNodeList xnList = tmp_servers.SelectSingleNode("/server_list").ChildNodes;
                XmlNode default_node = default_servers.SelectSingleNode("/server_list");
                //let's add the root element
                servers.AppendChild(servers.CreateElement("", "server_list", ""));
                foreach (XmlNode server in xnList)
                {
                    temp_xml = new XmlDocument();
                    temp_xml.LoadXml(server.OuterXml);
                    XmlDocument tmp_default = new XmlDocument();
                    tmp_default.LoadXml(default_node.InnerXml);
                    foreach (XmlNode ChNode in tmp_default.ChildNodes)
                    {
                        CompareLower(ChNode);
                    }
                    servers.DocumentElement.AppendChild(servers.ImportNode(temp_xml.SelectSingleNode("/server"), true));
                }
                servers.Save(servers_config_path);
            }
            else
            {
                default_servers.Save(servers_config_path);
                servers.Load(servers_config_path);
            }

            foreach (string server in list_servers())
            {
                string module_path = get_module_config_path(server);
                check_config(default_module, module_path);
            }
        }

        public string[] list_servers()
        {
            string server = "";
            XmlNodeList xnList = servers.SelectNodes("/server_list/server");
            foreach (XmlNode xn in xnList)
            {
                server += "," + xn["server_name"].InnerText;
            }
            char[] sep = new char[] { ',' };
            string[] server_list = server.Trim(',').Split(sep, StringSplitOptions.RemoveEmptyEntries);
            return server_list;
        }

        public bool init_server(string server_name, bool manual)
        {
            bool server_initiated = false;
            bot bot = get_bot_instance(server_name);
            if (File.Exists(servers_config_path) && bot == null)
            {
                BotConfig bot_conf = get_bot_conf(server_name);
                if (bot_conf.Auto_Connect || manual)
                {
                    bot bot_instance = new bot(this, bot_conf);
                    bot_instance.start_bot();
                    bot_instances.Add(bot_instance);
                    server_initiated = true;
                }
            }
            return server_initiated;
        }

        public bool start_bot(string server_name)
        {
            bool server_started = false;
            bot bot = get_bot_instance(server_name);
            if (bot != null)
            {
                if (bot.connected == false && bot.connecting == false && bot.disconnected == true && bot.Conf.Server_Name.Equals(server_name))
                {
                    server_started = true;
                    bot.start_bot();
                }
            }
            return server_started;
        }

        public bool stop_bot(string server_name)
        {
            bool server_terminated = false;
            foreach (Bot.bot bot in bot_instances)
            {
                if ((bot.connected == true || bot.connecting == true) && bot.Conf.Server_Name.Equals(server_name))
                {
                    server_terminated = true;
                    bot.worker.CancelAsync();
                    break;
                }
            }
            return server_terminated;
        }

        public bot get_bot_instance(string server_name)
        {
            foreach (Bot.bot bot in bot_instances)
            {
                if (server_name.Equals(bot.Conf.Server_Name))
                {
                    return bot;
                }
            }
            return null;
        }

        public bool remove_bot_instance(string server_name)
        {
            bool server_found = false;
            int index = 0;
            foreach (Bot.bot bot in bot_instances)
            {
                if (server_name.Equals(bot.Conf.Server_Name))
                {
                    server_found = true;
                    break;
                }
                else
                {
                    server_found = false;
                    index++;
                }
            }
            if (server_found == true && bot_instances.Count > index)
            {
                bot_instances.RemoveAt(index);
            }
            return server_found;
        }

        public bool bot_connected(string server_name)
        {
            foreach (Bot.bot bot in bot_instances)
            {
                if (bot.connected == true && bot.Conf.Server_Name.Equals(server_name))
                {
                    return bot.connected;
                }
            }
            return false;
        }

        public string get_module_config_path(string server_name)
        {
            if (server_name != null)
            {
                XmlNodeList xnList = servers.SelectNodes("/server_list/server");
                foreach (XmlNode xn in xnList)
                {
                    if (xn["server_name"].InnerText.Equals(server_name))
                    {
                        return Path.GetDirectoryName(servers_config_path) + Path.DirectorySeparatorChar + xn["module_path"].InnerText;
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        public XmlNode get_server_xml(string server_name)
        {
            if (server_name != null)
            {
                XmlNodeList xnList = servers.SelectNodes("/server_list/server");
                foreach (XmlNode xn in xnList)
                {
                    if (xn["server_name"].InnerText.Equals(server_name))
                    {
                        return xn;
                    }
                }
                return null;
            }
            else
            {
                return default_servers.SelectSingleNode("/server_list/server");
            }
        }

        public void add_server_xml(XmlNode server_xml)
        {
            XmlNode insertNode = servers.ImportNode(server_xml, true);
            servers.ChildNodes[0].AppendChild(insertNode);
            servers.Save(servers_config_path);
        }

        public bool save_server_xml(string server_name, XmlNode server_xml)
        {
            bool saved = false;
            XmlNodeList xnList = servers.SelectNodes("/server_list/server");
            foreach (XmlNode xn in xnList)
            {
                if (xn["server_name"].InnerText.Equals(server_name))
                {
                    xn.ParentNode.ReplaceChild(server_xml, xn);
                    saved = true;
                }
            }
            servers.Save(servers_config_path);
            return saved;
        }

        public bool delete_server_xml(string server_name)
        {
            bool deleted = false;
            if (server_name != null)
            {
                XmlNodeList xnList = servers.SelectNodes("/server_list/server");
                foreach (XmlNode xn in xnList)
                {
                    if (xn["server_name"].InnerText.Equals(server_name))
                    {
                        delete_module_xml(xn["module_path"].InnerText);
                        xn.ParentNode.RemoveChild(xn);
                        deleted = true;
                        break;
                    }
                }
            }
            servers.Save(servers_config_path);
            return deleted;
        }

        public XmlDocument get_module_xml(string server_name)
        {
            if (server_name != null)
            {
                string module_path = get_module_config_path(server_name);
                XmlDocument modules = new XmlDocument();
                modules.Load(module_path);
                return modules;
            }
            else
            {
                return default_module;
            }
        }

        public void add_module_xml(string server_name, XmlDocument module_xml)
        {
            string module_path = get_module_config_path(server_name);
            Directory.CreateDirectory(Path.GetDirectoryName(module_path));
            module_xml.Save(module_path);
        }

        public void save_module_xml(string server_name, XmlDocument module_xml)
        {
            string module_path = get_module_config_path(server_name);
            module_xml.Save(module_path);
        }

        public void delete_module_xml(string module_path)
        {
            File.Delete(Path.GetDirectoryName(servers_config_path) + Path.DirectorySeparatorChar + module_path);
            deleteEmptyDirectories(Path.GetDirectoryName(servers_config_path));
        }

        public void update_conf()
        {
            foreach (bot bot_instance in bot_instances)
            {
                bot_instance.Conf = get_bot_conf(bot_instance.Conf.Server_Name);
            }
        }

        public void update_conf(string server_name)
        {
            foreach (bot bot_instance in bot_instances)
            {
                if (bot_instance.Conf.Server_Name.Equals(server_name))
                {
                    bot_instance.Conf = get_bot_conf(bot_instance.Conf.Server_Name);
                    break;
                }
            }
        }

        public void update_conf(string old_server_name, string new_server_name)
        {
            foreach (bot bot_instance in bot_instances)
            {
                if (bot_instance.Conf.Server_Name.Equals(old_server_name))
                {
                    bot_instance.Conf = get_bot_conf(new_server_name);
                    break;
                }
            }
        }

        public BotConfig get_bot_conf(string server_name)
        {
            BotConfig bot_conf = new BotConfig();
            XmlNode xn = get_server_xml(server_name);
            if (xn != null)
            {
                string module_path = Path.GetDirectoryName(servers_config_path) + Path.DirectorySeparatorChar + xn["module_path"].InnerText;
                bot_conf.Modules = new List<Bot.Modules.Module>();
                bot_conf.Spam_Check = new List<spam_info>();
                bot_conf.Channel_List = new List<Channel_Info>();
                bot_conf.Name = xn["name"].InnerText;
                bot_conf.Nick = xn["nick"].InnerText;
                bot_conf.Secondary_Nicks = xn["sec_nicks"].InnerText;
                bot_conf.Pass = xn["password"].InnerText;
                bot_conf.Email = xn["email"].InnerText;
                bot_conf.Owner = xn["owner"].InnerText;
                bot_conf.Port = Convert.ToInt32(xn["port"].InnerText);
                bot_conf.Server_Name = xn["server_name"].InnerText;
                bot_conf.Server_Address = xn["server_address"].InnerText;
                bot_conf.Chans = xn["chan_list"].InnerText;
                bot_conf.Chan_Blacklist = xn["chan_blacklist"].InnerText;
                bot_conf.Ignore_List = xn["ignore_list"].InnerText;
                bot_conf.User_Level = Convert.ToInt32(xn["user_level"].InnerText);
                bot_conf.Voice_Level = Convert.ToInt32(xn["voice_level"].InnerText);
                bot_conf.Hop_Level = Convert.ToInt32(xn["hop_level"].InnerText);
                bot_conf.Op_Level = Convert.ToInt32(xn["op_level"].InnerText);
                bot_conf.Sop_Level = Convert.ToInt32(xn["sop_level"].InnerText);
                bot_conf.Founder_Level = Convert.ToInt32(xn["founder_level"].InnerText);
                bot_conf.Owner_Level = Convert.ToInt32(xn["owner_level"].InnerText);
                bot_conf.Auto_Connect = Convert.ToBoolean(xn["auto_connect"].InnerText);
                bot_conf.Command = xn["command_prefix"].InnerText;
                bot_conf.Spam_Enable = Convert.ToBoolean(xn["spam_enable"].InnerText);
                bot_conf.Spam_Ignore = xn["spam_ignore"].InnerText;
                bot_conf.Spam_Count_Max = Convert.ToInt32(xn["spam_count"].InnerText);
                bot_conf.Spam_Threshold = Convert.ToInt32(xn["spam_threshold"].InnerText);
                bot_conf.Spam_Timeout = Convert.ToInt32(xn["spam_timeout"].InnerText);
                bot_conf.Max_Message_Length = Convert.ToInt32(xn["max_message_length"].InnerText);
                bot_conf.Keep_Logs = xn["keep_logs"].InnerText;
                bot_conf.Logs_Path = xn["logs_path"].InnerText;
                bot_conf.Max_Log_Size = Convert.ToInt32(xn["max_log_size"].InnerText);
                bot_conf.Max_Log_Number = Convert.ToInt32(xn["max_log_number"].InnerText);
                bot_conf.Default_Level = Math.Min(bot_conf.User_Level, Math.Min(bot_conf.Voice_Level, Math.Min(bot_conf.Hop_Level, Math.Min(bot_conf.Op_Level, Math.Min(bot_conf.Sop_Level, Math.Min(bot_conf.Founder_Level, bot_conf.Owner_Level)))))) - 1;

                try
                {
                    bot_conf.Server_IP = Dns.GetHostAddresses(bot_conf.Server_Address);
                }
                catch
                {
                    bot_conf.Server_IP = null;
                }
            }
            return bot_conf;
        }

        internal Bot.Modules.Module get_module_conf(string server_name, string module_class_name)
        {
            Bot.Modules.Module module_conf = new Bot.Modules.Module();
            module_conf.Blacklist = new List<string>();
            module_conf.Commands = new List<Bot.Modules.Command>();
            module_conf.Options = new Dictionary<string, dynamic>();

            XmlDocument xmlDocModules = new XmlDocument();
            xmlDocModules = get_module_xml(server_name);
            XmlNode xnNode = xmlDocModules.SelectSingleNode("/modules");
            XmlNodeList xnList = xnNode.ChildNodes;
            foreach (XmlNode xn_module in xnList)
            {
                if (xn_module["class_name"].InnerText.Equals(module_class_name))
                {
                    module_conf.Name = xn_module["name"].InnerText;
                    module_conf.Class_Name = xn_module["class_name"].InnerText;
                    module_conf.Enabled = Convert.ToBoolean(xn_module["enabled"].InnerText);
                    module_conf.Blacklist.AddRange(xn_module["blacklist"].InnerText.Split(','));
                    module_conf.Loaded = false;

                    XmlNodeList optionList = xn_module.ChildNodes;
                    foreach (XmlNode option in optionList)
                    {
                        if (option.Name.Equals("commands"))
                        {
                            XmlNodeList Options = option.ChildNodes;
                            foreach (XmlNode options in Options)
                            {
                                Bot.Modules.Command tmp_command = new Bot.Modules.Command();
                                tmp_command.Triggers = new List<string>();
                                tmp_command.Blacklist = new List<string>();
                                tmp_command.Name = options["name"].InnerText;
                                tmp_command.Description = options["description"].InnerText;
                                tmp_command.Triggers.AddRange(options["triggers"].InnerText.Split('|'));
                                tmp_command.Syntax = options["syntax"].InnerText;
                                tmp_command.Access = Convert.ToInt32(options["access_level"].InnerText);
                                tmp_command.Blacklist.AddRange(options["blacklist"].InnerText.Split(','));
                                tmp_command.Show_Help = Convert.ToBoolean(options["show_help"].InnerText);
                                tmp_command.Spam_Check = Convert.ToBoolean(options["spam_check"].InnerText);
                                module_conf.Commands.Add(tmp_command);
                            }
                        }
                        if (option.Name.Equals("options"))
                        {
                            XmlNodeList Options = option.ChildNodes;
                            foreach (XmlNode options in Options)
                            {
                                dynamic value = null;
                                switch (options["type"].InnerText)
                                {
                                    case "textbox":
                                        value = options["value"].InnerText;
                                        break;
                                    case "checkbox":
                                        value = Convert.ToBoolean(options["checked"].InnerText);
                                        break;
                                }
                                module_conf.Options.Add(options.Name, value);
                            }
                        }
                    }
                }
            }
            return module_conf;
        }

        public List<string> get_module_list(string server_name)
        {
            List<string> module_list = new List<string>();

            XmlDocument xmlDocModules = new XmlDocument();
            xmlDocModules = get_module_xml(server_name);
            XmlNode xnNode = xmlDocModules.SelectSingleNode("/modules");
            XmlNodeList xnList = xnNode.ChildNodes;
            foreach (XmlNode xn_module in xnList)
            {
                module_list.Add(xn_module["class_name"].InnerText);
            }
            return module_list;
        }

        public void run_command(string server_name, string channel, string command, string[] args)
        {
            bool bot_command = true;
            char[] charSeparator = new char[] { ' ' };
            string type = "channel";
            string msg = "";
            if (!channel.StartsWith("#"))
            {
                type = "query";
            }
            bot bot = get_bot_instance(server_name);
            if (bot != null)
            {
                if (args != null)
                {
                    foreach (string arg in args)
                    {
                        msg += " " + arg;
                    }
                }
                string line = ":" + bot.Nick + " PRIVMSG " + channel + " :" + bot.Conf.Command + command + msg;
                string[] ex = line.Split(charSeparator, 5);
                //Run Enabled Modules
                foreach (Bot.Modules.Module module in bot.Conf.Modules)
                {
                    if (module.Loaded)
                    {
                        module.control(bot, bot.Conf, ex, command, bot.Conf.Owner_Level, bot.Nick, channel, bot_command, type);
                    }
                }
            }
        }

        public void run_command(string server_name, string nick, string channel, string command, string[] args)
        {
            bool bot_command = true;
            char[] charSeparator = new char[] { ' ' };
            string type = "channel";
            string msg = "";
            if (!channel.StartsWith("#"))
            {
                type = "query";
            }
            bot bot = get_bot_instance(server_name);
            if (bot != null)
            {
                if (args != null)
                {
                    foreach (string arg in args)
                    {
                        msg += " " + arg;
                    }
                }
                string line = ":" + nick + " PRIVMSG " + channel + " :" + bot.Conf.Command + command + msg;
                string[] ex = line.Split(charSeparator, 5);
                //Run Enabled Modules
                foreach (Bot.Modules.Module module in bot.Conf.Modules)
                {
                    if (module.Loaded)
                    {
                        module.control(bot, bot.Conf, ex, command, bot.get_nick_access(nick, channel), nick, channel, bot_command, type);
                    }
                }
            }
        }

        public void send_data(string server_name, string cmd, string param)
        {
            bot bot = get_bot_instance(server_name);
            if (bot != null)
            {
                bot.sendData(cmd, param);
            }
        }

        private XmlDocument temp_xml;
        public void check_config(XmlDocument original, string xml_path)
        {
            temp_xml = new XmlDocument();
            temp_xml.Load(xml_path);
            foreach (XmlNode ChNode in original.ChildNodes)
            {
                CompareLower(ChNode);
            }
            temp_xml.Save(xml_path);
        }

        private void CompareLower(XmlNode NodeName)
        {
            foreach (XmlNode ChlNode in NodeName.ChildNodes)
            {
                if (ChlNode.Name == "#text")
                {
                    continue;
                }

                string Path = CreatePath(ChlNode);

                if (temp_xml.SelectNodes(Path).Count <= 0)
                {
                    XmlNode TempNode = temp_xml.ImportNode(ChlNode, true);
                    temp_xml.SelectSingleNode(Path.Substring(0, Path.LastIndexOf("/"))).AppendChild(TempNode);
                }
                else
                {
                    CompareLower(ChlNode);
                }
                //server_xml.Save(server_xml_path);
            }
        }

        private string CreatePath(XmlNode Node)
        {

            string Path = "/" + Node.Name;

            while (!(Node.ParentNode.Name == "#document"))
            {
                Path = "/" + Node.ParentNode.Name + Path;
                Node = Node.ParentNode;
            }
            Path = "/" + Path;
            return Path;

        }

        private static void deleteEmptyDirectories(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                deleteEmptyDirectories(directory);
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        public string[] get_queue()
        {
            string[] queue;
            lock (listLock)
            {
                queue = queue_text.ToArray();
                queue_text.Clear();
            }
            return queue;
        }

        public void log(string log, bot bot, string channel, string date_stamp, string time_stamp)
        {
            if (bot != null && bot.Conf.Keep_Logs.Equals("True") && !log.Trim().Equals(string.Empty))
            {
                string file_name = "log.log";
                if (bot.Conf.Logs_Path == "")
                {
                    bot.Conf.Logs_Path = cur_dir + Path.DirectorySeparatorChar + "logs";
                }
                string pattern = "[^a-zA-Z0-9-_.+#]"; //regex pattern
                string parsed_chan = Regex.Replace(channel, pattern, "_");
                string full_path = bot.Conf.Logs_Path + Path.DirectorySeparatorChar + bot.Conf.Server_Name + Path.DirectorySeparatorChar + parsed_chan;
                if (Directory.Exists(full_path))
                {
                    if (File.Exists(full_path + Path.DirectorySeparatorChar + file_name))
                    {
                        FileInfo f = new FileInfo(full_path + Path.DirectorySeparatorChar + file_name);
                        long s1 = f.Length;
                        if (s1 > bot.Conf.Max_Log_Size)
                        {
                            if (File.Exists(full_path + Path.DirectorySeparatorChar + "log_" + bot.Conf.Max_Log_Number.ToString() + ".log"))
                            {
                                File.Delete(full_path + Path.DirectorySeparatorChar + "log_" + bot.Conf.Max_Log_Number.ToString() + ".log");
                            }
                            for (int x = bot.Conf.Max_Log_Number - 1; x >= 0; x--)
                            {
                                if (File.Exists(full_path + Path.DirectorySeparatorChar + "log_" + x.ToString() + ".log"))
                                {
                                    File.Move(full_path + Path.DirectorySeparatorChar + "log_" + x.ToString() + ".log", full_path + Path.DirectorySeparatorChar + "log_" + (x + 1).ToString() + ".log");
                                }
                            }
                            File.Move(full_path + Path.DirectorySeparatorChar + file_name, full_path + Path.DirectorySeparatorChar + "log_1.log");
                        }
                    }
                    StreamWriter log_file = File.AppendText(full_path + Path.DirectorySeparatorChar + file_name);
                    log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + log);
                    log_file.Close();
                }
                else
                {
                    Directory.CreateDirectory(full_path);
                    StreamWriter log_file = File.AppendText(full_path + Path.DirectorySeparatorChar + file_name);
                    log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + log);
                    log_file.Close();
                }
            }
        }

        public void log_error(Exception ex, string server_name)
        {
            bot bot = get_bot_instance(server_name);
            if (bot != null && bot.Conf.Keep_Logs.Equals("True") && ex != null)
            {
                string errorMessage =
                    "Unhandled Exception:\n\n" +
                    ex.Message + "\n\n" +
                    ex.GetType() +
                    "\n\nStack Trace:\n" +
                    ex.StackTrace;

                string file_name = "";
                file_name = "Errors.log";
                string time_stamp = DateTime.Now.ToString("hh:mm tt");
                string date_stamp = DateTime.Now.ToString("yyyy-MM-dd");
                string cur_dir = Directory.GetCurrentDirectory();

                if (Directory.Exists(cur_dir + Path.DirectorySeparatorChar + "errors"))
                {
                    StreamWriter log_file = File.AppendText(cur_dir + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                    log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + errorMessage);
                    log_file.Close();
                }
                else
                {
                    Directory.CreateDirectory(cur_dir + Path.DirectorySeparatorChar + "errors");
                    StreamWriter log_file = File.AppendText(cur_dir + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                    log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + errorMessage);
                    log_file.Close();
                }
            }
        }
    }
}
