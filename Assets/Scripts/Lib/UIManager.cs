using AVG.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AVG
{
    public class UIManager : MonoBehaviour
    {
        #region 引用区域
        [Header("Cooperative Managers")]
        public StageManager stageManager;
        public Controller controller;
        public HistoryManager historyManager;

        [Header("系统层")]
        [SerializeField] private GameObject systemLayer;

        [Header("System-Hide")]
        [Tooltip("开启隐藏的小按钮")]
        [SerializeField] private Button hideButton;
        [Tooltip("取消隐藏的全屏按钮")]
        [SerializeField] private Button hideCancelButton;
        [Tooltip("隐藏的UI层")]
        [SerializeField] private GameObject[] uiLayersToHide;

        [Header("System-History")]
        [Tooltip("历史记录按钮")]
        [SerializeField] private Button historyButton;
        [Tooltip("关闭历史记录按钮")]
        [SerializeField] private Button historyCloseButton;

        [Header("System-AutoPlay")]
        [Tooltip("自动播放按钮")]
        [SerializeField] private Button autoPlayButton;
        [Tooltip("自动播放按钮文本")]
        [SerializeField] private Text autoPlayButtonText;

        [Header("System-Skip")]
        [Tooltip("跳过按钮")]
        [SerializeField] private Button skipButton;
        [Tooltip("是否跳过弹窗预制体")]
        [SerializeField] private GameObject skipInfoPopupPrefab;


        [Header("对话层")]
        [SerializeField] private GameObject dialogLayer;
        [Tooltip("对话框上点击继续按钮")]
        [SerializeField] private Button continueButton;
        [Tooltip("进入新对话结点时的音效")]
        [SerializeField] private AudioSource newDialogueAudio;
        [Tooltip("对话框说话者名字文本")]
        [SerializeField] private Text speakerName;
        [Tooltip("对话框文字内容文本")]
        [SerializeField] private Text dialogueContent;
        [Tooltip("打字机时间间隔")]
        [SerializeField] private float typingInterval = 0.1f;

        [Header("交互层")]
        [Tooltip("交互层容器")]
        [SerializeField] private Transform interactionLayer;
        [Tooltip("交互层遮罩")]
        [SerializeField] private Image interactionMask;
        [Tooltip("信息弹窗容器")]
        [SerializeField] private Transform infoContainer;
        [Tooltip("选项组容器")]
        [SerializeField] private Transform optionContainer;

        [Header("Options")]
        [Tooltip("选项按钮预制体")]
        [SerializeField] private GameObject optionPrefab;

        #endregion

        #region 事件
        // 自动播放按钮被点击
        public UnityAction autoPlayButtonClicked;
        // 对话框继续按钮被点击
        public UnityAction continueButtonClicked;
        // 对话框内容更新
        public UnityAction<string, string> dialogueBoxUpdate;
        // 选项按钮被点击
        public UnityAction<string, string> optionButtonClicked;
        public UnityAction<string> targetIdChosen;
        // 历史记录按钮被点击
        public UnityAction<bool> historyButtonClicked;
        #endregion

        #region 公共调用接口
        /// <summary>
        /// 初始化UI
        /// </summary>
        public void InitUI()
        {
            // --- 系统层 ---
            systemLayer?.SetActive(true);

            // 1. 隐藏按钮
            hideButton?.onClick.AddListener(ToggleHideMode);
            hideCancelButton?.onClick.AddListener(ToggleHideMode);
            hideCancelButton.gameObject.SetActive(false); // 初始时关闭取消隐藏的全屏按钮

            // 2. 历史记录按钮
            historyButton?.onClick.AddListener(ToggleHistoryMode);
            historyCloseButton?.onClick.AddListener(ToggleHistoryMode);

            // 3. 自动播放按钮
            autoPlayButton?.onClick.AddListener(() => 
            {
                autoPlayButtonText.text = (autoPlayButtonText.text == "自动") ? "自动中..." : "自动";
                autoPlayButtonClicked?.Invoke();
            });
            autoPlayButtonText.text = "自动";

            // 4. 跳过按钮
            skipButton?.onClick.AddListener(Skip);

            // --- 对话层 ---
            dialogLayer?.SetActive(true);

            // 1. 对话框点击继续按钮
            continueButton?.onClick.AddListener(continueButtonClicked);

            // --- 交互层 ---
            infoContainer.gameObject.SetActive(false);
            optionContainer.gameObject.SetActive(false);
            interactionLayer.gameObject.SetActive(false);
            interactionMask.gameObject.SetActive(false);

            // --- 监听事件 ---
            if(stageManager != null)
            {
                stageManager.CGUpdated += ToggleHideMode;
            }

        }

        /// <summary>
        /// 开关隐藏模式
        /// </summary>
        public void ToggleHideMode()
        {
            _isHidden = !_isHidden;

            // 处理需要或者已经被隐藏的UI层
            foreach (var layer in uiLayersToHide)
            {
                layer.SetActive(!_isHidden);
            }

            // 处理取消隐藏的全屏按钮
            hideCancelButton.gameObject.SetActive(_isHidden);
        }

        /// <summary>
        /// 开关历史记录模式
        /// </summary>
        public void ToggleHistoryMode()
        {
            _isOnHistory = !_isOnHistory;

            historyButtonClicked?.Invoke(_isOnHistory);
        }

        /// <summary>
        /// 当玩家点击“跳过”按钮时触发
        /// </summary>
        public void Skip()
        {
            // 打开交互层、交互层遮罩、信息弹窗容器，维护当前状态
            interactionLayer.gameObject.SetActive(true);
            interactionMask.gameObject.SetActive(true);
            _isInteracting = true;
            infoContainer.gameObject.SetActive(true);
            // 创造信息弹窗
            GameObject infoPopupObj = Instantiate(skipInfoPopupPrefab, infoContainer);
            // 设置信息弹窗上的信息文字
            Information_YorN infoPopupScript = infoPopupObj.GetComponent<Information_YorN>();
            infoPopupScript.SetInformationText("确认要跳过本章剧情？");
            infoPopupScript.SetYesText("确认");
            infoPopupScript.SetNoText("取消");
            // 设置信息弹窗关闭效果
            infoPopupScript.SetCloseEffect(() =>
            {
                /*如果先失活信息弹窗容器，则作为容器子对象的信息弹窗随之失活，可能不会被销毁；
                 而如果先销毁信息弹窗，则作为信息弹窗关闭效果的将容器失活等操作可能不会执行。
                所以先开启失活容器的协程，之后执行信息弹窗的销毁，失活容器等操作最后在协程中完成*/
                StartCoroutine(CloseInfoPopup());
                Destroy(infoPopupObj);
            });
        }

        /// <summary>
        /// 对话框是否正在打字
        /// </summary>
        public bool IsTyping()
        {
            return _isTyping;
        }

        /// <summary>
        /// 是否正在交互状态
        /// </summary>
        public bool IsInteracting()
        {
            return _isInteracting;
        }

        /// <summary>
        /// 是否正在查看历史记录状态
        /// </summary>
        public bool IsOnHistory()
        {
            return _isOnHistory;
        }

        /// <summary>
        /// 是否正在隐藏状态
        /// </summary>
        public bool IsHidden()
        {
            return _isHidden;
        }

        /// <summary>
        /// 更新对话框的 文字内容 和 说话者名字
        /// </summary>
        public void UpdateDialogueBox(string name, string content)
        {
            // --- 触发音效 ---
            newDialogueAudio.Play();
            // --- 更新说话者名字 ---
            speakerName.text = name;
            // --- 打字 ---
            dialogueContent.text = "";
            _typeTextCoroutine = StartCoroutine(TypeText(content));
            // --- 触发回调函数 ---
            dialogueBoxUpdate?.Invoke(name, content);
        }

        /// <summary>
        /// 立即完成对话框的更新
        /// </summary>
        public void FinishUpdateDialogueBox(string content)
        {
            // 停止打字机协程
            StopCoroutine(_typeTextCoroutine);
            // 直接显示完整文字
            dialogueContent.text = content;
            // 维护状态
            _isTyping = false;
            tagList.Clear();
            tagStack.Clear();
        }

        /// <summary>
        /// 根据所给的选项结点信息生成交互选项
        /// </summary>
        /// <param name="options">选项结点信息</param>
        public void ShowOptions(List<OptionNode> options)
        {
            StartCoroutine(ShowOptionsAfterTyping(options));
        }


        #endregion

        #region 运行时状态
        private bool _isInteracting = false; // 是否处于正在与玩家交互的状态

        private bool _isHidden = false; // 是否处于隐藏状态

        private bool _isOnHistory = false; // 是否处于查看历史记录状态

        private bool _isTyping = false; // 是否正在打字
        private Coroutine _typeTextCoroutine = null; // 用于记录已经开启的打字机协程
        private Stack<string> tagStack = new Stack<string>(); // 记录富文本标签名的栈，用于自动追加闭合标签
        private List<string> tagList = new List<string>(); // 记录富文本标签名的列表，用于自动追加开标签
        #endregion

        #region 生命周期
        private void Start()
        {
            // --- 初始化 ---
            InitUI();
        }
        #endregion

        #region 私有逻辑方法
        /// <summary>
        /// 用于失活信息弹窗容器、交互层、交互层遮罩的协程函数
        /// </summary>
        IEnumerator CloseInfoPopup()
        {
            yield return null;
            infoContainer.gameObject.SetActive(false);
            interactionMask.gameObject .SetActive(false);
            interactionLayer.gameObject .SetActive(false);
            _isInteracting = false;
        }

        /// <summary>
        /// 一个打字机协程，用于对话框等文字内容的打字机效果展示
        /// </summary>
        /// <param name="targrtStr">要打印的最终文字</param>
        IEnumerator TypeText(string targetStr)
        {
            #region 调试信息
            print("打字机协程已启动");
            #endregion
            // 维护状态
            _isTyping = true;

            // 遍历目标字符串的每个字符，逐个添加到对话框当前文本的末尾，遇到标签则特殊处理
            int length = targetStr.Length;
            for (int i = 0; i < length; i++)
            {
                // 读取新字符
                char c = targetStr[i];

                // --- 检测到富文本标签特征 ---
                if (c == '<')
                {
                    // 如果确实是标签，处理标签
                    int closeIndex = targetStr.IndexOf('>', i);
                    if (closeIndex != -1)
                    {
                        // 获取整个标签
                        string fullTag = targetStr.Substring(i, closeIndex - i + 1);

                        // 是否是闭标签
                        bool isClosingTag = fullTag.StartsWith("</");

                        // 如果是闭标签，弹栈，出列表
                        if (isClosingTag)
                        {
                            tagStack.Pop();
                            tagList.RemoveAt(tagList.Count - 1);
                        }

                        // 如果是开标签，压栈，入列表
                        else
                        {
                            // 开标签可以不经处理直接加入tagList列表
                            tagList.Add(fullTag);

                            // 开标签需要处理成对应的闭合标签后才能压入tagStack栈
                            int equalIndex = fullTag.IndexOf('=');
                            if (equalIndex != -1)
                            {
                                fullTag = fullTag.Remove(equalIndex, fullTag.Length - equalIndex - 1);
                            }
                            fullTag = fullTag.Insert(1, "/");
                            tagStack.Push(fullTag);
                        }

                        // 标签处理完毕，指针跳到标签末尾，继续遍历字符串
                        i = closeIndex;
                        continue;
                    }
                }

                // --- 正常字符处理方式 ---
                // 0. 准备将该单个字符追加到当前文字末尾
                string additionStr = "";

                // 1. 添加有效的开标签
                foreach (string tag in tagList)
                {
                    additionStr += tag;
                }

                // 2. 添加当前单字
                additionStr += c;

                // 3. 添加有效的闭合标签
                foreach (string tag in tagStack)
                {
                    additionStr += tag;
                }

                // 4. 追加最终单字
                dialogueContent.text += additionStr;

                yield return new WaitForSeconds(typingInterval);
            }

            // 目标字符串遍历完毕，保险起见，清空标签栈和列表
            tagStack.Clear();
            tagList.Clear();

            // 维护状态
            _isTyping = false;
        }

        /// <summary>
        /// 用于生成对话结点中的 选项组 的协程函数
        /// </summary>
        IEnumerator ShowOptionsAfterTyping(List<OptionNode> options)
        {
            // 直到打字机打印全部文字后再显示选项
            yield return new WaitUntil(() => !_isTyping);
            
            // 启动交互层
            interactionLayer.gameObject.SetActive(true);
            interactionMask.gameObject.SetActive(true);
            _isInteracting = true;

            // 打开选项组容器
            optionContainer.gameObject.SetActive(true);

            // 准备选项点击事件上的回调函数
            optionButtonClicked += OnOptionClicked;

            // 生成选项
            foreach (var opt in options)
            {
                // 1. 生成Option预制体
                GameObject optionObj = Instantiate(optionPrefab, optionContainer);

                // 2. 闭包捕获变量
                string textOfOpt = opt.text;
                string targetIdOfOpt = opt.targetId;

                // 3. 设置Option文字
                optionObj.GetComponentInChildren<Text>().text = textOfOpt;

                // 4. 监听点击选项事件
                Button btn = optionObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    optionButtonClicked?.Invoke(textOfOpt, targetIdOfOpt);
                });
            }
        }

        /// <summary>
        /// 用于绑定对话选项的回调函数
        /// </summary>
        /// <param name="targetId">所选选项的下一个对话结点的Id</param>
        void OnOptionClicked(string text, string targetId)
        {
            // --- 清理现场 ---
            // 销毁所有选项按钮
            foreach (Transform childOption in optionContainer)
            {
                Destroy(childOption.gameObject);
            }

            // 关闭选项组容器
            optionContainer.gameObject.SetActive(false);

            // 关闭交互层
            interactionMask.gameObject.SetActive(false);
            interactionLayer.gameObject.SetActive(false);
            _isInteracting = false;

            // 执行目标结点选定的回调函数
            targetIdChosen?.Invoke(targetId);
        }
        #endregion
    }
}
