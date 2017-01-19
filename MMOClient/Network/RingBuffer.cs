using System;
using System.Collections;

public interface ReadableQueue
{
    bool IsEmpty();
    int Size();
    int Capacity();
    bool Write(byte[] data, int size);
    bool Read(ref byte[] data, int size);
    bool Peek(ref byte[] data, int size);
    bool Seek(int size);
}


public class RingBuffer : ReadableQueue
{
    private int DEFAULT_SIZE = 4096;

    public byte[] m_Buffer;
    public int m_Head;
    public int m_Tail;
    
    private byte[] m_SeekDummyBuffer = new byte[0];

    public RingBuffer()
    {
        AllocBuffer(DEFAULT_SIZE);
        Clear();
    }

    public RingBuffer(int size)
    {
        AllocBuffer(size);
        Clear();
    }

    public bool IsEmpty()
    {
        return (m_Head == m_Tail);
    }

    public int Size()
    {
        int[] remainSize = RemainSize();
        return remainSize[0] + remainSize[1];
    }

    public int Capacity()
    {
        return m_Buffer.Length;
    }

    public bool Write(byte[] data, int size)
    {
        // data를 Write할 공간이 부족하면 false를 리턴.
        if (Size() + size >= m_Buffer.Length)
        {
            return false;
        }

        int emptySize = EmptySize();
        int writeSize = Math.Min(size, emptySize);
        int extraWriteSize = size - writeSize;

        Buffer.BlockCopy(data, 0, m_Buffer, m_Tail, writeSize);
        m_Tail += writeSize;
        m_Tail %= m_Buffer.Length;

        // 나누어서 Write할 data가 있다면.
        if (extraWriteSize > 0)
        {
            Buffer.BlockCopy(data, writeSize, m_Buffer, m_Tail, extraWriteSize);
            m_Tail += extraWriteSize;
        }

        return true;
    }

    public bool Read(ref byte[] data, int size)
    {
        return InternalRead(ref data, size, false);
    }

    public bool Peek(ref byte[] data, int size)
    {
        return InternalRead(ref data, size, true);
    }

    public bool Seek(int size)
    {
        return InternalRead(ref m_SeekDummyBuffer, size, false);
    }

    private bool InternalRead(ref byte[] data, int size, bool IsPeek)
    {
        // size 만큼의 데이터가 없다면 false 리턴
        if (Size() < size)
        {
            return false;
        }

        int[] remainSize = RemainSize();
        int readSize = Math.Min(size, remainSize[0]);
        int extraReadSize = size - readSize;

        int head = m_Head;

        // data가 m_SeekDummyBuffer인 경우에는 data에 copy를 할 필요가 없음.
        if (data != m_SeekDummyBuffer)
        {

            Buffer.BlockCopy(m_Buffer, head, data, 0, readSize);
        }
        head += readSize;
        head %= m_Buffer.Length;

        // 나누어서 Read할 data가 있다면.
        if (extraReadSize > 0)
        {
            // data가 m_SeekDummyBuffer인 경우에는 data에 copy를 할 필요가 없음.
            if (data != m_SeekDummyBuffer)
            {
                Buffer.BlockCopy(m_Buffer, head, data, readSize, extraReadSize);
            }
            head += extraReadSize;
        }

        if(!IsPeek)
        {
            m_Head = head;
        }

        return true;
    }

    private int[] RemainSize()
    {
        int[] remainSize = new int[2];

        // Tail >= Head : Tail - Head = size
        // Tail < Head : Capacity - Head + Tail = size;
        if (m_Tail >= m_Head)
        {
            remainSize[0] = m_Tail - m_Head;
            remainSize[1] = 0;
        }
        else
        {
            remainSize[0] = Capacity() - m_Head;
            remainSize[1] = m_Tail;
        }

        return remainSize;
    }

    private int EmptySize()
    {
        if (m_Tail >= m_Head)
        {
            return Capacity() - m_Tail;
        }
        else
        {
            return m_Head - m_Tail;
        }
    }

    private void Clear()
    {
        m_Head = 0;
        m_Tail = 0;
    }

    private void AllocBuffer(int size)
    {
        if (m_Buffer != null)
        {
            return;
        }

        // buffer를 생성한다.
        m_Buffer = new byte[size];
        Clear();
    }
}
