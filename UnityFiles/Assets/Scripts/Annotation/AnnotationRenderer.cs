using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

public class AnnotationRenderer : MonoBehaviour
{
    [Header("Dependencies")] [SerializeField]
    private Camera captureCamera;

    [SerializeField] private float fallbackDepth = 2f;
    [SerializeField] private AnnotationManager annotationManager;

    [Header("Line Settings")] [SerializeField]
    private float lineWidth = 0.01f;

    [SerializeField] private Material lineMaterial;

    [Header("Testing")] [SerializeField] private bool runDummyOnStart = true;

    private readonly List<GameObject> _annotations = new List<GameObject>();

    void Start()
    {
        annotationManager.OnAnnotationReceived += RenderAnnotation;
        annotationManager.OnClearAnnotations += ClearAll;

        if (runDummyOnStart)
            RenderAnnotation(GetDummyData());
    }

    private ParsedAnnotation GetDummyData()
    {
        return new ParsedAnnotation
        {
            points = new AnnotationPoint[]
            {
                new AnnotationPoint { x = 0.4f, y = 0.4f },
                new AnnotationPoint { x = 0.5f, y = 0.35f },
                new AnnotationPoint { x = 0.6f, y = 0.4f },
                new AnnotationPoint { x = 0.6f, y = 0.5f },
                new AnnotationPoint { x = 0.5f, y = 0.55f },
                new AnnotationPoint { x = 0.4f, y = 0.5f },
                new AnnotationPoint { x = 0.4f, y = 0.4f },
            },
            color = "#FF0000"
        };
    }

    private void RenderAnnotation(ParsedAnnotation data)
    {
        if (data.points == null || data.points.Length < 2)
        {
            //Debug.Log("RenderAnnotation: points null or too few");
            return;
        }

        // Convert all 2D points to 3D
        var worldPoints = new List<Vector3>();
        foreach (var p in data.points)
        {
            var world = AnnotationConverter.ViewportToWorld(captureCamera, p, fallbackDepth);
            worldPoints.Add(world);
        }

        // Parse color
        if (!ColorUtility.TryParseHtmlString(data.color, out Color color))
            color = Color.red;

        // Create GameObject with LineRenderer
        var go = new GameObject(data.isFading ? "Annotation_Fading" : "Annotation_Permanent");
        go.layer = LayerMask.NameToLayer("Annotations");  
        _annotations.Add(go);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = worldPoints.Count;
        lr.SetPositions(worldPoints.ToArray());
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        var baseMat = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.material = new Material(baseMat);
        lr.startColor = color;
        lr.endColor = color;

        if (data.isFading)
            StartCoroutine(FadeAndDestroy(go, lr, color, data.FadeDurationSeconds > 0 ? data.FadeDurationSeconds : 2f));
    }

    private System.Collections.IEnumerator FadeAndDestroy(GameObject go, LineRenderer lr, Color baseColor,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (go == null) yield break;

            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - elapsed / duration);

            var c = baseColor;
            c.a = alpha;
            lr.startColor = c;
            lr.endColor = c;

            yield return null;
        }

        if (go != null)
        {
            _annotations.Remove(go);
            Destroy(go);
        }
    }

    public void ClearAll()
    {
        Debug.Log($"AnnotationRenderer: ClearAll called, destroying {_annotations.Count} annotations");
        foreach (var go in _annotations)
            if (go != null)
                Destroy(go);
        _annotations.Clear();
    }

    void OnDestroy()
    {
        if (annotationManager != null)
        {
            annotationManager.OnAnnotationReceived -= RenderAnnotation;
            annotationManager.OnClearAnnotations -= ClearAll;

        }
    }
}