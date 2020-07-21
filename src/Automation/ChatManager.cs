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
        }

        public async Task startDiscord()
        {
            _discord = new DiscordBot(this, RunConfig);
            _discord.RunBotAsync();
        }

        // receive a connect message from this server, process then broadcast it
        public void userConnect(MatchCollection line)
        {
            Regex r = new Regex(@": (.+),");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();

            broadcastMessage(String.Format("(§b§l{0}§r) §e{1}§r connected", RunConfig.WorldName, user));
        }

        // receive a disconnect message from this server, process then broadcast it
        public void userDisconnect(MatchCollection line)
        {
            Regex r = new Regex(@": (.+)[,]");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();

            broadcastMessage(String.Format("(§b§l{0}§r) §e{1}§r left", RunConfig.WorldName, user));
        }

        // receive a chat message from this server, process then broadcast it
        public void sendChat(MatchCollection line)
        {
            Regex r = new Regex(@"\[.+\].+\[(.+)\] (.+)");
            Match m = r.Match(line[0].ToString());
            string user = m.Groups[1].ToString();
            string chat = m.Groups[2].ToString();
            if (chat.StartsWith('.'))
            {
                return; // don't transmit dot commands
            }
            string message = String.Format("(§b§l{0}§r) [§e{1}§r] {2}", RunConfig.WorldName, user, chat);

            message = message.Trim();
            broadcastMessage(message);
        }

        public void postMessage(string message)
        {
            _bds.SendTellraw(message, "");
        }

        // broadcast a message to the other servers & discord if applicable
        public void broadcastMessage(string message)
        {
            if (RunConfig.ChatSync.EnableDiscord && _discord != null)
            {
                Task.Run(async delegate
                {
                    await _discord.sendMessage(message);
                });
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
            
        }
    }
}