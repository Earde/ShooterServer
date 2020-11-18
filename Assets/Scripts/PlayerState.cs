using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerState
{
    public Vector3 position { get; set; }
    public Quaternion rotation { get; set; }
    public float _yVelocity { get; set; }
    public float time { get; set; }
}