using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;


public abstract class BaseData
{
    public abstract int GetBytesNum();
    public abstract byte[] Writting();
    public abstract int Reading(byte[] bytes ,int beginindex=0);
    public void WriteInt(byte[] bytes,int value,ref int index)
    {
       BitConverter.GetBytes(value).CopyTo(bytes,index);
        index +=sizeof(int);
    }
    public void WriteString(byte[] bytes, String value, ref int index)
    {
        byte[] strbytes= Encoding.UTF8.GetBytes(value);
        WriteInt(bytes,strbytes.Length,ref index);
        strbytes.CopyTo(bytes, index);
        index +=strbytes.Length;
    }
    public void WriteShort(byte[] bytes, short value, ref int index)
    {
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(short);
    }
    public void WriteBool(byte[] bytes, bool value, ref int index)
    {
        BitConverter.GetBytes(value).CopyTo(bytes, index);
        index += sizeof(bool);
    }
    public void WriteData(byte[] bytes, BaseData value, ref int index)
    {
        //value.Writting().CopyTo(bytes, index);
        //    index += value.GetBytesNum();

        Array.Copy(value.Writting(), 0, bytes, index, value.GetBytesNum());
        index += value.GetBytesNum();
    }
    public short ReadShort(byte[] bytes,ref int index)
    {
        short value = BitConverter.ToInt16(bytes, index);
        index += sizeof(short);
        return value;
    }
    public int ReadInt(byte[] bytes, ref int index)
    {
        int value = BitConverter.ToInt32(bytes, index);
        index += sizeof(int);
        return value;
    }
    public bool ReadBool(byte[] bytes, ref int index)
    {
        bool value = BitConverter.ToBoolean(bytes, index);
        index += sizeof(bool);
        return value;
    }
    public string ReadString(byte[] bytes , ref int index)
    {
       int length= ReadInt(bytes, ref index);
       
        string value = Encoding.UTF8.GetString(bytes, index, length);
        index += length;
        return value;
    }
    public T ReadData<T>(byte[] bytes,ref int index) where T : BaseData,new()
    {
        T value = new T();
        index += value.Reading(bytes,index);
      return  value;

    }
}
