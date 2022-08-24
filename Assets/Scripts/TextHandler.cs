using UnityEngine;
using UnityEditor;
using System.IO;

// Based on https://support.unity.com/hc/en-us/articles/115000341143-How-do-I-read-and-write-data-from-a-text-file-
public class TextHandler
{
    private FileStream stream;
    private StreamReader reader;
    private StreamWriter writer;

    public TextHandler(string path)
    {
        stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        reader = new StreamReader(stream);
        writer = new StreamWriter(stream);
    }
    public string Read()
    {
        return reader.ReadToEnd();
    }

    public void Print(string str)
    {
        Debug.Log(str);
        Write(str);
    }

    private void Write(string str)
    {
        writer.Write(str);

        // Re-import the file to update the reference in the editor
        //AssetDatabase.ImportAsset(path);
        //TextAsset asset = (TextAsset)Resources.Load("log");
    }
}
