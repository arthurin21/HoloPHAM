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

public class SensorArray : MonoBehaviour {

    //---------------------------------------
    // VARIABLE DECLARATIONS
    //---------------------------------------
    #region Variable Declarations

    //MPL Properties
    #region MPL Properties
    //Contact Sensors - contact IDs as define in MUD
    public enum CONTACT_SENSOR_ID : int 
    {
        INDEX_INTERMEDIATE_CONTACT,     //S1
        INDEX_PROXIMAL_CONTACT,	        //S2
        MIDDLE_INTERMEDIATE_CONTACT,	//S3
        MIDDLE_PROXIMAL_CONTACT,	    //S4
        RING_INTERMEDIATE_CONTACT,	    //S5
        RING_PROXIMAL_CONTACT,	        //S6
        LITTLE_INTERMEDIATE_CONTACT,
        LITTLE_PROXIMAL_CONTACT,
        PALM_CONTACT1,	        //S7
        PALM_CONTACT2,	        //S8
        PALM_CONTACT3,	        //S9
        PALM_CONTACT4,	        //S10
        THUMB_DISTAL_CONTACT1,	//S11
        THUMB_DISTAL_CONTACT2,	//S11
        THUMB_DISTAL_CONTACT3,	//S11
        THUMB_DISTAL_CONTACT4,	//S11
        THUMB_DISTAL_CONTACT5,	//S11
        INDEX_DISTAL_CONTACT1,	//S12
        INDEX_DISTAL_CONTACT2,	//S12
        INDEX_DISTAL_CONTACT3,	//S12
        INDEX_DISTAL_CONTACT4,	//S12
        INDEX_DISTAL_CONTACT5,	//S12
        MIDDLE_DISTAL_CONTACT1,	//S13
        MIDDLE_DISTAL_CONTACT2,	//S13
        MIDDLE_DISTAL_CONTACT3,	//S13
        MIDDLE_DISTAL_CONTACT4,	//S13
        MIDDLE_DISTAL_CONTACT5,	//S13
        RING_DISTAL_CONTACT1,
        RING_DISTAL_CONTACT2,
        RING_DISTAL_CONTACT3,
        RING_DISTAL_CONTACT4,
        RING_DISTAL_CONTACT5,
        LITTLE_DISTAL_CONTACT1,
        LITTLE_DISTAL_CONTACT2,
        LITTLE_DISTAL_CONTACT3,
        LITTLE_DISTAL_CONTACT4,
        LITTLE_DISTAL_CONTACT5
    };

    //Force Sensors - FTSN IDs as define in MUD
    public enum SegmentPerceptFTSNIdType : int
    {
        INDEX_FTSN,
        MIDDLE_FTSN,
        RING_FTSN,
        LITTLE_FTSN,
        THUMB_FTSN
    };

    //Which Arm / Handedness
    public enum whichArm : int
    {
        RIGHT,
        LEFT
    };


    //SensorArray Location - Which Arm
    protected int i_WhichArm = (int)whichArm.RIGHT; //Right is default - can determine by looking at the layer of the object that the sensor is attached to
    protected GameObject go_ThisObject;
    //protected GameObject go_AttachedObject;
    protected LayerMask layer_ThisObject;
    
    #endregion //MPL Functions

    //MPL Sensors
    #region MPL Sensors
    private ContactSensor[] m_contactSensors;
    private short[] m_contacts;
    //private int NUMBER_FTSN_PADS = 14;
    
    //Old FTSN
    private ForceSensor[] m_forceSensors;
    private Vector3[] m_force;

    //New FTSN
    //private FTSN14Sensor[,] m_ftsn14Sensors;
    //private float[,] m_ftsn14Data;

    
    /// <summary>
    /// This is used only for the first prototype of sensory 
    /// percepts.  It will likely be removed in the next build.
    /// </summary>
    private bool m_binarySensorMode = false;

    //Public Data
    #region Public Pointers to Sensor Data
#if UNITY_EDITOR
    [field: System.NonSerialized]
    public bool m_thumbDistalContact;
    [field: System.NonSerialized]
    public bool m_indexDistalContact;
    [field: System.NonSerialized]
    public bool m_indexMedialContact;
    [field: System.NonSerialized]
    public bool m_indexProximalContact;
    [field: System.NonSerialized]
    public bool m_middleDistalContact;
    [field: System.NonSerialized]
    public bool m_middleMedialContact;
    [field: System.NonSerialized]
    public bool m_middleProximalContact;
    [field: System.NonSerialized]
    public bool m_ringMedialContact;
    [field: System.NonSerialized]
    public bool m_ringProximalContact;
    [field: System.NonSerialized]
    public bool m_palmContactS7;
    [field: System.NonSerialized]
    public bool m_palmContactS8;
    [field: System.NonSerialized]
    public bool m_palmContactS9;
    [field: System.NonSerialized]
    public bool m_palmContactS10;
#endif
    #endregion //Public Pointers to Sensor Data


