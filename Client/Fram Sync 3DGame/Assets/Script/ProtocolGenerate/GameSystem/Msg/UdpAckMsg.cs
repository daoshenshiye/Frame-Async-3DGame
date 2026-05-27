using GamePlayer;
using System.Collections.Generic;
using System.Text;
namespace GameSystem{
		public class UdpAckMsg:BaseMsg{
		public  long seq;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=8;
		return num;
		}
		public override byte[] Writting()
		{
		int num = GetBytesNum();
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteInt(bytes,GetID(),ref index);
		WriteInt(bytes, num - 8, ref index);
		WriteLong(bytes,seq,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		seq=ReadLong(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 404;}
		}
}