using Server.Network;
using Server.Game;

const int port = 9000;

var roomManager = new RoomManager();

var packetHandlers = new PacketHandlers(roomManager);

var server = new TcpServer(port);
server.OnPacketReceived = packetHandlers.Handle;

Console.WriteLine($"서버 시작 중... (port={port})");
await server.StartAsync();


