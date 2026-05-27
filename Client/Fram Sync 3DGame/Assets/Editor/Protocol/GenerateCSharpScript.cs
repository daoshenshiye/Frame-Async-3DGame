using Codice.Client.Common.TreeGrouper;
using GameMsg;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
public class GenerateCSharpScript
{
    private static string SAVE_PATH= Application.dataPath + "/Script/ProtocolGenerate/";
    private static string SERVER_PATH= "D:\\unity-Git\\Frame Async 3DGame\\Frame Async 3DGame\\Server\\ClientSocket\\ClientSocket" + "\\Msg\\ProtocolGenerate\\";

    public static void WriteScript(string clientOrserver,string path,string serverpath,string name,string script)
    {
        if (name.Contains("Handler"))
        {
            if (clientOrserver.Contains("c"))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path+= name + ".cs";
                if(!File.Exists(path))
                File.WriteAllText(path, script);   
            }

            if (clientOrserver.Contains("s"))
            {
                if (!Directory.Exists(serverpath))
                {
                    Directory.CreateDirectory(serverpath);
                }
                serverpath+= name + ".cs";
                if(!File.Exists(path))
                File.WriteAllText(serverpath, script);
            }
        }
        else
        {
            if (clientOrserver.Contains("c"))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path+= name + ".cs";
                File.WriteAllText(path, script);   
            }

