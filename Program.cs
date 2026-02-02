// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Ircd.IRCServer ircd = new Ircd.IRCServer(6667);

await ircd.StartAsync(); // Waits here until the server stops