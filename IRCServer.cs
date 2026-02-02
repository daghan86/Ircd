using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ircd
{


    class IRCServer
    {
        private TcpListener listener;

        // Dictionary of all channels (key = channel name)
        private Dictionary<string, IRCChannel> Channels { get; } = new Dictionary<string, IRCChannel>();
        private List<IRCClient> Clients { get; } = new List<IRCClient>();
        public IRCServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        string serverName = "MyIRCD";

        public async Task StartAsync()
        {
            listener.Start();
            Console.WriteLine("IRC Server started...");


            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client); // fire-and-forget
            }
        }


        private async Task HandleClientAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            byte[] buffer = new byte[1024];



            // Find or create IRCClient object
            var ircClient = Clients.Find(c => c.TcpClient == client);
            if (ircClient == null)
            {
                ircClient = new IRCClient(client); // Make sure IRCClient stores TcpClient
                ircClient.IsRegistered = false;
                Clients.Add(ircClient);
            }



            int bytesRead;

            StringBuilder leftover = new StringBuilder();


            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                //string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                //Console.WriteLine($"Received: {message}");


                // Echo back for now
                /*
                 byte[] response = Encoding.UTF8.GetBytes(message + "\r\n");
                await stream.WriteAsync(response, 0, response.Length);*/
                // Handle PING immediately


                leftover.Append(data);

                string allData = leftover.ToString();
                string[] lines = allData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                if (!allData.EndsWith("\n"))
                {

                    leftover.Append(lines[^1]);
                    lines = lines[..^1];
                }



                //foreach (var line in data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)) {
                  foreach (var rawLine in lines) {
                        string line = rawLine.Trim();
                        if (line.Length == 0) continue;
                        Console.WriteLine($"HeiHei: {line}"); 

                    if (rawLine.StartsWith("PING"))
                    {
                        string server = line.Substring(5); // skip "PING "
                        await SendMessage(ircClient, $"PONG {server}");
                        continue;
                    }



                    // Pass message to IRC handler
                    await HandleIRCMessage(ircClient, rawLine);

                }




            }


            client.Close();
        }


        private async Task HandleIRCMessage(IRCClient client, string message)
        {
            // Split the message into command and parameters
            //string[] parts = message.Split(' ', 2);
            string[] parts = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string command = parts[0].Trim().ToUpper();
            string parameters = parts.Length > 1 ? parts[1].Trim() : "";


            bool handled = false;
            Console.WriteLine($"Current Command: {command}");
            switch (command)
            {
                case "NICK":
                    client.Nickname = parameters;
                    Console.WriteLine($"NickaneM: {client.Nickname}");
                    handled = true;
                    break;
                case "USER":
                    var userParts = parameters.Split(' ', 4);
                    client.Username = userParts.Length > 0 ? userParts[0] : "";
                    client.Realname = userParts.Length > 3 ? userParts[3].TrimStart(':') : "";
                    //client.Username = parameters;
                    handled = true;
                    break;
            }




            switch (command) { 
                case "JOIN":
                    if (string.IsNullOrEmpty(client.Nickname) || string.IsNullOrEmpty(client.Username))
                    {
                        await SendMessage(client, $":MyIRCD 451 * :You must set NICK and USER first");
                    }
                    else
                    {
                        await JoinChannel(client, parameters);
                        handled = true;
                    }
                    break;
                case "PRIVMSG":
                    await HandlePrivMsg(client, parameters);
                    handled = true;
                    break;
                case "CAP":
                    // CAP LS -> respond with CAP * LS :... (or just CAP ACK LS ...)
                    await SendMessage(client, "CAP * LS :multi-prefix"); // minimal
                    handled = true;
                    break;
            }


            if (!handled)
            {
                //await SendMessage(client, $":{serverName} 421 {client.Nickname} {command} :Unknown command");
                await SendMessage(client, $":{serverName} 421 * {command} :Unknown command");
            }


            bool testbool = !client.IsRegistered && !string.IsNullOrEmpty(client.Nickname) && !string.IsNullOrEmpty(client.Username);
            bool bIsRegistered = !client.IsRegistered;
            bool bNickname = !string.IsNullOrEmpty(client.Nickname);
            bool buserName = !string.IsNullOrEmpty(client.Username);
            Console.WriteLine($"{testbool}");
            Console.WriteLine($"{bIsRegistered}");
            Console.WriteLine($"{bNickname}");
            Console.WriteLine($"{buserName}");

            if (!client.IsRegistered && !string.IsNullOrEmpty(client.Nickname) && !string.IsNullOrEmpty(client.Username))
            {
                // Welcome messages mIRC expects
                await SendMessage(client, $":{serverName} 001 {client.Nickname} :Welcome to {serverName}, {client.Nickname}");
                await SendMessage(client, $":{serverName} 002 {client.Nickname} :Your host is {serverName}");

                Console.WriteLine($"Sending 001 welcome to {client.Nickname}");
                Console.WriteLine($":{serverName} 001 {client.Nickname} :Welcome to {serverName}, {client.Nickname}");
                Console.WriteLine($":{serverName} 002 {client.Nickname} :Your host is {serverName}");


                await SendMessage(client, $":{serverName} 003 {client.Nickname} :This server was created 02/02/2026"); // date can be anything
                await SendMessage(client, $":{serverName} 004 {client.Nickname} {serverName} v1.0 iowghraAbcFz");


                client.IsRegistered = true;
            }


        }

        public async Task JoinChannel(IRCClient client, string channelName)
        {


           

            if (!channelName.StartsWith("#"))
                channelName = "#" + channelName;


            if (!Channels.TryGetValue(channelName, out var channel))
            {
                channel = new IRCChannel(channelName);
                Channels[channelName] = channel;
            }


            if (!channel.Clients.Contains(client))
            {
                channel.Clients.Add(client);




                // Send JOIN
                await SendMessage(client, $":{client.Nickname}!{client.Username}@{serverName} JOIN :{channelName}");


                // No topic
                await SendMessage(client, $":{serverName} 331 {client.Nickname} {channelName} :No topic is set");


                // Names list
                await SendMessage(client, $":{serverName} 353 {client.Nickname} = {channelName} :{string.Join(" ", channel.Clients.Select(c => c.Nickname))}");


                // End of names
                await SendMessage(client, $":{serverName} 366 {client.Nickname} {channelName} :End of /NAMES list");


                foreach (var c in channel.Clients)
                {
                    if (c != client)
                    {
                        await SendMessage(c, $":{client.Nickname}!{client.Username}@{serverName} JOIN {channelName}");
                    }
                }

            }

            //client.CurrentChannel?.Clients.Remove(client);
            client.CurrentChannel = channelName;


            //duplicate... channel.Clients.Add(client);
        }


        private async Task HandlePrivMsg(IRCClient client, string parameters)
        {

            int firstSpace = parameters.IndexOf(' ');
            if (firstSpace == -1) return; // Invalid PRIVMSG format

            string target = parameters.Substring(0, firstSpace);
            string message = parameters.Substring(firstSpace + 1);

            // If message starts with ":", remove it (IRC protocol)
            if (message.StartsWith(":")) message = message[1..];


            // Check if target is a channel
            if (Channels.TryGetValue(target, out var channel))
            {
                // Send to all clients in the channel
                foreach (var c in channel.Clients)
                {

                    // skip sender 
                    if (c != client) { // skip sender 
                        await SendMessage(c, $":{client.Nickname}!{client.Username}@{serverName} PRIVMSG {target} :{message}");
                    }
                }


            }


            else
            {
                // Target might be a single user
                var c = Clients.Find(c => c.Nickname == target);
                if (c != null)
                {
                    // old await SendMessage(c, $"{client.Nickname}: {message}");
                    await SendMessage(c, $":{client.Nickname}!{client.Username}@{serverName} PRIVMSG {target} :{message}");
                }
            }

        }

        private async Task SendMessage(IRCClient client, string message)
        {
            var stream = client.TcpClient.GetStream();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message + "\r\n");
            await stream.WriteAsync(data, 0, data.Length);
        }


    }
}
