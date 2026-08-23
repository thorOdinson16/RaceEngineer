using System;
using System.Collections.Generic;
using UnityEngine;

public class RacePositionManager : MonoBehaviour
{
    // ============================================================
    // RACE CARS
    // ============================================================

    [Header("Race Cars")]
    public CircuitCarAI[] cars;

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private LapTracker[] lapTrackers;

    private CircuitCarAI leader;

    private CircuitCarAI[] orderedCars;

    // Permanent finishing order
    private CircuitCarAI[] finishedCars;

    private int finishedCount;

    // ============================================================
    // PUBLIC VALUES
    // ============================================================

    public CircuitCarAI RaceLeader
    {
        get
        {
            return leader;
        }
    }

    public CircuitCarAI[] OrderedCars
    {
        get
        {
            return orderedCars;
        }
    }

    public int GetCarCount()
    {
        if (cars == null)
            return 0;

        return cars.Length;
    }

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        if (cars == null ||
            cars.Length == 0)
        {
            return;
        }

        // --------------------------------------------------------
        // CREATE TRACKERS
        // --------------------------------------------------------

        lapTrackers =
            new LapTracker[cars.Length];

        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i] == null)
                continue;

            lapTrackers[i] =
                cars[i].GetComponent<LapTracker>();
        }

        // --------------------------------------------------------
        // CREATE ORDER ARRAYS
        // --------------------------------------------------------

        orderedCars =
            new CircuitCarAI[cars.Length];

        finishedCars =
            new CircuitCarAI[cars.Length];

        finishedCount = 0;

        // Initial order
        for (int i = 0; i < cars.Length; i++)
        {
            orderedCars[i] =
                cars[i];
        }
    }

    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        if (cars == null ||
            cars.Length == 0)
        {
            return;
        }

        // First record newly finished cars.
        RecordFinishedCars();

        // Then update live order.
        UpdateRacePositions();

        // Then update leaderboard order.
        UpdateOrderedCars();
    }

    // ============================================================
    // RECORD FINISHED CARS
    // ============================================================

    private void RecordFinishedCars()
    {
        for (int i = 0; i < cars.Length; i++)
        {
            CircuitCarAI car =
                cars[i];

            if (car == null)
                continue;

            LapTracker tracker =
                lapTrackers[i];

            if (tracker == null)
                continue;

            if (!tracker.Finished)
                continue;

            // ----------------------------------------------------
            // Check if already recorded
            // ----------------------------------------------------

            bool alreadyRecorded = false;

            for (int j = 0;
                 j < finishedCount;
                 j++)
            {
                if (finishedCars[j] == car)
                {
                    alreadyRecorded = true;
                    break;
                }
            }

            if (alreadyRecorded)
                continue;

            // ----------------------------------------------------
            // Permanently store finishing position
            // ----------------------------------------------------

            if (finishedCount <
                finishedCars.Length)
            {
                finishedCars[finishedCount] =
                    car;

                finishedCount++;

                car.StartFinishParking(
                    this,
                    finishedCount
                );

                Debug.Log(
                    car.gameObject.name +
                    " FINISHED P" +
                    finishedCount
                );
            }
        }
    }

    // ============================================================
    // UPDATE RACE LEADER
    // ============================================================

    private void UpdateRacePositions()
    {
        // --------------------------------------------------------
        // If someone has finished, the first finisher is leader.
        // --------------------------------------------------------

        if (finishedCount > 0 &&
            finishedCars[0] != null)
        {
            leader =
                finishedCars[0];

            return;
        }

        // --------------------------------------------------------
        // Find live leader
        // --------------------------------------------------------

        CircuitCarAI bestCar = null;

        int bestLap = -1;
        float bestT = -1f;

        for (int i = 0;
             i < cars.Length;
             i++)
        {
            CircuitCarAI car =
                cars[i];

            if (car == null)
                continue;

            LapTracker tracker =
                lapTrackers[i];

            if (tracker == null)
                continue;

            // Finished cars are no longer part of
            // the live-race calculation.
            if (tracker.Finished)
                continue;

            int lap =
                tracker.CurrentLap;

            float t =
                car.CurrentT;

            // ----------------------------------------------------
            // Higher lap
            // ----------------------------------------------------

            if (lap > bestLap)
            {
                bestLap = lap;
                bestT = t;
                bestCar = car;
            }

            // ----------------------------------------------------
            // Same lap → higher T
            // ----------------------------------------------------

            else if (lap == bestLap)
            {
                if (t > bestT)
                {
                    bestT = t;
                    bestCar = car;
                }
            }
        }

        leader =
            bestCar;
    }

    // ============================================================
    // UPDATE ORDERED CARS
    // ============================================================

    private void UpdateOrderedCars()
    {
        if (orderedCars == null ||
            orderedCars.Length != cars.Length)
        {
            orderedCars =
                new CircuitCarAI[cars.Length];
        }

        // --------------------------------------------------------
        // 1. PUT FINISHED CARS FIRST
        // --------------------------------------------------------

        int index = 0;

        for (int i = 0;
             i < finishedCount;
             i++)
        {
            if (finishedCars[i] == null)
                continue;

            orderedCars[index] =
                finishedCars[i];

            index++;
        }

        // --------------------------------------------------------
        // 2. COLLECT ACTIVE CARS
        // --------------------------------------------------------

        CircuitCarAI[] activeCars =
            new CircuitCarAI[cars.Length];

        int activeCount = 0;

        for (int i = 0;
             i < cars.Length;
             i++)
        {
            CircuitCarAI car =
                cars[i];

            if (car == null)
                continue;

            LapTracker tracker =
                lapTrackers[i];

            if (tracker != null &&
                tracker.Finished)
            {
                continue;
            }

            activeCars[activeCount] =
                car;

            activeCount++;
        }

        // --------------------------------------------------------
        // 3. SORT ACTIVE CARS
        // --------------------------------------------------------

        Array.Sort(
            activeCars,
            0,
            activeCount,
            Comparer<CircuitCarAI>.Create(
                CompareCars
            )
        );

        // --------------------------------------------------------
        // 4. ADD ACTIVE CARS AFTER FINISHED CARS
        // --------------------------------------------------------

        for (int i = 0;
             i < activeCount;
             i++)
        {
            orderedCars[index] =
                activeCars[i];

            index++;
        }

        // --------------------------------------------------------
        // 5. CLEAR UNUSED SLOTS
        // --------------------------------------------------------

        while (index < orderedCars.Length)
        {
            orderedCars[index] =
                null;

            index++;
        }
    }

    // ============================================================
    // COMPARE ACTIVE CARS
    // ============================================================

    private int CompareCars(
        CircuitCarAI a,
        CircuitCarAI b
    )
    {
        if (a == null && b == null)
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        LapTracker trackerA =
            a.GetComponent<LapTracker>();

        LapTracker trackerB =
            b.GetComponent<LapTracker>();

        if (trackerA == null &&
            trackerB == null)
        {
            return 0;
        }

        if (trackerA == null)
            return 1;

        if (trackerB == null)
            return -1;

        // --------------------------------------------------------
        // LAP
        // --------------------------------------------------------

        int lapA =
            trackerA.CurrentLap;

        int lapB =
            trackerB.CurrentLap;

        if (lapA != lapB)
        {
            return lapB.CompareTo(lapA);
        }

        // --------------------------------------------------------
        // SPLINE POSITION
        // --------------------------------------------------------

        float tA =
            a.CurrentT;

        float tB =
            b.CurrentT;

        return tB.CompareTo(tA);
    }

    // ============================================================
    // GET POSITION
    // ============================================================

    public int GetPosition(
        CircuitCarAI targetCar
    )
    {
        if (targetCar == null ||
            orderedCars == null)
        {
            return -1;
        }

        // --------------------------------------------------------
        // Position comes directly from the current/final order.
        // --------------------------------------------------------

        for (int i = 0;
             i < orderedCars.Length;
             i++)
        {
            if (orderedCars[i] ==
                targetCar)
            {
                return i + 1;
            }
        }

        return -1;
    }
}