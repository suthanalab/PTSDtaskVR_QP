using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideBlockages : MonoBehaviour
{
    public List<Transform> blockers2Hide;
    public List<Transform> blockers2Activate;
    //public StimPlacer stimPlacer;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player") {
            for (int i=0; i<blockers2Hide.Count; i++) {
                //Debug.Log("Hiding blocker:" +blockers2Hide[i].name);
                blockers2Hide[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < blockers2Activate.Count; i++)
            {
                //Debug.Log("Activating blocker:" + blockers2Activate[i].name);
                blockers2Activate[i].gameObject.SetActive(true);
            }
            gameObject.SetActive(false);
            //if(stimPlacer)
            //    stimPlacer.BuildFears();
        }
    }
}
