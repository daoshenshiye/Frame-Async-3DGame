using System.Collections.Generic;
using System.Text;
namespace GamePlayer{
		public class InputData:BaseData{
		public  float Vertical;
		public  float Horizontal;
		public  bool Jump;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=1;
		return num;
		}
		public override byte[] Writting()
		{
		int index=0;
		byte[] bytes=new byte[1024*1024];
		WriteFloat(bytes,Vertical,ref index);
		WriteFloat(bytes,Horizontal,ref index);
		WriteBool(bytes,Jump,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		Vertical=ReadFloat(bytes,ref index);
		Horizontal=ReadFloat(bytes,ref index);
		Jump=ReadBool(bytes,ref index);
		return index-beginIndex;
		}
		}
}