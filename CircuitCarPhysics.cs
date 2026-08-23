using UnityEngine;

public class CircuitCarPhysics : MonoBehaviour
{
    // ============================================================
    // BASIC PHYSICS
    // ============================================================

    private const float ACCELERATION = 35f;
    private const float BRAKING = 50f;

    private const float LATERAL_GRIP = 8f;
    private const float MAX_LATERAL_SPEED = 15f;

    private const float MAX_SPEED = 45f;

    private const float GRIP_SPEED_REFERENCE = 30f;
    private const float HIGH_SPEED_GRIP_FACTOR = 0.35f;

    private const float CAR_HEIGHT_ABOVE_TERRAIN = 0.1f;

    // ============================================================
    // TIRE PHYSICS
    // ============================================================

    // Minimum speed at which cornering tire effects matter.
    private const float CORNERING_MIN_SPEED = 5f;

    // ------------------------------------------------------------
    // UNDERSTEER
    // ------------------------------------------------------------

    /*
     * Front tire grip reduction.
     *
     * We intentionally keep this conservative.
     *
     * Understeer is created by reducing FRONT grip.
     * We do NOT create sideways velocity.
     */

    private const float FRONT_MIN_GRIP_FACTOR = 0.65f;

    private const float FRONT_CORNERING_EFFECT = 0.30f;

    private const float FRONT_SPEED_EFFECT = 0.25f;

    // ------------------------------------------------------------
    // OVERSTEER
    // ------------------------------------------------------------

    /*
     * Rear tire grip reduction.
     *
     * Oversteer is created by reducing REAR grip.
     * Existing lateral velocity is allowed to persist longer.
     *
     * We do NOT inject lateral velocity.
     */

    private const float REAR_MIN_GRIP_FACTOR = 0.60f;

    private const float REAR_CORNERING_EFFECT = 0.25f;

    private const float REAR_SPEED_EFFECT = 0.20f;

    private const float REAR_ACCELERATION_EFFECT = 0.15f;

    // ============================================================
    // WEIGHT TRANSFER
    // ============================================================

    private const float LONGITUDINAL_WEIGHT_TRANSFER = 0.10f;

    // ============================================================
    // TRACTION CIRCLE
    // ============================================================

    private const float TRACTION_CIRCLE_EFFECT = 0.20f;

    // ============================================================
    // COLLISION SETTINGS
    // ============================================================

    private const float COLLISION_PUSH_STRENGTH = 0.65f;

    private const float COLLISION_IMPACT_SPEED = 12f;

    private const float MIN_COLLISION_DEFLECTION = 0.15f;
    private const float MAX_COLLISION_DEFLECTION = 2.0f;

    private const float COLLISION_RESPONSE_COOLDOWN = 0.10f;

    // ============================================================
    // COLLISION SPEED LOSS
    // ============================================================

    private const float MAX_COLLISION_SPEED_LOSS = 0.18f;

    private const float COLLISION_SPEED_RECOVERY = 2.5f;

    private float collisionSpeedPenalty;

    // ============================================================
    // BRAKE / LOCK-UP
    // ============================================================

    private const float FRONT_BRAKE_BIAS = 0.65f;
    private const float REAR_BRAKE_BIAS = 0.35f;

    private const float BRAKE_GRIP_LIMIT = 40f;

    private const float LOCKUP_RECOVERY = 6f;
    private const float LOCKUP_BUILD_RATE = 12f;

    private const float FRONT_LOCKUP_GRIP_FACTOR = 0.20f;
    private const float REAR_LOCKUP_GRIP_FACTOR = 0.35f;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private CircuitCarAI ai;

    private Vector3 velocity;

    private LayerMask terrainLayer;

    private float frontLockAmount;
    private float rearLockAmount;

    private float collisionCooldown;

    private Collider carCollider;

    private const string TERRAIN_LAYER_NAME = "Terrain";

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        ai =
            GetComponent<CircuitCarAI>();

