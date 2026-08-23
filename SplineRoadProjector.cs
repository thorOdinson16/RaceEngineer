using UnityEngine;
using UnityEngine.Splines;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SplineRoadProjector : MonoBehaviour
{
    public SplineContainer spline;

    [Header("Raycast")]
    public LayerMask roadLayer;
    public float rayHeight = 50f;
    public float rayDistance = 100f;

    [Header("Spline Height")]
    public float heightOffset = 0.2f;

    [ContextMenu("Project Spline To Road")]
    public void ProjectSplineToRoad()
    {
        if (spline == null)
        {
            Debug.LogError("Spline Container not assigned.");
            return;
        }

        Spline currentSpline = spline.Spline;

        for (int i = 0; i < currentSpline.Count; i++)
        {
            BezierKnot knot = currentSpline[i];

            Vector3 worldPosition =
                spline.transform.TransformPoint(knot.Position);

            Vector3 rayOrigin = new Vector3(
                worldPosition.x,
                worldPosition.y + rayHeight,
                worldPosition.z
            );

            if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                rayDistance,
                roadLayer))
            {
                Vector3 projectedPosition = hit.point;

                projectedPosition.y += heightOffset;

                knot.Position =
                    spline.transform.InverseTransformPoint(projectedPosition);

                currentSpline[i] = knot;
            }
            else
            {
                Debug.LogWarning(
                    $"No road found under spline knot {i}"
                );
            }
        }

        Debug.Log("Spline projected onto road.");

#if UNITY_EDITOR
        EditorUtility.SetDirty(spline);
#endif
    }
}