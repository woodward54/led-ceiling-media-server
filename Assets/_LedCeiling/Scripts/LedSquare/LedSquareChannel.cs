using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NightDriver;
using UnityEngine;

public class LedSquareChannel : MonoBehaviour
{
    [SerializeField] private GameObject _deviceUiPrefab;

    public ConnectionState CurrentState { get; private set; } = ConnectionState.Disconnected;
    private ConnectionState _previousState = ConnectionState.Disconnected;

    private LedSquareConfiguration _config;
    private LedFrameProcessor _frameProcessor;
    private UIDevice _uiDevice;
    private DateTime _lastFrameEnqueuedAt = DateTime.UtcNow;
    private DateTime _lastBatchSentAt = DateTime.UtcNow;
    private float _framePeriodMs;

    private string _hostName;
    private uint _framesPerSecond;
    private Vector2Int _offsetPosition;
    private LedSquare _squareData;
    private CancellationTokenSource _cancellationToken = new CancellationTokenSource();

    private static ConcurrentDictionary<string, LedControllerSocket> _hostControllerSockets = new();
    private readonly ConcurrentQueue<byte[]> _frameSendQueue = new();
    public readonly ConcurrentQueue<(LogType, string)> DebugMsgQueue = new();

    public SocketResponse Response;
    public int SendQueueCountDebug;

    public ulong LocalSequence = 0;

    public static LedControllerSocket ControllerSocketForHost(string host)
    {
        if (_hostControllerSockets.ContainsKey(host))
        {
            _hostControllerSockets.TryGetValue(host, out LedControllerSocket controller);
            return controller;
        }
        return null;
    }

    public void Setup(string hostName, uint framesPerSecond, Vector2Int offsetPosition, LedSquare squareData)
    {
        _hostName = hostName;
        _framesPerSecond = framesPerSecond;
        _offsetPosition = offsetPosition;
        _squareData = squareData;

        _framePeriodMs = 1000.0f / _framesPerSecond;

        InitializeComponents();

        if (Application.isPlaying)
        {
            // Draw loop
            Task.Run(WorkerDrawLoop);

            // Send loop
            Task.Run(() => WorkerConnectAndSendLoop());
        }
    }

    private void InitializeComponents()
    {
        _config = new LedSquareConfiguration();
        _frameProcessor = new LedFrameProcessor(_config, _framesPerSecond);

        InitializeUI();
    }

    private void InitializeUI()
    {
        var obj = Instantiate(_deviceUiPrefab);
        _uiDevice = obj.GetComponent<UIDevice>();
        _uiDevice.name = _hostName;
        _uiDevice.Setup(_squareData, _hostName, ConnectionState.Disconnected);
    }

    // Worker thread, No UnityAPI here
    private async void WorkerConnectAndSendLoop()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            LedControllerSocket controllerSocket
                = _hostControllerSockets.GetOrAdd(_hostName, (hostname) =>
                {
                    return new LedControllerSocket(hostname, this, _config);
                });

            if (controllerSocket == null || controllerSocket.IsDead)
            {
                CurrentState = ConnectionState.Disconnected;

                DisposeControllerSocket();
                await Task.Delay(_config.ReconnectDelay, cancellationToken: _cancellationToken.Token);
                continue;
            }

            if (controllerSocket.EnsureConnected() == false)
            {
                if (controllerSocket.IsDead)
                {
                    DebugMsgQueue.Enqueue((LogType.Log, "Closing disconnected socket"));
                    DisposeControllerSocket();
                }

                CurrentState = ConnectionState.Disconnected;

                await Task.Delay(10, cancellationToken: _cancellationToken.Token);
                continue;
            }

            // Connected
            CurrentState = ConnectionState.Connected;

            if (ShouldSendBatch())
            {
                _lastBatchSentAt = DateTime.UtcNow;

                var messages = _frameSendQueue.DequeueChunk(_frameSendQueue.Count()).ToArray();
                byte[] combinedData = LEDInterop.CombineByteArrays(messages);

                if (combinedData.Length == 0) continue;

                // DebugMsgQueue.Enqueue((LogType.Log, "Sending " + messages.Length + " frames. " + combinedData.Length + " bytes"));

                try
                {
                    // TODO: timeout, SendData is always returning even if the send fails

                    var bytesSet = controllerSocket.SendData(combinedData, ref Response);
                    if (bytesSet != combinedData.Length)
                    {
                        DebugMsgQueue.Enqueue((LogType.Error, "Could not write all bytes. Closing socket."));
                        DisposeControllerSocket();
                        continue;
                    }
                }
                catch (SocketException ex)
                {
                    DebugMsgQueue.Enqueue((LogType.Error, "Exception writing to socket: " + ex.Message));
                    DisposeControllerSocket();
                }
            }

