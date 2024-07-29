using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public abstract class ContentSquareData
{
    public string Name;
    public int PlayTimeSeconds;
}

[Serializable]
public class ContentSquareDataVideo : ContentSquareData
{
    public string Path;
    public float Speed;
}

[Serializable]
public class ContentSquareDataShader : ContentSquareData
{
    // TODO shader material
    
    public float Intensity;
    public float Value1;
    public float Value2;
    public float Value3;
}

public class ContentSquareOld : MonoBehaviour
{
    [SerializeField] Image _previewImg;
    [SerializeField] Button _button;

    public ContentSquareData Data;

    public void Setup(ContentSquareData data)
    {
        Data = data;

        _button.onClick.AddListener(delegate { ContentSquareManager.Instance.SelectContent(Data); });
    }
}