        carCollider =
            GetComponent<Collider>();

        terrainLayer =
            LayerMask.GetMask(
                TERRAIN_LAYER_NAME
            );

        velocity =
            Vector3.zero;

        frontLockAmount =
            0f;

        rearLockAmount =
            0f;

        collisionCooldown =
            0f;

        collisionSpeedPenalty =
            0f;

        if (carCollider == null)
        {
            Debug.LogError(
                gameObject.name +
                ": CircuitCarPhysics requires a Collider!"
            );
        }
    }

    // ============================================================
    // FIXED UPDATE
    // ============================================================

    void FixedUpdate()
    {
        float dt =
            Time.fixedDeltaTime;

        if (ai == null)
            return;

        // ========================================================
        // COLLISION TIMER
        // ========================================================

        collisionCooldown =
            Mathf.Max(
                0f,
                collisionCooldown -
                dt
            );

        // ========================================================
        // COLLISION SPEED RECOVERY
        // ========================================================

        collisionSpeedPenalty =
            Mathf.MoveTowards(
                collisionSpeedPenalty,
                0f,
                COLLISION_SPEED_RECOVERY *
                dt
            );

        // ========================================================
        // 1. GET AI REQUEST
        // ========================================================

        float targetSpeed =
            ai.DesiredSpeed;

        // ========================================================
        // COLLISION SPEED PENALTY
        // ========================================================

        targetSpeed *=
            1f -
            collisionSpeedPenalty;

        targetSpeed =
            Mathf.Clamp(
                targetSpeed,
                0f,
                MAX_SPEED
            );

        // ========================================================
        // 2. GET TRAVEL DIRECTION
        // ========================================================

        /*
         * DO NOT CHANGE THIS.
         *
         * CircuitCarAI defines the travel direction.
         *
         * Physics must use the AI's established convention.
         */

        Vector3 forward =
            ai.DesiredDirection;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return;

        forward.Normalize();

        // ========================================================
        // 3. CAR RIGHT
        // ========================================================

        Vector3 right =
            transform.right;

        right.y = 0f;

        if (right.sqrMagnitude > 0.001f)
            right.Normalize();
        else
            right =
                Vector3.Cross(
                    Vector3.up,
                    forward
                ).normalized;

        // ========================================================
        // 4. CURRENT VELOCITY
        // ========================================================

        float forwardSpeed =
            Vector3.Dot(
                velocity,
                forward
            );

        float lateralSpeed =
            Vector3.Dot(
                velocity,
                right
            );

        // ========================================================
        // 5. ACCELERATION / BRAKING
        // ========================================================

        float rate;

        if (targetSpeed < forwardSpeed)
        {
            rate =
                BRAKING;
        }
        else
        {
            rate =
                ACCELERATION;
        }

        // ========================================================
        // 6. BRAKING DEMAND
        // ========================================================

        float brakingDemand =
            0f;

        if (targetSpeed < forwardSpeed)
        {
            brakingDemand =
                Mathf.Abs(
                    forwardSpeed -
                    targetSpeed
                ) /
                Mathf.Max(
                    dt,
                    0.0001f
                );
        }

        // ========================================================
        // 7. FRONT / REAR BRAKING
        // ========================================================

        float frontBrakingDemand =
            brakingDemand *
            FRONT_BRAKE_BIAS;

        float rearBrakingDemand =
            brakingDemand *
            REAR_BRAKE_BIAS;

        // ========================================================
        // 8. FRONT LOCK-UP
        // ========================================================

        float frontLockTarget =
            Mathf.Clamp01(
                frontBrakingDemand /
                BRAKE_GRIP_LIMIT
            );

        if (
            frontLockTarget >
            frontLockAmount
        )
        {
            frontLockAmount =
                Mathf.MoveTowards(
                    frontLockAmount,
                    frontLockTarget,
                    LOCKUP_BUILD_RATE *
                    dt
                );
        }
        else
        {
            frontLockAmount =
                Mathf.MoveTowards(
                    frontLockAmount,
                    0f,
                    LOCKUP_RECOVERY *
                    dt
                );
        }

        // ========================================================
        // 9. REAR LOCK-UP
        // ========================================================

        float rearLockTarget =
            Mathf.Clamp01(
                rearBrakingDemand /
                BRAKE_GRIP_LIMIT
            );

        if (
            rearLockTarget >
            rearLockAmount
        )
        {
            rearLockAmount =
                Mathf.MoveTowards(
                    rearLockAmount,
                    rearLockTarget,
                    LOCKUP_BUILD_RATE *
                    dt
                );
        }
        else
        {
            rearLockAmount =
                Mathf.MoveTowards(
                    rearLockAmount,
                    0f,
                    LOCKUP_RECOVERY *
                    dt
                );
        }

        // ========================================================
        // 10. OVERALL LOCK-UP
        // ========================================================

        float overallLockAmount =
            Mathf.Max(
                frontLockAmount,
                rearLockAmount
            );

        // ========================================================
        // 11. EFFECTIVE BRAKING
        // ========================================================

        float effectiveBrakeRate =
            Mathf.Lerp(
                rate,
                rate * 0.45f,
                overallLockAmount
            );

        float newForwardSpeed =
            Mathf.MoveTowards(
                forwardSpeed,
                targetSpeed,
                effectiveBrakeRate *
                dt
            );

        // ========================================================
        // SAFETY: NO REVERSE MOTION
        // ========================================================

        newForwardSpeed =
            Mathf.Max(
                0f,
                newForwardSpeed
            );

        // ========================================================
        // 12. SPEED-DEPENDENT BASE GRIP
        // ========================================================

        float speedRatio =
            Mathf.Clamp01(
                Mathf.Abs(
                    forwardSpeed
                ) /
                GRIP_SPEED_REFERENCE
            );

        float baseGrip =
            Mathf.Lerp(
                LATERAL_GRIP,
                LATERAL_GRIP *
                HIGH_SPEED_GRIP_FACTOR,
                speedRatio
            );

        // ========================================================
        // 13. ACCELERATION DEMAND
        // ========================================================

        float accelerationDemand =
            0f;

        if (targetSpeed > forwardSpeed)
        {
            accelerationDemand =
                Mathf.Clamp01(
                    (
                        targetSpeed -
                        forwardSpeed
                    ) /
                    10f
                );
        }

        // ========================================================
        // 14. BRAKING DEMAND NORMALIZED
        // ========================================================

        float brakingDemandNormalized =
            Mathf.Clamp01(
                brakingDemand /
                BRAKE_GRIP_LIMIT
            );

        // ========================================================
        // 15. SIMPLE WEIGHT TRANSFER
        // ========================================================

        /*
         * Acceleration:
         *      load -> rear
         *
         * Braking:
         *      load -> front
         */

        float weightTransfer =
            (
                accelerationDemand -
                brakingDemandNormalized
            ) *
            LONGITUDINAL_WEIGHT_TRANSFER;

        float frontLoadFactor =
            Mathf.Clamp(
                1f -
                weightTransfer,
                0.85f,
                1.15f
            );

        float rearLoadFactor =
            Mathf.Clamp(
                1f +
                weightTransfer,
                0.85f,
                1.15f
            );

        // ========================================================
        // 16. EXISTING LATERAL DEMAND
        // ========================================================

        /*
         * This is intentionally based on EXISTING slip.
         *
         * We never manufacture lateral velocity.
         */

        float lateralDemand =
            Mathf.Clamp01(
                Mathf.Abs(
                    lateralSpeed
                ) /
                MAX_LATERAL_SPEED
            );

        // ========================================================
        // 17. CORNERING DEMAND
        // ========================================================

        /*
         * Compare actual velocity direction against requested
         * travel direction.
         *
         * This tells us whether the tires are already struggling
         * to follow the AI's requested path.
         */

        Vector3 horizontalVelocity =
            velocity;

        horizontalVelocity.y = 0f;

        float directionMismatch =
            0f;

        if (
            horizontalVelocity.sqrMagnitude >
            0.01f
        )
        {
            horizontalVelocity.Normalize();

            directionMismatch =
                Vector3.Angle(
                    horizontalVelocity,
                    forward
                ) /
                45f;

            directionMismatch =
                Mathf.Clamp01(
                    directionMismatch
                );
        }

        float speedCornerFactor =
            Mathf.InverseLerp(
                CORNERING_MIN_SPEED,
                GRIP_SPEED_REFERENCE,
                Mathf.Abs(
                    forwardSpeed
                )
            );

        float corneringDemand =
            Mathf.Max(
                lateralDemand,
                directionMismatch
            ) *
            speedCornerFactor;

        // ========================================================
        // 18. FRONT TIRE SATURATION
        // ========================================================

        /*
         * UNDERSTEER
         *
         * High speed + tire demand
         *          ↓
         * front tires lose grip
         *          ↓
         * lateral velocity is corrected more slowly
         *
         * No lateral velocity is generated.
         */

        float frontSaturation =
            Mathf.Clamp01(
                corneringDemand *
                (
                    FRONT_CORNERING_EFFECT +
                    FRONT_SPEED_EFFECT
                )
            );

        float frontCorneringFactor =
            Mathf.Lerp(
                1f,
                FRONT_MIN_GRIP_FACTOR,
                frontSaturation
            );

        float frontGrip =
            baseGrip *
            frontCorneringFactor *
            frontLoadFactor;

        // Front lock-up reduces lateral grip.
        frontGrip =
            Mathf.Lerp(
                frontGrip,
                frontGrip *
                FRONT_LOCKUP_GRIP_FACTOR,
                frontLockAmount
            );

        // ========================================================
        // 19. REAR TIRE SATURATION
        // ========================================================

        /*
         * OVERSTEER
         *
         * High speed
         *      +
         * cornering demand
         *      +
         * acceleration
         *      ↓
         * rear tires lose grip
         *      ↓
         * existing lateral slip persists
         *
         * Again, no new sideways velocity is created.
         */

        float rearSaturation =
            Mathf.Clamp01(
                corneringDemand *
                (
                    REAR_CORNERING_EFFECT +
                    REAR_SPEED_EFFECT
                )
                +
                accelerationDemand *
                REAR_ACCELERATION_EFFECT
            );

        float rearCorneringFactor =
            Mathf.Lerp(
                1f,
                REAR_MIN_GRIP_FACTOR,
                rearSaturation
            );

        float rearGrip =
            baseGrip *
            rearCorneringFactor *
            rearLoadFactor;

        // Rear lock-up reduces lateral grip.
        rearGrip =
            Mathf.Lerp(
                rearGrip,
                rearGrip *
                REAR_LOCKUP_GRIP_FACTOR,
                rearLockAmount
            );

        // ========================================================
        // 20. TRACTION CIRCLE
        // ========================================================

        /*
         * If the rear tires are already being used heavily
         * for acceleration AND lateral movement, reduce their
         * available lateral grip.
         */

        float tractionUsage =
            Mathf.Clamp01(
                accelerationDemand *
                lateralDemand
            );

        float tractionGripFactor =
            Mathf.Lerp(
                1f,
                1f -
                TRACTION_CIRCLE_EFFECT,
                tractionUsage
            );

        rearGrip *=
            tractionGripFactor;

        // ========================================================
        // 21. COMBINE FRONT / REAR GRIP
        // ========================================================

        float effectiveGrip =
            Mathf.Lerp(
                rearGrip,
                frontGrip,
                0.65f
            );

        // ========================================================
        // 22. LATERAL SPEED LIMIT
        // ========================================================

        lateralSpeed =
            Mathf.Clamp(
                lateralSpeed,
                -MAX_LATERAL_SPEED,
                MAX_LATERAL_SPEED
            );

        // ========================================================
        // 23. LATERAL TIRE RESPONSE
        // ========================================================

        /*
         * THIS IS THE IMPORTANT PART.
         *
         * Grip removes EXISTING lateral velocity.
         *
         * More grip:
         *      faster correction
         *
         * Less grip:
         *      slower correction
         *
         * Therefore:
         *
         *      understeer = front grip reduction
         *      oversteer  = rear grip reduction
         */

        float gripResponse =
            Mathf.Clamp01(
                effectiveGrip *
                dt
            );

        lateralSpeed =
            Mathf.Lerp(
                lateralSpeed,
                0f,
                gripResponse
            );

        // ========================================================
        // 24. REAR LOCK-UP INSTABILITY
        // ========================================================

        /*
         * Do NOT add:
         *
         *      right.x * instability
         *
         * That is world-space dependent and was one of the
         * problematic behaviors in the previous implementation.
         *
         * We only reduce lateral correction.
         */

        if (rearLockAmount > 0.01f)
        {
            float lockGripResponse =
                Mathf.Lerp(
                    1f,
                    0.70f,
                    rearLockAmount
                );

            lateralSpeed *=
                lockGripResponse;
        }

        // ========================================================
        // 25. FINAL LATERAL SAFETY LIMIT
        // ========================================================

        lateralSpeed =
            Mathf.Clamp(
                lateralSpeed,
                -MAX_LATERAL_SPEED,
                MAX_LATERAL_SPEED
            );

        // ========================================================
        // 26. BUILD VELOCITY
        // ========================================================

        /*
         * CRITICAL:
         *
         * The car moves using AI DesiredDirection.
         *
         * We do not use transform.forward here.
         */

        velocity =
            forward *
            newForwardSpeed;

        velocity +=
            right *
            lateralSpeed;

        // ========================================================
        // 27. MAX SPEED
        // ========================================================

        if (
            velocity.magnitude >
            MAX_SPEED
        )
        {
            velocity =
                velocity.normalized *
                MAX_SPEED;
        }

        // ========================================================
        // 28. PREDICT MOVEMENT
        // ========================================================

        Vector3 newPosition =
            transform.position +
            velocity *
            dt;

        // ========================================================
        // 29. CAR COLLISION
        // ========================================================

        ApplyCarCollision(
            ref newPosition
        );

        // ========================================================
        // 30. TERRAIN HEIGHT
        // ========================================================

        KeepCarOnTerrain(
            ref newPosition
        );

        // ========================================================
        // FINAL POSITION
        // ========================================================

        transform.position =
            newPosition;
    }

    // ============================================================
    // CAR COLLISION
    // ============================================================

    private void ApplyCarCollision(
        ref Vector3 newPosition
    )
    {
        if (carCollider == null)
            return;

        if (collisionCooldown > 0f)
            return;

        // ========================================================
        // PREDICTED COLLIDER BOUNDS
        // ========================================================

        Vector3 movement =
            newPosition -
            transform.position;

        Bounds predictedBounds =
            carCollider.bounds;

        predictedBounds.center +=
            movement;

        predictedBounds.Expand(
            0.05f
        );

        Collider[] nearbyCars =
            Physics.OverlapBox(
                predictedBounds.center,
                predictedBounds.extents,
                transform.rotation,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        foreach (
            Collider otherCollider
            in nearbyCars
        )
        {
            if (otherCollider == null)
                continue;

            if (otherCollider == carCollider)
                continue;

            // ====================================================
            // FIND OTHER CAR
            // ====================================================

            CircuitCarPhysics otherPhysics =
                otherCollider.GetComponent<
                    CircuitCarPhysics
                >();

            if (otherPhysics == null)
            {
                otherPhysics =
                    otherCollider.GetComponentInParent<
                        CircuitCarPhysics
                    >();
            }

            if (otherPhysics == null)
                continue;

            if (otherPhysics == this)
                continue;

            CircuitCarAI otherAI =
                otherPhysics.GetComponent<
                    CircuitCarAI
                >();

            if (otherAI == null)
                continue;

            if (!otherAI.RaceStarted)
                continue;

            // ====================================================
            // CHECK COLLISION AT PREDICTED POSITION
            // ====================================================

            bool overlapping =
                Physics.ComputePenetration(
                    carCollider,

                    newPosition,

                    transform.rotation,

                    otherCollider,

                    otherCollider.transform.position,

                    otherCollider.transform.rotation,

                    out Vector3 separationDirection,

                    out float penetrationDistance
                );

            if (!overlapping)
                continue;

            // ====================================================
            // COLLISION NORMAL
            // ====================================================

            separationDirection.y = 0f;

            if (
                separationDirection.sqrMagnitude <
                0.001f
            )
            {
                Vector3 fallback =
                    transform.position -
                    otherCollider.transform.position;

                fallback.y = 0f;

                if (
                    fallback.sqrMagnitude <
                    0.001f
                )
                {
                    fallback =
                        transform.right;
                }

                separationDirection =
                    fallback.normalized;
            }
            else
            {
                separationDirection.Normalize();
            }

            // ====================================================
            // PUSH CAR OUT
            // ====================================================

            float correction =
                penetrationDistance *
                COLLISION_PUSH_STRENGTH;

            correction =
                Mathf.Max(
                    correction,
                    0.01f
                );

            newPosition +=
                separationDirection *
                correction;

            // ====================================================
            // COLLISION SPEED
            // ====================================================

            Vector3 otherVelocity =
                otherPhysics.GetVelocity();

            Vector3 relativeVelocity =
                velocity -
                otherVelocity;

            float velocityIntoOther =
                Vector3.Dot(
                    relativeVelocity,
                    -separationDirection
                );

            if (velocityIntoOther > 0f)
            {
                // =================================================
                // IMPACT STRENGTH
                // =================================================

                float impactStrength =
                    Mathf.Clamp01(
                        velocityIntoOther /
                        COLLISION_IMPACT_SPEED
                    );

                // =================================================
                // DEFLECTION
                // =================================================

                float collisionDeflection =
                    Mathf.Lerp(
                        MIN_COLLISION_DEFLECTION,
                        MAX_COLLISION_DEFLECTION,
                        impactStrength
                    );

                velocity +=
                    separationDirection *
                    velocityIntoOther *
                    collisionDeflection;

                // =================================================
                // TEMPORARY SPEED LOSS
                // =================================================

                float speedLoss =
                    Mathf.Lerp(
                        0.02f,
                        MAX_COLLISION_SPEED_LOSS,
                        impactStrength
                    );

                collisionSpeedPenalty =
                    Mathf.Max(
                        collisionSpeedPenalty,
                        speedLoss
                    );
            }

            // ====================================================
            // COLLISION COOLDOWN
            // ====================================================

            collisionCooldown =
                COLLISION_RESPONSE_COOLDOWN;

            break;
        }
    }

    // ============================================================
    // TERRAIN
    // ============================================================

    private void KeepCarOnTerrain(
        ref Vector3 position
    )
    {
        if (terrainLayer.value == 0)
            return;

        Vector3 origin =
            new Vector3(
                position.x,
                position.y + 10f,
                position.z
            );

        if (
            Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                30f,
                terrainLayer
            )
        )
        {
            position.y =
                hit.point.y +
                CAR_HEIGHT_ABOVE_TERRAIN;
        }
    }

    // ============================================================
    // VELOCITY ACCESS
    // ============================================================

    public Vector3 GetVelocity()
    {
        return velocity;
    }
}