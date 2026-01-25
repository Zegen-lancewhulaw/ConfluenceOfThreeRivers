using AVG;
using AVG.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class Controller : MonoBehaviour, IRuntimeRecorder
{
    #region 一、管理器引用
    [Header("Cooperative Managers")]
    public StageManager stageManager;
    public UIManager uiManager;
    public HistoryManager historyManager;
    #endregion

    #region 二、游戏状态机
    private GameState _currentState = GameState.Init;
    private GameState _previousState = GameState.Init;

    /// <summary>
    /// 切换状态统一入口
    /// </summary>
    private void ChangeGameState(GameState newState)
    {
        if(_currentState == newState) return;
        // 切换状态
        _previousState = _currentState;
        _currentState = newState;

        // 触发状态变更事件
        EventCenter.OnGameStateChanged?.Invoke();
    }

    private void ChangeGameStateToInit()
    {
        ChangeGameState(GameState.Init);
    }

    private void ChangeGameStateToNormal()
    {
        ChangeGameState(GameState.Normal);
    }

    private void ChangeGameStateToTyping()
    {
        ChangeGameState(GameState.Typing);
    }

    private void ChangeGameStateToInteracting()
    {
        ChangeGameState(GameState.Interacting);
    }

    private void ChangeGameStateToHistory()
    {
        ChangeGameState(GameState.History);
    }

    private void ChangeGameStateToPaused()
    {
        ChangeGameState(GameState.Paused);
    }

    private void ChangeGameStateToPrevious()
    {
        ChangeGameState(_previousState);
    }
    #endregion

    #region 三、职责

    #region 1. 章节管理

    #region （1）运行时变量
    private ChapterModel _currentChapter; // 当前正在播放的章节
    private Dictionary<string, DialogueNode> _dialoguesDic; // 当前章节中的对话结点的字典，以结点id为键
    private DialogueNode _currentNode; // 当前正在播放的对话结点
    #endregion （1）运行时变量

    #region （2）方法

    /// <summary>
    /// 获取当前要播放的章节及其中的对话结点
    /// </summary>
    /// <param name="currentChapter">当前章节变量</param>
    /// <param name="dialoguesDic">当前章节的对话结点字典，以结点id为键</param>
    private void InitCurrentChapter(string chapterId, out ChapterModel currentChapter, out Dictionary<string, DialogueNode> dialoguesDic)
    {
        currentChapter = ScriptsManager.LoadJson<ChapterModel>(chapterId, Application.streamingAssetsPath);

        if(currentChapter == null)
        {
            Debug.LogWarning("初始化章节失败");
        }
        else
        {
            print("初始化章节成功");
        }

        dialoguesDic = ScriptsManager.GetDialoguesDic(currentChapter);

        if(dialoguesDic == null)
        {
            Debug.LogWarning("提取对话结点失败");
        }
        else
        {
            print("提取对话结点成功");
            foreach(string key in dialoguesDic.Keys)
            {
                print(key);
            }
        }
    }

    /// <summary>
    /// 将指定id的结点设置为当前播放结点
    /// </summary>
    /// <param name="newId">要播放的结点的id</param>
    /// <returns>是否设置成功</returns>
    private bool SetCurrentNode(string newId)
    {
        if (_dialoguesDic.ContainsKey(newId))
        {
            _currentNode = _dialoguesDic[newId];
            return true;
        }
        else
        {
            #region 调试信息
            Debug.LogWarning($"结点{newId}不存在");
            #endregion
            return false;
        }
    }

    #endregion （2）方法

    #endregion 1. 章节管理

    #region 2. 播放结点

    #region （2）方法

    /// <summary>
    /// 当玩家点击对话框UI按钮时触发，用来决定接下来的动作
    /// </summary>
    private void DetermineToPlayNextNode()
    {
        if (_currentState != GameState.Normal && _currentState != GameState.Typing) return;

        if (_currentState == GameState.Typing)
        {
            uiManager.FinishUpdateDialogueBox(_currentNode.content);
        }
        else
        {
            PlayNextNode();
        }
    }

    /// <summary>
    /// 播放当前结点的下一个结点
    /// </summary>
    private void PlayNextNode()
    {
        if(_currentNode != null && _currentNode.nextId != null)
        {
            PlayNode(_currentNode.nextId);
        }
    }

    /// <summary>
    /// 玩家选择选项、选项互动结束后，更新选项旅程，播放目标结点
    /// </summary>
    /// <param name="optionId">选项的id</param>
    /// <param name="targetId">选项所指向的目标对话结点</param>
    private void PlayNodeAfterChoice(string optionId, string targetId)
    {
        // 更新选择旅程
        UpdateChoiceJourney(_currentNode.id, optionId);

        // 播放目标结点
        PlayNode(targetId);
    }

    /// <summary>
    /// 播放给定id编号的对话结点
    /// </summary>
    /// <param name="id">对话节点的id编号</param>
    private void PlayNode(string id)
    {
        if (SetCurrentNode(id)) PlayNode(_currentNode);
        else return;
    }
    
    /// <summary>
    /// 播放所给定的对话结点
    /// </summary>
    private void PlayNode(DialogueNode node)
    {
        if (node == null) return;

        string targetName = node.speaker; // 取当前结点的 说话者名字
        string targetContent = node.content; // 取当前结点的 内容

        // 1. 更新对话层UI
        uiManager.StartUpdateDialogueBox(targetName,targetContent);

        // 2. 更新交互层UI
        uiManager.ShowOptions(node.options);

        // 3. 更新舞台
        stageManager.UpdateStage(node);

        // --- 更新结点旅程 ---
        UpdateNodeJourney(node.id);
   
    }

    #endregion （2）方法

    #endregion 2. 播放结点

    #region 3. 自动播放

    #region （1）配置区域
    [Header("System-AutoPlay")]
    [Tooltip("自动播放时间间隔(s)")]
    [SerializeField] private float autoPlayInterval = 3f; // 从对话框文字全部显示完毕时开始到自动播放下一个对话为止的时间间隔
    #endregion （2）配置区域

    #region （2）运行时变量

    private bool _isAutoPlaying = false; // 是否处于自动播放状态
    private Coroutine _autoPlayCoroutine = null; // 用于记录所启动的自动播放下一个结点的协程

    #endregion （2）运行时变量

    #region （3）方法

    /// <summary>
    /// 开关自动播放模式
    /// </summary>
    public void ToggleAutoMode()
    {
        _isAutoPlaying = !_isAutoPlaying;
    }

    /// <summary>
    /// 决定是否启动自动播放下一个结点协程
    /// </summary>
    private void DetermineToAutoPlay()
    {
        // 如果当前处于自动播放状态 并且 目前没有启动自动播放协程 并且 没有在打字
        // 则应当启动自动播放下一个结点协程
        if (_currentState == GameState.Normal && _isAutoPlaying && _autoPlayCoroutine == null)
        {
            // 启动自动播放下一个结点协程
            _autoPlayCoroutine = StartCoroutine(AutoPlayNextNode(() => {
                _autoPlayCoroutine = null;
            }));
        }
        // 如果自动播放下一个结点的协程已经被启动
        // 但是状态演变为不应当启动，则要停止协程
        else if (_autoPlayCoroutine != null)
        {
            if (_currentState != GameState.Normal || !_isAutoPlaying)
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
    /// <param name="act">外部实现的用于置空_autoPlayCoroutine变量的函数</param>
    /// <returns></returns>
    private IEnumerator AutoPlayNextNode(UnityAction act)
    {
        // 1. 第一步，等待指定时间
        yield return new WaitForSeconds(autoPlayInterval);

        // 2. 第二步，播放下一个结点
        PlayNextNode();

        // 3. 第三步，置空_autoPlayCoroutine变量
        act.Invoke();
    }

    #endregion （3）方法

    #endregion 3. 自动播放

    #region 4. 存档数据/还原读档数据

    #region 运行时数据
    // 结点旅程
    NodeJourney _nodeJourney = new NodeJourney();

    // 选项旅程
    ChoiceJourney _choiceJourney = new ChoiceJourney();

    // 存档截图
    Texture2D _screenshot = null;
    #endregion 运行时数据

    #region 方法

    /// <summary>
    /// 玩家点击主游戏场景中的“存档”按钮时触发
    /// 在游戏界面点击“存档”按钮、开启存档界面之前，先截取游戏画面，再开启存档界面
    /// </summary>
    /// <param name="act">用于承载开启存档界面的函数</param>
    private void HandleOpenSavePanelButtonClicked()
    {
        StartCoroutine(ScreenshotAndOpenSavePanelCoroutine());
    }

    private IEnumerator ScreenshotAndOpenSavePanelCoroutine()
    {
        // 等待帧末尾渲染完毕后截图
        yield return new WaitForEndOfFrame();

        _screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        // 截图完成，打开存档界面
        GlobalUIManager.Instance.ShowSaveLoadPanel(true);
    }

    public async void Save(string saveIdNum)
    {
        // 准备存档头信息
        SaveHeader saveHeader = new SaveHeader
        {
            saveIdNum = saveIdNum,
            screenshotName = $"save{saveIdNum}",
            chapterName = _currentChapter.chapterName,
            saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            saveTimeStamp = DateTime.Now.Ticks
        };

        // 命令存档管理器更新并写入存档头、写入截图
        await SaveManager.WriteSaveHeadersAsync(saveHeader, _screenshot);

        // 准备存档信息
        // 获取舞台图片信息
        Dictionary<string, string> stageImagesNames = stageManager.GetStageImagesNames();
        SaveEntry saveEntry = new SaveEntry
        {
            // ---游戏现场---
            // 章节进度
            chapterId = _currentChapter.chapterId,
            chapterName = _currentChapter.chapterName,
            nodeId = _currentNode.id,
            nodeJourney = _nodeJourney,
            choiceJourney = _choiceJourney,

            // 历史记录
            historyRecords = PrepareHistoryRecords(),

            // 视觉快照
            bgName = stageImagesNames["bg"],
            charLeftName = stageImagesNames["charLeft"],
            charCenterName = stageImagesNames["charCenter"],
            charRightName = stageImagesNames["charRight"],
            cgName = stageImagesNames["cg"],

            // 音乐
            // TODO : 从音效管理器获取背景音乐信息

            // --- 玩家数据 ---
            // TODO ：男主好感度

            // TODO : 背包
        };

        // 命令存档管理器写入存档
        await SaveManager.SaveAsync(saveIdNum, saveEntry);

        // 写入操作全部完成，触发事件
        EventCenter.OnSaveOperationFinished?.Invoke();
    }

    public void Load(string saveIdNum)
    {
        StartCoroutine(LoadCoroutine(saveIdNum));
    }

    private IEnumerator LoadCoroutine(string saveIdNum)
    {
        uiManager.ShowLoadingMusk(true);
        ChangeGameState(GameState.Paused);

        SaveEntry saveEntry = null;

        Task<SaveEntry> task = SaveManager.LoadAsync(saveIdNum);
        yield return new WaitUntil(() => task.IsCompleted);
        saveEntry = task.Result;

        if (saveEntry == null)
        {
            Debug.LogError($"存档{saveIdNum}获取失败");
            uiManager.ShowLoadingMusk(false);
            ChangeGameState(GameState.Normal);
            yield return null;
        }

        if(saveEntry.nodeId == null)
        {
            Debug.LogError($"存档{saveIdNum}严重错误，未记录结点");
            uiManager.ShowLoadingMusk(false);
            ChangeGameState(GameState.Normal);
            yield break;
        }

        InitCurrentChapter(saveEntry.chapterId, out _currentChapter, out _dialoguesDic);

        if (SetCurrentNode(saveEntry.nodeId))
        {
            // --- 旅程和记录 ---
            // 还原结点旅程和选择旅程
            _nodeJourney = saveEntry.nodeJourney;
            _choiceJourney = saveEntry.choiceJourney;

            // 还原历史记录
            historyManager.SetHistoryRecords(saveEntry.historyRecords);

            // --- 舞台 ---
            stageManager.InitStage();

            List<Coroutine> list = new List<Coroutine>();

            if(saveEntry.bgName != null) list.Add(stageManager.UpdateImage(E_StageImageType.Background, saveEntry.bgName));
            if(saveEntry.charLeftName != null) list.Add(stageManager.UpdateImage(E_StageImageType.CharLeft, saveEntry.charLeftName));
            if(saveEntry.charCenterName != null) list.Add(stageManager.UpdateImage(E_StageImageType.CharCenter, saveEntry.charCenterName));
            if(saveEntry.charRightName != null) list.Add(stageManager.UpdateImage(E_StageImageType.CharRight, saveEntry.charRightName));
            if(saveEntry.cgName != null) list.Add(stageManager.UpdateImage(E_StageImageType.CG, saveEntry.cgName));

            // --- 对话框 ---
            uiManager.SetDialogueBox(_currentNode.speaker, _currentNode.content);

            // --- 选项 ---
            if(_currentNode.options != null)
            {
                uiManager.ShowOptionsDirectly(_currentNode.options);
            }

            // TODO : 音乐、背包、好感度......

            

            // 等待所有图片更新协程完成
            foreach(Coroutine coroutine in list)
            {
                yield return coroutine;
            }

            // 至少等待0.5秒
            yield return new WaitForSeconds(0.5f);

            Debug.Log("读档完成");
            EventCenter.OnLoadOperationFinished?.Invoke();

            uiManager.ShowLoadingMusk(false);
            if (_currentNode.options != null)
            {
                ChangeGameState(GameState.Interacting);
            }
            else
            {
                ChangeGameState(GameState.Normal);
            }

        }
        else
        {
            Debug.LogError("存档记录的结点在章节中不存在");
            uiManager.ShowLoadingMusk(false);
            ChangeGameState(GameState.Normal);
            yield return null;
        }
        
    }

    /// <summary>
    /// 进入新结点时，更新结点旅程
    /// </summary>
    void UpdateNodeJourney(string nodeId)
    {
        _nodeJourney.list.Add(nodeId);
    }

    /// <summary>
    /// 读取存档的结点旅程覆盖目前游戏结点旅程
    /// </summary>
    void UpdateNodeJourney(NodeJourney loadedNodeJourney)
    {
        _nodeJourney = loadedNodeJourney;
    }

    /// <summary>
    /// 玩家选择某一选项后更新选择旅程
    /// </summary>
    void UpdateChoiceJourney(string nodeId, string choiceId)
    {
        if (_choiceJourney.dic.ContainsKey(nodeId))
        {
            Debug.LogError($"游戏流程中结点{nodeId}出现多次选择！");
            return;
        }
        else
        {
            _choiceJourney.dic.Add(nodeId, choiceId);
        }
    }

    /// <summary>
    /// 读取存档时的选择旅程覆盖目前游戏选择旅程
    /// </summary>
    void UpdateChoiceJourney(ChoiceJourney loadedChoiceJourney)
    {
        _choiceJourney = loadedChoiceJourney;
    }

    /// <summary>
    /// 从historyManager获取历史记录目前游戏运行时的历史记录信息
    /// </summary>
    HistoryRecords PrepareHistoryRecords()
    {
        return historyManager.GetHistoryRecords();
    }

    /// <summary>
    /// 读取存档时的历史记录信息覆盖目前游戏运行时的历史记录信息
    /// </summary>
    void LoadHistoryRecords(HistoryRecords loadedHistoryRecords)
    {
        historyManager.SetHistoryRecords(loadedHistoryRecords);
    }
    
    #endregion 方法

    #endregion 4. 打包存档数据/还原读档数据

    #endregion 三、职责

    #region 四、生命周期

    private void Start()
    {
        ChangeGameState(GameState.Init);
        // --- 加载剧本 ---
        InitCurrentChapter("debug_02", out _currentChapter, out _dialoguesDic);

        // --- 检查引用 ---
        InitReference();

        // --- 监听事件 ---
        InitEvent();

        ChangeGameState(GameState.Normal);
        // 开始时，播放第一句
        PlayNode("line_01");

        print(Application.persistentDataPath);
    }

    private void Update()
    {
        DetermineToAutoPlay();
    }

    private void OnDestroy()
    {
        ClearEvent();
    }

    #endregion 四、生命周期

    #region 五、生命周期辅助函数

    private void InitReference()
    {
        // --- 确认舞台管理器工作 ---
        if (stageManager == null)
        {
            Debug.LogError("stageManager 为空");
        }
        else if (stageManager.enabled == false)
        {
            Debug.LogError("stageManager 脚本未激活");
        }
        else if (stageManager.gameObject.activeSelf == false)
        {
            Debug.LogError("stageManager 游戏对象未激活");
        }

        // --- 确认UI管理器工作
        if (uiManager == null)
        {
            Debug.LogError("uiManager 为空");
        }
        else if (uiManager.enabled == false)
        {
            Debug.LogError("uiManager 脚本未激活");
        }
        else if (uiManager.gameObject.activeSelf == false)
        {
            Debug.LogError("uiManager 游戏对象未激活");
        }

        // --- 确认历史记录管理器工作
        if (historyManager == null)
        {
            Debug.LogError("historyManager 为空");
        }
        else if (historyManager.enabled == false)
        {
            Debug.LogError("historyManager 脚本未激活");
        }
        else if (historyManager.gameObject.activeSelf == false)
        {
            Debug.LogError("historyManager 游戏对象未激活");
        }
    }

    private void InitEvent()
    {
        // 基于交互事件的开始和结束维护状态机
        EventCenter.OnInteractionStarted += ChangeGameStateToInteracting;
        EventCenter.OnInteractionFinished += ChangeGameStateToPrevious;

        // 点击对话框时决定是播放下一个还是完成打字
        uiManager.OnContinueButtonClicked += DetermineToPlayNextNode;

        // 监视打字机状态
        uiManager.OnTypingStarted += ChangeGameStateToTyping;
        uiManager.OnTypingFinished += ChangeGameStateToNormal;
        EventCenter.OnGameStateChanged += () =>
        {
            if (_currentState != GameState.Normal && _currentState != GameState.Typing) uiManager.PauseUpdateDialogueBox(true);
            else uiManager.PauseUpdateDialogueBox(false);
        };

        // 选项交互结束后播放所选选项指向的结点
        uiManager.OnOptionsDestroyed += PlayNodeAfterChoice;

        // 基于历史模式的开关维护游戏状态机
        uiManager.OnHistoryButtonClicked += ChangeGameStateToHistory;
        uiManager.OnHistoryCloseButtonClicked += ChangeGameStateToPrevious;

        // 点击自动按钮时切换自动播放状态
        uiManager.OnAutoPlayButtonClicked += ToggleAutoMode;

        // 点击游戏界面上的存档按钮时，先截屏，再打开存档界面
        uiManager.OnOpenSavePanelButtonClicked += HandleOpenSavePanelButtonClicked;

        // 点击存档界面上的存档按钮时，根据存档编号，保存运行时数据
        GlobalUIManager.Instance.OnSaveButtonClickedHandlerPipline += Save;

        // 点击读档界面上的读档按钮时，根据存档编号，进行读档
        GlobalUIManager.Instance.OnLoadButtonClickedHandlerPipline += Load;

        // 读档结束时，先进入正常游戏状态
        EventCenter.OnLoadOperationFinished += ChangeGameStateToNormal;

        // 状态变化时调试信息
        EventCenter.OnGameStateChanged += LogGameStateChange;
    }

    private void LogGameStateChange()
    {
        print($"Current Game State: from {_previousState} to {_currentState}");
    }

    private void ClearEvent()
    {
        EventCenter.OnGameStateChanged -= LogGameStateChange;
        EventCenter.OnInteractionStarted -= ChangeGameStateToInteracting;
        EventCenter.OnInteractionFinished -= ChangeGameStateToPrevious;
        uiManager.OnContinueButtonClicked -= DetermineToPlayNextNode;
        uiManager.OnTypingStarted -= ChangeGameStateToTyping;
        uiManager.OnTypingFinished -= ChangeGameStateToNormal;
        uiManager.OnOptionsDestroyed -= PlayNodeAfterChoice;
        uiManager.OnHistoryButtonClicked -= ChangeGameStateToHistory;
        uiManager.OnHistoryCloseButtonClicked -= ChangeGameStateToNormal;
        uiManager.OnAutoPlayButtonClicked -= ToggleAutoMode;
        uiManager.OnOpenSavePanelButtonClicked -= HandleOpenSavePanelButtonClicked;
        GlobalUIManager.Instance.OnSaveButtonClickedHandlerPipline -= Save;
        GlobalUIManager.Instance.OnLoadButtonClickedHandlerPipline -= Load;
        EventCenter.OnLoadOperationFinished -= ChangeGameStateToNormal;
    }

    #endregion 五、生命周期辅助函数
}
