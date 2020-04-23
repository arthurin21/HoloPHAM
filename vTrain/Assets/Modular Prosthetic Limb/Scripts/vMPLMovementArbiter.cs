using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.IO.IsolatedStorage;
using System;



public class vMPLMovementArbiter : MonoBehaviour
{
    //---------------------------------------
    // PROPERTIES
    //---------------------------------------
    #region Properties


    //MPL Variables
    #region MPL Variables
    //MPL FEATURES
    // Upper-Arm Joint Angles (7 total) for home position
    private float[] f_HomePositionAngles = new float[7];
	// Finger Joint Angles for home poistion
	private float[] f_HomeFingerAngles = new float[20];

    //Determines whether it is necessary to tell VulcanXHandle to turn off MUD Command inputs
    private bool b_NeedToHaltUDPCommands = false;

    // Modified by Chris Hunt
    // private float[] f_CurrentFingerAngles = new float[20];

    #region MPL Default Position/Rotation Values
    //private float m_default_hand_position_x = -50f;
    //private float m_default_hand_position_y = -10f;
    //private float m_default_hand_position_z = 0f;
    //private float m_default_hand_orientation_x = 90f;
    //private float m_default_hand_orientation_y = 0f;
    //private float m_default_hand_orientation_z = 90f;

    #endregion //MPL Default Position/Rotation Values


    //
    // These member variables hold the named joint position in degrees.
    //
    #region Left Position Member Variables

    static protected float ms_leftShoulderFE;
    static protected float ms_leftShoulderAA;
    static protected float ms_leftHumeralRot;
    static protected float ms_leftElbowFE;
    static protected float ms_leftWristRot;
    static protected float ms_leftWristDev;
    static protected float ms_leftWristFE;
    static protected float ms_leftIndexAA;
    static protected float ms_leftIndexMCP;
    static protected float ms_leftIndexPIP;
    static protected float ms_leftIndexDIP;
    static protected float ms_leftMiddleAA;
    static protected float ms_leftMiddleMCP;
    static protected float ms_leftMiddlePIP;
    static protected float ms_leftMiddleDIP;
    static protected float ms_leftRingAA;
    static protected float ms_leftRingMCP;
    static protected float ms_leftRingPIP;
    static protected float ms_leftRingDIP;
    static protected float ms_leftLittleAA;
    static protected float ms_leftLittleMCP;
    static protected float ms_leftLittlePIP;
    static protected float ms_leftLittleDIP;
    static protected float ms_leftThumbAA;
    static protected float ms_leftThumbFE;
    static protected float ms_leftThumbMCP;
    static protected float ms_leftThumbDIP;

    #endregion


    //
    // These member variables hold the named joint position in degrees.
    //
    #region Right Position Member Variables

    static protected float ms_rightShoulderFE;
    static protected float ms_rightShoulderAA;
    static protected float ms_rightHumeralRot;
    static protected float ms_rightElbowFE;
    static protected float ms_rightWristRot;
    static protected float ms_rightWristDev;
    static protected float ms_rightWristFE;
    static protected float ms_rightIndexAA;
    static protected float ms_rightIndexMCP;
    static protected float ms_rightIndexPIP;
    static protected float ms_rightIndexDIP;
    static protected float ms_rightMiddleAA;
    static protected float ms_rightMiddleMCP;
    static protected float ms_rightMiddlePIP;
    static protected float ms_rightMiddleDIP;
    static protected float ms_rightRingAA;
    static protected float ms_rightRingMCP;
    static protected float ms_rightRingPIP;
    static protected float ms_rightRingDIP;
    static protected float ms_rightLittleAA;
    static protected float ms_rightLittleMCP;
    static protected float ms_rightLittlePIP;
    static protected float ms_rightLittleDIP;
    static protected float ms_rightThumbAA;
    static protected float ms_rightThumbFE;
    static protected float ms_rightThumbMCP;
    static protected float ms_rightThumbDIP;

    #endregion


    #endregion //MPL VARIABLES


    //Movement States
    #region Movement States

    public const int MOVEMENT_STATE_STOP = 0;
    public const int MOVEMENT_STATE_CLENCH = 1;
    public const int MOVEMENT_STATE_FINGER_ROLL = 2;
    public const int MOVEMENT_STATE_WAVE = 3;
    public const int MOVEMENT_STATE_HAND_SHAKE = 4;
    public const int MOVEMENT_STATE_WAG_FINGER = 5;
    public const int MOVEMENT_STATE_FIST_BUMP = 6;
    public const int MOVEMENT_STATE_SPHERE_GRASP = 7;
    public const int MOVEMENT_STATE_CYLINDER_GRASP = 8;

