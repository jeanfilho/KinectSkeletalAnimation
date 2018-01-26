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
        Kinect();
        UpdateMesh();
    }

    void OnApplicationQuit()
    {
        Reader.Dispose();
        if (Sensor.IsOpen)
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

        var bindPoses = new Matrix4x4[ReferenceMesh.sharedMesh.bindposes.Length];
        Array.Copy(ReferenceMesh.sharedMesh.bindposes, bindPoses, bindPoses.Length);

        //bones could for example be pulled from a SkinnedMeshRenderer
        for (var i = 0; i < ReferenceMesh.bones.Length; i++)
        {
            var localPosition = bindPoses[i].inverse.GetColumn(3);
            var localRotation = bindPoses[i].inverse.rotation;

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
                localRotation = (bindPoses[parentIndex] * bindPoses[i].inverse).rotation;
                localPosition = (bindPoses[parentIndex] * bindPoses[i].inverse).GetColumn(3);


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

        //Deactivate class
        ReferenceMesh.gameObject.SetActive(false);
    }

    //Update skeleton using kinect sensor data
    private void Kinect()
    {
        //Get most actual kinect frame
        var frame = Reader.AcquireLatestFrame();
        if (frame != null)
        {
            //Update data
            if (Data == null)
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
                    UpdateSkeleton();
                }
            }
            //Dispose current frame
            frame.Dispose();
            frame = null;
        }

    }

    private void UpdateSkeleton()
    {
        foreach (var bone in _knucklesToKinect)
        {
            var jointOrientation = Data[0].JointOrientations[bone.Value].Orientation.ToQuaternion(false);
            var jointPosition = Data[0].Joints[bone.Value].Position.ToUnityVector3();

            //For root, no adjustment needed
            if (bone.Key == "root")
            {
                Skeleton.BoneIdBoneDictionary[bone.Key].LocalRotation = jointOrientation;
                continue;
            }

            //Calculate the local rotation from the kinect system -> https://msdn.microsoft.com/en-us/library/hh973073.aspx
            var parent = GameObject.Find(bone.Key).transform.parent;
            while (!_knucklesToKinect.ContainsKey(parent.name))
                parent = parent.transform.parent;

            var parentRotation = Data[0].JointOrientations[_knucklesToKinect[parent.name]].Orientation
                .ToQuaternion(false);
            var parentPosition = Data[0].Joints[_knucklesToKinect[parent.name]].Position.ToUnityVector3();

            //Head effect "cheat"
            if (bone.Key == "Head")
            {
                var direction = (jointPosition - parentPosition).normalized;
                var perpendicular = Vector3.Cross(direction, Vector3.forward);
                var binormal = Vector3.Cross(perpendicular, direction);

                Quaternion rotation;
                if (binormal.magnitude > 0.0001f)
                    rotation = Quaternion.Euler(-90, 180, 0) * Quaternion.LookRotation(direction, binormal);
                else
                    rotation = Quaternion.identity;
                Skeleton.BoneIdBoneDictionary[bone.Key].LocalRotation = rotation;
            }

            //Other adjustments
            else if (bone.Key == "Shoulder.R")
            {
                jointOrientation = Quaternion.Euler(0, 0, -30) * jointOrientation;
            }
            else if (bone.Key == "Shoulder.L")
            {
                jointOrientation = Quaternion.Euler(0, 0, 30) * jointOrientation;
            }
            else if (bone.Key == "Upper leg.R")
            {
                jointOrientation = Quaternion.Euler(jointOrientation.eulerAngles.z, jointOrientation.eulerAngles.y - 90, jointOrientation.eulerAngles.x);
            }
            else if (bone.Key == "Lower leg.R")
            {
                jointOrientation = Quaternion.Inverse(Quaternion.Euler(parentRotation.eulerAngles.z, parentRotation.eulerAngles.y - 90, parentRotation.eulerAngles.x)) * Quaternion.Euler(jointOrientation.eulerAngles.z + 20, jointOrientation.eulerAngles.y - 90, jointOrientation.eulerAngles.x);
            }
            else if (bone.Key == "Upper leg.L")
            {
                jointOrientation = Quaternion.Euler(-jointOrientation.eulerAngles.z, jointOrientation.eulerAngles.y + 90, -jointOrientation.eulerAngles.x);
            }
            else if (bone.Key == "Lower leg.L")
            {
                jointOrientation = Quaternion.Inverse(Quaternion.Euler(-parentRotation.eulerAngles.z, parentRotation.eulerAngles.y + 90, -parentRotation.eulerAngles.x)) * Quaternion.Euler(-jointOrientation.eulerAngles.z + 20, jointOrientation.eulerAngles.y + 90, -jointOrientation.eulerAngles.x);
            }
            var current = Skeleton.BoneIdBoneDictionary[bone.Key].LocalRotation;
            Skeleton.BoneIdBoneDictionary[bone.Key].LocalRotation = Quaternion.Lerp(current, (bone.Key.Contains("Lower leg") ? Quaternion.identity : Quaternion.Inverse(parentRotation)) * jointOrientation, 0.5f);
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


        ////Head and neck
        _knucklesToKinect.Add("Neck", JointType.Neck);
        _knucklesToKinect.Add("Head", JointType.Head);

        //Left arm
        _knucklesToKinect.Add("Shoulder.R", JointType.ShoulderLeft);
        _knucklesToKinect.Add("Upper arm.R", JointType.ElbowLeft);
        _knucklesToKinect.Add("Lower arm.R", JointType.WristLeft);
        _knucklesToKinect.Add("Hand.R", JointType.HandLeft);

        //Right arm
        _knucklesToKinect.Add("Shoulder.L", JointType.ShoulderRight);
        _knucklesToKinect.Add("Upper arm.L", JointType.ElbowRight);
        _knucklesToKinect.Add("Lower arm.L", JointType.WristRight);
        _knucklesToKinect.Add("Hand.L", JointType.HandRight);

        //Left leg
        _knucklesToKinect.Add("Upper leg.R", JointType.KneeLeft);
        _knucklesToKinect.Add("Lower leg.R", JointType.AnkleLeft);

        //Right leg
        _knucklesToKinect.Add("Upper leg.L", JointType.KneeRight);
        _knucklesToKinect.Add("Lower leg.L", JointType.AnkleRight);
    }

    void OnDrawGizmos()
    {
        if (Data == null)
            return;

        foreach (var bone in _knucklesToKinect)
        {
            var position = Data[0].Joints[bone.Value].Position.ToUnityVector3();
            var orientation = Data[0].JointOrientations[bone.Value].Orientation.ToQuaternion(false);

            Gizmos.DrawLine(position, position + orientation * Vector3.forward);
        }
    }
}
