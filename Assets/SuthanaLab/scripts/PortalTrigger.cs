using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortalTrigger : MonoBehaviour
{
    public bool enteringRelax;
    public Transform environments;
    AudioSource relaxaudio;
    Image transitionFX;
    Color transientcolor;
    float targetAlpha;
    public Transform beginSpot;
    public GameObject portalPrefab;
    public FearTaskManager feartaskManager;
    public float transitionTime = 2.0f;
    public int fadeOrder=2;
    float transitionTimer;
    Renderer mrender;
    Collider mcol;
    public float relaxTime=3f;
    public float growSpeed=2f;
    public float rotSpeed = 5f;
    float growth=0f;
    Vector3 portalpos;
    Vector3 portalscale;
    Transform mtransform;
    public Transform ratingsgrp;
    public Text ratingsUI;

    void Start()
    {
        relaxaudio = GameObject.Find("relaxingMusic").GetComponent<AudioSource>();
        transitionFX = Camera.main.transform.Find("FadeFX").GetChild(1).GetComponent<Image>();
        mrender = GetComponent<Renderer>();
        mrender.enabled = true;
        mcol = GetComponent<Collider>();
        mcol.enabled = true;
        //transientcolor = Color.black;
        //transitionFX.color = transientcolor;
        mtransform = transform;
        portalpos = mtransform.position;
        portalscale = new Vector3(0.9f, growth, 0.9f);
    }
    void Update() {
        /*
        if (Input.GetKeyDown(KeyCode.J)) {
            //test fade
            targetAlpha = 1f;
            transitionTimer = transitionTime;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            //test fade
            targetAlpha = 0f;
            transitionTimer = transitionTime;
        }
        */
        if (transitionTimer > 0f)
        {
            transitionTimer -= Time.deltaTime;
            if (targetAlpha > 0f)
            {
                transientcolor.a = Mathf.Pow((transitionTime - transitionTimer) / transitionTime, 1);
            }
            else {
                transientcolor.a = 1f-Mathf.Pow((transitionTime - transitionTimer) / transitionTime, fadeOrder);
            }
            transitionFX.color = transientcolor;
            if (transitionTimer <= 0f) {
                transientcolor.a = targetAlpha;
                transitionFX.color = transientcolor;
                Debug.Log("finished transition");
            }
        }

        mtransform.Rotate(0f,rotSpeed*Time.deltaTime,0f);
        if (growth < 1.02f)
        {
            growth += growSpeed * Time.deltaTime;
            portalpos.y = growth;
            portalscale.y = growth;
            mtransform.position = portalpos;
            mtransform.localScale = portalscale;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            Debug.Log(gameObject.name + "triggered by player");
            //transitionFX.CrossFadeAlpha(1f, transitionTime, false);
            targetAlpha = 1f;
            transitionTimer = transitionTime;
            mrender.enabled = false;
            mcol.enabled = false;
            StartCoroutine(iTransition()); 
        }
    }
    IEnumerator iTransition() {
        Debug.Log("started itransition");
        yield return new WaitForSeconds(transitionTime);
        StartTransition();
        //yield return new WaitForSeconds(1.0f);
        StartCoroutine(fTransition());
    }
    IEnumerator fTransition()
    {
        Debug.Log("started ftransition");
        targetAlpha = 0f;
        transitionTimer = transitionTime;
        yield return new WaitForSeconds(transitionTime);
        //transitionFX.CrossFadeAlpha(0.1f, transitionTime, false);
        if(!enteringRelax)
            Destroy(gameObject);
    }
    void StartTransition(){        
        if (enteringRelax) {
            environments.gameObject.SetActive(false);
            relaxaudio.Play();

            StartCoroutine(DelayedExtinctionPortal());
        }
        else {
            relaxaudio.Stop();
            environments.gameObject.SetActive(true);
            //start extinction
            feartaskManager.StartExtinction();
        }
        Debug.Log("<color=yellow>Player reached the portal!</color>");
    }
    IEnumerator DelayedExtinctionPortal() {
        yield return new WaitForSeconds(relaxTime);
        //spawn the other portal to go back to extinction
        GameObject relaxPortal = GameObject.Instantiate(portalPrefab);
        Vector3 portalpos = beginSpot.position;
        portalpos.y = 0f;
        relaxPortal.transform.position = portalpos;
        PortalTrigger portalTrigger = relaxPortal.GetComponent<PortalTrigger>();
        portalTrigger.enteringRelax = false;
        portalTrigger.environments = environments;
        portalTrigger.feartaskManager = GameObject.Find("TaskManager").GetComponent<FearTaskManager>();

        //reposition ratings grp with the ui and display
        ratingsUI.text = "Please walk through the new portal";
        Transform participantTransform = Camera.main.transform.parent.parent;
        ratingsgrp.position = participantTransform.position + 0.5f*Vector3.ProjectOnPlane((portalpos-participantTransform.position).normalized,Vector3.up);
        ratingsgrp.forward = Vector3.ProjectOnPlane((participantTransform.position - ratingsgrp.position).normalized ,Vector3.up);

        Destroy(gameObject);
    }
}
