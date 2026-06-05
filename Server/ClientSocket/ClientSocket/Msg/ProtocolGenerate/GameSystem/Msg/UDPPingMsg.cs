using GamePlayer;
using GamePlayer;
using System.Collections.Generic;
using System.Text;
namespace GameSystem{
		public class UDPPingMsg:BaseMsg{
		public  long SendTime;
		public  int playerId;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=8;
		num+=4;
		return num;
		}
		public override byte[] Writting()
		{
		int num = GetBytesNum();
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteInt(bytes,GetID(),ref index);
		WriteInt(bytes, num - 8, ref index);
		WriteLong(bytes,SendTime,ref index);
		WriteInt(bytes,playerId,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		SendTime=ReadLong(bytes,ref index);
		playerId=ReadInt(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 449;}
		}
}