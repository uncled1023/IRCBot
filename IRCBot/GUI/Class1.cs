using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRCBot.GUI
{
    public class spam_info
    {
        public string spam_channel { get; set; }
        public int spam_count { get; set; }
        public bool spam_activated { get; set; }
    }

    public class timer_info
    {
        public string spam_channel { get; set; }
        public System.Windows.Forms.Timer spam_timer { get; set; }
    }
}
