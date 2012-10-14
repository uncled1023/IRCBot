using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot
{
    class chat
    {
        private System.Timers.Timer chat_time = new System.Timers.Timer();
        private bool still_chatting;
        private int chain_length;
        private int max_words;
        private string[] separator;
        private string stop_word;

        private chat()
        {
            chat_time.Interval = 5000;
            chat_time.AutoReset = false;
            chat_time.Elapsed += stop_chat;

            still_chatting = false;
            chain_length = 2;
            max_words = 30;
            separator = new string[] { "\x01" };
            stop_word = "\x02";
        }

        public void chat_control(string[] line, Interface ircbot, IRCConfig conf, int nick_access, string nick)
        {
            if (line.GetUpperBound(0) > 3)
            {
                string msg = line[3].TrimStart(':') + " " + line[4];
                string[] words = msg.Split(' ');
                bool me_in = false;
                foreach (string word in words)
                {
                    if (word.ToLower().Equals(conf.nick.ToLower()))
                    {
                        me_in = true;
                        break;
                    }
                }
                if (me_in == true || still_chatting == true)
                {
                    if (words.GetUpperBound(0) > chain_length)
                    {
                        //start_chat(words, ircbot);
                    }
                }
            }
        }

        private List<string> split_message(string[] words)
        {
            List<string> word_list = words.ToList();
            List<string> final_yield = new List<string>();
            word_list.Add(stop_word);
            for (int x = 0; x < (word_list.Count() - chain_length); x++)
            {
                for (int i = x; i < (x + chain_length); i++)
                {
                    final_yield.Add(word_list[i]);
                }
            }
            return final_yield;
        }

        private List<string> generate_message(string seed, Interface ircbot)
        {
            string key = seed;
            List<string> gen_words = new List<string>();
            gen_words.Add(" ");

            for (int x = 0; x < max_words; x++)
            {
                string[] words = key.Split(separator, StringSplitOptions.None);
                gen_words.Add(words[0]);
                string next_word = "";
            }
            return gen_words;
        }

        private void stop_chat(object sender, EventArgs e)
        {
            still_chatting = false;
            chat_time.Enabled = false;
        }
    }
}
