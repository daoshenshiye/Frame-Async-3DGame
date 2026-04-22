using System.Collections;
using System.Collections.Generic;


public class QuitMessage : BaseMsg
{
    public override int GetBytesNum()
    {
        return 8;
    }
    public override int Reading(byte[] bytes, int beginIndex = 0)
    {
        int index = beginIndex;
        ReadInt(bytes, ref index);
        ReadInt(bytes, ref index);
        return index-beginIndex;
    }
    public override byte[] Writting()
    {
        int index=0;
        byte[] bytes = new byte[8];
        WriteInt(bytes, GetID(),ref index);
        WriteInt(bytes, 0,ref index);
        return bytes;
    }
    public override int GetID()
    {
        return 404;
    }
}
