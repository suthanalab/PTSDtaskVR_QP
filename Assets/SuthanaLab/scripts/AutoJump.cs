using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BezierSolution;

public class AutoJump : MonoBehaviour
{
    FearTaskManager taskManager;
    Transform headTransform;
    Transform mtransform;
    Transform spiderParent;
    Transform spiderHeadSpot;
    Animator manim;

    AudioSource maudio;
    public AudioClip hissSound;
    public AudioClip hitSound;
    public float hissDelay = 0.5f;
    public float creepSpeed = 0.1f;

    public BezierSpline jumpInPath;
    BezierFlight bzflight;

    float facecrawlTimer;
    public float jumpInDelay = 0.3f;
    public float jumpOutDelay = 0.3f;
    public float jumpSpeed = 2f;
    public float facecrawlTime = 3f;

    public Transform spiderAdjustIn;    
    Transform spiderAdjust;

    public float gravityFactor = 10f;
    public float bounceFactor = 0.5f;
    float jumpUpSpeed;
    public float jumpOutSpeed = 2.5f;
    Vector3 jumpOutVector;
    Vector3 fleeDir;
    public float lookFactor = 10f;
    int bounces;
    public int bounceTarget=5;

    public enum SpiderPhases
    {
        creepin,
        jumpin,
        facecrawl,
        jumpout,
        groundflee
    }
    SpiderPhases phase;

    void Awake()
    {
        manim = GetComponent<Animator>();
        maudio = GetComponent<AudioSource>();
        bzflight = GetComponent<BezierFlight>();
    }
    // Start is called before the first frame update
    void Start()
    {
        headTransform = Camera.main.transform;
        spiderHeadSpot = headTransform.Find("spiderSpot");
        Transform secondSpot = headTransform.Find("spiderSpot2");
        Transform thirdSpot = headTransform.Find("spiderSpot3");
        Transform forthSpot = headTransform.Find("spiderSpot4");
        if (secondSpot && thirdSpot && forthSpot)
        {
            Debug.Log("Found all spider spots!");
            int arandn = Random.Range(1, 101);
            if (arandn < 25)
                spiderHeadSpot = secondSpot;
            else if (arandn >= 25 && arandn < 50)
                spiderHeadSpot = thirdSpot;
            else if (arandn >= 50 && arandn < 75)
                spiderHeadSpot = forthSpot;
        }
        mtransform = transform;
        taskManager = GameObject.Find("TaskManager").GetComponent<FearTaskManager>();
        spiderParent = transform.parent;
        spiderAdjust = spiderAdjustIn;
        NextPhase();
    }

    // Update is called once per frame
    void Update()
    {
        switch (phase)
        {
            case SpiderPhases.jumpin:
                //adjust last point
                spiderAdjust.position = headTransform.position;
                break;
            case SpiderPhases.facecrawl:
                facecrawlTimer -= Time.deltaTime;
                if (facecrawlTimer <= 0f)
                {
                    //next phase
                    NextPhase();
                }
                break;
            case SpiderPhases.jumpout:
                //do the math
                jumpUpSpeed -= gravityFactor * Time.deltaTime;
                jumpOutVector.y = jumpUpSpeed;
                mtransform.position += jumpOutVector * Time.deltaTime;
                //align rotation
                mtransform.rotation = Quaternion.Lerp(mtransform.rotation, Quaternion.LookRotation(jumpOutVector), lookFactor*Time.deltaTime);

                //check for ground and bounce
                if (mtransform.position.y <= 0f)
                {
                    if (jumpUpSpeed < 0f && bounces < bounceTarget)
                    {
                        bounces++;
                        //Debug.Log("bounced "+bounces);
                        jumpUpSpeed = -bounceFactor * jumpUpSpeed;

                        if(jumpUpSpeed <= 0f || bounces >= bounceTarget) {
                            phase = SpiderPhases.groundflee;
                            jumpOutVector *= 1.5f;
                            jumpOutVector.y = 0f;
                            Vector3 snap2ground = mtransform.position;
                            snap2ground.y = 0f;
                            mtransform.position = snap2ground;

                            if (Random.Range(0, 10) <= 5)
                                fleeDir = Vector3.forward;
                            else
                                fleeDir = -Vector3.forward;
                        }
                    }                    
                }
                break;
            case SpiderPhases.groundflee:
                //math
                jumpOutVector = Vector3.Lerp(jumpOutVector, fleeDir, 0.5f*lookFactor*Time.deltaTime);
                mtransform.position += jumpOutVector * Time.deltaTime;
                //align rotation
                mtransform.rotation = Quaternion.Lerp(mtransform.rotation, Quaternion.LookRotation(jumpOutVector), lookFactor * Time.deltaTime);
                break;
        }
    }
    IEnumerator DelayedJumpout()
    {
        yield return new WaitForSeconds(jumpOutDelay);
        spiderAdjust.position = headTransform.position;

        //bzflight.enabled = true;
        //bzflight.spline = jumpOutPath;
        //bzflight.flightSpeed = jumpSpeed/2;

        //bzflight.ResetPos();
        //mtransform.position = spiderAdjust.position;
        //bzflight.enabled = false;
        bzflight.enabled = false;
        taskManager.SwitchBackNormalLight();
    }
    IEnumerator DelayedJumpIn()
    {
        yield return new WaitForSeconds(jumpInDelay);
        manim.SetBool("jumpIn", true);
        manim.SetBool("creepIn", false);
        bzflight.enabled = true;
        bzflight.spline = jumpInPath;
        bzflight.flightSpeed = jumpSpeed;
        bzflight.ResetPos();
        maudio.PlayOneShot(hissSound);
        Debug.Log("jumpin");
    }    
    public void NextPhase()
    {
        phase++;
        switch (phase)
        {
            case SpiderPhases.jumpin:
                manim.SetBool("creepIn", true);
                manim.SetBool("jumpOut", false);
                StartCoroutine(DelayedJumpIn());
                //StartCoroutine(DelayedHiss());
                Debug.Log("preparejump");
                break;
            case SpiderPhases.facecrawl:
                bzflight.enabled = false;
                facecrawlTimer = facecrawlTime;
                mtransform.up = headTransform.forward;
                mtransform.parent = spiderHeadSpot;
                mtransform.localPosition = Vector3.zero;
                mtransform.localRotation = Quaternion.identity;
                taskManager.CamShake(0.2f);
                maudio.PlayOneShot(hitSound, 1f);
                Debug.Log("facecrawl");                
                break;
            case SpiderPhases.jumpout:
                mtransform.parent = spiderParent;
                manim.SetBool("jumpOut", true);
                manim.SetBool("jumpIn", false);
                jumpUpSpeed = jumpOutSpeed;
                if (Random.Range(0, 10) <= 5)
                    jumpOutVector = Vector3.right;
                else
                    jumpOutVector = -Vector3.right;
                StartCoroutine(DelayedJumpout());
                break;
            case SpiderPhases.groundflee:
                
                break;
        }
    }
}
