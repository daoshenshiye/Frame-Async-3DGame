using System.Collections.Generic;
using System.Text;
namespace GamePlayer{
		public class PlayerEnterRoomMsg:BaseMsg{
		public  int playerId;
		public  int roomId;
		public  bool success;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=4;
		num+=4;
		num+=1;
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
		WriteInt(bytes,roomId,ref index);
		WriteBool(bytes,success,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		playerId=ReadInt(bytes,ref index);
		roomId=ReadInt(bytes,ref index);
		success=ReadBool(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 305;}
		}
}