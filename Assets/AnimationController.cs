using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public KinectSensor Sensor;
    public Skeleton Skeleton;
    public SkinnedMeshRenderer BaseMesh;
    public MeshFilter AnimatedMesh;

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
        AnimatedMesh.mesh = BaseMesh.sharedMesh.CloneMesh();
        AnimatedMesh.GetComponent<Renderer>().materials = BaseMesh.GetComponent<Renderer>().materials;

        var basePoseVertices = new Vector3[BaseMesh.sharedMesh.vertices.Length];
        var nameBoneDictionary = new Dictionary<string, BaseBone>();
        var vertexIdBoneWeightDictionary = new Dictionary<int, Dictionary<string, float>>();
        
        //Load vertices
        Array.Copy(BaseMesh.sharedMesh.vertices, basePoseVertices, basePoseVertices.Length);

        //bones could for example be pulled from a SkinnedMeshRenderer
        BaseMesh.rootBone.name = "root";
        nameBoneDictionary.Add(BaseMesh.rootBone.name, new RootBone(BaseMesh.rootBone.localPosition, BaseMesh.rootBone.localRotation));
        foreach (var bone in BaseMesh.bones)
        {
            if (BaseMesh.rootBone == bone) continue;
            nameBoneDictionary.Add(bone.name, new Bone(bone.localPosition, bone.localRotation, bone.parent.name));
        }
        
        //Unity BoneWeight class can assign up to four bones to each vertex, acessable via bone inicies
        var boneWeights = BaseMesh.sharedMesh.boneWeights;
        for (var i = 0; i < basePoseVertices.Length; i++)
        {
            Dictionary<string, float> dic = new Dictionary<string, float>();
            var name0 = BaseMesh.bones[boneWeights[i].boneIndex0].name;
            var name1 = BaseMesh.bones[boneWeights[i].boneIndex1].name;
            var name2 = BaseMesh.bones[boneWeights[i].boneIndex2].name;
            var name3 = BaseMesh.bones[boneWeights[i].boneIndex3].name;

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
        Skeleton.BoneIdBoneDictionary["Upper arm.R"].LocalRotation = Quaternion.Euler(0, 10 * Time.timeSinceLevelLoad, 0);
    }

    //Apply the bone transformations to the mesh
    private void UpdateMesh()
    {
        Vector3[] vertices = new Vector3[AnimatedMesh.mesh.vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = AnimatedMesh.mesh.vertices[i];

        AnimatedMesh.mesh.vertices = Skeleton.UpdatePose(vertices);
        AnimatedMesh.mesh.UploadMeshData(false);
    }
}
