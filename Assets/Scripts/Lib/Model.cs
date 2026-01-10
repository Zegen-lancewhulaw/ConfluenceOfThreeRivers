using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Model{
    // ========================================
    // 0. 数据模型 (model)
    // ========================================
    public class ChapterModel
    {
        public string chapterId;
        public List<DialogueNode> dialogues;
    }

    public class DialogueNode
    {
        public string id;
        public string speaker;
        public string content;
        public string nextId;
        public List<OptionNode> options;
    }

    public class OptionNode
    {
        public string text;
        public string targetId;
    }
}
