using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CobraAttack : MonoBehaviour
{
    enum states { 
        creepin,
        stare,
        //prepare2atk,
        atk,
        holding,
        fallback
    }
    states state;

    public float creepinSpeed = 0.1f;
    public float creepprogress = 0.95f;    
    public float stareDistFactor = 0.85f;
    public float stareLookFactor = 0.5f;
    public float creeplookspeed = 0.5f;
    float aimlookspeed = 1f;
    public float creepTime = 3f;

    public float holdTime = 3f;
    public float maxRange = 3f;

    public float boteSpeed = 5f;
    public float creepoutSpeed = 0.2f;
    public float telegraphAtkPerc = 0.25f;
    BezierCobra bezierCobra;
    public Transform headTransform;
    public Transform endppath;
    public Transform prepareppath;
    Transform cobraHead;
    Transform mtransform;
    BezierSolution.BezierPoint endPathTangent;
    Vector3 supportPoint;
    Vector3 BiteDir;
    Vector3 initialPreparePos;
    Animator manim;
    public AudioSource maudio;
    public AudioClip attackSound;
    public AudioClip hitSound;
    bool telegraphAtk;

    Transform BiteSpot;
    Transform tintfx;
    FearTaskManager taskManager;
        
    // Start is called before the first frame update
    void Start()
    {
        bezierCobra = GetComponent<BezierCobra>();
        endPathTangent = endppath.GetComponent<BezierSolution.BezierPoint>();
        cobraHead = bezierCobra.targetTransform;
        mtransform = transform;
        manim = GetComponent<Animator>();
        initialPreparePos = endppath.localPosition;
        if (!headTransform)
            headTransform = Camera.main.transform;

        BiteSpot = headTransform.Find("cobraSpot");
        maudio.playOnAwake = false;
        taskManager = GameObject.Find("TaskManager").GetComponent<FearTaskManager>();        

        CreepIn();
        
        tintfx = headTransform.Find("TintFX");
    }

    // Update is called once per frame
    void Update()
    {
        switch (state) {
            case states.creepin:
                if (bezierCobra.progress < creepprogress)
                {
                    bezierCobra.progress += creepinSpeed * Time.deltaTime;
                    //endPathTangent.followingControlPointLocalPosition = Vector3.Lerp(endPathTangent.followingControlPointLocalPosition, (Vector3.ProjectOnPlane(headTransform.position - endppath.parent.position, Vector3.up)).normalized,  stareFactor * Time.deltaTime);
                }
                else { state = states.stare; }
                break;
            case states.stare:
                if(aimlookspeed < creeplookspeed)
                    aimlookspeed += 10f * Time.deltaTime;
                if(telegraphAtk)
                    stareDistFactor -= 0.3f * Time.deltaTime;

                //making cobra stare
                endppath.position = Vector3.Lerp(endppath.position, prepareppath.position + stareDistFactor * (headTransform.position - prepareppath.position), aimlookspeed * Time.deltaTime);

                endPathTangent.followingControlPointLocalPosition = Vector3.Lerp(endPathTangent.followingControlPointLocalPosition, stareLookFactor * (Vector3.ProjectOnPlane(headTransform.position - endppath.position, Vector3.up)).normalized, 4 * aimlookspeed * Time.deltaTime);
                break;
            case states.atk:
                //remove prepare point
                supportPoint = prepareppath.position;
                BiteDir = headTransform.forward;
                //Destroy(prepareppath.gameObject);
                prepareppath.gameObject.SetActive(false);
                StartCoroutine(Prepare2Release());
                state = states.holding;
                if (tintfx)
                {
                    tintfx.GetChild(0).gameObject.SetActive(true);
                    StartCoroutine(ClearTint());
                }
                break;
            case states.holding:
                if (bezierCobra.progress < 1f - boteSpeed * Time.deltaTime)
                {
                    bezierCobra.progress += boteSpeed * Time.deltaTime;
                    if (bezierCobra.progress >= 1f - boteSpeed * Time.deltaTime)
                    {
                        taskManager.CamShake(0.125f);
                        maudio.PlayOneShot(hitSound);
                    }
                }
                else
                {
                    bezierCobra.progress = 1f;
                }
                endppath.position = BiteSpot.position;
                endPathTangent.followingControlPointLocalPosition = -stareLookFactor*BiteSpot.forward;
                
                //check if stretched too thin
                if (Vector3.Distance(endppath.position, supportPoint) > maxRange){// || Vector3.Angle(BiteDir, headTransform.forward) > 90f) {
                    Debug.Log("Cobra stretched too thin, releasing now");
                    //release
                    StopAllCoroutines();
                    Release();
                }
                break;
            case states.fallback:
                //make the cobra return slowly, not creepy at all
                if (bezierCobra.progress > 0.1f)
                {
                    bezierCobra.progress -= creepoutSpeed * Time.deltaTime;
                }
                else {
                    prepareppath.gameObject.SetActive(true);
                    //Destroy(bezierCobra.spline.gameObject);
                    //Destroy(gameObject);
                }
                break;
        }
    }

    IEnumerator Attack() {
        yield return new WaitForSeconds((1f-telegraphAtkPerc)*creepTime);
        maudio.Stop();
        telegraphAtk = true;
        yield return new WaitForSeconds(telegraphAtkPerc*creepTime);
        state = states.atk;
        manim.SetBool("openMouth", true);
        maudio.PlayOneShot(attackSound);
    }
    IEnumerator Prepare2Release() {
        yield return new WaitForSeconds(holdTime);
        Release();
    }
    void Release() {
        state = states.fallback;
        manim.SetBool("openMouth",false);
        maudio.Play();
        //prepareppath.gameObject.SetActive(true);
    }
    public void CreepIn() {
        //endppath.localPosition = initialPreparePos;
        //prepareppath.position = new Vector3(prepareppath.position.x, (prepareppath.position.y + headTransform.position.y)/2f, prepareppath.position.z);
        aimlookspeed = 1f;
        state = states.creepin;
        maudio.Play();
        StartCoroutine(Attack());
    }
    IEnumerator ClearTint()
    {
        yield return new WaitForSeconds(holdTime);
        tintfx.GetChild(0).gameObject.SetActive(false);
    }
}
