using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton
{
    public readonly Dictionary<string, BaseBone> BoneIdBoneDictionary;
    public readonly Dictionary<int, Dictionary<string, float>> VertexIdBoneWeightDictionary;

    public readonly Vector3[] BasePoseVertices;
    
    //Root bone should have ID == "root"
    public Skeleton(Dictionary<string, BaseBone> boneIdBoneDictionary, Dictionary<int, Dictionary<string, float>> vertexIdBoneWeightDictionary, Vector3[] basePoseVertices)
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
    }


    //Transform mesh vertices accourding to the transformation of each bone
    public Vector3[] UpdatePose(Vector3[] currentMesh)
    {
        //Get the current pose position for every vertex
        for (int i = 0; i < BasePoseVertices.Length; i++)
        {
            var finalPosition = Vector4.zero;

            //Sum over all bone influences
            foreach (var boneWeight in VertexIdBoneWeightDictionary[i])
            {
                //Value too small to be considered - skip this bone
                if(Math.Abs(boneWeight.Value) < 0.000001f)
                    continue;

                var currentBone = BoneIdBoneDictionary[boneWeight.Key];
                var basePoseMatrix = currentBone.GetLocalBasePoseTransformation(this);
                var currentPoseMatrix = currentBone.GetWorldCurrentPoseTransformation(this);
                var currentPosition = new Vector4(BasePoseVertices[i].x, BasePoseVertices[i].y, BasePoseVertices[i].z, 1);
                var updatedPosition = currentPoseMatrix * basePoseMatrix * currentPosition;
                updatedPosition *= boneWeight.Value;

                finalPosition += updatedPosition;
            }
            //Normalize bone influences
            currentMesh[i] = finalPosition;
        }

        return currentMesh;
    }
}


public abstract class BaseBone
{
    public Vector3 LocalPosition; //Bone (link) position without any rotation applied to it in local space
    public Quaternion LocalRotation;

    protected BaseBone(Vector3 localPosition, Quaternion localRotation)
    {
        LocalPosition = localPosition;
        LocalRotation = localRotation;
    }


    public abstract Matrix4x4 GetLocalBasePoseTransformation(Skeleton skeleton);
    public abstract Matrix4x4 GetWorldCurrentPoseTransformation(Skeleton skeleton);
}

public class RootBone : BaseBone
{
    public RootBone(Vector3 localPosition, Quaternion localRotation) : base(localPosition, localRotation) { }

    public override Matrix4x4 GetLocalBasePoseTransformation(Skeleton skeleton)
    {
        return Matrix4x4.TRS(Vector3.zero, LocalRotation, Vector3.one).inverse;
    }

    public override Matrix4x4 GetWorldCurrentPoseTransformation(Skeleton skeleton)
    {
        return Matrix4x4.TRS(LocalPosition, LocalRotation, Vector3.one);
    }
}

public class Bone : BaseBone
{
    public readonly string PreviousBoneId;

    public Bone(Vector3 localPosition, Quaternion localRotation, string previousBoneId) : base(localPosition, localRotation)
    {
        PreviousBoneId = previousBoneId;
    }

    public override Matrix4x4 GetLocalBasePoseTransformation(Skeleton skeleton)
    {
        BaseBone previousBone;
       if(!skeleton.BoneIdBoneDictionary.TryGetValue(PreviousBoneId, out previousBone))
            Debug.LogException(new Exception("Bone not found: " + PreviousBoneId));
        return Matrix4x4.TRS(LocalPosition, LocalRotation, Vector3.one).inverse * previousBone.GetLocalBasePoseTransformation(skeleton);
    }

    public override Matrix4x4 GetWorldCurrentPoseTransformation(Skeleton skeleton)
    {
        BaseBone previousBone;
        if (!skeleton.BoneIdBoneDictionary.TryGetValue(PreviousBoneId, out previousBone))
            Debug.LogException(new Exception("Bone not found: " + PreviousBoneId));
        return previousBone.GetWorldCurrentPoseTransformation(skeleton) * Matrix4x4.TRS(LocalPosition, LocalRotation, Vector3.one);
    }
}