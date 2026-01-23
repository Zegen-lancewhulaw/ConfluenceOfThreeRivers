using AVG.Model;
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
        /// 根据给定的对话结点更新舞台
        /// </summary>
        /// <param name="node">对话结点数据</param>
        public void UpdateStage(DialogueNode node)
        {
            // 检测是否有背景图片变化
            if (node.background != null)
            {
                UpdateImage(bgImage, node.background);
            }

            // 检测是否有立绘变化
            if (node.charLeft != null)
            {
                UpdateImage(charLeft, node.charLeft);
            }

            if (node.charCenter != null)
            {
                UpdateImage(charCenter, node.charCenter);
            }

            if (node.charRight != null)
            {
                UpdateImage(charRight, node.charRight);
            }

            // 检测是否有CG变化
            if (node.cgImage != null)
            {
                UpdateImage(cgImage, node.cgImage);
            }
        }

        /// <summary>
        /// 根据指定的图片容器类型 和 新图片资源名字 更新图片
        /// </summary>
        /// <param name="type">图片容器类型枚举变量</param>
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
            // --- 更新该图片容器 ---
            UpdateImage(imageToUpdate, newSpriteName);
        }

        /// <summary>
        /// 根据指定的图片容器 和 新图片资源名字 更新图片
        /// </summary>
        /// <param name="imageToUpdate">图片容器</param>
        /// <param name="newSpriteName">新图片名字</param>
        public void UpdateImage(Image imageToUpdate, string newSpriteName)
        {
            // --- 获取图片容器当前内容 ---
            string oldSpriteName = imageSpriteNameDic[imageToUpdate];

            // --- 卫语句：处理特殊情况 ---

            // 情况A：指令无效
            if (string.IsNullOrEmpty(newSpriteName)) return;

            // 情况B：指令是清除图片
            if (newSpriteName == "REMOVE")
            {
                UpdateImageWithResource(imageToUpdate, null, null); // 执行清理
                return;
            }

            // 情况C：请求的图片就是当前显示的图片
            if (oldSpriteName == newSpriteName) return;

            // --- 开启协程，先获取新图片资源，再更新图片 ---
            StartCoroutine(UpdateImageSearchingResource(imageToUpdate, newSpriteName));
        }
        #endregion

        #region 事件
        // CG图更新事件
        public UnityAction CGUpdated;
        #endregion

        #region 运行时状态
        // 用于记录Image对象目前承载的图片的名字
        private Dictionary<Image, string> imageSpriteNameDic = new Dictionary<Image, string>();
        #endregion

        #region 生命周期
        private void Awake()
        {
            // --- 初始化 ---
            InitStage();
        }
        #endregion

        #region 私有逻辑函数
        /// <summary>
        /// 用来更新背景图片、立绘、CG图片的协程函数
        /// </summary>
        /// <param name="imageToUpdate">要更新的图片容器</param>
        /// <param name="newSpriteName">图片资源的名字</param>
        private IEnumerator UpdateImageSearchingResource(Image imageToUpdate, string newSpriteName)
        {
            // --- 获取资源 ---
            var task = ResourceManager.Instance.GetSpriteAsync(newSpriteName);
            yield return new WaitUntil(() => task.IsCompleted);

            Sprite spriteToSet = task.Result;
            if (spriteToSet != null)
            {
                // --- 执行最终应用逻辑 ---
                UpdateImageWithResource(imageToUpdate, newSpriteName, spriteToSet);
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
        private void UpdateImageWithResource(Image imageToUpdate, string newSpriteName, Sprite newSprite)
        {
            // 1. 更新 UI 和 字典
            imageToUpdate.sprite = newSprite;
            imageSpriteNameDic[imageToUpdate] = newSpriteName;

            // 2. 特殊 UI 状态处理
            // 立绘显隐
            if (imageToUpdate == charLeft || imageToUpdate == charCenter || imageToUpdate == charRight)
            {
                UpdateCharImage(imageToUpdate, newSprite == null); // null 意味着 REMOVE
            }
            // CG层开关
            else if (imageToUpdate == cgImage)
            {
                UpdateCGImage(imageToUpdate, newSprite, newSprite == null); // null 意味着 REMOVE
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
