using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor.Build.Pipeline;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AVG
{
    public class ResourceManager
    {
        #region 单例模式访问点
        private ResourceManager() { }
        private static ResourceManager _instance = new ResourceManager();
        public static ResourceManager Instance { get => _instance; }
        #endregion

        #region 对外提供的服务
        public async Task<Sprite> GetSpriteAsync(string spriteName)
        {
            if (spritesDic.ContainsKey(spriteName))
            {
                return spritesDic[spriteName].Result;
            }

            Sprite loadedSprite = await LoadSpriteAsnyc(spriteName);

            return loadedSprite;
        }
        #endregion

        #region 内部资源
        // 用于记录成功缓存的精灵图片的句柄
        private Dictionary<string, AsyncOperationHandle<Sprite>> spritesDic = new Dictionary<string, AsyncOperationHandle<Sprite>>();
        #endregion

        #region 私有逻辑函数
        async Task<Sprite> LoadSpriteAsnyc(string spriteName)
        {
            
            // --- 开始异步加载 --- 
            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(spriteName);
            #region 调试信息
            Debug.Log($"ResourceManager: 开始加载资源: {spriteName}");
            #endregion
            await handle.Task;

            // --- 异步加载结束 ---
            // 1. 加载失败
            if(handle.Status != AsyncOperationStatus.Succeeded)
            {
                #region 调试信息
                Debug.LogWarning($"ResourceManager: 资源加载失败: {spriteName}");
                #endregion

                Addressables.Release(handle);
                return null;
            }
            // 2. 加载成功
            else
            {
                #region 调试信息
                Debug.Log($"ResourceManager: 资源加载成功: {spriteName}");
                #endregion

                if (!spritesDic.ContainsKey(spriteName))
                {
                    spritesDic.Add(spriteName, handle);
                }
                return handle.Result;
            }
        }
        #endregion
    }
}
