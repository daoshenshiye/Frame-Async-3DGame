using System.Collections.Generic;
using System.Text;
namespace GamePlayer{
		public class PlayerPosData:BaseData{
		public  float x;
		public  float y;
		public  float z;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=4;
		return num;
		}
		public override byte[] Writting()
		{
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteFloat(bytes,x,ref index);
		WriteFloat(bytes,y,ref index);
		WriteFloat(bytes,z,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		x=ReadFloat(bytes,ref index);
		y=ReadFloat(bytes,ref index);
		z=ReadFloat(bytes,ref index);
		return index-beginIndex;
		}
		}
}