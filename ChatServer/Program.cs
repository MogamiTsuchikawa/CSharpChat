using System;
using Fleck;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;


namespace ChatServer
{
    class Program
    {
        static object lockObj = new object();
        static List<Connection> connections = new List<Connection>();
        static void Main(string[] args)
        {
            var server = new Fleck.WebSocketServer("ws://0.0.0.0:8000");
            server.Start(socket =>
            {
                socket.OnOpen  = () => OnOpen(socket);
                socket.OnClose = () => OnClose(socket);
                socket.OnMessage = message => OnMessage(socket, message);
            });
            string data = "";
            while(!data.StartsWith("exit"))
            {
                data = Console.ReadLine();
            }
            foreach (var c in connections)
            {
                c.socket.Close();
            }
            server.Dispose();
        }

        static async void OnOpen(IWebSocketConnection socket)
        {
            lock (lockObj)
            {
                connections.Add(new Connection(){socket=socket});
            }
            var info = socket.ConnectionInfo;
            Console.WriteLine($"Open: {info.ClientIpAddress} {info.ClientPort}");
            
        }
        static void OnClose(IWebSocketConnection socket)
        {
            var info = socket.ConnectionInfo;
            Console.WriteLine($"Closed {info.ClientIpAddress} {info.ClientPort}");
            lock(lockObj)
            {
                connections.Remove(connections.Where(c => c.socket==socket).First());
            }
        }
        static void OnMessage(IWebSocketConnection socket, string message)
        {
            var info = socket.ConnectionInfo;
            Console.WriteLine($"Received from {info.ClientIpAddress} {info.ClientPort}: {message}");
            var connection = connections.Where(c => c.socket==socket).First();
            if(connection.room == null)
            {
                var room = JsonSerializer.Deserialize<Room>(message);
                if(room.roomName == null)return;
                connection.room = room.roomName;
            }
            else if(connection.name == null)
            {
                var connectionName = JsonSerializer.Deserialize<ConnectionName>(message);
                if(connectionName.name == null)return;
                connection.name = connectionName.name;
            }
            else
            {
                var chatMessage = JsonSerializer.Deserialize<ChatMessage>(message);
                if(chatMessage.message == null)return;
                chatMessage.fromName = connection.name;
                SendMessageToRoom(connection.room,chatMessage);
            }
        }
        static void SendMessageToRoom(string targetRoom, ChatMessage message)
        {
            string jsonText = JsonSerializer.Serialize(message);
            connections.ForEach(c => {
                c.socket.Send(jsonText);
            });
        }
    }
    
}
