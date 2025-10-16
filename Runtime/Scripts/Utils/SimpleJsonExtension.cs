using System;
using SimpleJSON;

public static class JSONNodeExtensions
{
    // Check if key exists (case-insensitive)
    public static bool HasKeyCI(this JSONNode node, string key)
    {
        // Try exact match first (fastest)
        if (node.HasKey(key)) return true;
        
        // Try case-insensitive search
        foreach (var kvp in node)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    
    // Get value with case-insensitive key
    public static JSONNode GetKeyCI(this JSONNode node, string key)
    {
        // Try exact match first (fastest)
        if (node.HasKey(key)) return node[key];
        
        // Try case-insensitive search
        foreach (var kvp in node)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                return node[kvp.Key];
        }
        return null;
    }
}