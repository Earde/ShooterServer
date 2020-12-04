using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    #region TCP
    private static void SendTCPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clients[toClient].tcp.SendData(packet);
    }

    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(packet);
        }
    }

    private static void SendTCPDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.clients[i].tcp.SendData(packet);
            }
        }
    }
    #endregion

    #region UDP
    private static void SendUDPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clients[toClient].udp.SendData(packet);
    }

    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(packet);
        }
    }

    private static void SendUDPDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.clients[i].udp.SendData(packet);
            }
        }
    }
    #endregion

    #region Packets
    public static void Welcome(int toClient, string msg)
    {
        using (Packet packet = new Packet((int)ServerPackets.welcome))
        {
            packet.Write(msg);
            packet.Write(toClient);

            SendTCPData(toClient, packet);
        }
    }

    public static void TimeSync(int toClient, int packetId, float SRTT)
    {
        using (Packet packet = new Packet((int)ServerPackets.timeSync))
        {
            packet.Write(packetId);
            packet.Write(SRTT);
            SendUDPData(toClient, packet);
        }
    }

    public static void SpawnPlayer(int toClient, Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            packet.Write(player.id);
            packet.Write(player.username);
            packet.Write(player.transform.position);
            packet.Write(player.transform.rotation);

            SendTCPData(toClient, packet);
        }
    }

    public static void PlayerData(int id, PlayerState state, int damageDone, int kills, int deaths)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerData))
        {
            packet.Write(id);
            packet.Write(state.Position);
            packet.Write(state.Rotation);
            packet.Write(state.YVelocity);
            packet.Write(state.Time);

            packet.Write(damageDone);
            packet.Write(kills);
            packet.Write(deaths);

            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerDisconnected(int playerId)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            packet.Write(playerId);

            SendTCPDataToAll(packet);
        }
    }

    public static void PlayerHealth(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerHealth))
        {
            packet.Write(player.id);
            packet.Write(player.health);

            SendTCPDataToAll(packet);
        }
    }

    public static void PlayerRespawned(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerRespawn))
        {
            packet.Write(player.id);

            SendTCPDataToAll(packet);
        }
    }

    public static void PlayerHitmark(int toClient)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerHitmark))
        {
            packet.Write(toClient);
            SendUDPData(toClient, packet);
        }
    }

    public static void PlayerShot(int toClient, int clientThatShot, float time, Vector3 position, Vector3 forward)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerShot))
        {
            packet.Write(clientThatShot);
            packet.Write(time);
            packet.Write(position);
            packet.Write(forward);
            SendUDPData(toClient, packet);
        }
    }

    public static void CreateItemSpawner(int toClient, int spawnerId, Vector3 spawnerPosition, bool hasItem)
    {
        using(Packet packet = new Packet((int)ServerPackets.createItemSpawner))
        {
            packet.Write(spawnerId);
            packet.Write(spawnerPosition);
            packet.Write(hasItem);

            SendTCPData(toClient, packet);
        }
    }

    public static void ItemSpawned(int spawnerId)
    {
        using (Packet packet = new Packet((int)ServerPackets.itemSpawned))
        {
            packet.Write(spawnerId);

            SendTCPDataToAll(packet);
        }
    }

    public static void ItemPickedUp(int spawnerId, int byPlayer)
    {
        using (Packet packet = new Packet((int)ServerPackets.itemPickedUp))
        {
            packet.Write(spawnerId);
            packet.Write(byPlayer);

            SendTCPDataToAll(packet);
        }
    }

    public static void SpawnProjectile(Projectile projectile, int thrownByPlayer)
    {
        using (Packet packet = new Packet((int)ServerPackets.spawnProjectile))
        {
            packet.Write(projectile.id);
            packet.Write(projectile.transform.position);
            packet.Write(thrownByPlayer);

            SendTCPDataToAll(packet);
        }
    }

    public static void ProjectilePosition(Projectile projectile)
    {
        using (Packet packet = new Packet((int)ServerPackets.projectilePosition))
        {
            packet.Write(projectile.id);
            packet.Write(projectile.transform.position);

            SendTCPDataToAll(packet);
        }
    }

    public static void ProjectileExploded(Projectile projectile)
    {
        using (Packet packet = new Packet((int)ServerPackets.projectileExploded))
        {
            packet.Write(projectile.id);
            packet.Write(projectile.transform.position);

            SendTCPDataToAll(packet);
        }
    }
    #endregion
}
