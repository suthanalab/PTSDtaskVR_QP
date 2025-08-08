using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BezierSolution;
using UnityEngine.Events;

public class BezierFlight : MonoBehaviour
{
    public BezierSpline spline;
    public float flightSpeed = 0.1f;
    Transform targetTransform;
    Quaternion targetRotation;
    float progress;
    public float progressOffset;
    public bool lookForward = true;
    public float movementLerpModifier = 10.0f;
    public float rotationLerpModifier = 10.0f;
    public float stepAhead = 0.25f;
    public float swerveFactor = 1f;
    public bool negativeFlight;
    Vector3 swerveUp;
    Vector3 initialPos;
    public bool loop = true;
    public UnityEvent onPathCompleted = new UnityEvent();

    // Start is called before the first frame update
    void Awake()
    {
        targetTransform = transform;
        initialPos = targetTransform.position;
    }

    // Update is called once per frame
    void Update()
    {
        progress += flightSpeed * Time.deltaTime + progressOffset;
        if (progress >= 1.0f)
        {
            if(loop){
                progress = 0.0f - progressOffset;
            }
            else {
                if (onPathCompleted.GetPersistentEventCount() > 0)
                {
                    onPathCompleted.Invoke();
                    onPathCompleted.RemoveAllListeners();
                }
            }
        }

        targetTransform.position = Vector3.Lerp(targetTransform.position, spline.GetPoint(progress), movementLerpModifier * Time.deltaTime);
        
        if (lookForward)
        {
            Debug.DrawLine(targetTransform.position, spline.GetPoint(progress + stepAhead),Color.magenta);
            swerveUp = swerveFactor * (spline.GetPoint(progress + stepAhead) - targetTransform.position)+(1f-swerveFactor)*Vector3.up;
            Debug.DrawRay(targetTransform.position, swerveUp, Color.blue);
            if (negativeFlight) {
                targetRotation = Quaternion.LookRotation(spline.GetTangent(progress), -swerveUp);
            }
            else { targetRotation = Quaternion.LookRotation(spline.GetTangent(progress), swerveUp); }
            if (loop)
            {
                targetTransform.rotation = Quaternion.Lerp(targetTransform.rotation, targetRotation, rotationLerpModifier * Time.deltaTime);
            }
            else {
                if(progress+stepAhead < 1f)
                    targetTransform.rotation = Quaternion.Lerp(targetTransform.rotation, targetRotation, rotationLerpModifier * Time.deltaTime);
            }
        }
    }
    public void ResetPos() { 
        progress = 0f; 
        targetTransform.position = spline.GetPoint(0f);
        targetRotation = Quaternion.LookRotation(spline.GetTangent(0f), Vector3.up);
        Debug.DrawRay(targetTransform.position, 100f*Vector3.up, Color.white,10f);
        targetTransform.rotation = targetRotation;
    }
    public void RestartProgress() { progress = 0f; }
}
