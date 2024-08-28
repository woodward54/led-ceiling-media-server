using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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

[RequireComponent(typeof(Image), typeof(Button))]
public class ContentSquare : MonoBehaviour
{
    [SerializeField] TMP_Text _duration;

    Image _previewImg;
    Button _button;
    VideoClip _myClip;

    void Awake()
    {
        _previewImg = GetComponent<Image>();
        _button = GetComponent<Button>();

        if (_duration != null) _duration.text = "";
    }

    public void Setup(VideoClip video)
    {
        _myClip = video;

        ThumbnailCreator.GetThumbnailFromVideo(video, SetThumbnail);

        _button.onClick.AddListener(delegate { OnClick(); });

        if (_duration != null)
        {
            TimeSpan time = TimeSpan.FromSeconds(video.length);

            _duration.text = time.ToString(@"hh\:mm\:ss");
        }
    }

    void SetThumbnail(Sprite thumbnail)
    {
        _previewImg.sprite = thumbnail;
    }

    void OnClick()
    {
        ContentSelector.Instance.SelectContent(_myClip);
    }
}