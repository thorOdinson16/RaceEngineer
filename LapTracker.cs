using UnityEngine;

public class LapTracker : MonoBehaviour
{
    // ============================================================
    // RACE SETTINGS
    // ============================================================

    // RaceStateManager is the single source of truth.
    // This is only a safe fallback before initialization.
    private int totalLaps = 1;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private CircuitCarAI ai;
    private RaceStateManager raceStateManager;

    private float previousT;

    private int currentLap;

    private bool trackingStarted;
    private bool finished;

    // ============================================================
    // LAP TIMING
    // ============================================================

    private float lapStartRaceTime;
    private float lastLapTime;
    private float bestLapTime;

    // ============================================================
    // PUBLIC VALUES
    // ============================================================

    public int CurrentLap
    {
        get
        {
            return currentLap;
        }
    }

    public int TotalLaps
    {
        get
        {
            return totalLaps;
        }
    }

    public bool Finished
    {
        get
        {
            return finished;
        }
    }

    // ============================================================
    // CURRENT LAP TIME
    // ============================================================

    public float CurrentLapTime
    {
        get
        {
            if (
                !trackingStarted ||
                raceStateManager == null
            )
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                raceStateManager.RaceTime -
                lapStartRaceTime
            );
        }
    }

    // ============================================================
    // LAST COMPLETED LAP
    // ============================================================

    public float LastLapTime
    {
        get
        {
            return lastLapTime;
        }
    }

    // ============================================================
    // BEST LAP
    // ============================================================

    public float BestLapTime
    {
        get
        {
            return bestLapTime;
        }
    }

    // ============================================================
    // HAS LAP TIME
    // ============================================================

    public bool HasLapTime
    {
        get
        {
            return lastLapTime > 0f;
        }
    }

    // ============================================================
    // RACE PROGRESS
    // ============================================================

    /*
     * Continuous progress around the circuit.
     *
     * Lap 1 + T 0.50 = 0.50
     * Lap 2 + T 0.20 = 1.20
     *
     * This is used by RaceStateManager to compare
     * cars on the circuit.
     */

    public float RaceProgress
    {
        get
        {
            if (finished)
            {
                return totalLaps;
            }

            if (ai == null)
            {
                return 0f;
            }

            return Mathf.Max(
                0f,
                (currentLap - 1) +
                ai.CurrentT
            );
        }
    }

    // ============================================================
    // SET TOTAL LAPS
    // ============================================================

    public void SetTotalLaps(int laps)
    {
        totalLaps =
            Mathf.Max(
                1,
                laps
            );
    }

    // ============================================================
    // SET RACE STATE MANAGER
    // ============================================================

    public void SetRaceStateManager(
        RaceStateManager manager
    )
    {
        raceStateManager =
            manager;
    }

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        ai =
            GetComponent<CircuitCarAI>();

        currentLap =
            1;

        previousT =
            0f;

        trackingStarted =
            false;

        finished =
            false;

        lapStartRaceTime =
            0f;

        lastLapTime =
            0f;

        bestLapTime =
            0f;
    }

    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        if (ai == null)
            return;

        if (raceStateManager == null)
            return;

        // --------------------------------------------------------
        // Only track once the CENTRAL race state says racing.
        // --------------------------------------------------------

        if (!raceStateManager.IsRacing)
            return;

        // --------------------------------------------------------
        // Don't do anything after finishing.
        // --------------------------------------------------------

        if (finished)
            return;

        float currentT =
            ai.CurrentT;

        // ========================================================
        // FIRST FRAME OF RACE
        // ========================================================

        if (!trackingStarted)
        {
            previousT =
                currentT;

            trackingStarted =
                true;

            lapStartRaceTime =
                raceStateManager.RaceTime;

            Debug.Log(
                gameObject.name +
                " started Lap 1 / " +
                totalLaps
            );

            return;
        }

        // ========================================================
        // LAP COMPLETION DETECTION
        // ========================================================

        /*
         * Spline T normally moves:
         *
         * 0.00 → 0.25 → 0.50 → 0.75 → 0.99
         *
         * Then wraps:
         *
         * 0.99 → 0.00
         *
         * That wrap means the car crossed
         * the start/finish line.
         */

        bool crossedStartFinish =
            previousT > 0.75f &&
            currentT < 0.25f;

        if (crossedStartFinish)
        {
            CompleteLap();
        }

        // --------------------------------------------------------
        // Save current spline position.
        // --------------------------------------------------------

        previousT =
            currentT;
    }

    // ============================================================
    // COMPLETE LAP
    // ============================================================

    private void CompleteLap()
    {
        // --------------------------------------------------------
        // Calculate lap time from CENTRAL race clock.
        // --------------------------------------------------------

        float currentRaceTime =
            raceStateManager.RaceTime;

        float completedLapTime =
            currentRaceTime -
            lapStartRaceTime;

        completedLapTime =
            Mathf.Max(
                0f,
                completedLapTime
            );

        lastLapTime =
            completedLapTime;

        if (
            bestLapTime <= 0f ||
            completedLapTime < bestLapTime
        )
        {
            bestLapTime =
                completedLapTime;
        }

        // --------------------------------------------------------
        // The lap that just finished.
        // --------------------------------------------------------

        int completedLap =
            currentLap;

        Debug.Log(
            gameObject.name +
            " completed Lap " +
            completedLap +
            " / " +
            totalLaps +
            " | Lap Time: " +
            completedLapTime.ToString("F3") +
            "s"
        );

        // ========================================================
        // FINAL LAP
        // ========================================================

        if (completedLap >= totalLaps)
        {
            finished =
                true;

            Debug.Log(
                gameObject.name +
                " FINISHED THE RACE!" +
                " | Final Lap: " +
                completedLapTime.ToString("F3") +
                "s" +
                " | Best: " +
                bestLapTime.ToString("F3") +
                "s"
            );

            return;
        }

        // ========================================================
        // NEXT LAP
        // ========================================================

        currentLap++;

        lapStartRaceTime =
            currentRaceTime;

        Debug.Log(
            gameObject.name +
            " is now on Lap " +
            currentLap +
            " / " +
            totalLaps
        );
    }
}