    public int i_MovementState = 5;
    private float stateTimer;

    #endregion // Movement States

    //VulcanX Interface Communication
    #region VulcanX Communication
    // World Interface Handle - will allow the user to pause the game through the World Interface Wrapper script (keeping that functionality centralized)
    //WorldInterfaceWrapper WIFHandle;

    // VulcanX Interface Handle - will allow the user to access VulcanXInterface functions
    //ECoGVulcanXInterface VulcanXHandle;
    VulcanXInterface VulcanXHandle;


    #endregion //VulcanX Communication


    #endregion //Properties


    //---------------------------------------
    // FUNCTIONS - UNITY3D (awake, start, update, reset)
    //---------------------------------------
    #region Unity3D Functions

    #region Start
    /// <summary>
    /// Start function - called on start-up of program
    /// </summary>
	void Start() 
    {

        // --------------------
        // VULCANX COMMUNICATION, MPL HANDLERS
        // --------------------
        #region VulcanX Communication Initialization
        //VulcanX Handlers
        //Directs the WorldInterface to the one in VIESYS - same object (publc World Interface functions can now be called from GameManager)
        GameObject VIESYSHandle = GameObject.Find("VIESYS");
        //WIFHandle = (WorldInterfaceWrapper)VIESYSHandle.GetComponent(typeof(WorldInterfaceWrapper));
        //VulcanXHandle = (ECoGVulcanXInterface)VIESYSHandle.GetComponent(typeof(ECoGVulcanXInterface));
        VulcanXHandle = (VulcanXInterface)VIESYSHandle.GetComponent(typeof(VulcanXInterface));
        #endregion //VulcanX Communication Initialization


        f_HomePositionAngles = new float[7] { 0f,0f, 0f, 0f,0f, 0f, 0f };
        //f_HomePositionAngles = new float[7] { 50f, -10f, 0f, 0f, 0f, 0f, 0f };
        f_HomeFingerAngles = new float[20] { 0f, 20f, 20f, 20f, 0f, 20f, 20f, 20f, 0f, 20f, 20f, 20f, 0f, 20f, 20f, 20f, 20f, 20f, 20f, 20f };
        //f_HomeFingerAngles = new float[20] { 0f, 20f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };


        // --------------------
        // GAME ELEMENTS SETUP
        // --------------------
        #region Game Elements Setup
        //Send the vMPL to the home position defined in the config file
        StartCoroutine(CommandRightMPLPosition(5, f_HomePositionAngles, f_HomeFingerAngles));

        //Set the home position value based on the current position of the hand/palm (after moving it above)
        //f_HomePositionXYZ = GameObject.Find("rPalm").transform.position;//Currently Home Position Sphere location set by XML file

        // Create the Home Position Sphere (depends on config file values for joint angles, and setting of arm to this home position)
        //CreateHomePositionSphere();

        #endregion //Game Elements Setup




        //Movement State (to Start)
        // SetMovementState(MOVEMENT_STATE_FINGER_ROLL);
        SetMovementState(MOVEMENT_STATE_STOP);


    }//function - Start

    #endregion //Start

