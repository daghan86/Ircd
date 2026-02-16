using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Ircd
{


    public class IRCClient
    {
        public TcpClient TcpClient { get; } // The TCP connection
        public string? Nickname { get; set; } // The user’s nickname
        public string? Username { get; set; } // The IRC USER name
        //public string? CurrentChannel { get; set; } // The channel the user has joined
        //public HashSet<IRCChannel> Channels { get; } = new HashSet<IRCChannel>();
        public List<IRCChannel> Channels { get; } = new List<IRCChannel>();
        public string? Realname { get; set; }  // <- Add this
        public bool IsRegistered { get; set; }

        public IRCClient(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }


        public async Task SendMessage(string message)
        {
            var stream = TcpClient.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message + "\r\n");
            await stream.WriteAsync(data, 0, data.Length);
        }


    }

}
