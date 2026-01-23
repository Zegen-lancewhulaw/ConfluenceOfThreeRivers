using System;
using System.Collections;
using System.Collections.Generic;

namespace AVG.Model
{
    // 结点旅程
    [Serializable]
    public class NodeJourney
    {
        public List<string> list = new List<string>();
    }

    // 选择旅程
    [Serializable]
    public class ChoiceJourney
    {
        // key为选项组所在的对话结点的id，value为玩家所选择的选项的id
        public Dictionary<string, string> dic = new Dictionary<string, string>();
    }

    // 历史记录
    [Serializable]
    public class HistoryRecords : IEnumerable<HistoryRecord>
    {
        public List<HistoryRecord> list = new List<HistoryRecord>();

        public IEnumerator<HistoryRecord> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    // 历史记录的一个条目
    [Serializable]
    public struct HistoryRecord
    {
        public string name;
        public string content;

        public HistoryRecord(string name, string content)
        {
            this.name = name;
            this.content = content;
        }
    }

    // 一个存档
    [Serializable]
    public class SaveEntry
    {
        // --- 元数据 ---
        public string saveId; // 存档的标识
        public string saveTime; // "yyyy-MM-dd HH:mm"

        // --- 游戏现场 ---
        // 1. 章节进度
        public string chapterId;
        public string chapterName;
        public string nodeId;
        public NodeJourney nodeJourney;
        public ChoiceJourney choiceJourney;

        // 2. 历史记录
        public HistoryRecords historyRecords;

        // 3. 视觉快照
        public string bgName;
        public string charLeftName;
        public string charCenterName;
        public string charRightName;
        public string cgName;

        // 4. 音乐
        public string bgmName;

        // --- 玩家数据 ---
        // 1. 男主好感度
        public Dictionary<string, int> favorabilities;

        // 2. 背包
        public Dictionary<string, int> bag;

    }

    public interface IRuntimeRecorder
    {
        SaveEntry PrePareSaveEntry(string saveId);
    }
}
