using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BezierSolution;

public class SpiderJumper : MonoBehaviour
{
    Animator manim;
    public BezierSpline creepInPath;
    public BezierSpline jumpOutPath;
    public BezierSpline jumpInPath;
    public BezierSpline fleePath;
    BezierFlight bzflight;
    BezierWalkerWithSpeed bzwalker;
    public float creepTime = 2f;
    Transform spiderParent;

    AudioSource maudio;
    public AudioClip hissSound;
    public AudioClip hitSound;
    public float hissDelay = 0.5f;
    public float creepSpeed = 0.1f;
    
    public float fleeSpeed = 0.2f;

    public enum SpiderPhases { 
        creepin,
        jumpin,
        facecrawl,
        jumpout,
        groundflee
    }
    SpiderPhases phase;
    Transform jumpto;
    public Transform spiderAdjustIn;
    public Transform spiderAdjustOut;
    Transform spiderAdjust;
    Vector3 jumpvec;
    public float upovershoot = 0.25f;
    public float gravityFactor = 10f;
    public float jumpSpeed = 2f;
    public float distTol = 0.1f;
    float facecrawlTimer;
    public float jumpInDelay = 0.3f;
    public float facecrawlTime = 3f;
    public float jumpOutDelay = 0.3f;
    Transform headTransform;
    Transform mtransform;
    Transform spiderHeadSpot;
    Transform tintfx;
    FearTaskManager taskManager;

    // Start is called before the first frame update
    void Awake()
    {
        manim = GetComponent<Animator>();
        maudio = GetComponent<AudioSource>();
        bzflight = GetComponent<BezierFlight>();
        bzwalker = GetComponent<BezierWalkerWithSpeed>();
        spiderParent = transform.parent;
    }
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
        tintfx = headTransform.Find("TintFX");
        CreepIn();
    }

    // Update is called once per frame
    void Update()
    {
        /*//for physics jump, aborted to use the bezier jump instead, which we can easily match player's head
        if (phase == SpiderPhases.jumpin) {
            mtransform.position += jumpvec * Time.deltaTime;
            jumpvec -= gravityFactor *Vector3.up * Time.deltaTime;
            if (Vector3.Distance(mtransform.position, jumpto.position) < distTol) {
                phase++;
            }
        }
        */
        switch (phase) { 
            case SpiderPhases.jumpin:
                //adjust last point
                spiderAdjust.position = headTransform.position;
                break;
            case SpiderPhases.facecrawl:
                facecrawlTimer -= Time.deltaTime;
                if (facecrawlTimer <= 0f) {
                    //next phase
                    NextPhase();
                }
                break;
        }
    }

    public void CreepIn() {
        bzwalker.enabled = false;
        bzflight.enabled = true;
        bzflight.spline = creepInPath;
        bzflight.flightSpeed = creepSpeed;
        //bezier curves apparently need at least a frame to correctly position themselves
        //bzflight.ResetPos();
        phase = SpiderPhases.creepin;
        //start the animator
        manim.SetBool("creepIn", true);
        manim.SetBool("jumpOut", false);
        //bzflight.onPathCompleted.AddListener((UnityEngine.Events.UnityAction)JumpIn); doesnt work! it is faster to set in the editor
    }
    IEnumerator DelayedHiss()
    {
        yield return new WaitForSeconds(hissDelay);
        maudio.PlayOneShot(hissSound);
        //bzflight.flightSpeed = jumpSpeed;
    }
    IEnumerator DelayedJumpOut() {
        yield return new WaitForSeconds(creepTime);
        JumpOut();
    }
    IEnumerator DelayedJumpout() {
        yield return new WaitForSeconds(jumpOutDelay);
        spiderAdjust = spiderAdjustOut;
        spiderAdjust.position = headTransform.position;

        //bzflight.enabled = true;
        //bzflight.spline = jumpOutPath;
        //bzflight.flightSpeed = jumpSpeed/2;

        //bzflight.ResetPos();
        //mtransform.position = spiderAdjust.position;
        //bzflight.enabled = false;
        bzflight.enabled = false;
        bzwalker.enabled = true;
        bzwalker.spline = jumpOutPath;
        bzwalker.NormalizedT = 0f;
        bzwalker.speed = jumpSpeed;

        if (tintfx)
        {
            tintfx.GetChild(0).gameObject.SetActive(false);
        }
        taskManager.SwitchBackNormalLight();
    }
    IEnumerator DelayedJumpIn() {
        yield return new WaitForSeconds(jumpInDelay);
        bzflight.enabled = true;
        bzflight.spline = jumpInPath;
        bzflight.flightSpeed = jumpSpeed;
        bzflight.ResetPos();
        maudio.PlayOneShot(hissSound);
        Debug.Log("jumpin");
    }
    public void JumpOut(){
        bzflight.spline = jumpOutPath;
        manim.SetBool("jumpOut", true);
    }
    void JumpIn() {
        bzflight.enabled = false;
        manim.SetBool("jumpIn", true);
        phase = SpiderPhases.jumpin;
        jumpto = headTransform;

        Vector3 jumpdirplane = Vector3.ProjectOnPlane(jumpto.position - mtransform.position, Vector3.up);
        float vertspeed;
        float tp = Mathf.Sqrt((8*upovershoot)/gravityFactor);
        float Vv = gravityFactor * tp * 0.5f;
        float dist = jumpdirplane.magnitude;
        float ts = (dist / jumpSpeed) - tp;
        float dh = dist - jumpSpeed * tp;
        float tsl = dh / jumpSpeed;        
        //Vvertical = Vvertical0+g*ts
        vertspeed = ((headTransform.position.y - mtransform.position.y) + gravityFactor * ts * ts * 0.5f) / ts;//Vv + gravityFactor * ts;

        float ts3 = (vertspeed - Vv) / gravityFactor;
        Debug.Log("vert: "+vertspeed+" tp: "+tp+" Vv:"+Vv+" ts:"+ts+" dist:"+ dist+" ts':"+tsl+" ts3: "+ts3);
        jumpvec = Vector3.up * vertspeed + jumpSpeed * jumpdirplane.normalized;
        StartCoroutine(DelayedHiss());
    }
    public void NextPhase() {
        phase++;
        switch (phase) {
            case SpiderPhases.jumpin:
                bzflight.enabled = false;
                spiderAdjust = spiderAdjustIn;
                manim.SetBool("jumpIn", true);
                manim.SetBool("creepIn", false);
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
                maudio.PlayOneShot(hitSound,1f);
                Debug.Log("facecrawl");
                if (tintfx)
                {
                    tintfx.GetChild(0).gameObject.SetActive(true);
                }
                break;
            case SpiderPhases.jumpout:
                mtransform.parent = spiderParent;
                manim.SetBool("jumpOut", true);
                manim.SetBool("jumpIn", false);
                StartCoroutine(DelayedJumpout());
                break;
            case SpiderPhases.groundflee:                
                bzwalker.spline = fleePath;
                bzwalker.NormalizedT = 0f;
                bzwalker.speed = jumpSpeed / 2;
                break;
        }
    }
}
