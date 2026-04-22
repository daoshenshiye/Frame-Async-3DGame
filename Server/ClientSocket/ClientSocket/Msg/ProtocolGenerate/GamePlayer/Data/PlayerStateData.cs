using System.Collections.Generic;
using System.Text;
namespace GamePlayer{
		public class PlayerStateData:BaseData{
		public  int hp;
		public  PlayerPosData playerPos;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
            num += (playerPos != null) ? playerPos.GetBytesNum() : 0;
            return num;
		}
		public override byte[] Writting()
		{
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteInt(bytes,hp,ref index);
		WriteData(bytes, (playerPos != null) ? playerPos:new PlayerPosData(), ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		hp=ReadInt(bytes,ref index);
		playerPos=ReadData<PlayerPosData>(bytes,ref index);
		return index-beginIndex;
		}
		}
}