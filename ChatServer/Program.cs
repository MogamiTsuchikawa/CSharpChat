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
            var server = new Fleck.WebSocketServer("ws://0.0.0.0:1919");
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
            var info = socket.ConnectionInfo;
            Console.WriteLine(info.Path);
            var query = info.Path.Split('?')[1];
            string name = string.Empty;
            if(query.Split('=')[0] == "name")name=query.Split('=')[1];
            string roomName = info.Path.Split('?')[0].Replace("/","");
            lock (lockObj)
            {
                connections.Add(new Connection(){socket=socket, room=roomName, name=name});
            }
            
            Console.WriteLine($"Open: {info.ClientIpAddress} {info.ClientPort} RoomName: {roomName} UserName: {name}");
            
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
            var chatMessage = JsonSerializer.Deserialize<ChatMessage>(message);
            if(chatMessage.message == null)return;
            chatMessage.fromName = connection.name;
            SendMessageToRoom(connection.room,chatMessage);
            
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
