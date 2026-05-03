using System.Collections.Generic;
using System.Text;
using GamePlayer;

namespace GameMessage{
		public class InputMessage:BaseMsg{
		public  int PlayerId;
		public  InputData input;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=4;
		num+=input.GetBytesNum();
		return num;
		}
		public override byte[] Writting()
		{
		int num = GetBytesNum();
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteInt(bytes,GetID(),ref index);
		WriteInt(bytes, num - 8, ref index);
		WriteInt(bytes,PlayerId,ref index);
		WriteData(bytes,input,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		PlayerId=ReadInt(bytes,ref index);
		input=ReadData<InputData>(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 140;}
		}
}