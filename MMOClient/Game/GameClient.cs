using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using Protocol;
using SharpDX;

namespace MMOClient.Game
{
	class GameClient : ITcpClientConnectionReceiver
	{
		private const int MoveSpeed = 20;

		private TcpClientConnection m_connection;
		private int m_id;

		public List<PkGameObjectInfo> GameObjectList = new List<PkGameObjectInfo>();

		public Action<string> LogAction;

		public GameClient()
		{
			m_id = Process.GetCurrentProcess().Id;

			m_connection = new TcpClientConnection(this);
		}

		public void Move(float destX, float destZ)
		{
			PkMoveInfo info = new PkMoveInfo
			{
				MoveState = 1,
				StartPos = GameObjectList[0].Pos,
				DestPos = new PkVector3(destX, 0, destZ)
			};

			GameObjectList[0].MoveInfo = info;

			CSPkReqMove csPkReqMove = new CSPkReqMove
									  {
										  MoveInfo = info
									  };

			Send(CSPacketCommand.CSPkReqMove, csPkReqMove);
		}

		public void Connect(int port)
		{
			Random r = new Random((int)DateTime.Now.Ticks);
			m_connection.Connect(IPAddress.Parse("127.0.0.1"), port);
		}

		public void Update(float elapsedTime)
		{
			foreach (PkGameObjectInfo pkGameObjectInfo in GameObjectList)
			{
				if (pkGameObjectInfo.MoveInfo != null && pkGameObjectInfo.MoveInfo.MoveState == 1)
				{
					Vector3 dest = pkGameObjectInfo.MoveInfo.DestPos;

					Vector3 pos = pkGameObjectInfo.Pos;
					Vector3 forward = (dest - pos);

					if (MoveSpeed * elapsedTime >= forward.Length())
					{
						pkGameObjectInfo.MoveInfo.MoveState = 0;
						pkGameObjectInfo.Pos = pkGameObjectInfo.MoveInfo.DestPos;
					}
					else
					{
						forward.Normalize();
						pos += forward * MoveSpeed * elapsedTime;
						pkGameObjectInfo.Pos = pos;
					}
				}	
			}

			m_connection.FixedUpdate();
		}

		public void OnConnect()
		{
			CSPkReqLogin reqLogin = new CSPkReqLogin { AccountId = m_id.ToString() };
			Send(CSPacketCommand.CSPkReqLogin, reqLogin);
		}

		public void OnCantConnect(SocketError errCode)
		{
		}

		public void OnRecv(ReadableQueue queue)
		{
			while (true)
			{
				if (queue.Size() >= 6)
				{
					byte[] buf = new byte[6];

					queue.Peek(ref buf, 6);

					int bodyLength = BitConverter.ToInt32(buf, 2);
					int totalSize = bodyLength + 6;

					if (queue.Size() >= totalSize)
					{
						buf = new byte[totalSize];
						queue.Read(ref buf, totalSize);

						DispatchPacket(buf, totalSize);
					}
					else
					{
						break;
					}
				}
				else
				{
					break;
				}
			}
		}

		public void OnDisconnect()
		{
		}

		public void OnError(SocketError errCode)
		{
		}

		public bool Send<T>(CSPacketCommand command, T packet)
		{
			MemoryStream stream = new MemoryStream();

			if (packet != null)
			{
				Serializer.Serialize(stream, packet);
			}

			byte[] sendBuf;

			if (stream.Length > 0)
			{
				sendBuf = new byte[stream.Length + 6];
				Buffer.BlockCopy(stream.GetBuffer(), 0, sendBuf, 6, (int)stream.Length);
			}
			else
			{
				sendBuf = new byte[6];
			}

			Buffer.BlockCopy(BitConverter.GetBytes((ushort)command), 0, sendBuf, 0, 2);
			Buffer.BlockCopy(BitConverter.GetBytes(stream.Length), 0, sendBuf, 2, 4);
			

			return m_connection.Send(sendBuf, sendBuf.Length);
		}

		public void DispatchPacket(byte[] buf, int size)
		{
			CSPacketCommand command = (CSPacketCommand)BitConverter.ToInt16(buf, 0);

			switch (command)
			{
				case CSPacketCommand.CSPkResLogin:
					Send(CSPacketCommand.CSPkReqReadyEnterWorld, (object)null);
					break;
				case CSPacketCommand.CSPkResReadyEnterWorld:
					{
						CSPkResReadyEnterWorld packet = Serializer.Deserialize<CSPkResReadyEnterWorld>(new MemoryStream(buf, 6, size - 6));
						GameObjectList.Add(packet.CharacterInfo);
						Send(CSPacketCommand.CSPkReqEnterWorld, (object)null);
					}
					break;
				case CSPacketCommand.CSPkResEnterWorld:
					break;
				case CSPacketCommand.CSPkNotifyEnterGameObject:
					{
						CSPkNotifyEnterGameObject packet = Serializer.Deserialize<CSPkNotifyEnterGameObject>(new MemoryStream(buf, 6, size - 6));

						if (packet.GameObjectInfoList != null)
						{
							GameObjectList.AddRange(packet.GameObjectInfoList);
						}

						LogAction("enter : object count : " + GameObjectList.Count);
					}
					break;
				case CSPacketCommand.CSPkNotifyLeaveGameObject:
					{
						CSPkNotifyLeaveGameObject packet = Serializer.Deserialize<CSPkNotifyLeaveGameObject>(new MemoryStream(buf, 6, size - 6));

						if (packet.HandleList != null)
						{
							foreach (long handle in packet.HandleList)
							{
								GameObjectList.Remove(GameObjectList.Find(o => o.Handle == handle));
							}
						}

						LogAction("leave : object count : " + GameObjectList.Count);
					}
					break;
				case CSPacketCommand.CSPkNotifyMove:
					{
						CSPkNotifyMove packet = Serializer.Deserialize<CSPkNotifyMove>(new MemoryStream(buf, 6, size - 6));

						PkGameObjectInfo gameObject = GameObjectList.FirstOrDefault(o => o.Handle == packet.Handle);
						gameObject.MoveInfo = packet.MoveInfo;
						gameObject.Pos = packet.MoveInfo.StartPos;

						LogAction("move : object count : " + GameObjectList.Count);
					}
				break;
			}
		}
	}
}
