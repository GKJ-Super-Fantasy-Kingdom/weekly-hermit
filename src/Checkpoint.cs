using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SuperFantasyKingdom;

namespace WeeklyHermit
{
    // Couple the decision to the snapshot that contains its effects. A retry
    // from an earlier dawn must never lose the offer or duplicate a saved unit.
    internal static class Checkpoint
    {
        private static int resolvedDay;
        internal static bool Resolved => resolvedDay == DaytimeManager.Instance.CurrentDay();
        internal static void Resolve() => resolvedDay = DaytimeManager.Instance.CurrentDay();
        internal static ManualLogSource Log;

        private static string Marker(GameData data)
        {
            var key = Helper.GenerateHash(string.Join("|", SettingsManager.Instance.GetProfile(),
                data.runID, data.kingdom, data.day, data.lastPlayed, data.hash));
            return Path.Combine(Paths.ConfigPath, "WeeklyHermit", "checkpoints", key + ".done");
        }

        [HarmonyPatch(typeof(GameDataManager), nameof(GameDataManager.Load))]
        private static class Load
        {
            // The game clears its hash during Load. Read its saved value first.
            private static void Prefix(GameData ___m_GameData)
            {
                resolvedDay = ___m_GameData != null && File.Exists(Marker(___m_GameData))
                    ? ___m_GameData.day + 1 : 0;
            }
        }

        [HarmonyPatch(typeof(GameDataManager), "GenerateGameData")]
        private static class Save
        {
            private static void Postfix(GameData __result)
            {
                // The game stores the dawn that will load as a zero-based day.
                if (__result == null || resolvedDay == 0 || __result.day + 1 != resolvedDay)
                    return;
                try
                {
                    string path = Marker(__result);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    // File existence is the entire record, so no partial payload can be read.
                    using (File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) { }
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    Log.LogWarning("Could not record the saved Hermit decision: " + e.Message);
                }
            }
        }
    }
}
