# Named Pipe Wrapper Json for .NET 4.0

[![Visual Studio 2022](https://img.shields.io/badge/Visual%20Studio-2022-blue)](../..)
[![Nuke](https://img.shields.io/badge/Nuke-Build-blue)](https://nuke.build/)
[![License MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](../../actions/workflows/Build.yml/badge.svg)](../../actions)

***This project is based of Andrew C. Dvorak's work at [Named Pipe Wrapper](https://github.com/acdvorak/named-pipe-wrapper)***

A simple, easy to use, strongly-typed wrapper around .NET named pipes.

# NuGet Package

Available as a [NuGet package](https://www.nuget.org/packages/NamedPipeWrapper/).

# Features

*  Create named pipe servers that can handle multiple client connections simultaneously.
*  Send strongly-typed messages between clients and servers: any serializable .NET object can be sent over a pipe and will be automatically serialized/deserialized, including cyclical references and complex object graphs.
*  Messages are sent and received asynchronously on a separate background thread and marshalled back to the calling thread (typically the UI).
*  Supports large messages - up to 300 MiB.

# Requirements

Requires .NET 4.0 full.

# Usage

Server:

```csharp
var server = new NamedPipeServer<SomeClass>("MyServerPipe");

server.ClientConnected += delegate(NamedPipeConnection<SomeClass> conn)
    {
        Console.WriteLine("Client {0} is now connected!", conn.Id);
        conn.PushMessage(new SomeClass { Text: "Welcome!" });
    };

server.ClientMessage += delegate(NamedPipeConnection<SomeClass> conn, SomeClass message)
    {
        Console.WriteLine("Client {0} says: {1}", conn.Id, message.Text);
    };

// Start up the server asynchronously and begin listening for connections.
// This method will return immediately while the server runs in a separate background thread.
server.Start();

// ...
```

Client:

```csharp
var client = new NamedPipeClient<SomeClass>("MyServerPipe");

client.ServerMessage += delegate(NamedPipeConnection<SomeClass> conn, SomeClass message)
    {
        Console.WriteLine("Server says: {0}", message.Text);
    };

// Start up the client asynchronously and connect to the specified server pipe.
// This method will return immediately while the client runs in a separate background thread.
client.Start();

// ...
```

# MIT License

Named Pipe Wrapper for .NET is licensed under the [MIT license](LICENSE.txt).
