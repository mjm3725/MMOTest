using Protocol;
using SharpNav;

namespace MMOServer.Game
{
	public enum GameObjectType
	{
		Player,
	}

	public enum MoveState
	{
		Stop,
		Move,
	}

	public class GameObject
	{
		public const float MoveSpeed = 10;

		public long Handle;
		public GameObjectType Type;
		public string Name;
		public GameSession Session;
		
		public Vector3 Position		{ get; private set; }
		public Vector3 PrePosition	{ get; private set; }

		public PkMoveInfo MoveInfo;

		public void SetPosition(float x, float y, float z)
		{
			Position = new Vector3(x, y, z);
			PrePosition = Position;
		}

		public void Update(float elapsedTime)
		{
			if (MoveInfo != null && MoveInfo.MoveState == (int)MoveState.Move)
			{
				PrePosition = Position;

				Vector3 forward = (MoveInfo.DestPos - Position);
				
				if (MoveSpeed * elapsedTime >= forward.Length())
				{
					// 목적지까지 남은 거리가 실제 이동거리보다 적으면 멈추고 목적지로 위치셋팅함
					MoveInfo.MoveState = (int)MoveState.Stop;
					Position = MoveInfo.DestPos;
				}
				else
				{
					forward.Normalize();
					Position += forward * MoveSpeed * elapsedTime;
				}
			}
		}

		public void Send<T>(CSPacketCommand cmd, T packet)
		{
			if (Session == null)
			{
				return;
			}

			Session.SendPacket(cmd, packet);
		}

		public PkGameObjectInfo GetPkGameObjectInfo()
		{
			return new PkGameObjectInfo
					 {
						 Handle = Handle,
						 Type = (int)Type,
						 Pos = Position,
						 Name = Name,
						 MoveInfo = MoveInfo
					 };
		}

		public void SetPkGameObjectInfo(PkGameObjectInfo info)
		{
			Handle = info.Handle;
			Type = (GameObjectType)info.Type;
			Position = info.Pos;
			PrePosition = Position;
			Name = info.Name;
			MoveInfo = info.MoveInfo;
		}
		
		public void Move(PkMoveInfo moveInfo)
		{
			PrePosition = Position;
			Position = moveInfo.StartPos;

			MoveInfo = moveInfo;
		}
	}
}
