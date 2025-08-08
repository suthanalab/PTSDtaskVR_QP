using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vespa : MonoBehaviour
{
    Animator manim;
    int phase;
    AudioSource maudio;
    public float buzzStartDelay = 0.3f;
    public float buzzStopDelay = 0.5f;
    public bool comeFromLeft = false;
    Transform headTransform;
    Transform vespaHeadSpot;
    Transform selfcontainer;

    BezierFlight bzflight;
    public BezierSolution.BezierSpline fromLeftPath;
    public BezierSolution.BezierSpline fromRightPath;
    public BezierSolution.BezierSpline fleepathLeft;
    public BezierSolution.BezierSpline fleepathRight;
    public Transform lastPathPointLeft;
    public Transform lastPathPointRight;
    
    public float faceTime = 3f;
    Transform adjustingPoint;
    Transform tintfx;
    FearTaskManager taskManager;

    // Start is called before the first frame update
    void Start()
    {
        manim = GetComponentInChildren<Animator>();
        phase = 0;
        maudio = GetComponent<AudioSource>();

        bzflight = GetComponent<BezierFlight>();
        headTransform = Camera.main.transform;
        vespaHeadSpot = headTransform.Find("vespaSpot");
        Transform secondSpot = headTransform.Find("vespaSpot2");
        Transform thirdSpot = headTransform.Find("vespaSpot3");
        Transform forthSpot = headTransform.Find("vespaSpot4");
                
        if (secondSpot && thirdSpot && forthSpot) {
            Debug.Log("Found all vespa spots!");
            int arandn = Random.Range(1,101);
            if(arandn < 25)
                vespaHeadSpot = secondSpot;
            else if (arandn >= 25 && arandn < 50)
                vespaHeadSpot = thirdSpot;
            else if (arandn >= 50 && arandn < 75)
                vespaHeadSpot = forthSpot;
        }


        selfcontainer = transform.parent;
        //decide which side to come from
        if (comeFromLeft) {
            bzflight.spline = fromLeftPath;
            
            //adjust 3rd point
            adjustingPoint = lastPathPointLeft;
        }
        else {
            bzflight.spline = fromRightPath;
            
            //adjust 3rd point
            adjustingPoint = lastPathPointRight;
        }
        taskManager = GameObject.Find("TaskManager").GetComponent<FearTaskManager>();
        tintfx = headTransform.Find("TintFX");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L)) {
            phase++;
            if (phase > 2)
                phase = 0;
                
            manim.SetInteger("phase",phase);
            if (phase == 1)
                StartCoroutine(StopBuzz());
            if (phase == 2)
                StartCoroutine(StartBuzz());
        }
        adjustingPoint.position = headTransform.position;
    }
    IEnumerator StopBuzz() {
        yield return new WaitForSeconds(buzzStopDelay);
        maudio.Stop();
    }
    IEnumerator StartBuzz() {
        yield return new WaitForSeconds(buzzStartDelay);
        maudio.Play();
    }
    IEnumerator GoAway()
    {
        yield return new WaitForSeconds(faceTime);
        //unparent from head spot
        transform.parent = selfcontainer;
        if (comeFromLeft) {
            bzflight.spline = fleepathLeft;
        }
        else {
            bzflight.spline = fleepathRight;
        }
        //adjust initial point
        bzflight.spline.transform.GetChild(0).position = headTransform.position;
        bzflight.RestartProgress();
        bzflight.enabled = true;
        //bzflight.flightSpeed = 0.5f * bzflight.flightSpeed;
        phase = 2;
        manim.SetInteger("phase", phase);
        //resume buzz
        maudio.Play();
        taskManager.SwitchBackNormalLight();
    }
    public void LandOnFace() {
        if (phase == 0)
        {
            phase = 1;
            manim.SetInteger("phase", phase);
            //parent it to the head
            transform.parent = vespaHeadSpot;
            bzflight.enabled = false;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            StartCoroutine(StopBuzz());
            StartCoroutine(GoAway());
            taskManager.CamShake(0.2f);
            if (tintfx)
            {
                tintfx.GetChild(0).gameObject.SetActive(true);
                StartCoroutine(ClearTint());
            }
        }
        if (phase == 2) {
            selfcontainer.gameObject.SetActive(false);
        }
        //Debug.Log("phase:"+phase);
    }
    IEnumerator ClearTint()
    {
        yield return new WaitForSeconds(faceTime);
        tintfx.GetChild(0).gameObject.SetActive(false);
    }
}
