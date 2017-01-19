using System.Collections.Generic;
using ProtoBuf;
using SharpDX;

namespace Protocol
{
	enum PacketResultCode
	{
		Success,
		Fail,
	}

	enum CSPacketCommand
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

	enum SSPacketCommand
	{
		SSPkNotifyEnterGameObject,
		SSPkNotifyLeaveGameObject,

		SSPkReqGameObjectList,
		SSPkResGameObjectList,

		SSPkNotifyMove,
	}

	[ProtoContract]
	class PkVector3
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

		public static implicit operator Vector3(PkVector3 v)
		{
			return new Vector3(v.X, v.Y, v.Z);
		}

		public static implicit operator PkVector3(Vector3 v)
		{
			return new PkVector3(v.X, v.Y, v.Z);
		}
	}

	[ProtoContract]
	class PkVector2
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

		public static implicit operator Vector2(PkVector2 v)
		{
			return new Vector2(v.X, v.Y);
		}

		public static implicit operator PkVector2(Vector2 v)
		{
			return new PkVector2(v.X, v.Y);
		}
	}

	[ProtoContract]
	class CSPkReqLogin
	{
		[ProtoMember(1)]
		public string AccountId;
	}

	[ProtoContract]
	class CSPkResLogin
	{
		[ProtoMember(1)]
		public PacketResultCode Result;
	}

	[ProtoContract]
	class CSPkResReadyEnterWorld
	{
		[ProtoMember(1)]
		public PacketResultCode Result;

		[ProtoMember(2)]
		public PkGameObjectInfo CharacterInfo;
	}

	[ProtoContract]
	class CSPkResEnterWorld
	{
		[ProtoMember(1)]
		public PacketResultCode Result;

		[ProtoMember(2)]
		PkVector3 Pos;
	}

	[ProtoContract]
	class PkGameObjectInfo
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
	class CSPkNotifyEnterGameObject
	{
		[ProtoMember(1)]
		public List<PkGameObjectInfo> GameObjectInfoList;
	}

	[ProtoContract]
	class CSPkNotifyLeaveGameObject
	{
		[ProtoMember(1)]
		public List<long> HandleList;
	}

	[ProtoContract]
	class PkMoveInfo
	{
		[ProtoMember(1)]
		public PkVector3 StartPos;

		[ProtoMember(2)]
		public PkVector3 DestPos;

		[ProtoMember(3)]
		public int MoveState;
	}


	[ProtoContract]
	class CSPkReqMove
	{
		[ProtoMember(1)]
		public PkMoveInfo MoveInfo;
	}

	[ProtoContract]
	class CSPkResMove
	{
		[ProtoMember(1)]
		public PacketResultCode Result;
	}

	[ProtoContract]
	class CSPkNotifyMove
	{
		[ProtoMember(1)]
		public long Handle;

		[ProtoMember(2)]
		public PkMoveInfo MoveInfo;
	}




	////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////

	[ProtoContract]
	class SSPkReqGameObjectList
	{
		[ProtoMember(1)]
		public List<PkVector2> SectorList;
	}

	[ProtoContract]
	class SSPkResGameObjectList
	{
		[ProtoMember(1)]
		public List<PkGameObjectInfo> GameObjectList;
	}

	[ProtoContract]
	class SSPkNotifyEnterGameObject
	{
		[ProtoMember(1)]
		public PkGameObjectInfo GameObjectInfo;
	}

	[ProtoContract]
	class SSPkNotifyLeaveGameObject
	{
		[ProtoMember(1)]
		public long Handle;
	}

	[ProtoContract]
	class SSPkNotifyMove
	{
		[ProtoMember(1)]
		public long Handle;

		[ProtoMember(2)]
		public PkMoveInfo MoveInfo;
	}
}
