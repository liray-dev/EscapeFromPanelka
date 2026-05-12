using System.Numerics;
using Silk.NET.Assimp;
using File = System.IO.File;

namespace EFP.View.Rendering.Models;

public static class ModelLoader
{
    private const PostProcessSteps DefaultSteps =
        PostProcessSteps.Triangulate
        | PostProcessSteps.GenerateNormals
        | PostProcessSteps.JoinIdenticalVertices
        | PostProcessSteps.ImproveCacheLocality
        | PostProcessSteps.OptimizeMeshes;

    public static unsafe MeshData Load(string absolutePath, bool flipUv)
    {
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException($"Model not found: {absolutePath}");

        var assimp = Assimp.GetApi();
        var steps = DefaultSteps;
        if (flipUv) steps |= PostProcessSteps.FlipUVs;

        var scene = assimp.ImportFile(absolutePath, (uint)steps);
        try
        {
            if (scene == null || (scene->MFlags & Assimp.SceneFlagsIncomplete) != 0 ||
                scene->MRootNode == null || scene->MNumMeshes == 0)
            {
                var error = assimp.GetErrorStringS();
                throw new InvalidOperationException(
                    $"Assimp failed to load '{absolutePath}': {(string.IsNullOrEmpty(error) ? "no scene" : error)}");
            }

            var vertices = new List<float>();
            var indices = new List<uint>();
            uint vertexOffset = 0;

            for (uint meshIndex = 0; meshIndex < scene->MNumMeshes; meshIndex++)
            {
                var mesh = scene->MMeshes[meshIndex];
                if (mesh == null) continue;

                for (uint v = 0; v < mesh->MNumVertices; v++)
                {
                    var p = mesh->MVertices[v];
                    var n = mesh->MNormals != null
                        ? mesh->MNormals[v]
                        : new Vector3(0f, 1f, 0f);

                    vertices.Add(p.X);
                    vertices.Add(p.Y);
                    vertices.Add(p.Z);
                    vertices.Add(n.X);
                    vertices.Add(n.Y);
                    vertices.Add(n.Z);
                    vertices.Add(1f);
                    vertices.Add(1f);
                    vertices.Add(1f);
                }

                for (uint f = 0; f < mesh->MNumFaces; f++)
                {
                    var face = mesh->MFaces[f];
                    if (face.MNumIndices != 3) continue;
                    indices.Add(vertexOffset + face.MIndices[0]);
                    indices.Add(vertexOffset + face.MIndices[1]);
                    indices.Add(vertexOffset + face.MIndices[2]);
                }

                vertexOffset += mesh->MNumVertices;
            }

            if (indices.Count == 0)
                throw new InvalidOperationException(
                    $"Model '{absolutePath}' produced no triangles after import.");

            return new MeshData(vertices.ToArray(), indices.ToArray());
        }
        finally
        {
            if (scene != null) assimp.ReleaseImport(scene);
            assimp.Dispose();
        }
    }
}