using UnityEngine;
using UnityEngine.Splines;

public class CircuitCarAI : MonoBehaviour
{
    // ============================================================
    // TRACK
    // ============================================================

    [Header("Track")]
    public SplineContainer trackSpline;

    // ============================================================
    // CAR SETTINGS
    // ============================================================

    private const float MAX_STRAIGHT_SPEED = 30f;
    private const float HAIRPIN_SPEED = 12f;

    private const float ACCELERATION = 20f;
    private const float BRAKING_POWER = 35f;

    private const float TURN_SPEED = 120f;

    private const float CAR_HEIGHT_ABOVE_ROAD = 0.1f;

    private const float START_T = 0f;
    private const float START_ORIENTATION_OFFSET = 0f;

    // ============================================================
    // AI SETTINGS
    // ============================================================

    private const float LOOK_AHEAD_DISTANCE = 4f;
    private const float CURVATURE_CHECK_DISTANCE = 20f;

    // ============================================================
    // INDIVIDUAL CAR PERFORMANCE
    // ============================================================

    private const float ACCELERATION_VARIATION = 0.05f;
    private const float CORNERING_VARIATION = 0.05f;
    private const float TOP_SPEED_VARIATION = 0.02f;

    // ============================================================
    // OVERTAKING
    // ============================================================

    private const float OVERTAKE_DETECTION_DISTANCE = 14f;
    private const float OVERTAKE_T_DISTANCE = 0.12f;
    private const float OVERTAKE_SPEED_ADVANTAGE = 1.0f;

    private const float OVERTAKE_OFFSET = 2.0f;
    private const float MIN_CAR_SIDE_DISTANCE = 3.0f;

    private const float MAX_OVERTAKE_CURVE = 0.30f;

    private const float SIDE_CHECK_FORWARD_DISTANCE = 12f;
    private const float SIDE_CHECK_BACK_DISTANCE = 4f;

    private const float OVERTAKE_BLEND_SPEED = 2.5f;
    private const float OVERTAKE_MIN_TIME = 0.8f;
    private const float OVERTAKE_CLEAR_DISTANCE = 4f;

    private const float FOLLOW_DISTANCE = 5f;

    // ============================================================
    // DEFENDING
    // ============================================================

    // A defender may make ONE lateral move when a faster car
    // approaches from behind. Once the move is made, the side
    // is locked until the battle is over.
    private const float DEFENSE_DETECTION_DISTANCE = 10f;
    private const float DEFENSE_T_DISTANCE = 0.10f;
    private const float DEFENSE_SPEED_ADVANTAGE = 0.5f;
    private const float DEFENSE_OFFSET = 1.5f;
    private const float DEFENSE_BLEND_SPEED = 3.0f;
    private const float DEFENSE_MIN_TIME = 0.6f;
    private const float DEFENSE_CLEAR_DISTANCE = 5f;
    private const float DEFENSE_COOLDOWN = 2.5f;

    // ============================================================
    // FINISH PARKING
    // ============================================================

    private const float FINISH_SLOT_SPACING = 3.5f;

    private const float FINISH_FORWARD_OFFSET = 4f;

    private const float FINISH_STOP_DISTANCE = 0.15f;

    private const float FINISH_MOVE_SPEED = 8f;

    private const float FINISH_TURN_SPEED = 180f;

    // ============================================================
    // ROAD
    // ============================================================

    private const string TERRAIN_LAYER_NAME = "Terrain";

    private LayerMask terrainLayer;

    // ============================================================
    // WALL AVOIDANCE
    // ============================================================

    private const string BARRIER_LAYER_NAME = "Barrier";

    private const float WALL_AVOIDANCE_DISTANCE = 4f;
    private const float WALL_AVOIDANCE_STRENGTH = 0.5f;

    private LayerMask barrierLayer;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private float currentSpeed;
    private float lastT;
    private Vector3 smoothedSeparation;

    // ============================================================
    // INDIVIDUAL PERFORMANCE
    // ============================================================

    private float accelerationMultiplier;
    private float corneringMultiplier;
    private float topSpeedMultiplier;

    // ============================================================
    // OVERTAKING STATE
    // ============================================================

    private CircuitCarAI carAhead;

    private CircuitCarAI overtakeTarget;

    private bool overtaking;

    private float overtakeSide;

    private float overtakeTimer;

    private float overtakeBlend;

    // ============================================================
    // DEFENDING STATE
    // ============================================================

    private CircuitCarAI defendingAgainst;
    private bool defending;
    private float defenseSide;
    private float defenseTimer;
    private float defenseBlend;
    private float defenseCooldownTimer;

    // ============================================================
    // FINISH STATE
    // ============================================================

    private bool finishParking;

    private bool finishParked;

    private int finishPosition = -1;

    private RacePositionManager finishPositionManager;

    // ============================================================
    // RACE STATE
    // ============================================================

    public bool RaceStarted = false;

