using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AVG.Model
{
    // 成就解锁情况
    [Serializable]
    public static class Achievements
    {
        public static Dictionary<string, bool> achievements;
    }

    // CG解锁情况
    [Serializable]
    public static class UnlockedCGs
    {
        public static Dictionary<string, bool> unlockedCGs;
    }

    // 全部结点已读情况
    [Serializable]
    public static class ExperiencedNodes
    {
        public static Dictionary<string, ExperiencedNodesOfChapter> chapter;
    }

    // 某章节结点已读情况
    [Serializable]
    public class ExperiencedNodesOfChapter
    {
        public Dictionary<string, bool> node;
    }
}
