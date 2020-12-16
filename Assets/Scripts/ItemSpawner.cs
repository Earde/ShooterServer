using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public static Dictionary<int, ItemSpawner> spawners = new Dictionary<int, ItemSpawner>();
    private static int nextSpawnerId = 1;

    public int spawnerId;
    public bool hasItem = false;

    private void Start()
    {
        hasItem = false;
        spawnerId = nextSpawnerId;
        nextSpawnerId++;
        spawners.Add(spawnerId, this);

        StartCoroutine(SpawnItem());
    }

    /// <summary>
    /// Pick up item
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (hasItem && other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player.AttemptPickupItem())
            {
                ItemPickedUp(player.GetID());
            }
        }
    }

    /// <summary>
    /// Spawn item
    /// </summary>
    /// <returns></returns>
    private IEnumerator SpawnItem()
    {
        yield return new WaitForSeconds(10f);
        hasItem = true;
        ServerSend.ItemSpawned(spawnerId);
    }

    /// <summary>
    /// Send itemPickedUp to server
    /// </summary>
    /// <param name="byPlayer"></param>
    private void ItemPickedUp(int byPlayer)
    {
        hasItem = false;
        ServerSend.ItemPickedUp(spawnerId, byPlayer);

        StartCoroutine(SpawnItem());
    }
}
