using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Holder : MonoBehaviour {
    private bool activated;

    public void activate()
    {
        activated = true;
    }

    public void deactivate()
    {
        activated = false;
    }

    public bool isActivated()
    {
        return activated;
    }

}
