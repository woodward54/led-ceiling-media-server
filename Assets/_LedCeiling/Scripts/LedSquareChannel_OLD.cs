// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net;
// using System.Net.Sockets;
// using System.Threading;
// using System.Threading.Tasks;
// using NightDriver;
// using UnityEngine;

// public class LedSquareChannel_OLD : MonoBehaviour, IDisposable
// {
//     public enum ConnectionState
//     {
//         Disconnected,
//         Connected,
//     }

//     [Header("Configuration")]
//     [SerializeField] private GameObject _deviceUiPrefab;
//     [SerializeField] private byte _channel = 0;
//     [SerializeField] private bool _compressData = true;

//     [Header("LED Configuration")]
//     [SerializeField] private uint _width = 480;
//     [SerializeField] private uint _height = 1;

//     [Header("Debug")]
//     [SerializeField] private bool _simulateNetworkIssues = false;

//     private const int BATCH_SIZE = 24;
//     private const double BATCH_TIMEOUT = 0.5;
//     private const int MAX_QUEUE_SIZE = 45;
//     private const ushort WIFI_COMMAND_PIXELDATA64 = 3;

//     private readonly Vector2Int _square2Offset = new Vector2Int(62, 62);

//     private readonly ConcurrentQueue<byte[]> _dataQueue = new();
//     private readonly CancellationTokenSource _cancellationToken = new();

//     private CRGB[] _mainLEDs;
//     private UIDevice _myUi;
//     private DateTime _lastBatchTime = DateTime.UtcNow;
//     private DateTime _lastFrameTime = DateTime.UtcNow;
//     private bool _isDisposed;

//     // Network
//     private float _checkSocketInterval = 5f;
//     private int _serverPort = 49152;

//     private TcpClient _tcpClient;
//     private NetworkStream _networkStream;
//     private bool _isConnected = false;

//     public string HostName { get; set; }
//     public uint FramesPerSecond { get; set; }
//     public Vector2Int OffsetPosition { get; set; }
//     public LedSquare SquareData { get; set; }

//     private ConnectionState _connectionStatusInternal;
//     public ConnectionState ConnectionStatus
//     {
//         get => _connectionStatusInternal;
//         set
//         {
//             _isConnected = value == ConnectionState.Connected;
//             _connectionStatusInternal = value;
//             if (_myUi != null)
//             {
//                 _myUi.Status = value;
//                 if (value == ConnectionState.Disconnected)
//                 {
//                     _myUi.Ip = "";
//                 }
//             }
//         }
//     }

//     protected virtual void Start()
//     {
//         ConnectionStatus = ConnectionState.Disconnected;

//         InitializeLEDs();
//         InitializeUI();

//         ConnectToClient();

//         if (Application.isPlaying)
//         {
//             StartTasks();
//         }
//     }

//     private void InitializeLEDs()
//     {
//         _mainLEDs = new CRGB[_width * _height];
//         for (int i = 0; i < _mainLEDs.Length; i++)
//         {
//             _mainLEDs[i] = new CRGB(0, 0, 0);
//         }
//     }

//     private void InitializeUI()
//     {
//         var obj = Instantiate(_deviceUiPrefab);
//         _myUi = obj.GetComponent<UIDevice>();
//         _myUi.name = HostName;
//         _myUi.Setup(SquareData, HostName, ConnectionStatus);
//     }

//     private void StartTasks()
//     {
//         Task.Run(SendLoopAsync);
//     }

//     private void ConnectToClient()
//     {
//         if (IsConnected())
//             return;

//         try
//         {
//             CloseSocket();

//             var hostEntry = Dns.GetHostEntry(HostName);
//             if (hostEntry.AddressList.Length == 0)
//             {
//                 Debug.LogWarning("No IP address found for " + HostName);
//                 HandleConnectionError();
//                 return;
//             }

//             var ipAddress = hostEntry.AddressList[0];

//             _tcpClient = new TcpClient();
//             _tcpClient.Connect(ipAddress, _serverPort);
//             _networkStream = _tcpClient.GetStream();

//             _isConnected = true;
//             ConnectionStatus = ConnectionState.Connected;
//             _myUi.Ip = ipAddress.ToString();

//             Debug.Log($"Connected to {HostName}");
//         }
//         catch (Exception ex)
//         {
//             Debug.LogWarning($"Failed to connect to {HostName}: {ex}");
//             HandleConnectionError();
//         }
//     }

