using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SFB;
using System;
using System.Collections;

public static class UnityUtils
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Open a download dialog and download given text data to disk using file system.
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    // Called from browser
    public void OnFileDownload() {
        Debug.Log("OnFileDownload");
    }

#endif

    public static void SaveTextFile(string _data, string name="sample", string ext="txt")
    {
#if UNITY_WEBGL && !UNITY_EDITOR

        var bytes = System.Text.Encoding.UTF8.GetBytes(_data);
        DownloadFile("gameObject.name", "OnFileDownload", $"{name}.{ext}", bytes, bytes.Length);

#else

        var path = SFB.StandaloneFileBrowser.SaveFilePanel("Title", "", name, ext);
        if (!string.IsNullOrEmpty(path)) {
            System.IO.File.WriteAllText(path, _data);
        }

#endif
    }
}