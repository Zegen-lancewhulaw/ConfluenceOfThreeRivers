using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LitJson;
using System.Linq;
using Model;

public class ControllerTestWithGUI : MonoBehaviour
{

    // ========================================
    // 1. 配置区域
    // ========================================
    [Header("对话框")]
    public Rect dialogueBoxRect;
    public GUIStyle dialogueBoxStyle;
    [Space()]
    [Header("继续")]
    public Rect nextBtnRect;
    public GUIStyle nextBtnStyle;
    [Space()]
    [Header("选项框")]
    public Rect optionsLayoutRect;
    public GUIStyle optionStyle;

    // ========================================
    // 1. 运行时状态
    // ========================================
    private Dictionary<string, DialogueNode> _dialogueMap;
    private DialogueNode _currentNode;

    // ========================================
    // 2. 生命周期
    // ========================================

    // ---初始化---
    private void Start()
    {
        LoadJson();
        PlayNode("line_01"); // 开始时，播放第一句
    }

    // ---GUI测试---
    private void OnGUI()
    {
        // 如果当前已经没有对话节点，显示 游戏结束
        if(_currentNode == null)
        {
            GUI.Label(dialogueBoxRect, "游戏结束", dialogueBoxStyle);
        }
        // 否则将 对话节点中的内容 显示在对话框 或 选项栏中
        else
        {
            // 显示 对话节点中对话
            GUI.Label(dialogueBoxRect, _currentNode.content, dialogueBoxStyle);

            // 若有选择，则显示 选项栏
            if (_currentNode.options != null && _currentNode.options.Count > 0)
            {
                GUI.BeginGroup(optionsLayoutRect);
                foreach (var opt in _currentNode.options)
                {
                    if (GUILayout.Button(opt.text, optionStyle))
                    {
                        PlayNode(opt.targetId);
                    }
                }
                GUI.EndGroup();
            }
            // 否则显示 继续
            else
            {
                if (GUI.Button(nextBtnRect, "继续", nextBtnStyle))
                {
                    PlayNode(_currentNode.nextId);
                }
            }
        }
    }

    // ========================================
    // 3. 私有逻辑方法
    // ========================================

    /// <summary>
    /// 读取Json剧本并加载到运行时
    /// </summary>
    void LoadJson()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "test_chapter.json");

        if(File.Exists(filePath))
        {
            string jsonStr = File.ReadAllText(filePath);

            ChapterModel chapter = JsonMapper.ToObject<ChapterModel>(jsonStr);
            _dialogueMap = chapter.dialogues.ToDictionary<DialogueNode, string>((x) => x.id);

            print("Json 加载完毕，节点数：" + _dialogueMap.Count);
            foreach(var i in _dialogueMap)
            {
                print(i.Key);
            }
        }
        else
        {
            print("找不到 Json 文件：" + filePath);
        }
    }

    /// <summary>
    /// 播放给定id编号的对话结点
    /// </summary>
    /// <param name="id">对话节点的id编号</param>
    void PlayNode(string id)
    {
        if(id == "END")
        {
            _currentNode = null;
            print("剧情结束！");
            return;
        }

        if (_dialogueMap.ContainsKey(id))
        {
            _currentNode = _dialogueMap[id];
        }
        else
        {
            print("找不到结点ID：" + id);
        }
    }
}