    #region Update
    //-----------------------------------------------------------------------------
    // UPDATED BY CHRISTOPHER HUNT <chunt11@jhmi.edu>
    void Update()
    {
        // Is called periodically for handling events


        // VulcanXHandle.SetRightUpperArmAngles(f_HomePositionAngles); //Passes an array of the upper 7 joint angles
        // VulcanXHandle.SetRightFingerAngles(f_HomeFingerAngles);


        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    i_MovementState = MOVEMENT_STATE_FINGER_ROLL;

        //}//if - KeyDown (R)
        //else if (Input.GetKeyDown(KeyCode.C))
        //{
        //    i_MovementState = MOVEMENT_STATE_CLENCH;

        //}//if - KeyDown (C)
        //else if (Input.GetKeyDown(KeyCode.S))
        //{
        //    i_MovementState = MOVEMENT_STATE_STOP;

        //}//if - KeyDown (S)
        //else if (Input.GetKeyDown(KeyCode.W))
        //{
        //    i_MovementState = MOVEMENT_STATE_WAVE;

        //}//if - KeyDown (W)
        //else if (Input.GetKeyDown(KeyCode.H))
        //{
        //    i_MovementState = MOVEMENT_STATE_HAND_SHAKE;

        //}//if - KeyDown (H)
        //else if (Input.GetKeyDown(KeyCode.F))
        //{
        //    i_MovementState = MOVEMENT_STATE_WAG_FINGER;

        //}//if - KeyDown (F)
        //else if (Input.GetKeyDown(KeyCode.B))
        //{
        //    i_MovementState = MOVEMENT_STATE_FIST_BUMP;

        //}


                
        // int ii = 0;


        
        // if (GetMovementState() == MOVEMENT_STATE_STOP)
        // {
            
        // }
        // else if (GetMovementState() == MOVEMENT_STATE_CLENCH)
        // {
        //     float f_durationClench = 2f;

        //     float f_OscillatingValueClench = Mathf.PingPong(Time.time, f_durationClench) / f_durationClench;

        //     float f_FingerExtent = 60f;
        //     float[] f_RightFingerAnglesClench = new float[20] { 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent };

        //     for (ii = 0; ii < f_HomeFingerAngles.Length; ii++)
        //     {
        //         f_RightFingerAnglesClench[ii] = f_RightFingerAnglesClench[ii] * f_OscillatingValueClench;

        //     }//for - traverse the finger joints

        //     //Send values to adjust commanded joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesClench);

        // }
        // else if (GetMovementState() == MOVEMENT_STATE_FINGER_ROLL)
        // {
        //     float f_durationFingerRoll = 0.5f;

        //     float f_OscillatingValueIndex = Mathf.PingPong(Time.time, f_durationFingerRoll) / f_durationFingerRoll;
        //     float f_OscillatingValueMiddle = Mathf.PingPong(Time.time - 1 * f_durationFingerRoll / 3, f_durationFingerRoll) / f_durationFingerRoll;
        //     float f_OscillatingValueRing = Mathf.PingPong(Time.time - 2 * f_durationFingerRoll / 3, f_durationFingerRoll) / f_durationFingerRoll;
        //     float f_OscillatingValueLittle = Mathf.PingPong(Time.time - 3 * f_durationFingerRoll / 3, f_durationFingerRoll) / f_durationFingerRoll;

        //     float f_FingerExtent = 60f;
        //     float[] f_RightFingerAnglesRoll = new float[20] { 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent };

        //     for (ii = 0; ii < 4; ii++)
        //     {
        //         f_RightFingerAnglesRoll[ii] = f_RightFingerAnglesRoll[ii] * f_OscillatingValueIndex;

        //     }//for - traverse the finger joints

        //     for (ii = 4; ii < 8; ii++)
        //     {
        //         f_RightFingerAnglesRoll[ii] = f_RightFingerAnglesRoll[ii] * f_OscillatingValueMiddle;

        //     }//for - traverse the finger joints

        //     for (ii = 8; ii < 12; ii++)
        //     {
        //         f_RightFingerAnglesRoll[ii] = f_RightFingerAnglesRoll[ii] * f_OscillatingValueRing;

        //     }//for - traverse the finger joints

        //     for (ii = 12; ii < 16; ii++)
        //     {
        //         f_RightFingerAnglesRoll[ii] = f_RightFingerAnglesRoll[ii] * f_OscillatingValueLittle;

        //     }//for - traverse the finger joints

        //     for (ii = 16; ii < 20; ii++)
        //     {
        //         f_RightFingerAnglesRoll[ii] = f_RightFingerAnglesRoll[ii] * 0.30f;

        //     }//for - traverse the finger joints

        //     //Send values to adjust commanded joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesRoll);


        // }//if - check movement type
        // else if (GetMovementState() == MOVEMENT_STATE_WAVE)
        // {
        //     float[] f_RightFingerAnglesWave = new float[20] { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
        //     float[] f_RightArmJointAnglesWave = new float[7] { -20f, -40f, -90f, 115f, 0f, 0f, 0f };

        //     float f_durationWave = 1f;
        //     float f_OscillatingValueWave = (Mathf.PingPong(Time.time, f_durationWave) / f_durationWave) - 0.5f;
        //     f_RightArmJointAnglesWave[3] = f_RightArmJointAnglesWave[3] + f_OscillatingValueWave * 20f;


        //     // VulcanXHandle.SetRightUpperArmAngles(f_RightArmJointAnglesWave); //Passes an array of the upper 7 joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesWave);


        // }//if - Wave
        // else if (GetMovementState() == MOVEMENT_STATE_HAND_SHAKE)
        // {
        //     float f_FingerExtent = 20f;
        //     float[] f_RightFingerAnglesHandShake = new float[20] { 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent * 1.2f, f_FingerExtent * 1.2f, f_FingerExtent*1.2f, f_FingerExtent*3, f_FingerExtent, f_FingerExtent, f_FingerExtent };

        //     float[] f_RightArmJointAnglesHandShake = new float[7] { 0f, 0f, 00f, 70f, 0f, 0f, 0f };


        //     float f_durationHandShake = 1f;
        //     float f_OscillatingValueHandShake = (Mathf.PingPong(Time.time, f_durationHandShake) / f_durationHandShake) - 0.5f;
        //     f_RightArmJointAnglesHandShake[3] = f_RightArmJointAnglesHandShake[3] + f_OscillatingValueHandShake * 20f;


        //     // VulcanXHandle.SetRightUpperArmAngles(f_RightArmJointAnglesHandShake); //Passes an array of the upper 7 joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesHandShake);


        // }//if - Hand Shake
        // else if (GetMovementState() == MOVEMENT_STATE_WAG_FINGER)
        // {
        //     float f_FingerExtent = 60f;
        //     float[] f_RightFingerAnglesWagFinger = new float[20] { 0f, 0f, 0f, 0f, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent };
                       
        //     float[] f_RightArmJointAnglesWagFinger = new float[7] { -20f, -40f, -90f, 115f, 0f, 0f, 0f };

        //     float f_durationWave = 0.5f;
        //     float f_OscillatingValueFingerWag = (Mathf.PingPong(Time.time, f_durationWave) / f_durationWave) - 0.5f;

        //     //Wagging Finger directly
        //     f_RightFingerAnglesWagFinger[0] = f_OscillatingValueFingerWag * 30f;
        //     f_RightArmJointAnglesWagFinger[5] = f_RightArmJointAnglesWagFinger[5] + f_OscillatingValueFingerWag * 20f;


        //     //Wagging the Wrist Deviator
        //     //f_RightArmJointAnglesWave[5] = f_OscillatingValueFingerWag * 30f;

        //     //Send values to adjust commanded joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesWagFinger);
        //     // VulcanXHandle.SetRightUpperArmAngles(f_RightArmJointAnglesWagFinger); //Passes an array of the upper 7 joint angles
        
        // }//if - Wag Finger
        // else if (GetMovementState() == MOVEMENT_STATE_FIST_BUMP)
        // {
        //     float f_FingerExtent = 65f;
        //     float[] f_RightFingerAnglesFistBump = new float[20] { 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent };

        //     float[] f_RightArmJointAnglesFistBump = new float[7] { 0f, 0f, 0f, 100f, 90f, 0f, 20f };
            
        //     float f_durationFistBump = 1f;
        //     float f_OscillatingValueFistBump = (Mathf.PingPong(Time.time, f_durationFistBump) / f_durationFistBump) - 0.75f;
        //     f_RightArmJointAnglesFistBump[0] = f_RightArmJointAnglesFistBump[0] + f_OscillatingValueFistBump * 20f;
            
        //     // VulcanXHandle.SetRightUpperArmAngles(f_RightArmJointAnglesFistBump); //Passes an array of the upper 7 joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesFistBump);
            
        // }//if - Fist Bump
        // else if (GetMovementState() == MOVEMENT_STATE_SPHERE_GRASP)
        // {
        //     float f_durationClench = 2f;

        //     stateTimer += Time.deltaTime;
        //    // float f_OscillatingValueClench = Mathf.Min(stateTimer / f_durationClench,1);
        //     float[] f_RightArmJointAnglesSphereGrasp = new float[7] { 0f, 0f, 00f, 100f, 130f, 0f, 0f };
        //     float f_OscillatingValueClench = Mathf.PingPong(Time.time, f_durationClench) / f_durationClench;

        //     float f_FingerExtent = 60f;
        //     float[] f_RightFingerAnglesClench = new float[20] { 50f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 50f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 50f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 50f, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent+30, f_FingerExtent- 30, f_FingerExtent- 30, f_FingerExtent-30};

        //     for (ii = 0; ii < f_HomeFingerAngles.Length; ii++)
        //     {
        //         f_RightFingerAnglesClench[ii] = f_RightFingerAnglesClench[ii] * f_OscillatingValueClench;

        //     }//for - traverse the finger joints            

        //     //Send values to adjust commanded joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesClench);
        //     // VulcanXHandle.SetRightUpperArmAngles(f_RightArmJointAnglesSphereGrasp);

        // } //if - Sphere Grasp
        // else if (GetMovementState() == MOVEMENT_STATE_CYLINDER_GRASP)
        // {
        //     float f_durationClench = 2f;

        //     stateTimer += Time.deltaTime;
        //     //float f_OscillatingValueClench = Mathf.Min(stateTimer / f_durationClench, 1);
        //     float[] f_RightArmJointAnglesCylinderGrasp = new float[7] { 0f, 0f, 00f, 100f, 25f, 0f, 0f };
        //     float f_OscillatingValueClench = Mathf.PingPong(Time.time, f_durationClench) / f_durationClench;

        //     float f_FingerExtent = 50f;
        //     float[] f_RightFingerAnglesClench = new float[20] { 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, 0f, f_FingerExtent, f_FingerExtent, f_FingerExtent, f_FingerExtent + 40, f_FingerExtent - 20, f_FingerExtent - 20, f_FingerExtent - 20 };
        //     /*
        //     for (ii = 0; ii < f_HomeFingerAngles.Length; ii++)
        //     {
        //         f_RightFingerAnglesClench[ii] = f_RightFingerAnglesClench[ii] * f_OscillatingValueClench;

        //     }//for - traverse the finger joints            
        //     */
        //     //Send values to adjust commanded joint angles
        //     VulcanXHandle.SetRightFingerAngles(f_RightFingerAnglesClench);
        //     // VulcanXHandle.SetRightUpperArmAngles(f_RightArmJointAnglesCylinderGrasp);

        // } //if - Cylinder Grasp

        float[] f_RightArmJointAngles = new float[7] { ms_rightShoulderFE,
                                                       ms_rightShoulderAA,
                                                       ms_rightHumeralRot,
                                                       ms_rightElbowFE,
                                                       ms_rightWristRot,
                                                       ms_rightWristDev,
                                                       ms_rightWristFE };
        VulcanXHandle.SetRightUpperArmAngles(f_RightArmJointAngles);
        
        float[] f_RightFingerJointAngles = new float[20] { ms_rightIndexAA,  ms_rightIndexMCP,  ms_rightIndexPIP,  ms_rightIndexDIP,
                                                           ms_rightMiddleAA, ms_rightMiddleMCP, ms_rightMiddlePIP, ms_rightMiddleDIP,
                                                           ms_rightRingAA,   ms_rightRingMCP,   ms_rightRingPIP,   ms_rightRingDIP,
                                                           ms_rightLittleAA, ms_rightLittleMCP, ms_rightLittlePIP, ms_rightLittleDIP,
                                                           ms_rightThumbAA,  ms_rightThumbFE,   ms_rightThumbMCP,  ms_rightThumbDIP };
        VulcanXHandle.SetRightFingerAngles( f_RightFingerJointAngles );

    }//function - Update

