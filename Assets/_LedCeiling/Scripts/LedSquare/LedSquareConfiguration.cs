using System;

public class LedSquareConfiguration
{
    public uint Width { get; }
    public uint Height { get; }
    public byte Channel { get; }
    public bool CompressData { get; }
    public int BatchSize { get; }
    public double BatchTimeout { get; }
    public int MaxFrameQueueSize { get; }
    public int ServerPort { get; }
    public TimeSpan ReconnectDelay { get; }
    public TimeSpan SendTimeout { get; }

    public uint FramesPerSecond;

    public LedSquareConfiguration(
        uint width = 480,
        uint height = 1,
        byte channel = 0,
        bool compressData = true,
        int batchSize = 1,
        double batchTimeout = 10,
        int maxFrameQueueSize = 50,
        int serverPort = 49152)
    {
        Width = width;
        Height = height;
        Channel = channel;
        CompressData = compressData;
        BatchSize = batchSize;
        BatchTimeout = batchTimeout;
        MaxFrameQueueSize = maxFrameQueueSize;
        ServerPort = serverPort;
        ReconnectDelay = TimeSpan.FromSeconds(0.25);
        SendTimeout = TimeSpan.FromSeconds(2);
    }
}
