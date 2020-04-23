using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GraspingLogic : MonoBehaviour {
    public bool Grasping = true;
    private GameObject palm = null;
    private GameObject cylinder = null;
    private vMPLMovementArbiter arbiter = null;
    private const float GRASP_DIST_THRESHOLD = 1f;
    private const float GRASP_ANGLE_THRESHOLD = 2f;

	// Use this for initialization
	void Start () {
        Grasping = false;
        palm = GameObject.Find("rPalm"); // or Endpoint
        arbiter = GameObject.Find("vMPLMovementArbiter").GetComponent<vMPLMovementArbiter>();
    }
	
	// Update is called once per frame
	void Update () {
        float [] angles = arbiter.GetRightFingerAngles();

		if ( Grasping )
        {
            GetComponent<Rigidbody>().mass = 0.01f;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

            gameObject.transform.position = palm.transform.position
                                            - 0.45f * palm.transform.up 
                                            - .3f * palm.transform.forward;
            gameObject.transform.up = palm.transform.right;

            if ( angles[1] < 60.0 || angles[5] < 60.0 || angles[9] < 60.0 || angles[13] < 60.0 || angles[18] < 60.0 ) {
                Grasping = false;
            }
        }
        else
        {
            float angle_diff = Mathf.Min( Vector3.Distance( palm.transform.right, -1.0f * gameObject.transform.up ),
                                          Vector3.Distance(palm.transform.right, gameObject.transform.up) );
            Vector3 distance = palm.transform.position - gameObject.transform.position;

//            Debug.Log(string.Format("Triggering...{0}, {1}", angle_diff, distance.magnitude));
            float norm_diff = distance.magnitude;
            //arbiter.GetMovementState() == vMPLMovementArbiter.MOVEMENT_STATE_CYLINDER_GRASP
                //&&
            
            
			if ( angles[1] > 60.0 && angles[5] > 60.0 && angles[9] > 60.0 && angles[13] > 60.0 && angles[18] > 60.0 && angle_diff <= GRASP_ANGLE_THRESHOLD && norm_diff <= GRASP_DIST_THRESHOLD) {
				Grasping = true;
			}
        }

        if (!Grasping && GetComponent<PHAM_Cylinder>().success())
        {
            PHAM_Manager.nextTask();
        }

        else if(!Grasping && !GetComponent<PHAM_Cylinder>().success())
        {
            GetComponent<Rigidbody>().mass = 5;
            GetComponent<Rigidbody>().useGravity = true;
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }

    }

    //void OnTriggerEnter(Collider other)
    //{   
    //    if()
    //        gameObject.transform.parent = other.gameObject.transform;
    //        gameObject.GetComponent<Collider>().enabled = false;

    //}

}
