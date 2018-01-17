using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton
{
    public readonly Dictionary<string, BaseBone> BoneIdBoneDictionary;
    public readonly Dictionary<int, Dictionary<string, float>> VertexIdBoneWeightDictionary;

    public readonly Vector3[] BasePoseVertices;
    public readonly Vector3[] BasePoseNormals;

    //Root bone should have ID == "root"
    public Skeleton(Dictionary<string, BaseBone> boneIdBoneDictionary, Dictionary<int, Dictionary<string, float>> vertexIdBoneWeightDictionary, Vector3[] basePoseVertices, Vector3[] basePoseNormals)
    {
        //Check if weights are normalized
        foreach (var vertexIdBoneWeight in vertexIdBoneWeightDictionary)
        {
            foreach (var boneWeight in vertexIdBoneWeight.Value)
            {
                if (boneWeight.Value < 0f || boneWeight.Value > 1f)
                    Debug.LogException(new Exception("Weight at vertex " + vertexIdBoneWeight.Key + " for bone " + "\"" + boneWeight.Key + "\"" + " is not normalized: " + boneWeight.Value));
            }
        }

        //Check for root bone
        if(!boneIdBoneDictionary.ContainsKey("root"))
            Debug.LogException(new Exception("No \"root\" bone found in dictionary - Mark the root bone with the id \"root\""));
        
        //Assign variables
        BoneIdBoneDictionary = boneIdBoneDictionary;
        VertexIdBoneWeightDictionary = vertexIdBoneWeightDictionary;
        BasePoseVertices = basePoseVertices;
        BasePoseNormals = basePoseNormals;

        //Calculate base pose matrices
        foreach (var bone in BoneIdBoneDictionary)
            bone.Value.UpdateBasePoseTransformation(this);
    }


    //Transform mesh vertices accourding to the transformation of each bone
    public void UpdateVertices(out Vector3[] vertices, out Vector3[] normals)
    {
        //Update current pose matrices
        foreach (var bone in BoneIdBoneDictionary)
            bone.Value.UpdateCurrentPoseTransformation(this);

        vertices = new Vector3[BasePoseVertices.Length];
        normals = new Vector3[BasePoseNormals.Length];

        //Get the current pose position and normal for every vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            var finalPosition = Vector4.zero;
            var finalNormal = Vector4.zero;
            var weightSum = 0f;

            //Sum over all bone influences
            foreach (var boneWeight in VertexIdBoneWeightDictionary[i])
            {
                //Value too small to be considered - skip this bone
                if(Math.Abs(boneWeight.Value) < 0.000001f)
                    continue;

                var currentBone = BoneIdBoneDictionary[boneWeight.Key];
                var basePoseMatrix = currentBone.GetBasePoseTransformation().inverse;
                var currentPoseMatrix = currentBone.GetCurrentPoseTransformation();

                var currentPosition = new Vector4(BasePoseVertices[i].x, BasePoseVertices[i].y, BasePoseVertices[i].z, 1.0f);
                var updatedPosition = (currentPoseMatrix * basePoseMatrix * currentPosition) * boneWeight.Value;
                
                var currentNormal = new Vector4(BasePoseNormals[i].x, BasePoseNormals[i].y, BasePoseNormals[i].z, 0f);
                var updatedNormal = (currentPoseMatrix * basePoseMatrix * currentNormal) * boneWeight.Value;

                finalPosition += updatedPosition;
                finalNormal += updatedNormal;
                weightSum += boneWeight.Value;
            }
            //Normalize bone influences
            var normalizationFactor = 1f / weightSum;
            vertices[i] = finalPosition * normalizationFactor;
            normals[i] = (finalNormal * normalizationFactor).normalized;
        }
    }
}


public abstract class BaseBone
{
    public Vector3 LocalLinkPosition; //Bone (link) position without any rotation applied to it in local space
    public Quaternion LocalRotation;

    protected readonly Vector3 BaseLocalLinkPosition;
    protected readonly Quaternion BaseLocalRotation;

    protected Matrix4x4 BasePoseTransformation = Matrix4x4.identity;
    protected Matrix4x4 CurrentPoseTransformation = Matrix4x4.identity;

    protected BaseBone(Vector3 localLinkPosition, Quaternion localRotation)
    {
        LocalLinkPosition = localLinkPosition;
        BaseLocalLinkPosition = localLinkPosition;

        LocalRotation = localRotation;
        BaseLocalRotation = localRotation;
    }


    public Matrix4x4 GetBasePoseTransformation()
    {
        return BasePoseTransformation;
    }

    public Matrix4x4 GetCurrentPoseTransformation()
    {
        return CurrentPoseTransformation;
    }
    public abstract void UpdateBasePoseTransformation(Skeleton skeleton);
    public abstract void UpdateCurrentPoseTransformation(Skeleton skeleton);
}

public class RootBone : BaseBone
{
    public RootBone(Vector3 localLinkPosition, Quaternion localRotation) : base(localLinkPosition, localRotation) { }

    public override void UpdateBasePoseTransformation(Skeleton skeleton)
    {
        BasePoseTransformation = Matrix4x4.Translate(BaseLocalLinkPosition) * Matrix4x4.Rotate(BaseLocalRotation);
    }

    public override void UpdateCurrentPoseTransformation(Skeleton skeleton)
    {
        CurrentPoseTransformation = Matrix4x4.Translate(LocalLinkPosition) * Matrix4x4.Rotate(LocalRotation);
    }
}

public class Bone : BaseBone
{
    public readonly string PreviousBoneId;

    public Bone(Vector3 localLinkPosition, Quaternion localRotation, string previousBoneId) : base(localLinkPosition, localRotation)
    {
        PreviousBoneId = previousBoneId;
    }

    public override void UpdateBasePoseTransformation(Skeleton skeleton)
    {
        BaseBone previousBone;
        if (!skeleton.BoneIdBoneDictionary.TryGetValue(PreviousBoneId, out previousBone))
            Debug.LogException(new Exception("Bone not found: " + PreviousBoneId));

        previousBone.UpdateBasePoseTransformation(skeleton);
        BasePoseTransformation = previousBone.GetBasePoseTransformation() * Matrix4x4.Translate(BaseLocalLinkPosition) * Matrix4x4.Rotate(BaseLocalRotation);
    }

    public override void UpdateCurrentPoseTransformation(Skeleton skeleton)
    {
        BaseBone previousBone;
        if (!skeleton.BoneIdBoneDictionary.TryGetValue(PreviousBoneId, out previousBone))
            Debug.LogException(new Exception("Bone not found: " + PreviousBoneId));

        previousBone.UpdateCurrentPoseTransformation(skeleton);
        CurrentPoseTransformation = previousBone.GetCurrentPoseTransformation() * Matrix4x4.Translate(LocalLinkPosition) * Matrix4x4.Rotate(LocalRotation);
    }
}