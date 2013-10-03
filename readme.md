# About IRCBot

Created by: Chris Woodward

The IRCBot is designed to provide an all-in-one solution for those who wish to run an IRC bot easily.  It includes many useful features as well as fun games.  It can even be used as your personal client!

## Feature Set

* Channel Moderation
* Custom Access Levels
* Full GUI configuration
* Owner Control Functions
* Automatic nick registration
* Ghost on Nick in Use
* Flood Protection
* 4chan thread/reply viewing and searching
* URL/file parsing
* Google Search
* Wolfram Alpha Search
* SED
* Ping Requests
* Last Seen Nick
* Channel Rules
* Weather and Forcasts
* Magic 8ball
* Pass the Hbomb game
* Fun commands
* Chat Protocol (A.L.I.C.E.)
* Channel Roll Call
* Version Checker
* Idle
* Dice Rolls
* GitHub issue submission
* Full logging support
* Custom Alarms
* Surveys

## Installation - Windows

1) Download the Release.7z from the latest release and extract the files to a directory of your choice.
2) Run IRCBot.exe

## Installation - Linux (Alpha)

1) Install mono and libgdiplus packages.
1) Download the Release.7z from the latest release and extract the files to a directory of your choice.
3) Open a terminal emulator and cd it to the directory with the IRCBot.exe.
4) Type: `mono IRCBot.exe`
3) Click Tools>Configuration and configure your bot's settings
4) Select the Server you added from the server list and click Connect, or restart the Bot application.

* Current Limitations: Does not display any output, some functions may not work, buggy.

## Configuration

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

## Command List

Each command has the following properties:

<table>
  <tr>
    <th>Property</th><th>Format</th>
  </tr>
  <tr>
    <td>name</td><td>string</td>
  </tr>
  <tr>
    <td>description</td><td>string</td>
  </tr>
  <tr>
    <td>triggers</td><td>comma separated string array</td>
  </tr>
  <tr>
    <td>syntax</td><td>string</td>
  </tr>
  <tr>
    <td>access_level</td><td>int32</td>
  </tr>
  <tr>
    <td>blacklist</td><td>comma separated string array</td>
  </tr>
  <tr>
    <td>show_help</td><td>boolean</td>
  </tr>
  <tr>
    <td>spam_check</td><td>boolean</td>
  </tr>
</table>

### Fortunes

* `fortune` Displays a fortune.
Usage: `fortune`

### Trivia

* `trivia` Starts a new game of trivia.
Usage: `trivia`

* `stoptrivia` Stops a running game of trivia.
Aliases: `strivia`
Usage: `stoptrivia`

* `scores` Displays the top 10 scores.
Usage: `scores`

* `score` Shows your current rank and score.
Usage: `score`

### 4chan

* `4chan` Views a specific thread ID or OP number of a board, or a list of boards on 4chan.
Usage: `4chan [{board}] [{(#)thread_ID|OP_index}] [{(#)reply_ID|reply_index}]`

* `next_thread` Displays the next OP on the current board.
Aliases: `nt`
Usage: `next_thread`

* `next_reply` Displays the next reply on the current thread.
Aliases: `nr`
Usage: `nr`

* `4chansearch` Searchs a specific board for a thread that contains the specified query.
Aliases: `4chs`, `4cs`, `4chans`
Usage: `4chansearch {board} {query}`

### Is It Up

* `isitup` Checks if the web address specified is accessible from the bot.
Aliases: `isup`
Usage: `isitup {url}`

### Ping Me

* `pingme` Gets the ping time between the bot and the client requesting the ping.
Usage: `pingme`

### Seen

* `seen` Displays the last time the nick has been seen in the channel.
Usage: `seen`

### Access

* `setaccess` Adds the specified nick to the access list with the specified level.
Aliases: `addaccess`
Usage: `setaccess {nick} {access_level}`

* `delaccess` Removes the specified access level from the nick.
Usage: `delaccess {nick} {access_level}`

* `listaccess` Lists all the users with access on the channel and their level.
Aliases: `accesslist`
Usage: `listaccess`

* `getaccess` Displays the current access level of a user.
Aliases: `access`
Usage: `getaccess [{channel}] {nick}`

### Moderation

* `founder` Sets the nick to Owner of the chan.
Usage: `founder {nick}`

