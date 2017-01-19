using MMOServer.Network;
using SuperSocket.SocketBase;

namespace MMOServer.Game
{
	public class GameSession : PacketSession<GameSession>
	{
		public enum SessionState
		{
			None,
			Logined,
			Ready,
			World,
		}

		public SessionState State;
		public GameObject GameObject;

		protected override void OnSessionClosed(CloseReason reason)
		{
			if (State == SessionState.World)
			{
				GameServer server = AppServer as GameServer;

				server.TaskExecutor.PushAction(() =>
				{
					server.World.LeaveWorld(GameObject);
				});
			}
		}
	}
}
