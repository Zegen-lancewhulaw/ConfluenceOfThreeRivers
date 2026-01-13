using LitJson;
using Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ControllerTestWithUGUI : MonoBehaviour
{
    // ========================================
    // 1. 配置区域
    // ========================================
    [Header("UI")]
    [SerializeField] private Text content; // 对话框文字内容
    [SerializeField] private Text speakerName; // 说话者名字

    [Header("Options")]
    [SerializeField] private Transform optionContainer; // 选项组容器
    [SerializeField] private GameObject optionPrefab; // 选项按钮预制体

    [Header("其他")]
    [Tooltip("打字机速度")]
    [SerializeField] private float typingInterval = 0.1f; // 打字时间间隔
    [SerializeField] private GameObject maskLayer; // 遮罩层
    [SerializeField] private GameObject overlayLayer; // 覆盖层

    // ========================================
    // 2. 公共调用接口
    // ========================================

    /// <summary>
    /// 当玩家点击对话框（上的Button）的时候触发
    /// </summary>
    public void NextDialogue()
    {
        // 如果正在打字
        if (_isTyping)
        {
            StopCoroutine(_typeTextCoroutine); // 停止打字机协程
            //打字机协程函数可能没有走到结尾就被停止，函数末尾的清空富文本标签栈和列表的语句可能未执行
            //保险起见，在此执行之
            if(tagList.Count > 0)
            {
                tagList.Clear();
            }
            if(tagStack.Count > 0)
            {
                tagStack.Clear();
            }


            content.text = _currentNode.content;
            _isTyping = false;
            return;
        }
        // 如果不处于打字状态，且是最后一个对话结点
        if(_currentNode != null && _currentNode.nextId == "END")
        {
            print("本章节剧情播放结束");
            
        }
        // 如果不处于打字状态，且不是最后一个对话结点
        else if(_currentNode != null)
        {
            PlayNode(_currentNode.nextId);
        }
    }

    // ========================================
    // 3. 运行时状态
    // ========================================
    private Dictionary<string, DialogueNode> _dialogueMap;
    private DialogueNode _currentNode;
    private bool _isTyping = false; // 是否正在打字
    private Coroutine _typeTextCoroutine = null; // 用于记录已经开启的打字机协程
    private Stack<string> tagStack = new Stack<string>(); // 记录富文本标签名的栈，用于自动追加闭合标签
    private List<string> tagList = new List<string>(); // 记录富文本标签名的列表，用于自动追加开标签
    

    // ========================================
    // 4. 生命周期
    // ========================================

    // ---初始化---
    private void Start()
    {
        LoadJson();

        if(maskLayer != null) // 初始时，关闭遮罩层
        {
            maskLayer.SetActive(false);
        }

        if (overlayLayer != null) // 初始时，关闭覆盖层
        {
            overlayLayer.SetActive(false);
        }

        PlayNode("line_01"); // 开始时，播放第一句
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
        if (_dialogueMap.ContainsKey(id))
        {
            _currentNode = _dialogueMap[id];
            string targetStr = _currentNode.content;
            content.text = "";

            // 1. 设置说话者名字
            speakerName.text = _currentNode.speaker;

            // 2. 启动打字机协程，将当前播放结点的content打字到对话框上
            _typeTextCoroutine = StartCoroutine(TypeText(targetStr));

            // 3. 检测是否有选项
            if(_currentNode.options != null && _currentNode.options.Count > 0)
            {
                StartCoroutine(ShowOptionsAfterTyping());
            }
        }
    }

    /// <summary>
    /// 一个打字机协程，用于对话框等文字内容的打字机效果展示
    /// </summary>
    /// <param name="container">承载打出来的字的地方</param>
    /// <param name="targrtStr">要打印的最终文字</param>
    /// <param name="typingInterval">打字时隙</param>
    /// <returns></returns>
    IEnumerator TypeText(string targetStr)
    {
        // 调试信息
        print("打字机协程已启动");

        _isTyping = true;

        int cnt = targetStr.Length;

        for(int i = 0; i < cnt; i++)
        {
            char c = targetStr[i];

            // 检测到富文本标签特征
            if(c == '<')
            {
                int closeIndex = targetStr.IndexOf('>', i);
                if(closeIndex != -1)
                {
                    // 整个标签
                    string fullTag = targetStr.Substring(i, closeIndex - i + 1);
                    // 是否是闭标签
                    bool isClosingTag = fullTag.StartsWith("</");

                    // 如果是闭标签，弹栈，出列表
                    if (isClosingTag)
                    {
                        tagStack.Pop();
                        tagList.RemoveAt(tagList.Count - 1);
                    }
                    //如果是开标签，压栈，入列表
                    else
                    {
                        // 进入tagList的是开标签，不需要特别处理
                        tagList.Add(fullTag);

                        // 压入tagStack的标签应该是闭合标签，为此要特别处理
                        int equalIndex = fullTag.IndexOf('=');
                        if (equalIndex != -1)
                        {
                            fullTag = fullTag.Remove(equalIndex, fullTag.Length - equalIndex - 1);
                        }
                        fullTag = fullTag.Insert(1, "/");
                        tagStack.Push(fullTag);
                    }
                    i = closeIndex;
                    continue;
                }
            }

            // 正常字符处理方式
            string additionStr = "";
            foreach(string tag in tagList)
            {
                additionStr += tag;
            }
            additionStr += c;
            foreach(string tag in tagStack)
            {
                additionStr += tag;
            }

            content.text += additionStr;

            yield return new WaitForSeconds(typingInterval);
        }

        // 保险起见，清空标签栈和列表
        tagStack.Clear();
        tagList.Clear();

        _isTyping = false;
    }

    /// <summary>
    /// 用于生成对话结点中的 选项组 的协程函数
    /// </summary>
    IEnumerator ShowOptionsAfterTyping()
    {
        // 直到打字机打印全部文字后再显示选项
        yield return new WaitUntil(() => _isTyping == false);

        maskLayer.SetActive(true); // 打开遮罩层
        overlayLayer.SetActive(true); // 打开覆盖层

        foreach (var opt in _currentNode.options)
        {
            // 1. 生成Option预制体
            GameObject option = Instantiate(optionPrefab, optionContainer);

            // 2. 设置Option文字
            option.GetComponentInChildren<Text>().text = opt.text;

            // 3. 绑定点击事件
            Button btn = option.GetComponent<Button>();
            string targetId = opt.targetId; // 闭包捕获变量
            btn.onClick.AddListener(() => { OnOptionClicked(targetId); });
        }
    }

    /// <summary>
    /// 用于绑定对话选项的回调函数
    /// </summary>
    /// <param name="targetId">所选选项的下一个对话结点的Id</param>
    void OnOptionClicked(string targetId)
    {
        foreach (Transform childOption in optionContainer)
        {
            Destroy(childOption.gameObject);
        }

        maskLayer.SetActive(false); // 关闭遮罩层
        overlayLayer.SetActive(false); // 关闭覆盖层

        PlayNode(targetId);
    }
}
