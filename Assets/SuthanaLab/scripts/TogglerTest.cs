using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;

public class TogglerTest : MonoBehaviour
{

    public List<GameObject> gotogglers;
    int curr;
    bool down;
    bool trigger;
    public Text ddt;
    
    // Update is called once per frame
    void Update()
    {

        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.secondaryButton, out down);
        if(trigger != down){
            trigger = down;

            if (trigger)
            {
                for (int i = 0; i < gotogglers.Count; i++) { gotogglers[i].SetActive(false); }
                curr++;
                if (curr >= gotogglers.Count)
                    curr = 0;
                gotogglers[curr].SetActive(true);
                ddt.text = gotogglers[curr].name;
            }
        }
    }
}
