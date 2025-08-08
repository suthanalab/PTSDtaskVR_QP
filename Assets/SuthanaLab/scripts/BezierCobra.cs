using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BezierSolution;
using UnityEngine.Events;

public class BezierCobra : MonoBehaviour
{
    public BezierSpline spline;
    public float speed = 0.1f;
    public Transform targetTransform;
    public Transform spineRoot;
    Transform[] childLinks;
    List<float> childOffsets;
    Quaternion targetRotation;
    public float progress;
    public float progressOffset;
    public bool lookForward = true;
    public float movementLerpModifier = 10.0f;
    public float rotationLerpModifier = 10.0f;
    public float stepAhead = 0.25f;
    public float swerveFactor = 1f;
    public bool negativeFlight;
    Vector3 swerveUp;
    float pathLen;
    public bool loop = true;
    public UnityEvent onPathCompleted = new UnityEvent();

    // Start is called before the first frame update
    void Start()
    {
        childLinks = spineRoot.GetComponentsInChildren<Transform>();
        pathLen = spline.length;
        Debug.Log("Found: " + childLinks.Length + " spine links, length of path:" + pathLen + " first link:"+childLinks[0].name);

        //initializing offsets
        childOffsets = new List<float>();
        for (int i=0;i<childLinks.Length;i++) {
            childOffsets.Add(Vector3.Distance(targetTransform.position,childLinks[i].position)); 
        }
        Debug.Log("First link offset: " + childOffsets[0] + " last link: "+childOffsets[childOffsets.Count-1]);        
    }
        

    // Update is called once per frame
    void Update()
    {
        
        progress += speed * Time.deltaTime + progressOffset;
        if (progress > 1.0f)
        {
            if(loop){
                progress = 0.0f - progressOffset;
            }
            else {
                if (onPathCompleted.GetPersistentEventCount() > 0)
                {
                    onPathCompleted.Invoke();
                }
            }
        }        

        targetTransform.position = Vector3.Lerp(targetTransform.position, spline.GetPoint(progress), movementLerpModifier * Time.deltaTime);
        targetTransform.forward = Vector3.Lerp(targetTransform.forward, -spline.GetTangent(progress), rotationLerpModifier * Time.deltaTime);

        //position children links
        pathLen = spline.length;
        for (int i = 0; i < childLinks.Length; i++)
        {
            childLinks[i].position = Vector3.Lerp(childLinks[i].position, spline.GetPoint(progress-childOffsets[i]/pathLen), movementLerpModifier * Time.deltaTime);
            
            targetRotation = Quaternion.LookRotation(-spline.GetTangent(progress - childOffsets[i]/pathLen));
            childLinks[i].rotation = Quaternion.Lerp(childLinks[i].rotation, targetRotation, rotationLerpModifier * Time.deltaTime);
        }
    }
    public void ResetPos() { 
        progress = 0f; 
        targetTransform.position = spline.GetPoint(0f);
        targetRotation = Quaternion.LookRotation(spline.GetTangent(0f), Vector3.up);
        Debug.DrawRay(targetTransform.position, 100f*Vector3.up, Color.white,10f);
        targetTransform.rotation = targetRotation;
    }
}
