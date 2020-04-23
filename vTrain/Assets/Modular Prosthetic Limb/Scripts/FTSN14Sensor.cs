//
// README: IMPORTANT WARNING - EXPORT CONTROL LANGUAGE
// 
// This information, software, technology being shared MUST be 
// handled in accordance with the statement below.  All documentation
// related to Software and Technology Development associated with 
// this shared information must include this statement:
//
// “The information we are providing contains proprietary software/
// technology and is therefore export controlled.   The specific 
// Export Control Classification Number (ECCN) applied to this 
// software, 3D991, is currently controlled to only 5 countries: 
// N. Korea, Syria, Sudan, Cuba, or Iran.  Before providing this 
// software or data to any foreign person, you should consult with 
// your organization’s export compliance or legal office.  Of course,
// the nature of our contractual relationship requires that only 
// people associated with Revolutionizing Prosthetics Phase 3 may 
// have access to this information.”
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FTSN14Sensor : MonoBehaviour {

    //---------------------------------------
    // VARIABLE DECLARATIONS
    //---------------------------------------
    #region Variable Declarations

    
    //VMPL OBJECTS
    #region VMPL OBJECTS
    //Local pointers to all limb Hinge Joints 
    #region HingeJoint Pointers
    protected HingeJoint m_shFE;
    protected HingeJoint m_shAA;
    protected HingeJoint m_humRot;
    protected HingeJoint m_elbFE;
    protected HingeJoint m_wrRot;
    protected HingeJoint m_wrDev;
    protected HingeJoint m_wrFE;
    protected HingeJoint m_finAA;
    protected HingeJoint m_mcpFE;
    protected HingeJoint m_proxFE;
    protected HingeJoint m_medFE;

    #endregion //HingeJoint Pointers


    //Joint Angle Values (commanded and actual)
    #region Joint Angle Values (Commanded, Actual)
    //protected float[] m_actVal = new float[11];
    protected float[] m_cmdVal = new float[11];
    
    #endregion //Joint Angle Values (Commanded, Actual)


    //Finger Objects & Delegate Methods
    #region Finger/Thumb Objects (used to poll actual joint angles)
    protected delegate float ValueAA14();
    protected delegate float ValueMCP14();
    protected delegate float ValuePIP14();
    protected delegate float ValueDIP14();
    protected ValueAA14 m_valAA;
    protected ValueMCP14 m_valMCP;
    protected ValuePIP14 m_valPIP;
    protected ValueDIP14 m_valDIP;

    #endregion //Finger/Thumb Objects (used to poll actual joint angles)

    #endregion //VMPL OBJECTS


    //SENSORS and COLLISIONS
    #region SENSORS AND COLLISIONS

    //Records of existing collisions
//    protected Dictionary<Collider, string> m_validContacts;

    protected static readonly object m_valLock = new object();

    //Sensor Location in Arm
    protected string str_WhichArm = "Right"; //Right is default - can determine by looking at the layer of the object that the sensor is attached to
    protected int i_SensorLocation = 10; //10 is default (default location for force sensor is distal segment of finger)
    protected int i_SensorLocationTorqueMeasureBegin = 7; //7 is the default - beginning of finger... where attached to palm (record discrepency in angle from this location and distal)
    protected int i_SensorLocationTorqueMeasureEnd = 10; //10 is the default - tip of finger (record discrepency in angle from this location to 'begin' location)
    protected GameObject go_ThisObject; //Is the pointer to the game object that the sensor is attached to
    protected GameObject go_AttachedObject; //Is the pointer to the game object that the sensor is attached to
    protected LayerMask layer_ThisObject;
    protected List<HingeJoint> list_UnilateralHinges; //Will contain all the hinges, in the proper order, of the current limb from sensor to root (if sensor is not at the finger tip, will only point to sensor location and proximal to shoulder root)
    
    //Last detected collision
//    private Collision m_lastCollision = null;

    //Current Object Name
    public string m_name;

    //Force and Velocity Vectors for passing out data
    private Vector3 m_normal = new Vector3();
    private Vector3 m_ColliderNormal = new Vector3();
    private Vector3 m_relvel = new Vector3();
    private float f_CollisionDistance = 0f;

    // Simulated contact force
    protected float m_forceMagnitude;
    protected float f_ForceMagnitudeMulitplierScalar = 2f;
    protected Vector3 m_contactForce;
    protected float f_ReportedForce = 0f;
    protected bool m_contact = false;
    public bool InContact { get { return m_contact; } }

    //to convert sum of angle differences use the following
    //max measured sum using Matlab is 97 but we'll top it off at 100
    //max sensor force is 67N (see RP3-SD630-0106-DBD)
    //protected float m_scaleNewton = 67.0F / 100.0F;

    private bool b_MoveableSensorsEnabled = false;

    #endregion //SENSORS AND COLLISIONS


    //DEBUGGERS
    #region GUI DEBUGGERS
    /*
    public string str_GUILabelStringTitle = "Force Sensors (Magnitude, X, Y, Z)";
    public string str_GUILabelStringIndexForceSensorAxes = "";
    public string str_GUILabelStringMiddleForceSensorAxes = "";
    public string str_GUILabelStringThumbForceSensorAxes = "";
    public const int i_labelWidth = 1000;
    public const int i_labelLeft = 175;
    public int i_labelHeight = 20;
    public const bool flag_ShowLabels = true;
    public string str_label = "";
    public const string str_format1 = "##0.00";
    public const string str_GUILabelStringPIDTitle = "PID Controller Values (tarV, deltaAng, P, I, D, dV, DampN, DampD)";
    */
    #endregion //GUI DEBUGGERS


    #endregion //Variable Declarations


    //---------------------------------------
    // FUNCTIONS - INITIALIZATION
    //---------------------------------------
    //INITIALIZATION FUNCTIONS
    #region INITIALIZATION FUNCTIONS

    /// <summary>
    /// This function will determine where this sensor is attached to, and which arm
    /// The location will determine how the force is calculated, including the axis in 
    /// space, and which joints to use to determine difference in angle
    /// </summary>
    private void GetSensorLocation()
    {
        //Will determine the object that this sensor is attached to.
        //Assumes that the sensor is a script attached to an individual, moveable gameobject
        // that is the child object of a parent object on the limb
        int ii = 0; //used as a counter that is referenced out of loop

        //GAME OBJECT
        #region Game Object
        go_ThisObject = this.gameObject;
        go_AttachedObject = go_ThisObject.transform.parent.gameObject; //assumes that currently a child of vMPL Component
        go_AttachedObject = go_AttachedObject.transform.parent.transform.parent.gameObject; //two levels up required to get to finger tip

        //Debug.Log("Object Name: " + go_ThisObject.name.ToString() + ", Attached Object: + " + go_AttachedObject.name.ToString());

        #endregion //Game Object


        if (go_AttachedObject != null)
        {
            //RIGHT or LEFT Arm
            #region Right or Left Arm
            layer_ThisObject = go_AttachedObject.layer;

            //if (String.Equals(go_AttachedObject.layer, "RightArm"))
            if (GameObject.Find("rPalm") != null)
            {
                //if (go_AttachedObject.layer == GameObject.Find("rPalm").layer)
                if ((go_AttachedObject.layer == LayerMask.NameToLayer("RightArmSensor")) || (go_AttachedObject.layer == GameObject.Find("rPalm").layer))
                {
                    str_WhichArm = "Right";
                }
            }
            if (GameObject.Find("lPalm") != null)
            {
                //else if (String.Equals(go_AttachedObject.layer, "LeftArm"))
                //if (go_AttachedObject.layer == GameObject.Find("lPalm").layer)
                if ((go_AttachedObject.layer == LayerMask.NameToLayer("LeftArmSensor")) || (go_AttachedObject.layer == GameObject.Find("lPalm").layer))
                {
                    //Left has inversion object, need to go down one level to reach sensor
                    go_AttachedObject = go_AttachedObject.transform.parent.gameObject;

                    str_WhichArm = "Left";
                }//if - check to see which limb the sensor is attached to
            }//if - check to see if right/left arm present in scenario    
            #endregion //Right or Left Arm


            //GET HINGES FROM CURRENT ARM
            #region Get Hinges From Arm
            //UnityEngine.Object[] objects_Temp = GameObject.FindObjectsOfType(typeof(HingeJoint));
            UnityEngine.Object[] objects_Temp = GameObject.FindObjectsOfType(typeof(HingeJoint));
            List<HingeJoint> list_AllHinges = new List<HingeJoint>();
            List<HingeJoint> list_TempUnilateralHinges = new List<HingeJoint>();
            
            //Instantiate Hinge List for Ordered Hinges
            list_UnilateralHinges = new List<HingeJoint>();

            foreach (UnityEngine.Object o in objects_Temp)
            {
                list_AllHinges.Add(o as HingeJoint);
            }//foreach - add all hinges in scenario

            for (ii = 0; ii < list_AllHinges.Count; ii++)
            {
                //Add hinges that are on the connected arm
                if (list_AllHinges[ii].gameObject.layer == layer_ThisObject) //checks against assigned layer - should be "RightArm" or "LeftArm"
                {
                    list_TempUnilateralHinges.Add(list_AllHinges[ii]);
                }//if - check for current (unilateral) arm

            }//for - traverse all hinges in scenario

            #endregion //Get Hinges From Arm


            //LOCATION ON ARM
            #region Location on Arm
            //For specific object name, could use a catch to determine where in arm - hardcoded for the vMPL
            //For location in the heiarchy of the limb, independent of number of segments, use recursion/iteration

            //GameObject go_Temp = go_AttachedObject;
            HingeJoint hj_Temp = new HingeJoint();
            Transform t_Temp = go_AttachedObject.transform; //IF FORCE SENSOR SCRIPT IS ON INDP. OBJECT for SENSOR
            //Transform t_Temp = go_ThisObject.transform; //IF FORCE SENSOR SCRIPT IS ON FINGER

            //Determine how many joints to include in Force Magnitude calculation (for finger, all finger joints... for another location, just most proximal joint)
            i_SensorLocationTorqueMeasureBegin = 0;
            i_SensorLocationTorqueMeasureEnd = 0; //Only consider measuring forces/torques for segment that sensor is attached to, 


            for (ii = 0; ii < 20; ii++) //shouldn't be more than 10 iterations
            {
                if (string.Equals(t_Temp.gameObject.name, "rShoulderRoot") || string.Equals(t_Temp.gameObject.name, "lShoulderRoot")) //check for last segment in tree
                //if (string.Equals(t_Temp.gameObject.name, "rShoulderFlexAssembly") || string.Equals(t_Temp.gameObject.name, "lShoulderFlexAssembly")) //check for last segment in tree
                {
                    break;
                }//if - check to see if at the root - stop iterating

                //Get Hinge from transform
                hj_Temp = FindHingeJoint(list_TempUnilateralHinges, t_Temp); //Middle segment of finger

                //Debug.Log("Traversing... current: " + t_Temp.gameObject.name + " - " + hj_Temp.name);

                if (hj_Temp != null)
                {
                    //Add Hinge to list in order (rebuilding tree)
                    list_UnilateralHinges.Add(hj_Temp);

                    //Get transform of the hinge for next interation
                    t_Temp = hj_Temp.transform; //assign the temp transform for the next move up the tree
                    //list_UnilateralHinges.Add(FindHingeJoint(list_UnilateralHinges, hj_Temp.transform)); //Middle segment of finger, //assign the temp transform for the next move up the tree

                    //Check to see if at Palm - if find palm after first iteration, then sensor attached to finger - make not of location of palm in tree
                    if (string.Equals(t_Temp.gameObject.name, "rPalm") || string.Equals(t_Temp.gameObject.name, "lPalm")) //check for palm - if found, sensor attached to some part on finger
                    {
                        i_SensorLocationTorqueMeasureEnd = ii;
                    }//if - check to see where attached to arm - if distal to palm (finger) or proximal

                }//if - check to make sure hinge was found
                else
                {
                    //Debug.Log("Hit end of tree");
                    break; //exit for loop if ran through tree
                    
                }//if - check for null hinge

            }//for - traverse up hinges - reconstruct 'tree' or limb


            //Assign location in tree
            i_SensorLocation = ii; //How many segment trips up the tree to get to root


            #region Debug Logging
            
            //Test Navigation up tree
            //Debug.Log(this.name + " - Sensor Location: (" + i_SensorLocation + ", " + i_SensorLocationTorqueMeasureBegin + ", " + i_SensorLocationTorqueMeasureEnd + ") - " + list_UnilateralHinges[0].name + ", " + list_UnilateralHinges[1].name + ", " + list_UnilateralHinges[2].name + ", " + list_UnilateralHinges[3].name + ", " + list_UnilateralHinges[4].name + ", " + list_UnilateralHinges[5].name + ", " + list_UnilateralHinges[6].name + ", .. (" + list_UnilateralHinges.Count + " total)");
            string str_DebugMessage = m_name + " (" + list_UnilateralHinges.Count + " components) - Location: (" + i_SensorLocation + ", " + i_SensorLocationTorqueMeasureBegin + ", " + i_SensorLocationTorqueMeasureEnd + ") - " ;
            for (ii = 0; ii < list_UnilateralHinges.Count; ii++)
            {
                str_DebugMessage = str_DebugMessage + list_UnilateralHinges[ii].name + ", ";

            }//for - add all components in the tree

            //Debug.Log(this.name + ", " + m_name + " - Sensor Location: (" + i_SensorLocation + ", " + i_SensorLocationTorqueMeasureBegin + ", " + i_SensorLocationTorqueMeasureEnd + ") - " + list_UnilateralHinges[0].name + ", " + list_UnilateralHinges[1].name + ", " + list_UnilateralHinges[2].name + ", " + list_UnilateralHinges[3].name + ", " + list_UnilateralHinges[4].name + ", " + list_UnilateralHinges[5].name + ", " + list_UnilateralHinges[6].name + ", " + list_UnilateralHinges[7].name + ", " + list_UnilateralHinges[8].name + ", " + list_UnilateralHinges[9].name + ", " + list_UnilateralHinges[10].name + ", .. (" + list_UnilateralHinges.Count + " total)");
//            Debug.Log(str_DebugMessage);

            #endregion //Debug Logging


            #endregion //Location on Arm

        }//if - check to make sure attached object is not null
        else
        {
            Debug.Log("Force Sensor attached to invalid object");
        }//if - check to make sure attached object is not null

    }//function - GetSensorLocation


    /// <summary>
    /// Will assign/reassign WIF ID based on the object name
    /// </summary>
    private void SetWIFID()
    {
        try
        {
            //Assume that the go_AttachedObject has been reset and is valid
            this.gameObject.AddComponent<WorldObject>();
            
            WorldObject wo_Temp = this.GetComponent<WorldObject>(); //Points to World Object object

            //Assign the current object name to the World Object
            wo_Temp.m_id = m_name;
        }
        catch
        {

        }//try - attempt to find World Object and set name

    }//function - SetWIFID


    /// <summary>
    /// Will assign/reassign the parent of the object based on the current fixed joint in place
    /// </summary>
    private void SetFTSNParentToFixedJointAttachment()
    {
        //Assume that the go_AttachedObject has been reset and is valid
        
        FixedJoint fj_Temp = this.GetComponent<FixedJoint>(); //Points to Fixed Joint for FTSN
        

        //Assign Parent
        this.transform.parent = fj_Temp.connectedBody.transform; //Reassign the Fixed Joint for the FTSN based on the current "parent" component of vMPL

        this.GetComponent<FixedJoint>().breakForce = 1000f;
        fj_Temp.breakForce = 1000f;

        

    }//function - SetFTSNParentToFixedJointAttachment


    /// <summary>
    /// Will assign/reassign the sensor location in space
    /// </summary>
    private void SetFTSNLocation(Vector3 v3_LocationInSpace)
    {
        //Assume that the go_AttachedObject has been reset and is valid
        
        this.transform.position = v3_LocationInSpace;

        
    }//function - SetFTSNLocation


    #endregion //Initialization Functions


    //COMPONENT INITIALIZATION / ASSIGNMENTS
    #region COMPONENT ASSIGNMENTS

    //Fixed Joint (Attachment of FTSN to location on vMPL)
    #region Fixed Joint
    
    /// <summary>
    /// Will assign/reassign fixed joint between FTSN and vMPL Component
    /// </summary>
    private void SetFTSNFixedJointTovMPL()
    {
        /*
        //Assume that the go_AttachedObject has been reset and is valid
        FixedJoint fj_Temp = this.GetComponent<FixedJoint>(); //Points to Fixed Joint for FTSN
        if (go_AttachedObject != null)
        {
            if (fj_Temp != null)
            {
                fj_Temp.connectedBody = go_AttachedObject.rigidbody; //Reassign the Fixed Joint for the FTSN based on the current "parent" component of vMPL
                
            }
            else
            {
                Debug.Log("Null Fixed Joint: " + this.name + ", Parent: " + this.transform.parent.name.ToString());
            }
        }
        else
        {
            Debug.Log("Null Attached Object: " + this.name + ", Parent: " + this.transform.parent.name.ToString());
        }

        //Destroy(this.GetComponent<FixedJoint>());
        //Destroy(this.rigidbody);

        */
    }//function - SetFTSNFixedJointTovMPL

    #endregion //Fixed Joint


    //Rigid Body Parameters
    #region Rigid Body Parameters

    /// <summary>
    /// Will assign/reassign fixed joint between FTSN and vMPL Component
    /// </summary>
    private void SetRigidBodyParameters()
    {
        if (this.GetComponent<Rigidbody>() != null)
        {
            //Set the Drag to avoid bouncing of the sensors (give their location on the arm, and their small mass)
            this.GetComponent<Rigidbody>().drag = 100f;

            this.GetComponent<Rigidbody>().angularDrag = 100f;

            this.GetComponent<Rigidbody>().mass = 0.00001f;

        }//if - test for rigidbody presence

    }//function - SetRigidBodyParameters


    #endregion //Rigid Body Paramters

    //Hinge Joints
    #region Hinge Joints
    /// <summary>
    /// Will assign hinge joint game objects to local pointers
    /// </summary>
    private void MapHingeJoints()
    {
        //Function has been replaced with modular dynamic flexible assignments 
        // of hinges that update based on location on vMPL

        #region Commented Out
        /*
        UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(HingeJoint));
        List<HingeJoint> hinges = new List<HingeJoint>();
        foreach (UnityEngine.Object o in objects)
        {
            hinges.Add(o as HingeJoint);
        }

        m_medFE = FindHingeJoint(hinges, transform); //Most distal part of finger
        m_proxFE = FindHingeJoint(hinges, m_medFE.transform); //Middle segment of finger
        m_mcpFE = FindHingeJoint(hinges, m_proxFE.transform); //the flexor segment of finger
        m_finAA = FindHingeJoint(hinges, m_mcpFE.transform); //Finger abductor (connection of finger to palm)
        m_wrFE = FindHingeJoint(hinges, m_finAA.transform); //Wrist Flexor joint (connection of palm to lower arm/wrist)
        m_wrDev = FindHingeJoint(hinges, m_wrFE.transform); //Wrist 
        m_wrRot = FindHingeJoint(hinges, m_wrDev.transform);
        m_elbFE = FindHingeJoint(hinges, m_wrRot.transform);
        m_humRot = FindHingeJoint(hinges, m_elbFE.transform);
        m_shAA = FindHingeJoint(hinges, m_humRot.transform);
        m_shFE = FindHingeJoint(hinges, m_shAA.transform);

        //Debug.Log("Final Object Name: " + m_shFE.gameObject.name);

        */

        //Previous use of rigidbodies
        #region Commented Out
        /*
        m_medFE = FindHingeJoint(hinges, rigidbody);
        m_proxFE = FindHingeJoint(hinges, m_medFE.rigidbody);
        m_mcpFE = FindHingeJoint(hinges, m_proxFE.rigidbody);
        m_finAA = FindHingeJoint(hinges, m_mcpFE.rigidbody);
        m_wrFE = FindHingeJoint(hinges, m_finAA.rigidbody);
        m_wrDev = FindHingeJoint(hinges, m_wrFE.rigidbody);
        m_wrRot = FindHingeJoint(hinges, m_wrDev.rigidbody);
        m_elbFE = FindHingeJoint(hinges, m_wrRot.rigidbody);
        m_humRot = FindHingeJoint(hinges, m_elbFE.rigidbody);
        m_shAA = FindHingeJoint(hinges, m_humRot.rigidbody);
        m_shFE = FindHingeJoint(hinges, m_shAA.rigidbody);
        */
        #endregion //Commented Out

        #endregion //Commented Out

    }//function - MapHingeJoints


    /// <summary>
    /// Will return hinge joints of passed in rigidbody, compared to pool of hinge joints passed in
    /// </summary>
    private HingeJoint FindHingeJoint(List<HingeJoint> hinges, Rigidbody rigidbody)
    {

        HingeJoint hj = null;
        foreach (HingeJoint joint in hinges)
        {
            if (joint.connectedBody == rigidbody)
            {
                hj = joint;
                break;
            }
        }

        // Hinge found, so remove it from the searchable pool of hinges
        if (hj != null)
            hinges.Remove(hj);

        return hj;
    }//function - FindHingeJoint

    

    /// <summary>
    /// Will return hinge joints of passed in rigidbody, compared to pool of hinge joints passed in
    /// </summary>
    private HingeJoint FindHingeJoint(List<HingeJoint> hinges, Transform t_transform)
    {

        HingeJoint hj = null;
        foreach (HingeJoint joint in hinges)
        {
            if (joint.connectedBody.transform == t_transform)
            {
                hj = joint;
                break;
            }
        }

        // Hinge found, so remove it from the searchable pool of hinges
        if (hj != null)
            hinges.Remove(hj);

        return hj;
    }//function - FindHingeJoint

    #endregion //Hinge Joints


    //Fingers
    #region Finger Segment Assignments (Sensorized)
    /// <summary>
    /// Pulls the joint angle from each of the measured joints from VulcanXInterface (sole instance) running in scenario
    /// </summary>
    private void MapDelegates()
    {
        //Map Delegates for functions in VulcanXInterface that collect currently commanded joint angles
        // This will map delegates for finger joints, which can vary based on where the sensor is 
        // attached to the limb, will need to check attached object and assign dynamically


        //If user can dynamically change location/attachment of force sensors - this function will need 
        // to be routinely called to update these assignments

        String str_AttachedObjectName = go_AttachedObject.name;


        if (string.Equals(str_AttachedObjectName.Substring(0, 1), "r"))
        {
            //RIGHT vMPL
            //Debug.Log("This: " + this.name + ", attached: " + go_AttachedObject.name);

            #region Right vMPL Finger/Thumb Assignments
            if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Ind"))
            {
                //INDEX FINGER
                m_valAA = delegate() { return VulcanXInterface.RightIndexAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightIndexMCP(); };
                m_valPIP = delegate() { return VulcanXInterface.RightIndexPIP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightIndexDIP(); };
            }
            else if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Mid"))
            {
                //MIDDLE FINGER
                m_valAA = delegate() { return VulcanXInterface.RightMiddleAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightMiddleMCP(); };
                m_valPIP = delegate() { return VulcanXInterface.RightMiddlePIP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightMiddleDIP(); };
            }
            else if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Rin"))
            {
                //RING FINGER
                m_valAA = delegate() { return VulcanXInterface.RightRingAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightRingMCP(); };
                m_valPIP = delegate() { return VulcanXInterface.RightRingPIP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightRingDIP(); };
            }
            else if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Lit"))
            {
                //LITTLE FINGER
                m_valAA = delegate() { return VulcanXInterface.RightLittleAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightLittleMCP(); };
                m_valPIP = delegate() { return VulcanXInterface.RightLittlePIP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightLittleDIP(); };
            }
            else if (string.Equals(str_AttachedObjectName.Substring(1, 2), "Th") || string.Equals(str_AttachedObjectName.Substring(1, 3), "Pla"))
            {
                //THUMB
                m_valAA = delegate() { return VulcanXInterface.RightThumbAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightThumbFE(); };
                m_valPIP = delegate() { return VulcanXInterface.RightThumbMCP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightThumbDIP(); };
            }//if - check for fingers and thumbs, if non, then 
            else
            {
                //Proximal to finger/thumb (palm, wrist, upper arm)
                //This situation won't require/calculate any values, so any assignment is ok - index finger default
                m_valAA = delegate() { return VulcanXInterface.RightIndexAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightIndexMCP(); };
                m_valPIP = delegate() { return VulcanXInterface.RightIndexPIP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightIndexDIP(); };
            }//if - check for fingers and thumbs, if non, then 

            #endregion //Right vMPL Finger/Thumb Assignments

            #region Commented Out
            //Will pull all segment angle values of which script is attached
            switch (go_AttachedObject.name)
            {
                //Thumb FTSN
                //case "rThDistal":
                case "rThDistalFTSN":
                    m_valAA = delegate() { return VulcanXInterface.RightThumbAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.RightThumbFE(); };
                    m_valPIP = delegate() { return VulcanXInterface.RightThumbMCP(); };
                    m_valDIP = delegate() { return VulcanXInterface.RightThumbDIP(); };
                    break;

                //Index Finger FTSN
                //case "rIndDistal":
                case "rIndDistalFTSN":
                    m_valAA = delegate() { return VulcanXInterface.RightIndexAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.RightIndexMCP(); };
                    m_valPIP = delegate() { return VulcanXInterface.RightIndexPIP(); };
                    m_valDIP = delegate() { return VulcanXInterface.RightIndexDIP(); };
                    break;

                //Middle Finger FTSN
                //case "rMidDistal":
                case "rMidDistalFTSN":
                    m_valAA = delegate() { return VulcanXInterface.RightMiddleAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.RightMiddleMCP(); };
                    m_valPIP = delegate() { return VulcanXInterface.RightMiddlePIP(); };
                    m_valDIP = delegate() { return VulcanXInterface.RightMiddleDIP(); };
                    break;
            }
            #endregion //Commented Out

        }//if - check for left/right arm
        else
        {
            //Debug.Log("This: " + this.name + ", attached: " + go_AttachedObject.name);
               
            
            if (string.Equals(go_AttachedObject.name.Substring(0, 1), "l"))
            {
                //LEFT vMPL
                //Debug.Log("This: " + this.name + ", attached: " + go_AttachedObject.name);
                
                #region Left vMPL Finger/Thumb Assignments
                if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Ind"))
                {
                    //INDEX FINGER
                    m_valAA = delegate() { return VulcanXInterface.LeftIndexAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.LeftIndexMCP(); };
                    m_valPIP = delegate() { return VulcanXInterface.LeftIndexPIP(); };
                    m_valDIP = delegate() { return VulcanXInterface.LeftIndexDIP(); };
                }
                else if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Mid"))
                {
                    //MIDDLE FINGER
                    m_valAA = delegate() { return VulcanXInterface.LeftMiddleAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.LeftMiddleMCP(); };
                    m_valPIP = delegate() { return VulcanXInterface.LeftMiddlePIP(); };
                    m_valDIP = delegate() { return VulcanXInterface.LeftMiddleDIP(); };
                }
                else if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Rin"))
                {
                    //RING FINGER
                    m_valAA = delegate() { return VulcanXInterface.LeftRingAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.LeftRingMCP(); };
                    m_valPIP = delegate() { return VulcanXInterface.LeftRingPIP(); };
                    m_valDIP = delegate() { return VulcanXInterface.LeftRingDIP(); };
                }
                else if (string.Equals(str_AttachedObjectName.Substring(1, 3), "Lit"))
                {
                    //LITTLE FINGER
                    m_valAA = delegate() { return VulcanXInterface.LeftLittleAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.LeftLittleMCP(); };
                    m_valPIP = delegate() { return VulcanXInterface.LeftLittlePIP(); };
                    m_valDIP = delegate() { return VulcanXInterface.LeftLittleDIP(); };
                }
                else if (string.Equals(str_AttachedObjectName.Substring(1, 2), "Th") || string.Equals(str_AttachedObjectName.Substring(1, 3), "Pla"))
                {
                    //THUMB
                    m_valAA = delegate() { return VulcanXInterface.LeftThumbAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.LeftThumbFE(); };
                    m_valPIP = delegate() { return VulcanXInterface.LeftThumbMCP(); };
                    m_valDIP = delegate() { return VulcanXInterface.LeftThumbDIP(); };
                }//if - check for fingers and thumbs, if non, then 
                else
                {
                    //Proximal to finger/thumb (palm, wrist, upper arm)
                    //This situation won't require/calculate any values, so any assignment is ok - index finger default
                    m_valAA = delegate() { return VulcanXInterface.LeftIndexAA(); };
                    m_valMCP = delegate() { return VulcanXInterface.LeftIndexMCP(); };
                    m_valPIP = delegate() { return VulcanXInterface.LeftIndexPIP(); };
                    m_valDIP = delegate() { return VulcanXInterface.LeftIndexDIP(); };
                }//if - check for fingers and thumbs, if non, then 

                #endregion //Left vMPL Finger/Thumb Assignments


            }//if - check for left/right arm
        
        }//if - check for right

    }//function - MapDelegates

    #endregion //Finger Segment Assignments (Sensorized)

    #endregion //COMPONENT ASSIGNMENTS

   
    //---------------------------------------
    // FUNCTIONS - UNITY3D (awake, start, reset, Update, FixedUpdate)
    //---------------------------------------
    #region Unity methods

        /// <summary>
    /// Script initialization.  Static members are initialized, so 
    /// ms_initialized used to ensure they're initialized only once.
    /// </summary>
    void Awake()
    {

        //Set / Reset Name (could be changed dynamically based on object the sensor is attached to)
        m_name = this.name;
        //Debug.Log("Resetting Sensor Name: " + name + "," + this.name + " - " + m_name);


        //Set the World Interface (WIF) Script and ID
//        SetWIFID();  //Too many sensors/objects for WIF data stream UDP limit (~1400 bytes)
        

    }//function - Awake


    /// <summary>
    /// Is called at the beginning of program, will instantial objects
    /// </summary>
    void Start()
    {
        //INITIALIZATION
        #region Initilization
        //Pulls the name from the attached object ("rIndDistal", for example)

        
        //Initize Struct for collision information
//        m_validContacts = new Dictionary<Collider, string>();

        //Force Values
        #region Force Values
        //Measured values (forces)
        m_forceMagnitude = 0.0F; //Magnitude of Force
        m_contactForce = Vector3.zero; //Direction and Magnitude of Force
        m_contact = false; //Boolean of whether in contact/collision
        #endregion //Force Values

        #endregion //Initilization


        //OBJECT ASSIGNMENT
        #region Object Assignment

        //Find location where sensor is attached to vMPL (also sets m_name - call before other initilization functions)
        GetSensorLocation(); // Called before MapDelegates as this finds the location which dictates the delegations


        //Set the Fixed joint between the FTSN and vMPL (in case initial setting incorrect)
//        SetFTSNFixedJointTovMPL();


        //Set Physics Parameters for the sensors (that have rigid bodies due to fixed joint with finger)
        SetRigidBodyParameters();

        //Assign local pointeres to hinge joints of specified vMPL components
        //MapHingeJoints();

        //Assign local instances of vMPL components/values in VulcanXInterface
        MapDelegates();

        
        #endregion //Object Assignment

        
    }//function - Start


    /// <summary>
    /// Called once before each rendered frame.
    /// </summary>
    void Update()
    {

        #region Commented Out
        //Debug.DrawRay(this.gameObject.transform.TransformPoint(0, 0, 0), this.gameObject.transform.forward * 0.5f, Color.blue, Time.deltaTime, false); //PRESSURE
        //Debug.DrawRay(this.gameObject.transform.TransformPoint(0, 0, 0), this.gameObject.transform.up * 0.5f, Color.green, Time.deltaTime, false); //AXIAL
        //Debug.DrawRay(this.gameObject.transform.TransformPoint(0, 0, 0), this.gameObject.transform.right * 0.5f, Color.red, Time.deltaTime, false); //SHEAR
        #endregion //Commented Out


        if (b_MoveableSensorsEnabled)//not supported for FTSN14
        {
            //CHECK PARENT OBJECT - ENSURE SAME (check for sensor movement from WIF command)
            #region Parent Object Check

            //Two methods used - checking the parent assignment (current versus originally set), and checking Fixed Joint comparison with current parent
            if (this.GetComponent<FixedJoint>() != null)
            {
                if (!string.Equals(go_ThisObject.transform.parent.gameObject.name, this.GetComponent<FixedJoint>().connectedBody.name))
                {

                    //FIXED JOINT CHANGED - reassign parent

                    //Set the parent to the connected body
                    //                SetFTSNParentToFixedJointAttachment();

                    //SetFTSNLocation(new Vector3(0, 0, 0) + this.GetComponent<FixedJoint>().connectedBody.transform.position); //should be respective to new parent

                    //Update the Game Objects/names, If Left/Right limb, Hinge Values, and location on limb
                    //                GetSensorLocation();

                }//if - check to see if connected body (through Fixed Joint) and parent are the same
            }//if - check to see if fixed joint temporarily removed for reassignment
            //Check to see if the current parent object has changes (from user moving the Sensor to another location on the limb using the WIF)
            else if (go_AttachedObject != null && go_ThisObject.transform.parent.gameObject != null)
            {

                if (string.Equals(go_AttachedObject.name, go_ThisObject.transform.parent.gameObject.name))
                {
                }
                else
                {
                    //PARENT CHANGED - reassign fixed Joint - values for proper pointers used to grab force/vector data

                    //Update the Game Objects/names, If Left/Right limb, Hinge Values, and location on limb
                    GetSensorLocation();

                    //Update the fixed Joint between the FTSN and vMPL
                    SetFTSNFixedJointTovMPL();

                }//if - check to see if the parent object has changed due to the user moving the Sensor using the WIF

            }//test for null objects - possible with detactment of fixed joint from sensor object from parent

            #endregion //Parent Object Check

        }//if - check for moveable sensors

    }//function - Update


    //void FixedUpdate()
    //{
        //var currentVelocity = rigidbody.velocity;

        //if (currentVelocity.y <= 0f)
            //return;

        //currentVelocity.y = 0f;

        //rigidbody.velocity = new Vector3(0f, 0f, 0f);// currentVelocity;

    //}//function - FixedUpdate


    /// <summary>
    /// Called before each physics time step.  
    /// </summary>
    void FixedUpdate()
    {

        //rigidbody.velocity = new Vector3(0f, 0f, 0f);// currentVelocity;

        m_forceMagnitude = 0.0F; //reset force magnitude value

        
        //---------------------------------------
        // FORCE MAGNITUDE CALCULATION
        //---------------------------------------
        #region Force Magnitude Calculation
        lock (m_valLock)
        {
            #region Collect Joint Angle Values (Actual, Commanded)
            #region Commented Out
            /*
            //save actual joint values
            m_actVal[0] = m_shFE.angle;
            m_actVal[1] = m_shAA.angle;
            m_actVal[2] = m_humRot.angle;
            m_actVal[3] = m_elbFE.angle;
            m_actVal[4] = m_wrRot.angle;
            m_actVal[5] = m_wrDev.angle;
            m_actVal[6] = m_wrFE.angle;
            m_actVal[7] = m_finAA.angle;
            m_actVal[8] = m_mcpFE.angle;
            m_actVal[9] = m_proxFE.angle;
            m_actVal[10] = m_medFE.angle;

            
            //save commanded joint values
            m_cmdVal[0] = VulcanXInterface.RightShoulderFE();
            m_cmdVal[1] = VulcanXInterface.RightShoulderAA();
            m_cmdVal[2] = VulcanXInterface.RightHumeralRot();
            m_cmdVal[3] = VulcanXInterface.RightElbowFE();
            m_cmdVal[4] = VulcanXInterface.RightWristRot();
            m_cmdVal[5] = VulcanXInterface.RightWristDev();
            m_cmdVal[6] = VulcanXInterface.RightWristFE();
            m_cmdVal[7] = m_valAA();  //get all segments on finger of the finger segment this script is attached to
            m_cmdVal[8] = m_valMCP();
            m_cmdVal[9] = m_valPIP();
            m_cmdVal[10] = m_valDIP();
            */
            #endregion //Commented Out

            //save commanded joint values - built tree in reverse order
            m_cmdVal[10] = VulcanXInterface.RightShoulderFE();
            m_cmdVal[9] = VulcanXInterface.RightShoulderAA();
            m_cmdVal[8] = VulcanXInterface.RightHumeralRot();
            m_cmdVal[7] = VulcanXInterface.RightElbowFE();
            m_cmdVal[6] = VulcanXInterface.RightWristRot();
            m_cmdVal[5] = VulcanXInterface.RightWristDev();
            m_cmdVal[4] = VulcanXInterface.RightWristFE();
            m_cmdVal[3] = m_valAA();  //get all segments on finger of the finger segment this script is attached to
            m_cmdVal[2] = m_valMCP();
            m_cmdVal[1] = m_valPIP();
            m_cmdVal[0] = m_valDIP();
            #endregion //Collect Joint Angle Values (Actual, Commanded)

        }//lock

        //estimate force by summing the differences between command joint values and actual values
        
        #region Commented Out
        /*
        //Set the specific 'level' where forces are calculated by where the sensor exists in the arm (if repositioned) - 10-6-13
        //for (int i = 7; i <= 10; i++)
        {
            m_forceMagnitude += Mathf.Abs(m_actVal[i] - m_cmdVal[i]); //Calculate the Force value

        }//for - traverse specific finger values
        */
        #endregion //Commented Out
        

        //Set the specific 'level' where forces are calculated by where the sensor exists in the arm (if repositioned) - 10-6-13
        for (int ii = i_SensorLocationTorqueMeasureBegin; ii <= i_SensorLocationTorqueMeasureEnd; ii++)
        {
            //m_forceMagnitude += Mathf.Abs(m_actVal[10-i] - m_cmdVal[10-i]); //Calculate the Force value
            //m_forceMagnitude += Mathf.Abs(list_UnilateralHinges[i].angle - m_cmdVal[i]); //Calculate the Force value

            //Assumes default 11 values total in limb - will determine comparison based on location on limb (number of comparisons, which joints to compare)
            m_forceMagnitude += Mathf.Abs(list_UnilateralHinges[ii].angle - m_cmdVal[ii + (11-i_SensorLocation)]); //Calculate the Force value - modular with moving location on hand
            
            
        }//for - traverse specific finger values

        //MULTIPLIER
        m_forceMagnitude *= f_ForceMagnitudeMulitplierScalar;

        if (m_forceMagnitude > 0.1)
        {
            //Debug.Log("Force detected: " + m_forceMagnitude.ToString());
        }

        #region Commented Out
        //Debug.Log("Force Differences: (" + i_SensorLocationTorqueMeasureBegin + ", " + i_SensorLocationTorqueMeasureEnd + " - " + (10 - i_SensorLocationTorqueMeasureEnd) + ", " + (10 - i_SensorLocationTorqueMeasureBegin) + ") - " + m_forceMagnitude + ", " + m_forceMagnitude2 + " (" + (m_forceMagnitude - m_forceMagnitude2) + ")  ...." + list_UnilateralHinges[0].name);
        //Debug.Log(this.name + " - Measured Angles: (" + m_finAA.name + ", " + m_mcpFE.name + ", " + m_proxFE.name + ", " + m_medFE.name + ") - (" + list_UnilateralHinges[0].name + ", " + list_UnilateralHinges[1].name + ", " + list_UnilateralHinges[2].name + ", " + list_UnilateralHinges[3].name + ")");
        //Debug.Log("Force Differences: (" + i_SensorLocationTorqueMeasureBegin + ", " + i_SensorLocationTorqueMeasureEnd + " - " + (10 - i_SensorLocationTorqueMeasureEnd) + ", " + (10 - i_SensorLocationTorqueMeasureBegin) + ") - " + m_forceMagnitude + ")  ...." + list_UnilateralHinges[0].name);
        //Debug.Log(this.name + " - Measured Angles: (" + list_UnilateralHinges[0].name + ", " + list_UnilateralHinges[1].name + ", " + list_UnilateralHinges[2].name + ", " + list_UnilateralHinges[3].name + ")");

        //scale it for real world
        //m_forceMagnitude = Mathf.Max(m_forceMagnitude, 100.0F);
        //m_forceMagnitude *= m_scaleNewton;
        #endregion //Commented Out

        #endregion //Force Magnitude Calculation


    }//function - FixedUpdate

    #endregion //Unity methods


    //---------------------------------------
    // FUNCTIONS - COLLISION DETECTION 
    //---------------------------------------
    #region Collision Methods
    /*
    /// <summary>
    ///  Function that is called when collision is detected
    /// </summary>
    void OnCollisionEnter(Collision colInfo)
    {
        //Debug.Log("Collision Enter");

        //Set the last collision event info (this event handling)
//        m_lastCollision = colInfo;

        //Debug.Log("Colliders: (" + name + "," + this.name + ", " + m_name + ")");

        //REMOVE WARNING ONLY
//        m_contact = m_lastCollision.collider.enabled;
        //m_contact = colInfo.collider.enabled;
        //REMOVE WARNING ONLY
        
        //Set flag for collision
        m_contact = true;

    }//function - OnCollisionEnter


    /// <summary>
    /// OnCollisionStay is called once per frame for every collider/rigidbody that is touching rigidbody/collider.
    /// </summary>
    void OnCollisionStay(Collision colInfo)
    {
        
        //Set collision/contact flag to true
        m_contact = true;

        //Summate all forces for total focrce calculation
        Vector3 v3_ForceSum = new Vector3(0, 0, 0);

        //Consider each collision on object that script is attached to
        foreach (ContactPoint p in colInfo.contacts)
        {
            //Assume Single Collision Point to Measure
            //if (p.thisCollider.name == (m_name.Insert(m_name.Length, "FTSN"))) //IF SCRIPT IS ON FINGER

            //Debug.Log("Colliders: " + p.thisCollider.name + ", " + m_name);

            if (string.Equals(p.thisCollider.name, m_name)) //IF SCRIPT IS ON INDV OBJECT
            //if (p.thisCollider.name == m_name) //IF SCRIPT IS ON INDV OBJECT
            {
                //m_normal = colInfo.contacts[0].normal;
                m_normal = p.normal; //Grabs the normal for the object being collided - if this is flat, then normal will always be in same direction no matter what point of contact on the finger/thumb tip
                m_relvel = colInfo.relativeVelocity;

                //CALCULATE FORCE VECTORS
                #region Calculated Force Vectors

                //FTSN Original - Calculate SUM OF Vector directions and magnitude based on Dot product with collider vector axes
                //v3_ForceSum.x += Vector3.Dot(m_normal, p.thisCollider.transform.up) * m_forceMagnitude; //AXIAL //GREEN
                //v3_ForceSum.y += Vector3.Dot(m_normal, p.thisCollider.transform.right) * m_forceMagnitude; //SHEAR //RED
                //v3_ForceSum.z += Vector3.Dot(m_normal, p.thisCollider.transform.forward) * m_forceMagnitude; //PRESSURE  //PRESSURE

                //FTSN14 - With implementation of the FTSN14 and laying out of new collider elements, these directions have changed:
                v3_ForceSum.x += Vector3.Dot(m_normal, p.thisCollider.transform.up) * m_forceMagnitude; //AXIAL from sensor normal //GREEN
                v3_ForceSum.y += Vector3.Dot(m_normal, p.thisCollider.transform.right) * m_forceMagnitude; //PRESSURE from sensor normal //RED
                v3_ForceSum.z += Vector3.Dot(m_normal, p.thisCollider.transform.forward) * m_forceMagnitude; //SHEAR from sensor normal//BLUE

                #endregion //Calculated Force Vectors

            }//if - check for specific collider


        }//foreach - traverse each collide object

        //FORCES SUMMATION
        #region Summed Forces
        //Assign the summed forces
        m_contactForce.x = v3_ForceSum.x;
        m_contactForce.y = -v3_ForceSum.y; //Colliders are flipped, switch to negative
        m_contactForce.z = v3_ForceSum.z;
        #endregion //Summed Forces

        //FTSN14 Reported Force
        SetReportedForce(-v3_ForceSum.y);

        //DRAW FORCE VECTORS
        #region Visual Depiction of Force (Magnitude/Direction)
        //Draw the Magnitude and Direction of the Collision Force

        //FTSN Original
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.up * m_contactForce.x, Color.green, .5f, false); //AXIAL
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * m_contactForce.y, Color.red, .5f, false); //SHEAR
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.forward * m_contactForce.z, Color.blue, .5f, false); //PRESSURE

        //FTSN14
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.up * m_contactForce.x, Color.green, .25f, false); //AXIAL from sensor normal //GREEN
        Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * GetReportedForce(), Color.red, .25f, false); //PRESSURE from sensor normal //RED
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.forward * m_contactForce.z, Color.blue, .25f, false); //SHEAR from sensor normal//BLUE

        if (GetReportedForce() != 0)
        {
            //Debug.Log("Reported Force: " + f_ReportedForce.ToString());
        }

        #endregion //Visual Depiction of Force (Magnitude/Direction)

    }//function - OnCollisionStay


    /// <summary>
    /// OnCollisionExit is called when this collider/rigidbody has stopped touching another rigidbody/collider.
    /// </summary>
    void OnCollisionExit(Collision colInfo)
    {
        //Currently supports single normal/magnitude value
        //These clears happen as if there are no other contact points (these functions only consider single contact as this point)
        //Debug.Log("Collision Exit: " + this.transform.parent.name);

        m_normal = new Vector3();
        m_relvel = new Vector3();
        m_contact = false; 
//        m_validContacts.Remove(colInfo.collider);
        m_contactForce = Vector3.zero;

        SetReportedForce(0f);

    }//function - OnCollisionExit
    */
    #endregion //Collision Methods


    #region Trigger Methods

    /// <summary>
    ///  Function that is called when collision is detected
    /// </summary>
    void OnTriggerEnter(Collider col_Collider)
    {
        
        //Set flag for collision
        m_contact = true;

    }//function - OnTriggerEnter


    /// <summary>
    /// Is called once per frame for every collider/rigidbody that is touching rigidbody/collider.
    /// </summary>
    void OnTriggerStay(Collider col_Collider)
    {
        //Set collision/contact flag to true
        m_contact = true;

        //Summate all forces for total focrce calculation
        Vector3 v3_ForceSum = new Vector3(0, 0, 0);

        //Consider each collision on object that script is attached to
        //foreach (ContactPoint p in colInfo.contacts)
        //{
            //Assume Single Collision Point to Measure
            //if (p.thisCollider.name == (m_name.Insert(m_name.Length, "FTSN"))) //IF SCRIPT IS ON FINGER

            //Debug.Log("Colliders: " + p.thisCollider.name + ", " + m_name);

            //if (string.Equals(p.thisCollider.name, m_name)) //IF SCRIPT IS ON INDV OBJECT
//            if (string.Equals(col_Collider.name, m_name)) //IF SCRIPT IS ON INDV OBJECT
            //{
                //m_normal = colInfo.contacts[0].normal;
                //m_normal = col_Collider.transform.gameObject.collider..eulerAngles;

                //Determine location of collision
                //m_normal = (this.gameObject.collider.ClosestPointOnBounds(col_Collider.transform.position)); //Location of Collision

                //Vector3 v3_closestPoint = this.gameObject.collider.ClosestPointOnBounds(col_Collider.bounds.center);
                Vector3 v3_closestPoint = col_Collider.ClosestPointOnBounds(this.gameObject.GetComponent<Collider>().bounds.center);
                f_CollisionDistance = Vector3.Distance(v3_closestPoint, this.gameObject.GetComponent<Collider>().bounds.center);
                f_CollisionDistance = (this.gameObject.GetComponent<Collider>().bounds.extents.magnitude - f_CollisionDistance) / (this.gameObject.GetComponent<Collider>().bounds.extents.magnitude);
                if (f_CollisionDistance < 0)
                {
                    f_CollisionDistance = f_CollisionDistance * -1;
                }//check for distance > extents


                //Create heading from position of collision to collider center
                m_normal = (this.gameObject.GetComponent<Collider>().ClosestPointOnBounds(col_Collider.bounds.center)); //Location of Collision
                m_normal = (m_normal - (this.transform.position));//.normalized;
                
                //m_ColliderNormal = (col_Collider.ClosestPointOnBounds(this.gameObject.collider.bounds.center)); //Location of Collision
                m_ColliderNormal = (this.gameObject.GetComponent<Collider>().ClosestPointOnBounds(col_Collider.bounds.center)); //Location of Collision
                m_ColliderNormal = (m_ColliderNormal - (this.transform.position)).normalized;
                if (m_ColliderNormal.magnitude > 0) //check if not normal to collider (reduce magnitude of force if so)
                {
                    //Collider is not normal to collision, make dot product to diminish magnitude
                    m_forceMagnitude = m_forceMagnitude * Vector3.Dot(m_ColliderNormal, this.transform.right);

                }//if - check to see if collision not normal to plane of collider

                //this.gameObject.collider.bounds.extents.magnitude;
                //col_Collider.bounds.
                
                //f_CollisionDistance = Vector3.Distance(this.transform.position, m_normal);
                //f_CollisionDistance = 100 * col_Collider.bounds.SqrDistance(this.transform.position);//this.gameObject.collider.ClosestPointOnBounds(col_Collider.transform.position));
                //f_CollisionDistance = 100 * this.gameObject.collider.bounds.SqrDistance(this.gameObject.collider.ClosestPointOnBounds(col_Collider.transform.position));
                //f_CollisionDistance = 1 * this.gameObject.collider.bounds.SqrDistance(col_Collider.transform.position);
                //f_CollisionDistance = 1 * this.gameObject.collider.bounds.SqrDistance(col_Collider.transform.position);
//                f_CollisionDistance = 100 * col_Collider.bounds.SqrDistance(this.collider.bounds.center);
                //f_CollisionDistance = col_Collider.bounds.SqrDistance(this.collider.bounds.center);
                //Debug.Log("Dist/ext: " + f_CollisionDistance.ToString() + ", " + (this.gameObject.collider.bounds.extents.magnitude / 2).ToString() + " ," + (((this.gameObject.collider.bounds.extents.magnitude / 2f) - f_CollisionDistance) / (this.gameObject.collider.bounds.extents.magnitude * 2)).ToString());

                //f_CollisionDistance = ((this.gameObject.collider.bounds.extents.magnitude / 2f) - f_CollisionDistance) / (this.gameObject.collider.bounds.extents.magnitude / 2f);
                //f_CollisionDistance = ((this.gameObject.collider.bounds.extents.magnitude) - f_CollisionDistance) / (this.gameObject.collider.bounds.extents.magnitude);
                
        
                //if (f_CollisionDistance > 0) { Debug.Log("Squared Distance: " + f_CollisionDistance.ToString()); }

                
                //f_CollisionDistance = Vector3.Dot(m_normal, m_ColliderNormal) * f_CollisionDistance; //AXIAL from sensor normal //GREEN
                //float f_CollisionDistanceDot = Vector3.Dot(this.collider.transform.right.normalized, m_ColliderNormal) * f_CollisionDistance; //AXIAL from sensor normal //GREEN
                

                
                //m_normal = col_Collider.bounds.IntersectRay(this.gameObject.collider.ClosestPointOnBounds(col_Collider.transform.position));
                //                m_normal = p.normal; //Grabs the normal for the object being collided - if this is flat, then normal will always be in same direction no matter what point of contact on the finger/thumb tip
//                m_relvel = colInfo.relativeVelocity;

                //CALCULATE FORCE VECTORS
                #region Calculated Force Vectors

                //FTSN Original - Calculate SUM OF Vector directions and magnitude based on Dot product with collider vector axes
                //v3_ForceSum.x += Vector3.Dot(m_normal, p.thisCollider.transform.up) * m_forceMagnitude; //AXIAL //GREEN
                //v3_ForceSum.y += Vector3.Dot(m_normal, p.thisCollider.transform.right) * m_forceMagnitude; //SHEAR //RED
                //v3_ForceSum.z += Vector3.Dot(m_normal, p.thisCollider.transform.forward) * m_forceMagnitude; //PRESSURE  //PRESSURE

                //FTSN14 - With implementation of the FTSN14 and laying out of new collider elements, these directions have changed:
                v3_ForceSum.x += Vector3.Dot(m_normal, col_Collider.transform.up) * m_forceMagnitude; //AXIAL from sensor normal //GREEN
                v3_ForceSum.y += Vector3.Dot(m_normal, col_Collider.transform.right) * m_forceMagnitude; //PRESSURE from sensor normal //RED
                v3_ForceSum.z += Vector3.Dot(m_normal, col_Collider.transform.forward) * m_forceMagnitude; //SHEAR from sensor normal//BLUE

                #endregion //Calculated Force Vectors

            //}//if - check for specific collider


        //}//foreach - traverse each collide object

        //FORCES SUMMATION
        #region Summed Forces
        //Assign the summed forces
        m_contactForce.x = v3_ForceSum.x;
        m_contactForce.y = -v3_ForceSum.y; //Colliders are flipped, switch to negative
        m_contactForce.z = v3_ForceSum.z;
        #endregion //Summed Forces

        //FTSN14 Reported Force
        //SetReportedForce(-v3_ForceSum.y);
        //SetReportedForce(Mathf.Abs(v3_ForceSum.y));
        SetReportedForce(Mathf.Abs(v3_ForceSum.x));

        //DRAW FORCE VECTORS
        #region Visual Depiction of Force (Magnitude/Direction)
        //Draw the Magnitude and Direction of the Collision Force

        #region Commented Out
        //FTSN Original
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.up * m_contactForce.x, Color.green, .5f, false); //AXIAL
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * m_contactForce.y, Color.red, .5f, false); //SHEAR
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.forward * m_contactForce.z, Color.blue, .5f, false); //PRESSURE

        //FTSN14 Collision Axis Testing
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.up * GetReportedForce(), Color.green, .5f, false); //AXIAL
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * 10f, Color.red, .5f, false); //SHEAR
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.forward * GetReportedForce(), Color.blue, .5f, false); //PRESSURE

        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), col_Collider.transform.up * 10f, Color.yellow, .5f, false); //AXIAL
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), col_Collider.transform.right * 10f, Color.white, .5f, false); //SHEAR
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), col_Collider.transform.forward * 10f, Color.grey, .5f, false); //PRESSURE

        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.up * m_contactForce.x, Color.green, .25f, false); //AXIAL from sensor normal //GREEN
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.forward * m_contactForce.z, Color.blue, .25f, false); //SHEAR from sensor normal//BLUE


        //Check for Measured Force
        //if ((GetReportedForce() != 0) || (m_forceMagnitude != 0))
        //{
        //    //Debug.Log("Reported Force (" + this.collider.name + ", " + col_Collider.name + "): " + f_ReportedForce.ToString() + " (mag: " + m_forceMagnitude.ToString() + " - [" + v3_ForceSum.x + "," + v3_ForceSum.y + "," + v3_ForceSum.z + "] )");
        //}

        #endregion //Commented Out

        //COLLISION NORMAL - Show the Normal from the collision point to center of collider
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), m_normal * 5f, Color.yellow, .5f, false); //Direction from point of collision to center of colliding object
        
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), m_normal * f_CollisionDistance, Color.yellow, .5f, false); //Direction from point of collision to center of colliding object
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * Vector3.Magnitude(m_normal * f_CollisionDistance), Color.yellow, .5f, false); //Direction from point of collision to center of colliding object
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), m_ColliderNormal * f_CollisionDistance, Color.gray, .5f, false); //Direction from point of collision to center of colliding object
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * f_CollisionDistance, Color.white, .5f, false); //Direction from point of collision to center of colliding object
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * Vector3.Dot(this.collider.transform.right * f_CollisionDistance, m_ColliderNormal * f_CollisionDistance), Color.white, .5f, false); //Direction from point of collision to center of colliding object
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.forward * f_CollisionDistanceDot, Color.blue, .5f, false); //Direction from point of collision to center of colliding object
        
        //FTSN14 Force
        Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.GetComponent<Collider>().transform.right * GetReportedForce(), Color.red, .25f, false); //PRESSURE from sensor normal //RED
        //Debug.DrawRay(transform.TransformPoint(0, 0, 0), this.collider.transform.right * this.gameObject.collider.bounds.extents.magnitude, Color.white, 0.25f, false);

        #endregion //Visual Depiction of Force (Magnitude/Direction)

    }//function - OnTriggerStay


    /// <summary>
    /// Is called when this collider/rigidbody has stopped touching another rigidbody/collider.
    /// </summary>
    void OnTriggerExit(Collider colInfo)
    {
        //Currently supports single normal/magnitude value
        //These clears happen as if there are no other contact points (these functions only consider single contact as this point)
        //Debug.Log("Collision Exit: " + this.transform.parent.name);

        m_normal = new Vector3();
        m_relvel = new Vector3();
        m_contact = false;
        //        m_validContacts.Remove(colInfo.collider);
        m_contactForce = Vector3.zero;

        SetReportedForce(0f);

    }//function - OnTriggerExit

    #endregion //Trigger Methods
    


    private void SetReportedForce(float f_NewForce)
    {
        f_ReportedForce = f_NewForce;

    }//function - SetReportedForce

    private float GetReportedForce()
    {
        return f_ReportedForce;
    }//function - GetReportedForce


    //---------------------------------------
    // FUNCTIONS - COMMUNICATION
    //---------------------------------------
    #region Communication

    //Colliders
    #region Colliders Reporting

    /// <summary>
    /// Passes collision info of the "current" measured collision - will not take into account multiple collisions. 
    ///   Currently utilized by ForceLogger
    /// </summary>
    private float[] CollisionInfo
    {
        get
        {
            float[] a = new float[6];
            a[0] = m_normal.x;
            a[1] = m_normal.y;
            a[2] = m_normal.z;
            a[3] = m_relvel.x;
            a[4] = m_relvel.y;
            a[5] = m_relvel.z;
            return a;
        }
    }//function - CollisionInfo

    #endregion //Colliders Reporting


    //Force Reporting
    #region Force Reporting

    /// <summary>
    /// Passes force sensor information from "current" measured collision - will not take into account multiple collisions.
    ///   Passes Binary (contact on/off) information - 1/0
    ///   Currently utilized by SensorArray
    /// </summary>
    public Vector3 FTSNBinaryForce
    {
        get
        {
            if (!m_contact)
                return Vector3.zero;
            else
                return Vector3.one;
        }
    }//function - FTSNBinaryForce


    /// <summary>
    /// Passes force sensor information from "current" measured collision - will not take into account multiple collisions. 
    ///   Passes Continuous (force data) information - Vector 3 Floats
    ///   Currently utilized by SensorArray
    /// </summary>
    public Vector3 FTSNFullForce
    {
        get
        {
            if (!m_contact)
                return Vector3.zero;
            else
                return m_contactForce;
        }
    }//function - FTSNFullForce


    /// <summary>
    /// Passes FTSN14 force sensor information from "current" measured collision - will not take into account multiple collisions. 
    ///   Passes Continuous (force data) information - Vector 3 Floats
    ///   Currently utilized by SensorArray
    /// </summary>
    public float FTSN14Force
    {
        get
        {
            //if - check if in contact with object / collision
            if (!m_contact)
            {
                return 0f;
            }
            else
            {
                if (GetReportedForce() > 0)
                {
//                    Debug.Log("Force detected from last loop (" + this.transform.parent.name + "): " + GetReportedForce().ToString() + ", mag: " + m_forceMagnitude.ToString());
                }//if - check for force

                return GetReportedForce();
                //return m_forceMagnitude;
            }//if - check if in contact with object / collision
        }//get
    }//function - FTSN14Force

    #endregion //Force Reporting

    
    #endregion //Communication


}//class - FTSN14Sensor
