using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace AVG
{
    /// <summary>
    /// 规定舞台上的图片类型
    /// </summary>
    public enum E_StageImageType
    {
        Background,
        CharLeft,
        CharCenter,
        CharRight,
        CG
    }

    public class StageManager : MonoBehaviour
    {
        #region 引用区域
        [Header("Cooperative Manager")]
        public Controller controller;
        public UIManager uiManager;
        public HistoryManager historyManager;

        [Header("Background")]
        [Tooltip("背景层")]
        [SerializeField] private GameObject bgLayer;
        [Tooltip("背景图片")]
        [SerializeField] private Image bgImage;

        [Header("Character Illustration")]
        [Tooltip("人物立绘层")]
        [SerializeField] private GameObject charLayer;
        [Tooltip("左侧人物立绘")]
        [SerializeField] private Image charLeft;
        [Tooltip("中间人物立绘")]
        [SerializeField] private Image charCenter;
        [Tooltip("右侧人物立绘")]
        [SerializeField] private Image charRight;

        [Header("CG")]
        [Tooltip("CG层")]
        [SerializeField] private GameObject cgLayer;
        [Tooltip("CG图片")]
        [SerializeField] private Image cgImage;
        #endregion

        #region 公共调用接口
        /// <summary>
        /// 初始化舞台（背景、立绘、CG）
        /// </summary>
        public void InitStage()
        {
            if(bgLayer != null)
            {
                bgLayer.SetActive(true);
            }

            if(bgImage != null) // 初始时，背景为空
            {
                cgImage.gameObject.SetActive(true);
                cgImage.sprite = null;
            }

            if (charLeft != null) // 初始时，左侧立绘为空且透明
            {
                charLeft.gameObject.SetActive(true);
                charLeft.sprite = null;
                charLeft.color = new Color(1, 1, 1, 0);
            }

            if (charCenter != null) // 初始时，中间立绘为空且透明
            {
                charCenter.gameObject.SetActive(true);
                charCenter.sprite = null;
                charCenter.color = new Color(1, 1, 1, 0);
            }

            if (charRight != null) // 初始时，右侧立绘为空且透明
            {
                charRight.gameObject.SetActive(true);
                charRight.sprite = null;
                charRight.color = new Color(1, 1, 1, 0);
            }

            if(cgImage != null) // 初始时，CG为空，且关闭CG层
            {
                cgImage.gameObject.SetActive(true);
                cgImage.sprite = null;
            }

            if (cgLayer != null) 
            {
                cgLayer.SetActive(false);
            }

            // 初始记录被控制的若干Image容器当前存储的图片的名字
            imageSpriteNameDic.Add(bgImage, null);
            imageSpriteNameDic.Add(charLeft, null);
            imageSpriteNameDic.Add(charCenter, null);
            imageSpriteNameDic.Add(charRight, null);
            imageSpriteNameDic.Add(cgImage, null);
        }

        /// <summary>
        /// 根据指定的图片类型 和 新图片资源名字 更新图片
        /// </summary>
        /// <param name="type">图片类型枚举变量</param>
        /// <param name="newSpriteName">新图片名字</param>
        public void UpdateImage(E_StageImageType type, string newSpriteName)
        {
            // --- 确定要更新的图片容器 ---
            Image imageToUpdate = null;
            switch (type)
            {
                case E_StageImageType.Background: imageToUpdate = bgImage; break;
                case E_StageImageType.CharLeft: imageToUpdate = charLeft; break;
                case E_StageImageType.CharCenter: imageToUpdate = charCenter; break;
                case E_StageImageType.CharRight: imageToUpdate = charRight; break;
                case E_StageImageType.CG: imageToUpdate = cgImage; break;
            }

            #region 调试信息
            if(imageToUpdate == null)
            {
                Debug.LogWarning("定位图片容器失败！");
            }
            else
            {
                print("定位图片容器成功");
            }
            #endregion

            // --- 获取图片容器当前内容 ---
            string oldSpriteName = imageSpriteNameDic[imageToUpdate];

            // --- 卫语句：处理特殊情况 ---

            // 情况A：指令无效
            if (string.IsNullOrEmpty(newSpriteName)) return;

            // 情况B：指令是清除图片
            if (newSpriteName == "REMOVE")
            {
                PerformUpdateImage(imageToUpdate, null, null); // 执行清理
                return;
            }

            // 情况C：请求的图片就是当前显示的图片
            if (oldSpriteName == newSpriteName) return;

            // --- 开启协程，先获取新图片资源，再更新图片 ---
            StartCoroutine(UpdateImageAfterLoad(imageToUpdate, newSpriteName));
        }
        #endregion

        #region 事件
        public UnityAction CGUpdated;
        #endregion

        #region 运行时状态
        // 用于记录Image对象目前承载的图片的名字
        private Dictionary<Image, string> imageSpriteNameDic = new Dictionary<Image, string>();
        #endregion

        #region 生命周期
        private void Start()
        {
            // --- 初始化 ---
            InitStage();
        }
        #endregion

        #region 私有逻辑函数
        /// <summary>
        /// 用来更新背景图片、立绘图片的函数
        /// </summary>
        /// <param name="imageToUpdate">要变化的承载图片资源的Image对象</param>
        /// <param name="newSpriteName">图片资源的名字</param>
        private IEnumerator UpdateImageAfterLoad(Image imageToUpdate, string newSpriteName)
        {
            // --- 获取资源 ---
            var task = ResourceManager.Instance.GetSpriteAsync(newSpriteName);
            yield return new WaitUntil(() => task.IsCompleted);

            Sprite spriteToSet = task.Result;
            if (spriteToSet != null)
            {
                // --- 执行最终应用逻辑 ---
                PerformUpdateImage(imageToUpdate, newSpriteName, spriteToSet);
            }
            else
            {
                #region 调试信息
                Debug.LogWarning("ResourceManager未能提供图片资源");
                #endregion
            }
        }

        /// <summary>
        /// 执行最终的图片替换、UI状态切换
        /// </summary>
        private void PerformUpdateImage(Image image, string newSpriteName, Sprite newSprite)
        {
            // 1. 更新 UI 和 字典
            image.sprite = newSprite;
            imageSpriteNameDic[image] = newSpriteName;

            // 2. 特殊 UI 状态处理 (立绘显隐 / CG层开关)
            if (image == charLeft || image == charCenter || image == charRight)
            {
                UpdateCharImage(image, newSprite == null); // null 意味着 REMOVE
            }
            else if (image == cgImage)
            {
                UpdateCGImage(image, newSprite, newSprite == null); // null 意味着 REMOVE
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
                // 执行回调函数
                CGUpdated?.Invoke();
            }
        }
        #endregion
    }
}
