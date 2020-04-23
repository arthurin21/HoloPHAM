using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour {
    Camera maincam;
	// Use this for initialization
	void Start () {
        maincam = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        if (Input.GetKey("space")){
            enableSecond();
        } else
            ShowFirstPersonView();

    }
    public void enableSecond(){
        maincam.enabled = false;
        this.GetComponent<Camera>().enabled = true;
    }

    public void ShowFirstPersonView()
    {
        maincam.enabled = true;
        this.GetComponent<Camera>().enabled = false;

    }
}