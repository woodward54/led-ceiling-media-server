//+--------------------------------------------------------------------------
//
// NightDriver.Net - (c) 2019 Dave Plummer.  All Rights Reserved.
//
// File:        LEDControllerChannel.cs
//
// NightDriverUnity - (c) 2021 Plummer's Software LLC.  All Rights Reserved.  
//
// This file is part of the NightDriver software project.
//
//    NightDriver is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//   
//    NightDriver is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//   
//    You should have received a copy of the GNU General Public License
//    along with Nightdriver.  It is normally found in copying.txt
//    If not, see <https://www.gnu.org/licenses/>.
// Description:
//
//   Represents a specific channel on a particular strip and exposes the
//   GraphicsBase class for drawing directly on it.  
//
//   Each instance has a worker thread that manages keeping the socket 
//   connected and sending it the data that has been queued up for it.
//
// History:     Jun-15-2019        Davepl      Created
//
//---------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using static UIDevice;


// LEDControllerChannel
//
// Exposes ILEDGraphics via the GraphicsBase baseclass
// Abstract until deriving class implements GetDataFrame()

namespace NightDriver
{
    public abstract class LEDControllerChannel : MonoBehaviour
    {
        [SerializeField] protected GameObject _deviceUiPrefab;

        public string HostName;

        public LedSquare SquareData;
        public bool CompressData = true;
        public byte Channel = 0;

        public uint Connects = 0;
        public uint Watts = 0;
        public uint Width = 480;
        public uint Height = 1;
        public uint FramesPerSecond = 24;
        public bool RedGreenSwap = false;

        public const int BatchSize = 24; // was 5
        public const double BatchTimeout = 0.5;

        private ConcurrentQueue<byte[]> DataQueue = new ConcurrentQueue<byte[]>();

        private Thread _SendWorker;
        private Thread _DrawWorker;

        protected CRGB[] MainLEDs;

        protected UIDevice _myUi;

        protected virtual void Start()
        {
            MainLEDs = new CRGB[Width * Height];
            for (int i = 0; i < Width * Height; i++)
                MainLEDs[i] = new CRGB(0, 0, 0);

            if (Application.isPlaying)
            {
                _SendWorker = new Thread(WorkerConnectAndSendLoop);
                _SendWorker.IsBackground = true;
                _SendWorker.Priority = System.Threading.ThreadPriority.Normal;
                _SendWorker.Start();

                _DrawWorker = new Thread(DrawLoop);
                _DrawWorker.IsBackground = true;
                _DrawWorker.Priority = System.Threading.ThreadPriority.Normal;
                _DrawWorker.Start();
            }
        }

        protected virtual void Update() { } // Children will implement Update and set MainLEDs

        public void OnDestroy()
        {
            if (Application.isPlaying)
            {
                _SendWorker.Abort();
                _DrawWorker.Abort();
            }
        }

        public int QueueDepth
        {
            get
            {
                return DataQueue.Count;
            }
        }

        public bool HasSocket
        {
            get
            {
                ControllerSocket controllerSocket = ControllerSocketForHost(HostName);
                if (null == controllerSocket || controllerSocket._socket == null)
                    return false;
                return true;
            }
        }

        public bool ReadyForData    // Is there a socket and is it connected to the chip?
        {
            get
            {
                ControllerSocket controllerSocket = ControllerSocketForHost(HostName);
                if (null == controllerSocket || controllerSocket._socket == null || !controllerSocket._socket.Connected)
                    return false;
                return true;
            }
        }

        public bool NeedsClockStream
        {
            get;
            set;
        } = false;

        public bool Supports64BitClock
        {
            get;
            set;
        } = true;


        public uint MinimumSpareTime => (uint)_HostControllerSockets.Min(controller => controller.Value.BytesPerSecond);

        public uint BytesPerSecond
        {
            get
            {
                ControllerSocket controllerSocket = ControllerSocketForHost(HostName);
                if (null == controllerSocket || controllerSocket._socket == null)
                    return 0;
                return controllerSocket.BytesPerSecond;
            }
        }

        public uint TotalBytesPerSecond => (uint)_HostControllerSockets.Sum(controller => controller.Value.BytesPerSecond);

        public ControllerSocket ControllerSocket
        {
            get
            {
                return ControllerSocketForHost(HostName);
            }
        }

        public static ControllerSocket ControllerSocketForHost(string host)
        {
            if (_HostControllerSockets.ContainsKey(host))
            {
                _HostControllerSockets.TryGetValue(host, out ControllerSocket controller);
                return controller;
            }
            return null;
        }

        protected abstract byte[] GetDataFrame(CRGB[] MainLEDs, DateTime timeStart);