    #endregion //Update


    #endregion //Unity3D Function


    //---------------------------------------
    // FUNCTIONS - MPL MOVEMENT STATE
    //---------------------------------------
    #region MPL Movement State
    
    public void SetMovementState(int i_NewMovementState)
    {
        if (i_MovementState != i_NewMovementState)
            stateTimer = 0f;
        i_MovementState = i_NewMovementState;

    }//function - SetMovementState


    public int GetMovementState()
    {
        return i_MovementState;

    }//function - GetMovementState

    #endregion //MPL Movement State


    //---------------------------------------
    // FUNCTIONS - MPL INTERFACE (Movement, Attributes)
    //---------------------------------------
    #region MPL Interface (Movement, Attributes)

    /// <summary>
    /// Will reset the hand position to that of start
    /// </summary>
    private IEnumerator CommandRightMPLPosition(float f_Duration, float[] f_RightUpperArmJointAngles, float[] f_RightFingerJointAngles)
    {
        //Will Home the MPL based on position definition in XML file
        bool b_RightHandEnabled = false;
        bool b_LeftHandEnabled = false;

        if (GameObject.Find("rPalm") != null)
        {
            b_RightHandEnabled = true;
        }

        if (GameObject.Find("lPalm") != null)
        {
            b_LeftHandEnabled = true;
        }
                

        //Halt MPL Movement (turn off MUD Commands receipt and processing)
        #region HALT MPL

        if (b_NeedToHaltUDPCommands)
        {
            #region RIGHT HAND

            //Debug.Log("Moving Arm to Home");

            if (b_RightHandEnabled)
            {
                Debug.Log("Moving Right Arm to Home");

                //Freeze the MUD Command System (temporarily)
                VulcanXHandle.SetRightMPLMUDMovementEnable(false);

                //Turn OFF the colliders for the MPL so that it can move into position without being impeded/stalled
                //        Physics.IgnoreLayerCollision(GameObject.Find("Placement_TableTopShelf").layer, GameObject.Find("rPalm").layer, true);
                //        Physics.IgnoreLayerCollision(GameObject.Find("TableHandCol").layer, GameObject.Find("rPalm").layer, true);
                //        Physics.IgnoreLayerCollision(GameObject.Find("DefaultRoom").layer, GameObject.Find("rPalm").layer, true);
            }//if - test for each MPL (right/left)
            #endregion //RIGHT HAND

            #region LEFT HAND
            if (b_LeftHandEnabled)
            {
                Debug.Log("Moving Left Arm to Home");

                //Freeze the MUD Command System (temporarily)
                VulcanXHandle.SetLeftMPLMUDMovementEnable(false);

                //Turn OFF the colliders for the MPL so that it can move into position without being impeded/stalled
                //        Physics.IgnoreLayerCollision(GameObject.Find("Placement_TableTopShelf").layer, GameObject.Find("lPalm").layer, true);
                //        Physics.IgnoreLayerCollision(GameObject.Find("TableHandCol").layer, GameObject.Find("lPalm").layer, true);
                //        Physics.IgnoreLayerCollision(GameObject.Find("DefaultRoom").layer, GameObject.Find("lPalm").layer, true);

            }//if - test for each MPL (right/left)
            #endregion //LEFT HAND

        }//if - b_NeedToHaltUDPCommands

        #endregion //HALT MPL


        //Reset MPL Position for next trial (using internal controller)
        #region COMMANDS

        //Send the vMPL to the commanded position 
        
        
        int i = 0;
        int i_NumberSignals = 20;

        if (b_RightHandEnabled)
        {
            Debug.Log("Setting Right Hand Angles: (" + f_RightUpperArmJointAngles[0] + ", " + f_RightUpperArmJointAngles[1] + ", " + f_RightUpperArmJointAngles[2] + ", " + f_RightUpperArmJointAngles[3] + ", " + f_RightUpperArmJointAngles[4] + ", " + f_RightUpperArmJointAngles[5] + ", " + f_RightUpperArmJointAngles[6] + ")");
            VulcanXHandle.SetRightUpperArmAngles(f_RightUpperArmJointAngles); //Passes an array of the upper 7 joint angles
            VulcanXHandle.SetRightFingerAngles(f_RightFingerJointAngles);

            for (i = 1; i < i_NumberSignals; i++)
            {
                VulcanXHandle.SetRightUpperArmAngles(f_RightUpperArmJointAngles); //Passes an array of the upper 7 joint angles
                VulcanXHandle.SetRightFingerAngles(f_RightFingerJointAngles);

            }//for - continue passing in commands

        }//if - test for each MPL (right/left)


        

        //TODO - Replace with value in XML File
        //yield return new UnityEngine.WaitForSeconds(2);
        //yield return new UnityEngine.WaitForSeconds(timer_Default_PreTrial);
        yield return new UnityEngine.WaitForSeconds(f_Duration);

        #endregion //COMMANDS


        //Turn MPL movement back on (turn on MUD Commands receipt and processing)
        #region FREE MPL
        if (b_NeedToHaltUDPCommands)
        {
            #region RIGHT HAND
            if (b_RightHandEnabled)
            {
                //Turn ON the colliders for the MPL collides normally
                //        Physics.IgnoreLayerCollision(GameObject.Find("TableHandCol").layer, GameObject.Find("rPalm").layer, false);
                //        Physics.IgnoreLayerCollision(GameObject.Find("DefaultRoom").layer, GameObject.Find("rPalm").layer, false);
                //        Physics.IgnoreLayerCollision(GameObject.Find("Placement_TableTopShelf").layer, GameObject.Find("rPalm").layer, false);

                //Un-Freeze the MUD Command System
                VulcanXHandle.SetRightMPLMUDMovementEnable(true);

            }//if - test for each MPL (right/left)
            #endregion //RIGHT HAND

            #region LEFT HAND
            if (b_LeftHandEnabled)
            {
                //Turn ON the colliders for the MPL collides normally
                //        Physics.IgnoreLayerCollision(GameObject.Find("TableHandCol").layer, GameObject.Find("lPalm").layer, false);
                //        Physics.IgnoreLayerCollision(GameObject.Find("DefaultRoom").layer, GameObject.Find("lPalm").layer, false);
                //        Physics.IgnoreLayerCollision(GameObject.Find("Placement_TableTopShelf").layer, GameObject.Find("lPalm").layer, false);

                //Un-Freeze the MUD Command System
                VulcanXHandle.SetLeftMPLMUDMovementEnable(true);

            }//if - test for each MPL (right/left)
            #endregion //LEFT HAND

        }//if - b_NeedToHaltUDPCommands

        #endregion //FREE MPL


    }//function - CommandRightMPLPosition



