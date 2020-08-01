using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Vellum.Automation
{

    // tomrhollis addition to make use of ElementZero chat functionality
    // passes messages to other EZ BDS servers and discord
    // TODO: * poll other servers for player count & list

    public class ChatManager : Manager
    {
        private ProcessManager _bds; 
        public RunConfiguration RunConfig;
        private DiscordBot _discord;

        // initialize chat manager
        public ChatManager(ProcessManager p, RunConfiguration runConfig)
        {
            _bds = p;
            RunConfig = runConfig;
            _discord = null;
            if (RunConfig.ChatSync.EnableDiscord) this.StartDiscord();
        }

        public void Stop()
        {
            if (RunConfig.ChatSync.ServerStatusMessages)
                this.BroadcastMessage(String.Format("(§l§a{0}§r): ", RunConfig.WorldName), "Going down for backup").GetAwaiter().GetResult();
            if (RunConfig.ChatSync.EnableDiscord)
                this.StopDiscord().GetAwaiter().GetResult();
        }

        public async Task StartDiscord()
        {
            _discord = new DiscordBot(this, RunConfig);
            await Task.Run(() => _discord.RunBotAsync());
        }

        public async Task StopDiscord()
        {
            await _discord.ShutDown();
        }

        // receive a connect message from this server, process then broadcast it
        public void UserConnect(MatchCollection line)
        {
            Regex r = new Regex(@": (.+),");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();

            Task.Run(async delegate {
                await BroadcastMessage(String.Format("(§l§a{0}§r) ", RunConfig.WorldName), String.Format("§e{0}§r connected", user));
            });
        }

        // receive a disconnect message from this server, process then broadcast it
        public void UserDisconnect(MatchCollection line)
        {
            Regex r = new Regex(@": (.+)[,]");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();

            Task.Run(async delegate {
                await BroadcastMessage(String.Format("(§l§a{0}§r) ", RunConfig.WorldName), String.Format("§e{0}§r left", user));
            });

         }

        // receive a chat message from this server, process then broadcast it
        public void SendChat(MatchCollection line)
        {
            Regex r = new Regex(@"\[.+\].+\[(.+)\] (.+)");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();
            string chat = m.Groups[2].ToString().Trim();
            if (chat.StartsWith('.'))
            {
                return; // don't transmit dot commands
            }
            string prefix = String.Format("(§l§a{0}§r) [§e{1}§r] ", RunConfig.WorldName, user);

            Task.Run(async delegate { await BroadcastMessage(prefix, chat); });
        }

        public void DiscordMessageToMC(string message)
        {
            _bds.SendTellraw(message, "(§b§lDiscord§r) ");
        }

        // broadcast a message to the other servers & discord if applicable
        public async Task BroadcastMessage(string prefix, string chat)
        {
            if (RunConfig.ChatSync.EnableDiscord && _discord != null)
            {
                _discord.SendMessage(prefix + chat);
            }

            try
            {
                HttpClient client = new HttpClient();
                prefix = Regex.Replace(prefix, @"§", "\\u00a7");
                prefix = Regex.Replace(prefix, @"""", "\\\"");
                chat = Regex.Replace(chat, @"§", "\\u00a7");
                chat = Regex.Replace(chat, @"""", "\\\"");
                string message = "tellraw @a {\"rawtext\":[{\"text\":\"" + prefix + chat + "\"}]}";
                string address = "http://{0}:{1}/map/{2}/execute_command";
                    
                foreach (string server in RunConfig.ChatSync.OtherServers)
                {
                    StringContent content = new StringContent(message);
                    client.PostAsync(String.Format(address, RunConfig.ChatSync.BusAddress, RunConfig.ChatSync.BusPort, server), content);
                }
            }
            catch (Exception e)
            {
                Log("Something went wrong with the bus: " + e.Message);
            }
        }
    }
}