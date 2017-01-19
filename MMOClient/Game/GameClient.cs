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
        private const int MoveSpeed = 10;
		private const int HeaderSize = 4;
		private const int PacketLengthSize = 2;
		private const int CommandSize = 2;

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

		public void Connect()
		{
			Random r = new Random((int)DateTime.Now.Ticks);
			m_connection.Connect(IPAddress.Parse("127.0.0.1"), r.Next(0, 2) == 0 ? 5000 : 5001);
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
				if (queue.Size() >= PacketLengthSize)
				{
					byte[] buf = new byte[PacketLengthSize];

					queue.Peek(ref buf, PacketLengthSize);

					int packetLength = BitConverter.ToInt16(buf, 0);

					if (queue.Size() >= packetLength)
					{
						queue.Seek(PacketLengthSize); // command 위치로 이동

						int size = packetLength - PacketLengthSize;

						buf = new byte[size];
						queue.Read(ref buf, size);

						DispatchPacket(buf, size);
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
				sendBuf = new byte[stream.Length + HeaderSize];
				Buffer.BlockCopy(stream.GetBuffer(), 0, sendBuf, HeaderSize, (int)stream.Length);
			}
			else
			{
				sendBuf = new byte[HeaderSize];
			}

			Buffer.BlockCopy(BitConverter.GetBytes(sendBuf.Length), 0, sendBuf, 0, PacketLengthSize);
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)command), 0, sendBuf, PacketLengthSize, CommandSize);

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
						CSPkResReadyEnterWorld packet = Serializer.Deserialize<CSPkResReadyEnterWorld>(new MemoryStream(buf, 2, size - 2));
						GameObjectList.Add(packet.CharacterInfo);
						Send(CSPacketCommand.CSPkReqEnterWorld, (object)null);
					}
					break;
				case CSPacketCommand.CSPkResEnterWorld:
					break;
				case CSPacketCommand.CSPkNotifyEnterGameObject:
					{
						CSPkNotifyEnterGameObject packet = Serializer.Deserialize<CSPkNotifyEnterGameObject>(new MemoryStream(buf, 2, size - 2));

						if (packet.GameObjectInfoList != null)
						{
							GameObjectList.AddRange(packet.GameObjectInfoList);
						}

						LogAction("object count : " + GameObjectList.Count);
					}
					break;
				case CSPacketCommand.CSPkNotifyLeaveGameObject:
					{
						CSPkNotifyLeaveGameObject packet = Serializer.Deserialize<CSPkNotifyLeaveGameObject>(new MemoryStream(buf, 2, size - 2));

                        if (packet.HandleList != null)
                        {
                            foreach (long handle in packet.HandleList)
                            {
                                GameObjectList.Remove(GameObjectList.Find(o => o.Handle == handle));
                            }
                        }
					}
					break;
				case CSPacketCommand.CSPkNotifyMove:
					{
						CSPkNotifyMove packet = Serializer.Deserialize<CSPkNotifyMove>(new MemoryStream(buf, 2, size - 2));

						PkGameObjectInfo gameObject = GameObjectList.FirstOrDefault(o => o.Handle == packet.Handle);
						gameObject.MoveInfo = packet.MoveInfo;
						gameObject.Pos = packet.MoveInfo.StartPos;
					}
				break;
			}
		}
	}
}
