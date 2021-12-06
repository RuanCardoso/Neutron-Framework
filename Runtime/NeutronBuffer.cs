using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace NeutronNetwork.Internal
{
    // This class creates a single large buffer which can be divided up.
    // and assigned to SocketAsyncEventArgs objects for use with each.
    // socket I/O operation.
    // This enables bufffers to be easily reused and guards against.
    // fragmenting heap memory.
    //
    // The operations exposed on the NeutronBuffer class are not thread safe.
    public class NeutronBuffer
    {
        private int m_numBytes; // the total number of bytes controlled by the buffer pool.
        private Memory<byte> m_buffer; // the underlying byte array maintained by the Buffer Manager.
        private Stack<int> m_freeIndexPool;
        private int m_currentIndex;
        private int m_bufferSize;

        public NeutronBuffer(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        // Allocates buffer space used by the buffer pool.
        public void Init()
        {
            // create one big large buffer and divide that.
            // out to each SocketAsyncEventArg object.
            m_buffer = new byte[m_numBytes];
        }

        // Assigns a buffer from the buffer pool to the.
        // specified SocketAsyncEventArgs object.
        // <returns>true if the buffer was successfully set, else false</returns>
        public bool Set(SocketAsyncEventArgs args)
        {

            if (m_freeIndexPool.Count > 0)
            {
                int start = m_freeIndexPool.Pop();
                args.SetBuffer(m_buffer[start..(start + m_bufferSize)]); // assign a buffer from the buffer pool.
            }
            else
            {
                if ((m_numBytes - m_bufferSize) < m_currentIndex) // the client should not send more than the total number of bytes in the buffer pool.
                    return LogHelper.Error("Buffer Manager: Buffer is full!"); // we've exceeded the max size of the buffer pool.

                args.SetBuffer(m_buffer[m_currentIndex..(m_currentIndex + m_bufferSize)]); // assign a buffer from the buffer pool.

                m_currentIndex += m_bufferSize; // move the current index ahead.
            }

            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.
        // This frees the buffer back to the buffer pool.
        public void Free(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset); // add the index of the buffer being freed to the free buffer collection.
            args.SetBuffer(null, 0, 0); // Free the buffer.
        }
    }
}