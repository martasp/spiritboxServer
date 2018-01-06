using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;

namespace Socket.EndPoints
{
    public class MessagesEndPoint : EndPoint
    {
        public ConnectionList Connections { get; } = new ConnectionList();
        public Dictionary<string, string> ConnectionsD = new Dictionary<string, string>();
        public string temp1;
        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            Connections.Add(connection);


            if (Connections.Count % 2 == 0)
            {
                ConnectionsD.Add(connection.ConnectionId, temp1);
                ConnectionsD.Add(temp1, connection.ConnectionId);
            }
            else
            {
                temp1 = connection.ConnectionId;
            }


            await SentDataToMyRoom($"C{connection.ConnectionId} connected ({connection.Metadata[ConnectionMetadataNames.Transport]})", connection.ConnectionId);

            try
            {
                while (await connection.Transport.In.WaitToReadAsync())
                {
                    var MYRoomEnemy = ConnectionsD.GetValueOrDefault(connection.ConnectionId);
                    if (MYRoomEnemy == null) continue;
                    if (connection.Transport.In.TryRead(out var buffer))
                    {
                        // We can avoid the copy here but we'll deal with that later
                        var text = Encoding.UTF8.GetString(buffer);
                        // text = $"{connection.ConnectionId}: {text}";


                        await SentDataToMyRoom(Encoding.UTF8.GetBytes(text), connection.ConnectionId);
                    }
                }
            }
            finally
            {

                // var MYRoomEnemy = ConnectionsD.GetValueOrDefault(connection.ConnectionId);
                // var conContextEnemy = Connections[MYRoomEnemy];
                await SentDataToMyRoom($"D{connection.ConnectionId} disconnected ({connection.Metadata[ConnectionMetadataNames.Transport]})", connection.ConnectionId);
                Connections.Remove(connection);
            }
        }

        private Task Broadcast(string text)
        {
            return Broadcast(Encoding.UTF8.GetBytes(text));
        }

        private Task SentDataToMyRoom(string text, string MyConId)
        {
            return SentDataToMyRoom(Encoding.UTF8.GetBytes(text), MyConId);
        }
        private Task SentDataToMyRoom(byte[] payload, string MyConId)
        {
            var MYRoomEnemy = ConnectionsD.GetValueOrDefault(MyConId);
            if (MYRoomEnemy == null) { return Task.CompletedTask; }
            var conContextEnemy = Connections[MYRoomEnemy];
            if (conContextEnemy == null) { return Task.CompletedTask; }
            Task sent = conContextEnemy.Transport.Out.WriteAsync(payload); //siunciam mano kordinates priesui;

            return sent;
        }
        private Task Broadcast(byte[] payload)
        {
            var tasks = new List<Task>(Connections.Count);

            foreach (var c in Connections)
            {
                tasks.Add(c.Transport.Out.WriteAsync(payload));
            }

            return Task.WhenAll(tasks);
        }

    }
}
