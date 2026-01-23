using LitJson;
using AVG.Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AVG
{
    public static class ScriptsManager
    {
        /// <summary>
        /// 将存储的json文件读取并还原为指定的数据类型。
        /// 通常用于读取并还原 「章节」 ChapterModel
        /// </summary>
        /// <typeparam name="T">指定的数据类型</typeparam>
        /// <param name="fileName">带后缀的文件名</param>
        /// <param name="directoryPath">目录路径</param>
        /// <returns></returns>
        public static T LoadJson<T>(string fileName, string directoryPath) where T : class
        {
            // 当传入的文件名没有后缀时添加后缀
            if (!fileName.EndsWith(".json"))
            {
                fileName += ".json";
            }

            // 获取绝对路径
            string filePath = Path.Combine(directoryPath, fileName);

            // 尝试读取文件
            if (File.Exists(filePath))
            {
                string jsonStr = File.ReadAllText(filePath);
                return JsonMapper.ToObject<T>(jsonStr);
            }
            else
            {
                #region 调试信息
                Debug.LogWarning("找不到 Json 文件：" + filePath);
                #endregion

                return null;
            }
        }

        /// <summary>
        /// 用于从给定的 「章节」 中提取其中的对话结点
        /// </summary>
        /// <param name="chapter">给定的章节数据</param>
        /// <returns>以对话结点的id为键，以对话结点本身为值的字典</returns>
        public static Dictionary<string, DialogueNode> GetDialoguesDic(ChapterModel chapter)
        {
            // --- 1. 处理无效情况 ---
            if(chapter == null) return null;
            if(chapter.dialogues == null) return null;

            // --- 2. 正常情况 ---
            return chapter.dialogues.ToDictionary<DialogueNode, string>((x) => x.id);
        }
    }
}
