using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PHAM_Cylinder : MonoBehaviour {

    public int triggerCount, score;
    public bool successfulActivation;
    private string lastTrigger;
    private GameObject LastHolderTouched;
    public Text scoreHUD;

    void OnTriggerEnter(Collider other) {
        //Upon hitting one of the PHAM ends, add to trigger count
        if (other.gameObject.name.Contains("Holder") && other.gameObject.GetComponent<Holder>().isActivated())
        {
            //Failsafe incase cylinder glitches into the trigger space of another holder
            if (!successfulActivation)
            {
                lastTrigger = other.gameObject.name;
                triggerCount++;
            }

            //If both ends are touched, turn the holder to green and activate another PHAM holder
            if (triggerCount == 2)
            {
                successfulActivation = true;
                score++;
            }
        }
        // Reset if object hits floor
        if (other.gameObject.name == "Floor")
        {
            PHAM_Manager.ColorHolder();
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Upon exiting one of the PHAM ends, lower to trigger count
        if (other.gameObject.name == lastTrigger && other.gameObject.GetComponent<Holder>().isActivated())
        {
            //Change the color back to normal
            if (triggerCount == 2)
            {
                successfulActivation = false;
            }

            triggerCount--;

            //Deactivate the holder if object has left the holder, and deactivate the failsafe boolean
            if (triggerCount == 0 && successfulActivation)
            {
                other.gameObject.GetComponent<Holder>().deactivate();
                successfulActivation = false;
            }
        }
    }
    public bool success()
    {
        return successfulActivation;
    }

}
