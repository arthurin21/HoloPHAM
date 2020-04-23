//
// README: IMPORTANT WARNING - EXPORT CONTROL LANGUAGE
// 
// This information, software, technology being shared MUST be 
// handled in accordance with the statement below.  All documentation
// related to Software and Technology Development associated with 
// this shared information must include this statement:
//
// �The information we are providing contains proprietary software/
// technology and is therefore export controlled.   The specific 
// Export Control Classification Number (ECCN) applied to this 
// software, 3D991, is currently controlled to only 5 countries: 
// N. Korea, Syria, Sudan, Cuba, or Iran.  Before providing this 
// software or data to any foreign person, you should consult with 
// your organization�s export compliance or legal office.  Of course,
// the nature of our contractual relationship requires that only 
// people associated with Revolutionizing Prosthetics Phase 3 may 
// have access to this information.�
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.IO.IsolatedStorage;

public class FTSN14SensorArray : MonoBehaviour {

    //This script is attached to the Palm, and will handle sensor data collection for all 5 FTSN 
    // sensor arrays on each of the 5 fingers (each FTSN is composed of 14 sensor Pads)

    //---------------------------------------
    // VARIABLE DECLARATIONS
    //---------------------------------------
    #region Variable Declarations

    //MPL Properties
    #region MPL Properties

    #region Game Objects - MPL

    //Finger Distals
    #region Finger Distal GameObjects
    GameObject go_FingerDistalIndex;
    GameObject go_FingerDistalMiddle;
    GameObject go_FingerDistalRing;
    GameObject go_FingerDistalLittle;
    GameObject go_FingerDistalThumb;
    #endregion //Finger Distal GameObjects

    #endregion //Game Objects - MPL

    #region Enums for MPL Features, Objects

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

    #endregion //Enums for MPL Features, Objects

    //SensorArray Location - Which Arm
    protected int i_WhichArm = (int)whichArm.RIGHT; //Right is default - can determine by looking at the layer of the object that the sensor is attached to
    protected GameObject go_ThisObject;
    //protected GameObject go_AttachedObject;
    protected LayerMask layer_ThisObject;
    

    #endregion //MPL Functions

    //Contact and Force Sensors
    #region Contact and Force Sensors
    private ContactSensor[] m_contactSensors;
    private short[] m_contacts;
    private int NUMBER_FTSN_PADS = 14;

    private float f_ForceMaximum = 16f; //Newtons, reported to VulcanX (which passes it through)
    //Old FTSN
    //private ForceSensor[] m_forceSensors;
    //private Vector3[] m_force;

    //New FTSN
    private GameObject[] go_ftsn14SensorArrays; //Arrays (composed of 14 pads) - pointers to game object array includes all pad on one hand
    private FTSN14Sensor[,] m_ftsn14Sensors; //Sensor Pads (14 in each array) - multidimensional includes all pads on each finger in hand
    private float[,] m_ftsn14Data; //Sensor Data from Sensor Pads - multidimensional includes all pads on each finger in hand

    
    /// <summary>
    /// This is used only for the first prototype of sensory 
    /// percepts.  It will likely be removed in the next build.
    /// </summary>
    //private bool m_binarySensorMode = false;

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
    //public Vector3[] Forces { get { return m_force; } }
    public float[,] Forces { get { return m_ftsn14Data; } }

    #endregion //MPL Sensors

    //GUI Properties
    #region GUI Properties
    
    //Labels
    private GUIStyle m_labelStyle = null;

    #endregion //GUI Properties


    //Colors for Force Intensity
    #region Colors, Color Rendering

    //Renderers for each pad
    #region Renderers for FTSN Pads
    protected List<Renderer> renList_FTSNPadsIndex;
    protected List<Renderer> renList_FTSNPadsMiddle;
    protected List<Renderer> renList_FTSNPadsRing;
    protected List<Renderer> renList_FTSNPadsLittle;
    protected List<Renderer> renList_FTSNPadsThumb;
    #endregion //Renderers for FTSN Pads


    #endregion //Colors, Color Rendering


    //COLOR VISUALIZATION (HSL Range)
    #region COLOR VISUALIZATION
    public struct HSLColor
    {
        public float h;
        public float s;
        public float l;
        public float a;


        public HSLColor(float h, float s, float l, float a)
        {
            this.h = h;
            this.s = s;
            this.l = l;
            this.a = a;
        }


        public HSLColor(float h, float s, float l)
        {
            this.h = h;
            this.s = s;
            this.l = l;
            this.a = 1f;
        }


        public HSLColor(Color c)
        {
            HSLColor temp = FromRGBA(c);
            h = temp.h;
            s = temp.s;
            l = temp.l;
            a = temp.a;
        }


        public static HSLColor FromRGBA(Color c)
        {
            float h, s, l, a;
            a = c.a;

            float cmin = Mathf.Min(Mathf.Min(c.r, c.g), c.b);
            float cmax = Mathf.Max(Mathf.Max(c.r, c.g), c.b);

            l = (cmin + cmax) / 2f;

            if (cmin == cmax)
            {
                s = 0;
                h = 0;
            }
            else
            {
                float delta = cmax - cmin;

                s = (l <= .5f) ? (delta / (cmax + cmin)) : (delta / (2f - (cmax + cmin)));

                h = 0;

                if (c.r == cmax)
                {
                    h = (c.g - c.b) / delta;
                }
                else if (c.g == cmax)
                {
                    h = 2f + (c.b - c.r) / delta;
                }
                else if (c.b == cmax)
                {
                    h = 4f + (c.r - c.g) / delta;
                }

                h = Mathf.Repeat(h * 60f, 360f);
            }

            return new HSLColor(h, s, l, a);
        }


