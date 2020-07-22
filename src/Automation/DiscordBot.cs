using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Vellum.Automation
{
    // tomrhollis: Discord bot for Vellum + ElementZero

    class DiscordBot
    {
        private DiscordSocketClient _client;
        private ChatManager _chatMgr;
        private RunConfiguration RunConfig;
        private IMessageChannel Channel;

        public DiscordBot(ChatManager cm, RunConfiguration rc)
        {
            _chatMgr = cm;
            RunConfig = rc;
        }

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _client.MessageReceived += HandleMessage;

            await _client.LoginAsync(TokenType.Bot, RunConfig.ChatSync.DiscordToken);

            Channel = (ITextChannel)_client.GetChannel(RunConfig.ChatSync.DiscordChannel);

            await _client.StartAsync();           

            await Task.Delay(-1);
        }

        private async Task HandleMessage(SocketMessage arg)
        {
            Regex userRegex = new Regex(@"<@[!]?(\d+)>");
            Regex channelRegex = new Regex(@"<#(\d+)>");

            // set up the channel connection
            if (Channel == null) Channel = (ITextChannel)_client.GetChannel(RunConfig.ChatSync.DiscordChannel);

            var message = arg as SocketUserMessage;
            string msgText = message.Content;
            
            if (message.Author.IsBot || message.Channel.Id != RunConfig.ChatSync.DiscordChannel) return;

            // find user and channel mention codes
            MatchCollection userMatches = userRegex.Matches(msgText);
            MatchCollection channelMatches = channelRegex.Matches(msgText);

            // replace each userID strings with @Name
            foreach (Match um in userMatches)
            {
                ulong user = ulong.Parse(um.Groups[1].ToString());
                msgText = Regex.Replace(msgText, um.Value, "@"+(await Channel.GetUserAsync(user)).Username);
            }
            
            // replace channelID strings with #name
            foreach (Match cm in channelMatches)
            {
                ulong channelId = ulong.Parse(cm.Groups[1].ToString());
                var channel = _client.GetChannel(channelId) as SocketTextChannel;  
                
                string name = channel?.Name;
                msgText = Regex.Replace(msgText, cm.Value, "#"+name);
            }

            // post on this server (other servers will handle discord themselves)
            _chatMgr.PostMessage(String.Format("(Discord) [{0}] {1}", message.Author.Username, msgText));
        }
        
        public async Task SendMessage(string msg)
        {
            string message = Regex.Replace(msg, @"§[0-9a-z]", ""); // strip minecraft formatting

            // set up to do the find and replace for discord mentions
            Regex userRegex = new Regex(@"@<([^>]+)>");
            Regex channelRegex = new Regex(@"#([-a-z]+)", RegexOptions.IgnoreCase);
            MatchCollection userMatches = userRegex.Matches(message);
            MatchCollection channelMatches = channelRegex.Matches(message);
            var location = _client.GetChannel(RunConfig.ChatSync.DiscordChannel) as SocketTextChannel;

            // replace @<username> with a mention string
            foreach (Match um in userMatches)
            {
                var users = location.Guild.Users.Where(user => user.Username.ToLower() == um.Groups[1].Value.ToLower());
                string userMention = null;

                if (users.Count() > 0)
                {
                    userMention = users.First().Mention;
                    message = Regex.Replace(message, "@<" + um.Groups[1].Value + ">", userMention);
                }
            }

            // replace #channel-name with a mention string
            foreach (Match cm in channelMatches)
            {
                var channels = location.Guild.Channels.Where(channel => channel.Name == cm.Groups[1].Value.ToLower());
                ulong? channelId=null;

                if (channels.Count() > 0)
                {
                    channelId = channels.First().Id;
                    message = Regex.Replace(message, "#" + cm.Groups[1].Value, "<#" + channelId + ">");
                }
            }
                        
            // send
            await location.SendMessageAsync(message);
        }

        public async Task ShutDown()
        {
            await _client.LogoutAsync();

            await _client.StopAsync();
        }
    }
}
