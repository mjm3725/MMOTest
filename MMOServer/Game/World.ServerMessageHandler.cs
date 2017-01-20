using System.Collections.Generic;
using Protocol;

namespace MMOServer.Game
{
	public partial class World
	{
		public void OnSSPkNotifyEnterGameObject(string channel, string publisher, SSPkNotifyEnterGameObject packet)
		{
			GameObject gameObject = new GameObject();
			gameObject.SetPkGameObjectInfo(packet.GameObjectInfo);

			if (!AddGameObject(gameObject)) // 이미 존재하면 무시함. 이미 동기화 하고 있는 오브젝트의 정보가 sector이동으로 인해 들어올 수 있음
			{
				return;
			}

			m_sectorManager.Enter(gameObject);
			
			CSPkNotifyEnterGameObject notifyPacket = new CSPkNotifyEnterGameObject
										 {
											 GameObjectInfoList = new List<PkGameObjectInfo>{ packet.GameObjectInfo }
										 };

			BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyEnterGameObject, notifyPacket, true);    
		}

		public void OnSSPkNotifyLeaveGameObject(string channel, string publisher, SSPkNotifyLeaveGameObject packet)
		{
			GameObject gameObject = GetGameObject(packet.Handle);

			if (gameObject == null)
			{
				return;
			}

			RemoveGameObject(gameObject);

			// 패킷에 있는 위치를 가지고  sector에서 leave시킴
			m_sectorManager.Leave(gameObject);

			CSPkNotifyLeaveGameObject notifyPacket = new CSPkNotifyLeaveGameObject { HandleList = new List<long> { packet.Handle } };
			BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyLeaveGameObject, notifyPacket, true);    
		}

		public void OnSSPkReqGameObjectList(string channel, string publisher, SSPkReqGameObjectList packet)
		{
			SSPkResGameObjectList ssPkResGameObjectList = new SSPkResGameObjectList
														  {
															  GameObjectList = new List<PkGameObjectInfo>()
														  };

			m_sectorManager.VisitMany(packet.SectorList, (go) =>
														  {
															  if (go.Session != null)
															  {
																  ssPkResGameObjectList.GameObjectList.Add(go.GetPkGameObjectInfo());
															  }
														  });

			m_serverToServerManager.Publish(publisher, ssPkResGameObjectList);
		}

		public void OnSSPkResGameObjectList(string channel, string publisher, SSPkResGameObjectList packet)
		{
			if (packet.GameObjectList == null)
			{
				return;
			}

			foreach (PkGameObjectInfo pkGameObjectInfo in packet.GameObjectList)
			{
				GameObject gameObject = new GameObject();
				gameObject.SetPkGameObjectInfo(pkGameObjectInfo);

				AddGameObject(gameObject);

				m_sectorManager.Enter(gameObject);

				CSPkNotifyEnterGameObject notifyPacket = new CSPkNotifyEnterGameObject
				{
					GameObjectInfoList = new List<PkGameObjectInfo> { pkGameObjectInfo }
				};

				BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyEnterGameObject, notifyPacket, true);
			}
		}

		public void OnSSPkNotifyMove(string channel, string publisher, SSPkNotifyMove packet)
		{
			GameObject gameObject = GetGameObject(packet.Handle);

			if (gameObject == null)
			{
				return;
			}
			
			gameObject.Move(packet.MoveInfo);

			UpdateMoveSector(gameObject);

			CSPkNotifyMove csPkNotifyMove = new CSPkNotifyMove
											{
												Handle = gameObject.Handle,
												MoveInfo = gameObject.MoveInfo
											};

			BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyMove, csPkNotifyMove, true);
		}
	}
}
