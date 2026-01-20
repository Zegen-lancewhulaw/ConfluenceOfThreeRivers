using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AVG
{
    public class HistoryManager : MonoBehaviour
    {
        #region 引用区域
        [Header("Cooperative Managers")]
        public UIManager uiManager;
        public Controller controller;
        public StageManager stageManager;

        [Header("System-History")]
        [Tooltip("整个历史记录层")]
        [SerializeField] private GameObject historyLayer;
        [Tooltip("历史记录滚动视窗上的ScrollRect")]
        [SerializeField] private ScrollRect historyScrollRect;
        [Tooltip("容纳历史记录项目的容器")]
        [SerializeField] private Transform historyItemContainer;
        [Tooltip("历史记录项目预制体")]
        [SerializeField] private GameObject historyItemPrefab;
        [Tooltip("历史记录最大条目数")]
        [SerializeField] private int maxHistoryItemCount = 100;
        #endregion

        #region 公共调用接口
        /// <summary>
        /// 初始化历史记录
        /// </summary>
        public void InitHistory()
        {
            // --- 清空历史记录 ---
            ClearHistory();
            // --- 关闭历史记录层 --- 
            CloseHistory();
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearHistory()
        {
            if (historyItemContainer != null && historyItemContainer.childCount > 0)
            {
                foreach (Transform child in historyItemContainer)
                {
                    Destroy(child.gameObject);
                }
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
        /// 关闭历史记录
        /// </summary>
        public void CloseHistory()
        {
            historyLayer.SetActive(false);
        }

        /// <summary>
        /// 添加对话历史记录
        /// </summary>
        /// <param name="name">说话者名字</param>
        /// <param name="content">说话内容</param>
        public void AddDialogueHistoryItem(string name, string content)
        {
            AddHistoryItem(name, content, false);
        }

        /// <summary>
        /// 添加选项历史记录
        /// </summary>
        /// <param name="text">选项内容</param>
        /// <param name="targetId">选项指向的下一个结点id</param>
        public void AddOptionHistoryItem(string text, string targetId)
        {
            AddHistoryItem(null, text, true);
        }

        /// <summary>
        /// 添加历史记录
        /// （对话框内容更新时 或者 玩家选择选项时触发
        /// 将已经播放的对话的说话者名字、说话内容，或者选项标志、选项内容...
        /// ...以历史记录条目的形式加入到历史记录层的滚动视窗中）
        /// </summary>
        /// <param name="name">说话者名字 或者 当是选项的时候为“选项”二字</param>
        /// <param name="content">说话内容 或者 选项内容</param>
        /// <param name="isOption">是否为选项</param>
        public void AddHistoryItem(string name, string content, bool isOption)
        {
            // --- 实例化预制体 ---
            GameObject historyItemObj = Instantiate(historyItemPrefab, historyItemContainer);

            // --- 填写历史记录条目字段 ---
            HistoryItem historyItem = historyItemObj.GetComponent<HistoryItem>();
            #region 调试信息
            if (historyItem != null)
            {
                print("找到HistoricalDialogueItem脚本");
            }
            else
            {
                print("没找到HistoricalDialogueItem脚本");
            }
            #endregion

            // 1. 填入名字字段
            if (isOption)
            {
                historyItem.SetName("选项");
            }
            else
            {
                historyItem.SetName(name);
            }

            // 2. 填入内容字段
            historyItem.SetContent(content);

            // --- 限制数量 ---
            // 如果历史记录条目数超过最大允许数量，则删除最早的一个条目，以节省内存
            if (historyItemContainer.childCount > maxHistoryItemCount)
            {
                Destroy(historyItemContainer.GetChild(0).gameObject);
            }
        }
        #endregion

        #region 生命周期
        private void Start()
        {
            // --- 初始化 ---
            InitHistory();
            // --- 监听事件 ---
            if(uiManager != null)
            {
                uiManager.dialogueBoxUpdate += AddDialogueHistoryItem;
                uiManager.optionButtonClicked += AddOptionHistoryItem;
                uiManager.historyButtonClicked += (isOnHistory) =>
                {
                    if (isOnHistory)
                        OpenHistory();
                    else
                        CloseHistory();
                };
            }
        }
        #endregion

        #region 私有逻辑函数
        /// <summary>
        /// 自动将历史记录界面滚动到底端的协程函数
        /// </summary>
        private IEnumerator AutoScrollToBottom()
        {
            yield return new WaitForEndOfFrame(); // 等待这一帧结束，因为UI的高度不是实时刷新，而是下一帧才算出来

            historyScrollRect.verticalNormalizedPosition = 0f;
        }

        #endregion
    }
}
