using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RateConfirmer : MonoBehaviour
{
    GameObject highlightFX;
    
    public SphereRater sprater;
    [Tooltip("0 for a no button\n1 for yes button\n2 for a confirm button")]
    public int yesnoorconfirm;
        
    // Start is called before the first frame update
    void Start()
    {
        highlightFX = transform.GetChild(0).gameObject;
        highlightFX.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "GameController") {
            //Debug.Log("Rate confirmer triggered by: " + other.name);            
            highlightFX.SetActive(true);

            switch (yesnoorconfirm)
        {
                case 0:
                    sprater.ableno = true;
                    break;
                case 1:
                    sprater.ableyes = true;
                    break;
                case 2:
                    sprater.ableconfirm = true;
                    break;            
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "GameController")
        {
            highlightFX.SetActive(false);
            switch (yesnoorconfirm)
            {
                case 0:
                    sprater.ableno = false;
                    break;
                case 1:
                    sprater.ableyes = false;
                    break;
                case 2:
                    sprater.ableconfirm = false;
                    break;
            }
        }
    }
}
