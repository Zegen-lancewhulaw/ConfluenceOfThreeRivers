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

            await LoadSpriteAsnyc(spriteName);

            return spritesDic.ContainsKey(spriteName) ? spritesDic[spriteName].Result : null;
        }
        #endregion

        #region 内部资源
        // 用于记录成功缓存的精灵图片的句柄
        private Dictionary<string, AsyncOperationHandle<Sprite>> spritesDic = new Dictionary<string, AsyncOperationHandle<Sprite>>();
        #endregion

        #region 私有逻辑函数
        /// <summary>
        /// 异步加载指定名字的精灵图片资源，如果找到则加入到字典
        /// </summary>
        /// <param name="spriteName">要加载的精灵图片的名字</param>
        private async Task<Sprite> LoadSpriteAsnyc(string spriteName)
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

                // 在将加载成功的资源添加到字典前，检查是否在异步返回之前已经被其他线程抢先加载到该资源
                if (spritesDic.ContainsKey(spriteName))
                {
                    Addressables.Release(handle);
                    return null;
                }
                else
                {
                    spritesDic.Add(spriteName, handle);
                    return handle.Result;
                }
            }
        }

        /// <summary>
        /// 清空资源缓存
        /// </summary>
        private void ClearCache()
        {
            // --- 清理精灵图片资源 ---
            ClearSpritesCache();

            // 清理其他资源 : TODO
        }

        /// <summary>
        /// 清空精灵图片资源缓存
        /// </summary>
        private void ClearSpritesCache()
        {
            foreach(var item in spritesDic)
            {
                if (item.Value.IsValid())
                {
                    Addressables.Release(item.Value);
                }
            }
            spritesDic.Clear();
        }
        #endregion
    }
}
