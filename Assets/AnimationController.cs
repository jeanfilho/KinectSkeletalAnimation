using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public KinectSensor Sensor;
    public Skeleton Skeleton;
    public SkinnedMeshRenderer ReferenceMesh;
    public MeshFilter AnimatedMesh;

    private List<Vector3> _vertices;

    void Awake()
    {
        Sensor = KinectSensor.GetDefault();
        LoadModel();
    }

    void Update()
    {
        UpdateBones();
        UpdateMesh();
    }

    //Load model from unity, map model bones to our bone structure and create a skeleton
    private void LoadModel()
    {
        //Clone the referenced mesh
        AnimatedMesh.mesh = ReferenceMesh.sharedMesh.CloneMesh();
        AnimatedMesh.GetComponent<Renderer>().materials = ReferenceMesh.GetComponent<Renderer>().materials;
        _vertices = new List<Vector3>(AnimatedMesh.mesh.vertices.Length);

        var basePoseVertices = new Vector3[ReferenceMesh.sharedMesh.vertices.Length];
        var nameBoneDictionary = new Dictionary<string, BaseBone>();
        var vertexIdBoneWeightDictionary = new Dictionary<int, Dictionary<string, float>>();
        
        //Load vertices for basePose
        Array.Copy(ReferenceMesh.sharedMesh.vertices, basePoseVertices, basePoseVertices.Length);

        //bones could for example be pulled from a SkinnedMeshRenderer
        ReferenceMesh.rootBone.name = "root";
        nameBoneDictionary.Add(ReferenceMesh.rootBone.name, new RootBone(ReferenceMesh.rootBone.localPosition, ReferenceMesh.rootBone.localRotation));
        foreach (var bone in ReferenceMesh.bones)
        {
            if (ReferenceMesh.rootBone == bone) continue;
            nameBoneDictionary.Add(bone.name, new Bone(bone.localPosition, bone.localRotation, bone.parent.name));
        }
        
        //Unity BoneWeight class can assign up to four bones to each vertex, acessable via bone inicies
        var boneWeights = ReferenceMesh.sharedMesh.boneWeights;
        for (var i = 0; i < basePoseVertices.Length; i++)
        {
            Dictionary<string, float> dic = new Dictionary<string, float>();
            var name0 = ReferenceMesh.bones[boneWeights[i].boneIndex0].name;
            var name1 = ReferenceMesh.bones[boneWeights[i].boneIndex1].name;
            var name2 = ReferenceMesh.bones[boneWeights[i].boneIndex2].name;
            var name3 = ReferenceMesh.bones[boneWeights[i].boneIndex3].name;

            dic.Add(name0, boneWeights[i].weight0);
            if (!dic.ContainsKey(name1))
                dic.Add(name1, boneWeights[i].weight1);
            if (!dic.ContainsKey(name2))
                dic.Add(name2, boneWeights[i].weight2);
            if (!dic.ContainsKey(name3))
                dic.Add(name3, boneWeights[i].weight3);
            vertexIdBoneWeightDictionary.Add(i, dic);
        }

        //Create a skeleton
        Skeleton = new Skeleton(nameBoneDictionary, vertexIdBoneWeightDictionary, basePoseVertices);
    }

    //Update skeleton using kinect sensor data
    private void UpdateBones()
    {
        //TODO - do actual implementation, this is just a test
        Skeleton.BoneIdBoneDictionary["root"].LocalRotation *= Quaternion.Euler(100, 45, 50);
    }

    //Apply the bone transformations to the mesh
    private void UpdateMesh()
    {
        AnimatedMesh.mesh.vertices = Skeleton.UpdatePose(AnimatedMesh.mesh.vertices);
        AnimatedMesh.mesh.RecalculateBounds();
    }
}
