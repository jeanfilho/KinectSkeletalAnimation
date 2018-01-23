using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiggedShaderHelper : MonoBehaviour
{

    private static ComputeBuffer boneweights, boneIDs, boneMatrices;
    private Material material;

    public void setMaterial(Material mat)
    {
        material = mat;
    }

    public void setBuffers(float[] weights, uint[] IDs, Matrix4x4[] matices)
    {
        boneweights = new ComputeBuffer(weights.Length, sizeof(float));
        boneIDs = new ComputeBuffer(IDs.Length, sizeof(uint));
        boneMatrices = new ComputeBuffer(matices.Length, sizeof(Matrix4x4));

        boneweights.SetData(weights);
        boneIDs.SetData(IDs);
        boneMatrices.SetData(matices);

        material.SetBuffer("_weights", boneweights);
        material.SetBuffer("_IDs", boneIDs);
        material.SetBuffer("_Matices", boneMatrices);
    }
}
