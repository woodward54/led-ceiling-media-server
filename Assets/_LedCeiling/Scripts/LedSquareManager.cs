using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class LedSquareManger : Singleton<LedSquareManger>
{
    [SerializeField] string _hostnameDomain;
    [SerializeField] List<LedSquare> _squares;
    [SerializeField] uint _framesPerSecond = 24;
    [SerializeField] int AntiAliasing = 8;
    [SerializeField] LedSquareChannel _squareControllerPrefab;
    [SerializeField] Camera _captureCamera;
    [SerializeField] RectTransform _videoCanvas;

    public Vector2 _capturePos;

    public Texture2D ScreenShot { get { return _screenShot; } }
    public static Color32[] Pixels32 { get; private set; }
    public static DateTime FrameTimestamp { get; private set; }

    Texture2D _screenShot;
    List<LedSquareChannel> _squareControllers;

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

            newSquare.Setup(s.Hostname + _hostnameDomain, _framesPerSecond, s.Position, s);

            _squareControllers.Add(newSquare);
        }
    }

    public bool _takeScreenshot;

    void Update()
    {
        CaptureScreenshot();

        if (_takeScreenshot)
        {
            var path = Path.Combine(Application.persistentDataPath, "Img1.png");
            Debug.Log("Saving screenshot to " + path);
            File.WriteAllBytes(path, _screenShot.EncodeToPNG());
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
        Pixels32 = _screenShot.GetPixels32(sourceMipLevel);
        FrameTimestamp = DateTime.UtcNow;

        _videoCanvas.anchoredPosition = startPos;
    }
}