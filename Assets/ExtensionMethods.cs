using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods {

    public static Mesh CloneMesh(this Mesh mesh)
    {
        var newMesh = new Mesh
        {
            vertices = mesh.vertices,
            triangles = mesh.triangles,
            uv = mesh.uv,
            uv2 = mesh.uv2,
            uv3 = mesh.uv3,
            uv4 = mesh.uv4,
            subMeshCount = mesh.subMeshCount,
            normals = mesh.normals,
            colors = mesh.colors,
            tangents = mesh.tangents
        };

        for (var i = 0; i < mesh.subMeshCount; i++)
        {
            newMesh.SetTriangles(mesh.GetTriangles(i), i);
        }

        return newMesh;
    }
}
