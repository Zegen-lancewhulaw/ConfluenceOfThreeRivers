using AVG;
using AVG.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    #region 配置区域
    [Header("Cooperative Managers")]
    public StageManager stageManager;
    public UIManager uiManager;
    public HistoryManager historyManager;

    [Header("System-AutoPlay")]
    [Tooltip("自动播放时间间隔(s)")]
    [SerializeField] private float autoPlayInterval = 3f; // 从对话框文字全部显示完毕时开始到自动播放下一个对话为止的时间间隔
    #endregion

    #region 公共调用接口
    /// <summary>
    /// 开关自动播放模式
    /// </summary>
    public void ToggleAutoMode()
    {
        _isAutoPlay = !_isAutoPlay;
    }

    /// <summary>
    /// 是否处于自动播放状态
    /// </summary>
    public bool IsAutoPlay()
    {
        return _isAutoPlay;
    }

    /// <summary>
    /// 当玩家点击对话框（上的Button）的时候触发
    /// </summary>
    public void NextDialogue()
    {
        // 如果正在打字，则停止打字机
        // 如果不在打字，则播放下一个结点
        // 是否打字的状态由uiManager控制
        if (uiManager.IsTyping())
        {
            uiManager.FinishUpdateDialogueBox(_currentNode.content);
        }
        else
        {
            PlayNode(_currentNode.nextId);
        }
    }

    #endregion

    #region 运行时状态
    // --- 1. 章节 和 结点 ---
    private ChapterModel _currentChapter; // 当前正在播放的章节
    private Dictionary<string, DialogueNode> _dialoguesDic; // 当前章节中的对话结点的字典，以结点id为键
    private DialogueNode _currentNode; // 当前正在播放的对话结点

    // --- 2. 自动播放 ---
    private bool _isAutoPlay = false; // 是否处于自动播放状态
    private Coroutine _autoPlayCoroutine = null; // 用于记录所启动的自动播放下一个结点的协程

    #endregion

    #region 生命周期

    // ---初始化---
    private void Start()
    {
        // --- 1. 加载剧本 ---
        _currentChapter = ScriptsManager.LoadJson<ChapterModel>("test_chapter2.json", Application.streamingAssetsPath);
        _dialoguesDic = ScriptsManager.GetDialoguesDic(_currentChapter);

        // --- 2. 确认舞台管理器工作 ---
        if(stageManager == null)
        {
            Debug.LogError("stageManager 为空");
        }
        else if(stageManager.enabled == false)
        {
            Debug.LogError("stageManager 脚本未激活");
        }
        else if(stageManager.gameObject.activeSelf == false)
        {
            Debug.LogError("stageManager 游戏对象未激活");
        }

        // --- 3. 确认UI管理器工作
        if(uiManager == null)
        {
            Debug.LogError("uiManager 为空");
        }
        else if(uiManager.enabled == false)
        {
            Debug.LogError("uiManager 脚本未激活");
        }
        else if(uiManager.gameObject.activeSelf == false)
        {
            Debug.LogError("uiManager 游戏对象未激活");
        }

        // --- 4. 确认历史记录管理器工作
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

        // --- 5. 监听事件 ---
        uiManager.autoPlayButtonClicked += ToggleAutoMode;
        uiManager.continueButtonClicked += NextDialogue;
        uiManager.targetIdChosen += PlayNode;

        // 开始时，播放第一句
        PlayNode("line_01");
    }

    private void Update()
    {
        DetermineToAutoPlay();
    }
    #endregion

    #region 私有逻辑方法  

    /// <summary>
    /// 播放给定id编号的对话结点
    /// </summary>
    /// <param name="id">对话节点的id编号</param>
    void PlayNode(string id)
    {
        // 如果要播放的结点id存在
        if (_dialoguesDic.ContainsKey(id))
        {
            _currentNode = _dialoguesDic[id]; // 将要播放的结点设置为 当前结点
            string targetName = _currentNode.speaker; // 取当前结点的 说话者名字
            string targetStr = _currentNode.content; // 取当前结点的 内容

            // 1. 更新对话框
            uiManager.UpdateDialogueBox(targetName, targetStr);

            // 2. 检测是否有选项
            if(_currentNode.options != null && _currentNode.options.Count > 0)
            {
                uiManager.ShowOptions(_currentNode.options);
            }

            // 3. 检测是否有背景图片变化
            if(_currentNode.background != null)
            {
                stageManager.UpdateImage(E_StageImageType.Background, _currentNode.background);
            }

            // 4. 检测是否有立绘变化
            if(_currentNode.charLeft != null)
            {
                stageManager.UpdateImage(E_StageImageType.CharLeft, _currentNode.charLeft);
            }

            if(_currentNode.charCenter != null)
            {
                stageManager.UpdateImage(E_StageImageType.CharCenter, _currentNode.charCenter);
            }

            if(_currentNode.charRight != null)
            {
                stageManager.UpdateImage(E_StageImageType.CharRight, _currentNode.charRight);
            }

            // 5. 检测是否有CG变化
            if(_currentNode.cgImage != null)
            {
                stageManager.UpdateImage(E_StageImageType.CG, _currentNode.cgImage);
            }
        }
    }

    /// <summary>
    /// 决定是否启动自动播放下一个结点协程
    /// </summary>
    private void DetermineToAutoPlay()
    {
        // 如果当前处于自动播放状态 并且 目前没有启动自动播放协程 并且 不处于一系列不应当启动协程的状态
        // 则应当启动自动播放下一个结点协程
        if (_autoPlayCoroutine == null && _isAutoPlay && !uiManager.IsTyping() && !uiManager.IsHidden() && !uiManager.IsInteracting() && !uiManager.IsOnHistory())
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
            if (!_isAutoPlay || uiManager.IsHidden() || uiManager.IsInteracting() || uiManager.IsOnHistory())
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
    #endregion
}
