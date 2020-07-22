**Vellum** is a **Minecraft: Bedrock Server** (BDS) backup and map-rendering **automation tool** by [**clarkx86**](https://github.com/clarkx86) and contributors, primarily made to create incremental backups and render interactive maps of your world using [**PapyrusCS**](https://github.com/mjungnickel18/papyruscs).

## What is this fork?
This is where I store changes to Vellum that I've made for my own use including:
* Pull requests for issues in the main repository
* Early merging-in of other people's pull requests on the main repo that would be useful to my server
* Adding functionality to Vellum to take advantage of [**ElementZero**](https://github.com/Element-0/ElementZero), a version of BDS that adds many features.

## What is different so far?
* Use ElementZero bus to sync chats between multiple servers
* Connect to a Discord bot to sync chat with a Discord channel
* Fixed notification before shutting down to render or backup
* Early merge in of [**BDS process watchdog**](https://github.com/clarkx86/vellum/pull/10) by [**bennydiamond**](https://github.com/bennydiamond)

## Configuration 
These are the settings that have been added to configuration.json:
```
KEY                  VALUE              ABOUT
----------------------------------------------------------
CHAT SETTINGS
-----------------
EnableChatSync        Boolean (!)        Enables this whole chat section

BusAddress            String  (!)        The address of the ElementZero bus that all
                                         the servers are connected to
                                      
BusPort               Integer (!)        The port where the bus is listening

OtherServers          String Array (!)   The names of all the other servers to
                                         broadcast messages to

PlayerConnMessages    Boolean (!)        Broadcasts player connect/disconnect
                                         messages

ServerStatusMessages  Boolean (!)        Broadcasts server up/down messages
                                      
EnableDiscord         Boolean (!)        Enables the discord functionality
                                         **If you're not using the bus chat sync,
                                         keep EnableChatSync true but leave
                                         OtherServers as an empty array: [ ]
                                     
DiscordToken          String (!)         The secret token for the discord bot

DiscordChannel        ULong (!)          The numerical ID for the discord channel where
                                         the bot should send the messages

-------------------
ADDITIONAL SETTINGS
-------------------
BdsWatchdog           Boolean            Watches the BDS process and tries to restart
                                         it automatically if it crashes
```

## How should I contribute?
Everyone is encouraged to contribute to the [**main repository**](https://github.com/clarkx86/vellum) over this one.  Any suggestions for features that don't require ElementZero should definitely go over there.

If you encounter any problems using this version though, please use the issue tracker here. If it turns out to be a problem with the main codebase, the issue will be closed here and directed over there.

## License?
Vellum has not selected a license at the time of writing.  I have obtained permission for what's going on here.

