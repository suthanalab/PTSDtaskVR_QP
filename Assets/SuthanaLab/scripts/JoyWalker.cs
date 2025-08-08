using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]

public class JoyWalker : MonoBehaviour
{
    CharacterController myctrl;
    Transform myTransform;
    Transform camTransform;
    Vector3 movedir;
    public float forthSpeed = 10f;
    public float lateralSpeed = 10f;
    public float lookupGain = 0.1f;
    public float looksideGain = 0.1f;
    public bool frozen;
    float vert;
    float hori;
    float lateral;

    public float yawSpeed = 100f;

    public float grav = 10.0f;
    float xRot, yRot;
    Vector3 rotx, roty;
    public bool actualWalk;
    //public bool runningOnPC;
    public float snapTurnStep = 1f;
    public bool allowbackwardsMove;

    public float shakeDuration = 0.5f;
    public float shakePower = 0.05f;
    public float shakeFreq = 100.0f;
    float shakeAmp;
    float shakeStarted;
    public Transform shakeTransform;
    Vector3 shakepos = Vector3.zero;
    float shakeGain = 1.0f;
    public bool RightHanded;
    public bool pcmode;

    Vector2 ljoy;
    Vector2 rjoy;

    void Awake()
    {
        myctrl = GetComponent<CharacterController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        camTransform = Camera.main.transform;
        if (!myTransform)
            myTransform = transform;

        if (actualWalk)
            this.enabled = false;

        myctrl.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if(pcmode){
            Transform camparent = camTransform.parent;
            camTransform.localPosition = new Vector3(0f,1.6f,0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!frozen)
        {
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out ljoy);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primary2DAxis, out rjoy);

            if (RightHanded) {
                vert = Input.GetAxis("Vertical")+rjoy.y;
                hori = Input.GetAxis("Horizontal")+rjoy.x;
            }
            else {
                vert = Input.GetAxis("Vertical")+ljoy.y;
                hori = Input.GetAxis("Horizontal") + ljoy.x;
            }
            
            //preventing backwards movement
            if (!allowbackwardsMove)
                vert = Mathf.Clamp(vert, 0f,1f);

            if (pcmode)
            {
                movedir = camTransform.forward * forthSpeed * vert;
                movedir += camTransform.right * lateralSpeed * hori;
            }
            else {
                movedir = myTransform.forward * forthSpeed * vert;
                movedir += myTransform.right * lateralSpeed * hori;
            }
        }
        else {
            movedir *= 0f;
            vert = 0f;
        }
        /*
        if (RightHanded) {
            lateral = Input.GetAxis("HorizontalArrows") + rjoy.x;
        }
        else {
            lateral = Input.GetAxis("HorizontalArrows")+ljoy.x;
        }
        if (Mathf.Abs(lateral) > Mathf.Abs(vert))
        {
            movedir *= 0f;
            myTransform.Rotate(0f, lateral * yawSpeed * Time.deltaTime, 0f);
        }
        */
        movedir = -Vector3.up * grav + Vector3.ProjectOnPlane(movedir, Vector3.up);
        myctrl.Move(movedir * Time.deltaTime);

        /*
         if (snapTurnRight.GetStateDown(SteamVR_Input_Sources.RightHand) || Input.GetKeyDown(CustomInputs.snapTurnRight))
            {
                myTransform.Rotate(0f, snapTurnStep * yawSpeed, 0f);
            }
            if(snapTurnLeft.GetStateDown(SteamVR_Input_Sources.LeftHand) || Input.GetKeyDown(CustomInputs.snapTurnLeft))
            {
                myTransform.Rotate(0f, -snapTurnStep * yawSpeed, 0f);
            }
        */

        //mouse look
        xRot = Input.GetAxis("Mouse X") * looksideGain;
        yRot = Input.GetAxis("Mouse Y") * lookupGain;
        //Debug.Log ("rotx: "+xRot.ToString()+" roty: "+yRot.ToString());
        if (pcmode)
        {
            rotx = camTransform.forward * Mathf.Cos(xRot) + camTransform.right * Mathf.Sin(xRot);
            roty = camTransform.forward * Mathf.Cos(yRot) + camTransform.up * Mathf.Sin(yRot);
            camTransform.forward = Vector3.Normalize(rotx + roty);
        }
                
        if (shakeAmp > 0f)
        {
            shakeAmp -= Time.deltaTime / (shakeDuration * shakeGain);
            if(shakeAmp < 0f)
                shakeAmp = 0f;
            shakepos.y = shakeAmp * shakePower * Mathf.Sin(shakeFreq * (Time.time - shakeStarted));
            shakeTransform.localPosition = shakepos;
        }

        //Debug.Log("vertical input: "+vert);
        //some debug keys
        if (Input.GetKeyDown(KeyCode.Joystick1Button2) || Input.GetKeyDown(KeyCode.C)) { CamShake();}
        
    }

    public void CamShake(float ShakeGain = 1.0f) {
        //start the camera shake
        shakeGain = ShakeGain;
        shakeAmp = 1.0f;
        shakeStarted = Time.time;
    }
}
