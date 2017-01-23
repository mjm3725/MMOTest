using System;
using System.Collections.Generic;
using System.Linq;
using Protocol;
using SharpNav;


namespace MMOServer.Game
{
	public partial class World
	{
		private readonly Dictionary<long, GameObject> m_gameObjects = new Dictionary<long, GameObject>();
		
		private SectorManager m_sectorManager;

		private long m_lastUpdateTick = DateTime.Now.Ticks;
		
		private ServerToServerManager m_serverToServerManager;
		
		private readonly List<GameObject> m_deletedGameObjects = new List<GameObject>();

		public int Id;

		
		public void EnterWorld(GameObject gameObject)
		{
			AddGameObject(gameObject);

			m_sectorManager.Enter(gameObject);

			// 다른 플레이어들에게 보낼 내 정보
			CSPkNotifyEnterGameObject pkMyInfo = new CSPkNotifyEnterGameObject
			{
				GameObjectInfoList = new List<PkGameObjectInfo>
				{
					 gameObject.GetPkGameObjectInfo()
				}
			};

			// 내가 받을 다른 플레이어들 정보
			CSPkNotifyEnterGameObject pkOthersInfo = new CSPkNotifyEnterGameObject
			{
				GameObjectInfoList = new List<PkGameObjectInfo>()
			};

			// 다른 애들의 정보를 모음
			m_sectorManager.VisitArround(gameObject,
				(go) =>
				{
					if (go != gameObject)
					{
						pkOthersInfo.GameObjectInfoList.Add(go.GetPkGameObjectInfo());
					}
				});

			// 내 정보 브로드캐스트
			BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyEnterGameObject, pkMyInfo, true);

			// 나한테 다른 플레이어들 정보 보냄
			gameObject.Send(CSPacketCommand.CSPkNotifyEnterGameObject, pkOthersInfo);

			//-----------------------------------------------------------------------------------------
			// 서버간 동기화
			//-----------------------------------------------------------------------------------------

			// 서버가 주변정보를 받을 수 있게 subscribe
			SubscribeSector(m_sectorManager.GetArroundSectorSet(gameObject));

			// 다른 서버에 진입통보
			SSPkNotifyEnterGameObject ssPkNotifyEnterGameObject = new SSPkNotifyEnterGameObject
			{
				GameObjectInfo = pkMyInfo.GameObjectInfoList[0]
			};

			m_serverToServerManager.Publish(m_sectorManager.GetSectorCoordinate(gameObject).ToString(), ssPkNotifyEnterGameObject);
		}

		public void LeaveWorld(GameObject gameObject)
		{
			RemoveGameObject(gameObject);
			m_sectorManager.Leave(gameObject);

			CSPkNotifyLeaveGameObject pkNotifyLeave = new CSPkNotifyLeaveGameObject
			{
				HandleList = new List<long> { gameObject.Handle }
			};

			BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyLeaveGameObject, pkNotifyLeave, false);

			SSPkNotifyLeaveGameObject pkNotifyLeaveToServer = new SSPkNotifyLeaveGameObject
			{
				Handle = gameObject.Handle
			};

			// 다른 서버와도 동기화
			UnsubscribeSector(m_sectorManager.GetArroundSectorSet(gameObject));