        public Color ToRGBA()
        {
            float r, g, b, a;
            a = this.a;

            float m1, m2;

            //	Note: there is a typo in the 2nd International Edition of Foley and
            //	van Dam's "Computer Graphics: Principles and Practice", section 13.3.5
            //	(The HLS Color Model). This incorrectly replaces the 1f in the following
            //	line with "l", giving confusing results.
            m2 = (l <= .5f) ? (l * (1f + s)) : (l + s - l * s);
            m1 = 2f * l - m2;

            if (s == 0f)
            {
                r = g = b = l;
            }
            else
            {
                r = Value(m1, m2, h + 120f);
                g = Value(m1, m2, h);
                b = Value(m1, m2, h - 120f);
            }

            return new Color(r, g, b, a);
        }


        static float Value(float n1, float n2, float hue)
        {
            hue = Mathf.Repeat(hue, 360f);

            if (hue < 60f)
            {
                return n1 + (n2 - n1) * hue / 60f;
            }
            else if (hue < 180f)
            {
                return n2;
            }
            else if (hue < 240f)
            {
                return n1 + (n2 - n1) * (240f - hue) / 60f;
            }
            else
            {
                return n1;
            }
        }


        public static implicit operator HSLColor(Color src)
        {
            return FromRGBA(src);
        }


        public static implicit operator Color(HSLColor src)
        {
            return src.ToRGBA();
        }

    }//struct - HSLColor

//    float f_ColorFadeTime = 0.1f; //Default time is 0.1 seconds

    float f_IntensityValue = 0.4f; //Default (deep dark color)

//    bool b_TestWithMouseLateralMovement = false; //turns on the testing with the mouse

    //Hue Parameters
    #region Hue Parameters
    float f_HueValue = 0f;
    float f_HueRange = 360f; //HSL Value

    //f_HueStart - Between 0f and f_MaxColor
    //float f_HueStart = 342f;   //Deep Red
    float f_HueStop = 342f / 360f;   //Deep Red

    //f_HueStop - Between f_MinColor and (1f + f_MinColor), will roll around
    //float f_HueStop = 252f;  //Blue
    float f_HueStart = 252f / 360f;  //Blue

    //Direction of Hue Change
    bool b_ForwardColorMovement = false; //If true, travels from MinHue to MaxHue, if false, will travel from MaxHue to MinHue

    #endregion //Hue Parameters

    //Saturation Parameters
    #region Saturation Parameters
    float f_SaturationValue = 0f;
    float f_SaturationRange = 1f; //HSL Value

    //f_MinSaturation - Between 0f and f_Max 
    float f_MinSaturation = 0.20f; //Slight-Light

    //f_MaxSaturation - Between f_Min and (1f + f_Min) 
    float f_MaxSaturation = 1.00f; //Dark

    //Direction of Saturation Change
    bool b_ForwardSaturationMovement = true; //If true, travels from Min/Light to Max/Dark color, if false, will travel from Max/Dark color to Min/Light color

    #endregion //Saturation Parameters


    #endregion //COLOR VISUALIZATION


    //FILE SYSTEM
    #region FILE SYSTEM
    protected const string m_CONFIG_FILENAME = "vMPLFTSNConfig.xml";

    #endregion //FILE SYSTEM


    #endregion //Variable Declarations


    //---------------------------------------
    // FUNCTIONS - UNITY3D (awake, start, reset, Update, FixedUpdate)
    //---------------------------------------
    #region Unity methods

    void Awake()
    {
        //CONFIGURATION FILE
        #region CONFIGURATION FILE

        // Set percept ip address and PID filter values.
        ReadXmlConfiguration();

        #endregion //CONFIGURATION FILE

    }//function - Awake

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


        //Finger Game Objects
        #region Finger Assignments
        AssignFingerDistals();
        #endregion //Finger Assignments


        //Contact Sensor Setup
        #region Contact Sensor Setup

        //Get number of contact sensors
        int i_NumberOfSensors = Enum.GetValues(typeof(CONTACT_SENSOR_ID)).Length; //Currently 37

        //Debug.Log("Number of contact sensors reported on percepts stream: " + len.ToString()); //Currently 37

        m_contactSensors = new ContactSensor[i_NumberOfSensors];  //Create local pointer array
        m_contacts = new short[i_NumberOfSensors]; //Create local array for data
        // AssignContactSensors();  //Populate pointer array

        #endregion //Contact Sensor Setup


        //Force Sensor Setup
        #region Force Sensor Setup

        //Get number of force sensors
        i_NumberOfSensors = Enum.GetValues(typeof(SegmentPerceptFTSNIdType)).Length; //Should be 5 for each finger FTSN
        //m_forceSensors = new ForceSensor[i_NumberOfSensors]; //Create local pointer array
        
        go_ftsn14SensorArrays = new GameObject[i_NumberOfSensors];
        m_ftsn14Sensors = new FTSN14Sensor[i_NumberOfSensors, NUMBER_FTSN_PADS];

        //m_force = new Vector3[i_NumberOfSensors]; //Create local array for data
        m_ftsn14Data = new float[i_NumberOfSensors, NUMBER_FTSN_PADS]; //Create local array for data
        AssignForceSensors(); //Populate pointer array

        #endregion //Force Sensor Setup


        //Color, Renderers (colors for intensity)
        #region FTSN Renderers (Color Output)
        AssignFTSNPadRenderers();

        //FTSN Colors
        InitializeFTSNColors();
        
