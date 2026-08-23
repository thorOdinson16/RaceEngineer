using UnityEngine;

public class TyreSystem : MonoBehaviour
{
    // ============================================================
    // TYRE COMPOUNDS
    // ============================================================

    public enum TyreCompound
    {
        Soft,
        Medium,
        Hard,
        Intermediate,
        Wet
    }

    // ============================================================
    // WEATHER
    // ============================================================

    public enum WeatherCondition
    {
        Dry,
        LightRain,
        HeavyRain
    }

    // ============================================================
    // CURRENT TYRE
    // ============================================================

    [Header("Current Tyre")]
    [SerializeField]
    private TyreCompound currentCompound = TyreCompound.Medium;

    [SerializeField]
    private float tyreAge;

    [SerializeField]
    [Range(0f, 100f)]
    private float tyreCondition = 100f;

    // ============================================================
    // WEATHER
    // ============================================================

    [Header("Weather")]
    [SerializeField]
    private WeatherCondition currentWeather = WeatherCondition.Dry;

    // ============================================================
    // BASE GRIP
    // ============================================================

    /*
     * Fresh-tyre compound performance.
     *
     * Soft   = fastest
     * Medium = balanced
     * Hard   = slower
     *
     * Inter/Wet will be tuned later around weather.
     */

    private const float SOFT_BASE_GRIP = 1.05f;
    private const float MEDIUM_BASE_GRIP = 1.00f;
    private const float HARD_BASE_GRIP = 0.96f;

    private const float INTERMEDIATE_BASE_GRIP = 0.94f;
    private const float WET_BASE_GRIP = 0.90f;

    // ============================================================
    // EFFECTIVE LIFETIME
    // ============================================================

    /*
     * These are strategic target lifetimes, NOT hard expiration.
     */

    private const float SOFT_EFFECTIVE_LIFE = 2.5f;
    private const float MEDIUM_EFFECTIVE_LIFE = 5f;
    private const float HARD_EFFECTIVE_LIFE = 8f;

    private const float INTERMEDIATE_EFFECTIVE_LIFE = 6f;
    private const float WET_EFFECTIVE_LIFE = 6f;

    // ============================================================
    // MINIMUM CONDITION
    // ============================================================

    /*
     * A tyre never disappears.
     *
     * At very low condition it is simply extremely bad.
     */

    private const float MIN_TYRE_CONDITION = 20f;

    // ============================================================
    // PUBLIC VALUES
    // ============================================================

    public TyreCompound CurrentCompound
    {
        get
        {
            return currentCompound;
        }
    }

    public WeatherCondition CurrentWeather
    {
        get
        {
            return currentWeather;
        }
    }

    public float TyreAge
    {
        get
        {
            return tyreAge;
        }
    }

    public float TyreCondition
    {
        get
        {
            return tyreCondition;
        }
    }

    // ============================================================
    // BASE GRIP
    // ============================================================

    public float BaseGrip
    {
        get
        {
            switch (currentCompound)
            {
                case TyreCompound.Soft:
                    return SOFT_BASE_GRIP;

                case TyreCompound.Medium:
                    return MEDIUM_BASE_GRIP;

                case TyreCompound.Hard:
                    return HARD_BASE_GRIP;

                case TyreCompound.Intermediate:
                    return INTERMEDIATE_BASE_GRIP;

                case TyreCompound.Wet:
                    return WET_BASE_GRIP;

                default:
                    return MEDIUM_BASE_GRIP;
            }
        }
    }

    // ============================================================
    // EFFECTIVE LIFE
    // ============================================================

    public float EffectiveLife
    {
        get
        {
            switch (currentCompound)
            {
                case TyreCompound.Soft:
                    return SOFT_EFFECTIVE_LIFE;

                case TyreCompound.Medium:
                    return MEDIUM_EFFECTIVE_LIFE;

                case TyreCompound.Hard:
                    return HARD_EFFECTIVE_LIFE;

                case TyreCompound.Intermediate:
                    return INTERMEDIATE_EFFECTIVE_LIFE;

                case TyreCompound.Wet:
                    return WET_EFFECTIVE_LIFE;

                default:
                    return MEDIUM_EFFECTIVE_LIFE;
            }
        }
    }

    // ============================================================
    // GRIP MULTIPLIER
    // ============================================================

    /*
     * Final tyre grip:
     *
     * Compound grip
     *      ×
     * Tyre degradation
     *      ×
     * Weather suitability
     */

    public float GripMultiplier
    {
        get
        {
            float degradationMultiplier =
                CalculateDegradationMultiplier();

            float weatherMultiplier =
                CalculateWeatherMultiplier();

            return
                BaseGrip *
                degradationMultiplier *
                weatherMultiplier;
        }
    }

