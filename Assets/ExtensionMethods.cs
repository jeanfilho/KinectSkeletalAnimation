using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods {

    public static Mesh CloneMesh(this Mesh mesh)
    {
        var newMesh = new Mesh();
        var vertices = new Vector3[mesh.vertices.Length];
        Array.Copy(mesh.vertices, vertices, vertices.Length);

        newMesh.vertices = vertices;
        newMesh.triangles = mesh.triangles;
        newMesh.uv = mesh.uv;
        newMesh.uv2 = mesh.uv2;
        newMesh.uv3 = mesh.uv3;
        newMesh.uv4 = mesh.uv4;
        newMesh.subMeshCount = mesh.subMeshCount;
        newMesh.normals = mesh.normals;
        newMesh.colors = mesh.colors;
        newMesh.tangents = mesh.tangents;

        for (var i = 0; i < mesh.subMeshCount; i++)
        {
            newMesh.SetTriangles(mesh.GetTriangles(i), i);
        }

        return newMesh;
    }
}
