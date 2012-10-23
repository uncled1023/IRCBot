using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot
{
    class version
    {
        public void version_control(string[] line, Interface ircbot, IRCConfig conf, int conf_id, string nick)
        {
            string version = ":\u0001VERSION\u0001";
            if (line[3] == version)
            {
                AboutBox1 about = new AboutBox1();
                ircbot.sendData("NOTICE", nick + " :\u0001VERSION IRCBot v" + about.AssemblyVersion + " on " + conf.module_config[conf_id][2] + "\u0001");
            }
        }
    }
}