        #endregion //FTSN Renderers (Color Output)

    }//function - Start


    /// <summary>
    /// Called once before each rendered frame.
    /// </summary>
    void Update()
    {
        //Poll Data from Sensors
        #region Sensor Data
        //int i_FTSNValidCounter = 0;
        float f_Percentage = 0f; //percentage (from range of possible) of force read-out

        //Will pass back a short value that will be processed later 

        //Contact Sensors
        #region Contact Sensors
        //Contact Sensors
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
        }//foreach - Contact Sensor

        #endregion //Contact Sensors


        //Force Sensor (FTSN)
        #region Force Sensors (FTSN)

        //Debug.Log("Contact Sensor Data: (" + m_contacts[0] + ", " + m_contacts[1] + ", " + m_contacts[2] + ", " + m_contacts[3] + ", " + m_contacts[4] + ", " + m_contacts[5] + ", " + m_contacts[6] + ", " + m_contacts[7] + ", " + m_contacts[8] + ", " + m_contacts[9] + ", " + m_contacts[10] + ", " + m_contacts[11] + ")");

        //get forces
        foreach (int fingerIndx in Enum.GetValues(typeof(SegmentPerceptFTSNIdType)))
        {
            //if (m_forceSensors[indx] != null)
            //    m_force[indx] = m_forceSensors[indx].FTSNFullForce;

            for (int ii = 0; ii < NUMBER_FTSN_PADS; ii++)
            {

                if (m_ftsn14Sensors[fingerIndx, ii] != null)
                {
                    //i_FTSNValidCounter++;

                    if (m_ftsn14Sensors[fingerIndx, ii].FTSN14Force > 0)  //Negative forces are detected by objects passing through sensors, remove for now
                    {
                        m_ftsn14Data[fingerIndx, ii] = m_ftsn14Sensors[fingerIndx, ii].FTSN14Force;

                        //Alter Color based on reading
                        f_Percentage = (m_ftsn14Data[fingerIndx, ii]) / f_ForceMaximum; //type cast byte value into integer, and then into a percentage by dividing by max value
                        
                        if (f_Percentage > .99f)
                        {
                            //Set Max for colors
                            f_Percentage = 0.99f;

                            //Set Max for reported value
                            m_ftsn14Data[fingerIndx, ii] = f_ForceMaximum;

                        }
                        if (f_Percentage < 0.005f)
                        {
                            //Set Min for colors
                            f_Percentage = 0f;

                            //Set Min for reported value
                            m_ftsn14Data[fingerIndx, ii] = 0f;

                        }//if - test for over 100%

                        //SetFTSNPadColor(fingerIndx, ii, CalculateHueValue(f_Percentage), CalculateSaturationValue(f_Percentage), f_IntensityValue); //Custom HSL Value

                        //Debug.Log("Force Data registered (" + this.transform.parent.name + ", " + ii.ToString() + "): " + m_ftsn14Data[fingerIndx, ii].ToString());

                    }//if - check for negative/invalid sensor information
                    else
                    {
                        //Set Min for colors
                        f_Percentage = 0f;

                        //Set Min for reported Value
                        m_ftsn14Data[fingerIndx, ii] = 0f;

                        //SetFTSNPadColor(fingerIndx, ii, CalculateHueValue(0), CalculateSaturationValue(0), f_IntensityValue); //Custom HSL Value
    
                    }//if - check for value

                    SetFTSNPadColor(fingerIndx, ii, CalculateHueValue(f_Percentage), CalculateSaturationValue(f_Percentage), f_IntensityValue); //Custom HSL Value
                    
                }//if - check for null value

            }//for - traversing pads on FTSN

        }//foreach - each Finger Tip

        #endregion //Force Sensors (FTSN)

        #endregion //Sensor Data


        //Collect values from sensors (whether in contact)
        #region Sensor Values (contact) //Commented Out
#if UNITY_EDITOR
        /*
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
        */
