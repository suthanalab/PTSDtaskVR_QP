using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

using System.Runtime.InteropServices;


public class EyetrackHook : MonoBehaviour
{
    OVREyeGaze eyeGaze;
    Ray gazeray;
    RaycastHit hit;
    public Image leftPupil;
    public Image rightPupil;
    public Text lp;
    public Text rp;
    public Text gazeTarget;
    Transform virtualMoveTransform;

    string seenobj;
    string mname;
    int layerMask;

    int inputdd = 0;
    bool isdone;
    float ltriggervalue;
    float rtriggervalue;

    //List<UnityEngine.XR.InputDevice> inputDevices;
    bool eye_callback_registered;
    float pupilLeft;
    float pupilRight;

    public Text dd;    
    
    //testing eyetracking XR module
    Eyes userEyes;
    Vector3 fixationPoint;
    Vector3 eyeposL,eyeposR;

    private OVRPlugin.EyeGazesState _currentEyeGazesState;
    //private OVRPermissionsRequester.Permission EyeTrackingPermission = OVRPermissionsRequester.Permission.EyeTracking;
    public Transform leftEyeObj;
    public Transform rightEyeObj;
        
    public bool debugMode;
    Material seenmat;
    //OVRBoundary questbounds;
    string alignstr;
    public Transform worldTransform;

    void Awake()
    {
        virtualMoveTransform = Camera.main.transform.parent.parent;
    }

