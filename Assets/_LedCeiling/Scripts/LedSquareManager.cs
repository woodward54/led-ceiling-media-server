using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.IO;

public class LedSquareManger : Singleton<LedSquareManger>
{
    [SerializeField] string _hostnameDomain;
    [SerializeField] List<LedSquare> _squares;
    [SerializeField] uint _framesPerSecond = 24;
    [SerializeField] int AntiAliasing = 8;
    [SerializeField] LedSquareController _squareControllerPrefab;
    [SerializeField] Camera _captureCamera;
    [SerializeField] RectTransform _videoCanvas;

    public Vector2 _capturePos;

    public Texture2D ScreenShot { get { return _screenShot; } }
    public Color32[] Pixels32 { get { return _pixels; } }

    Texture2D _screenShot;
    Color32[] _pixels;
    List<LedSquareController> _squareControllers;

    int resWidth = 620;
    int resHeight = 496;

    void Awake()
    {
        _squareControllers = new();

        _screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);

        foreach (var s in _squares)
        {
            if (!s.Enabled) continue;

            var newSquare = Instantiate(_squareControllerPrefab, Vector3.zero, Quaternion.identity, transform);

            newSquare.name = s.Hostname;
            newSquare.HostName = s.Hostname + _hostnameDomain;
            newSquare.FramesPerSecond = _framesPerSecond;
            newSquare.OffsetPosition = s.Position;
            newSquare.SquareData = s;

            _squareControllers.Add(newSquare);
        }
    }

    public bool _takeScreenshot;

    void Update()
    {
        CaptureScreenshot();

        if (_takeScreenshot)
        {
            File.WriteAllBytes(Path.Combine(Application.persistentDataPath + "Img1.png"), _screenShot.EncodeToPNG());
            _takeScreenshot = false;
        }
    }

    public void CaptureScreenshot()
    {
        // Move the _videoCanvas to 0,0 to take the screenshot
        Vector2 startPos = _videoCanvas.anchoredPosition;
        _videoCanvas.anchoredPosition = Vector2.zero;

        var rt = new RenderTexture(resWidth, resHeight, 24);
        rt.antiAliasing = AntiAliasing;
        _captureCamera.targetTexture = rt;

        _captureCamera.Render();
        RenderTexture.active = rt;

        Rect rect = new Rect(Vector2.zero, new Vector2(resWidth, resHeight));

        _screenShot.ReadPixels(rect, 0, 0);

        _captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        int sourceMipLevel = 0;
        _pixels = _screenShot.GetPixels32(sourceMipLevel);

        _videoCanvas.anchoredPosition = startPos;
    }
}