using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public interface ITcpClientConnectionReceiver
{
	void OnConnect();
	void OnCantConnect(SocketError errCode);
	void OnRecv(ReadableQueue queue);
	void OnDisconnect();
	void OnError(SocketError errCode);
}

public abstract class ITcpClientConnection
{
    protected enum STATE
    {
        NONE,
        CONNECTING,
        CONNECTED,
        DISCONNECTING,
    }

    protected static int BUFFER_SIZE = 1024;

    protected WeakReference m_receiver;
    protected STATE m_state = STATE.NONE;
    protected RingBuffer m_writeQueue = new RingBuffer(8192);
    protected RingBuffer m_recvQueue = new RingBuffer(65535);
    protected byte[] m_buf = new byte[BUFFER_SIZE];

    protected int m_RecvPacketTotalSize;
    protected int m_RecvPacketTotalCount;
    protected int m_RecvPacketSizeCounter;
    protected int m_RecvPacketCountCounter;

    protected float m_RecvPacketSizeTimer;

    protected bool m_IsCanSend;

    public ITcpClientConnection(ITcpClientConnectionReceiver receiver)
    {
        m_receiver = new WeakReference(receiver);
        m_RecvPacketTotalSize = 0;
        m_RecvPacketTotalCount = 0;
        m_RecvPacketSizeCounter = 0;
        m_RecvPacketCountCounter = 0;

        m_RecvPacketSizeTimer = 0;
        m_IsCanSend = false;
    }

    public void SetReceiver(ITcpClientConnectionReceiver receiver)
    {
        m_receiver = new WeakReference(receiver);
    }

    public int GetRecvPacketSizePerSecond()
    {
        return m_RecvPacketTotalSize;
    }

    public int GetRecvPacketCountPerSecond()
    {
        return m_RecvPacketTotalCount;
    }

    public bool IsCanSend()
    {
        return m_IsCanSend;
    }

    public bool IsConnected()
    {
        return (m_state == STATE.CONNECTED);
    }

    public abstract bool Connect(IPAddress addr, int port);
    public abstract void SetSocket(Socket socket);
    public abstract bool Disconnect();
    public abstract bool Send(byte[] data, int size);
    public abstract void FixedUpdate();
}
