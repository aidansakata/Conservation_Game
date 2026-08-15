using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Makes the world-space hex grid register with the board art drawn on the UI canvas.
///
/// The board is a sub-region of Stage; the grid camera is full-screen. Those two can
/// never agree, so this drives the camera's viewport rect from a RectTransform placed
/// over the board's playfield, and sizes the orthographic view so the grid's world
/// bounds fit entirely inside that viewport.
///
/// Runs with [ExecuteAlways] so the Editor shows the correct framing without entering
/// Play mode. Only rect, orthographicSize and camera position are touched -- never
/// clear flags, culling mask, or any GridSizePreset value.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class GridViewportFitter : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("Camera whose viewport rect is driven. Its clear flags and culling mask are never modified.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("RectTransform marking the board's playfield inside Stage.")]
    [SerializeField] private RectTransform viewportRect;

    [Tooltip("Canvas the viewportRect lives under. Used to map the rect to normalized screen space " +
             "without going through the camera, which would be circular once we drive camera.rect.")]
    [SerializeField] private RectTransform canvasRect;

    [Header("Grid bounds source")]
    [SerializeField] private Tilemap tilemap;
    [Tooltip("Optional fallback when the tilemap is empty.")]
    [SerializeField] private Renderer boundsRenderer;

    [Header("Tuning")]
    [Tooltip("Fraction of extra margin left around the grid inside the viewport.")]
    [Range(0f, 0.5f)][SerializeField] private float padding = 0.02f;

    // Change detection -- WebGL canvas resizes do not reliably raise events, so poll.
    private int _lastW, _lastH;
    private Rect _lastViewport;
    private Bounds _lastBounds;
    private bool _hasLast;

    private void OnEnable() { _hasLast = false; Apply(); }
    private void OnValidate() { _hasLast = false; }
    private void Update() { Apply(); }

#if UNITY_EDITOR
    // Keeps the Editor live while nothing is playing.
    private void OnRenderObject() { Apply(); }
#endif

    // When rendering to a RenderTexture (aspect-ratio testing) Screen.* still reports
    // the Game view, so derive pixel dimensions from the actual render target.
    private int TargetW { get { var rt = targetCamera.targetTexture; return rt != null ? rt.width : Screen.width; } }
    private int TargetH { get { var rt = targetCamera.targetTexture; return rt != null ? rt.height : Screen.height; } }

    private void Apply()
    {
        if (targetCamera == null || viewportRect == null || canvasRect == null) return;
        if (!targetCamera.orthographic) return;

        Bounds b;
        if (!TryGetGridBounds(out b)) return;

        Rect vp;
        if (!TryGetNormalizedViewport(out vp)) return;

        bool changed = !_hasLast
                       || _lastW != TargetW || _lastH != TargetH
                       || RectDiffers(_lastViewport, vp)
                       || (b.center - _lastBounds.center).sqrMagnitude > 1e-6f
                       || (b.size - _lastBounds.size).sqrMagnitude > 1e-6f;
        if (!changed) return;

        _lastW = TargetW; _lastH = TargetH;
        _lastViewport = vp; _lastBounds = b; _hasLast = true;

        targetCamera.rect = vp;

        // Aspect of the viewport in pixels, not of the whole screen.
        float pxW = Mathf.Max(1f, vp.width * TargetW);
        float pxH = Mathf.Max(1f, vp.height * TargetH);
        float aspect = pxW / pxH;

        // Fit to the MORE constraining axis so the grid is never cropped.
        float halfH = b.extents.y;
        float halfHForWidth = (aspect > 0.0001f) ? b.extents.x / aspect : b.extents.y;
        float size = Mathf.Max(halfH, halfHForWidth) * (1f + padding);
        if (size < 0.0001f) return;

        targetCamera.orthographicSize = size;

        var p = targetCamera.transform.position;
        targetCamera.transform.position = new Vector3(b.center.x, b.center.y, p.z);
    }

    private static bool RectDiffers(Rect a, Rect c)
    {
        return Mathf.Abs(a.x - c.x) > 1e-5f || Mathf.Abs(a.y - c.y) > 1e-5f
            || Mathf.Abs(a.width - c.width) > 1e-5f || Mathf.Abs(a.height - c.height) > 1e-5f;
    }

    /// Maps viewportRect into 0..1 screen coordinates through the canvas rect rather
    /// than the camera, so driving camera.rect cannot feed back into this measurement.
    private bool TryGetNormalizedViewport(out Rect vp)
    {
        vp = new Rect(0, 0, 1, 1);
        var cr = canvasRect.rect;
        if (cr.width <= 0.0001f || cr.height <= 0.0001f) return false;

        var corners = new Vector3[4];
        viewportRect.GetWorldCorners(corners);

        float minU = float.MaxValue, minV = float.MaxValue;
        float maxU = float.MinValue, maxV = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector3 local = canvasRect.InverseTransformPoint(corners[i]);
            float u = (local.x - cr.xMin) / cr.width;
            float v = (local.y - cr.yMin) / cr.height;
            if (u < minU) minU = u;
            if (v < minV) minV = v;
            if (u > maxU) maxU = u;
            if (v > maxV) maxV = v;
        }

        minU = Mathf.Clamp01(minU); minV = Mathf.Clamp01(minV);
        maxU = Mathf.Clamp01(maxU); maxV = Mathf.Clamp01(maxV);

        float w = maxU - minU, h = maxV - minV;
        // A degenerate rect throws in the Editor -- refuse rather than assign it.
        if (w <= 0.0005f || h <= 0.0005f) return false;

        vp = new Rect(minU, minV, w, h);
        return true;
    }

    private bool TryGetGridBounds(out Bounds b)
    {
        b = new Bounds();
        if (tilemap != null)
        {
            tilemap.CompressBounds();
            var cb = tilemap.cellBounds;
            if (cb.size.x > 0 && cb.size.y > 0)
            {
                Vector3 min = tilemap.CellToWorld(new Vector3Int(cb.xMin, cb.yMin, 0));
                Vector3 max = tilemap.CellToWorld(new Vector3Int(cb.xMax, cb.yMax, 0));
                var lb = tilemap.localBounds;
                Vector3 wMin = tilemap.transform.TransformPoint(lb.min);
                Vector3 wMax = tilemap.transform.TransformPoint(lb.max);
                var c = new Bounds((wMin + wMax) * 0.5f, Vector3.zero);
                c.Encapsulate(wMin); c.Encapsulate(wMax);
                if (c.size.x > 0.0001f && c.size.y > 0.0001f) { b = c; return true; }
                var d = new Bounds((min + max) * 0.5f, Vector3.zero);
                d.Encapsulate(min); d.Encapsulate(max);
                if (d.size.x > 0.0001f && d.size.y > 0.0001f) { b = d; return true; }
            }
        }
        if (boundsRenderer != null)
        {
            var rb = boundsRenderer.bounds;
            if (rb.size.x > 0.0001f && rb.size.y > 0.0001f) { b = rb; return true; }
        }
        return false;   // empty tilemap before a level loads -- leave the camera alone
    }
}
