using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;
using Vector4 = Windows.Kinect.Vector4;

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

    public static Quaternion ToQuaternion(this Vector4 vector4, bool mirror)
    {
        return new Quaternion(vector4.X, (mirror ? -1 : 1) * vector4.Y, (mirror ? -1 : 1) * vector4.Z, vector4.W);
    }

    public static Vector3 ToUnityVector3(this CameraSpacePoint cameraSpacePoint)
    {
        return new Vector3(cameraSpacePoint.X, cameraSpacePoint.Y, cameraSpacePoint.Z);
    }
}
