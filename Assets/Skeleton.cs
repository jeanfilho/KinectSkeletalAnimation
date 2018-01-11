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
        if(!BoneIdBoneDictionary.ContainsKey("root"))
            Debug.LogException(new Exception("No \"root\" bone found in dictionary - Mark the root bone with the id \"root\""));


        //Assign variables
        BoneIdBoneDictionary = boneIdBoneDictionary;
        VertexIdBoneWeightDictionary = vertexIdBoneWeightDictionary;
        BasePoseVertices = basePoseVertices;
    }

    //Transform mesh vertices accourding to the transformation of each bone
    public Vector3[] GetCurrentPose()
    {
        //Deep clone vertices to keep basePoseVertices intact
        var result = new Vector3[BasePoseVertices.Length];
        Array.Copy(BasePoseVertices, result, BasePoseVertices.Length);

        //Number of bones - required to normalize the weights so that the sum over all weights = 1
        float numberOfBones = BoneIdBoneDictionary.Count;

        //Get the current pose position for every vertex
        for (int i = 0; i < BasePoseVertices.Length; i++)
        {
            var newPosition = Vector4.zero;

            //Sum over all bone influences
            foreach (var boneWeight in VertexIdBoneWeightDictionary[i])
            {
                //Value too small to be considered - skip this bone
                if(Math.Abs(boneWeight.Value) < 0.000001f)
                    continue;

                var currentBone = BoneIdBoneDictionary[boneWeight.Key];
                var basePoseMatrix = currentBone.GetLocalBasePoseTransformation();
                var currentPoseMatrix = currentBone.GetWorldCurrentPoseTransformation();
                newPosition += currentPoseMatrix * basePoseMatrix * new Vector4(BasePoseVertices[i].x, BasePoseVertices[i].y, BasePoseVertices[i].z, 1);
                newPosition *= boneWeight.Value;
            }
            //Normalize bone influences
            newPosition /= numberOfBones;
            result[i] = newPosition;
        }

        return result;
    }
}


public abstract class BaseBone
{
    public Vector3 LocalPosition; //Bone (link) position without any rotation applied to it in local space
    public Quaternion Rotation;

    protected BaseBone(Vector3 localPosition, Quaternion rotation)
    {
        LocalPosition = localPosition;
        Rotation = rotation;
    }


    public abstract Matrix4x4 GetLocalBasePoseTransformation();
    public abstract Matrix4x4 GetWorldCurrentPoseTransformation();
}

public class RootBone : BaseBone
{
    public RootBone(Vector3 localPosition, Quaternion rotation) : base(localPosition, rotation) { }

    public override Matrix4x4 GetLocalBasePoseTransformation()
    {
        return Matrix4x4.TRS(Vector3.zero, Rotation, Vector3.one).inverse;
    }

    public override Matrix4x4 GetWorldCurrentPoseTransformation()
    {
        return Matrix4x4.TRS(LocalPosition, Rotation, Vector3.one);
    }
}

public class Bone : BaseBone
{
    public BaseBone Previous;

    public Bone(Vector3 localPosition, Quaternion rotation, BaseBone previous) : base(localPosition, rotation)
    {
        Previous = previous;
    }

    public override Matrix4x4 GetLocalBasePoseTransformation()
    {
        return Matrix4x4.TRS(LocalPosition, Rotation, Vector3.one).inverse * Previous.GetLocalBasePoseTransformation();
    }

    public override Matrix4x4 GetWorldCurrentPoseTransformation()
    {
        return Previous.GetWorldCurrentPoseTransformation() * Matrix4x4.TRS(LocalPosition, Rotation, Vector3.one);
    }
}