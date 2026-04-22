using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class ProtocolTool
{
    private static string protocolPath = Application.dataPath + "/Editor/Protocol/protocolInfo.xml";
    [MenuItem("ProtocolTool/生成C#协议代码")]
   private static void GenerateCSharp()
    {
        GenerateCSharpScript.GenerateEnum(GetNodes("enum"));
        GenerateCSharpScript.GenerateData(GetNodes("Data"));
        GenerateCSharpScript.GenerateMsg(GetNodes("message"));
        AssetDatabase.Refresh();
    }
    [MenuItem("ProtocolTool/生成C++协议代码")]
    private static void GenerateCpp()
    {

    }
    [MenuItem("ProtocolTool/生成Java协议代码")]
    private static void GenerateJava()
    {

    }
    private static XmlNodeList GetNodes(string name)
    {
        XmlDocument xmlDocument = new XmlDocument();
       xmlDocument.Load(protocolPath);
      XmlNode root=  xmlDocument.SelectSingleNode("messages");
     return   root.SelectNodes(name);
    }
}
