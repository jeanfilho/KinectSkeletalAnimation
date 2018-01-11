using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public KinectSensor Sensor;
    public Skeleton Skeleton;

	void Awake () {
		Sensor = KinectSensor.GetDefault();
	}
	
	void Update () {
		
	}
}
