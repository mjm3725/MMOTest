using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using Protocol;
using SharpNav;
using SharpNav.Geometry;
using ZeroMQ;


namespace MMOServer.Game
{
	public interface IServerMessageHandler
	{
		void OnMessage(string channel, string publisher, SSPacketCommand command, object packet);
	}

	class ServerToServerManager
	{
		private ZContext m_zcontext;
		private ZSocket m_zsocketPub;
		private ZSocket m_zsocketSub;
		private IServerMessageHandler m_messageHandler;

		private Dictionary<Vector2, int> m_subscribeChannelMap = new Dictionary<Vector2, int>();

		private string m_publishKey;

		public List<Vector2> GetSubscribeList()
		{
			return m_subscribeChannelMap.Keys.ToList();
		}

		public void Initialize(int bindPort, int[] backendPorts, IServerMessageHandler messageHandler)
		{
			m_messageHandler = messageHandler;

			m_zcontext = new ZContext();

			m_zsocketPub = new ZSocket(m_zcontext, ZSocketType.PUB);
			m_zsocketPub.Bind("tcp://*:" + bindPort);

			m_publishKey = "S" + bindPort;	// 내 식별자

			m_zsocketSub = new ZSocket(m_zcontext, ZSocketType.SUB);

			m_zsocketSub.Subscribe(m_publishKey);	//unicast채널
			m_zsocketSub.Subscribe("A");			//sector동기화가 아닌 broadcast채널

			for (int i = 0; i < backendPorts.Length; i++)
			{
				m_zsocketSub.Connect("tcp://127.0.0.1:" + backendPorts[i]);
			}
		}

		public void Publish<T>(string channel, T packet)
		{
			SSPacketCommand commandEnum = 0;
			Enum.TryParse(typeof(T).Name, out commandEnum);

			MemoryStream stream = new MemoryStream();
			stream.Write(BitConverter.GetBytes((ushort)commandEnum), 0, 2);

			Serializer.Serialize(stream, packet);

			// subscribe채널, publisher식별자, 실제 데이터를 조합해서 메시지를 작성함
			ZMessage message = new ZMessage();
			message.Add(new ZFrame(channel));
			message.Add(new ZFrame(m_publishKey));
			message.Add(new ZFrame(stream.GetBuffer(), 0, (int)stream.Length));

			m_zsocketPub.Send(message);

			Console.WriteLine("Publish");
		}

		public int Subscribe(Vector2 channel)
		{
			if (!m_subscribeChannelMap.ContainsKey(channel))
			{
				m_subscribeChannelMap.Add(channel, 0);
			}

			m_subscribeChannelMap[channel]++;

			if (m_subscribeChannelMap[channel] == 1)
			{
				m_zsocketSub.Subscribe(channel.ToString());
			}

			return m_subscribeChannelMap[channel];
		}

		public int Unsubscribe(Vector2 channel)
		{
			if (!m_subscribeChannelMap.ContainsKey(channel))
			{
				return -1;
			}

			m_subscribeChannelMap[channel]--;

			if (m_subscribeChannelMap[channel] == 0)
			{
				m_subscribeChannelMap.Remove(channel);

				m_zsocketSub.Unsubscribe(channel.ToString());

				return 0;
			}

			return m_subscribeChannelMap[channel];
		}

		public bool IsSubscribe(Vector2 channel)
		{
			return m_subscribeChannelMap.ContainsKey(channel);
		}

		public void Update()
		{
			ZError error;
			ZMessage message = m_zsocketSub.ReceiveMessage(ZSocketFlags.DontWait, out error);

			if (message != null)
			{
				SSPacketCommand command = (SSPacketCommand)message[2].ReadInt16();

				Type packetType = Type.GetType("Protocol." + command);

				if (packetType == null)
				{
					return;
				}

				object packet = Serializer.NonGeneric.Deserialize(packetType, message[2]);

				m_messageHandler.OnMessage(message[0].ReadString(), message[1].ReadString(), command, packet);					
			}
		}
	}
}
