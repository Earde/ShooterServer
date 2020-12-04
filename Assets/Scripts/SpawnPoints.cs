using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SpawnPoints
{
    public static List<SpawnPoint> spawnPoints = new List<SpawnPoint>
    {
        new SpawnPoint{
            Position = new Vector3(20.5f, 2.5f, -1.8f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(-138, 0.5f, -73f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(-154f, 0.5f, 79f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(-3f, 5f, 80f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(-140f, 0.5f, 81f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(120f, 0.5f, -85f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(-14f, -24f, -50f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(-12f, -24f, 34f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(-65f, 0.5f, -25f),
            Rotation = Quaternion.identity
        },
        new SpawnPoint{
            Position = new Vector3(50f, 0.5f, -25f),
            Rotation = Quaternion.identity
        }
    };

    public static SpawnPoint GetRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }
}

public class SpawnPoint
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
}
