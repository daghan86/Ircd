using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;


namespace Ircd
{
    internal class ClientSession
    {

        private readonly IrcCommandHandler _handler; 
        private readonly List<IRCClient> _clients;
        public ClientSession(IrcCommandHandler handler, List<IRCClient> clients)
        {
            _handler = handler;
            _clients = clients;
        }


        public async Task RunAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            byte[] buffer = new byte[1024];



            // Find or create IRCClient object
            var ircClient = _clients.Find(c => c.TcpClient == client);
            if (ircClient == null)
            {
                ircClient = new IRCClient(client); // Make sure IRCClient stores TcpClient
                ircClient.IsRegistered = false;
                _clients.Add(ircClient);
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

                leftover.Clear();


                if (!allData.EndsWith("\n"))
                {

                    leftover.Append(lines[^1]);
                    lines = lines[..^1];
                }



                //foreach (var line in data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)) {
                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0) continue;
                    Console.WriteLine($"HeiHei: {line}");

                    if (rawLine.StartsWith("PING", StringComparison.OrdinalIgnoreCase))
                    {
                        string server = line.Substring(4); // skip "PING "
                        await SendMessage(ircClient, $"PONG {server}");
                        continue;
                    }



                    // Pass message to IRC handler
                    await _handler.HandleIRCMessage(ircClient, rawLine);

                }




            }


            client.Close();
        }


        private async Task SendMessage(IRCClient client, string message)
        {
            var stream = client.TcpClient.GetStream();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(message + "\r\n");
            await stream.WriteAsync(data, 0, data.Length);
        }


    }
}
