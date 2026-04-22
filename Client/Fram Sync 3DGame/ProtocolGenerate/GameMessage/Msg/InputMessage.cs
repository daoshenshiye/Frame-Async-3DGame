using System.Collections.Generic;
using System.Text;
namespace GameMessage{
		public class InputMessage:BaseMsg{
		public  string PlayerAddr;
		public  InputData input;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=4;
		num+=Encoding.UTF8.GetByteCount(PlayerAddr);
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
		WriteString(bytes,PlayerAddr,ref index);
		WriteData(bytes,input,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		PlayerAddr=ReadString(bytes,ref index);
		input=ReadData<InputData>(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 140;}
		}
}