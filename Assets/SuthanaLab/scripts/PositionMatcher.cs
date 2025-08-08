using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionMatcher : MonoBehaviour
{
    Transform headTransform;
    Transform mtransform;
    Vector3 mypos;

    // Start is called before the first frame update
    void Start()
    {
        headTransform = Camera.main.transform;
        mtransform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        mypos = Vector3.ProjectOnPlane(headTransform.position, Vector3.up);
        mtransform.position = mypos;
    }
}
