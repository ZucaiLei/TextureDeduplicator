using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class TextureDeduplicatorMenu
{
    [MenuItem("Assets/纹理（检查是否重复）", true)]
    private static bool Validate() => Selection.activeObject is Texture2D;

    [MenuItem("Assets/纹理（检查是否重复）")]
    private static void FindSameImages()
    {
        var target = Selection.activeObject as Texture2D;
        if (target == null)
            return;

        string path = AssetDatabase.GetAssetPath(target);
        string guid = AssetDatabase.AssetPathToGUID(path);
        string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), path);

        TextureDeduplicatorCache.Load();
        string targetHash = TextureDeduplicatorCache.GetHash(guid, fullPath, ComputeMD5);

        string[] allGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        List<string> matches = new();

        foreach (var otherGuid in allGuids)
        {
            if (otherGuid == guid)
                continue;

            string otherPath = AssetDatabase.GUIDToAssetPath(otherGuid);
            string otherFullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), otherPath);
            string otherHash = TextureDeduplicatorCache.GetHash(otherGuid, otherFullPath, ComputeMD5);
            if (otherHash == targetHash)
                matches.Add(otherPath);
        }

        TextureDeduplicatorCache.Save();
        TextureDeduplicatorWindow.Show(path, matches);
    }

    private static string ComputeMD5(byte[] bytes)
    {
        using var md5 = MD5.Create();
        byte[] hashBytes = md5.ComputeHash(bytes);
        StringBuilder sb = new();
        foreach (byte b in hashBytes) 
            sb.Append(b.ToString("x2"));
        
        return sb.ToString();
    }
}