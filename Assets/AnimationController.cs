using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public KinectSensor Sensor;
    public Skeleton Skeleton;
    public Mesh ModelMesh;

    void Awake()
    {
        Sensor = KinectSensor.GetDefault();
        LoadModel("DO YU KNO DA WAE!?");
    }

    void Update()
    {
        UpdateBones();
        UpdateMesh();
    }

    //Load model from the hard drive, map model bones to our bone structure and create a skeleton
    private void LoadModel(string path)
    {
        var basePoseVertices = new Vector3[0];
        var nameBoneDictionary = new Dictionary<string, BaseBone>();
        var vertexIdBoneWeightDictionary = new Dictionary<int, Dictionary<string, float>>();

        //TODO: load vertices

        //TODO: load bones - this is just as an example
        for(int i = 0; i < 0; i++)
        { 
            nameBoneDictionary.Add("root", new RootBone(Vector3.zero, Quaternion.identity));
        }

        //TODO: map vertices to bone id and weight - this is just an example
        for(int i = 0; i < 0; i++)
        {
            var boneWeightDictionary = new Dictionary<string, float>();

            //TODO: Map bone id to weights
            for(int j = 0; j < 0; j++)
            {
                var boneId = "root";
                boneWeightDictionary.Add(boneId, 1);
            }
            vertexIdBoneWeightDictionary.Add(0, boneWeightDictionary);
        }

        //Create a skeleton
        Skeleton = new Skeleton(nameBoneDictionary, vertexIdBoneWeightDictionary, basePoseVertices);
    }

    //Update skeleton using kinect sensor data
    private void UpdateBones()
    {
        //TODO
    }

    //Apply the bone transformations to the mesh
    private void UpdateMesh()
    {
        //TODO
        ModelMesh.vertices = Skeleton.GetCurrentPose();
    }
}
