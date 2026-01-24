using AVG.Model;
using LitJson;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AVG
{
    public static class SaveManager
    {
        public static int MaxSaveEntryCount { get => 37; private set => MaxSaveEntryCount = value; }
        private static string saveFilePath;
        
        static SaveManager()
        {
            // 检查存档文件是否都存在
            for(int i = 0; i < MaxSaveEntryCount; i++)
            {
                saveFilePath = GetSaveFilePath(i);
                if (!File.Exists(saveFilePath))
                {
                    File.WriteAllText(saveFilePath, JsonMapper.ToJson(new SaveEntry()));
                }
            }
            
        }

        public static void Save(int saveIdNum, SaveEntry saveEntry)
        {
            saveFilePath = GetSaveFilePath(saveIdNum);
            if(File.Exists(saveFilePath)) File.WriteAllText(saveFilePath, JsonMapper.ToJson(saveEntry));
        }

        public static SaveEntry Load(int saveIdNum)
        {
            saveFilePath = GetSaveFilePath(saveIdNum);
            if (File.Exists(saveFilePath)){
                return JsonMapper.ToObject<SaveEntry>(File.ReadAllText(saveFilePath));
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
                Debug.LogWarning($"存档编号\"{saveIdNum}\"错误");
                return null;
            }

            return Path.Combine(Application.persistentDataPath, $"save{saveIdNum}.json");
        }
    }
}
