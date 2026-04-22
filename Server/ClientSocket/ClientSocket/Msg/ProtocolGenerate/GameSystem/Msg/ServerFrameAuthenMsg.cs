using GamePlayer;
using System.Collections.Generic;
using System.Text;
namespace GameSystem{
		public class ServerFrameAuthenMsg:BaseMsg{
		public  long serLogicFrame;
		public  List<ServerInputAndStateData> ServerInputStateData;
		public override int GetBytesNum()
		{
		int num=0;
		num+=4;
		num+=4;
		num+=8;
		num+=4;
		for(int i=0;i<ServerInputStateData.Count;i++)
		{
			num+=ServerInputStateData[i].GetBytesNum();
		}
		return num;
		}
		public override byte[] Writting()
		{
		int num = GetBytesNum();
		int index=0;
		byte[] bytes=new byte[GetBytesNum()];
		WriteInt(bytes,GetID(),ref index);
		WriteInt(bytes, num - 8, ref index);
		WriteLong(bytes,serLogicFrame,ref index);
				WriteInt(bytes,ServerInputStateData.Count,ref index);
		for(int i=0;i<ServerInputStateData.Count;i++)
		{
			WriteData(bytes,ServerInputStateData[i],ref index);
		}
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		serLogicFrame=ReadLong(bytes,ref index);
				ServerInputStateData=new List<ServerInputAndStateData>();
			int Count = ReadInt(bytes, ref index);

        for (int i=0;i< Count; i++)
		{
			ServerInputAndStateData temp=ReadData<ServerInputAndStateData>(bytes,ref index);
		ServerInputStateData.Add(temp);
		}
		return index-beginIndex;
		}
		public override int GetID(){return 101;}
		}
}