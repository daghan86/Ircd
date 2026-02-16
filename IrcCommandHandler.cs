using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;


namespace Ircd
{
    internal class IrcCommandHandler
    {
        private readonly Dictionary<string, IRCChannel> _channels;
        private readonly List<IRCClient> _clients;
        private readonly string _serverName;

        public IrcCommandHandler(Dictionary<string, IRCChannel> channels, List<IRCClient> clients, string serverName)
        {
            _channels = channels;
            _clients = clients;
            _serverName = serverName;
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
            if (_channels.TryGetValue(target, out var channel))
            {
                // Send to all clients in the channel
                foreach (var c in channel.Clients)
                {

                    // skip sender 
                    if (c != client)
                    { // skip sender 
                        await c.SendMessage($":{client.Nickname}!{client.Username}@{_serverName} PRIVMSG {target} :{message}");
                    }
                }


            }


            else
            {
                // Target might be a single user
                var c = _clients.Find(c => c.Nickname == target);
                if (c != null)
                {
                    // old await SendMessage(c, $"{client.Nickname}: {message}");
                    await c.SendMessage($":{client.Nickname}!{client.Username}@{_serverName} PRIVMSG {target} :{message}");
                }
            }

        }



        public async Task HandleIRCMessage(IRCClient client, string message)
        {
            // Split the message into command and parameters
            //string[] parts = message.Split(' ', 2);
            Console.Write($"HandleIRCMessage received: {message}");
            string[] parts = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string command = parts[0].Trim().ToUpper();
            string parameters = parts.Length > 1 ? parts[1].Trim() : "";


            bool handled = false;
            Console.WriteLine($"Current Command: {command}");
            switch (command)
            {
                case "NICK":

                    var oldNick = client.Nickname;
                    var newNick = parameters;

                    if (_clients.Any(c => c != client && string.Equals(c.Nickname, newNick, StringComparison.OrdinalIgnoreCase)))
                    {
                        await client.SendMessage($":{_serverName} 433 * {newNick} :Nickname is already in use");
                        handled = true;
                        break;
                    }

                    if (string.IsNullOrEmpty(oldNick)) break;

                    var nickMsg = $":{oldNick}!{client.Username}@{_serverName} NICK :{newNick}";

                    client.Nickname = newNick;

                    foreach (var channel in client.Channels)
                    {
                        foreach (var c in channel.Clients) { 
                            if (c != client)
                            {
                                await c.SendMessage(nickMsg);
                            }
                        }
                    }
                    await client.SendMessage(nickMsg);

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




            switch (command)
            {
                case "JOIN":
                    if (string.IsNullOrEmpty(client.Nickname) || string.IsNullOrEmpty(client.Username))
                    {
                        await client.SendMessage($":MyIRCD 451 * :You must set NICK and USER first");
                        handled = true;
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
                    await client.SendMessage("CAP * LS :multi-prefix"); // minimal
                    handled = true;
                    break;
            }


            if (!handled)
            {
                //await SendMessage(client, $":{serverName} 421 {client.Nickname} {command} :Unknown command");
                await client.SendMessage($":{_serverName} 421 * {command} :Unknown command");
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
                await client.SendMessage($":{_serverName} 001 {client.Nickname} :Welcome to {_serverName}, {client.Nickname}");
                await client.SendMessage($":{_serverName} 002 {client.Nickname} :Your host is {_serverName}");

                Console.WriteLine($"Sending 001 welcome to {client.Nickname}");
                Console.WriteLine($":{_serverName} 001 {client.Nickname} :Welcome to {_serverName}, {client.Nickname}");
                Console.WriteLine($":{_serverName} 002 {client.Nickname} :Your host is {_serverName}");


                await client.SendMessage($":{_serverName} 003 {client.Nickname} :This server was created 02/02/2026"); // date can be anything
                await client.SendMessage($":{_serverName} 004 {client.Nickname} {_serverName} v1.0 iowghraAbcFz");


                client.IsRegistered = true;
            }




        }




        public async Task JoinChannel(IRCClient client, string channelName)
        {


            channelName = channelName.Trim();
            if (channelName.StartsWith(":"))
                channelName = channelName[1..];


            if (!channelName.StartsWith("#"))
                channelName = "#" + channelName;


            if (!_channels.TryGetValue(channelName, out var channel))
            {
                channel = new IRCChannel(channelName);
                _channels[channelName] = channel;
            }


            if (!channel.Clients.Contains(client))
            {
                channel.Clients.Add(client);
                client.Channels.Add(channel);




                // Send JOIN
                await client.SendMessage($":{client.Nickname}!{client.Username}@{_serverName} JOIN :{channelName}");


                // No topic
                await client.SendMessage($":{_serverName} 331 {client.Nickname} {channelName} :No topic is set");


                // Names list
                await client.SendMessage($":{_serverName} 353 {client.Nickname} = {channelName} :{string.Join(" ", channel.Clients.Select(c => c.Nickname))}");


                // End of names
                await client.SendMessage($":{_serverName} 366 {client.Nickname} {channelName} :End of /NAMES list");


                foreach (var c in channel.Clients)
                {
                    if (c != client)
                    {
                        await c.SendMessage($":{client.Nickname}!{client.Username}@{_serverName} JOIN :{channelName}");
                    }
                }

            }

            //client.CurrentChannel?.Clients.Remove(client);
            client.Channels.Add(channel);


            //duplicate... channel.Clients.Add(client);
        }




    }
}
