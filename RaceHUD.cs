using TMPro;
using UnityEngine;

public class RaceHUD : MonoBehaviour
{
    // ============================================================
    // PLAYER CAR
    // ============================================================

    [Header("Player Car")]
    public LapTracker playerCar;

    // ============================================================
    // RACE
    // ============================================================

    [Header("Race")]
    public RacePositionManager racePositionManager;
    public RaceStateManager raceStateManager;

    // ============================================================
    // BASIC HUD
    // ============================================================

    [Header("Basic HUD")]
    public TMP_Text lapText;
    public TMP_Text positionText;

    // ============================================================
    // TIMING HUD
    // ============================================================

    [Header("Timing")]
    public TMP_Text raceTimeText;
    public TMP_Text currentLapTimeText;
    public TMP_Text lastLapTimeText;
    public TMP_Text bestLapTimeText;

    // ============================================================
    // GAP HUD
    // ============================================================

    [Header("Gaps")]
    public TMP_Text gapAheadText;
    public TMP_Text gapBehindText;

    // ============================================================
    // POSITION CHANGE
    // ============================================================

    [Header("Position Change")]
    public TMP_Text positionsGainedText;

    // ============================================================
    // LEADERBOARD
    // ============================================================

    [Header("Leaderboard")]
    public TMP_Text leaderboardText;

    // ============================================================
    // UPDATE
    // ============================================================

    void Update()
    {
        UpdateLap();
        UpdateTiming();
        UpdatePosition();
        UpdateGaps();
        UpdatePositionsGained();
        UpdateLeaderboard();
    }

    // ============================================================
    // LAP
    // ============================================================

    private void UpdateLap()
    {
        if (
            playerCar == null ||
            lapText == null
        )
        {
            return;
        }

        if (playerCar.Finished)
        {
            lapText.text =
                "FINISHED";

            return;
        }

        lapText.text =
            "LAP " +
            playerCar.CurrentLap +
            "/" +
            playerCar.TotalLaps;
    }

    // ============================================================
    // TIMING
    // ============================================================

    private void UpdateTiming()
    {
        if (
            playerCar == null ||
            raceStateManager == null
        )
        {
            return;
        }

        // --------------------------------------------------------
        // RACE TIME
        // --------------------------------------------------------

        if (raceTimeText != null)
        {
            raceTimeText.text =
                "TIME  " +
                FormatTime(
                    raceStateManager.RaceTime
                );
        }

        // --------------------------------------------------------
        // CURRENT LAP
        // --------------------------------------------------------

        if (currentLapTimeText != null)
        {
            currentLapTimeText.text =
                "LAP   " +
                FormatTime(
                    playerCar.CurrentLapTime
                );
        }

        // --------------------------------------------------------
        // LAST LAP
        // --------------------------------------------------------

        if (lastLapTimeText != null)
        {
            if (playerCar.LastLapTime > 0f)
            {
                lastLapTimeText.text =
                    "LAST  " +
                    FormatTime(
                        playerCar.LastLapTime
                    );
            }
            else
            {
                lastLapTimeText.text =
                    "LAST  --:--.---";
            }
        }

        // --------------------------------------------------------
        // BEST LAP
        // --------------------------------------------------------

        if (bestLapTimeText != null)
        {
            if (playerCar.BestLapTime > 0f)
            {
                bestLapTimeText.text =
                    "BEST  " +
                    FormatTime(
                        playerCar.BestLapTime
                    );
            }
            else
            {
                bestLapTimeText.text =
                    "BEST  --:--.---";
            }
        }
    }

    // ============================================================
    // POSITION
    // ============================================================

    private void UpdatePosition()
    {
        if (
            raceStateManager == null ||
            positionText == null
        )
        {
            return;
        }

        int position =
            raceStateManager.PlayerPosition;

        int totalCars =
            raceStateManager.CarCount;

        if (
            position <= 0 ||
            totalCars <= 0
        )
        {
            positionText.text =
                "POS   --/--";

            return;
        }

        positionText.text =
            "POS   P" +
            position +
            "/" +
            totalCars;
    }

    // ============================================================
    // GAPS
    // ============================================================

    private void UpdateGaps()
    {
        if (raceStateManager == null)
            return;

        // --------------------------------------------------------
        // GAP AHEAD
        // --------------------------------------------------------

        if (gapAheadText != null)
        {
            float gapAhead =
                raceStateManager.GetGapToCarAhead();

            if (gapAhead < 0f)
            {
                gapAheadText.text =
                    "GAP AHEAD   ---";
            }
            else
            {
                gapAheadText.text =
                    "GAP AHEAD   +" +
                    gapAhead.ToString("0.000") +
                    "s";
            }
        }

        // --------------------------------------------------------
        // GAP BEHIND
        // --------------------------------------------------------

        if (gapBehindText != null)
        {
            float gapBehind =
                raceStateManager.GetGapToCarBehind();

            if (gapBehind < 0f)
            {
                gapBehindText.text =
                    "GAP BEHIND  ---";
            }
            else
            {
                gapBehindText.text =
                    "GAP BEHIND  +" +
                    gapBehind.ToString("0.000") +
                    "s";
            }
        }
    }

    // ============================================================
    // POSITIONS GAINED
    // ============================================================

    private void UpdatePositionsGained()
    {
        if (
            raceStateManager == null ||
            positionsGainedText == null
        )
        {
            return;
        }

        int gained =
            raceStateManager.GetPositionsGained();

        if (gained > 0)
        {
            positionsGainedText.text =
                "GAIN  +" +
                gained;
        }
        else if (gained < 0)
        {
            positionsGainedText.text =
                "GAIN  " +
                gained;
        }
        else
        {
            positionsGainedText.text =
                "GAIN  0";
        }
    }

    // ============================================================
    // LEADERBOARD
    // ============================================================

    private void UpdateLeaderboard()
    {
        if (
            racePositionManager == null ||
            leaderboardText == null
        )
        {
            return;
        }

        CircuitCarAI[] orderedCars =
            racePositionManager.OrderedCars;

        if (
            orderedCars == null ||
            orderedCars.Length == 0
        )
        {
            leaderboardText.text =
                "RACE ORDER";

            return;
        }

        string leaderboard =
            "RACE ORDER\n\n";

        for (
            int i = 0;
            i < orderedCars.Length;
            i++
        )
        {
            CircuitCarAI car =
                orderedCars[i];

            if (car == null)
                continue;

            leaderboard +=
                "P" +
                (i + 1) +
                "  " +
                car.gameObject.name;

            // ----------------------------------------------------
            // Gap to car immediately ahead
            // ----------------------------------------------------

            if (i > 0)
            {
                CircuitCarAI carAhead =
                    orderedCars[i - 1];

                if (
                    raceStateManager != null &&
                    carAhead != null
                )
                {
                    float gap =
                        raceStateManager
                            .GetGapBetweenCars(
                                carAhead,
                                car
                            );

                    if (gap >= 0f)
                    {
                        leaderboard +=
                            "   +" +
                            gap.ToString("0.000") +
                            "s";
                    }
                }
            }

            leaderboard +=
                "\n";
        }

        leaderboardText.text =
            leaderboard;
    }

    // ============================================================
    // TIME FORMAT
    // ============================================================

    private string FormatTime(float time)
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