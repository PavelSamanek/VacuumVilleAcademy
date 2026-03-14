using System.IO;
using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Core
{
    public static class SaveSystem
    {
        private static readonly string SavePath =
            Path.Combine(Application.persistentDataPath, "player_progress.json");

        public static void Save(PlayerProgress progress)
        {
            try
            {
                string json = JsonUtility.ToJson(progress, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        public static PlayerProgress Load()
        {
            if (!File.Exists(SavePath))
                return new PlayerProgress();

            try
            {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerProgress>(json) ?? new PlayerProgress();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load: {e.Message}");
                return new PlayerProgress();
            }
        }

        public static void Delete()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
