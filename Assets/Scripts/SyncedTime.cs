using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SyncedTime : MonoBehaviour
{
    class TimePacket
    {
        public int id { get; set; }
        public float serverTime { get; set; }
    }

    List<TimePacket> timePackets = new List<TimePacket>();

    public bool isReady = false;

    private float smoothedRTT = 0.0f;
    private float timeDiff = 0.0f;

    private float errorRate = 0.0f;

    private int packetId = 0;

    private int packetsLossed = 0;

    private bool DEBUG = false;

    public float GetClientTime()
    {
        return Time.time - timeDiff;
    }

    public void SendTimePacket(int playerId)
    {
        timePackets.Add(new TimePacket { id = packetId, serverTime = Time.time });
        ServerSend.TimeSync(playerId, packetId);
        packetId++;
    }

    public void UpdateTime(int packetId, float clientTime)
    {
        TimePacket tp = timePackets.FirstOrDefault(x => x.id == packetId);
        if (tp != default && tp != null)
        {
            float rtt = Time.time - tp.serverTime;
            smoothedRTT = smoothedRTT * 0.8f + rtt * 0.2f;
            float newTimeDiff = Time.time - clientTime - smoothedRTT / 2.0f;
            errorRate = Mathf.Abs(newTimeDiff - timeDiff);
            timeDiff = newTimeDiff;
        }
        timePackets.Remove(tp);
        packetsLossed += timePackets.RemoveAll(t => t.serverTime < Time.time - 1.0f);
        isReady = true;
        if (DEBUG) Logs();
    }

    private void Logs()
    {
        Debug.Log("Packets Lossed: " + packetsLossed);
        Debug.Log("Time sync error: " + errorRate);
    }
}
