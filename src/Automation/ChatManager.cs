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
                this.BroadcastMessage(String.Format("{0} is shutting down", RunConfig.WorldName)).GetAwaiter().GetResult();
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
                await BroadcastMessage(String.Format("({0}) {1} connected", RunConfig.WorldName, user));
            });
        }

        // receive a disconnect message from this server, process then broadcast it
        public void UserDisconnect(MatchCollection line)
        {
            Regex r = new Regex(@": (.+)[,]");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();

            Task.Run(async delegate {
                await BroadcastMessage(String.Format("({0}) {1} left", RunConfig.WorldName, user));
            });

         }

        // receive a chat message from this server, process then broadcast it
        public void SendChat(MatchCollection line)
        {
            Regex r = new Regex(@"\[.+\].+\[(.+)\] (.+)");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();
            string chat = m.Groups[2].ToString();
            if (chat.StartsWith('.'))
            {
                return; // don't transmit dot commands
            }
            string message = String.Format("({0}) [{1}] {2}", RunConfig.WorldName, user, chat);

            message = message.Trim();
            Task.Run(async delegate { await BroadcastMessage(message); });
        }

        public void PostMessage(string message)
        {
            _bds.SendTellraw(message, "");
        }

        // broadcast a message to the other servers & discord if applicable
        public async Task BroadcastMessage(string message)
        {
            await Task.Run(() =>
            {
                if (RunConfig.ChatSync.EnableDiscord && _discord != null)
                {
                    _discord.SendMessage(message);
                }

                try
                {
                    HttpClient client = new HttpClient();
                    StringContent content = new StringContent("say " + message);
                    string address = "http://{0}:{1}/map/{2}/execute_command";
                    
                    foreach (string server in RunConfig.ChatSync.OtherServers)
                    {
                        client.PostAsync(String.Format(address, RunConfig.ChatSync.BusAddress, RunConfig.ChatSync.BusPort, server), content);
                    }
                }
                catch (Exception e)
                {
                    Log("Something went wrong with the bus: " + e.Message);
                }
            });
        }
    }
}