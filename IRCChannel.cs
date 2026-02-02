using System;
using System.Collections.Generic;
using System.Text;

namespace Ircd
{
    class IRCChannel
    {
        public string Name { get; }
        public List<IRCClient> Clients { get; } = new List<IRCClient>();


        // old public IRCChannel(string name) => Name = name;
        public IRCChannel? CurrentChannel { get; set; }

        public IRCChannel(string name)
        {
            Name = name;
        }
    }



}


