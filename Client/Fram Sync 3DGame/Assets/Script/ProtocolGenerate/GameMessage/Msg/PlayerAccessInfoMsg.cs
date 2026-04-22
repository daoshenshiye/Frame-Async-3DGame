using System.Collections.Generic;
using System.Text;
namespace GameMessage{
		public class PlayerAccessInfoMsg:BaseMsg{
		public  int PlayerId;
		public  string PlayerNickName;
		public  string username;
		public  string password;
		public override int GetBytesNum()
		{
            int num = 0;
            num += 4;
            num += 4;
            num += 4;
            num += 4;
            num += Encoding.UTF8.GetByteCount(PlayerNickName);
            num += 4;
            num += Encoding.UTF8.GetByteCount(username);
            num += 4;
            num += Encoding.UTF8.GetByteCount(password);

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
		WriteString(bytes,PlayerNickName,ref index);
		WriteString(bytes,username,ref index);
		WriteString(bytes,password,ref index);
		return bytes;
		}
		public override int Reading(byte[] bytes,int beginIndex=0)
		{
		int index=beginIndex;
		PlayerId=ReadInt(bytes,ref index);
		PlayerNickName=ReadString(bytes,ref index);
		username=ReadString(bytes,ref index);
		password=ReadString(bytes,ref index);
		return index-beginIndex;
		}
		public override int GetID(){return 450;}
		}
}