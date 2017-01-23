using System;
using MMOServer.Network;
using Protocol;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Game.Packet
{
	[SessionStateCommandFilter(GameSession.SessionState.Logined)]
	public class Cmd_CSPkReqReadyEnterWorld : PacketCommandBase<GameSession>
	{
		public static Random s_r = new Random((int)DateTime.Now.Ticks);

		public override void ExecuteCommand(GameSession session, BinaryRequestInfo requestInfo)
		{
			session.State = GameSession.SessionState.Ready;

			

			session.GameObject.SetPosition(s_r.Next(50, 100), 0, s_r.Next(50, 100));

			CSPkResReadyEnterWorld pkResReadyEnterWorld = new CSPkResReadyEnterWorld
			{
				Result = PacketResultCode.Success,
				CharacterInfo = session.GameObject.GetPkGameObjectInfo()
			};

			session.SendPacket(CSPacketCommand.CSPkResReadyEnterWorld, pkResReadyEnterWorld);
		}
	}
}
