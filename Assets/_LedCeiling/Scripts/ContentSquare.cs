using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(Image), typeof(Button))]
public class ContentSquare : MonoBehaviour
{
    Image _previewImg;
    Button _button;
    VideoClip _myClip;

    void Awake()
    {
        _previewImg = GetComponent<Image>();
        _button = GetComponent<Button>();
    }

    public void Setup(VideoClip video)
    {
        _myClip = video;

        ThumbnailCreator.GetThumbnailFromVideo(video, SetThumbnail);

        _button.onClick.AddListener(delegate { OnClick(); });
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