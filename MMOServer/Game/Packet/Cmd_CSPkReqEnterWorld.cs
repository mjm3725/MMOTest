using MMOServer.Network;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Game.Packet
{
	[SessionStateCommandFilter(GameSession.SessionState.Ready)]
	public class Cmd_CSPkReqEnterWorld : PacketCommandBase<GameSession>
	{
		public override void ExecuteCommand(GameSession session, BinaryRequestInfo requestInfo)
		{
			session.State = GameSession.SessionState.World;

			GameServer server = session.AppServer as GameServer;

			server.PushAction(() =>
			{
				server.World.EnterWorld(session.GameObject);
			});
		}
	}
}
