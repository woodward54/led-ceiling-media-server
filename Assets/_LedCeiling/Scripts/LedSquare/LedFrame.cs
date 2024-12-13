using System;
using NightDriver;

public readonly struct LedFrame
{
    public readonly CRGB[] Pixels { get; }
    public readonly uint Width { get; }
    public readonly uint Height { get; }
    public readonly DateTime Timestamp { get; }

    public LedFrame(CRGB[] pixels, uint width, uint height, DateTime timestamp)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        Timestamp = timestamp;
    }
}