    #endregion //MPL Interface (Movement, Attributes)


    //---------------------------------------
    // FUNCTIONS - MPL JOINT ANGLES
    //---------------------------------------
    #region MPL Joint Angles

    /// <summary>
    /// Will send an internal command to the limb that sets the right upper-arm joint angles
    /// </summary>
    public void SetRightUpperArmAngles(float[] f_JointAnglesDegrees)
    {
        // float[7] f_JointAnglesDegrees - Joint angles in degrees (0-180)

        #region UpperArmJoints
        ms_rightShoulderFE = f_JointAnglesDegrees[0];
        ms_rightShoulderAA = f_JointAnglesDegrees[1];
        ms_rightHumeralRot = f_JointAnglesDegrees[2];
        ms_rightElbowFE = f_JointAnglesDegrees[3];
        ms_rightWristRot = f_JointAnglesDegrees[4];
        ms_rightWristDev = f_JointAnglesDegrees[5];
        ms_rightWristFE = f_JointAnglesDegrees[6];
        #endregion //UpperArmJoints

        // Debug.Log( string.Format( "SetRightUpperArmAngles: {0}, {1}, {2}, {3}, {4}, {5}, {6}", ms_rightShoulderFE.ToString(), ms_rightShoulderAA.ToString(), ms_rightHumeralRot.ToString(), 
        //             ms_rightElbowFE.ToString(), ms_rightWristRot.ToString(), ms_rightWristDev.ToString(), ms_rightWristFE.ToString() ) );
    }//function - SetRightUpperArmAngles


