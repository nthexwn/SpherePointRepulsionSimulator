using UnityEngine;
using UnityEditor;
using System.IO;

// Based on https://support.unity.com/hc/en-us/articles/115000341143-How-do-I-read-and-write-data-from-a-text-file-
public class TextHandler
{
    public static void Print(string str)
    {
        Debug.Log(str);
        WriteString(str);
    }

    public static void WriteString(string str)
    {
        // Write the line
        string path = "Assets/Resources/log.txt";
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(str);
        writer.Close();

        // Re-import the file to update the reference in the editor
        AssetDatabase.ImportAsset(path);
        TextAsset asset = (TextAsset)Resources.Load("log");
    }

    public static string ReadString()
    {
        string path = "Assets/Resources/log.txt";
        StreamReader reader = new StreamReader(path);
        string str = reader.ReadToEnd();
        reader.Close();
        return str;
    }
}