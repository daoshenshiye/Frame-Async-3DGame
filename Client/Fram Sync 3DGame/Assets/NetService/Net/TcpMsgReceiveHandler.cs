using System;
using GameMsg;
using UnityEngine;
namespace NetService.Net
{
    public class TcpMsgReceiveHandler
    {
        private  readonly object _receiveBufferLock = new object();
        private byte[] _receiveBuffer;
        private int _recvValidLen;
        public Action<BaseHandler> OnMessageParsed;
        
        public TcpMsgReceiveHandler(int lenth)
        {
            _receiveBuffer=new byte[lenth];
        }

        public void HandleReceiveMsg(byte[] recvBytes, int recvLen)
        {
            if (recvBytes==null)
            {
                return;
            }
            lock (_receiveBufferLock)
            {
                if (_recvValidLen+recvLen>_receiveBuffer.Length)
                {
                    byte[] newbyte=new byte[_receiveBuffer.Length * 2];
                    Array.Copy(_receiveBuffer, newbyte, _recvValidLen);
                    _receiveBuffer=newbyte;
                }
                Array.Copy(recvBytes, 0, _receiveBuffer, _recvValidLen, recvLen);
                _recvValidLen += recvLen;
                while (_recvValidLen >=8)
                {
                    int offset = 0;
                    int id=BitConverter.ToInt32(_receiveBuffer,offset);
                    offset += 4;
                    int bodyLen=BitConverter.ToInt32(_receiveBuffer,offset);
                    offset += 4;
                    int fullPackageLen = bodyLen + 8;
                    if (fullPackageLen > 1024 * 8)
                    {
                        Console.WriteLine("收到超大非法数据包，清空缓冲区");
                        lock (_receiveBuffer) _recvValidLen = 0;
                        break;
                    }
                    if (_recvValidLen < fullPackageLen)
                        break;
                    
                   BaseMsg baseMsg= MsgPool.Instance.GetMsg(id);
                   if (baseMsg!=null)
                   {
                       baseMsg.Reading(_receiveBuffer, offset);
                      BaseHandler baseHandler= MsgPool.Instance.GetHandler(id);
                      if (baseHandler != null)
                      {
                          baseHandler.msg=baseMsg;
                          OnMessageParsed?.Invoke(baseHandler);
                      }
                   }
                   int remaining = _recvValidLen-fullPackageLen;
                   if (remaining>0)
                   {
                       Array.Copy(_receiveBuffer, fullPackageLen, _receiveBuffer, 0, remaining);
                   }
                   _recvValidLen = remaining;
                }
            }
        }
        public void ResetReadIndex()
        {
            _recvValidLen = 0;
        }
    }
}