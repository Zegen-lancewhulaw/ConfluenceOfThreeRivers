using AVG.Model;
using LitJson;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

namespace AVG
{
    public static class SaveManager
    {
        public static int MaxSaveEntryCount { get => 37; private set => MaxSaveEntryCount = value; }
        private static string saveFilePath;
        

        public static async void SaveAsync(int saveIdNum, SaveEntry saveEntry)
        {
            saveFilePath = GetSaveFilePath(saveIdNum);

            string json = JsonMapper.ToJson(saveEntry);
            await Task.Run(() => {
                File.WriteAllText(saveFilePath, json);
            });

            Debug.Log($"´æµµ{saveIdNum}±£´æ³É¹¦£¡");
        }

        public static async Task<SaveEntry> LoadAsync(int saveIdNum)
        {
            saveFilePath = GetSaveFilePath(saveIdNum);
            if (File.Exists(saveFilePath)){
                string json = await File.ReadAllTextAsync(saveFilePath);
                return JsonMapper.ToObject<SaveEntry>(json);
            }
            else
            {
                return null;
            }
        }

        private static string GetSaveFilePath(int saveIdNum)
        {
            if (saveIdNum < 0 || saveIdNum >= MaxSaveEntryCount)
            {
                Debug.LogWarning($"´æµµ±àºÅ\"{saveIdNum}\"´íÎó");
                return null;
            }

            return Path.Combine(Application.persistentDataPath, $"save{saveIdNum}.json");
        }
    }
}
