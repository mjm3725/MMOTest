using System;
using System.IO;
using ProtoBuf;
using Protocol;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Network
{
	public class PacketSession<TSession> : AppSession<TSession, BinaryRequestInfo>
		where TSession : PacketSession<TSession>, new()
	{
		public void SendPacket<T>(CSPacketCommand packetCommand, T message)
		{
			MemoryStream stream = new MemoryStream(4096);
			BinaryWriter writer = new BinaryWriter(stream);

			writer.Write((ushort)packetCommand);    // 커맨드 2byte
			writer.Write(0);                        // body length 들어갈 공간 확보

			Serializer.Serialize(stream, message);  // body write

			writer.Seek(2, SeekOrigin.Begin);        // body length 위치로 이동
			writer.Write((int)stream.Length - 6);                // body length

			Send(new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length));
		}
	}
}