//     private async Task SendLoopAsync()
//     {
//         var retryPolicy = new ExponentialBackoff(
//             initialDelay: TimeSpan.FromMilliseconds(100),
//             maxDelay: TimeSpan.FromSeconds(5));

//         while (!_cancellationToken.IsCancellationRequested)
//         {
//             if (_isConnected)
//             {
//                 try
//                 {
//                     await ProcessBatchSendAsync(_cancellationToken.Token);
//                 }
//                 catch (Exception ex)
//                 {
//                     _isConnected = false;
//                     await UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
//                     {
//                         Debug.LogError($"Failed to send data to {HostName}: {ex}");
//                         HandleConnectionError();
//                     });
//                     await retryPolicy.DelayAsync(_cancellationToken.Token);
//                 }
//             }
//             else
//             {
//                 await Task.Delay(100, _cancellationToken.Token);
//             }
//         }
//     }

//     protected virtual void Update()
//     {
//         var pixels32 = LedSquareManger.Instance.Pixels32;
//         var halfLedCount = _width * _height / 2;

//         for (int p = 0; p < halfLedCount; p++)
//         {
//             var localPosSquare1 = LedSquareUtils.LedIndexToXY(p, OffsetPosition, true);
//             var localPosSquare2 = LedSquareUtils.LedIndexToXY(p, OffsetPosition + _square2Offset, false);

//             var index1 = (localPosSquare1.y * 620) + localPosSquare1.x;
//             var index2 = (localPosSquare2.y * 620) + localPosSquare2.x;

//             var color1 = pixels32[index1];
//             var color2 = pixels32[index2];

//             var brightness = SettingsManager.Instance.Brightness;

//             var crgb1 = new CRGB();
//             var crgb2 = new CRGB();

//             crgb1.setRGB((byte)(color1.r * brightness), (byte)(color1.g * brightness), (byte)(color1.b * brightness));
//             crgb2.setRGB((byte)(color2.r * brightness), (byte)(color2.g * brightness), (byte)(color2.b * brightness));

//             _mainLEDs[p] = crgb1;
//             _mainLEDs[p + halfLedCount] = crgb2;
//         }

//         ProcessFrame();
//     }

//     // private async void DrawLoopAsync(CancellationToken cancellationToken)
//     // {
//     //     while (!cancellationToken.IsCancellationRequested)
//     //     {
//     //         var timeStart = DateTime.UtcNow;
//     //         ProcessFrame(timeStart);

//     //         var frameTime = 1000.0f / FramesPerSecond;
//     //         var delay = TimeSpan.FromMilliseconds(frameTime) - (DateTime.UtcNow - timeStart);
//     //         if (delay > TimeSpan.Zero)
//     //         {
//     //             await Task.Delay(delay, cancellationToken);
//     //         }
//     //     }
//     // }

//     private void ProcessFrame()
//     {
//         if (_dataQueue.Count > MAX_QUEUE_SIZE)
//         {
//             return;
//         }

//         var timeStart = DateTime.UtcNow;

//         var frameTime = 1000.0f / FramesPerSecond;
//         if (timeStart - _lastFrameTime < TimeSpan.FromMilliseconds(frameTime)) return;

//         _lastFrameTime = timeStart;

//         double epoch = (timeStart.Ticks - 621355968000000000 + (1.0 * TimeSpan.TicksPerSecond)) / (double)TimeSpan.TicksPerSecond;
//         double fraction = epoch - (long)epoch;

//         ulong seconds = (ulong)epoch;
//         ulong uSeconds = (ulong)(fraction * 1000000);

//         var data = GetPixelData(_mainLEDs);

//         var packet = LEDInterop.CombineByteArrays(
//             LEDInterop.WORDToBytes(WIFI_COMMAND_PIXELDATA64),
//             LEDInterop.WORDToBytes(_channel),
//             LEDInterop.DWORDToBytes((uint)data.Length / 3),
//             LEDInterop.ULONGToBytes(seconds),
//             LEDInterop.ULONGToBytes(uSeconds),
//             data
//         );

//         if (_compressData)
//         {
//             var compressed = CompressFrame(packet);
//             if (packet.Length > compressed.Length)
//             {
//                 packet = compressed;
//             }
//         }

//         _dataQueue.Enqueue(packet);
//     }

