using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(int fromClient, Packet packet)
    {
        int clientIdCheck = packet.ReadInt();
        string username = packet.ReadString();

        if (Server.clients.ContainsKey(fromClient))
            Debug.Log($"{Server.clients[fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {fromClient}");

        if (fromClient != clientIdCheck)
        {
            Debug.Log($"Player \"{username}\" ID: {fromClient} has assumed the wrong client ID ({clientIdCheck}).");
        }

        if (Server.clients.ContainsKey(fromClient)) Server.clients[fromClient].SendIntoGame(username);
    }

    public static void TimeSync(int fromClient, Packet packet)
    {
        int packetId = packet.ReadInt();
        float clientTime = packet.ReadFloat();
        if (Server.clients.ContainsKey(fromClient)) Server.clients[fromClient].player.UpdateSyncTime(packetId, clientTime);
    }

    public static void PlayerMovement(int fromClient, Packet packet)
    {
        float time = packet.ReadFloat();
        bool[] inputs = new bool[packet.ReadInt()];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = packet.ReadBool();
        }
        Quaternion rotation = packet.ReadQuaternion();
        if (Server.clients.ContainsKey(fromClient)) Server.clients[fromClient].player?.AddInput(inputs, rotation, time);
    }

    public static void PlayerShoot(int fromClient, Packet packet)
    {
        Vector3 shootDirection = packet.ReadVector3();
        float time = packet.ReadFloat();
        float enemyTime = packet.ReadFloat();

        if (Server.clients.ContainsKey(fromClient)) Server.clients[fromClient].player?.Shoot(shootDirection, time, enemyTime);
    }

    public static void PlayerThrowItem(int fromClient, Packet packet)
    {
        Vector3 throwDirection = packet.ReadVector3();
        if (Server.clients.ContainsKey(fromClient)) Server.clients[fromClient].player?.ThrowItem(throwDirection);
    }
}