    // ============================================================
    // PUBLIC VALUES
    // ============================================================

    public float DesiredSpeed
    {
        get
        {
            return RaceStarted
                ? currentSpeed
                : 0f;
        }
    }

    public Vector3 DesiredDirection
    {
        get
        {
            return -transform.forward;
        }
    }

    public float CurrentT
    {
        get
        {
            return lastT;
        }
    }

    public float CurrentSpeed
    {
        get
        {
            return currentSpeed;
        }
    }

    public bool IsOvertaking
    {
        get
        {
            return overtaking;
        }
    }

    public CircuitCarAI CarAhead
    {
        get
        {
            return carAhead;
        }
    }

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        if (trackSpline == null)
        {
            Debug.LogError(
                "CircuitCarAI: Track Spline is not assigned!"
            );

            return;
        }

        terrainLayer =
            LayerMask.GetMask(
                TERRAIN_LAYER_NAME
            );

        barrierLayer =
            LayerMask.GetMask(
                BARRIER_LAYER_NAME
            );

        // ========================================================
        // INITIAL SPEED
        // ========================================================

        currentSpeed =
            MAX_STRAIGHT_SPEED * 0.3f;

        // ========================================================
        // RANDOMIZE PERFORMANCE
        // ========================================================

        float accelerationVariation =
            Random.Range(
                -ACCELERATION_VARIATION,
                ACCELERATION_VARIATION
            );

        float corneringVariation =
            Random.Range(
                -CORNERING_VARIATION,
                CORNERING_VARIATION
            );

        float topSpeedVariation =
            Random.Range(
                -TOP_SPEED_VARIATION,
                TOP_SPEED_VARIATION
            );

        bool allPositive =
            accelerationVariation > 0f &&
            corneringVariation > 0f &&
            topSpeedVariation > 0f;

        bool allNegative =
            accelerationVariation < 0f &&
            corneringVariation < 0f &&
            topSpeedVariation < 0f;

        while (allPositive || allNegative)
        {
            accelerationVariation =
                Random.Range(
                    -ACCELERATION_VARIATION,
                    ACCELERATION_VARIATION
                );

            corneringVariation =
                Random.Range(
                    -CORNERING_VARIATION,
                    CORNERING_VARIATION
                );

            topSpeedVariation =
                Random.Range(
                    -TOP_SPEED_VARIATION,
                    TOP_SPEED_VARIATION
                );

            allPositive =
                accelerationVariation > 0f &&
                corneringVariation > 0f &&
                topSpeedVariation > 0f;

            allNegative =
                accelerationVariation < 0f &&
                corneringVariation < 0f &&
                topSpeedVariation < 0f;
        }

        accelerationMultiplier =
            1f + accelerationVariation;

        corneringMultiplier =
            1f + corneringVariation;

        topSpeedMultiplier =
            1f + topSpeedVariation;

        // ========================================================
        // STARTING POSITION
        // ========================================================

        lastT =
            START_T;

        // ========================================================
        // STARTING ORIENTATION
        // ========================================================

        Vector3 startTangent =
            ConvertToVector3(
                trackSpline.EvaluateTangent(
                    START_T
                )
            );

        startTangent.y = 0f;

        if (startTangent.sqrMagnitude > 0.001f)
        {
            startTangent.Normalize();

            Quaternion startRotation =
                Quaternion.LookRotation(
                    -startTangent,
                    Vector3.up
                );

            startRotation *=
                Quaternion.Euler(
                    0f,
                    START_ORIENTATION_OFFSET,
                    0f
                );

            transform.rotation =
                startRotation;
        }