    // Start is called before the first frame update
    void Start()
    {
        userEyes = new Eyes();
        gazeray = new Ray(Vector3.zero, Vector3.zero);
        // Bit shift the index of the layer (10) to get a bit mask, we want to collide against everything except layer 10. 
        // The ~ operator does this, it inverts a bitmask. layer 10 is the realParticipant layer
        layerMask = 1 << 10;
        layerMask = ~layerMask;
        mname = gameObject.name;

        inputdd = 404;
        /*
        inputDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(inputDevices);
        string zz="";
        foreach (var device in inputDevices)
        {
            zz += string.Format("Device name '{0}' has role '{1}'", device.name, device.characteristics.ToString());
        }

        dd.text = zz;
        */

        /*
        if (!OVRPermissionsRequester.IsPermissionGranted(EyeTrackingPermission))
        {
            OVRPermissionsRequester.PermissionGranted -= _onPermissionGranted;
            OVRPermissionsRequester.PermissionGranted += _onPermissionGranted;
            zz += "|no eyetrack permission";
        }
        */
                
        if (!OVRPlugin.StartEyeTracking())
        {
            Debug.LogWarning($"[{gameObject.name}] Failed to start eye tracking.");
            dd.text = "|failed to start eyetrack";
        }
        //SRanipal_Eye_API.LaunchEyeCalibration(System.IntPtr.Zero);
    }
    void AlignRealWorld() {
        if (OVRManager.boundary.GetConfigured())
        {
            Vector3[] guardianPoints = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
            
            alignstr = "boundary seems configured";
            alignstr += "\nfound x points: " + guardianPoints.Length;
            alignstr += "vector 2 is: " + guardianPoints[2].ToString();
            
            Vector3 centerPoint = guardianPoints[1] + 0.5f * (guardianPoints[3] - guardianPoints[2]) + 0.5f * (guardianPoints[2] - guardianPoints[1]);
            Vector3 forthWorld = Vector3.forward;
            float smallestSide = 0f;

            for (int i = 0; i < guardianPoints.Length; i++)
            {
                int j = i + 1;
                if (j >= guardianPoints.Length)
                    j = 0;
                float side = (guardianPoints[j] - guardianPoints[i]).magnitude;
                if (smallestSide == 0f || side < smallestSide)
                {
                    smallestSide = side;
                    forthWorld = guardianPoints[i] + 0.5f * (guardianPoints[j] - guardianPoints[i]) - centerPoint;
                }
            }
            worldTransform.position = centerPoint;
            worldTransform.forward = forthWorld;
            //transform.Rotate(0f, 180f, 0f);
            alignstr += "\nAligned worldAnchor";
        }
        else
        {
            alignstr = "boundary not configured";
            Debug.Log("<color=red>AutoAlign did not perform, boundary not available.</color>");
        }
        dd.text = alignstr;
    }
    void ShowGuardianPoints(OVRBoundary.BoundaryType zz)
    {
        if (OVRManager.boundary.GetConfigured())
        {
            Vector3[] guardianPoints = OVRManager.boundary.GetGeometry(zz);

            
            alignstr = "boundary seems configured";
            alignstr += "\nfound " + guardianPoints.Length+" points: ";
            alignstr += "\ndims: " + OVRManager.boundary.GetDimensions(zz) + "!";

            for (int i = 0; i < guardianPoints.Length; i++)
            {
                GameObject newdot = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Sphere);
                newdot.transform.position = guardianPoints[i];
                newdot.transform.localScale = 0.25f * Vector3.one;
                Destroy(newdot, 5.0f);
            }
            alignstr += "\nadded spheres";
            //StartCoroutine(HideGuardian());
        }
        else
        {
            alignstr = "boundary not configured";
            Debug.Log("<color=red>AutoAlign did not perform, boundary not set up.</color>");
        }
        dd.text = alignstr;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R)) {
            //OVRManager.display.RecenterPose();
            //questbounds = new OVRBoundary();
            //AlignRealWorld();
            ShowGuardianPoints(OVRBoundary.BoundaryType.PlayArea);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            //OVRManager.display.RecenterPose();
            //questbounds = new OVRBoundary();
            //AlignRealWorld();
            ShowGuardianPoints(OVRBoundary.BoundaryType.OuterBoundary);
        }

        if (OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref _currentEyeGazesState))
        {
            OVRPlugin.EyeGazeState eyeGazeL = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Left];
            OVRPlugin.EyeGazeState eyeGazeR = _currentEyeGazesState.EyeGazes[(int)OVRPlugin.Eye.Right];

            if (eyeGazeR.IsValid && eyeGazeL.IsValid)
            {
                if (eyeGazeL.Confidence >= 0.5f && eyeGazeR.Confidence >= 0.5f)
                {
                    OVRPose poseL = eyeGazeL.Pose.ToOVRPose();
                    OVRPose poseR = eyeGazeR.Pose.ToOVRPose();
                    eyeposL = virtualMoveTransform.TransformPoint(poseL.position);
                    eyeposR = virtualMoveTransform.TransformPoint(poseR.position);
                    
                    rightEyeObj.position = eyeposR;
                    rightEyeObj.rotation = poseR.orientation;
                    leftEyeObj.position = eyeposL;
                    leftEyeObj.rotation = poseL.orientation;
                    
                    rightEyeObj.forward = virtualMoveTransform.TransformDirection(rightEyeObj.forward);
                    leftEyeObj.forward = virtualMoveTransform.TransformDirection(leftEyeObj.forward);
                    
                }
            }
        }
        
        /*
            userEyes.TryGetFixationPoint(out fixationPoint);
            userEyes.TryGetLeftEyePosition(out eyeposL);
            userEyes.TryGetRightEyePosition(out eyeposR);
        */
        

        //gazeray.origin = virtualMoveTransform.TransformPoint(eyeTrackingData.GazeRay.Origin);        
        //gazeray.direction = virtualMoveTransform.TransformDirection(eyeTrackingData.GazeRay.Direction);
        gazeray.origin = eyeposL+0.5f*(eyeposR-eyeposL);
        gazeray.direction = 0.5f *(leftEyeObj.forward + rightEyeObj.forward);
        

        //pupilLeft = eyeTrackingData.Left.PupilDiameter;
        //pupilRight = eyeTrackingData.Right.PupilDiameter;
        userEyes.TryGetRightEyeOpenAmount(out pupilRight);
        userEyes.TryGetLeftEyeOpenAmount(out pupilLeft);

        
        Logger.gazeDir = gazeray.direction;
        Logger.leftPupilSize = pupilLeft;
        Logger.rightPupilSize = pupilRight;
        
        leftPupil.fillAmount = pupilLeft / 10f;
        rightPupil.fillAmount = pupilRight / 10f;
        lp.text = pupilLeft.ToString("0.00");
        rp.text = pupilRight.ToString("0.00");

        if (Physics.Raycast(gazeray, out hit, 100f, layerMask))
        {
            seenobj = hit.transform.name;
            if (hit.transform.name == mname)
                seenobj += " seeing yourself, adjust layer";

            Logger.gazeTarget = hit.transform.name;
            if (debugMode)
            {
                Material newmat = hit.transform.GetComponent<Renderer>().material;
                if (newmat != seenmat)
                {
                    if (seenmat)
                        seenmat.color = Color.black;
                    seenmat = newmat;
                    seenmat.color = Color.red;
                }
            }
        }
        else
        {            
            Logger.gazeTarget = "nothing";
            seenobj = "nothing";
            if (seenmat && debugMode) {
                seenmat.color = Color.black;
                seenmat = null;
            }
        }

        
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primaryButton, out isdone);
        if (isdone)
            inputdd = 21;
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.secondaryButton, out isdone);
        if (isdone)
            inputdd = 23;
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.gripButton, out isdone);
        if (isdone)
            inputdd = 25;
        
        
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primaryButton, out isdone);
        if (isdone)
            inputdd = 20;
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.secondaryButton, out isdone);
        if (isdone)
            inputdd = 22;
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.gripButton, out isdone);
        if (isdone)
            inputdd = 24;

        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.trigger, out ltriggervalue);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out rtriggervalue);
        
        gazeTarget.text = inputdd + "|"+ alignstr+"|" + (ltriggervalue + rtriggervalue).ToString("0.0") + "|" + seenobj;
    }
}
