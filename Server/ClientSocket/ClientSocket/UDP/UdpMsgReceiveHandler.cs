using GameMsg;
using GameSystem;

namespace ClientSocket.UDP;

public class UdpMsgReceiveHandler
{
    
        public Action<short,long,BaseHandler> OnCompleteReceive;
        
        public void HandleMsg(byte[] bytes,int receiveLegth)
        {
            try
            {
            int nowIndex = 0;
            int msgLength = 0;
            int ID = 0;
            long nowSeq = -1;
            
            byte[] ReceiveBytes = bytes;
            short type = -1;
            if (ReceiveBytes.Length>=sizeof(short))
            {
                type = BitConverter.ToInt16(ReceiveBytes, nowIndex);
            }
            else
            {
                return;
            }
            nowIndex += 2;
            msgLength = -1;
            if (receiveLegth >= 10)
            {
                if (type == 1)
                {
                    nowSeq = BitConverter.ToInt64(ReceiveBytes, nowIndex);
                    nowIndex += 8;
                }

                ID = BitConverter.ToInt32(ReceiveBytes, nowIndex);
                nowIndex += 4;
                msgLength = BitConverter.ToInt32(ReceiveBytes, nowIndex);
                nowIndex += 4;

                if (receiveLegth - nowIndex >= msgLength && msgLength != -1)
                {
                    BaseMsg baseMsg = null;
                    baseMsg = MsgPool.Instance.GetMsg(ID);
                    
                    if (baseMsg != null)
                    {
                        baseMsg.Reading(ReceiveBytes, nowIndex);
                        BaseHandler baseHandler = MsgPool.Instance.GetHandler(ID);
                        baseHandler.msg = baseMsg;
                        OnCompleteReceive?.Invoke(type, nowSeq, baseHandler);
                    }
                    nowIndex += msgLength;
                }
            }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(e.StackTrace);
                throw;
            }
        }
}