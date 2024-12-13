using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using NightDriver;
using UnityEngine;

public class LedControllerSocket : IDisposable
{
    private readonly string _hostName;
    private readonly LedSquareConfiguration _config;
    private TcpClient _tcpClient;
    private CancellationTokenSource _cancellationToken;
    private UIDevice _uiDevice;
    private LedSquareChannel _channel;

    private uint _bytesSentSinceFrame = 0;
    private DateTime _lastDataFrameTime;

    public IPAddress IpAddress => _ipAddress;
    private IPAddress _ipAddress;
    private IPEndPoint _remoteEP;

    public bool IsDead { get; private set; }

    public LedControllerSocket(string hostName, LedSquareChannel channel, LedSquareConfiguration config)
    {
        _hostName = hostName;
        _channel = channel;
        _config = config;
        _cancellationToken = new CancellationTokenSource();

        Dns.BeginGetHostAddresses(_hostName, OnDnsGetHostAddressesComplete, this);
    }

    private void OnDnsGetHostAddressesComplete(IAsyncResult result)
    {
        var This = (LedControllerSocket)result.AsyncState;

        try
        {
            This._ipAddress = Dns.EndGetHostAddresses(result)[0];
            This._remoteEP = new IPEndPoint(This._ipAddress, _config.ServerPort);
        }
        catch (Exception e)
        {
            This._channel.DebugMsgQueue.Enqueue((LogType.Error, "DNS Exception: can not resolve hostname. " + e.Message));
            IsDead = true;
        }
    }

    // If not already connected, initiates the connection so that perhaps next time we will ideally be connected
    public bool EnsureConnected()
    {
        if (IsDead == true)
            return false;

        if (_remoteEP == null)
            return false;

        if (_tcpClient != null && _tcpClient.Connected)
            return true;

        try
        {
            if (DateTime.UtcNow - _lastDataFrameTime < _config.ReconnectDelay)
            {
                //ConsoleApp.Stats.WriteLine("Bailing connection as too early!");
                return false;
            }

            _lastDataFrameTime = DateTime.UtcNow;
            _tcpClient = new TcpClient();

            _tcpClient.Connect(_remoteEP);

            _channel.DebugMsgQueue.Enqueue((LogType.Log, "Connected to " + _hostName));

            return true;
        }
        catch (SocketException)
        {
            IsDead = true;
            return false;
        }
    }

    unsafe public uint SendData(in byte[] data, ref SocketResponse response)
    {
        try
        {
            uint bytesSent = (uint)_tcpClient.Client.Send(data);
            if (bytesSent != data.Length)
            {
                _channel.DebugMsgQueue.Enqueue((LogType.Error, $"Failed to send batch: {bytesSent} bytes sent, {data.Length} bytes expected"));
                IsDead = true;
                return bytesSent;
            }

            TimeSpan timeSinceLastSend = DateTime.UtcNow - _lastDataFrameTime;
            if (timeSinceLastSend > TimeSpan.FromSeconds(10.0))
            {
                _lastDataFrameTime = DateTime.UtcNow;
                _bytesSentSinceFrame = 0;
            }
            else
            {
                _bytesSentSinceFrame += bytesSent;
            }

            DateTime startWaiting = DateTime.UtcNow;
            // Receive the response back from the socket we just sent to
            int cbToRead = sizeof(SocketResponse);
            byte[] buffer = new byte[cbToRead];

            // Wait until there's enough data to process or we've waited 5 seconds with no result

            //while (DateTime.UtcNow - startWaiting > TimeSpan.FromSeconds(5) && _socket.Available < cbToRead)
            //    Thread.Sleep(100);

            while (_tcpClient.Client.Available >= cbToRead)
            {
                var readBytes = _tcpClient.Client.Receive(buffer, cbToRead, SocketFlags.None);
                if (readBytes >= sizeof(SocketResponse) && buffer[0] >= sizeof(SocketResponse))
                {
                    GCHandle pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    nint pointer = pinnedArray.AddrOfPinnedObject();
                    response = Marshal.PtrToStructure<SocketResponse>(pointer);
                    pinnedArray.Free();
                }
                // FirmwareVersion = "v" + response.flashVersion;
            }

            return bytesSent;
        }
        catch (Exception)
        {
            IsDead = true;
            return 0;
        }
    }

    public void Dispose()
    {
        _cancellationToken.Cancel();

        if (_tcpClient != null)
        {
            _tcpClient.Dispose();
            _tcpClient = null;
        }

        _cancellationToken.Dispose();
    }
}