* `defounder` Unsets the nick as Owner of the chan.
Usage: `defounder {nick}

* `asop` Adds the nick to the Auto Super Op List.
Usage: `asop {nick}`

* `deasop` Removes the nick from the Auto Super Op List.
Usage: `deasop {nick}`

* `sop` Sets the nick as Super Op.
Usage: `sop {nick}`

* `desop` Removes the nick as Super Op.
Usage: `desop {nick}`

* `aop` Adds the nick to the Auto Op List.
Usage: `aop {nick}`

* `deaop` Removes the nick from the Auto Op List.
Usage: `deaop {nick}`

* `op` Sets the nick as an Op.
Usage: `op {nick}`

* `deop` Removes the nick as an Op.
Usage: `deop {nick}`

* `ahop` Adds the nick to the Auto HOP List.
Usage: `ahop {nick}`

* `deahop` Removes the nick from the Auto HOP List.
Usage: `deahop {nick}`

* `avoice` Adds the nick to the Auto Voice List.
Usage: `avoice {nick}`

* `deavoice` Removes the nick from the Auto Voice List.
Usage: `deavoice {nick}`

* `mode` Sets or unsets a channel mode.
Usage: `mode +/-{flags}`

* `topic` Sets the channels topic.
Usage: `topic {topic}`

* `invite` Invites the specified nick into the channel.
Usage: `invite {nick}`

* `ak` Adds the specified nick to the auto kick list.
Usage: `ak {nick} [{reason}]`

* `ab` Adds the specified nick to the auto ban list.
Usage: `ab {nick} [{reason}]`

* `akb` Adds the specified nick to the auto kick-ban list.
Usage: `akb {nick} [{reason}]`

* `deak` Removes the specified nick to the auto kick list.
Usage: `deak {nick} [{reason}]`

* `deab` Removes the specified nick to the auto ban list.
Usage: `deab {nick} [{reason}]`

* `deakb` Removes the specified nick to the auto kick-ban list.
Usage: `deakb {nick} [{reason}]`

* `hop` Sets the nick as Half Op.
Usage: `hop {nick}`

* `dehop` Removes the nick as Half Op.
Usage: `dehop {nick}`

* `b` Bans the specified nick.
Usage: `b {nick}`

* `ub` Unbans the specified nick.
Usage: `ub {nick}`

* `clearban` Clears all the bans in the channel.
Usage: `clearban`

* `kb` Bans and then Kicks the specified nick.
Usage: `kb {nick} [{reason}]`

* `tb` Bans the specified nick for the amount of time specified.
Usage: `tb {ban_time} {nick} [{reason}]`

* `tkb` Bans and then Kicks the specified nick for the amount of time specified.
Usage: `tkb {ban_time} {nick} [{reason}]`

* `k` Kicks the nick from the channel.
Usage: `k {nick} [{reason}]`

* `voice` Sets the nick as Voiced.
Usage: `voice {nick}`

* `devoice` Removes the nick as Voiced.
Usage: `devoice {nick}`

* `kme` Kicks the requesting nick from the channel.
Usage: `kme`

### Owner

* `owner` Identifies the nick as the Bot's Owner.
Usage: `owner {password}`

* `addowner` Adds the defined nick as an owner.
Usage: `addowner {nick}`

* `delowner` Removes the defined nick from the owners list.
Usage: `delowner {nick}`

* `nick` Changes the Bot's nickname to the one specified.
Usage: `nick {new_nick}`

* `id` Has the Bot identify to nickserv.
Usage: `id`

* `join` Tells the Bot to join the specified channel.
Usage: `join {channel}`

* `part` Tells the Bot to part the channel.
Usage: `part [{channel}]`

* `say` Has the Bot say the specified message to the channel.
Usage: `say [{channel}] {message}`

* `action` Displays the specified message as an action  in the channel.
Alternate Commands: `me`
Usage: `action [{channel}] {message}`

* `query` Private messages the specified nick.
Usage: `query {nick} {message}`

* `quit` Quits the server instance.
Usage: `quit`

* `quitall` Quits all of the connected server instances.
Usage: `quitall`

* `restart` Restarts the server instance.
Usage: `restart`

* `restartall` Restarts all of the connected server instances.
Usage: `restartall`

* `ignore` Adds the specified nick/chan to the ignore list.
Usage: `ignore {nick|channel}`

* `unignore` Removes the specified nick/chan from the ignore list.
Usage: `unignore {nick|channel}`

* `ignoremodule` Adds the specified nick/chan to a modules ignore list.
Usage: `ignoremodule {module} {nick|chan}`

* `unignoremodule` Removes the specified nick/chan from a modules ignore list.
Usage: `unignoremodule {module} {nick|chan}`

* `ignorecmd` Adds the specified nick/chan to a commands ignore list.
Usage: `ignorecmd {command} {nick|chan}

