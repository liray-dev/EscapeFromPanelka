namespace EFP.Rendering;

public static class MeshFactory
{
    public static (float[] Vertices, uint[] Indices) CreatePlane()
    {
        const float y = 0f;

        var vertices = new[]
        {
            -0.5f, y, -0.5f, 0f, 1f, 0f, 0.35f, 0.37f, 0.40f,
            0.5f, y, -0.5f, 0f, 1f, 0f, 0.35f, 0.37f, 0.40f,
            0.5f, y, 0.5f, 0f, 1f, 0f, 0.35f, 0.37f, 0.40f,
            -0.5f, y, 0.5f, 0f, 1f, 0f, 0.35f, 0.37f, 0.40f
        };

        uint[] indices = [0, 1, 2, 2, 3, 0];
        return (vertices, indices);
    }

    public static (float[] Vertices, uint[] Indices) CreateCube()
    {
        var vertices = new[]
        {
            // Front
            -0.5f, 0f, 0.5f, 0f, 0f, 1f, 0.85f, 0.56f, 0.24f,
            0.5f, 0f, 0.5f, 0f, 0f, 1f, 0.85f, 0.56f, 0.24f,
            0.5f, 1f, 0.5f, 0f, 0f, 1f, 0.93f, 0.68f, 0.32f,
            -0.5f, 1f, 0.5f, 0f, 0f, 1f, 0.93f, 0.68f, 0.32f,

            // Back
            -0.5f, 0f, -0.5f, 0f, 0f, -1f, 0.78f, 0.49f, 0.20f,
            0.5f, 0f, -0.5f, 0f, 0f, -1f, 0.78f, 0.49f, 0.20f,
            0.5f, 1f, -0.5f, 0f, 0f, -1f, 0.88f, 0.60f, 0.26f,
            -0.5f, 1f, -0.5f, 0f, 0f, -1f, 0.88f, 0.60f, 0.26f,

            // Left
            -0.5f, 0f, -0.5f, -1f, 0f, 0f, 0.72f, 0.46f, 0.18f,
            -0.5f, 0f, 0.5f, -1f, 0f, 0f, 0.72f, 0.46f, 0.18f,
            -0.5f, 1f, 0.5f, -1f, 0f, 0f, 0.83f, 0.57f, 0.25f,
            -0.5f, 1f, -0.5f, -1f, 0f, 0f, 0.83f, 0.57f, 0.25f,

            // Right
            0.5f, 0f, -0.5f, 1f, 0f, 0f, 0.74f, 0.48f, 0.20f,
            0.5f, 0f, 0.5f, 1f, 0f, 0f, 0.74f, 0.48f, 0.20f,
            0.5f, 1f, 0.5f, 1f, 0f, 0f, 0.86f, 0.60f, 0.28f,
            0.5f, 1f, -0.5f, 1f, 0f, 0f, 0.86f, 0.60f, 0.28f,

            // Top
            -0.5f, 1f, -0.5f, 0f, 1f, 0f, 0.95f, 0.76f, 0.38f,
            0.5f, 1f, -0.5f, 0f, 1f, 0f, 0.95f, 0.76f, 0.38f,
            0.5f, 1f, 0.5f, 0f, 1f, 0f, 0.98f, 0.82f, 0.42f,
            -0.5f, 1f, 0.5f, 0f, 1f, 0f, 0.98f, 0.82f, 0.42f,

            // Bottom
            -0.5f, 0f, -0.5f, 0f, -1f, 0f, 0.61f, 0.39f, 0.16f,
            0.5f, 0f, -0.5f, 0f, -1f, 0f, 0.61f, 0.39f, 0.16f,
            0.5f, 0f, 0.5f, 0f, -1f, 0f, 0.61f, 0.39f, 0.16f,
            -0.5f, 0f, 0.5f, 0f, -1f, 0f, 0.61f, 0.39f, 0.16f
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

        return (vertices, indices);
    }
}