    //Arrays for contact and force sensor values
    public short[] Contacts { get { return m_contacts; } }
    public Vector3[] Forces { get { return m_force; } }
    //public float[,] Forces { get { return m_ftsn14Data; } }

    #endregion //MPL Sensors

    //GUI Properties
    #region GUI Properties
    
    //Labels
    private GUIStyle m_labelStyle = null;

    #endregion //GUI Properties


    #endregion //Variable Declarations


    //---------------------------------------
    // FUNCTIONS - UNITY3D (awake, start, reset, Update, FixedUpdate)
    //---------------------------------------
    #region Unity methods

    /// <summary>
    /// Is called at the beginning of program, will instantial objects
    /// </summary>
    void Start()
    {
        //Which Arm (Left/Right)
        #region Which Arm
        //Determine which arm attached to first (works for left and right)
        GetSensorArrayLocation();

        #endregion //Which Arm


        //Contact Sensor Setup
        #region Contact Sensor Setup

        //Get number of contact sensors
        int len = Enum.GetValues(typeof(CONTACT_SENSOR_ID)).Length;
        m_contactSensors = new ContactSensor[len];  //Create local pointer array
        m_contacts = new short[len]; //Create local array for data
        AssignContactSensors();  //Populate pointer array

        #endregion //Contact Sensor Setup


        //Force Sensor Setup
        #region Force Sensor Setup

        //Get number of force sensors
        len = Enum.GetValues(typeof(SegmentPerceptFTSNIdType)).Length;
        m_forceSensors = new ForceSensor[len]; //Create local pointer array
        //m_ftsn14Sensors = new FTSN14Sensor[len,NUMBER_FTSN_PADS];

        m_force = new Vector3[len]; //Create local array for data
        //m_ftsn14Data = new float[len, NUMBER_FTSN_PADS]; //Create local array for data
        AssignForceSensors(); //Populate pointer array

        #endregion //Force Sensor Setup


    }//function - Start


    /// <summary>
    /// Called once before each rendered frame.
    /// </summary>
    void Update()
    {
        //Poll Data from Sensors
        #region Sensor Data

        //Collect data from each sensor (pointer->short)
        if (m_binarySensorMode)
        {
            //Will pass back a binary value that will be processed later

            //get contacts
            foreach (int indx in Enum.GetValues(typeof(CONTACT_SENSOR_ID)))
            {
                if (m_contactSensors[indx] != null)
                {
                    m_contacts[indx] = m_contactSensors[indx].BinaryValue;
                }
                else
                {
                    Debug.Log("Null Contact Sensor");
                }
            }//foreach - traverse all contact sensors

            //get forces
            foreach (int indx in Enum.GetValues(typeof(SegmentPerceptFTSNIdType)))
            {
                if (m_forceSensors[indx] != null)
                {
                    m_force[indx] = m_forceSensors[indx].FTSNBinaryForce;
                }

                //for (int ii = 0; ii < NUMBER_FTSN_PADS; ii++)
                //{

                //    if (m_ftsn14Sensors[fingerIndx, ii] != null)
                //    {
                //        m_ftsn14Data[fingerIndx, ii] = m_ftsn14Sensors[fingerIndx, ii].FTSN14Force;
                //    }//

                //}//for - traversing pads on FTSN
            }
        }
        else
        {
            //Will pass back a short value that will be processed later 
            
            //get contacts
            foreach (int indx in Enum.GetValues(typeof(CONTACT_SENSOR_ID)))
            {
                if (m_contactSensors[indx] != null)
                {
                    m_contacts[indx] = m_contactSensors[indx].FullValue;
                }
                else
                {
                    //Debug.Log("Null Contact Sensor: " + indx);
                }
            }

            //Debug.Log("Contact Sensor Data: (" + m_contacts[0] + ", " + m_contacts[1] + ", " + m_contacts[2] + ", " + m_contacts[3] + ", " + m_contacts[4] + ", " + m_contacts[5] + ", " + m_contacts[6] + ", " + m_contacts[7] + ", " + m_contacts[8] + ", " + m_contacts[9] + ", " + m_contacts[10] + ", " + m_contacts[11] + ")");

            //get forces
            foreach (int indx in Enum.GetValues(typeof(SegmentPerceptFTSNIdType)))
            {
                if (m_forceSensors[indx] != null)
                    m_force[indx] = m_forceSensors[indx].FTSNFullForce;

                //for (int ii = 0; ii < NUMBER_FTSN_PADS; ii++)
                //{

                //    if (m_ftsn14Sensors[fingerIndx, ii] != null)
                //    {
                //        m_ftsn14Data[fingerIndx, ii] = m_ftsn14Sensors[fingerIndx, ii].FTSN14Force;
                //    }//

                //}//for - traversing pads on FTSN
            }
        }//if - test for binary/full value mode

        #endregion //Sensor Data


        //Collect values from sensors (whether in contact)
        #region Sensor Values (contact)
#if UNITY_EDITOR
        
        //Force Sensors
        m_thumbDistalContact = false;// m_forceSensors[(int)SegmentPerceptFTSNIdType.THUMB_FTSN].InContact;
        m_indexDistalContact = false;// m_forceSensors[(int)SegmentPerceptFTSNIdType.INDEX_FTSN].InContact;
        m_middleDistalContact = false;// m_forceSensors[(int)SegmentPerceptFTSNIdType.MIDDLE_FTSN].InContact;

        //Contact Sensors
        m_indexProximalContact = m_contacts[(int)CONTACT_SENSOR_ID.INDEX_PROXIMAL_CONTACT] > 0 ? true : false;
        m_indexMedialContact = m_contacts[(int)CONTACT_SENSOR_ID.INDEX_INTERMEDIATE_CONTACT] > 0 ? true : false;
        m_middleProximalContact = m_contacts[(int)CONTACT_SENSOR_ID.MIDDLE_PROXIMAL_CONTACT] > 0 ? true : false;
        m_middleMedialContact = m_contacts[(int)CONTACT_SENSOR_ID.MIDDLE_INTERMEDIATE_CONTACT] > 0 ? true : false;
        m_ringProximalContact = m_contacts[(int)CONTACT_SENSOR_ID.RING_PROXIMAL_CONTACT] > 0 ? true : false;
        m_ringMedialContact = m_contacts[(int)CONTACT_SENSOR_ID.RING_INTERMEDIATE_CONTACT] > 0 ? true : false;

        m_palmContactS7 = m_contacts[(int)CONTACT_SENSOR_ID.PALM_CONTACT1] > 0 ? true : false;
        m_palmContactS8 = m_contacts[(int)CONTACT_SENSOR_ID.PALM_CONTACT2] > 0 ? true : false;
        m_palmContactS9 = m_contacts[(int)CONTACT_SENSOR_ID.PALM_CONTACT3] > 0 ? true : false;
        m_palmContactS10 = m_contacts[(int)CONTACT_SENSOR_ID.PALM_CONTACT4] > 0 ? true : false;
        
#endif
        #endregion Sensor Values (contact)


    }//function - Update


