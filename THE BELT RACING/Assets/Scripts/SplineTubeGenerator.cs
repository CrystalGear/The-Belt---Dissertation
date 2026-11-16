
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplineTubeGenerator: MonoBehaviour
{
    [Header("Spline")]
    [Tooltip("The spline that defines the path of the race track.")]
    public SplineContainer splineContainer;

    [Header("Tube Settings")]
    [Tooltip("Radius of the generated tube.")]
    [Min(0.1f)]
    public float tubeRadius = 15f;

    [Tooltip("Number of sections along the spline.")]
    [Min(2)]
    public int longitudinalSegments = 100;

    [Tooltip("Number of vertices around each tube section.")]
    [Min(3)]
    public int radialSegments = 16;

    [Tooltip("Generate faces pointing inward for an enclosed racing tunnel.")]
    public bool inwardFacing = true;

    [Header("UV Settings")]
    [Tooltip("Number of times the texture repeats along the track.")]
    public float textureRepeats = 10f;

    [Tooltip("Number of times the texture repeats around the tube.")]
    public float radialTextureRepeats = 1f;

    [Header("Rendering")]
    public Material tubeMaterial;

    [Header("Collision")]
    public bool generateCollider = true;

    [Header("Generation")]
    [Tooltip("Automatically generate the tube when the game starts.")]
    public bool generateOnStart = true;

    private Mesh generatedMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

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

        if (generateCollider)
        {
            meshCollider = GetComponent<MeshCollider>();

            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
        }
    }

    [ContextMenu("Generate Tube")]
    public void GenerateTube()
    {
        if (splineContainer == null)
        {
            Debug.LogWarning("Spline Tube Generator: No Spline Container assigned.", this);
            return;
        }

        GetComponents();

        // Create a new mesh.
        generatedMesh = new Mesh();
        generatedMesh.name = "Generated Race Track Tube";

        // Use 32-bit indices for larger tracks.
        generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int ringCount = longitudinalSegments + 1;
        int verticesPerRing = radialSegments + 1;

        Vector3[] vertices = new Vector3[ringCount * verticesPerRing];
        Vector2[] uvs = new Vector2[vertices.Length];

        int[] triangles = new int[
            longitudinalSegments * radialSegments * 6
        ];

        // Generate the tube vertices.
        for (int i = 0; i < ringCount; i++)
        {
            float t = (float)i / longitudinalSegments;

            // Evaluate the spline.
            splineContainer.Evaluate(
                t,
                out float3 splinePosition,
                out float3 splineTangent,
                out float3 splineUp
            );

            Vector3 position = splinePosition;
            Vector3 tangent = ((Vector3)splineTangent).normalized;
            Vector3 up = ((Vector3)splineUp).normalized;

            // Calculate the orientation of the tube.
            Vector3 right = Vector3.Cross(tangent, up).normalized;

            // Recalculate up to ensure a perpendicular orientation.
            up = Vector3.Cross(right, tangent).normalized;

            // Generate vertices around the tube.
            for (int j = 0; j < verticesPerRing; j++)
            {
                float angle = (float)j / radialSegments * Mathf.PI * 2f;

                Vector3 radialDirection =
                    right * Mathf.Cos(angle) +
                    up * Mathf.Sin(angle);

                int vertexIndex = i * verticesPerRing + j;

                vertices[vertexIndex] =
                    position + radialDirection * tubeRadius;

                // UV coordinates.
                uvs[vertexIndex] = new Vector2(
                    (float)j / radialSegments * radialTextureRepeats,
                    t * textureRepeats
                );
            }
        }

        // Generate triangle indices.
        int triangleIndex = 0;

        for (int i = 0; i < longitudinalSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * verticesPerRing + j;
                int next = current + verticesPerRing;

                // Two triangles per quad.
                if (inwardFacing)
                {
                    // Faces point inside the tube.
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next;

                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next + 1;
                    triangles[triangleIndex++] = next;
                }
                else
                {
                    // Faces point outside the tube.
                    triangles[triangleIndex++] = current;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = current + 1;

                    triangles[triangleIndex++] = current + 1;
                    triangles[triangleIndex++] = next;
                    triangles[triangleIndex++] = next + 1;
                }
            }
        }

        // Apply mesh data.
        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        // Assign mesh to the Mesh Filter.
        meshFilter.sharedMesh = generatedMesh;

        // Assign material.
        if (tubeMaterial != null)
        {
            GetComponent<MeshRenderer>().sharedMaterial = tubeMaterial;
        }

        // Generate collision mesh.
        if (generateCollider)
        {
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = generatedMesh;
        }

        Debug.Log("Spline Tube Generated Successfully!", this);
    }

    [ContextMenu("Clear Generated Tube")]
    public void ClearTube()
    {
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = null;
        }

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
        }

        if (generatedMesh != null)
        {
            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);

            generatedMesh = null;
        }
    }
}