#endif
        #endregion Sensor Values (contact)

        //Debug.Log("# FTSN Sensors: " + i_FTSNValidCounter.ToString());


        //COLOR - MOUSE TESTING
        #region Color Mouse Input Testing (Commented Out)
        /*
        if (b_TestWithMouseLateralMovement) //Set by the Config file setting <mouselateralmovementtesterturnedon>
        {
            float f_MousePositionX = 0f;


            f_MousePositionX = Input.mousePosition.x;

            if (f_MousePositionX > Screen.width)
            {
                f_MousePositionX = Screen.width;
            }//Max value

            float f_HuePercentage = 0f;

            //Percentage Values for HSL Color (Hue, Saturation, Light Intensity)
            f_HuePercentage = f_MousePositionX / Screen.width;

        }//if - check for flag - testing turned on/off
        */
        #endregion //Mouse Input Testing
        
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


    //FINGERS (DISTAL SEGMENTS)
    private void AssignFingerDistals()
    {
        if (i_WhichArm == (int)whichArm.LEFT)
        {
            go_FingerDistalIndex = GameObject.Find("lIndDistal");//.transform;
            go_FingerDistalMiddle = GameObject.Find("lMidDistal");//.transform;
            go_FingerDistalRing = GameObject.Find("lRingDistal");//.transform;
            go_FingerDistalLittle = GameObject.Find("lLittleDistal");//.transform;
            go_FingerDistalThumb = GameObject.Find("lThDistal");//.transform;
        }
        else if (i_WhichArm == (int)whichArm.RIGHT)
        {
            go_FingerDistalIndex = GameObject.Find("rIndDistal");//.transform;
            go_FingerDistalMiddle = GameObject.Find("rMidDistal");//.transform;
            go_FingerDistalRing = GameObject.Find("rRingDistal");//.transform;
            go_FingerDistalLittle = GameObject.Find("rLittleDistal");//.transform;
            go_FingerDistalThumb = GameObject.Find("rThDistal");//.transform;
        }//if - check for handidness

    }//function - AssignFingerDistals


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

        //Assumes that which arm and Fingers variable are set/determined - the Sensor Array is attached
        //  to either the left or right limb
            
        Transform t_TempFingerTip;

        //int i_FingerIndex = 0;
        //t_TempFingerTip = go_FingerDistalIndex.transform;
        //i_FingerIndex = (int)SegmentPerceptFTSNIdType.INDEX_FTSN;

        for (int i_FingerIndex=0; i_FingerIndex<Enum.GetValues(typeof(SegmentPerceptFTSNIdType)).Length;i_FingerIndex++) //Should be 5 for each finger FTSN
        {
            if(i_FingerIndex == (int)SegmentPerceptFTSNIdType.INDEX_FTSN)
            {
                t_TempFingerTip = go_FingerDistalIndex.transform;
            }
            else if(i_FingerIndex == (int)SegmentPerceptFTSNIdType.MIDDLE_FTSN)
            {
                t_TempFingerTip = go_FingerDistalMiddle.transform;
            }
            else if(i_FingerIndex == (int)SegmentPerceptFTSNIdType.RING_FTSN)
            {
                t_TempFingerTip = go_FingerDistalRing.transform;
            }
            else if(i_FingerIndex == (int)SegmentPerceptFTSNIdType.LITTLE_FTSN)
            {
                t_TempFingerTip = go_FingerDistalLittle.transform;
            }
            else if(i_FingerIndex == (int)SegmentPerceptFTSNIdType.THUMB_FTSN)
            {
                t_TempFingerTip = go_FingerDistalThumb.transform;
            }
            else 
            {
                t_TempFingerTip = go_FingerDistalIndex.transform;
            }//if - check for which Finger

            if (t_TempFingerTip.childCount > 0)
            {
                //if a left hand sensor, then need to account for the inversion object (meshes are all turned inside out from negative scale - need to re-invert for sensor colliders to be properly facing)
                if (i_WhichArm == (int)whichArm.RIGHT)
                {
                    //RIGHT
                    go_ftsn14SensorArrays[i_FingerIndex] = t_TempFingerTip.GetChild(0).gameObject;

                }
                else
                {
                    //Debug.Log("CHILD: " + t_TempFingerTip.GetChild(0).GetChild(0).name);
                    //LEFT
                    go_ftsn14SensorArrays[i_FingerIndex] = t_TempFingerTip.GetChild(0).GetChild(0).gameObject; //extra step because of inversion object

                }

                //Debug.Log("Assigning FTSN Sensor: " + go_ftsn14SensorArrays[i_FingerIndex].name + " (" + go_ftsn14SensorArrays[i_FingerIndex].transform.parent.name + ")");

                if (go_ftsn14SensorArrays[i_FingerIndex].name == "FTSN14SensorArray")
                {
                    for (int jj = 0; jj < NUMBER_FTSN_PADS; jj++)
                    {
                        //m_ftsn14Sensors[i_FingerIndex, jj] = (FTSN14Sensor)(go_FTSNSensorArray.GetComponentInChildren(typeof(FTSN14Sensor)));
                        m_ftsn14Sensors[i_FingerIndex, jj] = (FTSN14Sensor)(go_ftsn14SensorArrays[i_FingerIndex].transform.GetChild(jj).GetComponentInChildren(typeof(FTSN14Sensor)));

                    }//for - traverse all finger pads
                }
                else
                {
                    Debug.Log("Unexpected Fingertip Child found.... (" + go_ftsn14SensorArrays[i_FingerIndex].name + "), Number of Children: " + go_ftsn14SensorArrays[i_FingerIndex].transform.childCount + "");
                
                }//if - check for valid child object (is in fact FTSNArray)

            }//if - check for FTSNArray object present as child for finger distal gameobject

        }//for - traverse through each finger


    }//function - AssignForceSensors


    //RENDERERS (COLOR OUTPUT)
    private void AssignFTSNPadRenderers()
    {
        GameObject go_Temp;
        Renderer ren_Temp;

        //goArray_FTSNPads = new ArrayList(UDP_BUFFER_LENGTH);
        renList_FTSNPadsIndex = new List<Renderer>();
        renList_FTSNPadsMiddle = new List<Renderer>();
        renList_FTSNPadsRing = new List<Renderer>();
        renList_FTSNPadsLittle = new List<Renderer>();
        renList_FTSNPadsThumb = new List<Renderer>();

        for (int iFingerIndex = 0; iFingerIndex < Enum.GetValues(typeof(SegmentPerceptFTSNIdType)).Length; iFingerIndex++)
        {

            for (int iPadIndex = 0; iPadIndex < NUMBER_FTSN_PADS; iPadIndex++)
            {
                //for (int i = 0; i < renList_FTSNPads.Count; i++)
                //go_Temp = go_ftsn14SensorArrays[(int)SegmentPerceptFTSNIdType.INDEX_FTSN].transform.FindChild("FTSNPad" + (i + 1).ToString()).gameObject;
                
                //(FTSN14Sensor)(go_ftsn14SensorArrays[i_FingerIndex].transform.GetChild(jj).GetComponentInChildren(typeof(FTSN14Sensor)));
                //Transform t_Temp = go_ftsn14SensorArrays[iFingerIndex].transform.FindChild("FTSNPad" + (iPadIndex + 1).ToString());
                //Transform t_Temp = go_ftsn14SensorArrays[iFingerIndex].transform.FindChild("FTSNPad1");
                //Debug.Log("" + t_Temp.name);

                go_Temp = (GameObject)go_ftsn14SensorArrays[iFingerIndex].transform.Find("FTSNPad" + (iPadIndex + 1).ToString()).gameObject;
                ren_Temp = go_Temp.transform.Find("SensorPad").gameObject.GetComponentInChildren<Renderer>();

                if (iFingerIndex == (int)SegmentPerceptFTSNIdType.INDEX_FTSN)
                {
                    renList_FTSNPadsIndex.Add(ren_Temp);
                }
                else if (iFingerIndex == (int)SegmentPerceptFTSNIdType.MIDDLE_FTSN)
                {
                    renList_FTSNPadsMiddle.Add(ren_Temp);
                }
                else if (iFingerIndex == (int)SegmentPerceptFTSNIdType.RING_FTSN)
                {
                    renList_FTSNPadsRing.Add(ren_Temp);
                }
                else if (iFingerIndex == (int)SegmentPerceptFTSNIdType.LITTLE_FTSN)
                {
                    renList_FTSNPadsLittle.Add(ren_Temp);
                }
                else if (iFingerIndex == (int)SegmentPerceptFTSNIdType.THUMB_FTSN)
                {
                    renList_FTSNPadsThumb.Add(ren_Temp);
                }
                else
                {

                }
                

                //Debug.Log("Adding FTSN: " + (i + 1).ToString());
                //go_Temp = GameObject.Find("FTSNPad" + (i+1).ToString());
                //ren_Temp = go_Temp.GetComponentInChildren<Renderer>();
                //renList_FTSNPads.Add(ren_Temp);

            }//for - loop through pads

        }//for - loop through FTSNSensorArrays
    }//function - AssignFTSNPadRenderers


    //XML FUNCTIONS
    #region XML Functions
    //XML Polling Functions
    #region XML Polling Functions

    /// <summary>
    /// Open and parse xml config file.
    /// </summary>
    private void ReadXmlConfiguration()
    {
#if UNITY_EDITOR
        XmlReader reader = null;

        try
        {
            Debug.Log("Using FTSN config (" + this.gameObject.layer.ToString() + "): " + m_CONFIG_FILENAME);
            reader = XmlReader.Create(m_CONFIG_FILENAME);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Document:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        break;
                    case XmlNodeType.EndElement:
                        break;
                    case XmlNodeType.Element:
                        switch (reader.Name.ToLower())
                        {

                            //FORCE SETTINGS
                            #region Force Settings

                            case "forcemaximumvalue":

                                f_ForceMaximum = XMLReadFloat(reader);
                                
                                break;

                            #endregion //Force Settings


                            //COLOR SETTINGS
                            #region Color Settings

                            case "colorchangespeed":

//                                f_ColorFadeTime = XMLReadFloat(reader);
                                //Debug.Log("Color Change Speed: " + f_ColorFadeTime);


                                break;

                            #region Hue Color
                            case "huecolorstart":

                                f_HueStart = XMLReadFloat(reader);
                                if (f_HueStart > 360f)
                                {
                                    f_HueStart = 360f;
                                }
                                else if (f_HueStart < 0)
                                {
                                    f_HueStart = 0;
                                }//if - check for out of bounds

                                f_HueStart /= 360f; //Convert to percentage

                                Debug.Log("Min Hue: " + f_HueStart + "(" + f_HueStart * 360f + ")");

                                break;

                            case "huecolorend":

                                f_HueStop = XMLReadFloat(reader);
                                if (f_HueStop > 360f)
                                {
                                    f_HueStop = 360f;
                                }
                                else if (f_HueStop < 0)
                                {
                                    f_HueStop = 0;
                                }//if - check for out of bounds

                                f_HueStop /= 360f; //Convert to percentage

                                Debug.Log("Max Hue: " + f_HueStop + "(" + f_HueStop * 360f + ")");

                                break;

                            case "huecolormovementforward":
                                if (reader.Read())
                                {
                                    string ip = ReadString(reader);
                                    if (!string.IsNullOrEmpty(ip))
                                    {
                                        if (ip.ToLower() == "true")
                                        {
                                            b_ForwardColorMovement = true;
                                        }
                                        else
                                        {
                                            b_ForwardColorMovement = false;

                                        }//if - boolean assignment

                                        Debug.Log("Hue Color Movement: " + b_ForwardColorMovement.ToString());
                                    }
                                }
                                break;
                            #endregion //Hue Color


                            #region Saturation Color
                            case "saturationstart":

                                f_MinSaturation = XMLReadFloat(reader);
                                if (f_MinSaturation > 1f)
                                {
                                    f_MinSaturation = 1f;
                                }
                                else if (f_MinSaturation < 0f)
                                {
                                    f_MinSaturation = 0f;
                                }//if - check for out of bounds

                                //Debug.Log("Min Saturation: " + f_MinSaturation);

                                break;

                            case "saturationend":

                                f_MaxSaturation = XMLReadFloat(reader);
                                if (f_MaxSaturation > 1f)
                                {
                                    f_MaxSaturation = 1f;
                                }
                                else if (f_MaxSaturation < 0f)
                                {
                                    f_MaxSaturation = 0f;
                                }//if - check for out of bounds

                                //Debug.Log("Max Saturation: " + f_MaxSaturation);

                                break;

                            case "saturationmovementforward":
                                if (reader.Read())
                                {
                                    string ip = ReadString(reader);
                                    if (!string.IsNullOrEmpty(ip))
                                    {
                                        if (ip.ToLower() == "true")
                                        {
                                            b_ForwardSaturationMovement = true;
                                        }
                                        else
                                        {
                                            b_ForwardSaturationMovement = false;

                                        }//if - boolean assignment

                                        //Debug.Log("Saturation Color Movement: " + b_ForwardSaturationMovement.ToString());
                                    }
                                }
                                break;
                            #endregion //Saturation


                            #region Light Intensity
                            case "lightintensity":

                                f_IntensityValue = XMLReadFloat(reader);
                                if (f_IntensityValue > 1f)
                                {
                                    f_IntensityValue = 1f;
                                }
                                else if (f_IntensityValue < 0f)
                                {
                                    f_IntensityValue = 0f;
                                }//if - check for out of bounds

                                //Debug.Log("Light Intensity: " + f_IntensityValue);

                                break;

                            #endregion //Light Intensity

                            #endregion //Color Settings

                            //TESTOR (with lateral mouse movement)
                            #region Testor with Mouse

                            case "mouselateralmovementtesterturnedon":
                                if (reader.Read())
                                {
                                    string ip = ReadString(reader);
                                    if (!string.IsNullOrEmpty(ip))
                                    {
                                        if (ip.ToLower() == "true")
                                        {
//                                            b_TestWithMouseLateralMovement = true;
                                        }
                                        else
                                        {
//                                            b_TestWithMouseLateralMovement = false;

                                        }//if - boolean assignment

//                                        Debug.Log("Testing Hue Color with Lateral Mouse Movement: " + b_TestWithMouseLateralMovement.ToString());
                                    }
                                }
                                break;

                            #endregion //Testor with Mouse

                        } // end switch(reader.Name)
                        break;
                } // end switch (reader.NodeType)
            } // end while (reader.Read())
        }
        catch (FileNotFoundException)
        {
            // Use default configuration.
            Debug.Log("Could not open VulcanXInterface config file: " +
                m_CONFIG_FILENAME + ", using default config");
        }
        catch (IsolatedStorageException)
        {
            // This exception is thrown when running the compiled executable
            // instead of FileNotFoundException.

            // Use default configuration.
            Debug.Log("Could not open VulcanXInterface config file: " +
                m_CONFIG_FILENAME + ", using default config");
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
#endif
    }//function - ReadXmlConfiguration


    #endregion //XML Polling Functions


    //XML General Reader Functions
    #region XML Generic Reader Functions

    /// <summary>
    /// Reads string value from the XML stream. All leading and trailing 
    /// whitespace is removed.  An empty string is returned if an error
    /// occurs while reading.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    private static string ReadString(XmlReader reader)
    {
        string ip = string.Empty;
        try
        {
            ip = reader.ReadContentAsString();
            if (!string.IsNullOrEmpty(ip))
                ip = ip.Trim();
        }
        catch (InvalidCastException)
        {
            Debug.LogError(
                "Error reading string value from " + m_CONFIG_FILENAME);
        }
        catch (FormatException)
        {
            Debug.LogError(
                "Error reading string value from " + m_CONFIG_FILENAME);
        }

        return ip;
    }//function - ReadString


    /// <summary>
    /// Extracts a float value from the next value in the xml stream.  If
    /// no float exists, NaN is returned.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    private float XMLReadFloat(XmlReader xmlReader)
    {
        float val = float.NaN;

        if (xmlReader.Read())
        {
            try
            {
                val = xmlReader.ReadContentAsFloat();
            }
            catch (InvalidCastException)
            {
                Debug.LogError(
                    "Error reading float value from " + m_CONFIG_FILENAME);
            }
            catch (FormatException)
            {
                Debug.LogError(
                    "Error reading float value from " + m_CONFIG_FILENAME);
            }
        }

        return val;
    }//function - XMLReadFloat


    /// <summary>
    /// Extracts an integer value from the next value in the xml stream.  If
    /// no value exists, 0 is returned.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    private int XMLReadInteger(XmlReader xmlReader)
    {
        int val = 0;

        if (xmlReader.Read())
        {
            try
            {
                val = xmlReader.ReadContentAsInt();
            }
            catch (InvalidCastException)
            {
                Debug.LogError(
                    "Error reading integer value from " + m_CONFIG_FILENAME);
            }
            catch (FormatException)
            {
                Debug.LogError(
                    "Error reading integer value from " + m_CONFIG_FILENAME);
            }
        }

        return val;
    }//function - XMLReadInteger


    /// <summary>
    /// Extracts an string value from the next value in the xml stream.  If
    /// no value exists, "" is returned.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    private string XMLReadString(XmlReader xmlReader)
    {
        string val = "";

        if (xmlReader.Read())
        {
            try
            {
                val = xmlReader.ReadContentAsString();
            }
            catch (InvalidCastException)
            {
                Debug.LogError(
                    "Error reading string value from " + m_CONFIG_FILENAME);
            }
            catch (FormatException)
            {
                Debug.LogError(
                    "Error reading string value from " + m_CONFIG_FILENAME);
            }
        }

        return val;
    }//function - XMLReadString


    /// <summary>
    /// Extracts an boolean value from the next value in the xml stream.  If
    /// no value exists, false is returned.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    private bool XMLReadBoolean(XmlReader xmlReader)
    {
        bool val = false;

        if (xmlReader.Read())
        {
            try
            {
                val = xmlReader.ReadContentAsBoolean();
            }
            catch (InvalidCastException)
            {
                Debug.LogError(
                    "Error reading boolean value from " + m_CONFIG_FILENAME);
            }
            catch (FormatException)
            {
                Debug.LogError(
                    "Error reading boolean value from " + m_CONFIG_FILENAME);
            }
        }

        return val;
    }//function - XMLReadBoolean


    /// <summary>
    /// XML reader - Positions are in centimeters.
    /// </summary>
    /// <param name="reader"></param>
    private Vector3 XMLReadVector3(XmlReader xmlReader)
    {
        Vector3 v3_Temp = new Vector3();

        while (xmlReader.Read())
        {
            switch (xmlReader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (xmlReader.Name.ToLower()[0])
                    {
                        //case "x":
                        //    v3_Temp.x = XMLReadFloat(xmlReader);
                        //    break;
                        //case "y":
                        //    v3_Temp.y = XMLReadFloat(xmlReader);
                        //    break;
                        //case "z":
                        //    v3_Temp.z = XMLReadFloat(xmlReader);
                        //    break;
                        case 'x':
                            v3_Temp.x = XMLReadFloat(xmlReader);
                            break;
                        case 'y':
                            v3_Temp.y = XMLReadFloat(xmlReader);
                            break;
                        case 'z':
                            v3_Temp.z = XMLReadFloat(xmlReader);
                            break;
                    }//switch - different Vector3 namings
                    break;
                case XmlNodeType.EndElement:
                    break;
            }//switch - XML Reader

        }//while - loop through subtree

        return v3_Temp;

    }//function -XMLReadPosition

    #endregion //XML Generic Reader Functions

    #endregion //XML Functions

    //COLORS
    #region COLORS

    //InitializeFTSNColors
    public void InitializeFTSNColors()
    {
        //Debug.Log("Initialize Color... Hue: " + f_HueStart.ToString() + " (" + CalculateHueValue(f_HueStart).ToString() + "), Saturation: " + f_MinSaturation.ToString() + " (" + CalculateSaturationValue(f_MinSaturation).ToString() + "), Intensity: " + f_IntensityValue.ToString());
        //Debug.Log("Initialize Color... Hue: " + f_HueStart.ToString() + " (" + CalculateHueValue(0f).ToString() + "), Saturation: " + f_MinSaturation.ToString() + " (" + CalculateSaturationValue(0f).ToString() + "), Intensity: " + f_IntensityValue.ToString());

        //Will set initial colors for FTSN Pads
        //HSLColor hslcolor_PadColor = new HSLColor(CalculateHueValue(f_HueStart), CalculateSaturationValue(f_MinSaturation), f_IntensityValue);
//        HSLColor hslcolor_PadColor = new HSLColor(CalculateHueValue(0f), CalculateSaturationValue(0f), f_IntensityValue);
//        Color rgbcolor = (Color)hslcolor_PadColor.ToRGBA();

        /*

        for (int i = 0; i < renList_FTSNPadsIndex.Count; i++)
        {
            //Debug.Log("Setting Initial Color (" + i.ToString() + "): 0f");

            //SetFTSNPadColor(i, 0f, 0f, 0f); //Works off of Lerp (requires constant input)
            if (renList_FTSNPadsIndex[i] != null)
            {
                renList_FTSNPadsIndex[i].material.color = rgbcolor;
            }
            if (renList_FTSNPadsMiddle[i] != null)
            {
                renList_FTSNPadsMiddle[i].material.color = rgbcolor;
            }
            if (renList_FTSNPadsRing[i] != null)
            {
                renList_FTSNPadsRing[i].material.color = rgbcolor;
            }
            if (renList_FTSNPadsLittle[i] != null)
            {
                renList_FTSNPadsLittle[i].material.color = rgbcolor;
            }
            if (renList_FTSNPadsThumb[i] != null)
            {
                renList_FTSNPadsThumb[i].material.color = rgbcolor;
            }
            

        }//for - loop through FTSN Pads

        */

    }//function - InitializeFTSNColors

    #endregion //COLORS

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


    //---------------------------------------
    // FUNCTIONS - COLOR
    //---------------------------------------
    #region COLOR FUNCTIONS

    //Converts a Percentage Value into a Hue Value for HSL Color Range (Min/Max range of colors set internally
    public float CalculateHueValue(float f_HuePercentage)
    {

        //Hue Parameters
        f_HueValue = 0f;
        //f_HueRange = 360f; //HSL Value


        //f_HueStart - Between 0f and f_MaxColor
        //float f_MinHueTemp = f_HuePercentage * f_HueRange;   //Deep Red
//        float f_MinHueTemp = -0.05f * f_HueRange;   //Deep Red
        //float f_HueStart = 0.00f * f_HueRange;   //Red

        //f_HueStop - Between f_MinColor and (1f + f_MinColor), will roll around
//        float f_MaxHueTemp = 0.70f * f_HueRange;  //Blue
        //float f_HueStop = 1.00f * f_HueRange;  //Red

        //Direction of Hue Change
        //b_ForwardColorMovement = false; //If true, travels from MinHue to MaxHue, if false, will travel from MaxHue to MinHue

        //HUE CALCAULATION
        #region Hue %
        if (b_ForwardColorMovement)
        {
            //Move Through Forwards
            //f_HueValue = Mathf.Repeat((f_HueStart - (f_HuePercentage * (f_HueStop - f_HueStart))), f_HueRange);
            if (f_HueStop > f_HueStart)
            {
                f_HueValue = Mathf.Repeat((f_HueStart + (f_HuePercentage * (f_HueStop - f_HueStart))) * f_HueRange, f_HueRange);
            }
            else
            {
                f_HueValue = Mathf.Repeat((f_HueStart + (f_HuePercentage * (1 - (f_HueStart - f_HueStop)))) * f_HueRange, f_HueRange);
            }
            //f_HueValue = Mathf.Repeat((f_MinHueTemp + (f_HuePercentage * (f_MaxHueTemp - f_MinHueTemp))), f_HueRange);
        }
        else
        {
            //Move through Backwards
            //f_HueValue = Mathf.Repeat((f_HueStop + (f_HuePercentage * (f_HueStop - f_HueStart))), f_HueRange);
            //f_HueValue = Mathf.Repeat((f_HueStop + (f_HuePercentage * (f_HueStop - f_HueStart))) * f_HueRange, f_HueRange);
            //f_HueValue = Mathf.Repeat((f_MaxHueTemp - (f_HuePercentage * (f_MaxHueTemp - f_MinHueTemp))), f_HueRange);

            if (f_HueStop > f_HueStart)
            {
                f_HueValue = Mathf.Repeat((f_HueStart - (f_HuePercentage * (1 - (f_HueStop - f_HueStart)))) * f_HueRange, f_HueRange);
            }
            else
            {
                f_HueValue = Mathf.Repeat((f_HueStart - (f_HuePercentage * (f_HueStart - f_HueStop))) * f_HueRange, f_HueRange);
            }
        }//if - check movement through color wheel backwards
        #endregion //Hue %

        //Debug.Log("Set Color... Hue: " + f_HuePercentage.ToString() + " (" + f_HueValue.ToString() + ")");

        return f_HueValue;

    }//function - CalculateHueValue


    //Converts a Percentage Value into a Hue Value for HSL Color Range (Min/Max range of colors set internally
    public float CalculateSaturationValue(float f_SaturationPercentage)
    {

        //Saturation Parameters
//        f_SaturationValue = 0f;
//        f_SaturationRange = 1f; //HSL Value

        //f_MinSaturation - Between 0f and f_Max 
//        f_MinSaturation = 0.20f * f_SaturationRange; //Slight-Light

        //f_MaxSaturation - Between f_Min and (1f + f_Min) 
//        f_MaxSaturation = 1.00f * f_SaturationRange; //Dark

        //Direction of Saturation Change
//        b_ForwardSaturationMovement = true; //If true, travels from Min/Light to Max/Dark color, if false, will travel from Max/Dark color to Min/Light color

        //SATURATION CALCULATION
        #region Saturation %
        if (b_ForwardSaturationMovement)
        {
            //Move Through Forwards
            f_SaturationValue = Mathf.Repeat((f_MinSaturation + (f_SaturationPercentage * (f_MaxSaturation - f_MinSaturation))), f_SaturationRange);
        }
        else
        {
            //Move through Backwards
            f_SaturationValue = Mathf.Repeat((f_MaxSaturation - (f_SaturationPercentage * (f_MaxSaturation - f_MinSaturation))), f_SaturationRange);

        }//if - check movement through color wheel backwards

        #endregion //Saturation %

        return f_SaturationValue;

    }//function - CalculateSaturationValue


    //Will update the color of the FTSN Pad
    public void SetFTSNPadColor(int iWhichHand, int iWhichPad, float f_HueValueTemp, float f_SaturationValue, float f_IntensityValue)
    {
        //Color Parameters
        //f_ColorFadeTime = 0.05f;

        /*
        //Debug
        //Debug.Log("Hue Value: " + f_HueValue.ToString() + ", Saturation: " + f_SaturationValue.ToString() + ", Intensity: " + f_IntensityValue.ToString() );

        HSLColor hslcolor_PadColor = new HSLColor(f_HueValueTemp, f_SaturationValue, f_IntensityValue); //values already normalize
        Color rgbcolor = (Color)hslcolor_PadColor.ToRGBA();

        if (iWhichHand == (int)SegmentPerceptFTSNIdType.INDEX_FTSN)
        {
            renList_FTSNPadsIndex[iWhichPad].material.color = rgbcolor;// Color.Lerp(renList_FTSNPadsIndex[iWhichPad].material.color, rgbcolor, f_ColorFadeTime);

        }
        else if (iWhichHand == (int)SegmentPerceptFTSNIdType.MIDDLE_FTSN)
        {
            renList_FTSNPadsMiddle[iWhichPad].material.color = rgbcolor;// Color.Lerp(renList_FTSNPadsIndex[iWhichPad].material.color, rgbcolor, f_ColorFadeTime);

        }
        else if (iWhichHand == (int)SegmentPerceptFTSNIdType.RING_FTSN)
        {
            renList_FTSNPadsRing[iWhichPad].material.color = rgbcolor;// Color.Lerp(renList_FTSNPadsIndex[iWhichPad].material.color, rgbcolor, f_ColorFadeTime);

        }
        else if (iWhichHand == (int)SegmentPerceptFTSNIdType.LITTLE_FTSN)
        {
            renList_FTSNPadsLittle[iWhichPad].material.color = rgbcolor;// Color.Lerp(renList_FTSNPadsIndex[iWhichPad].material.color, rgbcolor, f_ColorFadeTime);

        }
        else if (iWhichHand == (int)SegmentPerceptFTSNIdType.THUMB_FTSN)
        {
            renList_FTSNPadsThumb[iWhichPad].material.color = rgbcolor;// Color.Lerp(renList_FTSNPadsIndex[iWhichPad].material.color, rgbcolor, f_ColorFadeTime);

        }
        else
        {

        }

        */

    }//function - SetFTSNPadColor

    #endregion //COLOR FUNCTIONS


}//class - FTSN14SensorArray
