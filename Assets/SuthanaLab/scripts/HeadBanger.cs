using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadBanger : MonoBehaviour
{
    AudioSource maudio;
    public AudioClip headbangSound;
    Transform headTransform;
    public Transform batAdjust;
    Transform tintfx;

    // Start is called before the first frame update
    void Start()
    {
        maudio = GetComponent<AudioSource>();
        headTransform = Camera.main.transform;
        tintfx = headTransform.Find("TintFX");
    }
    void Update()
    {
        //setting a pathpoint in the person's head so one of the bats bumps the camera
        batAdjust.position = headTransform.position;
    }
    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("just collided with: "+other.name);
        if (other.tag == "Player") {
            Debug.Log("just collided with player");
            maudio.PlayOneShot(headbangSound,1f);
            //trigger camera shake
            other.GetComponent<FearTaskManager>().CamShake();
            if (tintfx)
            {
                tintfx.GetChild(0).gameObject.SetActive(true);
                StartCoroutine(ClearTint());
            }
        }    
    }
    IEnumerator ClearTint() {
        yield return new WaitForSeconds(0.5f);
        tintfx.GetChild(0).gameObject.SetActive(false);
    }
}
