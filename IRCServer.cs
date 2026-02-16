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

        string serverName = "MyIRCD Testserver B"; 

        public async Task StartAsync()
        {
            listener.Start();
            Console.WriteLine("IRC Server started...");

            var handler = new IrcCommandHandler(Channels, Clients, serverName);

            while (true)
            {

                TcpClient client = await listener.AcceptTcpClientAsync();

                var session = new ClientSession(handler, Clients);



                //_ = HandleClientAsync(client); // fire-and-forget
                _ = session.RunAsync(client); // fire-and-forget
            }
        }




    }
}
