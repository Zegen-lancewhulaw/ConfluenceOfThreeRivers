using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SaveEntryPrefab : MonoBehaviour
{
    [SerializeField] private Button SaveButton;
    [SerializeField] private Button LoadButton;
    [SerializeField] private Button DeleteButton;
    [SerializeField] private GameObject newLabel;
    [SerializeField] private Image screenshotImage;
    [SerializeField] private Text chapterNameText;
    [SerializeField] private Text saveTimeText;


    public void ShowSaveButton(bool isShown)
    {
        SaveButton.gameObject.SetActive(isShown);
    }

    public void ShowLoadButton(bool isShown)
    {
        LoadButton.gameObject.SetActive(isShown);
    }

    public void ShowDeleteButton(bool isShown)
    {
        DeleteButton.gameObject.SetActive(isShown);
    }

    public void SetSaveButtonEffect(UnityAction action)
    {
        SaveButton.onClick.AddListener(action);
    }

    public void SetLoadButtonEffect(UnityAction action)
    {
        LoadButton.onClick.AddListener(action);
    }

    public void SetDeleteButtonEffect(UnityAction action)
    {
        DeleteButton.onClick.AddListener(action);
    }

    public void ShowNewLabel(bool shown)
    {
        newLabel.SetActive(shown);
    }

    public void SetScreenshotImage(Sprite sprite)
    {
        screenshotImage.sprite = sprite;
    }

    public void SetChapterNameText(string text)
    {
        chapterNameText.text = text;
    }

    public void SetSaveTimeText(string text)
    {
        saveTimeText.text = text;
    }
}
