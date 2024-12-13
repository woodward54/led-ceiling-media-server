using System;
using System.Runtime.InteropServices;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SocketResponse
{
    public uint size;
    public ulong sequence;
    public uint flashVersion;
    public double currentClock;
    public double oldestPacket;
    public double newestPacket;
    public double brightness;
    public double wifiSignal;
    public uint bufferSize;
    public uint bufferPos;
    public uint fpsDrawing;
    public uint watts;

    public void Reset()
    {
        size = 0;
        sequence = 0;
        flashVersion = 0;
        currentClock = 0;
        oldestPacket = 0;
        newestPacket = 0;
        brightness = 0;
        wifiSignal = 0;
        bufferSize = 0;
        bufferPos = 0;
        fpsDrawing = 0;
        watts = 0;
    }
};