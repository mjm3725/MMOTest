using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Metadata;

namespace MMOServer.Game.Packet
{
	class SessionStateCommandFilter : CommandFilterAttribute
	{
		private GameSession.SessionState m_state;

		public SessionStateCommandFilter(GameSession.SessionState state)
		{
			m_state = state;
		}

		public override void OnCommandExecuting(CommandExecutingContext commandContext)
		{
			GameSession session = (GameSession)commandContext.Session;

			if (session.State != m_state)
			{
				commandContext.Cancel = true;
			}
		}

		public override void OnCommandExecuted(CommandExecutingContext commandContext)
		{
			
		}
	}
}
