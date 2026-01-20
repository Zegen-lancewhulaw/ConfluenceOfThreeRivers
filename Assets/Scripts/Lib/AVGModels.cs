using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVG.Model{
    public class ChapterModel
    {
        public string chapterId;
        public List<DialogueNode> dialogues;
    }

    public class DialogueNode
    {
        // 对话结点编号
        public string id;

        // 说话者
        public string speaker;
        // 说话内容
        public string content;


        // 背景图路径，如果为空则表示维持上一张背景图不变
        public string background;

        // 立绘图路径，如果为空则表示维持该位置维持不变
        public string charLeft; // 左侧立绘
        public string charCenter; // 中间立绘
        public string charRight; // 右侧立绘

        // CG图路径，如果为空则表示维持不变
        public string cgImage;

        // 下一个对话结点的编号
        public string nextId;

        // 选项列表
        public List<OptionNode> options;
    }

    public class OptionNode
    {
        public string text;
        public string targetId;
    }
}
