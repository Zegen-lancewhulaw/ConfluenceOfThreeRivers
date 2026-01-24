using AVG.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AVG
{
    public class UIManager : MonoBehaviour
    {
        #region UI总成

        #region 公共调用接口
        public void InitUI()
        {
            // --- 系统层 ---
            InitSystemLayer();

            // --- 对话层---
            InitDialogueLayer();

            // --- 交互层 ---
            InitInteractionLayer();
        }

        #endregion 公共调用接口

        #region 生命周期
        private void Awake()
        {
            InitUI();
        }
        #endregion 生命周期

        #endregion UI总成

        #region UI层次结构

        #region UI层次2：系统层

        #region 系统层总成

        #region 引用
        [Header("系统层")]
        [SerializeField] private GameObject systemLayer;
        #endregion 引用

        #region 初始化
        public void InitSystemLayer()
        {
            if (systemLayer == null)
            {
                Debug.LogWarning("系统层为空");
            }
            InitHide();
            InitHistory();
            InitAuto();
            InitSkip();
        }
        #endregion 初始化

        #endregion 系统层总成

        #region UI模块2.1 隐藏

        #region 引用
        [Header("System-Hide")]
        [Tooltip("开启隐藏的小按钮")]
        [SerializeField] private Button hideButton;
        [Tooltip("取消隐藏的全屏按钮")]
        [SerializeField] private Button hideCancelButton; // 此为交互层下属的一个子对象，激活时视为交互事件
        [Tooltip("隐藏的UI层")]
        [SerializeField] private GameObject[] uiLayersToHide;
        #endregion 引用

        #region 方法

        /// <summary>
        /// 初始化隐藏按钮
        /// </summary>
        public void InitHide()
        {
            if(hideButton == null)
            {
                Debug.LogWarning("隐藏按钮为空");
            }
            else
            {
                hideButton.onClick.AddListener(HideLayers);
            }

            if(hideCancelButton == null)
            {
                Debug.LogWarning("取消隐藏按钮为空");
            }
            else
            {
                hideCancelButton.onClick.AddListener(() => CancelHideLayers());
                hideCancelButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 隐藏需要隐藏的UI
        /// </summary>
        private void HideLayers()
        {
            // 处理需要或者已经被隐藏的UI层
            foreach (var layer in uiLayersToHide)
            {
                layer.SetActive(false);
            }

            // 激活取消隐藏的全屏按钮
            interactionLayer.gameObject.SetActive(true);
            hideCancelButton.gameObject.SetActive(true);

            // 触发交互事件
            EventCenter.interactionStarted?.Invoke();
        }

        /// <summary>
        /// 显示需要隐藏的UI
        /// </summary>
        private void CancelHideLayers()
        {
            // 处理需要或者已经被隐藏的UI层
            foreach (var layer in uiLayersToHide)
            {
                layer.SetActive(true);
            }

            // 失活取消隐藏的全屏按钮
            hideCancelButton.gameObject.SetActive(false);
            interactionLayer.gameObject.SetActive(false);

            // 触发交互结束事件结束
            EventCenter.interactionFinished?.Invoke();
        }

        #endregion 方法

        #endregion UI模块2.1 隐藏

        #region UI模块2.2 历史

        #region 引用

        [Header("System-History")]
        [Tooltip("整个历史记录层")]
        [SerializeField] private GameObject historyLayer;
        [Tooltip("历史记录滚动视窗上的ScrollRect")]
        [SerializeField] private ScrollRect historyScrollRect;
        [Tooltip("历史记录按钮")]
        [SerializeField] private Button historyButton;
        [Tooltip("关闭历史记录按钮")]
        [SerializeField] private Button historyCloseButton;

        #endregion 引用

        #region 事件
        public UnityAction onHistoryButtonClicked;
        public UnityAction onHistoryCloseButtonClicked;
        #endregion 事件

        #region 方法

        /// <summary>
        /// 初始化历史记录UI
        /// </summary>
        public void InitHistory()
        {
            if (historyLayer == null)
            {
                Debug.LogWarning("历史记录层为空");
            }
            else
            {
                historyLayer.SetActive(false);
            }

            if (historyScrollRect == null)
            {
                Debug.LogWarning("historyScrollRect为空");

            }

            if (historyButton == null)
            {
                Debug.LogWarning("历史按钮为空");
            }
            else
            {
                historyButton.onClick.AddListener(() => {
                    OpenHistory();
                    onHistoryButtonClicked?.Invoke();
                });
            }

            if (historyCloseButton == null)
            {
                Debug.LogWarning("关闭历史按钮为空");
            }
            else
            {
                historyCloseButton.onClick.AddListener(() =>
                {
                    CloseHistory();
                    onHistoryCloseButtonClicked?.Invoke();
                });
            }
        }

        /// <summary>
        /// 打开历史记录
        /// </summary>
        public void OpenHistory()
        {
            historyLayer.SetActive(true);
            StartCoroutine(AutoScrollToBottom());
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
        /// 关闭历史记录
        /// </summary>
        public void CloseHistory()
        {
            historyLayer.SetActive(false);
        }

        #endregion 方法

        #endregion UI模块2.2 历史

        #region UI模块2.3 自动播放

        #region 引用
        [Header("System-AutoPlay")]
        [Tooltip("自动播放按钮")]
        [SerializeField] private Button autoPlayButton;
        [Tooltip("自动播放按钮文本")]
        [SerializeField] private Text autoPlayButtonText;
        #endregion 引用

        #region 事件
        public UnityAction onAutoPlayButtonClicked;
        #endregion 事件

        #region 初始化
        public void InitAuto()
        {
            if(autoPlayButtonText == null)
            {
                Debug.LogWarning("自动播放按钮文本为空");
            }
            else
            {
                autoPlayButtonText.text = "自动";
            }

            if (autoPlayButton == null)
            {
                Debug.LogWarning("自动播放按钮为空");
            }
            else
            {
                autoPlayButton.onClick.AddListener(() => {
                    // 文字变化
                    autoPlayButtonText.text = (autoPlayButtonText.text == "自动") ? "自动中..." : "自动";
                    // 点击后的其他效果
                    onAutoPlayButtonClicked?.Invoke();
                });
            }
        }
        #endregion 初始化

        #endregion UI模块2.3 自动播放

        #region UI模块2.4 跳过

        #region 引用

        [Header("System-ShowSkipInfoPopup")]
        [Tooltip("跳过按钮")]
        [SerializeField] private Button skipButton;
        [Tooltip("是否跳过弹窗预制体")]
        [SerializeField] private GameObject skipInfoPopupPrefab;

        #endregion 引用

        #region 方法

        /// <summary>
        /// 初始化跳过UI
        /// </summary>
        public void InitSkip()
        {
            if(skipInfoPopupPrefab == null)
            {
                Debug.LogWarning("跳过弹窗预制体为空");
            }

            if(skipButton == null)
            {
                Debug.LogWarning("跳过按钮为空");
            }
            else
            {
                skipButton.onClick.AddListener(() => ShowSkipInfoPopup());
            }
        }

        /// <summary>
        /// 当玩家点击“跳过”按钮时触发
        /// </summary>
        private void ShowSkipInfoPopup()
        {
            // 打开交互层、交互层遮罩、信息弹窗容器，维护当前状态
            interactionLayer.gameObject.SetActive(true);
            interactionMask.gameObject.SetActive(true);
            infoContainer.gameObject.SetActive(true);

            // 触发交互事件
            EventCenter.interactionStarted?.Invoke();

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

        #endregion 方法

        #endregion UI模块2.4 跳过

        #endregion UI层次2：系统层

        #region UI层次3：对话层

        #region 引用
        [Header("对话层")]
        [SerializeField] private GameObject dialogLayer;
        [Tooltip("对话框上点击继续按钮")]
        [SerializeField] private Button continueButton;
        [Tooltip("对话框说话者名字文本")]
        [SerializeField] private Text dialogueBoxName;
        [Tooltip("对话框文字内容文本")]
        [SerializeField] private Text dialogueBoxContent;

        [Tooltip("进入新对话结点时的音效")]
        [SerializeField] private AudioSource dialogueBoxUpdatingSound;
        #endregion

        #region 配置区域
        [Tooltip("打字机时间间隔")]
        [SerializeField] private float typingInterval = 0.1f;
        #endregion

        #region 事件
        // 对话框继续按钮被点击
        public UnityAction onContinueButtonClicked;
        // 对话框内容更新
        public UnityAction<string, string> dialogueBoxUpdate;
        // 开始打字
        public UnityAction TypingStarted;
        // 结束打字
        public UnityAction TypingFinished;
        #endregion

        #region 运行时变量
        private Coroutine _typeTextCoroutine = null; // 用于记录已经开启的打字机协程
        private Stack<string> tagStack = new Stack<string>(); // 记录富文本标签名的栈，用于自动追加闭合标签
        private List<string> tagList = new List<string>(); // 记录富文本标签名的列表，用于自动追加开标签

        private bool _isPaused = false;
        #endregion

        #region 公共接口
        /// <summary>
        /// 初始化对话层
        /// </summary>
        public void InitDialogueLayer()
        {
            // --- 对话层 ---
            dialogLayer?.SetActive(true);

            // 对话框点击继续按钮
            continueButton?.onClick.AddListener(() => onContinueButtonClicked());

            // 绑定音效
            // TODO : 绑定音效逻辑日后转移到专门的音效管理器中处理
            dialogueBoxUpdate += PlayDialogueBoxUpdatingSound;
        }

        /// <summary>
        /// 更新对话框的 文字内容 和 说话者名字
        /// </summary>
        public void StartUpdateDialogueBox(string name, string content)
        {
            // --- 事件唤醒 ---
            dialogueBoxUpdate?.Invoke(name, content);

            // --- 更新说话者名字 ---
            dialogueBoxName.text = name;

            // --- 打字 ---
            dialogueBoxContent.text = "";
            _typeTextCoroutine = StartCoroutine(TypeText(content));
        }

        /// <summary>
        /// 立即完成对话框的更新
        /// </summary>
        public void FinishUpdateDialogueBox(string content)
        {
            // 停止打字机协程
            StopCoroutine(_typeTextCoroutine);

            // 直接显示完整文字
            dialogueBoxContent.text = content;

            // 事件唤醒
            TypingFinished?.Invoke();

            // 维护运行时
            tagList.Clear();
            tagStack.Clear();
        }

        /// <summary>
        /// 用于暂停已经开启的打字机协程
        /// </summary>
        public void PauseUpdateDialogueBox(bool pause)
        {
            _isPaused = pause;
        }
        #endregion

        #region 私有逻辑函数

        /// <summary>
        /// 一个打字机协程，用于对话框等文字内容的打字机效果展示
        /// </summary>
        /// <param name="targrtStr">要打印的最终文字</param>
        private IEnumerator TypeText(string targetStr)
        {
            // 遍历目标字符串的每个字符，逐个添加到对话框当前文本的末尾，遇到标签则特殊处理
            int length = targetStr.Length;
            for (int i = 0; i < length; i++)
            {
                // 检查是否应该暂停打字
                while (_isPaused)
                {
                    yield return null;
                }

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
                // --- 事件唤醒 ---
                TypingStarted?.Invoke();
                dialogueBoxContent.text += additionStr;

                yield return new WaitForSeconds(typingInterval);
            }

            // 目标字符串遍历完毕，保险起见，清空标签栈和列表
            tagStack.Clear();
            tagList.Clear();

            // --- 事件唤醒 ---
            TypingFinished?.Invoke();
        }

        /// <summary>
        /// 对话框更新时的音效
        /// </summary>
        private void PlayDialogueBoxUpdatingSound(string str1, string str2)
        {
            dialogueBoxUpdatingSound?.Play();
        }

        #endregion

        #endregion UI层次3：对话层

        #region UI层次4：交互层

        #region 交互层总成

        #region 引用
        [Header("交互层")]
        [Tooltip("交互层容器")]
        [SerializeField] private Transform interactionLayer;
        [Tooltip("交互层遮罩")]
        [SerializeField] private Image interactionMask;
        #endregion 引用

        #region 初始化
        public void InitInteractionLayer()
        {
            // 检查引用
            if(interactionLayer == null)
            {
                Debug.LogWarning("交互层容器为空");
            }
            else
            {
                interactionLayer.gameObject.SetActive(false);
            }

            if(interactionMask == null)
            {
                Debug.LogWarning("交互层遮罩为空");
            }
            else
            {
                interactionMask.gameObject.SetActive(false);
            }

            // 初始化选项组交互
            InitOptionInteraction();

            // 初始化信息弹窗交互
            InitInfoInteraction();
        }
        #endregion 初始化

        #endregion 交互层总成

        #region UI模块4.1：选项组交互

        #region 引用

        [Header("Options")]
        [Tooltip("选项按钮预制体")]
        [SerializeField] private GameObject optionPrefab;
        [Tooltip("选项组容器")]
        [SerializeField] private Transform optionContainer;

        #endregion

        #region 事件

        public UnityAction<string, string, string> onOptionButtonClicked;

        public UnityAction<string, string> optionsDestroyed;

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化选项组交互模块
        /// </summary>
        public void InitOptionInteraction()
        {
            // --- 检查引用状态 ---
            if(optionPrefab == null)
            {
                Debug.LogWarning("选项按钮预制体为空");
            }

            if(optionContainer == null)
            {
                Debug.LogWarning("选项组容器为空");
            }
            else
            {
                optionContainer.gameObject.SetActive(false);
            }

            // --- 监听事件 ---
            // 打字结束时，获得生成选项的许可令牌
            TypingFinished += () => { optionToken = true; };
            // 选项按钮点击事件
            onOptionButtonClicked += OnOptionButtonClicked;
        }

        /// <summary>
        /// 根据所给的选项结点信息生成交互选项
        /// </summary>
        /// <param name="options">选项结点信息</param>
        public void ShowOptions(List<OptionNode> options)
        {
            StartCoroutine(ShowOptionsAfterTyping(options));
        }

        #endregion 公共方法

        #region 运行时变量

        // 表示是否允许生成选项的令牌
        bool optionToken = false;

        #endregion

        #region 私有方法

        /// <summary>
        /// 用于生成对话结点中的 选项组 的协程函数
        /// </summary>
        private IEnumerator ShowOptionsAfterTyping(List<OptionNode> options)
        {
            // 直到打字机打印全部文字后再显示选项
            yield return new WaitUntil(() => optionToken);

            // 消耗令牌
            optionToken = false;

            // 等待结束，检查发现没有选项要生成，停止本协程
            if(options == null) yield break;

            // 触发交互事件
            EventCenter.interactionStarted?.Invoke();

            // 启动交互层
            interactionLayer.gameObject.SetActive(true);
            interactionMask.gameObject.SetActive(true);

            // 打开选项组容器
            optionContainer.gameObject.SetActive(true);

            // 生成选项
            foreach (var opt in options)
            {
                // 1. 生成Option预制体
                GameObject optionObj = Instantiate(optionPrefab, optionContainer);

                // 2. 闭包捕获变量
                string idOfOpt = opt.id;
                string textOfOpt = opt.text;
                string targetIdOfOpt = opt.targetId;

                // 3. 设置Option文字
                optionObj.GetComponentInChildren<Text>().text = textOfOpt;

                // 4. 为选项按钮绑定点击效果
                Button btn = optionObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    onOptionButtonClicked?.Invoke(idOfOpt, textOfOpt, targetIdOfOpt);
                });
            }
        }

        /// <summary>
        /// 用于绑定对话选项的回调函数
        /// </summary>
        /// <param name="targetId">所选选项的下一个对话结点的Id</param>
        private void OnOptionButtonClicked(string id, string text, string targetId)
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

            // 触发交互结束事件
            EventCenter.interactionFinished?.Invoke();

            // 触发选项销毁事件
            optionsDestroyed?.Invoke(id, targetId);
        }

        #endregion 私有方法

        #endregion UI模块4.1：选项组交互

        #region UI模块4.2：信息弹窗交互

        #region 引用
        [Tooltip("信息弹窗容器")]
        [SerializeField] private Transform infoContainer;
        #endregion 引用

        #region 方法
        /// <summary>
        /// 初始化信息弹窗交互
        /// </summary>
        public void InitInfoInteraction()
        {
            if(infoContainer == null)
            {
                Debug.LogWarning("信息弹窗容器为空");
            }
            else
            {
                infoContainer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 用于失活信息弹窗容器、交互层、交互层遮罩的协程函数
        /// </summary>
        IEnumerator CloseInfoPopup()
        {
            yield return null;
            infoContainer.gameObject.SetActive(false);
            interactionMask.gameObject.SetActive(false);
            interactionLayer.gameObject.SetActive(false);
            EventCenter.interactionFinished?.Invoke();
        }

        #endregion 方法

        #endregion UI模块4.2：信息弹窗交互

        #endregion UI层次4：交互层

        #region UI层次5：遮罩层

        #region 引用
        [Header("遮罩层")]
        [SerializeField] private GameObject loadingMusk;
        #endregion 引用

        #region 方法

        public void ShowLoadingMusk(bool isShown)
        {
            if (isShown)
            {
                loadingMusk.SetActive(true);
            }
            else
            {
                loadingMusk.SetActive(false);
            }
        }

        #endregion 方法



        #endregion UI层次5：遮罩层

        #endregion UI层次结构

    }
}
