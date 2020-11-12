using System.Collections;
using System.Collections.Generic;
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
            equipCooldown = 1.25f,
            shotCooldown = 0.05f
        },
        //AK-47
        new GunEntity {
            headDamage = 45.0f,
            torsoDamage = 22.0f,
            armsDamage = 9.0f,
            hipsDamage = 17.0f,
            legsDamage = 8.0f,
            equipCooldown = 2.5f,
            shotCooldown = 0.05f
        }
    };

    private float equipTimer = 0.0f;

    private int activeGun = 0;

    private bool IsReady()
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

    public void SetGun(int gunId, float time)
    {
        if (activeGun != gunId)
        {
            activeGun = gunId;
            equipTimer = Time.time - time;
        }
    }

    public void Shoot(Vector3 viewDirection, Transform shootOrigin, float time, float enemyTime, int playerId)
    {
        if (IsReady())
        {
            // Set positions back in time
            foreach (Client c in Server.clients.Values)
            {
                if (c.player == null) continue;
                if (c.player.id == playerId)
                {
                    c.player.SetPositionInTime(time);
                }
                else
                {
                    c.player.SetPositionInTime(enemyTime);
                }
            }
            // Do hit collision
            Debug.DrawRay(shootOrigin.position, viewDirection.normalized * 1000f, Color.cyan, 0.5f);
            RaycastHit[] hits = Physics.RaycastAll(shootOrigin.position, viewDirection, 1000f);
            for (int i = 0; i < hits.Length; i++)
            {
                float damage = gunModels[activeGun].headDamage;
                if (hits[i].collider.CompareTag("Head")) damage = gunModels[activeGun].headDamage;
                else if (hits[i].collider.CompareTag("Torso")) damage = gunModels[activeGun].torsoDamage;
                else if (hits[i].collider.CompareTag("Hips")) damage = gunModels[activeGun].hipsDamage;
                else if (hits[i].collider.CompareTag("Arms")) damage = gunModels[activeGun].armsDamage;
                else if (hits[i].collider.CompareTag("Legs")) damage = gunModels[activeGun].legsDamage;
                EnemyHit(hits[i].collider, damage, playerId);
            }
            // Restore positions
            foreach (Client c in Server.clients.Values)
            {
                if (c.player == null) continue;
                c.player.SetLatestPosition();
            }
        }
    }

    private void EnemyHit(Collider c, float damage, int playerId)
    {
        Player p = c.GetComponentInParent<Player>();
        if (p.id != playerId)
        {
            Debug.Log($"Player {playerId} hit player {p.id} for {damage} damage.");
            //Hitmarker
            p.TakeDamage(damage);
        }
    }
}
