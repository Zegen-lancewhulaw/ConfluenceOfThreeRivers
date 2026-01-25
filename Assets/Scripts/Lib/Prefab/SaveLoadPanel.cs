using AVG;
using AVG.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SaveLoadPanel : MonoBehaviour
{
    #region 引用
    [Header("基本")]
    [SerializeField] private Text title;
    [SerializeField] private Button close;

    [Header("存档槽")]
    [SerializeField] private List<SaveEntryPrefab> saveEntryPrefabsList = new List<SaveEntryPrefab>();

    [Header("页码")]
    [SerializeField] private List<Button> pagesList = new List<Button>();
    [SerializeField] private Button left;
    [SerializeField] private Button right;

    #endregion 引用

    #region 运行时状态
    private int _currentPageIndex = 0; // 0表示页码1
    private bool _isSave = false; // 是否读档
    #endregion 运行时状态

    #region 公共调用

    public void ShowSaveLoadPanel(bool isSave)
    {
        _isSave = isSave;
        UpdateSaveLoadPanel();
    }

    public void UpdateSaveLoadPanel()
    {
        StartCoroutine(UpdateSaveLoadPanelCoroutine());
    }

    #endregion 公共调用

    #region 事件

    public UnityAction<string> OnSaveButtonClicked;
    public UnityAction<string> OnLoadButtonClicked;
    public UnityAction<string> OnDeleteButtonClicked;

    #endregion

    #region 生命周期
    // Start is called before the first frame update
    private void Start()
    {
        InitEvent();

    }

    // Update is called once per frame
    void OnDestroy()
    {
        ClearEvent();
    }
    #endregion 生命周期


    #region 私有逻辑方法

    private void InitEvent()
    {
        // 监听翻页按钮事件
        for(int i = 0; i < pagesList.Count; i++)
        {
            int index = i;
            pagesList[i].onClick.AddListener(() => { 
                _currentPageIndex = index;
                UpdateSaveLoadPanel();
            });
        }
        right.onClick.AddListener(() => { 
            _currentPageIndex = (_currentPageIndex + 1) % 6; 
            UpdateSaveLoadPanel();
        });
        left.onClick.AddListener(() => { 
            _currentPageIndex = (_currentPageIndex + 5) % 6; 
            UpdateSaveLoadPanel();
        });

        // 监听关闭按钮事件
        close.onClick.AddListener(() =>
        {
            GlobalUIManager.Instance.CloseSaveLoadPanel();
        });

        // 监听存档按钮事件
        for(int i = 0; i < saveEntryPrefabsList.Count; i++)
        {
            int index = _currentPageIndex * 6 + i + 1;
            saveEntryPrefabsList[i].SetSaveButtonEffect(() => {
                OnSaveButtonClicked?.Invoke(index.ToString()); // 最终回调controller的Save方法，存档并更新存档头
            });

        }

        // 监听读档按钮事件
        for(int i = 0; i < saveEntryPrefabsList.Count; i++)
        {
            int index = _currentPageIndex * 6 + i + 1;
            saveEntryPrefabsList[i].SetLoadButtonEffect(() =>
            {
                GlobalUIManager.Instance.CloseSaveLoadPanel();
                OnLoadButtonClicked?.Invoke(index.ToString()); // 最终回调controller的Load方法，进行读档
            });
        }

        // TODO : 监听删除按钮事件

        // 监听事件：存档操作完成时，更新存取界面UI
        EventCenter.OnSaveOperationFinished += UpdateSaveLoadPanel;
    }

    void ClearEvent()
    {
        EventCenter.OnSaveOperationFinished -= UpdateSaveLoadPanel;
    }

    private IEnumerator UpdateSaveLoadPanelCoroutine()
    {
        // 请求SaveManager确保存档头已经在内存中
        SaveManager.EnsureSaveHeadersExisting();

        float timer = 0f;
        while (SaveManager.saveHeaders == null && timer < 3f)
        {
            timer += Time.deltaTime;
            yield return null; // 等待下一帧
        }

        //如果超时，返回，不刷新存取档界面
        if (timer >= 3f)
        {
            Debug.LogError("获取存档头数据超时！");
            yield break;
        }

        // --- 刷新存取档界面 ---
        UpdateSaveLoadPanelInternal(_isSave);
    }

    /// <summary>
    /// 根据当前页数和存档头数据，刷新存取档页面的UI表现内容
    /// </summary>
    private void UpdateSaveLoadPanelInternal(bool isSave)
    {
        // --- 刷新页面标题 ---
        if (isSave)
        {
            title.text = $"存档page{_currentPageIndex + 1}";
        }
        else
        {
            title.text = $"读档page{_currentPageIndex + 1}";
        }

        // --- 刷新存档槽 ---
        for (int i = 1; i <= 6; i++)
        {
            // 1. 确定本轮循环要刷新的存档槽
            SaveEntryPrefab saveEntryToUpdate = saveEntryPrefabsList[i - 1];

            // 2 寻找存档槽对应的存档头数据
            // key就是存档头在字典中的键，1~6，7~12，……，31~36。
            // 字典中为key为0的存档槽是自动存档槽，不在存取档界面显示
            string key = (_currentPageIndex * 6 + i).ToString();

            // 如果有对应的存档头数据，根据数据刷新
            if (SaveManager.saveHeaders.headers.ContainsKey(key))
            {
                SaveHeader saveHeader = SaveManager.saveHeaders.headers[key];

                // （1）异步设置截图文件
                StartCoroutine(SetScreenshotCoroutine(saveHeader.screenshotName, saveEntryToUpdate.SetScreenshotImage));

                // （2）更新章节名和时间
                saveEntryToUpdate.SetChapterNameText(saveHeader.chapterName);
                saveEntryToUpdate.SetSaveTimeText(saveHeader.saveTime);

                // （3）根据存档还是读档，显示或关闭存档槽上的按钮
                if (isSave)
                {
                    // 激活存档按钮，关闭读档按钮，激活删除按钮
                    saveEntryToUpdate.ShowSaveButton(true);
                    saveEntryToUpdate.ShowLoadButton(false);
                    saveEntryToUpdate.ShowDeleteButton(true);
                }
                else
                {
                    // 关闭存档按钮，激活读档按钮，激活删除按钮
                    saveEntryToUpdate.ShowSaveButton(false);
                    saveEntryToUpdate.ShowLoadButton(true);
                    saveEntryToUpdate.ShowDeleteButton(true);
                }

            }

            // 如果没有，刷新为默认样式
            else
            {
                // （1）截图为空
                saveEntryToUpdate.SetScreenshotImage(null);

                // （2）更新章节名和时间
                saveEntryToUpdate.SetChapterNameText("---");
                saveEntryToUpdate.SetSaveTimeText("--- ---:---");

                // （3）根据存档还是读档，显示或关闭存档槽上的按钮
                if (isSave)
                {
                    // 激活存档按钮，关闭读档按钮，关闭删除按钮
                    saveEntryToUpdate.ShowSaveButton(true);
                    saveEntryToUpdate.ShowLoadButton(false);
                    saveEntryToUpdate.ShowDeleteButton(false);
                }
                else
                {
                    // 关闭存档按钮，关闭读档按钮，关闭删除按钮
                    saveEntryToUpdate.ShowSaveButton(false);
                    saveEntryToUpdate.ShowLoadButton(false);
                    saveEntryToUpdate.ShowDeleteButton(false);
                }
            }

            // 3. 检查该存档是否为最新存档，是的话则显示最新标签
            if (SaveManager.saveHeaders.latestSaveEntryId == key)
            {
                saveEntryToUpdate.ShowNewLabel(true);
            }
            else
            {
                saveEntryToUpdate.ShowNewLabel(false);
            }
        }
    }

    IEnumerator SetScreenshotCoroutine(string screenshotName, UnityAction<Sprite> setAction)
    {
        Task<Sprite> task = LoadScreenshotSpriteAsync(screenshotName);
        yield return new WaitUntil(() => task.IsCompleted);
        
        if(task.Result == null)
        {
            yield break;
        }

        setAction?.Invoke(task.Result);
    }

    async Task<Sprite> LoadScreenshotSpriteAsync(string screenshotName)
    {
        Texture2D screenshotTexture = await LoadScreenshotTextureAsync(screenshotName);

        if (screenshotTexture == null) return null;

        Sprite screenshotSprite = Sprite.Create(
            screenshotTexture,
            new Rect(0, 0, screenshotTexture.width, screenshotTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        return screenshotSprite;
    }

    async Task<Texture2D> LoadScreenshotTextureAsync(string screenshotName)
    {
        // 获取截图文件路径
        if (!screenshotName.EndsWith(".jpg"))
        {
            screenshotName += ".jpg";
        }
        string screenshotPath = Path.Combine(Application.persistentDataPath, screenshotName);

        // 验证文件存在
        if (!File.Exists(screenshotPath))
        {
            Debug.LogWarning($"{screenshotPath}不存在");
            return null;
        }

        // 异步读取图片文件字节数据
        byte[] screenshotBytes = await File.ReadAllBytesAsync(screenshotPath);

        // 装载Texture2D
        Texture2D screenshotTexture = new Texture2D(2, 2);
        bool success = screenshotTexture.LoadImage(screenshotBytes);
        
        return success ? screenshotTexture : null;
    }
    #endregion
}
