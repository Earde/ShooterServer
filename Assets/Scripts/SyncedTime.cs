using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SyncedTime
{
    class TimePacket
    {
        public int id { get; set; }
        public float serverTime { get; set; }
    }

    List<TimePacket> timePackets = new List<TimePacket>();

    public bool isReady = false;

    private float smoothedRTT = 0.0f;
    private float timeDifference = 0.0f;

    private int packetId = 0;

    public float GetTime()
    {
        return Time.time - timeDifference;
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
            float curClientTime = clientTime + smoothedRTT / 2.0f;
            timeDifference = Time.time - curClientTime;
        }
        timePackets.Remove(tp);
        isReady = true;
    }
}
