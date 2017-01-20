using System;
using System.IO;
using System.Threading;
using MMOServer.Network;
using ProtoBuf;
using Protocol;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Game.Packet
{
	[SessionStateCommandFilter(GameSession.SessionState.None)]
	public class Cmd_CSPkReqLogin : PacketCommandBase<GameSession>
	{
		private static long s_handle;

		public override void ExecuteCommand(GameSession session, BinaryRequestInfo requestInfo)
		{
			CSPkReqLogin packet = Serializer.Deserialize<CSPkReqLogin>(new MemoryStream(requestInfo.Body));

			long handle = Interlocked.Increment(ref s_handle);

			handle |= (long)(session.AppServer as GameServer).World.Id << 59;

			session.GameObject = new GameObject { Handle = handle, Type = GameObjectType.Player, Name = packet.AccountId, Session = session };

			CSPkResLogin pkResLogin = new CSPkResLogin
			{
				Result = PacketResultCode.Success
			};

			session.State = GameSession.SessionState.Logined;
			
			session.SendPacket(CSPacketCommand.CSPkResLogin, pkResLogin);
		}
	}
}
