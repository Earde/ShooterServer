using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Gun : MonoBehaviour
{
    private GunEntity[] gunModels = new GunEntity[] { 
        //Deagle
        new GunEntity { 
            headDamage = 99.0f,
            torsoDamage = 52.0f,
            armsDamage = 34.0f,
            hipsDamage = 36.0f,
            legsDamage = 26.0f,
            equipCooldown = 0.75f,
            shotCooldown = 0.6f
        },
        //AK-47
        new GunEntity {
            headDamage = 48.0f,
            torsoDamage = 22.0f,
            armsDamage = 17.0f,
            hipsDamage = 19.0f,
            legsDamage = 14.0f,
            equipCooldown = 2.5f,
            shotCooldown = 0.2f
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

    /// <summary>
    /// Set active gun at time
    /// </summary>
    /// <param name="gunId"></param>
    /// <param name="time"></param>
    /// <param name="serverTime"></param>
    public void SetGun(int gunId, float time, float serverTime)
    {
        if (activeGun != gunId)
        {
            activeGun = gunId;
            equipTimer = serverTime - time;
        }
    }

    /// <summary>
    /// Player shoot in viewDirection at time
    /// interpolationDelay for enemy rewind
    /// </summary>
    /// <param name="viewDirection"></param>
    /// <param name="shooter"></param>
    /// <param name="time"></param>
    /// <param name="interpolationDelay"></param>
    public void Shoot(Vector3 viewDirection, Player shooter, float time, float interpolationDelay)
    {
        if (EquipReady()) //TODO: Add ShootReady & AmmoReady
        {
            float packetDelay = shooter.syncedTime.GetClientTime() - time;
            shooter.DelayPosition(packetDelay);

            //Send shot to other players
            foreach (Client c in Server.clients.Values)
            {
                Player p = c?.player;
                if (p != null && p.GetID() != shooter.GetID())
                {
                    ServerSend.PlayerShot(p.GetID(), shooter.GetID(), p.syncedTime.GetClientTime(), shooter.shootOrigin.position, viewDirection);
                }
            }

            // Set positions back in time
            foreach (Client c in Server.clients.Values)
            {
                if (c.player == null) continue;
                if (c.player.GetID() != shooter.GetID())
                {
                    c.player.DelayPosition(interpolationDelay + packetDelay);
                    c.player.RewindAnimation(interpolationDelay + packetDelay);
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
                    if (p != null && p.GetID() != shooter.GetID())
                    {
                        if (!playerDamage.ContainsKey(p.GetID()))
                        {
                            playerDamage.Add(p.GetID(), damage);
                        } else if (damage > playerDamage[p.GetID()])
                        {
                            playerDamage[p.GetID()] = damage;
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
            float damageDone = 0.0f;
            int kills = 0;
            foreach (KeyValuePair<int, float> pd in playerDamage)
            {
                if (Server.clients.ContainsKey(pd.Key))
                {
                    //Debug.Log($"Player {shooter.username} hit player {pd.Key} for {pd.Value} damage.");
                    if (Server.clients[pd.Key].player != null)
                    {
                        bool dead = Server.clients[pd.Key].player.TakeDamage(pd.Value, out float damageDealt);
                        damageDone += damageDealt;
                        if (dead) kills++;
                    }
                    
                    hitmarker = true;
                }
            }
            shooter.AddDamage(damageDone);
            shooter.AddKills(kills);
            if (hitmarker)
            {
                ServerSend.PlayerHitmark(shooter.GetID());
            }
        }
    }
}
