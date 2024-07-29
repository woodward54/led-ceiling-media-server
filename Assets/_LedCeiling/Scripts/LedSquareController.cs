using System;
using System.Threading;
using NightDriver;
using UnityEngine;
using UnityEngine.Profiling;

public class LedSquareController : LEDControllerChannel
{
    public Vector2Int OffsetPosition;

    public const uint FramesPerBuffer = 80;              // How many buffer frames the chips have
    public const double PercentBufferUse = 0.80;            // How much of the buffer we should use up

    public double TimeOffset
    {
        get
        {
            if (0 == FramesPerSecond)                  // No speed indication yet, can't guess at offset, assume 1 second for now
                return 1.0;

            if (!Supports64BitClock)                            // Old V001 flash is locked at 22 fps
                return 1.0;
            else
                return (double)FramesPerBuffer / FramesPerSecond * PercentBufferUse;
        }
    }

    const UInt16 WIFI_COMMAND_PIXELDATA64 = 3;

    protected override void Start()
    {
        base.Start();

        // Create the UI
        var obj = Instantiate(_deviceUiPrefab);
        _myUi = obj.GetComponent<UIDevice>();
        _myUi.name = HostName;
        _myUi.OffsetPosition = OffsetPosition;

        _myUi.Setup(SquareData, HostName, UIDevice.ConnectedStatus.Disconnected);
    }

    Vector2Int _square2Offset = new Vector2Int(62, 62);
    protected override void Update()
    {
        base.Update();

        var pixels32 = LedSquareManger.Instance.Pixels32;

        var halfLedCount = Width * Height / 2; // 240
        for (int p = 0; p < halfLedCount; p++)
        {
            var localPosSquare1 = LedSquareUtils.LedIndexToXY(p, OffsetPosition, true);
            var localPosSquare2 = LedSquareUtils.LedIndexToXY(p, OffsetPosition + _square2Offset, false);

            var index1 = (localPosSquare1.y * 620) + localPosSquare1.x;
            var index2 = (localPosSquare2.y * 620) + localPosSquare2.x;

            var color1 = pixels32[index1];
            var color2 = pixels32[index2];

            var b = SettingsManager.Instance.Brightness;

            // Profiler.BeginSample("setRGB");

            var _crgb1 = new CRGB();
            var _crgb2 = new CRGB();

            _crgb1.setRGB((byte)(color1.r * b), (byte)(color1.g * b), (byte)(color1.b * b));
            _crgb2.setRGB((byte)(color2.r * b), (byte)(color2.g * b), (byte)(color2.b * b));

            MainLEDs[p] = _crgb1;
            MainLEDs[p + halfLedCount] = _crgb2;

            // Profiler.EndSample();
        }
    }

    private byte[] GetPixelData(CRGB[] MainLEDs)
    {
        return LEDInterop.GetColorBytes(MainLEDs);
    }

    protected override byte[] GetDataFrame(CRGB[] MainLEDs, DateTime timeStart)
    {
        // The old original code truncated 64 bit values down to 32, and we need to fix that, so it's a in a packet called PIXELDATA64
        // and is only sent to newer flashes taht support it.  Otherwise we send the old original foramt.


        // The timeOffset is how far in the future frames are generated for.  If the chips have a 2 second buffer, you could
        // go up to 2 seconds, but I shoot for the middle of the buffer depth.  Right now it's calculated as using 

        double epoch = (timeStart.Ticks - 621355968000000000 + (1.0 * TimeSpan.TicksPerSecond)) / (double)TimeSpan.TicksPerSecond;
        double fraction = epoch - (Int64)epoch;

        ulong seconds = (ulong)epoch;                                       // Whole part of time number (left of the decimal point)
        ulong uSeconds = (ulong)(fraction * 1000000);           // Fractional part of time (right of the decimal point)

        // Debug.Log("New Seconds: " + seconds + "uSec: " + uSeconds);

        var data = GetPixelData(MainLEDs);

        return LEDInterop.CombineByteArrays(LEDInterop.WORDToBytes(WIFI_COMMAND_PIXELDATA64),      // Offset, always zero for us
                                            LEDInterop.WORDToBytes((UInt16)Channel),               // LED channel on ESP32
                                            LEDInterop.DWORDToBytes((UInt32)data.Length / 3),      // Number of LEDs
                                            LEDInterop.ULONGToBytes(seconds),                      // Timestamp seconds (64 bit)
                                            LEDInterop.ULONGToBytes(uSeconds),                     // Timestmap microseconds (64 bit)
                                            data);                                                 // Color Data
    }
};


