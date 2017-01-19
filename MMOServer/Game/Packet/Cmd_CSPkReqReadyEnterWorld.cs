using System;
using MMOServer.Network;
using Protocol;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Game.Packet
{
	[SessionStateCommandFilter(GameSession.SessionState.Logined)]
	public class Cmd_CSPkReqReadyEnterWorld : PacketCommandBase<GameSession>
	{
		public override void ExecuteCommand(GameSession session, BinaryRequestInfo requestInfo)
		{
			session.State = GameSession.SessionState.Ready;

			Random r = new Random();

			session.GameObject.SetPosition(r.Next(50, 250), 0, r.Next(50, 250));

			CSPkResReadyEnterWorld pkResReadyEnterWorld = new CSPkResReadyEnterWorld
			{
				Result = PacketResultCode.Success,
				CharacterInfo = session.GameObject.GetPkGameObjectInfo()
			};

			session.SendPacket(CSPacketCommand.CSPkResReadyEnterWorld, pkResReadyEnterWorld);
		}
	}
}
