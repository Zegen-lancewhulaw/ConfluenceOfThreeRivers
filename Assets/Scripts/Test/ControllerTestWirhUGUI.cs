using LitJson;
using Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ControllerTestWirhUGUI : MonoBehaviour
{
    // ========================================
    // 1. 配置区域
    // ========================================
    [Header("UI")]
    [SerializeField]
    private Text content;
    [SerializeField]
    private Text speakerName;

    // ========================================
    // 2. 公共调用接口
    // ========================================

    public void NextDialogue()
    {
        if (_currentNode == null) return;
        PlayNode(_currentNode.nextId);
        _dialogueForward = true;
    }

    // ========================================
    // 3. 运行时状态
    // ========================================
    private Dictionary<string, DialogueNode> _dialogueMap;
    private DialogueNode _currentNode;
    private bool _dialogueForward;

    // ========================================
    // 4. 生命周期
    // ========================================

    // ---初始化---
    private void Start()
    {
        LoadJson();
        PlayNode("line_01"); // 开始时，播放第一句
        _dialogueForward = true;
    }

    private void Update()
    {
        if (_dialogueForward && _currentNode != null)
        {
            if(speakerName != null)
            {
                speakerName.text = _currentNode.speaker;
            }
            if(content != null)
            {
                content.text = _currentNode.content;
            }
            _dialogueForward = false;
        }
    }

    // ========================================
    // 5. 私有逻辑方法
    // ========================================

    /// <summary>
    /// 读取Json剧本并加载到运行时
    /// </summary>
    void LoadJson()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "test_chapter.json");

        if (File.Exists(filePath))
        {
            string jsonStr = File.ReadAllText(filePath);

            ChapterModel chapter = JsonMapper.ToObject<ChapterModel>(jsonStr);
            _dialogueMap = chapter.dialogues.ToDictionary<DialogueNode, string>((x) => x.id);

            print("Json 加载完毕，节点数：" + _dialogueMap.Count);
            foreach (var i in _dialogueMap)
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
        if (id == "END")
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
