using System.Collections.Generic;
using System.Text;
namespace GamePlayer{
		public class ServerInputAndStateData:BaseData{
		public  int playerId;
		public InputData  inputdata;
		public PlayerStateData  playerstate;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=inputdata.GetBytesNum();
		num+=playerstate.GetBytesNum();
		return num;
		}
		public override byte[] Writting()
		{
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteInt(bytes,playerId,ref index);
		WriteData(bytes,inputdata,ref index);
		WriteData(bytes,playerstate,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		playerId=ReadInt(bytes,ref index);
		inputdata=ReadData<InputData>(bytes,ref index);
		playerstate=ReadData<PlayerStateData>(bytes,ref index);
		return index-beginIndex;
		}
		}
}