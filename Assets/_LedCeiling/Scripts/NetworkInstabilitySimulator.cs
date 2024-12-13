using System;
using System.Threading;
using UnityEngine;

public class NetworkInstabilitySimulator
{
    private readonly System.Random _random = new System.Random();
    private readonly float _packetLossRate; // 0.0 to 1.0
    private readonly int _minLatencyMs;
    private readonly int _maxLatencyMs;

    public NetworkInstabilitySimulator(float packetLossRate = 0.1f, int minLatencyMs = 50, int maxLatencyMs = 200)
    {
        _packetLossRate = packetLossRate;
        _minLatencyMs = minLatencyMs;
        _maxLatencyMs = maxLatencyMs;
    }

    public bool ShouldDropPacket()
    {
        return _random.NextDouble() < _packetLossRate;
    }

    public void SimulateLatency()
    {
        int latency = _random.Next(_minLatencyMs, _maxLatencyMs);
        Thread.Sleep(latency);
    }
}