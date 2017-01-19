using System;
using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.Common;
using Protocol;

namespace MMOServer.Network
{
	// +-----+-------+-------------------------------+
	// | cmd |  len  |  request body                 |
	// | (2) |  (4)  |                               |
	// +-----+-------+-------------------------------+
	class PacketReceiveFilter : FixedHeaderReceiveFilter<BinaryRequestInfo>
	{
		public PacketReceiveFilter() : base(6)
		{

		}

		protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
		{
			return BitConverter.ToInt32(header, offset + 2);
		}

		protected override BinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
		{
			int cmd = BitConverter.ToUInt16(header.Array, header.Offset);

			return new BinaryRequestInfo("Cmd_" + (CSPacketCommand)cmd, bodyBuffer.CloneRange(offset, length));
		}

	}
}
