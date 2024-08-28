using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Video;
using DG.Tweening;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class ContentSelector : Singleton<ContentSelector>
{
    [SerializeField] VideoPlayer _videoPlayerL;
    [SerializeField] VideoPlayer _videoPlayerR;
    [SerializeField] ContentSquare _contentSquarePrefab;
    [SerializeField] GameObject _contentContainer;

    [SerializeField] float _crossFadeTime = 0.6f;
    [SerializeField] CanvasGroup _lCanvas;
    [SerializeField] CanvasGroup _rCanvas;

    public bool IsTransitioning { get { return _isTransitioning; } }
    bool _isTransitioning = false;

    bool isLeftActive;

    void Start()
    {
        // Reset state
        DeactivateEntireCanvasContent(_lCanvas);
        DeactivateEntireCanvasContent(_rCanvas);

        var clips = Resources.LoadAll<VideoClip>("Videos");

        foreach (var clip in clips)
        {
            var o = Instantiate(_contentSquarePrefab);
            o.gameObject.transform.SetParent(_contentContainer.transform, false);
            o.name = clip.name;

            o.Setup(clip);
        }

        isLeftActive = false;
        SelectContent("FullColor Shader", 1f);
    }

    async public void SelectContent(VideoClip videoClip)
    {
        if (_isTransitioning) return;

        _isTransitioning = true;

        // If L is active, we're about to transition to R
        var nextActivePlayer = isLeftActive ? _videoPlayerR : _videoPlayerL;
        nextActivePlayer.clip = videoClip;

        await PlayVideo(nextActivePlayer);

        SelectContent("Video");
    }

    public void SelectContent(string contentName, float delay = 0f)
    {
        _isTransitioning = true;

        CrossFade(contentName, delay);
    }

    async void CrossFade(string contentName, float delay = 0f)
    {
        isLeftActive = !isLeftActive;

        var activeCanvas = isLeftActive ? _lCanvas : _rCanvas;
        var nonActiveCanvas = !isLeftActive ? _lCanvas : _rCanvas;

        var go = Utils.FindChildByName(activeCanvas.gameObject, contentName);
        
        go.SetActive(true);

        if (isLeftActive)
        {
            _rCanvas.DOFade(0f, _crossFadeTime);

            _lCanvas.alpha = 0f;
            _lCanvas.DOFade(1f, _crossFadeTime).SetDelay(delay);
        }
        else
        {
            _lCanvas.DOFade(0f, _crossFadeTime);

            _rCanvas.alpha = 0;
            _rCanvas.DOFade(1f, _crossFadeTime).SetDelay(delay);
        }

        await Task.Delay((int)(_crossFadeTime * 1300));

        DeactivateEntireCanvasContent(nonActiveCanvas);

        _isTransitioning = false;
    }

    void DeactivateEntireCanvasContent(CanvasGroup canvasGroup)
    {
        foreach (Transform child in canvasGroup.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    async Task PlayVideo(VideoPlayer player)
    {
        player.gameObject.SetActive(true);
        player.Prepare();

        while (!player.isPrepared)
            await Task.Yield();

        player.frame = 0;
        player.Play();
    }
}