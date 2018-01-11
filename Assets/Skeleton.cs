using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton
{
    private readonly Dictionary<string, BaseBone> _boneIdBoneDictionary;
    private readonly Dictionary<int, Dictionary<string, float>> _vertexIdBoneWeightDictionary; 

    //Root bone should have ID == "root"
    public Skeleton(Dictionary<string, BaseBone> boneIdBoneDictionary, Dictionary<int, Dictionary<string, float>> vertexIdBoneWeightDictionary)
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
        if(!_boneIdBoneDictionary.ContainsKey("root"))
            Debug.LogException(new Exception("No \"root\" bone found in dictionary - Mark the root bone with the id \"root\""));


        //Assign variables
        _boneIdBoneDictionary = boneIdBoneDictionary;
        _vertexIdBoneWeightDictionary = vertexIdBoneWeightDictionary;
    }

    //Transform mesh vertices accourding to the transformation of each bone
    public Vector3[] TransformVertices(Vector3[] basePoseVertices)
    {
        //Deep clone vertices to keep basePoseVertices intact
        var result = new Vector3[basePoseVertices.Length];
        Array.Copy(basePoseVertices, result, basePoseVertices.Length);

        //Number of bones - required to normalize the weights so that the sum over all weights = 1
        float numberOfBones = _boneIdBoneDictionary.Count;

        //Get the current pose position for every vertex
        for (int i = 0; i < basePoseVertices.Length; i++)
        {
            var newPosition = Vector4.zero;

            //Sum over all bone influences
            foreach (var boneWeight in _vertexIdBoneWeightDictionary[i])
            {
                //Value too small to be considered - skip this bone
                if(Math.Abs(boneWeight.Value) < 0.000001f)
                    continue;

                var currentBone = _boneIdBoneDictionary[boneWeight.Key];
                var basePoseMatrix = currentBone.GetLocalBasePoseTransformation();
                var currentPoseMatrix = currentBone.GetWorldCurrentPoseTransformation();
                newPosition += currentPoseMatrix * basePoseMatrix * new Vector4(basePoseVertices[i].x, basePoseVertices[i].y, basePoseVertices[i].z, 1);
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
    public BaseBone Previous;

    public abstract Matrix4x4 GetLocalBasePoseTransformation();
    public abstract Matrix4x4 GetWorldCurrentPoseTransformation();
}

public class RootBone : BaseBone
{
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
    public override Matrix4x4 GetLocalBasePoseTransformation()
    {
        return Matrix4x4.TRS(LocalPosition, Rotation, Vector3.one).inverse * Previous.GetLocalBasePoseTransformation();
    }

    public override Matrix4x4 GetWorldCurrentPoseTransformation()
    {
        return Previous.GetWorldCurrentPoseTransformation() * Matrix4x4.TRS(LocalPosition, Rotation, Vector3.one);
    }
}