    /// <summary>
    /// Will send an internal command to the limb that sets the right finger joint angles
    /// </summary>
    public void SetRightFingerAngles(float[] j_FingerJointAnglesDegrees)
    {
        #region Fingers
        ms_rightIndexAA = j_FingerJointAnglesDegrees[0];
        ms_rightIndexMCP = j_FingerJointAnglesDegrees[1];
        ms_rightIndexPIP = j_FingerJointAnglesDegrees[2];
        ms_rightIndexDIP = j_FingerJointAnglesDegrees[3];
        ms_rightMiddleAA = j_FingerJointAnglesDegrees[4];
        ms_rightMiddleMCP = j_FingerJointAnglesDegrees[5];
        ms_rightMiddlePIP = j_FingerJointAnglesDegrees[6];
        ms_rightMiddleDIP = j_FingerJointAnglesDegrees[7];
        ms_rightRingAA = j_FingerJointAnglesDegrees[8];
        ms_rightRingMCP = j_FingerJointAnglesDegrees[9];
        ms_rightRingPIP = j_FingerJointAnglesDegrees[10];
        ms_rightRingDIP = j_FingerJointAnglesDegrees[11];
        ms_rightLittleAA = j_FingerJointAnglesDegrees[12];
        ms_rightLittleMCP = j_FingerJointAnglesDegrees[13];
        ms_rightLittlePIP = j_FingerJointAnglesDegrees[14];
        ms_rightLittleDIP = j_FingerJointAnglesDegrees[15];
        ms_rightThumbAA = j_FingerJointAnglesDegrees[16];
        ms_rightThumbFE = j_FingerJointAnglesDegrees[17];
        ms_rightThumbMCP = j_FingerJointAnglesDegrees[18];
        ms_rightThumbDIP = j_FingerJointAnglesDegrees[19];
        #endregion //Fingers

    }//function - SetRightFingerAngles




