using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Vellum.Automation
{

    // tomrhollis addition to make use of ElementZero chat functionality
    // right now, simply passes messages to the other servers
    // TODO: * include broadcast to discord
    //       * poll other servers for player count & list

    public class ChatManager : Manager
    {
        private ProcessManager _bds; // not used now, but will be later
        public RunConfiguration RunConfig;

        // initialize chat manager with the address and port of the EZ bus
        public ChatManager(ProcessManager p, RunConfiguration runConfig)
        {
            _bds = p;
            RunConfig = runConfig;
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


        // broadcast a message to the other servers
        public void broadcastMessage(string message)
        {
            try 
            {
                HttpClient client = new HttpClient();
                StringContent content = new StringContent("say " + message);
                string addressBase = "http://{0}:{1}/map/{2}/execute_command";
                string address = "";

                foreach (string server in RunConfig.ChatSync.OtherServers)
                {
                    address = String.Format(addressBase, RunConfig.ChatSync.BusAddress, RunConfig.ChatSync.BusPort, server);
                    client.PostAsync(address, content).Result.Content.ReadAsStringAsync();
                }

                // if (RunConfig.ChatSync.EnableDiscord) goes here

            } 
            catch (Exception e) 
            {
                Log("Something went wrong with the bus: " + e.Message);
            }

        }
    }
}