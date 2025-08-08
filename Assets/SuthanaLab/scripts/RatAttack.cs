using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BezierSolution;

public class RatAttack : MonoBehaviour
{
    Animator manim;
    public BezierSpline creepInPath;
    public BezierSpline jumpInPath;
    public BezierSpline jumpOutPath;
    public BezierSpline boingPath;
    public BezierSpline fleePath;
    BezierSpline spline;
    int stage;
    float side;
    float ratSpeed;
    public float walkSpeed = 0.5f;
    public float jumpInSpeed = 1.0f;
    public float jumpOutSpeed = 1.0f;
    public float fleeSpeed = 1.0f;
    float progress;
    Quaternion targetRotation;
    Transform targetTransform;
    public float movementLerpModifier = 10.0f;
    public float rotationLerpModifier = 10.0f;
    public float stepAhead = 0.25f;
    public float bendingGain = 0.25f;
    bool lookForward = true;
    public float biteHoldTime = 3.0f;
    Transform headTransform;
    Transform ratHeadSpot;
    Transform originalParent;
    public Transform landSpot;
    public Transform jumpSpot;
    AudioSource maudio;
    public AudioClip attackSound;
    public AudioClip releaseSound;
    public AudioClip hitSound;
    Transform tintfx;
    FearTaskManager taskManager;
    bool freeze;

    // Start is called before the first frame update
    void Start()
    {
        manim = GetComponent<Animator>();
        maudio = GetComponent<AudioSource>();
        targetTransform = transform;
        headTransform = Camera.main.transform;
        ratHeadSpot = headTransform.Find("ratSpot");
        originalParent = targetTransform.parent;
        taskManager = GameObject.Find("TaskManager").GetComponent<FearTaskManager>();
        RestartStim();
        tintfx = headTransform.Find("TintFX");
        //making sure we bend
        manim.SetLayerWeight(1, 0.7f);
    }

    // Update is called once per frame
    void Update()
    {
        if (stage != 3 && !freeze)
        {
            //targetTransform.position = Vector3.Lerp(targetTransform.position, spline.GetPoint(progress), movementLerpModifier * Time.deltaTime);
            targetTransform.position = spline.MoveAlongSpline(ref progress, ratSpeed * Time.deltaTime);
            targetRotation = Quaternion.LookRotation((lookForward ? 1 : -1) * spline.GetTangent(progress));
            targetTransform.rotation = Quaternion.Lerp(targetTransform.rotation, targetRotation, rotationLerpModifier * Time.deltaTime);

            Debug.DrawLine(targetTransform.position, spline.GetPoint(progress + stepAhead), Color.magenta);
            side = bendingGain * Vector3.Dot(spline.GetPoint(progress + stepAhead) - targetTransform.position, targetTransform.right);
            //Debug.Log("<color=yellow>anim bending:" + side + "</color>");
            manim.SetFloat("dir", side);
            //Debug.DrawRay(targetTransform.position, swerveUp, Color.blue);
            //progress += ratSpeed * Time.deltaTime;

            if (progress >= 1f)
            {
                stage++;
                switch (stage)
                {
                    case 1://start attack
                        manim.SetBool("attack", true);
                        manim.SetLayerWeight(1, 0f);
                        //standard values for the flee portion
                        bendingGain = 15f;
                        //to avoid movement
                        freeze = true;
                        //StartCoroutine(JumpInCoro());
                        progress = 0f;
                        break;
                    case 2:
                        //biting
                        //parent it to the head
                        targetTransform.parent = ratHeadSpot;
                        targetTransform.localPosition = Vector3.zero;
                        targetTransform.localRotation = Quaternion.identity;
                        progress = 0f;
                        freeze = true;
                        //targetTransform.forward = -ratHeadSpot.forward;
                        //release ragdoll
                        StartCoroutine(ReleaseBiteGrip());
                        taskManager.CamShake(0.25f);
                        maudio.PlayOneShot(hitSound);
                        if (tintfx)
                        {
                            tintfx.GetChild(0).gameObject.SetActive(true);
                            StartCoroutine(ClearTint());
                        }
                        break;
                    case 5:
                        spline = boingPath;
                        ratSpeed = ratSpeed / 5;
                        progress = 0f;
                        manim.SetBool("grounded", true);
                        break;
                    case 6:
                        //flee
                        spline = fleePath;
                        ratSpeed = fleeSpeed;
                        progress = 0f;
                        lookForward = true;
                        break;
                    default:
                        //RestartStim();
                        //Debug.Log("ended rat stim");
                        break;
                }
            }
        }
        if(stage >=1 && stage <= 3){
            jumpSpot.position = ratHeadSpot.position; landSpot.position = ratHeadSpot.position;
        }

        /*
        if (Input.GetKeyDown(KeyCode.Space)){
            stage++;
            if (stage == 1)
            {
                manim.SetBool("attack", true);
                manim.SetLayerWeight(1, 0f);
            }
            else if (stage == 2) {
                manim.SetBool("attack", false);
                stage = 0;
                StartCoroutine(RecoverAnimationBending());
            }
        }
        */
        if (Input.GetKeyDown(KeyCode.R))
            RestartStim();
    }
    IEnumerator RecoverAnimationBending() {
        yield return new WaitForSeconds(1f);
        manim.SetLayerWeight(1, 0.7f);
    }
    IEnumerator JumpInCoro() {
        yield return new WaitForSeconds(0.5f);
        JumpIn(); 
    }
    public void JumpIn(){
        spline = jumpInPath;
        ratSpeed = jumpInSpeed;
        stage = 1;
        freeze = false;
        maudio.Stop();
        maudio.PlayOneShot(attackSound);
        Debug.Log("<color=yellow>jumpin was called</color>");
    }
    
    IEnumerator ReleaseBiteGrip() {
        yield return new WaitForSeconds(biteHoldTime);
        //reparent it
        targetTransform.parent = originalParent;
        //finished biting, start falling
        //stop ragdoll sim
        spline = jumpOutPath;
        ratSpeed = jumpOutSpeed;
        lookForward = false;
        progress = 0f;
        
        stage = 4;
        freeze = false;
        manim.SetBool("attack", false);
        StartCoroutine(RecoverAnimationBending());
        maudio.clip = releaseSound;
        maudio.Play();
    }
    void RestartStim() {
        stage = 0;
        progress = 0.0f;
        lookForward = true;
        spline = creepInPath;
        ratSpeed = walkSpeed;
        manim.SetBool("grounded", false);
    }
    IEnumerator ClearTint()
    {
        yield return new WaitForSeconds(biteHoldTime);
        tintfx.GetChild(0).gameObject.SetActive(false);
    }
}
