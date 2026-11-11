using UnityEngine;

/// <summary>Owns scale deformation and its bottom offset; never moves the flight body.</summary>
internal sealed class RocketDeformation
{
    private readonly Transform root;
    private readonly Vector3 initialScale;
    private readonly Vector3[] bodyCorners;
    private readonly float initialBottomOffset;

    public RocketDeformation(Transform root)
    {
        this.root = root;
        initialScale = root.localScale;
        bodyCorners = CaptureBodyCorners(root);
        initialBottomOffset = BottomOffset();
    }

    // Add to the undeformed flight position. Its value returns smoothly to zero
    // as the launch restores the scale, so releasing the anchor cannot snap Y.
    public float VerticalOffset => initialBottomOffset - BottomOffset();

    public void ApplyInhale(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        root.localScale = Vector3.Scale(initialScale,
            new Vector3(1f + 0.1f * ratio, 1f - 0.5f * ratio, 1f + 0.1f * ratio));
    }

    public void RestoreDuringFlight(float deltaTime)
    {
        root.localScale = Vector3.MoveTowards(root.localScale, initialScale, deltaTime * 3f);
    }

    public void ResetScale() { root.localScale = initialScale; }

    private float BottomOffset()
    {
        float bottom = float.PositiveInfinity;
        foreach (Vector3 corner in bodyCorners)
            bottom = Mathf.Min(bottom, root.TransformVector(corner).y);
        return bottom;
    }

    private static Vector3[] CaptureBodyCorners(Transform root)
    {
        Bounds bodyBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool foundBody = false;
        // Cache static body meshes once. Particle renderers are not body geometry.
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) continue;
            foreach (Vector3 corner in Corners(filter.sharedMesh.bounds))
            {
                Vector3 local = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!foundBody) { bodyBounds = new Bounds(local, Vector3.zero); foundBody = true; }
                else bodyBounds.Encapsulate(local);
            }
        }
        if (!foundBody)
        {
            BoxCollider collider = root.GetComponent<BoxCollider>();
            if (collider != null) bodyBounds = new Bounds(collider.center, collider.size);
        }
        return Corners(bodyBounds);
    }

    private static Vector3[] Corners(Bounds bounds)
    {
        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < corners.Length; i++)
            corners[i] = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f,
                    (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
        return corners;
    }
}
