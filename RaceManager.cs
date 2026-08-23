using System.Collections;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    // ============================================================
    // CARS
    // ============================================================

    [Header("Race Cars")]
    public CircuitCarAI[] cars;

    // ============================================================
    // STARTING LIGHTS
    // ============================================================

    [Header("Starting Lights")]
    public GameObject[] raceLights;

    // ============================================================
    // TIMING
    // ============================================================

    [Header("Race Start Timing")]
    private float lightInterval = 1.0f;

    private float minimumGoDelay = 2.0f;
    private float maximumGoDelay = 3.5f;

    // ============================================================
    // RACE STATE
    // ============================================================

    [Header("Race State")]
    [SerializeField]
    private RaceStateManager raceStateManager;

    // ============================================================
    // START
    // ============================================================

    void Start()
    {
        // Cars cannot move before GO
        SetCarsRaceState(false);

        // All lights start OFF
        SetAllLights(false);

        StartCoroutine(StartRace());
    }

    // ============================================================
    // RACE START
    // ============================================================

    private IEnumerator StartRace()
    {
        // Give cars one frame to initialize
        yield return null;

        Debug.Log("=================================");
        Debug.Log("RACE STARTING");
        Debug.Log("=================================");

        // --------------------------------------------------------
        // LIGHT 1
        // --------------------------------------------------------

        SetLight(0, true);

        Debug.Log("LIGHT 1");

        yield return new WaitForSeconds(
            lightInterval
        );

        // --------------------------------------------------------
        // LIGHT 2
        // --------------------------------------------------------

        SetLight(1, true);

        Debug.Log("LIGHT 2");

        yield return new WaitForSeconds(
            lightInterval
        );

        // --------------------------------------------------------
        // LIGHT 3
        // --------------------------------------------------------

        SetLight(2, true);

        Debug.Log("LIGHT 3");

        yield return new WaitForSeconds(
            lightInterval
        );

        // --------------------------------------------------------
        // LIGHT 4
        // --------------------------------------------------------

        SetLight(3, true);

        Debug.Log("LIGHT 4");

        yield return new WaitForSeconds(
            lightInterval
        );

        // --------------------------------------------------------
        // LIGHT 5
        // --------------------------------------------------------

        SetLight(4, true);

        Debug.Log("LIGHT 5");

        // --------------------------------------------------------
        // RANDOM WAIT BEFORE GO
        // --------------------------------------------------------

        float goDelay =
            Random.Range(
                minimumGoDelay,
                maximumGoDelay
            );

        Debug.Log(
            "GO delay: " +
            goDelay.ToString("F2") +
            " seconds"
        );

        yield return new WaitForSeconds(
            goDelay
        );

        // ============================================================
        // GO
        // ============================================================

        Debug.Log("=================================");
        Debug.Log("GO!");
        Debug.Log("=================================");

        // Turn all red lights OFF
        SetAllLights(false);

        // Start the central race clock
        if (raceStateManager != null)
        {
            raceStateManager.StartRace();
        }

        // Release all cars
        SetCarsRaceState(true);
    }

    // ============================================================
    // SET INDIVIDUAL LIGHT
    // ============================================================

    private void SetLight(
        int index,
        bool state
    )
    {
        if (raceLights == null)
            return;

        if (index < 0 ||
            index >= raceLights.Length)
            return;

        if (raceLights[index] == null)
            return;

        raceLights[index].SetActive(state);
    }

    // ============================================================
    // SET ALL LIGHTS
    // ============================================================

    private void SetAllLights(
        bool state
    )
    {
        if (raceLights == null)
            return;

        foreach (GameObject lightObject in raceLights)
        {
            if (lightObject == null)
                continue;

            lightObject.SetActive(state);
        }
    }

    // ============================================================
    // SET CAR STATE
    // ============================================================

    private void SetCarsRaceState(
        bool started
    )
    {
        if (cars == null)
            return;

        foreach (CircuitCarAI car in cars)
        {
            if (car == null)
                continue;

            car.RaceStarted = started;
        }
    }
}