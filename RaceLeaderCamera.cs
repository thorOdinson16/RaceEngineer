using UnityEngine;

public class RaceLeaderCamera : MonoBehaviour
{
    // ============================================================
    // RACE
    // ============================================================

    [Header("Race")]
    public RacePositionManager racePositionManager;

    // ============================================================
    // CAMERA POSITION
    // ============================================================

    private const float HEIGHT = 40f;
    private const float DISTANCE = 15f;

    // ============================================================
    // CAMERA LOOK
    // ============================================================

    private const float LOOK_AHEAD = 6f;

    // ============================================================
    // CAMERA SMOOTHING
    // ============================================================

    private const float POSITION_SMOOTH = 0.35f;
    private const float DIRECTION_SMOOTH = 0.30f;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private Vector3 targetPosition;
    private Vector3 smoothedPosition;
    private Vector3 positionVelocity;

    private Vector3 targetDirection;
    private Vector3 smoothedDirection;

    private bool initialized;

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        initialized = false;
    }

    // ============================================================
    // LATE UPDATE
    // ============================================================

    void FixedUpdate()
    {
        if (racePositionManager == null)
            return;

        CircuitCarAI leader =
            racePositionManager.RaceLeader;

        if (leader == null)
            return;

        // ========================================================
        // 1. READ LEADER
        // ========================================================

        Vector3 leaderPosition =
            leader.transform.position;

        // ========================================================
        // 2. GET RACING DIRECTION
        // ========================================================

        Vector3 direction =
            leader.DesiredDirection;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();

        // ========================================================
        // 3. INITIALIZE
        // ========================================================

        if (!initialized)
        {
            smoothedPosition =
                leaderPosition;

            targetPosition =
                leaderPosition;

            smoothedDirection =
                direction;

            targetDirection =
                direction;

            initialized = true;

            return;
        }

        // ========================================================
        // 4. UPDATE TARGET
        // ========================================================

        targetPosition =
            leaderPosition;

        targetDirection =
            direction;

        // ========================================================
        // 5. SMOOTH POSITION
        // ========================================================

        smoothedPosition =
            Vector3.SmoothDamp(
                smoothedPosition,
                targetPosition,
                ref positionVelocity,
                POSITION_SMOOTH
            );

        // ========================================================
        // 6. SMOOTH DIRECTION
        // ========================================================

        float directionBlend =
            1f -
            Mathf.Exp(
                -Time.deltaTime /
                DIRECTION_SMOOTH
            );

        smoothedDirection =
            Vector3.Slerp(
                smoothedDirection,
                targetDirection,
                directionBlend
            );

        smoothedDirection.y = 0f;

        if (smoothedDirection.sqrMagnitude > 0.001f)
        {
            smoothedDirection.Normalize();
        }

        // ========================================================
        // 7. CAMERA POSITION
        // ========================================================

        Vector3 cameraPosition =
            smoothedPosition -
            smoothedDirection *
            DISTANCE;

        cameraPosition.y =
            smoothedPosition.y +
            HEIGHT;

        transform.position =
            cameraPosition;

        // ========================================================
        // 8. CAMERA LOOK TARGET
        // ========================================================

        Vector3 lookTarget =
            smoothedPosition +
            smoothedDirection *
            LOOK_AHEAD;

        lookTarget.y =
            smoothedPosition.y;

        // ========================================================
        // 9. LOOK DIRECTION
        // ========================================================

        Vector3 lookDirection =
            lookTarget -
            transform.position;

        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        lookDirection.Normalize();

        // ========================================================
        // 10. ROTATE CAMERA
        // ========================================================

        Quaternion targetRotation =
            Quaternion.LookRotation(
                lookDirection,
                Vector3.up
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                directionBlend
            );
    }
}