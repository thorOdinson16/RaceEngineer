using UnityEngine;

public class RaceStateManager : MonoBehaviour
{
    // ============================================================
    // RACE SETTINGS
    // ============================================================

    [Header("Race Settings")]

    [SerializeField]
    private int totalLaps = 12;

    // ============================================================
    // REFERENCES
    // ============================================================

    [Header("References")]

    [SerializeField]
    private RacePositionManager racePositionManager;

    [SerializeField]
    private CircuitCarAI playerCar;

    // ============================================================
    // RACE STATE
    // ============================================================

    public enum RaceStatus
    {
        PreRace,
        Racing,
        Finished
    }

    private RaceStatus currentStatus =
        RaceStatus.PreRace;

    // ============================================================
    // AUTHORITATIVE RACE CLOCK
    // ============================================================

    private float raceStartTime;
    private float raceTime;

    // ============================================================
    // PLAYER STARTING POSITION
    // ============================================================

    private int playerStartingPosition = -1;

    // ============================================================
    // PUBLIC VALUES
    // ============================================================

    public RaceStatus Status
    {
        get
        {
            return currentStatus;
        }
    }

    public bool IsRacing
    {
        get
        {
            return currentStatus ==
                   RaceStatus.Racing;
        }
    }

    public bool IsFinished
    {
        get
        {
            return currentStatus ==
                   RaceStatus.Finished;
        }
    }

    public int TotalLaps
    {
        get
        {
            return totalLaps;
        }
    }

    public float RaceTime
    {
        get
        {
            if (
                currentStatus ==
                RaceStatus.Racing
            )
            {
                return
                    Time.time -
                    raceStartTime;
            }

            return raceTime;
        }
    }

    public CircuitCarAI PlayerCar
    {
        get
        {
            return playerCar;
        }
    }

    // ============================================================
    // PLAYER POSITION
    // ============================================================

    public int PlayerPosition
    {
        get
        {
            if (
                racePositionManager == null ||
                playerCar == null
            )
            {
                return -1;
            }

            return racePositionManager.GetPosition(
                playerCar
            );
        }
    }

    // ============================================================
    // STARTING POSITION
    // ============================================================

    public int PlayerStartingPosition
    {
        get
        {
            return playerStartingPosition;
        }
    }

    // ============================================================
    // PLAYER LAP
    // ============================================================

    public int PlayerCurrentLap
    {
        get
        {
            if (playerCar == null)
                return -1;

            LapTracker tracker =
                playerCar.GetComponent<LapTracker>();

            if (tracker == null)
                return -1;

            return tracker.CurrentLap;
        }
    }

    // ============================================================
    // PLAYER CURRENT LAP TIME
    // ============================================================

    public float PlayerCurrentLapTime
    {
        get
        {
            if (playerCar == null)
                return 0f;

            LapTracker tracker =
                playerCar.GetComponent<LapTracker>();

            if (tracker == null)
                return 0f;

            return tracker.CurrentLapTime;
        }
    }

    // ============================================================
    // PLAYER LAST LAP
    // ============================================================

    public float PlayerLastLapTime
    {
        get
        {
            if (playerCar == null)
                return 0f;

            LapTracker tracker =
                playerCar.GetComponent<LapTracker>();

            if (tracker == null)
                return 0f;

            return tracker.LastLapTime;
        }
    }

    // ============================================================
    // PLAYER BEST LAP
    // ============================================================

    public float PlayerBestLapTime
    {
        get
        {
            if (playerCar == null)
                return 0f;

            LapTracker tracker =
                playerCar.GetComponent<LapTracker>();

            if (tracker == null)
                return 0f;

            return tracker.BestLapTime;
        }
    }

    // ============================================================
    // PLAYER FINISHED
    // ============================================================

    public bool PlayerFinished
    {
        get
        {
            if (playerCar == null)
                return false;

            LapTracker tracker =
                playerCar.GetComponent<LapTracker>();

            if (tracker == null)
                return false;

            return tracker.Finished;
        }
    }

    // ============================================================
    // LEADER
    // ============================================================

    public CircuitCarAI Leader
    {
        get
        {
            if (racePositionManager == null)
                return null;

            return racePositionManager.RaceLeader;
        }
    }

    // ============================================================
    // CAR COUNT
    // ============================================================

