using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator animator;

    private Vector3 prevPos = Vector3.zero;
    private Vector3 movingDirection = Vector3.zero;

    private float tempNormalizedTime = 0f;

    private void FixedUpdate()
    {
        movingDirection = transform.position - prevPos;
        prevPos = transform.position;

        Vector2 animationDir = GetMovingDirection(movingDirection, this.transform.forward);
        animator.SetFloat("xVelocity", animationDir.x);
        animator.SetFloat("zVelocity", animationDir.y);
    }

    public void RewindAnimation(float _lerp, Vector3 _movingDirection, Vector3 _forward)
    {
        tempNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

        Vector2 animationDir = GetMovingDirection(_movingDirection, _forward);
        animator.SetFloat("xVelocity", animationDir.x);
        animator.SetFloat("zVelocity", animationDir.y);
        animator.Update(0f);
        animator.Play(0, -1, _lerp);
        animator.Update(0f);
    }

    public void RestoreAnimation(Vector3 _movingDirection, Vector3 _forward)
    {
        Vector2 animationDir = GetMovingDirection(_movingDirection, _forward);
        animator.SetFloat("xVelocity", animationDir.x);
        animator.SetFloat("zVelocity", animationDir.y);
        animator.Update(0f);
        animator.Play(0, -1, tempNormalizedTime);
        animator.Update(0f);
    }

    private static Vector2 GetMovingDirection(Vector3 _movingDirection, Vector3 _forward)
    {
        Vector2 movDir = Vector3.zero;

        if (_movingDirection == Vector3.zero || _movingDirection.magnitude < 0.00001f) return movDir;

        float angle = CalculateAngle180(_forward, _movingDirection.normalized);
        if (Mathf.Abs(angle) <= 50.0f)
        {
            movDir.y = 1.0f;
        }
        else if (Mathf.Abs(angle) >= 130.0f)
        {
            movDir.y = -1.0f;
        }
        if (angle >= 40.0f && angle <= 140.0f)
        {
            movDir.x = 1.0f;
        }
        else if (angle <= -40.0f && angle >= -140.0f)
        {
            movDir.x = -1.0f;
        }

        return movDir.normalized * 5.0f;
    }

    /// <summary>
    /// Get euler angle between two direction (-180 to 180)
    /// </summary>
    /// <param name="fromDir"></param>
    /// <param name="toDir"></param>
    /// <returns></returns>
    private static float CalculateAngle180(Vector3 fromDir, Vector3 toDir)
    {
        float angle = Quaternion.FromToRotation(fromDir, toDir).eulerAngles.y;
        if (angle > 180) { return angle - 360f; }
        return angle;
    }
}
