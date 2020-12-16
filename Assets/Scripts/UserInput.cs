using UnityEngine;

/// <summary>
/// input & rotation at client game time
/// </summary>
class UserInput
{
    public bool[] Inputs { get; set; }
    public float Time { get; set; }
    public Quaternion Rotation { get; set; }
}