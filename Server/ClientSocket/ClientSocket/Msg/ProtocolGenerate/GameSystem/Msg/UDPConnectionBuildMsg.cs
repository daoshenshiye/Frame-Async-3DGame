using GamePlayer;
using GamePlayer;
using System.Collections.Generic;
using System.Text;
namespace GameSystem{
		public class UDPConnectionBuildMsg:BaseMsg{
		public  long ServerLogicFrame;
		public  int DelayBufferFrame;
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
		WriteLong(bytes,ServerLogicFrame,ref index);
		WriteInt(bytes,DelayBufferFrame,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		ServerLogicFrame=ReadLong(bytes,ref index);
		DelayBufferFrame=ReadInt(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 453;}
		}
}