    public int CarCount
    {
        get
        {
            if (racePositionManager == null)
                return 0;

            return racePositionManager.GetCarCount();
        }
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    void Start()
    {
        if (racePositionManager == null)
        {
            Debug.LogError(
                "RaceStateManager: " +
                "RacePositionManager is not assigned!"
            );

            return;
        }

        if (playerCar == null)
        {
            Debug.LogError(
                "RaceStateManager: " +
                "Player Car is not assigned!"
            );

            return;
        }

        // ========================================================
        // APPLY RACE LENGTH TO ALL CARS
        // ========================================================

        CircuitCarAI[] cars =
            racePositionManager.cars;

        if (cars != null)
        {
            foreach (CircuitCarAI car in cars)
            {
                if (car == null)
                    continue;

                LapTracker tracker =
                    car.GetComponent<LapTracker>();

                if (tracker == null)
                    continue;

                tracker.SetTotalLaps(
                    totalLaps
                );

                tracker.SetRaceStateManager(
                    this
                );
            }
        }

        // ========================================================
        // INITIAL STATE
        // ========================================================

        currentStatus =
            RaceStatus.PreRace;

        raceTime =
            0f;

        raceStartTime =
            0f;

        // ========================================================
        // STARTING POSITION
        // ========================================================

        playerStartingPosition =
            racePositionManager.GetPosition(
                playerCar
            );

        Debug.Log(
            "================================="
        );

        Debug.Log(
            "RaceStateManager initialized"
        );

        Debug.Log(
            "Race Length: " +
            totalLaps +
            " laps"
        );

        Debug.Log(
            "Starting Position: P" +
            playerStartingPosition
        );

        Debug.Log(
            "================================="
        );
    }

    // ============================================================
    // START RACE
    // ============================================================

    public void StartRace()
    {
        if (
            currentStatus ==
            RaceStatus.Racing
        )
        {
            return;
        }

        if (
            currentStatus ==
            RaceStatus.Finished
        )
        {
            return;
        }

        raceStartTime =
            Time.time;

        raceTime =
            0f;

        currentStatus =
            RaceStatus.Racing;

        playerStartingPosition =
            racePositionManager.GetPosition(
                playerCar
            );

        Debug.Log(
            "RaceStateManager: RACE CLOCK STARTED"
        );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        if (
            racePositionManager == null ||
            playerCar == null
        )
        {
            return;
        }

        if (
            currentStatus !=
            RaceStatus.Racing
        )
        {
            return;
        }

        // ========================================================
        // UPDATE AUTHORITATIVE RACE TIME
        // ========================================================

        raceTime =
            Time.time -
            raceStartTime;

        // ========================================================
        // CHECK FINISH
        // ========================================================

        if (AllCarsFinished())
        {
            currentStatus =
                RaceStatus.Finished;

            Debug.Log(
                "RaceStateManager: RACE FINISHED"
            );

            Debug.Log(
                "Total Race Time: " +
                FormatTime(raceTime)
            );
        }
    }

    // ============================================================
    // GAP TO CAR AHEAD
    // ============================================================

    public float GetGapToCarAhead()
    {
        if (
            racePositionManager == null ||
            playerCar == null
        )
        {
            return -1f;
        }

        CircuitCarAI[] orderedCars =
            racePositionManager.OrderedCars;

        if (
            orderedCars == null ||
            orderedCars.Length == 0
        )
        {
            return -1f;
        }

        int playerPosition =
            racePositionManager.GetPosition(
                playerCar
            );

        if (playerPosition <= 1)
        {
            return -1f;
        }

        int aheadIndex =
            playerPosition - 2;

        if (
            aheadIndex < 0 ||
            aheadIndex >= orderedCars.Length
        )
        {
            return -1f;
        }

        CircuitCarAI carAhead =
            orderedCars[aheadIndex];

        if (carAhead == null)
            return -1f;

        return CalculateTimeGap(
            carAhead,
            playerCar
        );
    }

    // ============================================================
    // GAP TO CAR BEHIND
    // ============================================================

    public float GetGapToCarBehind()
    {
        if (
            racePositionManager == null ||
            playerCar == null
        )
        {
            return -1f;
        }

        CircuitCarAI[] orderedCars =
            racePositionManager.OrderedCars;

        if (
            orderedCars == null ||
            orderedCars.Length == 0
        )
        {
            return -1f;
        }

        int playerPosition =
            racePositionManager.GetPosition(
                playerCar
            );

        if (playerPosition <= 0)
            return -1f;

        int behindIndex =
            playerPosition;

        // No car behind.
        if (
            behindIndex < 0 ||
            behindIndex >= orderedCars.Length
        )
        {
            return -1f;
        }

        CircuitCarAI carBehind =
            orderedCars[behindIndex];

        if (carBehind == null)
            return -1f;

        return CalculateTimeGap(
            playerCar,
            carBehind
        );
    }

    // ============================================================
    // TIME GAP
    // ============================================================

    private float CalculateTimeGap(
        CircuitCarAI ahead,
        CircuitCarAI behind
    )
    {
        if (
            ahead == null ||
            behind == null
        )
        {
            return -1f;
        }

        LapTracker aheadTracker =
            ahead.GetComponent<LapTracker>();

        LapTracker behindTracker =
            behind.GetComponent<LapTracker>();

        if (
            aheadTracker == null ||
            behindTracker == null
        )
        {
            return -1f;
        }

        // --------------------------------------------------------
        // Race progress
        // --------------------------------------------------------

        float aheadProgress =
            aheadTracker.RaceProgress;

        float behindProgress =
            behindTracker.RaceProgress;

        float progressDifference =
            aheadProgress -
            behindProgress;

        if (progressDifference <= 0f)
            return 0f;

        // --------------------------------------------------------
        // Track length
        // --------------------------------------------------------

        if (ahead.trackSpline == null)
            return -1f;

        float trackLength =
            ahead.trackSpline.Spline.GetLength();

        if (trackLength <= 0.01f)
            return -1f;

        // --------------------------------------------------------
        // Convert progress to distance
        // --------------------------------------------------------

        float distanceGap =
            progressDifference *
            trackLength;

        // --------------------------------------------------------
        // Current speeds
        // --------------------------------------------------------

        float aheadSpeed =
            Mathf.Max(
                ahead.CurrentSpeed,
                0.1f
            );

        float behindSpeed =
            Mathf.Max(
                behind.CurrentSpeed,
                0.1f
            );

        float averageSpeed =
            (
                aheadSpeed +
                behindSpeed
            ) *
            0.5f;

        float timeGap =
            distanceGap /
            averageSpeed;

        return Mathf.Max(
            0f,
            timeGap
        );
    }

    // ============================================================
    // ALL CARS FINISHED
    // ============================================================

    private bool AllCarsFinished()
    {
        CircuitCarAI[] cars =
            racePositionManager.cars;

        if (
            cars == null ||
            cars.Length == 0
        )
        {
            return false;
        }

        foreach (CircuitCarAI car in cars)
        {
            if (car == null)
                continue;

            LapTracker tracker =
                car.GetComponent<LapTracker>();

            if (tracker == null)
                return false;

            if (!tracker.Finished)
                return false;
        }

        return true;
    }

    // ============================================================
    // GAP BETWEEN ANY TWO CARS
    // ============================================================

    public float GetGapBetweenCars(
        CircuitCarAI ahead,
        CircuitCarAI behind
    )
    {
        if (
            ahead == null ||
            behind == null
        )
        {
            return -1f;
        }

        return CalculateTimeGap(
            ahead,
            behind
        );
    }

    // ============================================================
    // POSITIONS GAINED
    // ============================================================

    public int GetPositionsGained()
    {
        if (
            playerStartingPosition <= 0
        )
        {
            return 0;
        }

        int currentPosition =
            PlayerPosition;

        if (currentPosition <= 0)
            return 0;

        return
            playerStartingPosition -
            currentPosition;
    }

    // ============================================================
    // PLAYER CAR NAME
    // ============================================================

    public string GetPlayerCarName()
    {
        if (playerCar == null)
            return "";

        return playerCar.gameObject.name;
    }

    // ============================================================
    // TIME FORMAT
    // ============================================================

    public string FormatTime(float time)
    {
        if (time < 0f)
            return "--:--.---";

        int minutes =
            Mathf.FloorToInt(
                time / 60f
            );

        float seconds =
            time -
            minutes * 60f;

        return
            minutes.ToString("00") +
            ":" +
            seconds.ToString("00.000");
    }
}