    /// <summary>
    /// Will send an internal command to the limb that sets the left upper-arm joint angles
    /// </summary>
    public void SetLeftUpperArmAngles(float[] f_JointAnglesDegrees)
    {
        // float[7] f_JointAnglesDegrees - Joint angles in degrees (0-180)

        #region UpperArmJoints
        ms_leftShoulderFE = f_JointAnglesDegrees[0];
        ms_leftShoulderAA = f_JointAnglesDegrees[1];
        ms_leftHumeralRot = f_JointAnglesDegrees[2];
        ms_leftElbowFE = f_JointAnglesDegrees[3];
        ms_leftWristRot = f_JointAnglesDegrees[4];
        ms_leftWristDev = f_JointAnglesDegrees[5];
        ms_leftWristFE = f_JointAnglesDegrees[6];
        #endregion //UpperArmJoints

    }//function - SetLeftUpperArmAngles


    /// <summary>
    /// Will send an internal command to the limb that sets the left finger joint angles
    /// </summary>
    public void SetLeftFingerAngles(float[] j_FingerJointAnglesDegrees)
    {
        #region Fingers
        ms_leftIndexAA = j_FingerJointAnglesDegrees[0];
        ms_leftIndexMCP = j_FingerJointAnglesDegrees[1];
        ms_leftIndexPIP = j_FingerJointAnglesDegrees[2];
        ms_leftIndexDIP = j_FingerJointAnglesDegrees[3];
        ms_leftMiddleAA = j_FingerJointAnglesDegrees[4];
        ms_leftMiddleMCP = j_FingerJointAnglesDegrees[5];
        ms_leftMiddlePIP = j_FingerJointAnglesDegrees[6];
        ms_leftMiddleDIP = j_FingerJointAnglesDegrees[7];
        ms_leftRingAA = j_FingerJointAnglesDegrees[8];
        ms_leftRingMCP = j_FingerJointAnglesDegrees[9];
        ms_leftRingPIP = j_FingerJointAnglesDegrees[10];
        ms_leftRingDIP = j_FingerJointAnglesDegrees[11];
        ms_leftLittleAA = j_FingerJointAnglesDegrees[12];
        ms_leftLittleMCP = j_FingerJointAnglesDegrees[13];
        ms_leftLittlePIP = j_FingerJointAnglesDegrees[14];
        ms_leftLittleDIP = j_FingerJointAnglesDegrees[15];
        ms_leftThumbAA = j_FingerJointAnglesDegrees[16];
        ms_leftThumbFE = j_FingerJointAnglesDegrees[17];
        ms_leftThumbMCP = j_FingerJointAnglesDegrees[18];
        ms_leftThumbDIP = j_FingerJointAnglesDegrees[19];
        #endregion //Fingers

    }//function - SetRightFingerAngles

#endregion //MPL Joint Angles