    // ============================================================
    // PERFORMANCE MULTIPLIER
    // ============================================================

    /*
     * Tyre condition affects overall straight-line performance.
     *
     * This is intentionally separate from GripMultiplier.
     *
     * GripMultiplier:
     *     cornering / lateral behaviour
     *
     * PerformanceMultiplier:
     *     acceleration / achievable speed
     *
     * Fresh tyre:
     *     1.00
     *
     * Tattered tyre:
     *     0.50
     *
     * The falloff is intentionally aggressive because tyre
     * degradation is a major strategic mechanic in this game.
     */

    public float PerformanceMultiplier
    {
        get
        {
            float condition =
                Mathf.Clamp01(
                    tyreCondition / 100f
                );

            float performance =
                Mathf.Lerp(
                    0.50f,
                    1.00f,
                    Mathf.Pow(
                        condition,
                        0.70f
                    )
                );

            return performance;
        }
    }

    // ============================================================
    // DEGRADATION MULTIPLIER
    // ============================================================

    private float CalculateDegradationMultiplier()
    {
        if (tyreCondition >= 100f)
        {
            return 1f;
        }

        float normalizedCondition =
            Mathf.InverseLerp(
                MIN_TYRE_CONDITION,
                100f,
                tyreCondition
            );

        /*
         * Grip degradation is intentionally stronger than a
         * simple linear relationship.
         *
         * 100% condition = full grip
         * 20% condition  = heavily worn
         */

        return Mathf.Lerp(
            0.55f,
            1f,
            normalizedCondition
        );
    }

    // ============================================================
    // WEATHER MULTIPLIER
    // ============================================================

    private float CalculateWeatherMultiplier()
    {
        switch (currentWeather)
        {
            // ====================================================
            // DRY
            // ====================================================

            case WeatherCondition.Dry:

                if (
                    currentCompound ==
                    TyreCompound.Intermediate
                )
                {
                    return 0.92f;
                }

                if (
                    currentCompound ==
                    TyreCompound.Wet
                )
                {
                    return 0.82f;
                }

                return 1f;

            // ====================================================
            // LIGHT RAIN
            // ====================================================

            case WeatherCondition.LightRain:

                if (
                    currentCompound ==
                    TyreCompound.Intermediate
                )
                {
                    return 1.05f;
                }

                if (
                    currentCompound ==
                    TyreCompound.Wet
                )
                {
                    return 1.00f;
                }

                // Slick tyres lose significant performance.
                return 0.78f;

            // ====================================================
            // HEAVY RAIN
            // ====================================================

            case WeatherCondition.HeavyRain:

                if (
                    currentCompound ==
                    TyreCompound.Wet
                )
                {
                    return 1.05f;
                }

                if (
                    currentCompound ==
                    TyreCompound.Intermediate
                )
                {
                    return 0.88f;
                }

                // Slick tyres are extremely poor in heavy rain.
                return 0.55f;

            default:
                return 1f;
        }
    }

    // ============================================================
    // SET TYRE
    // ============================================================

    public void SetTyre(
        TyreCompound compound
    )
    {
        currentCompound =
            compound;

        tyreAge =
            0f;

        tyreCondition =
            100f;

        Debug.Log(
            gameObject.name +
            " fitted " +
            currentCompound +
            " tyres."
        );
    }

    // ============================================================
    // SET WEATHER
    // ============================================================

    public void SetWeather(
        WeatherCondition weather
    )
    {
        currentWeather =
            weather;
    }

    // ============================================================
    // AGE TYRES
    // ============================================================

    public void AgeTyres()
    {
        tyreAge += 1f;

        tyreCondition =
            CalculateConditionFromAge(
                tyreAge
            );

        Debug.Log(
            gameObject.name +
            " | " +
            currentCompound +
            " | Age: " +
            tyreAge.ToString("0") +
            " | Condition: " +
            tyreCondition.ToString("0.0") +
            "%"
        );
    }

    // ============================================================
    // CONDITION FROM AGE
    // ============================================================

    private float CalculateConditionFromAge(
        float age
    )
    {
        switch (currentCompound)
        {
            case TyreCompound.Soft:
                return CalculateSoftCondition(age);

            case TyreCompound.Medium:
                return CalculateMediumCondition(age);

            case TyreCompound.Hard:
                return CalculateHardCondition(age);

            case TyreCompound.Intermediate:
                return CalculateLongLifeCondition(
                    age,
                    INTERMEDIATE_EFFECTIVE_LIFE
                );

            case TyreCompound.Wet:
                return CalculateLongLifeCondition(
                    age,
                    WET_EFFECTIVE_LIFE
                );

            default:
                return 100f;
        }
    }

