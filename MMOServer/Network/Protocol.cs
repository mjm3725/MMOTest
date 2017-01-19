using System.Collections.Generic;
using ProtoBuf;
using SharpNav.Geometry;
#if !CLIENT
using SharpNav;
#endif

namespace Protocol
{
	public enum PacketResultCode
	{
		Success,
		Fail,
	}

	public enum CSPacketCommand
	{
		CSPkReqLogin,
		CSPkResLogin,
		
		CSPkReqReadyEnterWorld,
		CSPkResReadyEnterWorld,

		CSPkReqEnterWorld,
		CSPkResEnterWorld,

		CSPkNotifyEnterGameObject,
		CSPkNotifyLeaveGameObject,

		CSPkReqMove,
		CSPkResMove,

		CSPkNotifyMove,
	}

	public enum SSPacketCommand
	{
		SSPkNotifyEnterGameObject,
		SSPkNotifyLeaveGameObject,

		SSPkReqGameObjectList,
		SSPkResGameObjectList,

		SSPkNotifyMove,
	}

	[ProtoContract]
	public class PkVector3
	{
		[ProtoMember(1)]
		public float X;

		[ProtoMember(2)]
		public float Y;

		[ProtoMember(3)]
		public float Z;

		public PkVector3()
		{
			X = 0;
			Y = 0;
			Z = 0;
		}

		public PkVector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

#if !CLIENT
		public static implicit operator Vector3(PkVector3 v)
		{
			return new Vector3(v.X, v.Y, v.Z);
		}

		public static implicit operator PkVector3(Vector3 v)
		{
			return new PkVector3(v.X, v.Y, v.Z);
		}
#endif
	}

	[ProtoContract]
	public class PkVector2
	{
		[ProtoMember(1)]
		public float X;

		[ProtoMember(2)]
		public float Y;

		public PkVector2()
		{
			X = 0;
			Y = 0;
		}

		public PkVector2(float x, float y)
		{
			X = x;
			Y = y;
		}

#if !CLIENT
		public static implicit operator Vector2(PkVector2 v)
		{
			return new Vector2(v.X, v.Y);
		}

		public static implicit operator PkVector2(Vector2 v)
		{
			return new PkVector2(v.X, v.Y);
		}
#endif
	}

	[ProtoContract]
	public class CSPkReqLogin
	{
		[ProtoMember(1)]
		public string AccountId;
	}

	[ProtoContract]
	public class CSPkResLogin
	{
		[ProtoMember(1)]
		public PacketResultCode Result;
	}

	[ProtoContract]
	public class CSPkResReadyEnterWorld
	{
		[ProtoMember(1)]
		public PacketResultCode Result;

		[ProtoMember(2)]
		public PkGameObjectInfo CharacterInfo;
	}

	[ProtoContract]
	public class CSPkResEnterWorld
	{
		[ProtoMember(1)]
		public PacketResultCode Result;
	}

	[ProtoContract]
	public class PkGameObjectInfo
	{
		[ProtoMember(1)]
		public long Handle;

		[ProtoMember(2)]
		public int Type;

		[ProtoMember(3)]
		public PkVector3 Pos;

		[ProtoMember(4)]
		public string Name;

		[ProtoMember(5)]
		public PkMoveInfo MoveInfo;
	}

	[ProtoContract]
	public class CSPkNotifyEnterGameObject
	{
		[ProtoMember(1)]
		public List<PkGameObjectInfo> GameObjectInfoList;
	}

	[ProtoContract]
	public class CSPkNotifyLeaveGameObject
	{
		[ProtoMember(1)]
		public List<long> HandleList;
	}

	[ProtoContract]
	public class PkMoveInfo
	{
		[ProtoMember(1)]
		public PkVector3 StartPos;

		[ProtoMember(2)]
		public PkVector3 DestPos;

		[ProtoMember(3)]
		public int MoveState;
	}


	[ProtoContract]
	public class CSPkReqMove
	{
		[ProtoMember(1)]
		public PkMoveInfo MoveInfo;
	}

	[ProtoContract]
	public class CSPkResMove
	{
		[ProtoMember(1)]
		public PacketResultCode Result;
	}

	[ProtoContract]
	public class CSPkNotifyMove
	{
		[ProtoMember(1)]
		public long Handle;

		[ProtoMember(2)]
		public PkMoveInfo MoveInfo;
	}




	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////

	[ProtoContract]
	public class SSPkReqGameObjectList
	{
		[ProtoMember(1)]
		public List<PkVector2> SectorList;
	}

	[ProtoContract]
	public class SSPkResGameObjectList
	{
		[ProtoMember(1)]
		public List<PkGameObjectInfo> GameObjectList;
	}

	[ProtoContract]
	public class SSPkNotifyEnterGameObject
	{
		[ProtoMember(1)]
		public PkGameObjectInfo GameObjectInfo;
	}

	[ProtoContract]
	public class SSPkNotifyLeaveGameObject
	{
		[ProtoMember(1)]
		public long Handle;
	}

	[ProtoContract]
	public class SSPkNotifyMove
	{
		[ProtoMember(1)]
		public long Handle;

		[ProtoMember(2)]
		public PkMoveInfo MoveInfo;
	}
}
