namespace EFP.Rendering;

public static class MeshFactory
{
    public static (float[] Vertices, uint[] Indices) CreatePlane()
    {
        var vertices = new[]
        {
            -0.5f, 0f, -0.5f, 0.24f, 0.25f, 0.27f,
            0.5f, 0f, -0.5f, 0.26f, 0.27f, 0.29f,
            0.5f, 0f, 0.5f, 0.30f, 0.31f, 0.33f,
            -0.5f, 0f, 0.5f, 0.28f, 0.29f, 0.31f
        };

        var indices = new uint[]
        {
            0, 2, 1,
            0, 3, 2
        };

        return (vertices, indices);
    }

    public static (float[] Vertices, uint[] Indices) CreateCube()
    {
        var vertices = new[]
        {
            -0.5f, -0.5f, -0.5f, 0.87f, 0.58f, 0.20f,
            0.5f, -0.5f, -0.5f, 0.87f, 0.58f, 0.20f,
            0.5f, 0.5f, -0.5f, 0.87f, 0.58f, 0.20f,
            -0.5f, 0.5f, -0.5f, 0.87f, 0.58f, 0.20f,
            -0.5f, -0.5f, 0.5f, 0.95f, 0.66f, 0.24f,
            0.5f, -0.5f, 0.5f, 0.95f, 0.66f, 0.24f,
            0.5f, 0.5f, 0.5f, 0.95f, 0.66f, 0.24f,
            -0.5f, 0.5f, 0.5f, 0.95f, 0.66f, 0.24f
        };

        var indices = new uint[]
        {
            0, 1, 2, 2, 3, 0,
            4, 5, 6, 6, 7, 4,
            0, 4, 7, 7, 3, 0,
            1, 5, 6, 6, 2, 1,
            3, 2, 6, 6, 7, 3,
            0, 1, 5, 5, 4, 0
        };

        return (vertices, indices);
    }
}