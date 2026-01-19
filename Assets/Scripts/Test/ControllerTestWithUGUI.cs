using LitJson;
using Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class ControllerTestWithUGUI : MonoBehaviour
{
    #region 配置区域

    [Header("System-Hide")]
    [Tooltip("处于隐藏状态时，用于取消隐藏的全屏按钮")]
    [SerializeField] private GameObject hideModeLayer;
    [Tooltip("处于隐藏状态时，被隐藏的UI层")]
    [SerializeField] private GameObject[] uiLayersToHide;

    [Header("System-History")]
    [Tooltip("整个历史记录层")]
    [SerializeField] private GameObject historyLayer;
    [Tooltip("历史记录滚动视窗上的ScrollRect")]
    [SerializeField] private ScrollRect historyScrollRect;
    [Tooltip("容纳历史记录项目的容器")] 
    [SerializeField] private Transform historyContent;
    [Tooltip("历史记录项目预制体")]
    [SerializeField] private GameObject historyItemPrefab;
    [Tooltip("历史记录最大条目数")]
    [SerializeField] private int maxHistoryItemCount = 100;

    [Header("System-AutoPlay")]
    [Tooltip("自动播放按钮文本")]
    [SerializeField] private Text autoPlayButtonText;
    [Tooltip("自动播放时间间隔(s)")]
    [SerializeField] private float autoPlayInterval = 3f; // 从对话框文字全部显示完毕时开始到自动播放下一个对话为止的时间间隔

    [Header("System-Skip")]
    [Tooltip("是否跳过章节剧情提示窗预制体")]
    [SerializeField] private GameObject skipInformationPopupPrefab;
    [Tooltip("提示信息弹窗容器")]
    [SerializeField] private Transform informationContainer;

    [Header("Dialog")]
    [SerializeField] private Text DialogueContent; // 对话框文字内容
    [SerializeField] private Text speakerName; // 说话者名字
    [SerializeField] private AudioSource nextDialogueAudio; // 点击对话框进入下一个对话结点时候的音效
    [Tooltip("打字机速度")]
    [SerializeField] private float typingInterval = 0.1f; // 打字时间间隔

    [Header("Options")]
    [Tooltip("选项组容器")]
    [SerializeField] private Transform optionContainer; // 选项组容器
    [Tooltip("选项按钮预制体")]
    [SerializeField] private GameObject optionPrefab; // 选项按钮预制体

    [Header("Mask and Overlay")]
    [SerializeField] private GameObject maskLayer; // 遮罩层
    [SerializeField] private GameObject overlayLayer; // 覆盖层

    [Header("Visuals")]
    [Tooltip("背景图片")]
    [SerializeField] private Image bgImage;
    [Tooltip("左侧人物立绘")]
    [SerializeField] private Image charLeft;
    [Tooltip("中间人物立绘")]
    [SerializeField] private Image charCenter;
    [Tooltip("右侧人物立绘")]
    [SerializeField] private Image charRight;
    [Tooltip("CG层")]
    [SerializeField] private GameObject cgLayer;
    [Tooltip("CG图片")]
    [SerializeField] private Image cgImage;


    #endregion

    #region 公共调用接口
    /// <summary>
    /// 当玩家点击“隐藏”按钮时触发
    /// </summary>
    public void ToggleHideMode()
    {
        _isHidden = !_isHidden;

        // 处理全屏取消隐藏按键
        if(hideModeLayer != null)
        {
            hideModeLayer.SetActive(_isHidden);
        }

        // 处理需要或者已经被隐藏的UI层
        foreach(var layer in uiLayersToHide)
        {
            layer.SetActive(!_isHidden);
        }

    }

    /// <summary>
    /// 当玩家点击“历史”按钮时触发
    /// </summary>
    public void ToggleHistoryMode()
    {
        _isHistory = !_isHistory;

        // 处理历史记录层
        historyLayer.SetActive(_isHistory);

        // 打开时，自动滚动到底部
        StartCoroutine(AutoScrollToBottom());
    }

    /// <summary>
    /// 当玩家点击“自动”按钮时触发
    /// </summary>
    public void ToggleAutoMode()
    {
        _isAutoPlay = !_isAutoPlay;
        if (_isAutoPlay)
        {
            autoPlayButtonText.text = "自动中…";
        }
        else
        {
            autoPlayButtonText.text = "自动";
        }
    }

    /// <summary>
    /// 当玩家点击“跳过”按钮时触发
    /// </summary>
    public void Skip()
    {
        maskLayer.SetActive(true);
        overlayLayer.SetActive(true);
        _isOverlaid = true;
        informationContainer.gameObject.SetActive(true);

        GameObject infoPopupObj = Instantiate(skipInformationPopupPrefab, informationContainer);

        Information_YorN infoPopupScript = infoPopupObj.GetComponent<Information_YorN>();

        infoPopupScript.SetInformationText("确认要跳过本章剧情？");
        infoPopupScript.SetYesText("确认");
        infoPopupScript.SetNoText("取消");
        infoPopupScript.SetCloseEffect(() =>
        {
            informationContainer.gameObject.SetActive(false);
            overlayLayer.SetActive(false);
            _isOverlaid = false;
            maskLayer.SetActive(false);
        });
    }

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


            DialogueContent.text = _currentNode.content;
            _isTyping = false;
            return;
        }

        // 停止自动播放协程
        if (_isAutoPlay && _autoPlayCoroutine != null)
        {
            StopCoroutine(_autoPlayCoroutine);
        }

        // 触发点击音效
        if (nextDialogueAudio != null)
        {
            nextDialogueAudio.PlayOneShot(nextDialogueAudio.clip);
        }

        // 如果不处于打字状态，且是最后一个对话结点
        if (_currentNode != null && _currentNode.nextId == "END")
        {
            print("本章节剧情播放结束");
            
        }
        // 如果不处于打字状态，且不是最后一个对话结点
        else if(_currentNode != null)
        {
            // 播放下一个结点
            PlayNode(_currentNode.nextId);
        }
    }
    #endregion

    #region 运行时状态

    private Dictionary<string, DialogueNode> _dialogueMap;
    private DialogueNode _currentNode;

    private bool _isTyping = false; // 是否正在打字
    private Coroutine _typeTextCoroutine = null; // 用于记录已经开启的打字机协程
    private Stack<string> tagStack = new Stack<string>(); // 记录富文本标签名的栈，用于自动追加闭合标签
    private List<string> tagList = new List<string>(); // 记录富文本标签名的列表，用于自动追加开标签

    private bool _isOverlaid = false; // 是否处于被覆盖层覆盖的状态

    private bool _isHidden = false; // 是否处于隐藏状态

    private bool _isHistory = false; // 是否处于历史记录状态

    private bool _isAutoPlay = false; // 是否处于自动播放状态
    private Coroutine _autoPlayCoroutine = null; // 用于记录所启动的自动播放下一个结点的协程

    private Dictionary<Image, string> imageSpriteNameDic = new Dictionary<Image, string>(); // 用于记录Image对象目前承载的图片的名字
    private Dictionary<Image, int> _imageLoadScopeDic = new Dictionary<Image, int>(); // 用于记录Image更新时竞争请求的最新版本号
    private Dictionary<string, AsyncOperationHandle<Sprite>> spriteHandlesDic = new Dictionary<string, AsyncOperationHandle<Sprite>>(); // 用于记录成功缓存的精灵图片句柄
    private Dictionary<string, int> spriteHandleCountDic = new Dictionary<string, int>(); // 用于记录对缓存中某一精灵图片的引用数
    #endregion

    #region 生命周期

    // ---初始化---
    private void Start()
    {
        LoadJson("test_chapter2.json");

        if(cgLayer  != null) // 初始时，关闭CG层
        {
            cgLayer.SetActive(false);
        }

        if(charLeft != null) // 初始时，左侧立绘透明
        {
            charLeft.color = new Color(1, 1, 1, 0);
        }

        if (charCenter != null) // 初始时，中间立绘透明
        {
            charCenter.color = new Color(1, 1, 1, 0);
        }

        if (charRight != null) // 初始时，右侧立绘透明
        {
            charRight.color = new Color(1, 1, 1, 0);
        }

        if (hideModeLayer != null) // 初始时，关闭全局取消隐藏按键
        {
            hideModeLayer.SetActive(false);
        }

        if (historyLayer != null) // 初始时，关闭历史记录层
        {
            historyLayer.SetActive(false);
        }

        if(optionContainer != null) // 初始时，关闭选项容器
        {
            optionContainer.gameObject.SetActive(false);
        }

        if(informationContainer != null) // 初始时，关闭提示信息弹窗容器
        {
            informationContainer.gameObject.SetActive(false);
        }

        if (maskLayer != null) // 初始时，关闭遮罩层
        {
            maskLayer.SetActive(false);
        }

        if (overlayLayer != null) // 初始时，关闭覆盖层
        {
            overlayLayer.SetActive(false);
            _isOverlaid = false;
        }

        // 初始记录被控制的若干Image容器当前存储的图片的名字
        imageSpriteNameDic.Add(bgImage, null);
        imageSpriteNameDic.Add(charLeft, null);
        imageSpriteNameDic.Add(charCenter, null);
        imageSpriteNameDic.Add(charRight, null);
        imageSpriteNameDic.Add(cgImage, null);

        // 初始化被控制的若干Image容器 当变更时 最新请求的版本号
        _imageLoadScopeDic.Add(bgImage, 0);
        _imageLoadScopeDic.Add(charLeft, 0);
        _imageLoadScopeDic.Add(charCenter, 0);
        _imageLoadScopeDic.Add(charRight, 0);
        _imageLoadScopeDic.Add(cgImage, 0);

        PlayNode("line_01"); // 开始时，播放第一句
    }

    private void Update()
    {
        DetermineToAutoPlay();
    }
    #endregion

    #region 私有逻辑方法

    /// <summary>
    /// 读取Json剧本并加载到运行时
    /// </summary>
    /// <param name="fileName">要加载的Json剧本的文件名</param>
    void LoadJson(string fileName)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, fileName);

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
        // 如果要播放的结点id存在
        if (_dialogueMap.ContainsKey(id))
        {
            _currentNode = _dialogueMap[id]; // 将要播放的结点设置为 当前结点
            string targetName = _currentNode.speaker; // 取当前结点的 说话者名字
            string targetStr = _currentNode.content; // 取当前结点的 内容
            DialogueContent.text = ""; // 清空对话框内容

            // 1. 设置说话者名字
            speakerName.text = targetName;

            // 2. 生成历史记录条目
            AddHistoryItem(targetName, targetStr, false);

            // 3. 启动打字机协程，将当前播放结点的content打字到对话框上
            _typeTextCoroutine = StartCoroutine(TypeText(targetStr));

            // 4. 检测是否有选项
            if(_currentNode.options != null && _currentNode.options.Count > 0)
            {
                StartCoroutine(ShowOptionsAfterTyping());
            }

            // 5. 检测是否有背景图片变化
            if(_currentNode.background != null)
            {
                UpdateImage(bgImage, _currentNode.background);
            }

            // 6. 检测是否有立绘变化
            if(_currentNode.charLeft != null)
            {
                UpdateImage(charLeft, _currentNode.charLeft);
            }

            if(_currentNode.charCenter != null)
            {
                UpdateImage(charCenter, _currentNode.charCenter);
            }

            if(_currentNode.charRight != null)
            {
                UpdateImage(charRight, _currentNode.charRight);
            }

            // 7. 检测是否有CG变化
            if(_currentNode.cgImage != null)
            {
                UpdateImage(cgImage, _currentNode.cgImage);
            }
        }
    }

    /// <summary>
    /// 一个打字机协程，用于对话框等文字内容的打字机效果展示
    /// </summary>
    /// <param name="targrtStr">要打印的最终文字</param>
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

            DialogueContent.text += additionStr;

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
        _isOverlaid = true;
        optionContainer.gameObject.SetActive(true); // 打开选项组容器

        foreach (var opt in _currentNode.options)
        {
            // 1. 生成Option预制体
            GameObject option = Instantiate(optionPrefab, optionContainer);

            // 2. 闭包捕获变量
            string textOfOpt = opt.text;
            string targetIdOfOpt = opt.targetId;

            // 3. 设置Option文字
            option.GetComponentInChildren<Text>().text = textOfOpt;

            // 4. 绑定点击事件
            Button btn = option.GetComponent<Button>();

            btn.onClick.AddListener(() => { OnOptionClicked(textOfOpt, targetIdOfOpt); });
        }
    }

    /// <summary>
    /// 用于绑定对话选项的回调函数
    /// </summary>
    /// <param name="targetId">所选选项的下一个对话结点的Id</param>
    void OnOptionClicked(string text, string targetId)
    {
        // 1. 将被点击的内容做成历史记录条目添加
        AddHistoryItem("",text ,true);

        // 2. 销毁所有选项按钮，关闭遮罩层和覆盖层
        foreach (Transform childOption in optionContainer)
        {
            Destroy(childOption.gameObject);
        }

        maskLayer.SetActive(false); // 关闭遮罩层
        overlayLayer.SetActive(false); // 关闭覆盖层
        _isOverlaid = false;
        optionContainer.gameObject.SetActive(false); // 关闭选项组容器

        // 3. 播放所选选项指向的对话结点
        PlayNode(targetId);
    }

    /// <summary>
    /// 将已经播放的对话的说话者名字、说话内容，或者选项标志、选项内容以历史记录条目的形式
    /// 加入到历史记录层的滚动视窗中
    /// </summary>
    /// <param name="name">说话者名字 或者 当是选项的时候为“选项”二字</param>
    /// <param name="content">说话内容 或者 选项内容</param>
    /// <param name="isOption">是否为选项</param>
    private void AddHistoryItem(string name, string content, bool isOption)
    {
        // 1. 实例化预制体
        GameObject itemObj = Instantiate(historyItemPrefab, historyContent);


        HistoricalDialogueItem itemScript = itemObj.GetComponent<HistoricalDialogueItem>();

        #region 调试信息
        if(itemScript != null)
        {
            print("找到HistoricalDialogueItem脚本");
        }
        else
        {
            print("没找到HistoricalDialogueItem脚本");
        }
        #endregion

        // 2. 填入名字字段
        if (isOption)
        {
            itemScript.SetName("选项");
        }
        else
        {
            itemScript.SetName(name);
        }

        // 3. 填入内容字段
        itemScript.SetContent(content);

        // 4. 限制数量
        // 如果历史记录条目数超过最大允许数量，则删除最早的一个条目，以节省内存
        if(historyContent.childCount > maxHistoryItemCount)
        {
            Destroy(historyContent.GetChild(0).gameObject);
        }

        // 5. 如果历史记录界面处于打开状态，则自动滚动到底部
        if (historyLayer.activeSelf)
        {
            StartCoroutine(AutoScrollToBottom());
        }
    }

    /// <summary>
    /// 自动将历史记录界面滚动到底端的协程函数
    /// </summary>
    private IEnumerator AutoScrollToBottom()
    {
        yield return new WaitForEndOfFrame(); // 等待这一帧结束，因为UI的高度不是实时刷新，而是下一帧才算出来

        historyScrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// 决定是否启动自动播放下一个结点协程
    /// </summary>
    private void DetermineToAutoPlay()
    {
        // 如果当前处于自动播放状态 并且 目前没有启动自动播放协程 并且 不处于一系列不应当启动协程的状态
        // 则应当启动自动播放下一个结点协程
        if (_autoPlayCoroutine == null && _isAutoPlay && !_isTyping && !_isHidden && !_isOverlaid && !_isHistory)
        {
            if (_currentNode != null && _currentNode.nextId != "END")
            {
                // 启动自动播放下一个结点协程
                _autoPlayCoroutine = StartCoroutine(AutoPlayNode(_currentNode.nextId, () => {
                    _autoPlayCoroutine = null;
                }));
            }
        }
        // 如果自动播放下一个结点的协程已经被启动
        // 但是状态演变为不应当启动，则要停止协程
        else if (_autoPlayCoroutine != null)
        {
            if (!_isAutoPlay || _isHidden || _isOverlaid || _isHistory)
            {
                StopCoroutine(_autoPlayCoroutine);
                _autoPlayCoroutine = null;
                return;
            }
        }
    }

    /// <summary>
    /// 自动播放下一个对话结点的协程
    /// </summary>
    /// <param name="id">要播放的下一对话结点的id</param>
    /// <param name="act">外部实现的用于置空_autoPlayCoroutine变量的函数</param>
    /// <returns></returns>
    private IEnumerator AutoPlayNode(string id, UnityAction act)
    {
        // 1. 第一步，等待指定时间
        yield return new WaitForSeconds(autoPlayInterval);

        // 2. 第二步，播放
        PlayNode(id);

        // 3. 第三步，置空_autoPlayCoroutine变量
        act.Invoke();
    }

    /// <summary>
    /// 用来异步更新背景图片、立绘图片的函数
    /// </summary>
    /// <param name="image">要变化的承载图片资源的Image对象</param>
    /// <param name="spriteName">图片资源的名字</param>
    private async void UpdateImage(Image image, string spriteName)
    {
        // --- 1. 获取当前状态 ---
        string currentSpriteName = imageSpriteNameDic[image];

        // --- 2. 核心逻辑锁：只要进这个函数，就是一次新请求，获取其版本号 ---
        if (!_imageLoadScopeDic.ContainsKey(image)) _imageLoadScopeDic[image] = 0;
        int myScopeId = ++_imageLoadScopeDic[image];

        // --- 3. 卫语句：处理特殊情况 ---

        // 情况A：指令无效
        if (string.IsNullOrEmpty(spriteName)) return;

        // 情况B：指令是清除图片
        if (spriteName == "REMOVE")
        {
            PerformSetSprite(image, null, null); // 执行清理
            return;
        }

        // 情况C：请求的图片就是当前显示的图片
        if (currentSpriteName == spriteName) return;


        // --- 4. 获取资源 (异步或同步) ---
        Sprite spriteToSet = null;

        if (spriteHandlesDic.ContainsKey(spriteName))
        {
            // === 命中缓存 ===
            // 同步拿到资源，不需要等待
            if (spriteHandlesDic[spriteName].IsValid()) // 最好检查一下Handle是否有效
            {
                spriteToSet = spriteHandlesDic[spriteName].Result;
            }
        }
        else
        {
            // === 未命中缓存，发起异步加载 ===
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteName);

            // 等待加载...
            await handle.Task;

            // === 异步归来，立刻检查版本号 ===
            // 如果在我等待期间，又有新的 UpdateImage(image, ...) 被调用过
            // 那么 _imageLoadScopedDic[image] 肯定大于 myScopeId
            if (_imageLoadScopeDic[image] != myScopeId)
            {
                // 我是过期的请求，我的结果没用了，释放掉
                Addressables.Release(handle);
                return; // 结束，不更新UI
            }

            // 加载失败处理
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                Debug.LogWarning($"[Addressables] 资源加载失败: {spriteName}");
                return;
            }

            // 加载成功，存入缓存
            spriteHandlesDic.Add(spriteName, handle);
            spriteToSet = handle.Result;
        }

        // --- 5. 二次检查 (防御性编程) ---
        // 无论是同步还是异步拿到资源，最后应用前再查一次锁（防止极端逻辑漏洞）
        if (_imageLoadScopeDic[image] != myScopeId) return;


        // --- 6. 执行最终应用逻辑 (统一出口) ---
        PerformSetSprite(image, spriteName, spriteToSet);
    }

    /// <summary>
    /// 执行最终的图片替换、引用计数更新、UI状态切换
    /// </summary>
    private void PerformSetSprite(Image image, string newSpriteName, Sprite newSprite)
    {
        // 1. 记录旧名字 (用于释放)
        string oldSpriteName = imageSpriteNameDic[image];

        // 2. 更新 UI 和 字典
        image.sprite = newSprite;
        imageSpriteNameDic[image] = newSpriteName;

        // 3. 引用计数管理
        // 新图计数 +1 (如果是 REMOVE，newSpriteName 为 null，函数中不会进行任何操作)
        IncreaseSpriteReference(newSpriteName);

        // 旧图计数 -1
        ReleaseSpriteReference(oldSpriteName);

        // 4. 特殊 UI 状态处理 (立绘显隐 / CG层开关)
        if (image == charLeft || image == charCenter || image == charRight)
        {
            UpdateCharImage(image, newSprite == null); // null 意味着 REMOVE
        }
        else if (image == cgImage)
        {
            UpdateCGImage(image, newSprite, newSprite == null); // null 意味着 REMOVE
        }
    }

    void IncreaseSpriteReference(string spriteName)
    {
        if (!string.IsNullOrEmpty(spriteName))
        {
            if (spriteHandleCountDic.ContainsKey(spriteName))
                spriteHandleCountDic[spriteName]++;
            else
                spriteHandleCountDic.Add(spriteName, 1);

            #region 调试信息
            print($"资源{spriteName}引用数+1，当前计数：{spriteHandleCountDic[spriteName]}");
            #endregion
        }
    }

    void ReleaseSpriteReference(string spriteName)
    {
        if(spriteName != null && spriteHandleCountDic.ContainsKey(spriteName) && spriteHandleCountDic[spriteName] > 0)
        {
            spriteHandleCountDic[spriteName]--;
            #region 调试信息
            print($"资源{spriteName}引用数-1，当前计数：{spriteHandleCountDic[spriteName]}");
            #endregion
            if (spriteHandleCountDic[spriteName] == 0)
            {
                if (spriteHandlesDic.ContainsKey(spriteName)){
                    Addressables.Release(spriteHandlesDic[spriteName]);
                    spriteHandlesDic.Remove(spriteName);
                    spriteHandleCountDic.Remove(spriteName);
                    #region 调试信息
                    print($"已释放计数为0的资源：{spriteName}");
                    #endregion
                }
            }
        }
    }

    void UpdateCharImage(Image image, bool isREMOVE)
    {
        // 如果是在对话结点指示REMOVE此立绘Image时调用，则隐藏之
        if (isREMOVE)
        {
            image.color = new Color(1, 1, 1, 0);
        }
        // 否则显示之
        else
        {
            image.color = Color.white;
        }
    }

    void UpdateCGImage(Image image, Sprite sprite, bool isRemove)
    {
        // 如果是在对话结点指示REMOVE此CGImage时调用，则关闭CG层
        if (isRemove)
        {
            cgLayer.SetActive(false);
        }
        // 否则开启CG层，设置CG图片，开启隐藏模式
        else
        {
            cgLayer.SetActive(true);
            cgImage.sprite = sprite;
            ToggleHideMode();
        }
    }

    
    #endregion
}
