using Silk.NET.OpenGL;

namespace EFP.View.Rendering;

public static class MeshFactory
{
    public static (float[] Vertices, uint[] Indices, PrimitiveType PrimitiveType) CreatePlane()
    {
        const float y = 0f;

        var vertices = new[]
        {
            -0.5f, y, -0.5f, 0f, 1f, 0f, 0.19f, 0.21f, 0.24f,
            0.5f, y, -0.5f, 0f, 1f, 0f, 0.19f, 0.21f, 0.24f,
            0.5f, y, 0.5f, 0f, 1f, 0f, 0.19f, 0.21f, 0.24f,
            -0.5f, y, 0.5f, 0f, 1f, 0f, 0.19f, 0.21f, 0.24f
        };

        uint[] indices = [0, 1, 2, 2, 3, 0];
        return (vertices, indices, PrimitiveType.Triangles);
    }

    public static (float[] Vertices, uint[] Indices, PrimitiveType PrimitiveType) CreateCube()
    {
        var vertices = new[]
        {
            -0.5f, -0.5f, 0.5f, 0f, 0f, 1f, 0.78f, 0.51f, 0.22f,
            0.5f, -0.5f, 0.5f, 0f, 0f, 1f, 0.78f, 0.51f, 0.22f,
            0.5f, 0.5f, 0.5f, 0f, 0f, 1f, 0.90f, 0.64f, 0.31f,
            -0.5f, 0.5f, 0.5f, 0f, 0f, 1f, 0.90f, 0.64f, 0.31f,

            -0.5f, -0.5f, -0.5f, 0f, 0f, -1f, 0.71f, 0.45f, 0.18f,
            0.5f, -0.5f, -0.5f, 0f, 0f, -1f, 0.71f, 0.45f, 0.18f,
            0.5f, 0.5f, -0.5f, 0f, 0f, -1f, 0.82f, 0.55f, 0.24f,
            -0.5f, 0.5f, -0.5f, 0f, 0f, -1f, 0.82f, 0.55f, 0.24f,

            -0.5f, -0.5f, -0.5f, -1f, 0f, 0f, 0.66f, 0.41f, 0.17f,
            -0.5f, -0.5f, 0.5f, -1f, 0f, 0f, 0.66f, 0.41f, 0.17f,
            -0.5f, 0.5f, 0.5f, -1f, 0f, 0f, 0.77f, 0.51f, 0.23f,
            -0.5f, 0.5f, -0.5f, -1f, 0f, 0f, 0.77f, 0.51f, 0.23f,

            0.5f, -0.5f, -0.5f, 1f, 0f, 0f, 0.68f, 0.43f, 0.18f,
            0.5f, -0.5f, 0.5f, 1f, 0f, 0f, 0.68f, 0.43f, 0.18f,
            0.5f, 0.5f, 0.5f, 1f, 0f, 0f, 0.80f, 0.56f, 0.25f,
            0.5f, 0.5f, -0.5f, 1f, 0f, 0f, 0.80f, 0.56f, 0.25f,

            -0.5f, 0.5f, -0.5f, 0f, 1f, 0f, 0.93f, 0.76f, 0.41f,
            0.5f, 0.5f, -0.5f, 0f, 1f, 0f, 0.93f, 0.76f, 0.41f,
            0.5f, 0.5f, 0.5f, 0f, 1f, 0f, 0.98f, 0.84f, 0.48f,
            -0.5f, 0.5f, 0.5f, 0f, 1f, 0f, 0.98f, 0.84f, 0.48f,

            -0.5f, -0.5f, -0.5f, 0f, -1f, 0f, 0.57f, 0.36f, 0.15f,
            0.5f, -0.5f, -0.5f, 0f, -1f, 0f, 0.57f, 0.36f, 0.15f,
            0.5f, -0.5f, 0.5f, 0f, -1f, 0f, 0.57f, 0.36f, 0.15f,
            -0.5f, -0.5f, 0.5f, 0f, -1f, 0f, 0.57f, 0.36f, 0.15f
        };

        uint[] indices =
        [
            0, 1, 2, 2, 3, 0,
            4, 6, 5, 6, 4, 7,
            8, 9, 10, 10, 11, 8,
            12, 14, 13, 14, 12, 15,
            16, 17, 18, 18, 19, 16,
            20, 22, 21, 22, 20, 23
        ];

        return (vertices, indices, PrimitiveType.Triangles);
    }

    public static (float[] Vertices, uint[] Indices, PrimitiveType PrimitiveType) CreateGrid(int halfExtent, float step,
        float y)
    {
        var vertices = new List<float>();
        var indices = new List<uint>();
        uint index = 0;

        for (var i = -halfExtent; i <= halfExtent; i++)
        {
            var position = i * step;
            var major = i == 0;
            var color = major ? new[] { 0.35f, 0.40f, 0.48f } : new[] { 0.24f, 0.27f, 0.32f };

            vertices.AddRange([-halfExtent * step, y, position, 0f, 1f, 0f, color[0], color[1], color[2]]);
            vertices.AddRange([halfExtent * step, y, position, 0f, 1f, 0f, color[0], color[1], color[2]]);
            indices.Add(index++);
            indices.Add(index++);

            vertices.AddRange([position, y, -halfExtent * step, 0f, 1f, 0f, color[0], color[1], color[2]]);
            vertices.AddRange([position, y, halfExtent * step, 0f, 1f, 0f, color[0], color[1], color[2]]);
            indices.Add(index++);
            indices.Add(index++);
        }

        return (vertices.ToArray(), indices.ToArray(), PrimitiveType.Lines);
    }
}
