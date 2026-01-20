using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Information_YorN : MonoBehaviour
{
    [Header("子对象引用")]
    [SerializeField] private Text informationText;
    [SerializeField] private Text yesText;
    [SerializeField] private Text noText;
    [SerializeField] private Button close;
    public void SetInformationText(string value)
    {
        informationText.text = value;
    }

    public void SetYesText(string value)
    {
        yesText.text = value;
    }

    public void SetNoText(string value)
    {
        noText.text = value;
    }

    public void SetCloseEffect(UnityAction act)
    {
        if(close != null && act != null)
        {
            close.onClick.AddListener(act);
        }
    }

    public void Close()
    {
        Destroy(this.gameObject);
    }
}
