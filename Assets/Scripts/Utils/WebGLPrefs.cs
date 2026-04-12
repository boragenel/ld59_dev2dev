using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLPrefs
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void   SaveToLocalStorage(string key, string value);
    [DllImport("__Internal")] private static extern string LoadFromLocalStorage(string key);
    [DllImport("__Internal")] private static extern void   RemoveFromLocalStorage(string key);
#endif


    const string Prefix = "replicat_";

    static string WithPrefix(string key) => Prefix + key;

    // ---------- String ----------

    public static void SetString(string key, string value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SaveToLocalStorage(WithPrefix(key), value);
#else
        PlayerPrefs.SetString(WithPrefix(key), value);
#endif
    }

    public static string GetString(string key, string defaultValue = "")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var v = LoadFromLocalStorage(WithPrefix(key));
        return string.IsNullOrEmpty(v) ? defaultValue : v;
#else
        return PlayerPrefs.GetString(WithPrefix(key), defaultValue);
#endif
    }

    // ---------- Int ----------

    public static void SetInt(string key, int value)
    {
        SetString(key, value.ToString());
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        var s = GetString(key, null);
        if (string.IsNullOrEmpty(s))
            return defaultValue;

        if (int.TryParse(s, out var v))
            return v;

        return defaultValue;
    }

    // ---------- Float ----------

    public static void SetFloat(string key, float value)
    {
        // Use InvariantCulture so it’s consistent regardless of user locale.
        SetString(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        var s = GetString(key, null);
        if (string.IsNullOrEmpty(s))
            return defaultValue;

        if (float.TryParse(
                s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var v))
            return v;

        return defaultValue;
    }

    // ---------- Misc ----------

    public static bool HasKey(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var v = LoadFromLocalStorage(WithPrefix(key));
        return !string.IsNullOrEmpty(v);
#else
        return PlayerPrefs.HasKey(WithPrefix(key));
#endif
    }

    public static void DeleteKey(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RemoveFromLocalStorage(WithPrefix(key));
#else
        PlayerPrefs.DeleteKey(WithPrefix(key));
#endif
    }
}