            if (clientOrserver.Contains("s"))
            {
                if (!Directory.Exists(serverpath))
                {
                    Directory.CreateDirectory(serverpath);
                }
                serverpath+= name + ".cs";
                File.WriteAllText(serverpath, script);
            }  
        }
       
    }
    public static void GenerateEnum(XmlNodeList enumNodes)
    {

        string nameSpace = "";
        string name = "";
        string clientOrserver = "";
        string fieldStr = "";
        foreach (XmlNode enumNode in enumNodes) {
            nameSpace = enumNode.Attributes["namespace"].Value;                
            name= enumNode.Attributes["name"].Value;
            clientOrserver=enumNode.Attributes["clientOrserver"].Value;
              XmlNodeList fields=enumNode.SelectNodes("field");
            fieldStr = "";
            foreach (XmlNode field in fields)
            {
                fieldStr += field.Attributes["name"].Value;
                if(field.InnerText!="")
                {
                    fieldStr += " = " + field.InnerText;
                }
               fieldStr+= ",\r\n\t\t";
            }
            string script = $"namespace {nameSpace}" + "{" + "\r\n" +
                                                    "\t\t" + $"public enum {name}" + "{\r\n" +
                                                                                        "\t\t" + fieldStr + "}" + "\r\n}";
            string path = SAVE_PATH + nameSpace + "/Enum/";
            string serverpath=SERVER_PATH+nameSpace+"/Enum/";
            WriteScript(clientOrserver,path,serverpath,name,script);
        }
    }
    public static void GenerateData(XmlNodeList enumNodes) {
        string nameSpace = "";
        string name = "";
        string fieldStr = "";
        string byteNumStr = "";
        string WrittingStr = "";
        string ReadingStr = "";
        string clientOrserver = "";
        foreach (XmlNode enumNode in enumNodes)
        {
            nameSpace = enumNode.Attributes["namespace"].Value;
            name = enumNode.Attributes["name"].Value;
            clientOrserver=enumNode.Attributes["clientOrserver"].Value;
            XmlNodeList fields = enumNode.SelectNodes("field");
            fieldStr = "";
            byteNumStr = "";
            WrittingStr ="";
            ReadingStr = "";
            byteNumStr+="public override int GetBytesNum()\r\n\t\t{\r\n\t\t";
            byteNumStr += "int num=0;\r\n\t\t";
            WrittingStr+= "public override byte[] Writting()\r\n\t\t{\r\n" +
                "\t\t"+ "int index=0;\r\n\t\tbyte[] bytes=new byte[GetBytesNum()];\r\n\t\t";
            ReadingStr+= "public override int Reading(byte[] bytes,int beginIndex=0)\r\n\t\t{\r\n" +
                "\t\t"+"int index=beginIndex;\r\n\t\t";
            foreach (XmlNode field in fields)
            {
                GetBytesNumStr(ref byteNumStr,field);
                GetWrittingStr(ref WrittingStr, field);
                GetReadingStr(ref ReadingStr, field);
                string type= field.Attributes["type"].Value;
                string access = field.Attributes["access"].Value;
                if (access == "public")
                {
                    fieldStr += "public  ";
                }
                else if (access == "private")
                {
                    fieldStr += "private  ";
                }
                else if (access == "protected")
                {
                    fieldStr += "protected  ";
                }

                if (type == "array")
                {
                    fieldStr += field.Attributes["data"].Value + " [] ";
                }
                else if (type == "list")
                {
                    fieldStr += "List" + "<" + field.Attributes["T"].Value + "> ";
                }
                else if (type == "dic")
                {
                    fieldStr += "Dictionary" + "<" + field.Attributes["Tkey"].Value + "," + field.Attributes["Tval"].Value + "> ";
                }
                else
                {
                    fieldStr += type + " ";
                }
                fieldStr += field.Attributes["name"].Value;
                if (field.InnerText != "")
                {
                    fieldStr += " = " + field.InnerText;
                }
                fieldStr += ";\r\n\t\t";
            }

            string script = "using System.Collections.Generic;\r\nusing System.Text;\r\n" + $"namespace {nameSpace}" + "{" + "\r\n" +
                                                    "\t\t" + $"public class {name}:BaseData" + "{\r\n" +
                                                                                        "\t\t" + fieldStr
                                                                                      +byteNumStr+ "return num;\r\n\t\t}\r\n" +
                                                                                      "\t\t"+WrittingStr+"return bytes;\r\n\t\t}\r\n\t\t" +
                                                                                      ReadingStr+ "return index-beginIndex;\r\n\t\t}\r\n\t\t"
                                                                                      + "}" + "\r\n}";
            string path = SAVE_PATH + nameSpace + "/Data/";
            string serverpath=SERVER_PATH+nameSpace+"/Data/";
            WriteScript(clientOrserver, path, serverpath, name, script);
        }
    }
    public static void GenerateMsg(XmlNodeList MsgNodes)
    {
        string nameSpace = "";
        string name = "";
        string fieldStr = "";
        string byteNumStr = "";
        string WrittingStr = "";
        string ReadingStr = "";
        string clientOrserver = "";
        string id = "";
        string MsgPoolScript = "using System;\r\nusing System.Collections;\r\nusing System.Collections.Generic;\r\nusing UnityEngine;\r\n"+$"namespace GameMsg" + "{" + "\r\n" +
                                                  "\t\t" + $"public class MsgPool" + "{\r\n\t\t" +
                                                      " public static Dictionary<int,Type> ID_Dic= new Dictionary<int,Type>();\r\n  " +
                                                      "  public static Dictionary<int,Type> Handler_Dic=new Dictionary<int,Type>();\r\n"
                                                      + "private static MsgPool instance = new MsgPool();\r\n    public static MsgPool Instance => instance;\r\n"+
                                                      "public void Register(int id,Type MsgType,Type HandlerType)\r\n    " +
                                                      "{\r\n        ID_Dic.Add(id,MsgType);\r\n        Handler_Dic.Add(id,HandlerType);\r\n    }\r\n   " +
                                                      " public  BaseMsg GetMsg(int id)\r\n    {\r\n        if(ID_Dic.ContainsKey(id))\r\n        {\r\n            return (BaseMsg)Activator.CreateInstance(ID_Dic[id]);\r\n        }\r\n        return null;\r\n    }\r\n   " +
                                                      " public   BaseHandler GetHandler(int id)\r\n    {\r\n        if (Handler_Dic.ContainsKey(id))\r\n        {\r\n            return (BaseHandler)Activator.CreateInstance(Handler_Dic[id]);\r\n        }\r\n        return null;\r\n    }"
                                                      + " public MsgPool()\r\n    {\r\n ";
        foreach (XmlNode enumNode in MsgNodes)

        {
            nameSpace = enumNode.Attributes["namespace"].Value;
            name = enumNode.Attributes["name"].Value;
            id= enumNode.Attributes["id"].Value;
            clientOrserver= enumNode.Attributes["clientOrserver"].Value;
            XmlNodeList fields = enumNode.SelectNodes("field");
            fieldStr = "";
            byteNumStr = "";
            WrittingStr = "";
            ReadingStr = "";
            byteNumStr += "public override int GetBytesNum()\r\n\t\t{\r\n\t\t";
            byteNumStr += "int num=0;\r\n\t\t"+ "num+=4;\r\n\t\tnum+=4;\r\n\t\t";
            WrittingStr += "public override byte[] Writting()\r\n\t\t{\r\n" +
                "\t\t" + "int num = GetBytesNum();\r\n\t\tint index=0;\r\n\t\tbyte[] bytes=new byte[GetBytesNum()];\r\n\t\t" + "WriteInt(bytes,GetID(),ref index);\r\n\t\t" +
                "WriteInt(bytes, num - 8, ref index);\r\n\t\t";

            ReadingStr += "public override int Reading(byte[] bytes,int beginIndex=0)\r\n\t\t{\r\n" +
                "\t\t" + "int index=beginIndex;\r\n\t\t";
           
            GenerateHandler(enumNode, nameSpace,clientOrserver);
            GenerateMsgPool(ref MsgPoolScript,id,name,nameSpace);
            string script = "";
            foreach (XmlNode field in fields)
            {
                GetBytesNumStr(ref byteNumStr, field);
                GetWrittingStr(ref WrittingStr, field);
                GetReadingStr(ref ReadingStr, field);
                
                string type = field.Attributes["type"].Value;
                string access = field.Attributes["access"].Value;
                if (access=="public")
                {
                    fieldStr += "public  ";
                }else if (access=="private")
                {
                    fieldStr += "private  ";
                }
                else if (access=="protected")
                {
                    fieldStr += "protected  ";
                }
                
                if (type == "array")
                {
                    fieldStr += field.Attributes["data"].Value + " [] ";
                }
                else if (type == "list")
                {
                    fieldStr += "List" + "<" + field.Attributes["T"].Value + "> ";
                }
                else if (type == "dic")
                {
                    fieldStr += "Dictionary" + "<" + field.Attributes["Tkey"].Value + "," + field.Attributes["Tval"].Value + "> ";
                }
                else
                {
                    fieldStr += type+" ";
                    script += "using GamePlayer;\r\n";
                }
                fieldStr += field.Attributes["name"].Value;
                if (field.InnerText != "")
                {
                    fieldStr += " = " + field.InnerText;
                }
                fieldStr += ";\r\n\t\t";
            }

            script+="using System.Collections.Generic;\r\nusing System.Text;\r\n" + $"namespace {nameSpace}" + "{" + "\r\n" +
                                                    "\t\t" + $"public class {name}:BaseMsg" + "{\r\n" +
                                                                                        "\t\t" + fieldStr
                                                                                      + byteNumStr + "return num;\r\n\t\t}\r\n" +
                                                                                      "\t\t" + WrittingStr + "return bytes;\r\n\t\t}\r\n\t\t" +
                                                                                      ReadingStr + "return index-beginIndex;\r\n\t\t}\r\n\t\t"+
                                                                                      "public override int GetID(){return " + id +";}\r\n\t\t"
                                                                                        + "}" + "\r\n}";
           
            string path = SAVE_PATH + nameSpace + "/Msg/";
            string serverpath = SERVER_PATH + nameSpace + "/Msg/";
            WriteScript(clientOrserver, path, serverpath, name, script);
        }
        MsgPoolScript += "\r\n\t\t\t\t}\r\n\t\t}\r\n}";
        string path2 = SAVE_PATH + "MsgPool/";
        string serverpath2 = SERVER_PATH + "MsgPool/";
        WriteScript(clientOrserver, path2, serverpath2, "MsgPool", MsgPoolScript);
    }
    public static void GenerateHandler(XmlNode field,string nameSpace,string clientOrserver)
    {
        string name=field.Attributes["name"].Value;

        string HandlerScript = $"namespace {nameSpace}" + "{" + "\r\n" +
                                                    "\t\t" + $"public class {name}Handler:BaseHandler" + "{\r\n\t\t" +
                                                        "public override void HandlerDo(){" + nameSpace + "." + $"{name} message=msg as  " +nameSpace + "."+ name +";"+"}\r\n\t\t"
                                                        +"}\r\n}";
        string path = SAVE_PATH + nameSpace + "/Handler/";
        string severpath = SERVER_PATH + nameSpace + "/Handler/";
        WriteScript(clientOrserver, path, severpath, name+"Handler", HandlerScript);
    }
    public static void GenerateMsgPool(ref string MsgPoolScript,string id, string name, string nameSpace)
    {

        MsgPoolScript += $"    Register({id},typeof(" + nameSpace +"."+ name + "),typeof(" + nameSpace + "." + name + "Handler" + "));\r\n";
    }
    private static void GetBytesNumStr(ref string byteNumStr, XmlNode field)
    {
        string type = field.Attributes["type"].Value;
        string name=field.Attributes["name"].Value;
        if (type=="array")
        {
            byteNumStr+= "num+=4;\r\n\t\t";
          string data=field.Attributes["data"].Value;
            byteNumStr +="for(int i=0;i<"+name+".Length;i++)\r\n" +
                "\t\t{\r\n\t\t\t"+"num+="+GetOtherByteNum(data, name+"[i]")+";\r\n\t\t}\r\n\t\t";
        }
        else if(type=="list")
        {
            byteNumStr += "num+=4;\r\n\t\t";
            string T = field.Attributes["T"].Value;
            byteNumStr += "for(int i=0;i<" + name + ".Count;i++)\r\n" +
                "\t\t{\r\n\t\t\t" + "num+=" + GetOtherByteNum(T, name + "[i]") + ";\r\n\t\t}\r\n\t\t";
        }
        else if(type=="dic")
        {
            byteNumStr += "num+=4;\r\n\t\t";
            string key = field.Attributes["Tkey"].Value;
            string value = field.Attributes["Tval"].Value;
            
            byteNumStr+="foreach("+key+" key in "+name+".Keys)\r\n\t\t{\r\n" +
                "\t\t\t"+ "num+=" + GetOtherByteNum(key, "key") + ";\r\n" +
            "\t\t\t" +"num+=" + GetOtherByteNum(value, name+"[key]") + ";\r\n" +
                "\t\t}\r\n" +
                "\t\t";
        }
        else
        {
            byteNumStr += "num+=" + GetOtherByteNum(type, name)+";\r\n\t\t";
        }

    }
   
    private static string GetOtherByteNum(string type,string name)
    {
        string str = "";
        switch (type)
        {
            case "int":
            case "float":
            case "enum":
                str += "4";
                break;
            case "bool":
                str += "1";
                break;
            case "string":
                str += "4;\r\n\t\t";
                str +="num+=Encoding.UTF8.GetByteCount(" + name + ")";
                break;
            case "long":
                str += "8";
                break;
            default:
                return name + ".GetBytesNum()";
                break;
        }
        return str;
    }
    private static void GetWrittingStr(ref string WrittingStr,XmlNode field)
    {
        string type = field.Attributes["type"].Value;
        string name = field.Attributes["name"].Value;
        if (type == "array")
        {
           WrittingStr+= "\t\tWriteInt(bytes," + name + ".Length,ref index);\r\n\t\t";
            string data = field.Attributes["data"].Value;
            WrittingStr += "for(int i=0;i<" + name + ".Length;i++)\r\n" +
                "\t\t{\r\n\t\t\t" + GetOtherWrittingStr(data, name + "[i]") + "}\r\n\t\t";
        }
        else if (type == "list")
        {
            WrittingStr += "\t\tWriteInt(bytes," + name + ".Count,ref index);\r\n\t\t";
            string T = field.Attributes["T"].Value;
            WrittingStr += "for(int i=0;i<" + name + ".Count;i++)\r\n" +
                "\t\t{\r\n\t\t\t" + GetOtherWrittingStr(T, name + "[i]") + "}\r\n\t\t";
        }
        else if (type == "dic")
        {
           WrittingStr += "\t\tWriteInt(bytes," + name + ".Count,ref index);\r\n\t\t";
            string key = field.Attributes["Tkey"].Value;
            string value = field.Attributes["Tval"].Value;
            WrittingStr += "foreach(" + key + " key in " + name + ".Keys)\r\n\t\t{\r\n" +
                "\t\t\t" + GetOtherWrittingStr(key, "key") + "\t\t\r\n" +
            "\t\t\t" + GetOtherWrittingStr(value, name + "[key]") + "\t\t\r\n" +
                "\t\t}\r\n" +
                "\t\t";
        }
        else
        {
            WrittingStr +=GetOtherWrittingStr(type,name);
        }

    }
    private static string GetOtherWrittingStr(string type,string name)
    {
        string str = "";
        switch (type)
        {
            case "int":
                str+= "WriteInt(bytes," + name+",ref index);\r\n\t\t";
                break;
            case "float":
                str+= "WriteFloat(bytes," + name + ",ref index);\r\n\t\t";
                break;
            case "enum":
                str += "WriteInt(bytes," + $"BitConverter.ToInt32({name})" + ",ref index);\r\n\t\t";
                break;
            case "bool":
                str+= "WriteBool(bytes," + name + ",ref index);\r\n\t\t";
                break;
            case "short":
             str += "WriteShort(bytes," + name + ",ref index);\r\n\t\t";
                break;
            case "string":
                str += "WriteString(bytes," + name + ",ref index);\r\n\t\t";
                break;
            case "long":
                str += "WriteLong(bytes," + name + ",ref index);\r\n\t\t";
                break;
            default:
                return "WriteData(bytes," + name + ",ref index);\r\n\t\t";
                break;
        }
        return str;
    }
    private static void  GetReadingStr(ref string ReadingStr, XmlNode field)
    {
        string type = field.Attributes["type"].Value;
        string name = field.Attributes["name"].Value;
        if (type == "array")
        {
            ReadingStr += "\t\t"+name+"=new "+field.Attributes["data"].Value+"[ReadInt(bytes,ref index)];\r\n\t\t";
            string data = field.Attributes["data"].Value;
            ReadingStr += "for(int i=0;i<"+name+".Length;i++)\r\n" +
                "\t\t{\r\n\t\t\t" + GetOtherReadingStr(data, name + "[i]") + "}\r\n\t\t";
        }
        else if (type == "list")
        {
            ReadingStr += "\t\t" + name + "=new List<"+field.Attributes["T"].Value+">();\r\n\t\t"+
                "for(int i=0;i<ReadInt(bytes,ref index);i++)\r\n" +
                "\t\t{\r\n\t\t\t" + field.Attributes["T"].Value+" "+ GetOtherReadingStr(field.Attributes["T"].Value, "temp") + name+".Add(temp);\r\n\t\t}\r\n\t\t";
            
        }
        else if (type == "dic")
        {
            ReadingStr += "\t\t" + name + "=new Dictionary<"+field.Attributes["Tkey"].Value+","+field.Attributes["Tval"].Value+">();\r\n\t\t" +
                "for(int i=0;i<ReadInt(bytes,ref index);i++)\r\n" +
                "\t\t{\r\n\t\t\t" + field.Attributes["Tkey"].Value + " "+ GetOtherReadingStr(field.Attributes["Tkey"].Value, "key") + field.Attributes["Tval"].Value + " " 
                + GetOtherReadingStr(field.Attributes["Tval"].Value, "value") + name+".Add(key,value);\r\n\t\t}\r\n\t\t";
        }
        else
        {
            ReadingStr += GetOtherReadingStr(type,name);
        }
    }
    private static string GetOtherReadingStr(string type,string name)
    {
        string str = "";
        switch (type)
        {
            case "int":
                str += name+"=ReadInt(bytes,ref index);\r\n\t\t";
                break;
            case "float":
                str += name + "=ReadFloat(bytes,ref index);\r\n\t\t";
                break;
            case "enum":
                str += name + "=(" + type + ")ReadInt(bytes,ref index);\r\n\t\t";
                break;
            case "bool":
                str += name + "=ReadBool(bytes,ref index);\r\n\t\t";
                break;
            case "short":
                str += name + "=ReadShort(bytes,ref index);\r\n\t\t";
                break;
            case "string":
                str += name + "=ReadString(bytes,ref index);\r\n\t\t";
                break;
            case "long":
                str+= name + "=ReadLong(bytes,ref index);\r\n\t\t";
                break;
            default:
                return name+"=ReadData<"+type+">(bytes,ref index);\r\n\t\t";
                break;
        }
        return str;
    }
}
