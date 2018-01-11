using System.Collections;
using System.Collections.Generic;
using Windows.Kinect;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public KinectSensor Sensor;

	void Awake () {
		Sensor = KinectSensor.GetDefault();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
