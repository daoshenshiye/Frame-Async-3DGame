using GamePlayer;
using GamePlayer;
using System.Collections.Generic;
using System.Text;
namespace GameMessage{
		public class UdpPlayerAddMsg:BaseMsg{
		public  int playerId;
		public  PlayerStateData playerstate;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=4;
		num+=playerstate.GetBytesNum();
		return num;
		}
		public override byte[] Writting()
		{
		int num = GetBytesNum();
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteInt(bytes,GetID(),ref index);
		WriteInt(bytes, num - 8, ref index);
		WriteInt(bytes,playerId,ref index);
		WriteData(bytes,playerstate,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		playerId=ReadInt(bytes,ref index);
		playerstate=ReadData<PlayerStateData>(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 466;}
		}
}