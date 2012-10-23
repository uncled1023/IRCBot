using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using AIMLbot;

namespace IRCBot
{
    class chat
    {
        private System.Timers.Timer chat_time = new System.Timers.Timer();
        public bool still_chatting;
        private Bot myBot;
        private User myUser;
        private List<string> chatting_nick;

        public chat()
        {
            chat_time.Interval = 20000;
            chat_time.AutoReset = false;
            chat_time.Elapsed += stop_chat;
            chat_time.Enabled = false;

            still_chatting = false;
            chatting_nick = new List<string>();
            myBot = new Bot();
            myBot.loadSettings();
            myUser = new User("chat_nick", myBot);

            AIMLbot.Utils.AIMLLoader loader = new AIMLbot.Utils.AIMLLoader(myBot);
            myBot.isAcceptingUserInput = false;
            loader.loadAIML(myBot.PathToAIML);
            myBot.isAcceptingUserInput = true;
        }

        public void chat_control(string[] line, Interface ircbot, IRCConfig conf, string nick, string channel)
        {
            if (line.GetUpperBound(0) >= 3)
            {
                string msg = "";
                if (line.GetUpperBound(0) > 3)
                {
                    msg = line[3].TrimStart(':') + " " + line[4];
                }
                else
                {
                    msg = line[3].TrimStart(':');
                }
                string[] words = msg.Split(' ');
                bool me_in = false;
                foreach (string word in words)
                {
                    if (word.ToLower().Contains(conf.nick.ToLower()))
                    {
                        me_in = true;
                        break;
                    }
                }
                if (me_in == true || still_chatting == true)
                {
                    bool nick_found = false;
                    for (int x = 0; x < chatting_nick.Count(); x++)
                    {
                        if (chatting_nick[x].Equals(nick))
                        {
                            nick_found = true;
                        }
                    }
                    if (me_in == true && nick_found == false)
                    {
                        chatting_nick.Add(nick);
                        nick_found = true;
                    }
                    if (nick_found == true)
                    {
                        // Start Chatting
                        still_chatting = false;
                        chat_time.Stop();
                        Request r = new Request(msg, myUser, myBot);
                        Result res = myBot.Chat(r);
                        ircbot.sendData("PRIVMSG", channel + " :" + res.Output.Replace("[nick]", nick).Replace("[me]", conf.nick).Replace("[owner]", conf.owner));
                        chat_time.Start();
                        still_chatting = true;
                    }
                }
            }
        }

        private void stop_chat(object sender, EventArgs e)
        {
            still_chatting = false;
            chatting_nick.Clear();
            chat_time.Enabled = false;
            chat_time.Stop();
        }
    }
}
