using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Modules
{
    class version : Module
    {
        public override void control(bot ircbot, ref IRCConfig conf, int module_id, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if (type.Equals("query"))
            {
                string version = ":\u0001VERSION\u0001";
                if (line[3] == version)
                {
                    AboutBox1 about = new AboutBox1();
                    ircbot.sendData("NOTICE", nick + " :\u0001VERSION IRCBot v" + about.AssemblyVersion + " on " + conf.module_config[module_id][3] + "\u0001");
                }
            }
        }
    }
}
