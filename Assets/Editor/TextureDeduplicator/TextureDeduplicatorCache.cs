using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class TextureDeduplicatorCacheEntry
{
    public string guid;
    public string hash;
    public long fileSize;
    public long lastWriteTime;
}

[Serializable]
public class TextureDeduplicatorCacheData
{
    public List<TextureDeduplicatorCacheEntry> entries = new();
}

public static class TextureDeduplicatorCache
{
    private static string CachePath => Path.Combine(Application.dataPath, "../Library/.texture_deduplicator_cache.json");
    private static Dictionary<string, TextureDeduplicatorCacheEntry> cache = new();

    public static void Load()
    {
        cache.Clear();
        if (!File.Exists(CachePath))
            return;

        var json = File.ReadAllText(CachePath);
        var data = JsonUtility.FromJson<TextureDeduplicatorCacheData>(json);
        foreach (var entry in data.entries)
            cache[entry.guid] = entry;
    }

    public static void Save()
    {
        var data = new TextureDeduplicatorCacheData { entries = new List<TextureDeduplicatorCacheEntry>(cache.Values) };
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(CachePath, json);
    }

    public static string GetHash(string guid, string fullPath, Func<byte[], string> hashFunc)
    {
        if (!File.Exists(fullPath))
            return null;

        var info = new FileInfo(fullPath);
        long size = info.Length;
        long lastWrite = info.LastWriteTimeUtc.Ticks;
        if (cache.TryGetValue(guid, out var entry))
        {
            if (entry.lastWriteTime == lastWrite && entry.fileSize == size)
                return entry.hash;
        }

        byte[] bytes = File.ReadAllBytes(fullPath);
        string hash = hashFunc(bytes);
        cache[guid] = new TextureDeduplicatorCacheEntry { guid = guid, hash = hash, fileSize = size, lastWriteTime = lastWrite };
        return hash;
    }
}