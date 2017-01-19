using System;
using System.Net;
using System.Net.Sockets;


public class TcpClientConnection : ITcpClientConnection
{
    protected Socket m_socket;

    public TcpClientConnection(ITcpClientConnectionReceiver receiver) : base(receiver) { }

    public override bool Connect(IPAddress addr, int port)
    {
        if (m_state != STATE.NONE)
        {
            //TMDebug.LogError("연결할 수 없는 상태 : " + m_state + ", Addr : " + addr + ":" + port);
            return false;
        }

        // Establish the remote endpoint for the socket.
        var remoteEp = new IPEndPoint(addr, port);

        m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        m_socket.Blocking = false;
        m_socket.NoDelay = true;
        m_socket.ReceiveBufferSize = 8192 * 2;
        m_socket.SendBufferSize = 8192 * 2;

        try
        {
            // Connect!
            m_socket.Connect(remoteEp);
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode != SocketError.WouldBlock)
            {
                ((ITcpClientConnectionReceiver)m_receiver.Target).OnCantConnect(e.SocketErrorCode);
                return false;
            }
        }

        m_state = STATE.CONNECTING;
        return true;
    }

    public override void SetSocket(Socket socket)
    {
        m_socket = socket;
        m_state = STATE.CONNECTING;

        m_socket.Blocking = false;
        m_socket.NoDelay = true;
        m_socket.ReceiveBufferSize = 8192 * 2;
        m_socket.SendBufferSize = 8192 * 2;
    }

     public override bool Disconnect()
    {
        if (m_state != STATE.CONNECTED)
        {
            return false;
        }

        m_socket.Disconnect(false);
        m_state = STATE.NONE;

        return true;
    }

    public override bool Send(byte[] data, int size)
    {
        if (m_state != STATE.CONNECTED)
        {
            //UnityEngine.Debug.LogError("TcpClientConnection 연결하지 못했습니다.");
            return false;
        }

        bool isCanSend = m_writeQueue.IsEmpty();

        if (!m_writeQueue.Write(data, size))
        {
            //UnityEngine.Debug.LogError("TcpClientConnection 쓸 수 없다..");
            return false;
        }

        try
        {
            // write queue가 비어있다면 바로 Send를 한다.
            if (isCanSend)
            {
                int sentLength = m_socket.Send(data, size, SocketFlags.None);

                // 보낸 data는 queue에서 빼준다.
                m_writeQueue.Seek(sentLength);
            }
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode != SocketError.WouldBlock)
            {
                //UnityEngine.Debug.LogError("TcpClientConnection 흠..");
                return false;
            }
        }

        return true;
    }

    public override void FixedUpdate()
    {
        switch (m_state)
        {
            case STATE.CONNECTING:
                if (m_socket.Poll(0, SelectMode.SelectError))
                {
                    m_state = STATE.NONE;
                    ((ITcpClientConnectionReceiver)m_receiver.Target).OnCantConnect(SocketError.NotConnected);
                }
                else if (m_socket.Poll(0, SelectMode.SelectWrite))
                {
                    m_state = STATE.CONNECTED;
                    ((ITcpClientConnectionReceiver)m_receiver.Target).OnConnect();
                }
                break;
            case STATE.CONNECTED:
                if (m_socket.Poll(0, SelectMode.SelectRead))
                {
                    if (m_socket.Available == 0)
                    {
                        ((ITcpClientConnectionReceiver)m_receiver.Target).OnDisconnect();
                        m_state = STATE.NONE;
                        m_socket.Close();
                        return;
                    }

                    while (m_socket.Available > 0)
                    {
                        int length = m_socket.Receive(m_buf);
                        m_recvQueue.Write(m_buf, length);
                        m_RecvPacketSizeCounter += length;
                        m_RecvPacketCountCounter++;
                    }

                    ((ITcpClientConnectionReceiver)m_receiver.Target).OnRecv(m_recvQueue);
                }
                if (m_socket.Poll(0, SelectMode.SelectWrite))
                {
                    m_IsCanSend = true;
                    while (!m_writeQueue.IsEmpty())
                    {
                        int size = Math.Min(BUFFER_SIZE, m_writeQueue.Size());
                        m_writeQueue.Peek(ref m_buf, size);

                        try
                        {
                            int length = m_socket.Send(m_buf, size, SocketFlags.None);
                            m_writeQueue.Seek(length);
                        }
                        catch (SocketException e)
                        {
                            if (e.SocketErrorCode != SocketError.WouldBlock)
                            {
                                ((ITcpClientConnectionReceiver)m_receiver.Target).OnError(e.SocketErrorCode);
                            }

                            break;
                        }
                    }
                }
                else
                {
                    m_IsCanSend = false;
                }
                break;
        }
    }
}
