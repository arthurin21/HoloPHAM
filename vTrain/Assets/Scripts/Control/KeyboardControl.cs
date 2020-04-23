using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardControl : MonoBehaviour
{
    const int NUM_MPL_JOINT_ANGLES = 7;
    
    int dof = 0;
    float speed = 10.0f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    
    private GameObject hud = null;
    private GraspingLogic grasp = null;
    private vMPLMovementArbiter arbiter = null;
    private float [] joint_angles = new float[NUM_MPL_JOINT_ANGLES];

    // Start is called before the first frame update
    void Start()
    {
        // grasp = GameObject.Find("CylinderPrimitive").GetComponent<GraspingLogic>();
        hud = GameObject.Find( "HUD" );
        grasp = (GraspingLogic) GameObject.FindObjectOfType<GraspingLogic>();
        arbiter = GameObject.Find("vMPLMovementArbiter").GetComponent<vMPLMovementArbiter>();
    }

    // Update is called once per frame
    void Update()
    {
        // set arm degree of freedom
        if ( Input.GetKey( KeyCode.Alpha1 ) || Input.GetKey( KeyCode.Keypad1 ) ) { 
            Debug.Log( "Controlling Shoulder Flexion/Extension..." );
            dof = 0; 
        }
        else if ( Input.GetKey( KeyCode.Alpha2 ) || Input.GetKey( KeyCode.Keypad2 ) ) { 
            Debug.Log( "Controlling Shoulder Abduction/Adduction..." );
            dof = 1; 
        } 
        else if ( Input.GetKey( KeyCode.Alpha3 ) || Input.GetKey( KeyCode.Keypad3 ) ) { 
            Debug.Log( "Controlling Humeral Internal/External Rotation..." );
            dof = 2; 
        }
        else if ( Input.GetKey( KeyCode.Alpha4 ) || Input.GetKey( KeyCode.Keypad4 ) ) {
            Debug.Log( "Controlling Elbow Flexion/Extension..." );
            dof = 3; 
        }  
        else if ( Input.GetKey( KeyCode.Alpha5 ) || Input.GetKey( KeyCode.Keypad5 ) ) { 
            Debug.Log( "Controlling Wrist Pronation/Supination..." );
            dof = 4; 
        }
        else if ( Input.GetKey( KeyCode.Alpha6 ) || Input.GetKey( KeyCode.Keypad6 ) ) { 
            Debug.Log( "Controlling Wrist Radial/Ulnar Deviation..." );
            dof = 5; 
        }
        else if ( Input.GetKey( KeyCode.Alpha7 ) || Input.GetKey( KeyCode.Keypad7 ) ) { 
            Debug.Log( "Controlling Wrist Flexion/Extension..." );
            dof = 6; 
        }

        // move arm
        if ( Input.GetKey( KeyCode.Equals ) || Input.GetKey( KeyCode.KeypadPlus ) ) {
            joint_angles[dof]++;
        } else if ( Input.GetKey( KeyCode.Minus ) || Input.GetKey( KeyCode.KeypadMinus ) ) {
            joint_angles[dof]--;
        }
        

        // Debug.Log( string.Format( "Joint Angles: {0}, {1}, {2}, {3}, {4}, {5}, {6}", joint_angles[0].ToString("F1"), joint_angles[1].ToString("F1"), 
        //                                                                              joint_angles[2].ToString("F1"), joint_angles[3].ToString("F1"), 
        //                                                                              joint_angles[4].ToString("F1"), joint_angles[5].ToString("F1"), 
        //                                                                              joint_angles[6].ToString("F1") ) );
        arbiter.SetRightUpperArmAngles( joint_angles );

        // move camera
        if ( Input.GetKey( KeyCode.UpArrow ) ) {
            Debug.Log( "Moving Forward..." );
            Camera.main.transform.Translate( new Vector3( 0, 0, speed * Time.deltaTime ) );
        } else if ( Input.GetKey( KeyCode.DownArrow ) ) {
            Debug.Log( "Moving Backward..." );
            Camera.main.transform.Translate( new Vector3( 0, 0, -speed * Time.deltaTime ) );
        } else if ( Input.GetKey( KeyCode.LeftArrow ) ) {
            Debug.Log( "Moving Left..." );
            Camera.main.transform.Translate( new Vector3( -speed * Time.deltaTime, 0, 0 ) );
        } else if ( Input.GetKey( KeyCode.RightArrow ) ) {
            Debug.Log( "Moving Right..." );
            Camera.main.transform.Translate( new Vector3( speed * Time.deltaTime, 0, 0 ) );
        }

        // rotate camera
        if ( Input.GetMouseButton( 0 ) ) {
            yaw += Input.GetAxis( "Mouse X" );
            pitch -= Input.GetAxis( "Mouse Y" );
            Camera.main.transform.eulerAngles = new Vector3( pitch, yaw, 0.0f );
            // Debug.Log( Camera.main.transform.eulerAngles );
        }

        // grasp
        if ( Input.GetKeyDown( KeyCode.G ) ) {
            grasp.Grasping = !grasp.Grasping;
        } 

        // hud
        if ( Input.GetKeyDown( KeyCode.H ) ) {
            hud.SetActive( !hud.active );
        }
    }
}
