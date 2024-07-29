using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public static class ThumbnailCreator
{
    private struct ThumbnailRequest
    {
        public VideoClip ClipToProcess;
        public string UrlToProcess;
        public int FrameToCapture;
        public ThumnailCreated callback;
    }

    public delegate void ThumnailCreated(Sprite thumbnail);

    private static VideoPlayer videoPlayer
    {
        get
        {
            if (_videoPlayer == null)
            {
                GameObject ThumbnailProcessor = new GameObject("Thumbnail Processor");
                Object.DontDestroyOnLoad(ThumbnailProcessor);
                _videoPlayer = ThumbnailProcessor.AddComponent<VideoPlayer>();
                _videoPlayer.renderMode = VideoRenderMode.APIOnly;
                _videoPlayer.playOnAwake = false;
                _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            }
            return _videoPlayer;
        }
    }
    private static VideoPlayer _videoPlayer;

    private static ThumnailCreated thumbNailCreatedCallback;

    private static bool processInProgress;
    private static Queue<ThumbnailRequest> thumbnailQueue = new Queue<ThumbnailRequest>();

    /// <summary>
    /// Internal Function used only to consume enqueued requests
    /// </summary>
    private static void GetThumbnailFromVideo(ThumbnailRequest request)
    {
        if (request.ClipToProcess != null)
        {
            GetThumbnailFromVideo(request.ClipToProcess, request.callback, request.FrameToCapture);
        }
        else if (request.UrlToProcess != null)
        {
            GetThumbnailFromVideo(request.UrlToProcess, request.callback, request.FrameToCapture);
        }
    }

    /// <summary>
    /// Creates a thumbnail from the string video passed in. The thumbnail will be asynchronously passed to the callback function
    /// </summary>
    /// <param name="videoURL">The URL to load the video from</param>
    /// <param name="callback">Where to pass the created Sprite</param>
    public static void GetThumbnailFromVideo(string videoURL, ThumnailCreated callback, int frameToCapture = 0)
    {
        if (string.IsNullOrEmpty(videoURL))
        {
            Debug.LogWarning("Null videoURL detected. Unable to generate thumbnail.");
            return;
        }

        if (processInProgress)
        {
            thumbnailQueue.Enqueue(new ThumbnailRequest()
            {
                UrlToProcess = videoURL,
                FrameToCapture = frameToCapture,
                callback = callback
            });
            return;
        }
        processInProgress = true;
        thumbNailCreatedCallback = callback;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoURL;
        PrepareVideoForProcessing(frameToCapture);
    }

    /// <summary>
    /// Creates a thumbnail from the string video passed in. The thumbnail will be asynchronously passed to the callback function
    /// </summary>
    /// <param name="videoClip">The clip to take the screenshot from</param>
    /// <param name="callback">Where to pass the created Sprite</param>
    public static void GetThumbnailFromVideo(VideoClip videoClip, ThumnailCreated callback, int frameToCapture = 0)
    {
        if (videoClip == null)
        {
            Debug.LogWarning("Null videoURL detected. Unable to generate thumbnail.");
            return;
        }

        if (processInProgress)
        {
            thumbnailQueue.Enqueue(new ThumbnailRequest()
            {
                ClipToProcess = videoClip,
                FrameToCapture = frameToCapture,
                callback = callback
            });
            return;
        }
        processInProgress = true;

        thumbNailCreatedCallback = callback;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        PrepareVideoForProcessing(frameToCapture);
    }

    private static void PrepareVideoForProcessing(int frameToCapture)
    {
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady += ThumbnailReady;
        videoPlayer.frame = frameToCapture;
        // TODO This is bugged in Unity, check in the future if we can remove play and still recieve the frame with just .frame and Pause()
        videoPlayer.Play();
        videoPlayer.Pause();
    }

    private static void ThumbnailReady(VideoPlayer source, long frameIdx)
    {
        videoPlayer.sendFrameReadyEvents = false;
        videoPlayer.frameReady -= ThumbnailReady;
        Texture2D tex = new Texture2D(2, 2);
        RenderTexture renderTexture = source.texture as RenderTexture;
        if (tex.width != renderTexture.width || tex.height != renderTexture.height)
        {
            tex.Reinitialize(renderTexture.width, renderTexture.height);
        }

        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = currentActiveRT;
        renderTexture.Release();

        Sprite thumbnailSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

        // EyeTrackerManager.Instance.Sprites.Add(thumbnailSprite);
        thumbNailCreatedCallback?.Invoke(thumbnailSprite);

        processInProgress = false;
        if (thumbnailQueue.Count > 0)
        {
            GetThumbnailFromVideo(thumbnailQueue.Dequeue());
        }
    }
}