    // ============================================================
    // SOFT TYRE CURVE
    // ============================================================

    /*
    * First 2 laps = full performance.
    *
    * Lap 1 = 100
    * Lap 2 = 100
    * Lap 3 = degrading
    * Lap 4 = tattered
    */

    private float CalculateSoftCondition(float age)
    {
        if (age <= 2f)
        {
            return 100f;
        }

        if (age <= 4f)
        {
            float t =
                Mathf.InverseLerp(
                    2f,
                    4f,
                    age
                );

            /*
            * Sharp progressive degradation:
            *
            * Lap 2 = 100
            * Lap 3 = ~60
            * Lap 4 = 20
            */

            return Mathf.Lerp(
                100f,
                MIN_TYRE_CONDITION,
                t
            );
        }

        return MIN_TYRE_CONDITION;
    }


    // ============================================================
    // MEDIUM TYRE CURVE
    // ============================================================

    /*
    * First 2 laps = full performance.
    *
    * Lap 1 = 100
    * Lap 2 = 100
    * Lap 3 = degrading
    * Lap 4 = degrading
    * Lap 5 = degrading
    * Lap 6 = tattered
    */

    private float CalculateMediumCondition(float age)
    {
        if (age <= 2f)
        {
            return 100f;
        }

        if (age <= 6f)
        {
            float t =
                Mathf.InverseLerp(
                    2f,
                    6f,
                    age
                );

            /*
            * Lap 2 = 100
            * Lap 3 = ~80
            * Lap 4 = ~60
            * Lap 5 = ~40
            * Lap 6 = 20
            */

            return Mathf.Lerp(
                100f,
                MIN_TYRE_CONDITION,
                t
            );
        }

        return MIN_TYRE_CONDITION;
    }


    // ============================================================
    // HARD TYRE CURVE
    // ============================================================

    /*
    * First 2 laps = full performance.
    *
    * Lap 1 = 100
    * Lap 2 = 100
    * Lap 3 = degrading
    * Lap 4 = degrading
    * Lap 5 = degrading
    * Lap 6 = degrading
    * Lap 7 = degrading
    * Lap 8 = degrading
    * Lap 9 = tattered
    */

    private float CalculateHardCondition(float age)
    {
        if (age <= 2f)
        {
            return 100f;
        }

        if (age <= 9f)
        {
            float t =
                Mathf.InverseLerp(
                    2f,
                    9f,
                    age
                );

            /*
            * Lap 2 = 100
            * Lap 3 = ~89
            * Lap 4 = ~77
            * Lap 5 = ~66
            * Lap 6 = ~54
            * Lap 7 = ~43
            * Lap 8 = ~31
            * Lap 9 = 20
            */

            return Mathf.Lerp(
                100f,
                MIN_TYRE_CONDITION,
                t
            );
        }

        return MIN_TYRE_CONDITION;
    }

    // ============================================================
    // INTERMEDIATE / WET
    // ============================================================

    /*
     * These aren't part of the dry tyre balance pass yet.
     *
     * We keep their existing progressive model until we implement
     * the weather system properly.
     */

    private float CalculateLongLifeCondition(
        float age,
        float life
    )
    {
        float normalizedAge =
            age /
            Mathf.Max(
                life,
                0.1f
            );

        float degradation =
            Mathf.Pow(
                Mathf.Clamp01(
                    normalizedAge
                ),
                1.65f
            );

        float condition =
            Mathf.Lerp(
                100f,
                MIN_TYRE_CONDITION,
                degradation
            );

        if (age > life)
        {
            float extraAge =
                age - life;

            condition -=
                extraAge * 10f;
        }

        return Mathf.Clamp(
            condition,
            MIN_TYRE_CONDITION,
            100f
        );
    }

    // ============================================================
    // RESET
    // ============================================================

    public void ResetTyres()
    {
        tyreAge =
            0f;

        tyreCondition =
            100f;
    }

    // ============================================================
    // DEBUG INFORMATION
    // ============================================================

    public string GetTyreStatus()
    {
        return
            currentCompound +
            " | Age: " +
            tyreAge.ToString("0.0") +
            " | Condition: " +
            tyreCondition.ToString("0.0") +
            " | Grip: " +
            GripMultiplier.ToString("0.000");
    }

    // ============================================================
    // UNITY START
    // ============================================================

    private void Start()
    {
        tyreAge =
            Mathf.Max(
                tyreAge,
                0f
            );

        tyreCondition =
            Mathf.Clamp(
                tyreCondition,
                MIN_TYRE_CONDITION,
                100f
            );
    }
}