* `unignorecmd` Removes the specified nick/chan from a commands ignore list.
Usage: `unignorecmd {command} {nick|chan}`

* `blacklist` Adds the specified channel to the bot blacklist.
Usage: `blacklist {channel}`

* `unblacklist` Removes the specified channel from the bot blacklist.
Usage: `unblacklist {channel}`

* `update` Updates the Modules and Configurations on all server instances.
Usage: `update`

* `modules` Displays the loaded modules.
Usage: `modules`

* `loadmodule` Loads the specified module into the bot.
Aliases: `load`, `addmodule`
Usage: `loadmodule {module_class_name}`

* `delmodule` Unloads the specified module from the bot.
Aliases: `unload`, `unloadmodule`
Usage: `delmodule {module_class_name}`

* `addchan`Invites the bot to a specified channel.
Usage: `addchan {channel}`

* `addchanlist` Adds the specified channel to the auto-join list.
Usage: `addchanlist {channel}`

* `delchanlist` Removes the specified channel to the auto-join list.
Usage: `delchanlist {channel}`

* `channels` Lists the channels the bot is in on that server.
Usage: `channels`

* `servers` Lists the servers the bot is connected to.
Usage: `servers`

* `conf` Lists the bot's current configuration settings.
Usage: `conf [module_config]`

* `resources` Displays the current CPU and RAM used by the bot.
Usage: `resources`

* `clear` Kicks everyone from the channel except the initiator and the bot.
Usage: `clear [{channel}]`

### Help

* `help` Displays the modules available to the nick.  Viewing a specific module shows the commands.  Viewing a specific command shows the command properties.
Usage: `help [{module}] [{command}]`

### Rules

* `rules` Displays the channel rules.
Usage: `rules`

* `addrule` Adds the specified rule to the end of the channel rules.
Usage: `addrule {rule}`

* `delrule` Removes the specified rule from the channel rules.
Usage: `delrule {rule_number}`

### Messaging

* `message` Leaves a message for the specified nick.  Will be delivered once the nick joins or speaks in a channel the bot is in.
Aliases: `msg`
Usage: `message {nick} {message}`

* `anonmessage` Leaves a message for the specified nick without displaying the sender's nick.  Will be delivered once the nick joins or speaks in a channel the bot is in.
Aliases: `amsg`, `anonmsg`
Usage: `anonmessage {nick} {message}`

### Intro

* `intro` Adds a personal greeting for whenever you enter the channel.
Usage: `intro {greeting_1|greeting_2}`

* `introdelete` Deletes yoru itnroductions for that channel.
Aliases: `delintro`, `deleteintro`
Usage: `introdelete`

### Quote

* `quote` Displays a random quote from the channel.  If a nick is specified, it will get a quote from that nick.
Usage: `quote [{nick}]`

### Weather

* `weather` Displays the current weather conditions for the specified city.
Aliases: `w`
Usage: `weather {zip_code|city_name}`

* `forecast` Displays the weather forecast for the specified city.
Aliases: `f`
Usage: `forecast {zip_code|city_name}`

### Search

* `google` Displays the first result from Google.
Aliases: `g`
Usage: `google {query}`

### 8-Ball

* `8ball` Answers any yes/no question.
Usage: `8ball {question}`

### HBomb

* `hbomb` Initiates a new game of pass the HBomb.
Usage: `hbomb`

* `pass` Passes the HBomb to the specified nick.
Usage: `pass {nick}`

* `lock_bomb` Locks the bomb to the current holder, or passes it and then locks it.
Aliases: `lb`
Usage: `lock_bomb [{nick}]`

* `unlock_bomb` Unlocks the bomb from the current holder.
Aliases: `unlb`
Usage: `unlock_bomb`

* `set_bomb` Sets the bomb holder to the specified nick.
Aliases: `sb`
Usage: `set_bomb {nick}`

* `detonate` Detonates the current active bomb.
Usage: `detonate`

* `stop_bomb` Stops the bomb without it blowing up.
Usage: `stop_bomb`

