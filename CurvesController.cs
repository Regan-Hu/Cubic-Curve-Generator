using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class CurvesController : MonoBehaviour
{
    [Header("Component Settings")]
    public GameObject ballPrefab;
    public Button generateButton;
    public TMP_Dropdown curveDropdown;

    public int segmentsPerCurve = 100;
    private Vector3 t0;
    private Vector3 t1;
    private Dictionary<Transform, Vector3> tangents = new Dictionary<Transform, Vector3>();


    private List<Transform> points = new List<Transform>();
    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        InitializeCurve();

        // Dropdown Bar listener
        curveDropdown.onValueChanged.AddListener(delegate {
            UpdateCurve();
        });
    }

    void InitializeCurve()
    {
        if (points.Count < 2)
        {
            AddPoint(new Vector3(1f, 1f, -6f));
            AddPoint(new Vector3(3f, 1f, -3f));
        }
        t0 = new Vector3(Random.Range(1f, 10f), Random.Range(1f, 6f), 0);
        t1 = new Vector3(Random.Range(1f, 10f), Random.Range(1f, 6f), 0);
    }

    public void UpdateCurve() {
        switch (curveDropdown.value)
        {
            case 0: // 假设0是Hermite Curve
                UpdateHermiteCurve();
                break;
            case 1: // 假设1是Bezier Curve
                UpdateBezierCurve();
                break;
            case 2: // Catmull-Rom Spline
                UpdateCatmullRomSpline();
                break;
            case 3: // 假设3是Uniform B-Spline
                UpdateUniformBSpline();
                break;
            default:
                Debug.LogError("Unsupported curve type selected.");
                break;
        }
    }

    void AddPoint(Vector3 position)
    {
        GameObject newBall = Instantiate(ballPrefab, position, Quaternion.identity);
        Draggable draggableComponent = newBall.AddComponent<Draggable>();
        draggableComponent.curveController = this;
        points.Add(newBall.transform);

        TextMeshProUGUI tmp = newBall.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = $"{points.Count - 1}";
        }

        tangents[newBall.transform] = new Vector3(Random.Range(1f, 10f), Random.Range(1f, 6f), 0);

        //UpdateTangents();
        UpdateCurve();
    }

    public void GenerateNextSegment()
    {
        Vector3 newPosition = new Vector3(Random.Range(1f, 10f), Random.Range(1f, 6f), Random.Range(-6f, 0f));
        AddPoint(newPosition);
    }

    public void UpdateTangents()
    {

    }

    // -----------------Hermite Curve-------------------------------------------
    public void UpdateHermiteCurve()
    {
        if (points.Count < 3) return;

        lineRenderer.positionCount = (points.Count - 1) * segmentsPerCurve;
        int segmentCount = 0;

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 p0 = points[i - 1].position;
            Vector3 p1 = points[i].position;

            // update t value
            Vector3 tangent0 = tangents[points[i - 1]];
            Vector3 tangent1 = tangents[points[i]];

            for (int j = 0; j < segmentsPerCurve; j++)
            {
                float t = j / (float)(segmentsPerCurve - 1);
                Vector3 point = CalculateHermitePoint(t, p0, p1, tangent0, tangent1);
                lineRenderer.SetPosition(segmentCount * segmentsPerCurve + j, point);
            }
            segmentCount++;
        }
    }

    Vector3 CalculateHermitePoint(float t, Vector3 p1, Vector3 p2, Vector3 t0, Vector3 t1)
    {
        float h00 = (1 - 3 * t * t + 2 * t * t * t);
        float h10 = t * t * (3 - 2 * t);
        float h01 = t * Mathf.Pow(t - 1, 2);
        float h11 = t * t * (t - 1);

        return h00 * p1 + h10 * p2 + h01 * t0 + h11 * t1;
    }


    //--------------------Bezier Curve-----------------------------------------------------
    public void UpdateBezierCurve()
    {
        if (points.Count < 4) return; 

        // Calculate the expected number of curve segments based on the current number of points
        int totalSegments = ((points.Count - 1) / 3) * segmentsPerCurve;
        lineRenderer.positionCount = totalSegments;

        int pointIndex = 0; 
        for (int i = 0; i <= points.Count - 4; i += 3)
        {
            Vector3 p0, p1, p2, p3;

            if (i < 3)
            {
                p0 = points[i].position;
                p1 = points[i + 1].position;
                p2 = points[i + 2].position;
                p3 = points[i + 3].position;
            }
            else {
                p0 = points[i].position;
                p1 = 2 * points[i].position - points[i - 1].position;
                p2 = points[i + 2].position;
                p3 = points[i + 3].position;
            }

            for (int j = 0; j < segmentsPerCurve; j++)
            {
                float t = j / (float)(segmentsPerCurve - 1);
                Vector3 point = CalculateCubicBezierPoint(t, p0, p1, p2, p3);
                lineRenderer.SetPosition(pointIndex, point);
                pointIndex++; 
            }
        }
    }

    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        // Cubic Bezier curve formula
        Vector3 point = uuu * p0; // (1-t)^3 * P0
        point += 3 * uu * t * p1; // 3t(1-t)^2 * P1
        point += 3 * u * tt * p2; // 3t^2(1-t) * P2
        point += ttt * p3; // t^3 * P3

        return point;
    }


    //--------------------Catmull-Rom Spline-----------------------------------------------------
    public void UpdateCatmullRomSpline()
    {
        if (points.Count < 4) return; 

        List<Vector3> splinePoints = new List<Vector3>();
        for (int i = 0; i < points.Count - 3; i++)
        {
            for (int j = 0; j <= segmentsPerCurve; j++)
            {
                float t = j / (float)segmentsPerCurve;
                Vector3 point = CalculateCatmullRomPoint(t, points[i].position, points[i + 1].position, points[i + 2].position, points[i + 3].position);
                splinePoints.Add(point);
            }
        }

        lineRenderer.positionCount = splinePoints.Count;
        lineRenderer.SetPositions(splinePoints.ToArray());
    }

    Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float b1 = 0.5f * (-t3 + 2 * t2 - t);
        float b2 = 0.5f * (3 * t3 - 5 * t2 + 2);
        float b3 = 0.5f * (-3 * t3 + 4 * t2 + t);
        float b4 = 0.5f * (t3 - t2);

        return (b1 * p0 + b2 * p1 + b3 * p2 + b4 * p3);
    }

    //-----------------------Uniform B-Spline-----------------------------------------------------
    public void UpdateUniformBSpline()
    {
        if (points.Count < 4) return;

        List<Vector3> splinePoints = new List<Vector3>();
        for (int i = 0; i <= points.Count - 4; i++)
        {
            for (int j = 0; j <= segmentsPerCurve; j++)
            {
                float t = j / (float)segmentsPerCurve;
                Vector3 point = CalculateUniformBSplinePoint(t, points[i].position, points[i + 1].position, points[i + 2].position, points[i + 3].position);
                splinePoints.Add(point);
            }
        }

        lineRenderer.positionCount = splinePoints.Count;
        lineRenderer.SetPositions(splinePoints.ToArray());
    }



    Vector3 CalculateUniformBSplinePoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float b0 = ((1 - t) * (1 - t) * (1 - t)) / 6;
        float b1 = (3 * t3 - 6 * t2 + 4) / 6;
        float b2 = (-3 * t3 + 3 * t2 + 3 * t + 1) / 6;
        float b3 = t3 / 6;

        return b0 * p0 + b1 * p1 + b2 * p2 + b3 * p3;
    }
}