    #endregion //Unity methods


    //---------------------------------------
    // FUNCTIONS - INITIALIZATION
    //---------------------------------------
    #region Initialization Functions


    /// <summary>
    /// This function will determine where this sensor array is located (which arm)
    /// </summary>
    private void GetSensorArrayLocation()
    {
        //Will determine the object that this sensor array is attached to.

        //GAME OBJECT
        #region Game Object
        go_ThisObject = this.gameObject;
        
        #endregion //Game Object

        //Right or Left Arm
        #region Right or Left Arm
            
        if (go_ThisObject != null)
        {
            //RIGHT or LEFT Arm
            layer_ThisObject = go_ThisObject.layer;

            if (GameObject.Find("rPalm") != null)
            {
                if (go_ThisObject.layer == GameObject.Find("rPalm").layer)
                {
                    i_WhichArm = (int)whichArm.RIGHT;
                }
            }
            if (GameObject.Find("lPalm") != null)
            {
                //else if (String.Equals(go_AttachedObject.layer, "LeftArm"))
                if (go_ThisObject.layer == GameObject.Find("lPalm").layer)
                {
                    i_WhichArm = (int)whichArm.LEFT;

                }//if - check to see which limb the sensor is attached to
            }//if - check to see if right/left arm present in scenario    
            
 
        }//if - check to make sure attached object is not null
        else
        {
            Debug.Log("Sensor Array attached to invalid object");
        }//if - check to make sure attached object is not null

        #endregion //Right or Left Arm

    }//function - GetSensorArrayLocation


