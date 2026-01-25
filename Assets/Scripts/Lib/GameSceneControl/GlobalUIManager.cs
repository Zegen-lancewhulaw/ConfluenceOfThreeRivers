using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GlobalUIManager : MonoBehaviour
{
    // 单例模式
    public static GlobalUIManager Instance;

    #region 引用

    [Header("Prefebs")]
    [SerializeField] private GameObject saveLoadPanelPrefab;

    [Header("Runtime Instances")]
    [SerializeField] private Canvas _globalCanvas;
    [SerializeField] private SaveLoadPanel _saveLoadPanel;

    #endregion 引用

    #region 事件
    public UnityAction<string> OnSaveButtonClickedHandlerPipline;
    public UnityAction<string> OnLoadButtonClickedHandlerPipline;
    #endregion 事件

    #region 生命周期

    private void Awake()
    {
        // 确保只有一个单例被创建
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 1. 确保有一个覆盖全屏的 Canvas
        _globalCanvas = GetComponent<Canvas>();
        if(_globalCanvas == null)
        {
            GameObject canvasObj = new GameObject("GlobalCanvas");
            canvasObj.transform.SetParent(this.transform);
            _globalCanvas = canvasObj.AddComponent<Canvas>();
            _globalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _globalCanvas.sortingOrder = 999; // 保证在所有场景UI之上
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            canvasObj.SetActive(false); // 初始时失活它
        }
    }

    #endregion 生命周期

    public void ShowSaveLoadPanel(bool isSave)
    {
        if(_saveLoadPanel == null)
        {
            GameObject panelObj = Instantiate(saveLoadPanelPrefab, _globalCanvas.transform);
            _saveLoadPanel = panelObj.GetComponent<SaveLoadPanel>();
            _saveLoadPanel.OnSaveButtonClicked += OnSaveButtonClickedHandlerPipline;
            _saveLoadPanel.OnLoadButtonClicked += OnLoadButtonClickedHandlerPipline;
        }

        _globalCanvas.gameObject.SetActive(true);
        _saveLoadPanel.gameObject.SetActive(true);
        _saveLoadPanel.ShowSaveLoadPanel(isSave);
    }

    public void CloseSaveLoadPanel()
    {
        _saveLoadPanel.gameObject.SetActive(false);
        _globalCanvas.gameObject.SetActive(false);
    }
}
