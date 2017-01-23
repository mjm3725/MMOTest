using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMOServer.Network;
using ProtoBuf;
using Protocol;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Game.Packet
{
	[SessionStateCommandFilter(GameSession.SessionState.World)]
	public class Cmd_CSPkReqMove : PacketCommandBase<GameSession>
	{
		public override void ExecuteCommand(GameSession session, BinaryRequestInfo requestInfo)
		{
			CSPkReqMove packet = Serializer.Deserialize<CSPkReqMove>(new MemoryStream(requestInfo.Body));

			GameServer server = session.AppServer as GameServer;

			server.PushAction(() =>
			{
				server.World.Move(session.GameObject, packet.MoveInfo);
			});
		}
	}
}
