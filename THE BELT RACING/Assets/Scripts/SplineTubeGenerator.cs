
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplineTubeGenerator : MonoBehaviour
{
    [Header("Spline")]
    [Tooltip("The spline that defines the forcefield path.")]
    public SplineContainer splineContainer;

    [Header("Tube Settings")]
    [Tooltip("Radius of the circular forcefield.")]
    [Min(0.1f)]
    public float tubeRadius = 50f;

    [Tooltip("Number of samples used to calculate spline length.")]
    [Min(8)]
    public int longitudinalSegments = 200;

    [Tooltip("Target radius of each hexagonal panel.")]
    [Min(0.1f)]
    public float hexRadius = 3f;

    [Tooltip("Gap between panels as a fraction of panel size. Set to 0 for a continuous surface.")]
    [Range(0f, 0.4f)]
    public float panelGap = 0f;

    [Tooltip("Generate faces pointing inward.")]
    public bool inwardFacing = true;

    [Header("UV Settings")]
    [Tooltip("Texture repeats along the spline.")]
    public float textureRepeats = 10f;

    [Tooltip("Texture repeats around the tube.")]
    public float radialTextureRepeats = 1f;

    [Header("Rendering")]
    public Material tubeMaterial;

    [Header("Collision")]
    [Tooltip("Generate a collider matching the generated forcefield mesh.")]
    public bool generateCollider = true;

    [Header("Generation")]
    [Tooltip("Generate the forcefield when the game starts.")]
    public bool generateOnStart = true;

    private Mesh generatedMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private const int HexSides = 6;

    private void Awake()
    {
        GetComponents();
    }

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateTube();
        }
    }

    private void GetComponents()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (generateCollider && meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
    }

    [ContextMenu("Generate Tube")]
    public void GenerateTube()
    {
        if (splineContainer == null)
        {
            Debug.LogWarning(
                "Spline Tube Generator: No Spline Container assigned.",
                this
            );
            return;
        }

        GetComponents();

        // Remove the previous generated mesh.
        ClearTube();

        // Determine whether the spline forms a closed loop.
        bool isClosed = splineContainer.Spline.Closed;

        // Calculate the approximate world-space spline length.
        float splineLength = CalculateSplineLength();

        if (splineLength <= 0.01f)
        {
            Debug.LogWarning("Spline length is too small.", this);
            return;
        }

        // Calculate the circumference of the tube.
        float circumference = Mathf.PI * 2f * tubeRadius;

        // Determine how many hexagons fit around the tube.
        int hexColumns = Mathf.Max(
            3,
            Mathf.RoundToInt(
                circumference / (hexRadius * 1.5f)
            )
        );

        // Adjust horizontal hexagon size to close the circumference.
        float actualHexRadius = circumference / (hexColumns * 1.5f);

        // Calculate the number of rows along the spline.
        float estimatedRowSpacing = Mathf.Sqrt(3f) * actualHexRadius;

        int hexRows = isClosed
            ? Mathf.Max(3, Mathf.RoundToInt(splineLength / estimatedRowSpacing))
            : Mathf.Max(1, Mathf.CeilToInt(splineLength / estimatedRowSpacing));

        // Distribute rows evenly along the spline.
        float rowSpacing = splineLength / hexRows;

        // Adjust vertical hexagon size to fit the spline length.
        float verticalRadius = rowSpacing / Mathf.Sqrt(3f);

        // Distance between hexagon centres around circumference.
        float columnSpacing = circumference / hexColumns;

        // Build mesh data.
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        float gapScale = 1f - panelGap;

        // Generate hexagonal panels.
        for (int row = 0; row < hexRows; row++)
        {
            // Stagger alternate rows.
            float rowOffset = (row % 2) * columnSpacing * 0.5f;

            float centreDistance = isClosed
                ? row * rowSpacing
                : (row + 0.5f) * rowSpacing;

            for (int column = 0; column < hexColumns; column++)
            {
                float centreU = column * columnSpacing + rowOffset;

                int centreIndex = vertices.Count;

                // Calculate the centre of the hexagon.
                Vector3 centrePosition = GetSurfacePosition(
                    centreDistance,
                    centreU,
                    splineLength,
                    circumference
                );

                vertices.Add(transform.InverseTransformPoint(centrePosition));

                uvs.Add(new Vector2(
                    centreU / circumference * radialTextureRepeats,
                    centreDistance / splineLength * textureRepeats
                ));

                // Generate the six corners.
                for (int side = 0; side < HexSides; side++)
                {
                    float angle = side * 60f * Mathf.Deg2Rad;

                    float offsetU =
                        Mathf.Cos(angle) * actualHexRadius * gapScale;

                    float offsetV =
                        Mathf.Sin(angle) * verticalRadius * gapScale;

                    float vertexU = centreU + offsetU;
                    float vertexDistance = centreDistance + offsetV;

                    // Convert the panel vertex onto the curved spline surface.
                    Vector3 worldPosition = GetSurfacePosition(
                        vertexDistance,
                        vertexU,
                        splineLength,
                        circumference
                    );

                    vertices.Add(
                        transform.InverseTransformPoint(worldPosition)
                    );

                    uvs.Add(new Vector2(
                        vertexU / circumference * radialTextureRepeats,
                        vertexDistance / splineLength * textureRepeats
                    ));
                }

                // Create the six triangular faces of the hexagon.
                for (int side = 0; side < HexSides; side++)
                {
                    int current = centreIndex + 1 + side;
                    int next = centreIndex + 1 + (side + 1) % HexSides;

                    triangles.Add(centreIndex);

                    if (inwardFacing)
                    {
                        triangles.Add(next);
                        triangles.Add(current);
                    }
                    else
                    {
                        triangles.Add(current);
                        triangles.Add(next);
                    }
                }
            }
        }

        // Create the generated mesh.
        generatedMesh = new Mesh();
        generatedMesh.name = "Generated Hexagonal Forcefield";
        generatedMesh.indexFormat =
            UnityEngine.Rendering.IndexFormat.UInt32;

        generatedMesh.SetVertices(vertices);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0);

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        // Assign mesh to the MeshFilter.
        meshFilter.sharedMesh = generatedMesh;

        // Assign material.
        if (tubeMaterial != null)
        {
            meshRenderer.sharedMaterial = tubeMaterial;
        }

        // Generate a collider using the exact same mesh.
        if (generateCollider)
        {
            if (meshCollider == null)
            {
                meshCollider = GetComponent<MeshCollider>();

                if (meshCollider == null)
                {
                    meshCollider = gameObject.AddComponent<MeshCollider>();
                }
            }

            // Force a non-convex collider for the static forcefield.
            meshCollider.convex = false;

            // Clear the previous collider mesh.
            meshCollider.sharedMesh = null;

            // Assign the generated forcefield mesh.
            meshCollider.sharedMesh = generatedMesh;
        }

        Debug.Log(
            $"Hexagonal Forcefield Generated! " +
            $"Rows: {hexRows}, Columns: {hexColumns}",
            this
        );
    }

    private float CalculateSplineLength()
    {
        float length = 0f;
        Vector3 previousPosition = Vector3.zero;

        for (int i = 0; i <= longitudinalSegments; i++)
        {
            float t = (float)i / longitudinalSegments;

            splineContainer.Evaluate(
                t,
                out float3 position,
                out float3 tangent,
                out float3 up
            );

            Vector3 currentPosition = position;

            if (i > 0)
            {
                length += Vector3.Distance(
                    previousPosition,
                    currentPosition
                );
            }

            previousPosition = currentPosition;
        }

        return length;
    }

    private Vector3 GetSurfacePosition(
        float distance,
        float circumferencePosition,
        float splineLength,
        float circumference
    )
    {
        bool isClosed = splineContainer.Spline.Closed;

        // Wrap longitudinal position for closed splines.
        float sampleDistance = isClosed
            ? Mathf.Repeat(distance, splineLength)
            : Mathf.Clamp(distance, 0f, splineLength);

        float t = sampleDistance / splineLength;

        // Evaluate spline.
        splineContainer.Evaluate(
            t,
            out float3 splinePosition,
            out float3 splineTangent,
            out float3 splineUp
        );

        Vector3 position = splinePosition;
        Vector3 tangent = ((Vector3)splineTangent).normalized;
        Vector3 up = ((Vector3)splineUp).normalized;

        // Construct a perpendicular frame.
        Vector3 right = Vector3.Cross(tangent, up);

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.Cross(tangent, Vector3.up);

            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.Cross(tangent, Vector3.right);
            }
        }

        right.Normalize();
        up = Vector3.Cross(right, tangent).normalized;

        // Calculate radial direction around the tube.
        float angle = circumferencePosition / circumference * Mathf.PI * 2f;

        Vector3 radialDirection =
            right * Mathf.Cos(angle) +
            up * Mathf.Sin(angle);

        return position + radialDirection * tubeRadius;
    }

    [ContextMenu("Clear Generated Tube")]
    public void ClearTube()
    {
        if (meshFilter == null)
        {
            GetComponents();
        }

        // Clear the collider reference if it uses our generated mesh.
        if (meshCollider != null &&
            meshCollider.sharedMesh == generatedMesh)
        {
            meshCollider.sharedMesh = null;
        }

        // Clear the MeshFilter reference.
        if (meshFilter != null &&
            meshFilter.sharedMesh == generatedMesh)
        {
            meshFilter.sharedMesh = null;
        }

        // Destroy generated mesh.
        if (generatedMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedMesh);
            }
            else
            {
                DestroyImmediate(generatedMesh);
            }

            generatedMesh = null;
        }
    }

    private void OnDestroy()
    {
        ClearTube();
    }
}