        protected virtual byte[] GetClockFrame(DateTime timeStart)
        {
            // The timeOffset is how far in the future frames are generated for.  If the chips have a 2 second buffer, you could
            // go up to 2 seconds, but I shoot for the middle of the buffer depth.  

            double epoch = (timeStart.Ticks - 621355968000000000) / (double)TimeSpan.TicksPerSecond;
            UInt64 seconds = (UInt64)epoch;                                      // Whole part of time number (left of the decimal point)
            UInt64 uSeconds = (UInt64)((epoch - (UInt64)epoch) * 1000000);           // Fractional part of time (right of the decimal point)
            return LEDInterop.CombineByteArrays(LEDInterop.WORDToBytes((UInt16)2),             // Command, which is 2 for us
                                                LEDInterop.WORDToBytes((UInt16)0),             // LED channel on ESP32
                                                LEDInterop.ULONGToBytes(seconds),      // Number of LEDs
                                                LEDInterop.ULONGToBytes(uSeconds)      // Timestamp seconds
                                                );                                             // Color Data
        }

        byte[] CompressFrame(byte[] data)
        {
            const int COMPRESSED_HEADER_TAG = 0x44415645;       // Magic "DAVE" tag for compressed data - replaces size field
            byte[] compressedData = LEDInterop.Compress(data);
            byte[] compressedFrame = LEDInterop.CombineByteArrays(LEDInterop.DWORDToBytes((uint)COMPRESSED_HEADER_TAG),
                                                                  LEDInterop.DWORDToBytes((uint)compressedData.Length),
                                                                  LEDInterop.DWORDToBytes((uint)data.Length),
                                                                  LEDInterop.DWORDToBytes(0x12345678),
                                                                  compressedData);
            return compressedFrame;
        }

        DateTime _timeLastSend = DateTime.UtcNow;
        DateTime _lastBatchTime = DateTime.UtcNow;

        // _HostSockets
        //
        // We can only have one socket open per ESP32 chip per channel, so this concurrent dictionary keeps track of which sockets are
        // open to what chips so that the socket can be reused.  

        static ConcurrentDictionary<string, ControllerSocket> _HostControllerSockets = new ConcurrentDictionary<string, ControllerSocket>();

        private int _iPacketCount = 0;

        // https://files.sexyandfunny.com/mp4/usr_506ef5a36788d.mp4
        // https://www.youtube.com/watch?v=G3HUp7LH5ig

        public void DrawLoop()
        {
            for (; ; )
            {
                DateTime timeStart = DateTime.UtcNow;

                CompressAndEnqueueData(MainLEDs, timeStart);

                uint ms = (uint)(1000 * (1.0f / FramesPerSecond));
                TimeSpan alreadyElapsed = (DateTime.UtcNow - timeStart);
                TimeSpan delay = TimeSpan.FromMilliseconds(ms) - alreadyElapsed;
                if (delay.TotalMilliseconds > 0)
                    Thread.Sleep((int)delay.TotalMilliseconds);
            }
        }

        public void CompressAndEnqueueData(CRGB[] MainLEDs, DateTime timeStart)
        {
            if (DataQueue.Count > 20)
            {
                // ConsoleApp.Stats.WriteLine("Queue full so dicarding frame for " + HostName);
                return;
            }

            // If there is already a socket open, we will use that; otherwise, a new connection will be opened and if successful
            // it will be placed into the _HostSockets concurrent dictionary

            if (_HostControllerSockets.ContainsKey(HostName) == false && (DateTime.UtcNow - _timeLastSend).TotalSeconds < 2)
            {
                //ConsoleApp.Stats.WriteLine("Too early to retry for " + HostName);
                return;
            }
            _timeLastSend = DateTime.UtcNow;

            ControllerSocket controllerSocket = ControllerSocketForHost(HostName);
            if (null == controllerSocket)
                _HostControllerSockets[HostName] = controllerSocket;


            if (_iPacketCount % 100 == 0 && NeedsClockStream)
            {
                byte[] msgclock = GetClockFrame(timeStart);
                DataQueue.Enqueue(msgclock);
            }

            // Optionally compress the data, but when we do, if the compressed is larger, we send the original

            byte[] msgraw = GetDataFrame(MainLEDs, timeStart);
            byte[] msg = CompressData ? CompressFrame(msgraw) : msgraw;
            if (msg.Length >= msgraw.Length)
            {
                msg = msgraw;
            }

            DataQueue.Enqueue(msg);
            _iPacketCount++;

        }

        public bool ShouldSendBatch
        {
            get
            {
                if (DataQueue.Any())
                    if ((DateTime.UtcNow - _lastBatchTime).TotalSeconds > BatchTimeout)
                        return true;

                if (DataQueue.Count() > BatchSize)
                    return true;

                return false;
            }
        }

        // WorkerConnectAndSendLoop
        //
        // Every controller has a worker thread that sits and spins in a thread loop doing the work of connecting to
        // the chips, pulling data from the queues, and sending it off

