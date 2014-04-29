using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Modules
{
    public class Module
    {
        public virtual void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type) { }

        private string name;
        internal string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        private string class_name;
        internal string Class_Name
        {
            get
            {
                return class_name;
            }

            set
            {
                class_name = value;
            }
        }

        private bool enabled;
        internal bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                enabled = value;
            }
        }

        private bool loaded;
        internal bool Loaded
        {
            get
            {
                return loaded;
            }

            set
            {
                loaded = value;
            }
        }

        private List<string> blacklist;
        internal List<string> Blacklist
        {
            get
            {
                return blacklist;
            }

            set
            {
                blacklist = value;
            }
        }

        private Dictionary<string, dynamic> options;
        internal Dictionary<string, dynamic> Options
        {
            get
            {
                return options;
            }

            set
            {
                options = value;
            }
        }

        private List<Command> commands;
        internal List<Command> Commands
        {
            get
            {
                return commands;
            }

            set
            {
                commands = value;
            }
        }
    }

    internal class Command
    {
        private string name;
        internal string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        private string description;
        internal string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        private List<string> triggers;
        internal List<string> Triggers
        {
            get
            {
                return triggers;
            }

            set
            {
                triggers = value;
            }
        }

        private string syntax;
        internal string Syntax
        {
            get
            {
                return syntax;
            }

            set
            {
                syntax = value;
            }
        }

        private int access;
        internal int Access
        {
            get
            {
                return access;
            }

            set
            {
                access = value;
            }
        }

        private List<string> blacklist;
        internal List<string> Blacklist
        {
            get
            {
                return blacklist;
            }

            set
            {
                blacklist = value;
            }
        }

        private bool show_help;
        internal bool Show_Help
        {
            get
            {
                return show_help;
            }

            set
            {
                show_help = value;
            }
        }

        private bool spam_check;
        internal bool Spam_Check
        {
            get
            {
                return spam_check;
            }

            set
            {
                spam_check = value;
            }
        }
    }
}
