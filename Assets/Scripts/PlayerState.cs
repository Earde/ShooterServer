using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Server state of player at client time
/// </summary>
public class PlayerState
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public float YVelocity { get; set; }
    public float Time { get; set; }

    public float AnimationTime { get; set; }
}