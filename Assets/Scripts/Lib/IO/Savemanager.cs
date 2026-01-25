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
        private static string saveScreenshotPath;
        private static string saveHeadersPath = Path.Combine(Application.persistentDataPath, "save_headers.json");
        public static SaveHeaders saveHeaders;

        public static async void EnsureSaveHeadersExisting()
        {
            if (saveHeaders != null) return;
            saveHeaders = await ReadSaveHeadersAsync();
        }

        /// <summary>
        /// 根据传入的存档头数据 和 对应的游戏截图，更新内存中用于显示存档界面的存档头数据，并写入存储
        /// </summary>
        /// <param name="saveHeader">新的存档头</param>
        /// <param name="screenshot">对应游戏截图</param>
        public static async Task WriteSaveHeadersAsync(SaveHeader saveHeader, Texture2D screenshot)
        {
            // --- 更新内存中的存档头数据 ---
            // 1. 确保内存中有存档头数据
            if(saveHeaders == null)
            {
                Task<SaveHeaders> task = ReadSaveHeadersAsync();
                await task;
                if (task.IsCompletedSuccessfully && task.Result != null)
                {
                    saveHeaders = task.Result;
                }
                else
                {
                    Debug.LogError("读取存档头失败！");
                    return;
                }
            }

            // 2. 更新存档头数据
            if (saveHeaders.headers.ContainsKey(saveHeader.saveIdNum))
            {
                saveHeaders.headers[saveHeader.saveIdNum] = saveHeader;
            }
            else
            {
                saveHeaders.headers.Add(saveHeader.saveIdNum, saveHeader);
            }

            // 要求写入的新的存档头一定是最新的存档
            saveHeaders.latestSaveEntryId = saveHeader.saveIdNum;

            // --- 内存中的存档头数据更新完毕，开始写入存储 ---
            // 1. 写入存档头
            string json = JsonMapper.ToJson(saveHeaders);
            await File.WriteAllTextAsync(saveHeadersPath, json);
            
            // 2. 写入截图
            if(screenshot != null)
            {
                string screenshotPath = GetSaveScreenshotPath(saveHeader.saveIdNum);
                byte[] jpgBytes = screenshot.EncodeToJPG(50);
                await File.WriteAllBytesAsync(screenshotPath, jpgBytes);
            }

            Debug.Log($"存档{saveHeader.saveIdNum}的存档头写入 完成:{saveHeadersPath}");
        }

        /// <summary>
        /// 将存档头数据获取到内存
        /// </summary>
        public static async Task<SaveHeaders> ReadSaveHeadersAsync()
        {
            if (File.Exists(saveHeadersPath))
            {
                string json = await File.ReadAllTextAsync(saveHeadersPath);
                return JsonMapper.ToObject<SaveHeaders>(json);
            }
            else
            {
                SaveHeaders newSaveHeaders = new SaveHeaders();
                string json = JsonMapper.ToJson(newSaveHeaders);
                Task task = File.WriteAllTextAsync(saveHeadersPath, json);
                await task;
                if (task.IsCompletedSuccessfully)
                {
                    return newSaveHeaders;
                }
                else
                {
                    Debug.LogError("写入新的存档头失败！");
                    return null;
                }
            }
        }

        public static async Task SaveAsync(string saveIdNum, SaveEntry saveEntry)
        {
            saveFilePath = GetSaveFilePath(saveIdNum);

            string json = JsonMapper.ToJson(saveEntry);
            await Task.Run(() => {
                File.WriteAllText(saveFilePath, json);
            });

            Debug.Log($"存档{saveIdNum}保存成功:{saveFilePath}");
        }

        public static async Task<SaveEntry> LoadAsync(string saveIdNum)
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

        private static string GetSaveFilePath(string saveIdNumStr)
        {
            int saveIdNum  = int.Parse(saveIdNumStr);
            if (saveIdNum < 0 || saveIdNum >= MaxSaveEntryCount)
            {
                Debug.LogWarning($"存档编号\"{saveIdNum}\"错误");
                return null;
            }

            return Path.Combine(Application.persistentDataPath, $"save{saveIdNum}.json");
        }

        private static string GetSaveScreenshotPath(string saveIdNumStr)
        {
            int saveIdNum = int.Parse(saveIdNumStr);       
            if (saveIdNum < 0 || saveIdNum >= MaxSaveEntryCount)
            {
                Debug.LogWarning($"存档编号\"{saveIdNum}\"错误");
                return null;
            }

            return Path.Combine(Application.persistentDataPath, $"save{saveIdNum}.jpg");
        }
    }
}
