using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HistoricalDialogueItem : MonoBehaviour
{
    [Header("子对象引用")]
    [Tooltip("承载名字的Text")]
    [SerializeField] private Text nameChildText;
    [Tooltip("承载内容的Text")]
    [SerializeField] private Text contentChildText;

    public void SetName(string value)
    {
        nameChildText.text = value;
    }

    public void SetContent(string value)
    {
        contentChildText.text = value;

    }
}
