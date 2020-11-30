using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Gun : MonoBehaviour
{
    class GunEntity
    {
        public float headDamage { get; set; }
        public float torsoDamage { get; set; }
        public float hipsDamage { get; set; }
        public float armsDamage { get; set; }
        public float legsDamage { get; set; }
        public float equipCooldown { get; set; }
        public float shotCooldown { get; set; }
    }

    private GunEntity[] gunModels = new GunEntity[] { 
        //Deagle
        new GunEntity { 
            headDamage = 84.0f,
            torsoDamage = 44.0f,
            armsDamage = 18.0f,
            hipsDamage = 33.0f,
            legsDamage = 17.0f,
            equipCooldown = 1f,
            shotCooldown = 0.5f
        },
        //AK-47
        new GunEntity {
            headDamage = 45.0f,
            torsoDamage = 22.0f,
            armsDamage = 9.0f,
            hipsDamage = 17.0f,
            legsDamage = 8.0f,
            equipCooldown = 2f,
            shotCooldown = 0.22f
        }
    };

    private float equipTimer = 0.0f;

    private int activeGun = 0;

    private bool EquipReady()
    {
        if (equipTimer < gunModels[activeGun].equipCooldown)
        {
            return false;
        }
        return true;
    }

    void Update()
    {
        equipTimer += Time.deltaTime;
    }

    public void SetGun(int gunId, float time, float serverTime)
    {
        if (activeGun != gunId)
        {
            activeGun = gunId;
            equipTimer = serverTime - time;
        }
    }

    public void Shoot(Vector3 viewDirection, Player shooter, float time, float interpolationDelay)
    {
        if (EquipReady()) //TODO: Add ShootReady & AmmoReady
        {
            //Get server time
            float packetDelay = shooter.syncedTime.GetClientTime() - time;
            //Delay shooter position
            shooter.DelayPosition(packetDelay);
            shooter.RewindAnimation(packetDelay);
            //Send shot to other players
            foreach (Client c in Server.clients.Values)
            {
                Player p = c?.player;
                if (p != null && p.id != shooter.id)
                {
                    ServerSend.PlayerShot(p.id, shooter.id, p.syncedTime.GetClientTime() - packetDelay, shooter.shootOrigin.position, viewDirection);
                }
            }

            // Set positions back in time
            foreach (Client c in Server.clients.Values)
            {
                if (c.player == null) continue;
                if (c.player.id != shooter.id)
                {
                    c.player.DelayPosition(packetDelay + interpolationDelay);
                    c.player.RewindAnimation(packetDelay + interpolationDelay);
                }
            }
            // Do hit collision
            Debug.DrawRay(shooter.shootOrigin.position, viewDirection.normalized * 1000f, Color.cyan, 0.5f);

            RaycastHit[] hits = Physics.RaycastAll(shooter.shootOrigin.position, viewDirection, 1000f);
            hits = hits.OrderBy(h => h.distance).ToArray();

            Dictionary<int, float> playerDamage = new Dictionary<int, float>();
            for (int i = 0; i < hits.Length; i++)
            {
                float damage = -1.0f;
                if (hits[i].collider.CompareTag("Head")) damage = gunModels[activeGun].headDamage;
                else if (hits[i].collider.CompareTag("Torso")) damage = gunModels[activeGun].torsoDamage;
                else if (hits[i].collider.CompareTag("Hips")) damage = gunModels[activeGun].hipsDamage;
                else if (hits[i].collider.CompareTag("Arms")) damage = gunModels[activeGun].armsDamage;
                else if (hits[i].collider.CompareTag("Legs")) damage = gunModels[activeGun].legsDamage;
                if (damage > 0.0f)
                {
                    Player p = hits[i].collider.GetComponentInParent<Player>();
                    if (p != null && p.id != shooter.id)
                    {
                        if (!playerDamage.ContainsKey(p.id))
                        {
                            playerDamage.Add(p.id, damage);
                        } else if (damage > playerDamage[p.id])
                        {
                            playerDamage[p.id] = damage;
                        }
                    }
                }
            }
            // Restore positions
            foreach (Client c in Server.clients.Values)
            {
                if (c.player == null) continue;
                c.player.SetLatestTick();
            }
            // Handle damage and send hitmarker
            bool hitmarker = false;
            foreach (KeyValuePair<int, float> pd in playerDamage)
            {
                if (Server.clients.ContainsKey(pd.Key))
                {
                    Debug.Log($"Player {shooter.username} hit player {pd.Key} for {pd.Value} damage.");
                    Server.clients[pd.Key].player?.TakeDamage(pd.Value);
                    hitmarker = true;
                }
            }
            if (hitmarker)
            {
                ServerSend.PlayerHitmark(shooter.id);
            }
        }
    }
}
