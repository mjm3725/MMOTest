﻿using System.Collections.Generic;
using System.Linq;
using Protocol;
using SharpNav;

namespace MMOServer.Game
{
	public partial class World
	{
		public void OnSSPkNotifyEnterGameObject(string channel, string publisher, SSPkNotifyEnterGameObject packet)
		{
			GameObject gameObject = new GameObject();
			gameObject.SetPkGameObjectInfo(packet.GameObjectInfo);

			CSPkNotifyEnterGameObject notifyPacket = new CSPkNotifyEnterGameObject
										 {
											 GameObjectInfoList = new List<PkGameObjectInfo>{ packet.GameObjectInfo }
										 };

			BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyEnterGameObject, notifyPacket, true);    
		}

		public void OnSSPkNotifyLeaveGameObject(string channel, string publisher, SSPkNotifyLeaveGameObject packet)
		{
			CSPkNotifyLeaveGameObject notifyPacket = new CSPkNotifyLeaveGameObject { HandleList = new List<long> { packet.Handle } };
			BroadcastArround(packet.Pos, CSPacketCommand.CSPkNotifyLeaveGameObject, notifyPacket, true);    
		}

		public void OnSSPkNotifyEnterGameObjectByMove(string channel, string publisher, SSPkNotifyEnterGameObjectByMove packet)
		{
			GameObject gameObject = new GameObject();
			gameObject.SetPkGameObjectInfo(packet.GameObjectInfo);

			CSPkNotifyEnterGameObject notifyPacket = new CSPkNotifyEnterGameObject
			{
				GameObjectInfoList = new List<PkGameObjectInfo> { packet.GameObjectInfo }
			};

			BroadcastMany(packet.AddSectorList.Select(v => (Vector2)v), CSPacketCommand.CSPkNotifyEnterGameObject, notifyPacket);
		}

		public void OnSSPkNotifyLeaveGameObjectByMove(string channel, string publisher, SSPkNotifyLeaveGameObjectByMove packet)
		{
			CSPkNotifyLeaveGameObject notifyPacket = new CSPkNotifyLeaveGameObject { HandleList = new List<long> { packet.Handle } };
			BroadcastMany(packet.RemoveSectorList.Select(v => (Vector2)v), CSPacketCommand.CSPkNotifyLeaveGameObject, notifyPacket);
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

				CSPkNotifyEnterGameObject notifyPacket = new CSPkNotifyEnterGameObject
				{
					GameObjectInfoList = new List<PkGameObjectInfo> { pkGameObjectInfo }
				};

				BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyEnterGameObject, notifyPacket, true);
			}
		}

		public void OnSSPkNotifyMove(string channel, string publisher, SSPkNotifyMove packet)
		{
			CSPkNotifyMove csPkNotifyMove = new CSPkNotifyMove
											{
												Handle = packet.Handle,
												MoveInfo = packet.MoveInfo
											};

			BroadcastArround(packet.MoveInfo.StartPos, CSPacketCommand.CSPkNotifyMove, csPkNotifyMove, true);
		}
	}
}
