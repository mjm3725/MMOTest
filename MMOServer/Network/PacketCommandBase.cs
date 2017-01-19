using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Network
{
	public abstract class PacketCommandBase<TAppSession> : ICommand<TAppSession, BinaryRequestInfo> 
		where TAppSession : IAppSession
	{
		public string Name
		{
			get
			{
				return GetType().Name;
			}
		}

		public abstract void ExecuteCommand(TAppSession session, BinaryRequestInfo requestInfo);
	}
}