* `defuse` Cuts a wire of the HBomb.  If it is the correct wire, it will defuse the bomb and kick the previous holder.  If not, it will detonate.
Usage: `defuse {wire_color}`

### Fun

* `love` Sends some love to the specified nick or requesting nick.
Usage: `love [{nick}]`

* `hug` Sends a hug to the specified nick or requesting nick.
Usage: `hug [{nick}]`

* `slap` Slaps the specified nick or requesting nick.
Usage: `slap [{nick}]`

* `bots` Checks in to the channel.
Usage: `bots`

* `br` Responds with HUEHUEHUE.
Usage: `br`

* `net` Talks about the quality .NET Framework.
Usage: `net`

### Response

* `addresponse` Adds a new response to the dictionary.
Usage: `addresponse {allowed_chan}[,{allowed_chan}]:{trigger}[|{trigger}]:{response}[|[response}]`

* `delresponse` Removes the specified response from the dictionary.
Usage: `delresponse {response_number}`

* `listresponse` Lists all of the responses in the dictionary.
Usage: `listresponse`

### Chat

* `stopchat` Stops the bot from chatting in that channel.
Usage: `stopchat`

### Poll

* `poll` Creates a poll in the channel.
Usage: `poll {question}|{answer}[|{answer}...]`

* `addanswer` Adds an answer to the active poll.
Usage: `addanswer {answer}`

* `delanswer` Removes the specified answer from the active poll.
Usage: `delanswer {answer_number}`

* `stoppoll` Stops the active poll and displays the results.
Usage: `stoppoll`

* `results` Displays the results of the current poll.
Usage: `results`

* `vote` Votes for the specified answer.
Usage: `vote {answer_number}`

### Roll Call

* `rollcall` Displays every nick in the channel.
Usage: `rollcall [{channel}]`

### Version Response

* `ver` Sends a version request and displays the response.
Usage: `ver {nick}`

### Idle

* `idle` Sets you as idle.  Protects from certain games and actions.
Usage: `idle`

* `deidle` Returns you from idle.
Usage: `deidle`

### Roll Dice

* `roll` Rolls a specified number of dice with certain s amount of sides and displays the results.
Usage: `roll [{number_of_dice}] [{number_of_sides}]`

### Wolfram Alpha

* `wa` Returns the results of your query from Wolfram Alpha.
Usage: `wa {query}`

### GitHub

* `bug` Creates a new issue with a ''Bug'' label.
Usage: `bug {title}[|{description}]`

* `request` Creates a new issue with a ''Issue'' label.
Usage: `request {title}[|{description}]`

### About

* `about` Displays the bots current version, creator, and owners.
Usage: `about`

* `uptime` Displays how long the bot has been connected to the server.
Usage: `uptime`

* `runtime` Displays how long the bot program has been running.
Usage: `runtime`

### Logging

* `last` Displays information of the usage of specified commands.
Usage: `last [{command}] [{nick}] [{number_of_results}]`

### Alarms

* `alarm` Sets an alarm for the time specified.
Usage: `alarm {time} {message or command}`

### Surveys

* `survey` Starts a survey session for the requesting nick.
Usage: `survey {survey_number}`

* `surveys` Lists all available surveys, nicks who have taken the specified survey, or answers of the specified nick for the specified survey.
Usage: `surveys [{survey_number}] [{nick}]`

* `nextquestion` Submits your entered text as an answer and displays the next question.
Usage: `nextquestion`

* `addsurvey` Begins the add new survey wizard.
Usage: `addsurvey {survey_access_level} {survey_name}`

* `delsurvey` Deletes the specified survey.
Usage: `delsurvey {survey_number}`

* `finishsurvey` Finalizes the submitted survey and adds it to the list.
Usage: `finishsurvey`

*  `cancelsurvey` Cancels the current survey and erases the answers.
Usage: `cancelsurvey`

* `addsurveyowner` Adds an owner to the specified survey.
Usage: `addsurveyowner {survey_number} {new_owner}[,{new-owner}]`

* `delsurveyowner` Deletes an owner to the specified survey.
Usage: `delsurveyowner {survey_number} {owner}[,{owner}]`

## Bugs/Feature Requests

Please report all bugs you find to me so I can fix them as soon as possible.  Also if you have any feature requests, feel free to send them to me as well.

## Contact Info

Email: admin@inb4u.com
IRC: (irc.inb4u.com)#IRCBot
Nick: Uncled1023

## Acknowledgements

Cameron Lucas