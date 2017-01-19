using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using MMOServer.Network;
using Protocol;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Game
{
	public class GameServer : AppServer<GameSession, BinaryRequestInfo>, IServerMessageHandler
	{
		public TaskExecutor TaskExecutor;
		public World World;
		

		public GameServer() : base(new DefaultReceiveFilterFactory<PacketReceiveFilter, BinaryRequestInfo>())
		{
			
		}

		protected override bool Setup(IRootConfig rootConfig, IServerConfig config)
		{
			string worldList = ConfigurationManager.AppSettings["world_list"];
			string[] worldPorts = worldList.Split(',');

			int worldId = int.Parse(config.Options["world_id"]);
			int backendPort = int.Parse(config.Options["backend_port"]);

			World = new World();
			World.Initialize(worldId, backendPort,
							worldPorts.Select(r => int.Parse(r)).Where(r => r != backendPort).ToArray(),
							this);

			return true;
		}

		public void OnMessage(string channel, string publisher, SSPacketCommand command, object packet)
		{
			// 커맨드 네임과 매칭하는 메시지 핸들러 메소드를 얻음
			MethodInfo eventMethodInfo = typeof(World).GetMethod("On" + command);

			if (eventMethodInfo == null)
			{
				Logger.Error("can not find event method. " + command);
				return;
			}

			// job으로 던진 후 실행
			TaskExecutor.PushAction(() => eventMethodInfo.Invoke(World, new[] { channel, publisher, packet }));
		}
	}
}
