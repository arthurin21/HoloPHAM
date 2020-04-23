using UnityEngine;
using System.Collections;

public class PlaceArm : MonoBehaviour {

    [Tooltip("This makes the arm track the user at a constant offset")]
    public bool trackShoulder = true;
    private bool rotateShoulder = false;
    [Tooltip("This is the offset from the user's position to draw the object")]
    public Vector3 headOffset = Vector3.zero;

    // Use this for initialization
    void Start () {
        Debug.Log("PlaceArm Game Object: " + gameObject.name);
	}//function Start


    public void GazePlace()
    {
        // Do a raycast into the world based on the user's
        // head position and orientation.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;


        //Check for Cursor hitting/on Objects
        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo))
        {
            // If the raycast hit a hologram...

            // Move the cursor to the point where the raycast hit.
            this.transform.position = hitInfo.point;

            // Rotate the cursor to hug the surface of the hologram.
            this.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            this.transform.Rotate(Vector3.right, -90f);
            this.transform.Rotate(Vector3.forward, 180f);
            this.transform.localRotation.Set(0f, this.transform.localRotation.y, 0f, 0f);
            this.transform.Translate(Vector3.Scale(hitInfo.normal.normalized, new Vector3(-0.1f, -0.1f, -0.1f)));

        }//check for raycast hit success
    }//function - GazePlace

	
	// Update is called once per frame
	void Update () {
        //trackShoulder = true;
        if (trackShoulder)
        {
            //headOffset.x = 0.3f;
            //headOffset.y = 0.05f;
            //headOffset.z = 0.35f;
            Transform headTransform = Camera.main.transform;
            this.transform.position = headTransform.position + headOffset;
                //headTransform.forward * headOffset.z +
                //headTransform.right * headOffset.x +
                //headTransform.up * headOffset.y;

        }

        //if (rotateShoulder)
        //{
        //    Transform headTransform = Camera.main.transform;
        //    this.transform.LookAt(headTransform);
        //    this.transform.Rotate(0, 180, 0);
        //    rotateShoulder = false;
        //}
    }
}
