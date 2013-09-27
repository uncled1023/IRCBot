---------------------
About IRCBot
---------------------
Created by: Chris Woodward

The IRCBot is designed to provide an all-in-one solution for those who wish to run an IRC bot easily.

---------------------
Installation - Windows
---------------------
1) Copy all of the contents from the newest build in the release directory to a directory on your local machine
2) Run IRCBot.exe

When you first start up the IRC Bot, you will need to add your details into the configuration. You can do this one of two ways: By using the configuration manager in tools, or by editing the config.xml directly in the /config/ folder. The first is preferred as to reduce the chance of messing up the configuration file.

After clicking tools->configuration, you will then be presented with the configuration manager. From here, you can Add a new server, and configure bot settings

Once you have added your server, just click "Connect" and if you entered your configuration correctly your bot will then connect to the server and channels you specified.
Adding a Server

To add a new server, click the "Add Server" button in the Configuration window. The required fields are as follows:

    Server Name
        Format: <string>
        Default: Blank
    Server Address
        Format: irc.hostname.net
        Default: Blank
    Port Number
        Format: <integer>
        Default: 6667
    Name
        Format: <string>
        Default: Blank
    Nick
        Format: <string>
        Default: Blank

Each server has it's own settings for the Modules and Commands within the modules. You also can control the access level for each XOP level within the Op Levels Configuration tab.

---------------------
Installation - Linux (Alpha)
---------------------
1) Install mono and libgdiplus packages
1) Copy all of the contents from the newest build in the release folder to a directory on your local machine
3) Open a terminal emulator and cd it to the directory with the IRCBot.exe.
4) Type: 'mono IRCBot.exe'
3) Click Tools>Configuration and configure your bot's settings
4) Select the Server you added from the server list and click Connect, or restart the Bot application.

*Current Limitations: Does not display any output, some functions may not work, buggy.

---------------------
Bugs/Feature Requests
---------------------
Please report all bugs you find to me so I can fix them as soon as possible.  Also if you have any feature requests, feel free to send them to me as well.

---------------------
Contact Info
---------------------
Email: admin@inb4u.com
IRC: (irc.inb4u.com)#IRCBot
Nick: Uncled1023

---------------------
Acknowledgements
---------------------
Cameron Lucas