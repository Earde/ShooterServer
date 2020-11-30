using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public Animator animator;

    private float tempNormalizedTime = 0f;

    private void Start()
    {
        animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
    }

    public void Move(Vector3 movementDirection, Vector3 camForward)
    {
        Vector2 animationDir = GetMovingDirection(movementDirection, camForward);
        animator.SetFloat("xVelocity", animationDir.x);
        animator.SetFloat("zVelocity", animationDir.y);
    }

    public float GetNormalizedTime()
    {
        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }

    public void RewindAnimation(float normalizedTime, Vector3 movementDirection, Vector3 camForward)
    {
        tempNormalizedTime = GetNormalizedTime();
        Vector2 animationDir = GetMovingDirection(movementDirection, camForward);

        animator.SetFloat("xVelocity", animationDir.x);
        animator.SetFloat("zVelocity", animationDir.y);
        animator.Update(0f);
        animator.Play(0, -1, normalizedTime);
        animator.Update(0f);
    }

    public void RestoreAnimation(Vector3 movementDirection, Vector3 camForward)
    {
        Vector2 animationDir = GetMovingDirection(movementDirection, camForward);
        animator.SetFloat("xVelocity", animationDir.x);
        animator.SetFloat("zVelocity", animationDir.y);
        animator.Update(0f);
        animator.Play(0, -1, tempNormalizedTime);
        animator.Update(0f);
    }

    /// <summary>
    /// Get
    /// </summary>
    /// <param name="movementVelocity"></param>
    /// <param name="_forward"></param>
    /// <returns></returns>
    private static Vector2 GetMovingDirection(Vector3 movementVelocity, Vector3 _forward)
    {
        Vector2 movDir = Vector3.zero;

        if (movementVelocity == Vector3.zero || movementVelocity.magnitude < 0.00001f) return movDir;

        float angle = CalculateAngle180(_forward, movementVelocity.normalized);
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
