using System;
using NightDriver;
using UnityEngine;

public class LedFrameProcessor
{
    private const ushort WIFI_COMMAND_PIXELDATA64 = 3;
    private const int COMPRESSED_HEADER_TAG = 0x44415645;

    private readonly LedSquareConfiguration _config;
    private readonly Vector2Int _square2Offset;

    // The timeOffset is how far in the future frames are generated for.  If the chips have a 2 second buffer, you could
    // set this to 2 seconds.  This is used to generate frames that are 2 seconds in the future.
    private double _frameTimeOffset;

    public LedFrameProcessor(LedSquareConfiguration config, uint framesPerSecond)
    {
        _config = config;
        _square2Offset = new Vector2Int(62, 62);
        // _frameTimeOffset = _config.FramesPerBuffer * _config.PercentBufferUse / framesPerSecond;
        // _frameTimeOffset = 2.0; // 24fps * 2 = 48 buffers

        _frameTimeOffset = 2.0; // 15 * 3 = 45 buffers
    }

    public byte[] ProcessFrame(in Color32[] sourcePixels, Vector2Int offsetPosition, float brightness)
    {
        var ledData = CreateLedFrame(sourcePixels, offsetPosition, brightness);
        return CreateFramePacket(ledData);
    }

    private LedFrame CreateLedFrame(in Color32[] sourcePixels, Vector2Int offsetPosition, float brightness)
    {
        var pixels = new CRGB[_config.Width * _config.Height];
        var halfLedCount = _config.Width * _config.Height / 2;

        for (int p = 0; p < halfLedCount; p++)
        {
            ProcessPixelPair(p, sourcePixels, offsetPosition, brightness, pixels);
        }

        return new LedFrame(pixels, _config.Width, _config.Height, LedSquareManger.FrameTimestamp);
    }

    private void ProcessPixelPair(int index, in Color32[] sourcePixels, Vector2Int offsetPosition, float brightness, CRGB[] result)
    {
        var localPosSquare1 = LedSquareUtils.LedIndexToXY(index, offsetPosition, true);
        var localPosSquare2 = LedSquareUtils.LedIndexToXY(index, offsetPosition + _square2Offset, false);

        var index1 = (localPosSquare1.y * 620) + localPosSquare1.x;
        var index2 = (localPosSquare2.y * 620) + localPosSquare2.x;

        var color1 = sourcePixels[index1];
        var color2 = sourcePixels[index2];

        result[index] = CreateCRGB(color1, brightness);
        result[index + (_config.Width * _config.Height / 2)] = CreateCRGB(color2, brightness);
    }

    private CRGB CreateCRGB(Color32 color, float brightness)
    {
        var crgb = new CRGB();
        crgb.setRGB(
            (byte)(color.r * brightness),
            (byte)(color.g * brightness),
            (byte)(color.b * brightness)
        );
        return crgb;
    }

    private byte[] CreateFramePacket(LedFrame ledData)
    {
        var (seconds, microseconds) = GetEpochTime(ledData.Timestamp);
        var pixelData = LEDInterop.GetColorBytes(ledData.Pixels);

        var packet = LEDInterop.CombineByteArrays(
            LEDInterop.WORDToBytes(WIFI_COMMAND_PIXELDATA64),
            LEDInterop.WORDToBytes(_config.Channel),
            LEDInterop.DWORDToBytes((uint)pixelData.Length / 3),
            LEDInterop.ULONGToBytes(seconds),
            LEDInterop.ULONGToBytes(microseconds),
            pixelData
        );

        if (_config.CompressData)
        {
            var compressed = CompressFrame(packet);
            if (packet.Length > compressed.Length)
            {
                packet = compressed;
            }
        }

        return packet;
    }

    private byte[] CompressFrame(byte[] data)
    {
        byte[] compressedData = LEDInterop.Compress(data);
        return LEDInterop.CombineByteArrays(
            LEDInterop.DWORDToBytes(COMPRESSED_HEADER_TAG),
            LEDInterop.DWORDToBytes((uint)compressedData.Length),
            LEDInterop.DWORDToBytes((uint)data.Length),
            LEDInterop.DWORDToBytes(0x12345678),
            compressedData
        );
    }

    private (ulong seconds, ulong microseconds) GetEpochTime(DateTime timeStart)
    {
        double epoch = (timeStart.Ticks - DateTime.UnixEpoch.Ticks + _frameTimeOffset * TimeSpan.TicksPerSecond) / TimeSpan.TicksPerSecond;

        ulong seconds = (ulong)epoch;                                       // Whole part of time number (left of the decimal point)
        ulong microseconds = (ulong)((epoch - (int)epoch) * 1000000);           // Fractional part of time (right of the decimal point)

        return (seconds, microseconds);
    }
}