        void WorkerConnectAndSendLoop()
        {
            // We delay-start a random fraction of a quarter second to stagger the workload so that the WiFi is a little more balanced

            Thread.Sleep((int)(new System.Random().NextDouble() * 250));

            for (; ; )
            {
                ControllerSocket controllerSocket
                    = _HostControllerSockets.GetOrAdd(HostName, (hostname) =>
                    {
                        Connects++;
                        // Debug.Log("Connecting to " + HostName);
                        return new ControllerSocket(hostname, _myUi);
                    });

                if (controllerSocket == null)
                {
                    // _myUi.Status = ConnectedStatus.Disconnected;
                    Thread.Sleep(10);
                    continue;
                }

                if (false == controllerSocket.EnsureConnected())
                {
                    if (controllerSocket.IsDead)
                    {
                        // TODO status is not reporting disconnected

                        // Debug.Log("Closing disconnected socket: " + HostName);
                        // _myUi.Status = ConnectedStatus.Disconnected;
                        ControllerSocket oldSocket;
                        _HostControllerSockets.TryRemove(HostName, out oldSocket);
                    }
                    Thread.Sleep(10);
                    continue;
                }

                // Compose a message which is a binary block of N (where N is up to Count) dequeue packets all
                // in a row, which is how the chips can actually process them

                if (ShouldSendBatch)
                {
                    _lastBatchTime = DateTime.UtcNow;

                    byte[] msg = LEDInterop.CombineByteArrays(DataQueue.DequeueChunk(DataQueue.Count()).ToArray());
                    if (msg.Length > 0)
                    {
                        try
                        {
                            uint bytesSent = 0;
                            if (!controllerSocket.IsDead)
                                bytesSent = controllerSocket.SendData(msg);

                            if (bytesSent != msg.Length)
                            {
                                Debug.Log("Could not write all bytes so closing socket for " + HostName);
                                _myUi.Status = ConnectedStatus.Disconnected;
                                ControllerSocket oldSocket;
                                _HostControllerSockets.TryRemove(HostName, out oldSocket);
                            }
                            else
                            {
                                // double framesPerSecond = (double)(DateTime.UtcNow - _timeLastSend).TotalSeconds;
                                // Debug.Log("Sent " + bytesSent + ", FPS: " + framesPerSecond);
                            }
                        }
                        catch (SocketException ex)
                        {
                            Debug.Log("Exception writing to socket for " + HostName + ": " + ex.Message);
                            _myUi.Status = ConnectedStatus.Disconnected;
                            ControllerSocket oldSocket;
                            _HostControllerSockets.TryRemove(HostName, out oldSocket);
                        }
                    }
                }

                Thread.Sleep(10);
            }
        }
    }

    // ControllerSocket
    //
    // Wrapper for .Net Socket so that we can track the number of bytes sent and so on

    public class ControllerSocket
    {
        public Socket _socket;
        private IPAddress _ipAddress;
        private IPEndPoint _remoteEP;

        private DateTime LastDataFrameTime;

        private uint BytesSentSinceFrame = 0;

        public string HostName;

        public bool IsDead { get; protected set; } = false;

        public uint BytesPerSecond
        {
            get
            {
                double d = (DateTime.UtcNow - LastDataFrameTime).TotalSeconds;
                if (d < 0.001)
                    return 0;

                return (uint)(BytesSentSinceFrame / d);
            }
        }

        UIDevice _myUi;

        public ControllerSocket(string hostname, UIDevice myUi)
        {
            HostName = hostname;
            _myUi = myUi;

            _remoteEP = null;
            Dns.BeginGetHostAddresses(HostName, OnDnsGetHostAddressesComplete, this);
        }



        private void OnDnsGetHostAddressesComplete(IAsyncResult result)
        {
            var This = (ControllerSocket)result.AsyncState;

            try
            {
                This._ipAddress = Dns.EndGetHostAddresses(result)[0];
                This._remoteEP = new IPEndPoint(_ipAddress, 49152);
                // Debug.Log("Got IP of " + _remoteEP.Address.ToString() + " for  " + This.HostName);

                _myUi.Ip = _remoteEP.Address.ToString();
            }
            catch (Exception)
            {
                // Debug.Log("DNS Exception: " + HostName);
                IsDead = true;
            }
        }

        // EnsureConnected
        //
        // If not already connected, initiates the connection so that perhaps next time we will ideally be connected

        public bool EnsureConnected()
        {
            if (IsDead == true)
                return false;

            if (_remoteEP == null)
                return false;

            if (_socket != null && _socket.Connected)
                return true;

            try
            {
                if (DateTime.UtcNow - LastDataFrameTime < TimeSpan.FromSeconds(1))
                {
                    //ConsoleApp.Stats.WriteLine("Bailing connection as too early!");
                    return false;
                }
                LastDataFrameTime = DateTime.UtcNow;
                _socket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                _socket.Connect(_remoteEP);

                BytesSentSinceFrame = 0;
                // Debug.Log("Connected to " + _remoteEP);

                _myUi.Status = ConnectedStatus.Connected;

                return true;
            }
            catch (SocketException)
            {
                _myUi.Status = ConnectedStatus.Disconnected;
                IsDead = true;
                return false;
            }
        }

        public uint SendData(byte[] data)
        {
            uint result = (uint)_socket.Send(data);

            TimeSpan timeSinceLastSend = DateTime.UtcNow - LastDataFrameTime;
            if (timeSinceLastSend > TimeSpan.FromSeconds(10.0))
            {
                LastDataFrameTime = DateTime.UtcNow;
                BytesSentSinceFrame = 0;
            }
            else
            {
                BytesSentSinceFrame += result;
            }
            return result;
        }
    }
}