        Debug.Log(
            gameObject.name +
            " performance:" +
            "\nAcceleration: " +
            accelerationMultiplier.ToString("F3") +
            "\nCornering: " +
            corneringMultiplier.ToString("F3") +
            "\nTop Speed: " +
            topSpeedMultiplier.ToString("F3")
        );
    }

    // ============================================================
    // START FINISH PARKING
    // ============================================================

    public void StartFinishParking(
        RacePositionManager manager,
        int position
    )
    {
        if (finishParked)
            return;

        if (manager == null)
            return;

        if (position <= 0)
            return;

        finishPositionManager =
            manager;

        finishPosition =
            position;

        finishParking =
            true;

        finishParked =
            false;

        // Stop normal race behavior.
        RaceStarted =
            false;

        currentSpeed =
            0f;

        // Cancel any overtake.
        overtaking =
            false;

        overtakeTarget =
            null;

        carAhead =
            null;

        overtakeTimer =
            0f;

        overtakeBlend =
            0f;

        defending =
            false;

        defendingAgainst =
            null;

        defenseTimer =
            0f;

        defenseBlend =
            0f;

        Debug.Log(
            gameObject.name +
            " moving to finish slot P" +
            finishPosition
        );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        if (trackSpline == null)
            return;

        // --------------------------------------------------------
        // FINISHED CAR
        // --------------------------------------------------------

        if (finishParking)
        {
            UpdateFinishParking();
            return;
        }

        // --------------------------------------------------------
        // NORMAL RACE
        // --------------------------------------------------------

        if (!RaceStarted)
            return;

        if (defenseCooldownTimer > 0f)
        {
            defenseCooldownTimer -= Time.deltaTime;

            if (defenseCooldownTimer < 0f)
                defenseCooldownTimer = 0f;
        }

        float trackLength =
            trackSpline.Spline.GetLength();

        if (trackLength <= 0.01f)
            return;

        // ========================================================
        // 1. FIND CURRENT POSITION ON SPLINE
        // ========================================================

        Vector3 localCarPosition =
            trackSpline.transform.InverseTransformPoint(
                transform.position
            );

        SplineUtility.GetNearestPoint(
            trackSpline.Spline,
            localCarPosition,
            out _,
            out float candidateT
        );

        // ========================================================
        // 2. PREVENT SPLINE POSITION JUMPING
        // ========================================================

        float forwardDelta =
            Mathf.Repeat(
                candidateT - lastT,
                1f
            );

        if (forwardDelta < 0.25f)
        {
            lastT =
                candidateT;
        }

        float currentT =
            lastT;

        // ========================================================
        // 3. LOOK AHEAD
        // ========================================================

        float lookAheadT =
            LOOK_AHEAD_DISTANCE /
            trackLength;

        float targetT =
            Mathf.Repeat(
                currentT +
                lookAheadT,
                1f
            );

        Vector3 trackTargetPos =
            ConvertToVector3(
                trackSpline.EvaluatePosition(
                    targetT
                )
            );

        Vector3 trackTangent =
            ConvertToVector3(
                trackSpline.EvaluateTangent(
                    targetT
                )
            );

        trackTangent.y = 0f;

        if (trackTangent.sqrMagnitude > 0.001f)
        {
            trackTangent.Normalize();
        }

        // ========================================================
        // 4. LOOK FURTHER AHEAD FOR CORNERS
        // ========================================================

        float curvatureT =
            CURVATURE_CHECK_DISTANCE /
            trackLength;

        float farT =
            Mathf.Repeat(
                currentT +
                curvatureT,
                1f
            );

        Vector3 farTangent =
            ConvertToVector3(
                trackSpline.EvaluateTangent(
                    farT
                )
            );

        farTangent.y = 0f;

        if (farTangent.sqrMagnitude > 0.001f)
        {
            farTangent.Normalize();
        }

        float curveAngle =
            Vector3.Angle(
                trackTangent,
                farTangent
            );

        // ========================================================
        // 5. SPEED CONTROL
        // ========================================================

        float curveAmount =
            Mathf.Clamp01(
                curveAngle /
                75f
            );

        float targetSpeed =
            Mathf.Lerp(
                MAX_STRAIGHT_SPEED *
                topSpeedMultiplier,

                HAIRPIN_SPEED *
                corneringMultiplier,

                curveAmount
            );

        // ========================================================
        // 6. DETECT CAR AHEAD
        // ========================================================

        FindCarAhead();

        // ========================================================
        // 7. HANDLE DEFENDING
        // ========================================================

        HandleDefending();

        // ========================================================
        // 8. HANDLE OVERTAKING
        // ========================================================

        HandleOvertaking(
            curveAmount
        );

        // ========================================================
        // 8. BRAKING / ACCELERATION
        // ========================================================

        if (currentSpeed > targetSpeed)
        {
            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    targetSpeed,
                    BRAKING_POWER *
                    Time.deltaTime
                );
        }
        else
        {
            currentSpeed =
                Mathf.MoveTowards(
                    currentSpeed,
                    targetSpeed,
                    ACCELERATION *
                    accelerationMultiplier *
                    Time.deltaTime
                );
        }

        // ========================================================
        // 9. FOLLOW SLOW CAR
        // ========================================================

        if (!overtaking &&
            carAhead != null)
        {
            float distanceToCar =
                Vector3.Distance(
                    transform.position,
                    carAhead.transform.position
                );

            if (distanceToCar <
                FOLLOW_DISTANCE)
            {
                currentSpeed =
                    Mathf.Min(
                        currentSpeed,
                        carAhead.CurrentSpeed
                    );
            }
        }

        // ========================================================
        // 10. OVERTAKE TARGET POSITION
        // ========================================================

        Vector3 finalTargetPosition =
            trackTargetPos;

        // ========================================================
        // STABLE TRACK RIGHT VECTOR
        // ========================================================

        Vector3 travelDirection =
            -trackTangent;

        travelDirection.y = 0f;

        if (travelDirection.sqrMagnitude > 0.001f)
        {
            travelDirection.Normalize();
        }

        Vector3 trackRight =
            Vector3.Cross(
                Vector3.up,
                travelDirection
            );

        trackRight.y = 0f;

        if (trackRight.sqrMagnitude > 0.001f)
        {
            trackRight.Normalize();
        }

        // ========================================================
        // APPLY OVERTAKE OFFSET
        // ========================================================

        if (overtaking &&
            overtakeTarget != null)
        {
            overtakeBlend =
                Mathf.MoveTowards(
                    overtakeBlend,
                    1f,
                    OVERTAKE_BLEND_SPEED *
                    Time.deltaTime
                );

            Vector3 overtakePosition =
                trackTargetPos +
                trackRight *
                overtakeSide *
                OVERTAKE_OFFSET;

            finalTargetPosition =
                Vector3.Lerp(
                    trackTargetPos,
                    overtakePosition,
                    overtakeBlend
                );
        }
        else
        {
            overtakeBlend =
                Mathf.MoveTowards(
                    overtakeBlend,
                    0f,
                    OVERTAKE_BLEND_SPEED *
                    Time.deltaTime
                );
        }

        // ========================================================
        // APPLY DEFENSIVE POSITION
        // ========================================================

        if (defending &&
            defendingAgainst != null &&
            !overtaking)
        {
            defenseBlend =
                Mathf.MoveTowards(
                    defenseBlend,
                    1f,
                    DEFENSE_BLEND_SPEED *
                    Time.deltaTime
                );

            Vector3 defensivePosition =
                trackTargetPos +
                trackRight *
                defenseSide *
                DEFENSE_OFFSET;

            finalTargetPosition =
                Vector3.Lerp(
                    finalTargetPosition,
                    defensivePosition,
                    defenseBlend
                );
        }
        else if (!defending)
        {
            defenseBlend =
                Mathf.MoveTowards(
                    defenseBlend,
                    0f,
                    DEFENSE_BLEND_SPEED *
                    Time.deltaTime
                );
        }

        // ========================================================
        // 11. CALCULATE STEERING
        // ========================================================

        Vector3 targetDirection =
            finalTargetPosition -
            transform.position;

        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude > 0.001f)
        {
            targetDirection.Normalize();
        }

        // ========================================================
        // TRACK DIRECTION
        // ========================================================

        Vector3 steeringDirection;

        if (overtaking)
        {
            steeringDirection =
                Vector3.Slerp(
                    targetDirection,
                    trackTangent,
                    0.50f
                );
        }
        else
        {
            steeringDirection =
                Vector3.Slerp(
                    targetDirection,
                    trackTangent,
                    0.80f
                );
        }

        steeringDirection.y = 0f;

        if (steeringDirection.sqrMagnitude > 0.001f)
        {
            steeringDirection.Normalize();
        }

        // ========================================================
        // 12. EMERGENCY CAR SEPARATION
        // ========================================================

        ApplyCarSeparation(
            ref steeringDirection,
            trackRight
        );

        // ========================================================
        // 13. WALL AVOIDANCE
        // ========================================================

        Vector3 avoidance =
            Vector3.zero;

        if (barrierLayer.value != 0)
        {
            if (Physics.Raycast(
                transform.position,
                transform.right,
                out _,
                WALL_AVOIDANCE_DISTANCE,
                barrierLayer
            ))
            {
                avoidance -=
                    transform.right;
            }

            if (Physics.Raycast(
                transform.position,
                -transform.right,
                out _,
                WALL_AVOIDANCE_DISTANCE,
                barrierLayer
            ))
            {
                avoidance +=
                    transform.right;
            }
        }

        steeringDirection +=
            avoidance *
            WALL_AVOIDANCE_STRENGTH;

        steeringDirection.y = 0f;

        if (steeringDirection.sqrMagnitude > 0.001f)
        {
            steeringDirection.Normalize();
        }

        // ========================================================
        // 14. ROTATE CAR
        // ========================================================

        if (steeringDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    -steeringDirection,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    TURN_SPEED *
                    Time.deltaTime
                );
        }
    }

    // ============================================================
    // FINISH PARKING
    // ============================================================

    private void UpdateFinishParking()
    {
        if (!finishParking)
            return;

        if (finishPositionManager == null)
            return;

        if (finishPosition <= 0)
            return;

        // ========================================================
        // FINISH LINE POSITION
        // ========================================================

        Vector3 finishCenter =
            ConvertToVector3(
                trackSpline.EvaluatePosition(
                    0f
                )
            );

        // ========================================================
        // FINISH LINE TRAVEL DIRECTION
        // ========================================================

        Vector3 tangent =
            ConvertToVector3(
                trackSpline.EvaluateTangent(
                    0f
                )
            );

        tangent.y = 0f;

        if (tangent.sqrMagnitude < 0.001f)
            return;

        tangent.Normalize();

        // Cars travel opposite spline tangent.
        Vector3 travelDirection =
            -tangent;

        travelDirection.y = 0f;
        travelDirection.Normalize();

        // ========================================================
        // SIDE DIRECTION
        // ========================================================

        Vector3 sideDirection =
            Vector3.Cross(
                Vector3.up,
                travelDirection
            );

        sideDirection.y = 0f;

        if (sideDirection.sqrMagnitude < 0.001f)
            return;

        sideDirection.Normalize();

        // ========================================================
        // SIX-CAR CENTERED FINISH GRID
        //
        // For 6 cars:
        //
        // P6    P5    P4    P3    P2    P1
        //
        // The grid is centered around the finish line.
        // ========================================================

        int totalCars =
            finishPositionManager.GetCarCount();

        if (totalCars < 1)
            totalCars = 1;

        float centerOffset =
            (totalCars - 1) * 0.5f;

        float lateralOffset =
            (finishPosition - 1 - centerOffset) *
            FINISH_SLOT_SPACING;

        Vector3 targetPosition =
            finishCenter +
            sideDirection *
            lateralOffset;

        // Move slightly past the finish line.
        targetPosition +=
            travelDirection *
            FINISH_FORWARD_OFFSET;

        // ========================================================
        // TERRAIN HEIGHT
        // ========================================================

        KeepCarOnTerrain(
            ref targetPosition
        );

        // ========================================================
        // HORIZONTAL DISTANCE
        // ========================================================

        Vector3 difference =
            targetPosition -
            transform.position;

        difference.y = 0f;

        float distance =
            difference.magnitude;

        // ========================================================
        // ARRIVED
        // ========================================================

        if (distance <= FINISH_STOP_DISTANCE)
        {
            transform.position =
                targetPosition;

            Quaternion finalRotation =
                Quaternion.LookRotation(
                    travelDirection,
                    Vector3.up
                );

            transform.rotation =
                finalRotation;

            currentSpeed = 0f;

            finishParked =
                true;

            finishParking =
                false;

            return;
        }

        // ========================================================
        // MOVE TO SLOT
        // ========================================================

        Vector3 moveDirection =
            difference.normalized;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                FINISH_MOVE_SPEED *
                Time.deltaTime
            );

        // ========================================================
        // ROTATE TOWARD SLOT
        // ========================================================

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                FINISH_TURN_SPEED *
                Time.deltaTime
            );
    }

    // ============================================================
    // FIND CAR AHEAD
    // ============================================================

    private void FindCarAhead()
    {
        carAhead = null;

        if (overtaking &&
            overtakeTarget != null)
        {
            carAhead =
                overtakeTarget;

            return;
        }

        CircuitCarAI[] allCars =
            FindObjectsByType<CircuitCarAI>(
                FindObjectsSortMode.None
            );

        float closestDistance =
            OVERTAKE_DETECTION_DISTANCE;

        Vector3 forward =
            -transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return;

        forward.Normalize();

        foreach (CircuitCarAI other in allCars)
        {
            if (other == null)
                continue;

            if (other == this)
                continue;

            if (!other.RaceStarted)
                continue;

            Vector3 difference =
                other.transform.position -
                transform.position;

            difference.y = 0f;

            float worldDistance =
                difference.magnitude;

            if (worldDistance >
                OVERTAKE_DETECTION_DISTANCE)
                continue;

            if (difference.sqrMagnitude <
                0.001f)
                continue;

            Vector3 directionToOther =
                difference.normalized;

            float aheadDot =
                Vector3.Dot(
                    forward,
                    directionToOther
                );

            if (aheadDot < 0.35f)
                continue;

            float tDifference =
                Mathf.Repeat(
                    other.CurrentT -
                    CurrentT,
                    1f
                );

            if (tDifference >
                OVERTAKE_T_DISTANCE)
                continue;

            if (worldDistance <
                closestDistance)
            {
                closestDistance =
                    worldDistance;

                carAhead =
                    other;
            }
        }
    }

    // ============================================================
    // HANDLE DEFENDING
    // ============================================================

    private void HandleDefending()
    {
        // --------------------------------------------------------
        // Already defending:
        // keep the SAME side. Do not react to a later side change.
        // --------------------------------------------------------

        if (defending)
        {
            if (defendingAgainst == null ||
                !defendingAgainst.RaceStarted)
            {
                EndDefense();
                return;
            }

            defenseTimer +=
                Time.deltaTime;

            Vector3 defenseTravelDirection =
                -transform.forward;

            defenseTravelDirection.y = 0f;

            if (defenseTravelDirection.sqrMagnitude < 0.001f)
                return;

            defenseTravelDirection.Normalize();

            Vector3 toAttacker =
                defendingAgainst.transform.position -
                transform.position;

            toAttacker.y = 0f;

            float attackerForwardDistance =
                Vector3.Dot(
                    defenseTravelDirection,
                    toAttacker
                );

            // Attacker has gone clearly past us.
            if (defenseTimer >= DEFENSE_MIN_TIME &&
                attackerForwardDistance > DEFENSE_CLEAR_DISTANCE)
            {
                EndDefense();
                return;
            }

            // Attacker has fallen too far behind.
            if (attackerForwardDistance <
                -DEFENSE_DETECTION_DISTANCE * 1.5f)
            {
                EndDefense();
            }

            return;
        }

        // --------------------------------------------------------
        // A car that is already overtaking is not a defender.
        // --------------------------------------------------------

        if (overtaking)
            return;

        if (defenseCooldownTimer > 0f)
            return;

        CircuitCarAI[] allCars =
            FindObjectsByType<CircuitCarAI>(
                FindObjectsSortMode.None
            );

        Vector3 tangent =
            ConvertToVector3(
                trackSpline.EvaluateTangent(
                    CurrentT
                )
            );

        tangent.y = 0f;

        if (tangent.sqrMagnitude < 0.001f)
            return;

        tangent.Normalize();

        Vector3 travelDirection =
            -tangent;

        travelDirection.y = 0f;
        travelDirection.Normalize();

        Vector3 trackRight =
            Vector3.Cross(
                Vector3.up,
                travelDirection
            );

        trackRight.y = 0f;

        if (trackRight.sqrMagnitude < 0.001f)
            return;

        trackRight.Normalize();

        CircuitCarAI bestAttacker = null;
        float bestDistance = DEFENSE_DETECTION_DISTANCE;

        foreach (CircuitCarAI other in allCars)
        {
            if (other == null ||
                other == this)
            {
                continue;
            }

            if (!other.RaceStarted)
                continue;

            Vector3 relative =
                other.transform.position -
                transform.position;

            relative.y = 0f;

            float worldDistance =
                relative.magnitude;

            if (worldDistance >
                DEFENSE_DETECTION_DISTANCE)
            {
                continue;
            }

            if (worldDistance < 0.01f)
                continue;

            // Positive = attacker is ahead.
            // Negative = attacker is behind.
            float forwardDistance =
                Vector3.Dot(
                    travelDirection,
                    relative
                );

            // We only defend against a car coming from behind.
            if (forwardDistance >= 0f)
                continue;

            // Do not defend against a car that is too far behind.
            if (forwardDistance <
                -DEFENSE_DETECTION_DISTANCE)
            {
                continue;
            }

            float tBehind =
                Mathf.Repeat(
                    CurrentT -
                    other.CurrentT,
                    1f
                );

            if (tBehind >
                DEFENSE_T_DISTANCE)
            {
                continue;
            }

            float speedAdvantage =
                other.CurrentSpeed -
                currentSpeed;

            if (speedAdvantage <
                DEFENSE_SPEED_ADVANTAGE)
            {
                continue;
            }

            if (worldDistance < bestDistance)
            {
                bestDistance = worldDistance;
                bestAttacker = other;
            }
        }

        if (bestAttacker == null)
            return;

        // --------------------------------------------------------
        // DEFENSIVE SIDE
        //
        // Move toward the attacker's current side.
        // This is chosen ONCE and then locked.
        // --------------------------------------------------------

        Vector3 attackerRelative =
            bestAttacker.transform.position -
            transform.position;

        attackerRelative.y = 0f;

        float attackerSide =
            Vector3.Dot(
                attackerRelative,
                trackRight
            );

        if (Mathf.Abs(attackerSide) < 0.2f)
        {
            // If almost directly behind, choose the side with
            // more room rather than randomly switching later.
            float leftSpace =
                GetSideClearance(
                    this,
                    -1f
                );

            float rightSpace =
                GetSideClearance(
                    this,
                    1f
                );

            defenseSide =
                leftSpace >= rightSpace
                    ? -1f
                    : 1f;
        }
        else
        {
            defenseSide =
                attackerSide > 0f
                    ? 1f
                    : -1f;
        }

        defendingAgainst =
            bestAttacker;

        defending =
            true;

        defenseTimer =
            0f;

        defenseBlend =
            0f;

        Debug.Log(
            gameObject.name +
            " DEFENDING against " +
            bestAttacker.gameObject.name +
            " on " +
            (
                defenseSide > 0f
                    ? "RIGHT"
                    : "LEFT"
            )
        );
    }

    // ============================================================
    // END DEFENSE
    // ============================================================

    private void EndDefense()
    {
        if (defendingAgainst != null)
        {
            Debug.Log(
                gameObject.name +
                " finished defensive move against " +
                defendingAgainst.gameObject.name
            );
        }

        defending =
            false;

        defendingAgainst =
            null;

        defenseTimer =
            0f;

        defenseBlend =
            0f;

        // Prevent the defender from immediately making another
        // defensive move against the same approaching car.
        defenseCooldownTimer =
            DEFENSE_COOLDOWN;
    }

    // ============================================================
    // HANDLE OVERTAKING
    // ============================================================

    private void HandleOvertaking(
        float curveAmount
    )
    {
        if (overtaking)
        {
            if (overtakeTarget == null)
            {
                CancelOvertake();
                return;
            }

            overtakeTimer +=
                Time.deltaTime;

            Vector3 toTarget =
                overtakeTarget.transform.position -
                transform.position;

            toTarget.y = 0f;

            Vector3 travelDirection =
                -transform.forward;

            travelDirection.y = 0f;

            if (travelDirection.sqrMagnitude >
                0.001f)
            {
                travelDirection.Normalize();
            }

            float forwardDistance =
                Vector3.Dot(
                    travelDirection,
                    toTarget
                );

            // Don't finish an overtake too early.
            if (overtakeTimer >=
                OVERTAKE_MIN_TIME)
            {
                if (forwardDistance <
                    -OVERTAKE_CLEAR_DISTANCE)
                {
                    CompleteOvertake();
                }
            }

            return;
        }

        if (carAhead == null)
            return;

        if (curveAmount >
            MAX_OVERTAKE_CURVE)
        {
            return;
        }

        float speedDifference =
            currentSpeed -
            carAhead.CurrentSpeed;

        if (speedDifference <
            OVERTAKE_SPEED_ADVANTAGE)
        {
            return;
        }

        float leftSpace =
            GetSideClearance(
                carAhead,
                -1f
            );

        float rightSpace =
            GetSideClearance(
                carAhead,
                1f
            );

        bool leftAvailable =
            leftSpace >=
            MIN_CAR_SIDE_DISTANCE;

        bool rightAvailable =
            rightSpace >=
            MIN_CAR_SIDE_DISTANCE;

        if (!leftAvailable &&
            !rightAvailable)
        {
            currentSpeed =
                Mathf.Min(
                    currentSpeed,
                    carAhead.CurrentSpeed
                );

            return;
        }

        if (leftAvailable &&
            rightAvailable)
        {
            if (leftSpace >= rightSpace)
            {
                overtakeSide =
                    -1f;
            }
            else
            {
                overtakeSide =
                    1f;
            }
        }
        else if (leftAvailable)
        {
            overtakeSide =
                -1f;
        }
        else
        {
            overtakeSide =
                1f;
        }

        overtaking =
            true;

        overtakeTarget =
            carAhead;

        overtakeTimer =
            0f;

        overtakeBlend =
            0f;

        Debug.Log(
            gameObject.name +
            " attempting overtake of " +
            overtakeTarget.gameObject.name +
            " on " +
            (
                overtakeSide > 0f
                    ? "RIGHT"
                    : "LEFT"
            )
        );
    }

    // ============================================================
    // GET SIDE CLEARANCE
    // ============================================================

    private float GetSideClearance(
        CircuitCarAI target,
        float side
    )
    {
        CircuitCarAI[] allCars =
            FindObjectsByType<CircuitCarAI>(
                FindObjectsSortMode.None
            );

        Vector3 tangent =
            ConvertToVector3(
                trackSpline.EvaluateTangent(
                    CurrentT
                )
            );

        tangent.y = 0f;

        if (tangent.sqrMagnitude <
            0.001f)
        {
            return 0f;
        }

        tangent.Normalize();

        Vector3 travelDirection =
            -tangent;

        travelDirection.y = 0f;
        travelDirection.Normalize();

        Vector3 trackRight =
            Vector3.Cross(
                Vector3.up,
                travelDirection
            );

        trackRight.y = 0f;

        if (trackRight.sqrMagnitude <
            0.001f)
        {
            return 0f;
        }

        trackRight.Normalize();

        Vector3 targetPosition =
            target.transform.position;

        float targetLateral =
            Vector3.Dot(
                targetPosition -
                transform.position,
                trackRight
            );

        float desiredLateral =
            targetLateral +
            side *
            OVERTAKE_OFFSET;

        float availableSpace =
            100f;

        foreach (CircuitCarAI other in allCars)
        {
            if (other == null)
                continue;

            if (other == this)
                continue;

            if (!other.RaceStarted)
                continue;

            Vector3 relative =
                other.transform.position -
                transform.position;

            relative.y = 0f;

            float forwardDistance =
                Vector3.Dot(
                    travelDirection,
                    relative
                );

            if (forwardDistance <
                -SIDE_CHECK_BACK_DISTANCE)
            {
                continue;
            }

            if (forwardDistance >
                SIDE_CHECK_FORWARD_DISTANCE)
            {
                continue;
            }

            float lateral =
                Vector3.Dot(
                    relative,
                    trackRight
                );

            float lateralDistance =
                Mathf.Abs(
                    lateral -
                    desiredLateral
                );

            if (lateralDistance <
                MIN_CAR_SIDE_DISTANCE)
            {
                availableSpace =
                    Mathf.Min(
                        availableSpace,
                        lateralDistance
                    );
            }
        }

        // Conservative track-edge safety.
        if (Mathf.Abs(
                desiredLateral
            ) >
            OVERTAKE_OFFSET * 2f)
        {
            availableSpace =
                0f;
        }

        return availableSpace;
    }

    // ============================================================
    // CAR SEPARATION
    // ============================================================

    private void ApplyCarSeparation(
        ref Vector3 steeringDirection,
        Vector3 trackRight
    )
    {
        CircuitCarAI[] allCars =
            FindObjectsByType<CircuitCarAI>(
                FindObjectsSortMode.None
            );

        if (trackRight.sqrMagnitude < 0.001f)
            return;

        trackRight.Normalize();

        Vector3 desiredSeparation =
            Vector3.zero;

        foreach (CircuitCarAI other in allCars)
        {
            if (other == null)
                continue;

            if (other == this)
                continue;

            if (!other.RaceStarted)
                continue;

            Vector3 relative =
                other.transform.position -
                transform.position;

            relative.y = 0f;

            float distance =
                relative.magnitude;

            if (distance > MIN_CAR_SIDE_DISTANCE)
                continue;

            if (distance < 0.01f)
                continue;

            // ========================================================
            // LATERAL DISTANCE
            // ========================================================

            float lateralDistance =
                Vector3.Dot(
                    relative,
                    trackRight
                );

            float absLateral =
                Mathf.Abs(lateralDistance);

            // Already sufficiently separated sideways.
            if (absLateral > 2.0f)
                continue;

            // ========================================================
            // STABLE PUSH DIRECTION
            // ========================================================

            float pushDirection;

            if (Mathf.Abs(lateralDistance) < 0.05f)
            {
                pushDirection =
                    GetInstanceID() <
                    other.GetInstanceID()
                        ? -1f
                        : 1f;
            }
            else
            {
                pushDirection =
                    lateralDistance > 0f
                        ? -1f
                        : 1f;
            }

            // ========================================================
            // DISTANCE BASED STRENGTH
            // ========================================================

            float closeness =
                1f -
                Mathf.Clamp01(
                    distance /
                    MIN_CAR_SIDE_DISTANCE
                );

            float strength =
                Mathf.Lerp(
                    0.05f,
                    0.35f,
                    closeness
                );

            desiredSeparation +=
                trackRight *
                pushDirection *
                strength;
        }

        // ============================================================
        // SMOOTH SEPARATION
        // ============================================================

        Vector3 targetSeparation =
            desiredSeparation;

        if (targetSeparation.sqrMagnitude >
            0.001f)
        {
            targetSeparation.Normalize();
        }

        /*
        * Smooth the separation direction over time.
        *
        * This is the important part:
        *
        * OLD:
        *     separation changes → steering immediately changes
        *
        * NEW:
        *     separation changes → camera/car gradually follows
        */

        float smoothing =
            1f -
            Mathf.Exp(
                -8f *
                Time.deltaTime
            );

        smoothedSeparation =
            Vector3.Slerp(
                smoothedSeparation,
                targetSeparation,
                smoothing
            );

        smoothedSeparation.y = 0f;

        // ============================================================
        // APPLY VERY SMALL CORRECTION
        // ============================================================

        if (smoothedSeparation.sqrMagnitude >
            0.001f)
        {
            smoothedSeparation.Normalize();

            Vector3 correctedDirection =
                steeringDirection +
                smoothedSeparation *
                0.20f;

            correctedDirection.y = 0f;

            if (correctedDirection.sqrMagnitude >
                0.001f)
            {
                steeringDirection =
                    Vector3.Slerp(
                        steeringDirection,
                        correctedDirection.normalized,
                        0.20f
                    );
            }
        }

        steeringDirection.y = 0f;

        if (steeringDirection.sqrMagnitude >
            0.001f)
        {
            steeringDirection.Normalize();
        }
    }

    // ============================================================
    // COMPLETE OVERTAKE
    // ============================================================

    private void CompleteOvertake()
    {
        if (overtakeTarget != null)
        {
            Debug.Log(
                gameObject.name +
                " completed overtake of " +
                overtakeTarget.gameObject.name
            );
        }

        overtaking =
            false;

        overtakeTarget =
            null;

        carAhead =
            null;

        overtakeTimer =
            0f;
    }

    // ============================================================
    // CANCEL OVERTAKE
    // ============================================================

    private void CancelOvertake()
    {
        overtaking =
            false;

        overtakeTarget =
            null;

        overtakeTimer =
            0f;

        overtakeBlend =
            0f;
    }

    // ============================================================
    // KEEP CAR ON TERRAIN
    // ============================================================

    private void KeepCarOnTerrain(
        ref Vector3 position
    )
    {
        if (terrainLayer.value == 0)
            return;

        Vector3 rayOrigin =
            new Vector3(
                position.x,
                position.y + 10f,
                position.z
            );

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            30f,
            terrainLayer
        ))
        {
            position.y =
                hit.point.y +
                CAR_HEIGHT_ABOVE_ROAD;
        }
    }

    // ============================================================
    // FLOAT3 → VECTOR3
    // ============================================================

    private Vector3 ConvertToVector3(
        Unity.Mathematics.float3 value
    )
    {
        return new Vector3(
            value.x,
            value.y,
            value.z
        );
    }
}  