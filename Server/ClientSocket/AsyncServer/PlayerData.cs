using System.Collections;
using System.Collections.Generic;
using System.Text;


public class PlayerData : BaseData
{
    public int hp;
    public int level;
    public string name;
   
    public override int GetBytesNum()
    {
        return 4+4+4+Encoding.UTF8.GetBytes(name).Length;
    }

    public override int Reading(byte[] bytes, int beginindex = 0)
    {
       int index=beginindex;
        hp=ReadInt(bytes,ref index);
        level=ReadInt(bytes,ref index);
        name=ReadString(bytes,ref index);
        return index-beginindex;
    }

    public override byte[] Writting()
    {
        int index = 0;
        byte[] bytes = new byte[GetBytesNum()];
        WriteInt(bytes,hp,ref index);
        WriteInt(bytes,level,ref index);
        WriteString(bytes,name,ref index);
        return bytes;
    }
}