            await Task.Delay(5, cancellationToken: _cancellationToken.Token);
        }
    }

    private bool ShouldSendBatch()
    {
        return _frameSendQueue.Count >= _config.BatchSize;
        
        if (_frameSendQueue.Count == 0) return false;
        if (_frameSendQueue.Count >= _config.BatchSize) return true;
        return (DateTime.UtcNow - _lastBatchSentAt).TotalSeconds >= _config.BatchTimeout;
    }

    // Worker thread, No UnityAPI here
    private async void WorkerDrawLoop()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!ShouldProcessFrame())
                {
                    await Task.Delay(10, cancellationToken: _cancellationToken.Token);
                    continue;
                }

                // These scripts are still initializing
                if (LedSquareManger.Pixels32 == null)
                {
                    await Task.Delay(10, cancellationToken: _cancellationToken.Token);
                    continue;
                }

                var pixels = LedSquareManger.Pixels32;
                var frameData = _frameProcessor.ProcessFrame(
                    pixels,
                    _offsetPosition,
                    SettingsManager.Brightness
                );

                EnqueueFrame(frameData);

                await Task.Delay(10, cancellationToken: _cancellationToken.Token);
            }
            catch (Exception ex)
            {
                DebugMsgQueue.Enqueue((LogType.Error, "Error in WorkerDrawLoop: " + ex.Message));
                await Task.Delay(10, cancellationToken: _cancellationToken.Token);
                continue;
            }
        }
    }

    private void DisposeControllerSocket()
    {
        if (_hostControllerSockets.TryRemove(_hostName, out LedControllerSocket removedSocket))
        {
            try
            {
                removedSocket?.Dispose();
            }
            catch (Exception ex)
            {
                DebugMsgQueue.Enqueue((LogType.Error, $"Error disposing socket for {_hostName}: {ex.Message}"));
            }
        }
    }

    private void Update()
    {
        SendQueueCountDebug = _frameSendQueue.Count;

        if (CurrentState != _previousState)
        {
            UpdateConnectionState(CurrentState);
            _previousState = CurrentState;
        }

        if (DebugMsgQueue.TryDequeue(out (LogType, string) msg))
        {
            Log(msg.Item1, msg.Item2);
        }
    }

    private void EnqueueFrame(byte[] frame)
    {
        while (_frameSendQueue.Count >= _config.MaxFrameQueueSize)
        {
            // Dequeue and discard stale frames
            if (_frameSendQueue.TryDequeue(out _))
            {
                // DebugMsgQueue.Enqueue((LogType.Warning, "Removing stale frame from queue due to size limit."));
            }
        }

        _lastFrameEnqueuedAt = DateTime.UtcNow;
        _frameSendQueue.Enqueue(frame);
    }

    private bool ShouldProcessFrame()
    {
        // if (_framesPerSecond <= 0) return false;
        return (DateTime.UtcNow - _lastFrameEnqueuedAt).TotalMilliseconds >= _framePeriodMs;
    }

    private void UpdateConnectionState(ConnectionState newState)
    {
        CurrentState = newState;

        if (_uiDevice != null)
        {
            _uiDevice.Status = newState;

            _uiDevice.Ip = ControllerSocketForHost(_hostName)?.IpAddress?.ToString() ?? "";
        }
    }

    public void Log(LogType logType, string error)
    {
        Debug.unityLogger.Log(logType, $"[{_hostName}] {error}");
    }

    private async void OnDestroy()
    {
        // _networkManager.OnConnectionStateChanged -= HandleConnectionStateChanged;
        // _networkManager.OnError -= HandleError;

        _cancellationToken.Cancel();

        ControllerSocketForHost(_hostName)?.Dispose();

        _cancellationToken.Dispose();
    }
}