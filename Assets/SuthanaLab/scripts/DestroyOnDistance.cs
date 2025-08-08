using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnDistance : MonoBehaviour
{
    Transform headTransform;
    Transform mtransform;
    public float dist2destroy = 3f;

    // Start is called before the first frame update
    void Start()
    {
        headTransform = Camera.main.transform;
        mtransform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        float dist = Vector3.ProjectOnPlane(headTransform.position - mtransform.position, Vector3.up).magnitude;
        //Debug.Log("Instructions text distance: "+dist);
        if (dist > 7*dist2destroy)
            gameObject.SetActive(false);
    }
}