//     private async Task ProcessBatchSendAsync(CancellationToken cancellationToken)
//     {
//         if (!ShouldSendBatch())
//         {
//             await Task.Delay(10, cancellationToken);
//             return;
//         }

//         var messages = DequeueMessages();

//         await SendMessagesAsync(messages);
//     }

//     private byte[] GetPixelData(CRGB[] leds)
//     {
//         return LEDInterop.GetColorBytes(leds);
//     }

//     private bool ShouldSendBatch()
//     {
//         if (_dataQueue.Count == 0) return false;
//         if (_dataQueue.Count >= BATCH_SIZE) return true;

//         var timeSinceLastBatch = DateTime.UtcNow - _lastBatchTime;
//         return timeSinceLastBatch.TotalSeconds >= BATCH_TIMEOUT;
//     }

//     private byte[][] DequeueMessages()
//     {
//         var messages = new List<byte[]>();
//         while (messages.Count < BATCH_SIZE && _dataQueue.TryDequeue(out var message))
//         {
//             messages.Add(message);
//         }
//         return messages.ToArray();
//     }

//     private async Task SendMessagesAsync(byte[][] messages)
//     {
//         if (messages.Length == 0) return;

//         if (!IsConnected())
//         {
//             return;
//         }

//         _lastBatchTime = DateTime.UtcNow;

//         try
//         {
//             byte[] msgs = LEDInterop.CombineByteArrays(messages);

//             await _networkStream.WriteAsync(msgs, 0, msgs.Length);
//             await _networkStream.FlushAsync();

//             // double framesPerSecond = (double)(DateTime.UtcNow - _lastBatchTime).TotalSeconds;
//             // Debug.Log("Sent " + msgs.Length + ", FPS: " + framesPerSecond);
//         }
//         catch (Exception ex)
//         {
//             await UnityMainThreadDispatcher.Instance.EnqueueAsync(() =>
//             {
//                 Debug.LogError($"Failed to send data 2 to {HostName}: {ex}");

//             });
//         }
//     }

//     private byte[] CompressFrame(byte[] data)
//     {
//         const int COMPRESSED_HEADER_TAG = 0x44415645;       // Magic "DAVE" tag for compressed data - replaces size field
//         byte[] compressedData = LEDInterop.Compress(data);
//         byte[] compressedFrame = LEDInterop.CombineByteArrays(LEDInterop.DWORDToBytes(COMPRESSED_HEADER_TAG),
//                                                               LEDInterop.DWORDToBytes((uint)compressedData.Length),
//                                                               LEDInterop.DWORDToBytes((uint)data.Length),
//                                                               LEDInterop.DWORDToBytes(0x12345678),
//                                                               compressedData);
//         return compressedFrame;
//     }

//     private bool IsConnected()
//     {
//         return _isConnected && _networkStream != null && _tcpClient != null && _tcpClient.Connected;
//     }

//     private void HandleConnectionError()
//     {
//         ConnectionStatus = ConnectionState.Disconnected;

//         CloseSocket();
//     }

//     private void CloseSocket()
//     {
//         if (_networkStream != null)
//         {
//             _networkStream.Close();
//             _networkStream = null;
//         }

//         if (_tcpClient != null)
//         {
//             _tcpClient.Close();
//             _tcpClient = null;
//         }
//     }

//     protected virtual void OnDestroy()
//     {
//         Dispose();
//     }

//     public void Dispose()
//     {
//         if (_isDisposed) return;

//         _cancellationToken.Cancel();

//         CloseSocket();

//         _cancellationToken.Dispose();
//         _isDisposed = true;

//         GC.SuppressFinalize(this);
//     }

//     // Helper classes

//     private class ExponentialBackoff
//     {
//         private readonly TimeSpan _initialDelay;
//         private readonly TimeSpan _maxDelay;
//         private int _attempt;

//         public ExponentialBackoff(TimeSpan initialDelay, TimeSpan maxDelay)
//         {
//             _initialDelay = initialDelay;
//             _maxDelay = maxDelay;
//         }

//         public async Task DelayAsync(CancellationToken cancellationToken)
//         {
//             var delay = TimeSpan.FromMilliseconds(
//                 Math.Min(
//                     _initialDelay.TotalMilliseconds * Math.Pow(2, _attempt),
//                     _maxDelay.TotalMilliseconds
//                 )
//             );
//             _attempt++;
//             await Task.Delay(delay, cancellationToken);
//         }
//     }
// }