			m_serverToServerManager.Publish(m_sectorManager.GetSectorCoordinate(gameObject).ToString(), pkNotifyLeaveToServer);
		}

		public void Move(GameObject gameObject, PkMoveInfo moveInfo)
		{
			gameObject.Move(moveInfo);

			CSPkResMove pkResMove = new CSPkResMove
			{
				Result = PacketResultCode.Success
			};

			gameObject.Send(CSPacketCommand.CSPkResMove, pkResMove);

			UpdateMoveSector(gameObject);

			CSPkNotifyMove csPkNotifyMove = new CSPkNotifyMove
			{
				Handle = gameObject.Handle,
				MoveInfo = moveInfo
			};

			BroadcastArround(gameObject, CSPacketCommand.CSPkNotifyMove, csPkNotifyMove, true);

			SSPkNotifyMove ssPkNotifyMove = new SSPkNotifyMove
			{
				Handle = gameObject.Handle,
				MoveInfo = moveInfo
			};

			m_serverToServerManager.Publish(m_sectorManager.GetSectorCoordinate(gameObject).ToString(), ssPkNotifyMove);
		}
		
		public List<GameObject> GetGameObjectList()
		{
			return m_gameObjects.Values.ToList();	
		}

		public List<Vector2> GetSubscribeSectorList()
		{
			return m_serverToServerManager.GetSubscribeList();
		}

		public void Initialize(int id, int bindBackendPort, int[] backendPorts, IServerMessageHandler serverMessageHandler)
		{
			Id = id;

			m_serverToServerManager = new ServerToServerManager();
			m_serverToServerManager.Initialize(bindBackendPort, backendPorts, serverMessageHandler);

			m_sectorManager = new SectorManager(500, 500, 20);
		}

		public void Update()
		{
			UpdateGameObject();
			m_serverToServerManager.Update();
		}

		private void UpdateGameObject()
		{
			long curTick = DateTime.Now.Ticks;
			long elapsedTick = (long)new TimeSpan(curTick - m_lastUpdateTick).TotalMilliseconds;

			if (elapsedTick >= 33) //30프레임
			{
				do
				{
					foreach (KeyValuePair<long, GameObject> v in m_gameObjects)
					{
						v.Value.Update(0.033f);
						UpdateMoveSector(v.Value);
					}

					// 삭제된 오브젝트 실제로 삭제
					foreach (GameObject o in m_deletedGameObjects)
					{
						m_gameObjects.Remove(o.Handle);
					}
					
					m_deletedGameObjects.Clear();

					elapsedTick -= 33;

				} while (elapsedTick >= 33); //누적된 시간만큼 업데이트 처리

				m_lastUpdateTick = curTick;
			}
		}

		public bool AddGameObject(GameObject gameObject)
		{
			try
			{
				m_gameObjects.Add(gameObject.Handle, gameObject);
				
				return true;
			}
			catch
			{
				return false;
			}
		}

		public void RemoveGameObject(GameObject gameObject)
		{
			m_deletedGameObjects.Add(gameObject); // 바로 지우지 않고 삭제 대기 목록에 넣음. update루틴에서 한꺼번에 삭제함.
		}

		public GameObject GetGameObject(long handle)
		{
			GameObject ret;
			m_gameObjects.TryGetValue(handle, out ret);

			return ret;
		}

		public void BroadcastArround<T>(GameObject gameObject, CSPacketCommand cmd, T packet, bool isExceptMe)
		{
			if (!isExceptMe)
			{
				m_sectorManager.VisitArround(gameObject, (go) => go.Send(cmd, packet));
			}
			else
			{
				m_sectorManager.VisitArround(gameObject, (go) =>
														 {
															 if (go != gameObject)
															 {
																 go.Send(cmd, packet);
															 }
														 });
			}
		}

		public void BroadcastMany<T>(IEnumerable<Vector2> sectorList, GameObject sender, CSPacketCommand cmd, T packet)
		{
			m_sectorManager.VisitMany(sectorList, (go) =>
													{
														if (go != sender)
														{
															go.Send(cmd, packet);
														}
													});
		}

		public void SubscribeSector(IEnumerable<Vector2> sectorList)
		{
			List<PkVector2> newChannelList = new List<PkVector2>();

			foreach (Vector2 channel in sectorList)
			{
				if (m_serverToServerManager.Subscribe(channel) == 1)
				{
					newChannelList.Add(channel);
				}
			}

			if (newChannelList.Count > 0)
			{
				// 다른 서버들에게 주변 정보 요청
				SSPkReqGameObjectList ssPkReqGameObjectList = new SSPkReqGameObjectList
				{
					SectorList = newChannelList
				};

				m_serverToServerManager.Publish("A", ssPkReqGameObjectList);
			}
		}

		public void UnsubscribeSector(IEnumerable<Vector2> sectorList)
		{
			foreach (Vector2 channel in sectorList)
			{
				if (m_serverToServerManager.Unsubscribe(channel) == 0)
				{
					List<GameObject> removedObjList = new List<GameObject>();

					m_sectorManager.ClearSector((int)channel.X, (int)channel.Y, removedObjList);

					foreach (GameObject gameObject in removedObjList)
					{
						RemoveGameObject(gameObject);
					}
				}
			}
		}

		private void UpdateMoveSector(GameObject gameObject)
		{
			if (gameObject.MoveInfo == null || gameObject.MoveInfo.MoveState != (int)MoveState.Move)
			{
				return;
			}
			Vector2 preSector = m_sectorManager.GetSectorCoordinate(gameObject.PrePosition);
			Vector2 newSector = m_sectorManager.GetSectorCoordinate(gameObject.Position);

			if (preSector == newSector)
			{
				return;
			}

			// 이전 섹터에서 내보내고 새 섹터에 넣음
			m_sectorManager.Leave((int)preSector.X, (int)preSector.Y, gameObject);
			m_sectorManager.Enter((int)newSector.X, (int)newSector.Y, gameObject);

			CSPkNotifyLeaveGameObject csPkNotifyLeave = new CSPkNotifyLeaveGameObject
														{
															HandleList = new List<long>{ gameObject.Handle }
														};

			CSPkNotifyEnterGameObject csPkNotifyEnter = new CSPkNotifyEnterGameObject
														{
															GameObjectInfoList = new List<PkGameObjectInfo>
																				 {
																					 gameObject.GetPkGameObjectInfo()
																				 }
														};

			// 시야에서 사라진 섹터와 새로 시야에 추가된 섹터를 구함.
			SortedSet<Vector2> oldSet = m_sectorManager.GetArroundSectorSet(gameObject.PrePosition);
			SortedSet<Vector2> newSet = m_sectorManager.GetArroundSectorSet(gameObject.Position);

			List<Vector2> removedList = oldSet.Except(newSet).ToList();
			List<Vector2> addedList = newSet.Except(oldSet).ToList();

			// 사라진 섹터에 나갔다고 알리고 추가된 섹터에 들어왔다고 알림
			BroadcastMany(removedList, gameObject, CSPacketCommand.CSPkNotifyLeaveGameObject, csPkNotifyLeave);
			BroadcastMany(addedList, gameObject, CSPacketCommand.CSPkNotifyEnterGameObject, csPkNotifyEnter);

			if (gameObject.Session != null)
			{
				// 실제 플레이어일 경우에만 처리함
				// 추가된 섹터에 있는 오브젝트들의 정보를 나한테 알림
				CSPkNotifyEnterGameObject pkOthersInfo = new CSPkNotifyEnterGameObject
														 {
															 GameObjectInfoList = new List<PkGameObjectInfo>()
														 };

				m_sectorManager.VisitMany(addedList, (go) => pkOthersInfo.GameObjectInfoList.Add(go.GetPkGameObjectInfo()));

				if (pkOthersInfo.GameObjectInfoList.Count > 0)
				{
					gameObject.Send(CSPacketCommand.CSPkNotifyEnterGameObject, pkOthersInfo);
				}

				//  사라진 섹터에 있는 오브젝트 정보를 알림
				CSPkNotifyLeaveGameObject pkRemovedInfo = new CSPkNotifyLeaveGameObject
														  {
															  HandleList = new List<long>()
														  };

				m_sectorManager.VisitMany(removedList, (go) => pkRemovedInfo.HandleList.Add(go.Handle));

				if (pkRemovedInfo.HandleList.Count > 0)
				{
					gameObject.Send(CSPacketCommand.CSPkNotifyLeaveGameObject, pkRemovedInfo);
				}

				// 필요없는 섹터 unsubscribe
				UnsubscribeSector(removedList);

				// 필요한 섹터  subscribe
				SubscribeSector(addedList);

				// 이동한 섹터에 알림. 처음 시야에 들어온 서버가 있을 수 있어서 알려야함
				SSPkNotifyEnterGameObject ssPkNotifyEnterGameObject = new SSPkNotifyEnterGameObject
																	  {
																		  GameObjectInfo = gameObject.GetPkGameObjectInfo()
																	  };

				m_serverToServerManager.Publish(m_sectorManager.GetSectorCoordinate(gameObject).ToString(), ssPkNotifyEnterGameObject);
			}
			else
			{
				// 다른 서버에 있는 리모트 오브젝트일 경우 내 시야밖으로 나간 경우에는 오브젝트 삭제
				if (!m_serverToServerManager.IsSubscribe(newSector))
				{
					m_sectorManager.Leave((int)newSector.X, (int)newSector.Y, gameObject);
					RemoveGameObject(gameObject);
				}
			}
		}
	}
}
