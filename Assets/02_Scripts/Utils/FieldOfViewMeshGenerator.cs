using System.Collections.Generic;
using UnityEngine;

public static class FieldOfViewMeshGenerator
{
    /// <param name="squareWidth">Width of the centre rectangle (bridge between the two arc sectors).</param>
    /// <param name="fieldOfView">Total arc sweep angle in degrees. 0 = pure rectangle, full value = binocular.</param>
    public static Mesh Generate(float maxDistance, float fieldOfView, float squareWidth, float thickness, int segments = 16)
    {
        // Build 2D perimeter in XZ plane (clockwise)
        List<Vector3> perimeter = BuildPerimeter(maxDistance, fieldOfView, squareWidth, segments);

        // Extrude to 3D
        return Extrude(perimeter, thickness);
    }

    private static List<Vector3> BuildPerimeter(float maxDistance, float fieldOfView, float squareWidth, int segments)
    {
        List<Vector3> points = new();

        // halfSquareWidth positions the arc origins and back corners — these always match.
        float halfSquareWidth = squareWidth * 0.5f;
        float halfFov = fieldOfView * 0.5f;

        // Arc centres sit at the left/right edges of the centre rectangle.
        Vector3 leftOrigin = new Vector3(-halfSquareWidth, 0, 0);
        Vector3 rightOrigin = new Vector3(halfSquareWidth, 0, 0);

        float radius = maxDistance;

        // --- LEFT ARC ---
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfFov, 0f, t);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 p = new(leftOrigin.x + Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius);

            points.Add(p);
        }

        // --- RIGHT ARC ---
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(0f, halfFov, t);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 p = new(rightOrigin.x + Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius);

            points.Add(p);
        }

        // Add rear rectangle corners (close the shape — must match arc origins).
        points.Add(new Vector3(halfSquareWidth, 0, 0));
        points.Add(new Vector3(-halfSquareWidth, 0, 0));

        return points;
    }

    private static Mesh Extrude(List<Vector3> baseShape, float thickness)
    {
        int count = baseShape.Count;

        Vector3[] vertices = new Vector3[count * 2];

        // bottom
        for (int i = 0; i < count; i++)
            vertices[i] = baseShape[i];

        // top
        for (int i = 0; i < count; i++)
            vertices[i + count] = baseShape[i] + Vector3.up * thickness;

        List<int> triangles = new();

        // --- Bottom face (fan triangulation) ---
        for (int i = 1; i < count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        // --- Top face (reverse winding) ---
        int offset = count;
        for (int i = 1; i < count - 1; i++)
        {
            triangles.Add(offset);
            triangles.Add(offset + i + 1);
            triangles.Add(offset + i);
        }

        // --- Side walls ---
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            int a = i;
            int b = next;
            int c = next + offset;
            int d = i + offset;

            // First triangle
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);

            // Second triangle
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        Mesh mesh = new();
        mesh.name = "FieldOfViewMesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();


        Vector2[] uvs = new Vector2[vertices.Length];

        Bounds bounds = new Bounds(baseShape[0], Vector3.zero);
        for (int i = 1; i < baseShape.Count; i++)
            bounds.Encapsulate(baseShape[i]);

        // Project XZ → UV space
        for (int i = 0; i < count; i++)
        {
            Vector3 p = baseShape[i];

            float u = Mathf.InverseLerp(bounds.min.x, bounds.max.x, p.x);
            float v = Mathf.InverseLerp(bounds.min.z, bounds.max.z, p.z);

            uvs[i] = new Vector2(u, v);
            uvs[i + count] = new Vector2(u, v); // same for top
        }

        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}