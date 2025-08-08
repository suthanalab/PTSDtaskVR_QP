using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatFlapControl : MonoBehaviour
{
    Animator anim;
    Transform mtransform;
    float levelAmount;
    public float maxAngle = 90f;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        mtransform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (maxAngle > 0f)
        {
            levelAmount = Vector3.Angle(mtransform.up, Vector3.up);
            anim.SetFloat("flapSpeed", levelAmount / maxAngle);
        }
    }
}
