using Steamworks;
using UnityEngine;

public static class LobbyCodeHelper
{
    /// <summary>
    /// Converts a Steam lobby ID to a short alphanumeric code.
    /// Example: 109775243966098918 → "XKQP72"
    /// </summary>
    public static string ToCode(ulong lobbyID)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        ulong value = lobbyID;
        string code = "";

        for (int i = 0; i < 6; i++)
        {
            code = chars[(int)(value % (ulong)chars.Length)] + code;
            value /= (ulong)chars.Length;
        }
        return code;
    }

    /// <summary>
    /// Converts a code back to a lobby ID.
    /// Returns 0 if invalid.
    /// </summary>
    public static ulong FromCode(string code)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        code = code.ToUpper().Trim();
        ulong value = 0;

        foreach (char c in code)
        {
            int index = chars.IndexOf(c);
            if (index < 0) return 0;
            value = value * (ulong)chars.Length + (ulong)index;
        }
        return value;
    }
}