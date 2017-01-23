using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using MMOServer.Network;
using Protocol;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;

namespace MMOServer.Game
{
	public class GameServer : AppServer<GameSession, BinaryRequestInfo>, IServerMessageHandler
	{
		public World World;

		private Thread m_thread;
		private ConcurrentQueue<Action> m_queue = new ConcurrentQueue<Action>();
		

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

			m_thread = new Thread(Update);
			m_thread.Start();

			return true;
		}

		private void Update()
		{
			while (true)
			{
				Action action;
				while (m_queue.TryDequeue(out action))
				{
					action();
				}

				World.Update();
				Thread.Sleep(1);
			}
		}

		public void PushAction(Action action)
		{
			m_queue.Enqueue(action);
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

			Console.WriteLine("OnMesssage : " + command);

			// job으로 던진 후 실행
			PushAction(() => eventMethodInfo.Invoke(World, new[] { channel, publisher, packet }));
		}
	}
}
