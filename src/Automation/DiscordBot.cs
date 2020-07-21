using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Vellum.Automation
{
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

            await _client.StartAsync();           

            await Task.Delay(-1);
        }

        private async Task HandleMessage(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message.Author.IsBot) return;  // ignore anything another server's bot connection might have posted

            // post on this server (other servers will handle discord themselves)
            _chatMgr.postMessage(String.Format("(§b§lDiscord§r) [§e{0}§r] {1}", message.Author.Username, message.Content));
        }
        
        public async Task sendMessage(string msg)
        {
            // set up the channel connection
            if (Channel == null) Channel = (ITextChannel)_client.GetChannel(RunConfig.ChatSync.DiscordChannel);

            // strip minecraft formatting & send
            await Channel.SendMessageAsync(Regex.Replace(msg, @"§[0-9a-z]", ""));
        }
    }
}
