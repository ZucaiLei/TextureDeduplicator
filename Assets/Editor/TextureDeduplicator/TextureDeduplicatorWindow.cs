using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

public class TextureDeduplicatorWindow : EditorWindow
{
    private static string sourcePath;
    private static string sourceGUID;
    private static List<string> matches;
    private static Dictionary<string, List<string>> allDuplicateMap;
    private Vector2 scroll;
    private Vector2 allScroll;
    private bool showAllSection = false;

    public static void Show(string path, List<string> found)
    {
        sourcePath = path;
        sourceGUID = AssetDatabase.AssetPathToGUID(path);
        matches = found;
        allDuplicateMap = null;
        GetWindow<TextureDeduplicatorWindow>("相同图片资源").Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("原始图片:", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        Texture2D newTex = (Texture2D)EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath), typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck() && newTex != null)
        {
            string path = AssetDatabase.GetAssetPath(newTex);
            string guid = AssetDatabase.AssetPathToGUID(path);
            sourcePath = path;
            sourceGUID = guid;
            matches = new();
            Retry();
            Repaint();
            return;
        }

        EditorGUILayout.LabelField(sourcePath);

        /*if (GUILayout.Button("再次检查，是否存在相同图片", GUILayout.Height(24)))
        {
            Retry();
        }*/

        EditorGUILayout.Space();

        if (matches == null || matches.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到其他相同内容的图片", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("重复图片结果:", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(150));
            foreach (var path in matches)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("定位", GUILayout.Width(50)))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    EditorGUIUtility.PingObject(tex);
                    Selection.activeObject = tex;
                }
                EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<Texture2D>(path), typeof(Texture2D), false);
                EditorGUILayout.LabelField(path);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(10);

        showAllSection = EditorGUILayout.Foldout(showAllSection, "所有图片重复扫描", true, EditorStyles.foldoutHeader);
        if (showAllSection)
        {
            if (GUILayout.Button("📂 扫描所有图片中重复项", GUILayout.Height(28)))
            {
                ScanAllDuplicates();
            }

            if (allDuplicateMap != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("所有重复资源:", EditorStyles.boldLabel);
                allScroll = EditorGUILayout.BeginScrollView(allScroll);
                foreach (var kvp in allDuplicateMap)
                {
                    EditorGUILayout.LabelField("Hash: " + kvp.Key, EditorStyles.miniBoldLabel);
                    foreach (var path in kvp.Value)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("定位", GUILayout.Width(50)))
                        {
                            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                            EditorGUIUtility.PingObject(tex);
                            Selection.activeObject = tex;
                        }
                        EditorGUILayout.ObjectField(AssetDatabase.LoadAssetAtPath<Texture2D>(path), typeof(Texture2D), false);
                        EditorGUILayout.LabelField(path);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space(5);
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void Retry()
    {
        string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), sourcePath);
        if (!File.Exists(fullPath))
            return;

        EditorUtility.DisplayProgressBar("查找相同图片：", $"Doing some work...", 0.1f);
        TextureDeduplicatorCache.Load();
        string hash = TextureDeduplicatorCache.GetHash(sourceGUID, fullPath, ComputeMD5);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        var length = guids.Length;
        int i = 0;
        List<string> results = new();
        foreach (string guid in guids)
        {
            if (guid == sourceGUID)
                continue;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("查找相同图片：", $"Doing some work...{path}", i * 1.0f / length);
            string absPath = Path.Combine(Application.dataPath.Replace("Assets", ""), path);
            string otherHash = TextureDeduplicatorCache.GetHash(guid, absPath, ComputeMD5);
            if (otherHash == hash)
            {
                results.Add(path);
            }

            i++;
        }

        EditorUtility.ClearProgressBar();
        TextureDeduplicatorCache.Save();
        matches = results;
    }

    private string ComputeMD5(byte[] bytes)
    {
        using var md5 = MD5.Create();
        byte[] hashBytes = md5.ComputeHash(bytes);
        StringBuilder sb = new();
        foreach (byte b in hashBytes)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
    
    private void ScanAllDuplicates()
    {
        TextureDeduplicatorCache.Load();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        Dictionary<string, List<string>> hashToPaths = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string absPath = Path.Combine(Application.dataPath.Replace("Assets", ""), path);
            string hash = TextureDeduplicatorCache.GetHash(guid, absPath, ComputeMD5);
            if (hash == null) continue;
            if (!hashToPaths.ContainsKey(hash)) hashToPaths[hash] = new();
            hashToPaths[hash].Add(path);
        }

        TextureDeduplicatorCache.Save();
        allDuplicateMap = hashToPaths.Where(kv => kv.Value.Count > 1).ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
