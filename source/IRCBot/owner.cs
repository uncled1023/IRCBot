using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.IO;

namespace IRCBot
{
    class owner
    {
        public void owner_control(string[] line, string command, Interface ircbot, ref IRCConfig conf, int nick_access, string nick)
        {
            switch (command)
            {
                case "owner":
                    if (nick_access >= ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            if (line[4].Equals(conf.pass))
                            {
                                add_owner(nick, ircbot, ref conf);
                                ircbot.sendData("NOTICE", nick + " :You are now identified as an owner!");
                            }
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "addowner":
                    if (nick_access == ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            add_owner(line[4], ircbot, ref conf);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "delowner":
                    if (nick_access == ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            del_owner(line[4], ircbot, ref conf);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "id":
                    if (nick_access == ircbot.get_command_access(command))
                    {
                        ircbot.identify();
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "join":
                    if (nick_access == ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            line[4].Replace(' ', ',');
                            ircbot.sendData("JOIN", line[4]);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "part":
                    if (nick_access == ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            line[4].Replace(' ', ',');
                            ircbot.sendData("PART", line[4]);
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "say":
                    if (nick_access == ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) > 3)
                        {
                            char[] charS = new char[] { ' ' };
                            string[] new_line = line[4].Split(charS, 2);
                            if (new_line[0].StartsWith("#") == true && new_line.GetUpperBound(0) > 0)
                            {
                                ircbot.sendData("PRIVMSG", new_line[0] + " :" + new_line[1]);
                            }
                            else
                            {
                                ircbot.sendData("PRIVMSG", line[2] + " :" + line[4]);
                            }
                        }
                        else
                        {
                            ircbot.sendData("PRIVMSG", line[2] + " :" + nick + ", you need to include more info.");
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
                case "quit":
                    if (nick_access == ircbot.get_command_access(command))
                    {
                        if (line.GetUpperBound(0) <= 3)
                        {
                            ircbot.sendData("QUIT", "Leaving");
                        }
                        else
                        {
                            ircbot.sendData("QUIT", ":" + line[4]); //if the command is quit, send the QUIT command to the server with a quit message
                        }
                        ircbot.shouldRun = false; //turn shouldRun to false - the server will stop sending us data so trying to read it will not work and result in an error. This stops the loop from running and we will close off the connections properly
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                    }
                    break;
            }
        }

        private void add_owner(string nick, Interface ircbot, ref IRCConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(ircbot.cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + "\\config\\config.xml");
                XmlNode list = xmlDoc.SelectSingleNode("connection_settings");
                foreach (XmlNode node in list)
                {
                    if (node.Name == "owner")
                    {
                        string new_owner = node.InnerText + "," + nick;
                        node.InnerText = new_owner;
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + "\\config\\config.xml");
                conf.owner += "," + nick;
            }
        }

        private void del_owner(string nick, Interface ircbot, ref IRCConfig conf)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string new_owner = "";
            if (File.Exists(ircbot.cur_dir + "\\config\\config.xml"))
            {
                xmlDoc.Load(ircbot.cur_dir + "\\config\\config.xml");
                XmlNode list = xmlDoc.SelectSingleNode("connection_settings");
                foreach (XmlNode node in list)
                {
                    if (node.Name == "owner")
                    {
                        string[] new_owner_tmp = node.InnerText.Split(',');
                        for (int x = 0; x <= new_owner_tmp.GetUpperBound(0); x++)
                        {
                            if (new_owner_tmp[x].Equals(nick))
                            {
                            }
                            else
                            {
                                new_owner += new_owner_tmp[x] + ",";
                            }
                        }
                        node.InnerText = new_owner.TrimEnd(',');
                    }
                }
                xmlDoc.Save(ircbot.cur_dir + "\\config\\config.xml");
                conf.owner = new_owner.TrimEnd(',');
            }
        }
    }
}
