using System;
using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public KinectSensor Sensor;
    public BodyFrameReader Reader;
    public Body[] Data;
    public Skeleton Skeleton;
    public SkinnedMeshRenderer ReferenceMesh;
    public MeshFilter AnimatedMesh;

    private List<Vector3> _vertices;
    private Dictionary<string, JointType> _knucklesToKinect = new Dictionary<string, JointType>();

    void Awake()
    {
        Sensor = KinectSensor.GetDefault();
        Sensor.Open();
        Reader = Sensor.BodyFrameSource.OpenReader();
        MapKinectSkeletonToModel();
        LoadModel();
    }

    void Update()
    {
        UpdateBones();
        UpdateMesh();
    }

    void OnApplicationQuit()
    {
        Reader.Dispose();
        if(Sensor.IsOpen)
            Sensor.Close();
    }

    //Load model from unity, map model bones to our bone structure and create a skeleton
    private void LoadModel()
    {
        //Clone the referenced mesh
        AnimatedMesh.mesh = ReferenceMesh.sharedMesh.CloneMesh();
        AnimatedMesh.GetComponent<Renderer>().materials = ReferenceMesh.GetComponent<Renderer>().materials;
        _vertices = new List<Vector3>(AnimatedMesh.mesh.vertices.Length);

        var basePoseVertices = new Vector3[ReferenceMesh.sharedMesh.vertices.Length];
        var basePoseNormals = new Vector3[ReferenceMesh.sharedMesh.normals.Length];
        var nameBoneDictionary = new Dictionary<string, BaseBone>();
        var vertexIdBoneWeightDictionary = new Dictionary<int, Dictionary<string, float>>();

        //Load vertices and normals for basePose
        Array.Copy(ReferenceMesh.sharedMesh.vertices, basePoseVertices, basePoseVertices.Length);
        Array.Copy(ReferenceMesh.sharedMesh.normals, basePoseNormals, basePoseNormals.Length);

        //bones could for example be pulled from a SkinnedMeshRenderer
        for (var i = 0; i < ReferenceMesh.bones.Length; i++)
        {
            var localPosition = ReferenceMesh.sharedMesh.bindposes[i].inverse.GetColumn(3);
            var localRotation = ReferenceMesh.sharedMesh.bindposes[i].inverse.rotation;

            if (i == 0)
            {
                ReferenceMesh.rootBone.name = "root";
                nameBoneDictionary.Add(ReferenceMesh.rootBone.name, new RootBone(localPosition, localRotation));
            }
            else
            {
                var parentIndex = -1;
                for (var j = 0; j < ReferenceMesh.bones.Length; j++)
                {
                    if (ReferenceMesh.bones[j] != ReferenceMesh.bones[i].parent) continue;

                    parentIndex = j;
                    break;
                }
                localRotation = (ReferenceMesh.sharedMesh.bindposes[parentIndex] * ReferenceMesh.sharedMesh.bindposes[i].inverse).rotation;
                localPosition = (ReferenceMesh.sharedMesh.bindposes[parentIndex] * ReferenceMesh.sharedMesh.bindposes[i].inverse).GetColumn(3);
                nameBoneDictionary.Add(ReferenceMesh.bones[i].name, new Bone(localPosition, localRotation, ReferenceMesh.bones[i].parent.name));
            }
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
        Skeleton = new Skeleton(nameBoneDictionary, vertexIdBoneWeightDictionary, basePoseVertices, basePoseNormals);
    }

    //Update skeleton using kinect sensor data
    private void UpdateBones()
    {
        //Get most actual kinect frame
        var frame = Reader.AcquireLatestFrame();
        if (frame != null)
        {
            //Update data
            if(Data == null)
                Data = new Body[Sensor.BodyFrameSource.BodyCount];
            frame.GetAndRefreshBodyData(Data);

            //Transfer kinect bone data to our 3D model
            foreach (var body in Data)
            {
                if (body == null)
                {
                    continue;
                }

                if (body.IsTracked)
                {
                    foreach (var bone in _knucklesToKinect)
                    {
                        var jointOrientation = Data[0].JointOrientations[bone.Value].Orientation;
                        Skeleton.BoneIdBoneDictionary[bone.Key].LocalRotation = new Quaternion(jointOrientation.X, jointOrientation.Y, jointOrientation.Z, jointOrientation.W);
                    }
                }
            }
            

            //Dispose current frame
            frame.Dispose();
            frame = null;
        }
    }

    //Apply the bone transformations to the mesh
    private void UpdateMesh()
    {
        Vector3[] vertices, normals;

        Skeleton.UpdateVertices(out vertices, out normals);
        AnimatedMesh.mesh.vertices = vertices;
        AnimatedMesh.mesh.normals = normals;

        AnimatedMesh.mesh.RecalculateBounds();
    }

    //Creates a mapping between our model and kinect skeleton
    private void MapKinectSkeletonToModel()
    {
        //Main body
        _knucklesToKinect.Add("root", JointType.SpineBase);
        _knucklesToKinect.Add("Spine", JointType.SpineMid);
        _knucklesToKinect.Add("Chest", JointType.SpineShoulder);

        return;

        //Head and neck
        _knucklesToKinect.Add("Neck", JointType.Neck);
        _knucklesToKinect.Add("Head", JointType.Head);

        //Left arm
        _knucklesToKinect.Add("Upper arm.L", JointType.ShoulderLeft);
        _knucklesToKinect.Add("Lower arm.L", JointType.ElbowLeft);
        _knucklesToKinect.Add("Hand.L", JointType.WristLeft);
        _knucklesToKinect.Add("Upper thumb.L", JointType.ThumbLeft);

        //Left leg
        _knucklesToKinect.Add("Upper leg.L", JointType.HipLeft);
        _knucklesToKinect.Add("Lower leg.L", JointType.KneeLeft);
        _knucklesToKinect.Add("Foot.L", JointType.AnkleLeft);
        _knucklesToKinect.Add("Toe.L", JointType.FootLeft);

        //Right leg
        _knucklesToKinect.Add("Upper leg.R", JointType.HipRight);
        _knucklesToKinect.Add("Lower leg.R", JointType.KneeRight);
        _knucklesToKinect.Add("Foot.R", JointType.AnkleRight);
        _knucklesToKinect.Add("Toe.R", JointType.FootRight);

        
        //Right arm
        _knucklesToKinect.Add("Upper arm.R", JointType.ShoulderRight);
        _knucklesToKinect.Add("Lower arm.R", JointType.ElbowRight);
        _knucklesToKinect.Add("Hand.R", JointType.WristRight);
        _knucklesToKinect.Add("Upper thumb.R", JointType.ThumbRight);
    }
}