    //CONTACT SENSORS
    private void AssignContactSensors()
    {
        //Assumes that which arm variable is set/determined - the Sensor Array is attached
        //  to either the left or right limb

        //TODO - Consider a non hard-coded solution - 10/16/13

        if (i_WhichArm == (int)whichArm.LEFT)
        {
            #region Left Arm
            //Get access to the objects/scripts running on each contact sensor - will provide interface for pulling information

            m_contactSensors[(int)CONTACT_SENSOR_ID.INDEX_INTERMEDIATE_CONTACT] =
                (ContactSensor)(GameObject.Find("lContact_S1").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.INDEX_PROXIMAL_CONTACT] =
                (ContactSensor)(GameObject.Find("lContact_S2").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.MIDDLE_INTERMEDIATE_CONTACT] =
                (ContactSensor)(GameObject.Find("lContact_S3").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.MIDDLE_PROXIMAL_CONTACT] =
                (ContactSensor)(GameObject.Find("lContact_S4").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.RING_INTERMEDIATE_CONTACT] =
                (ContactSensor)(GameObject.Find("lContact_S5").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.RING_PROXIMAL_CONTACT] =
                (ContactSensor)(GameObject.Find("lContact_S6").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT1] =
                (ContactSensor)(GameObject.Find("lContact_S7").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT2] =
                (ContactSensor)(GameObject.Find("lContact_S8").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT3] =
                (ContactSensor)(GameObject.Find("lContact_S9").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT4] =
                (ContactSensor)(GameObject.Find("lContact_S10").GetComponent(typeof(ContactSensor)));
            #endregion //Left Arm

        }
        else if (i_WhichArm == (int)whichArm.RIGHT)
        {
            #region Right Arm
            //Get access to the objects/scripts running on each contact sensor - will provide interface for pulling information

            m_contactSensors[(int)CONTACT_SENSOR_ID.INDEX_INTERMEDIATE_CONTACT] =
                (ContactSensor)(GameObject.Find("rContact_S1").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.INDEX_PROXIMAL_CONTACT] =
                (ContactSensor)(GameObject.Find("rContact_S2").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.MIDDLE_INTERMEDIATE_CONTACT] =
                (ContactSensor)(GameObject.Find("rContact_S3").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.MIDDLE_PROXIMAL_CONTACT] =
                (ContactSensor)(GameObject.Find("rContact_S4").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.RING_INTERMEDIATE_CONTACT] =
                (ContactSensor)(GameObject.Find("rContact_S5").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.RING_PROXIMAL_CONTACT] =
                (ContactSensor)(GameObject.Find("rContact_S6").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT1] =
                (ContactSensor)(GameObject.Find("rContact_S7").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT2] =
                (ContactSensor)(GameObject.Find("rContact_S8").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT3] =
                (ContactSensor)(GameObject.Find("rContact_S9").GetComponent(typeof(ContactSensor)));

            m_contactSensors[(int)CONTACT_SENSOR_ID.PALM_CONTACT4] =
                (ContactSensor)(GameObject.Find("rContact_S10").GetComponent(typeof(ContactSensor)));

            #endregion //Right Arm

        }//if - test for which arm (left/right)

    }//function - AssignContactSensors


    //FORCE SENSORS
    private void AssignForceSensors()
    {
        //Get access to the objects/scripts running on each force sensor - will provide interface for pulling information

        //Assumes that which arm variable is set/determined - the Sensor Array is attached
        //  to either the left or right limb

        //TODO - Consider a non hard-coded solution - 10/16/13

        if (i_WhichArm == (int)whichArm.LEFT)
        {
            #region Left Arm
            
            m_forceSensors[(int)SegmentPerceptFTSNIdType.THUMB_FTSN] =
                    (ForceSensor)(GameObject.Find("lThDistalFTSN").GetComponent(typeof(ForceSensor)));
            m_forceSensors[(int)SegmentPerceptFTSNIdType.INDEX_FTSN] =
                    (ForceSensor)(GameObject.Find("lIndDistalFTSN").GetComponent(typeof(ForceSensor)));
            m_forceSensors[(int)SegmentPerceptFTSNIdType.MIDDLE_FTSN] =
                    (ForceSensor)(GameObject.Find("lMidDistalFTSN").GetComponent(typeof(ForceSensor)));
            
            #endregion //Left Arm

        }
        else if (i_WhichArm == (int)whichArm.RIGHT)
        {

            #region Right Arm
            
            m_forceSensors[(int)SegmentPerceptFTSNIdType.THUMB_FTSN] =
                    (ForceSensor)(GameObject.Find("rThDistalFTSN").GetComponent(typeof(ForceSensor)));
            m_forceSensors[(int)SegmentPerceptFTSNIdType.INDEX_FTSN] =
                    (ForceSensor)(GameObject.Find("rIndDistalFTSN").GetComponent(typeof(ForceSensor)));
            m_forceSensors[(int)SegmentPerceptFTSNIdType.MIDDLE_FTSN] =
                    (ForceSensor)(GameObject.Find("rMidDistalFTSN").GetComponent(typeof(ForceSensor)));
            
            #endregion //Right Arm

        }//if - check for which arm (left/right)

    }//function - AssignForceSensors


    #endregion //Initialization Functions


    //---------------------------------------
    // FUNCTIONS - GUI
    //---------------------------------------
    #region GUI Functions
    void SetLabelColor(bool contact)
    {
        if (contact)
            m_labelStyle.normal.textColor = Color.red;
        else
            m_labelStyle.normal.textColor = Color.white;
    }//function - SetLabelColor

    #endregion //GUI Functions


}//class - SensorArray