    // Added by Christopher Hunt <chunt11@jhmi.edu>
    public float [] GetRightUpperArmAngles() {
        float[] ret = new float[7];

        ret[0] = ms_rightShoulderFE;
        ret[1] = ms_rightShoulderAA;
        ret[2] = ms_rightHumeralRot;
        ret[3] = ms_rightElbowFE;
        ret[4] = ms_rightWristRot;
        ret[5] = ms_rightWristDev;
        ret[6] = ms_rightWristFE;

        return ret;
    }

    public float [] GetRightFingerAngles() {
        float[] ret = new float[20];

        ret[0] = ms_rightIndexAA;
        ret[1] = ms_rightIndexMCP;
        ret[2] = ms_rightIndexPIP;
        ret[3] = ms_rightIndexDIP;
        ret[4] = ms_rightMiddleAA;
        ret[5] = ms_rightMiddleMCP;
        ret[6] = ms_rightMiddlePIP;
        ret[7] = ms_rightMiddleDIP;
        ret[8] = ms_rightRingAA;
        ret[9] = ms_rightRingMCP;
        ret[10] = ms_rightRingPIP;
        ret[11] = ms_rightRingDIP;
        ret[12] = ms_rightLittleAA;
        ret[13] = ms_rightLittleMCP;
        ret[14] = ms_rightLittlePIP;
        ret[15] = ms_rightLittleDIP;
        ret[16] = ms_rightThumbAA;
        ret[17] = ms_rightThumbFE;
        ret[18] = ms_rightThumbMCP;
        ret[19] = ms_rightThumbDIP;

        return ret;
    }

}//file - CatchGameManager
