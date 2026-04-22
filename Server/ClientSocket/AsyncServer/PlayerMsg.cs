using System.Collections;
using System.Collections.Generic;


public class PlayerMsg : BaseMsg
{
    public int playerId;
    public PlayerData playerData;
    public override int GetBytesNum()
    {
        return 4 + 4+4 +playerData.GetBytesNum();
    }
    public override byte[] Writting()
    {
        int num = GetBytesNum();
        byte[] data = new byte[num];
        int index = 0;


        WriteInt(data, GetID(), ref index);
        WriteInt(data, num - 8, ref index);
        WriteInt(data, playerId, ref index);
        WriteData(data, playerData, ref index);
        return data;

    }
    public override int Reading(byte[] bytes, int beginIndex = 0)
    {
        int index = beginIndex;
        playerId = ReadInt(bytes, ref index);
      playerData=  ReadData<PlayerData>(bytes, ref index);
        return index - beginIndex;
    }
    public override int GetID()

    {
        return 100;
    }
}
