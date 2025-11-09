using UnityEngine;

public static class MeshUtils
{
    public static Mesh MergeMeshes(Mesh[] meshes)
    {
        CombineInstance[] combine = new CombineInstance[meshes.Length];

        for (int i = 0; i < meshes.Length; i++)
        {
            combine[i].mesh = meshes[i];
            combine[i].transform = Matrix4x4.identity; // optional transform per mesh
        }

        Mesh mergedMesh = new Mesh();
        mergedMesh.name = "MergedMesh";
        mergedMesh.CombineMeshes(combine, true, true); // merge submeshes & apply transforms

        return mergedMesh;
    }

    public static Mesh CreateRectangleMesh(params Vector3[] vertices)
    {
        // Define the triangles (indices)
        int[] triangles = new int[6];
        // Triangle 1
        triangles[0] = 0;
        triangles[1] = 2;
        triangles[2] = 1;
        // Triangle 2
        triangles[3] = 2;
        triangles[4] = 3;
        triangles[5] = 1;

        // Create a new mesh
        Mesh mesh = new();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Recalculate the normals for proper shading
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <param name="originPoint"> The center of the "circle" </param>
    public static Mesh CreateCircularTriangleMesh(Vector3 originPoint, Vector3 sidePoint, Vector3 forwardPoint, Vector2 forward, bool invertDrawOrder, int segments = 10)
    {
        // Calculate the radius of the circle
        float radius = Vector3.Distance(originPoint, sidePoint);

        // Calculate the angle between leftPoint and forwardPoint
        Vector3 centerToSide = sidePoint - originPoint;
        Vector3 centerToForward = forwardPoint - originPoint;
        float angle = Vector3.SignedAngle(centerToSide, centerToForward, Vector3.up);

        float angleForward = MathUtils.GetAngleFromDirection(forward);

        // Calculate vertices for the sector
        Vector3[] vertices = new Vector3[segments + 2]; // One additional vertex for the center
        vertices[0] = originPoint; // Center vertex
        for (int i = 1; i <= segments + 1; i++)
        {
            float theta = Mathf.Deg2Rad * angle / segments * (i - 1);
            float x = originPoint.x - radius * Mathf.Sin(theta);
            float z = originPoint.z + radius * Mathf.Cos(theta);
            Vector3 vertice = new(x, originPoint.y, z);
            vertices[i] = MathUtils.RotatePointAroundPivot(vertice, originPoint, new Vector3(0, angleForward, 0));
        }

        // Define the triangles (indices) for the sector
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            if (invertDrawOrder)
            {
                triangles[i * 3 + 2] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 0] = i + 2;
            }
            else
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        // Create a new mesh
        Mesh mesh = new();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Recalculate the normals for proper shading
        mesh.RecalculateNormals();
        return mesh;
    }
}
