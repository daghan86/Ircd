using System;
using System.Collections.Generic;
using System.Text;

namespace Ircd
{
    public class IRCChannel
    {
        public string Name { get; }
        public List<IRCClient> Clients { get; } = new List<IRCClient>();
        //public HashSet<IRCChannel> Clients { get; } = new HashSet<IRCChannel>();

        // old public IRCChannel(string name) => Name = name;
        //public IRCChannel? CurrentChannel { get; set; }


        public IRCChannel(string name)
        {
            Name = name;
        }
    }



}


