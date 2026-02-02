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
        public string? CurrentChannel { get; set; } // The channel the user has joined
        public string? Realname { get; set; }  // <- Add this
        public bool IsRegistered { get; set; }

        public IRCClient(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }
    }

}
