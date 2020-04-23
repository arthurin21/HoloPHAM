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
using System.Collections;
using System;
using System.IO;

public class ContactSensor : MonoBehaviour {

    //---------------------------------------
    // VARIABLE DECLARATIONS
    //---------------------------------------
    #region Variable Declarations

    private string m_name;
    protected static readonly object m_lock = new object();

    //MPL Properties
    #region MPL Properties
    protected int i_WhichArm = (int)whichArm.RIGHT; //Right is default - can determine by looking at the layer of the object that the sensor is attached to
    protected GameObject go_FingerSegmentAttachedTo; //which element in finger it is attached to
    protected GameObject go_ThisObject;
    protected LayerMask layer_ThisObject;

    //Which Arm / Handedness
    public enum whichArm : int
    {
        RIGHT,
        LEFT
    };
    #endregion //MPL Properties

    //Contact Sensor Properties
    #region Contact Sensor
    //Contact Sensor Signal Properties
    
    #region Contact Sensor - Signal Properties
    private const double m_minAmp = 360.0F;
    private const double m_maxAmp = 1895.0F;
    private const double m_diffAmp = m_maxAmp - m_minAmp;
    private const double m_ampOffset = 2200.0F;
    private const double m_minVel = 0.01F;  // cm/s
    private const double m_maxVel = 175.0F; // cm/s, max observed = 168
    private const double m_minDipToPeak = .12F;
    private const double m_maxDipToPeak = .15F;
    private const double m_diffDipToPeak = m_maxDipToPeak - m_minDipToPeak;
    private const double m_noiseAmp = 20.0F;
    
    
    
    //signal variable stuff
    private static short m_loopcnt;
    private short m_signalCnt;
    private double[] m_baseSignal;
    private double m_velocity;
    private double m_dipAmp;
    private double m_peakAmp;
    private int m_nDip;
    private int m_nPeak;
    private int m_signalLen;
    private double Noise{ get { return 2.0F * m_noiseAmp * UnityEngine.Random.value - m_noiseAmp; } }

    #endregion //Contact Sensor - Signal Properties

    //Collision/Trigger Event Handling
    private bool m_haveContact;
    private bool m_triggered;
    
    //Contact Sensor Signal
    private short m_value; //Binary Signal Value
    private short m_signal; //Full Signal Value
	
	// Number of updates to skip to send out percepts at 50Hz.
	private int m_skipNumUpdates;

    
    //Functionality to create log file for contact sensor output - currently turned off for release
    //StreamWriter fileWriter = null;

#if UNITY_EDITOR
    [field: System.NonSerialized]
    public bool m_gotContact;
#endif

    public bool HaveContact { get { return m_haveContact; } }
    public short BinaryValue { get { return m_value; } }
    public short FullValue { get { return m_signal; } }

    #endregion //Contact Sensor


    #endregion //Variable Declarations


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
        SetWIFID();


        #region Finger Segment
        //Assign Finger Segment sensor is attached to

        AssignMPLObjects();

        #endregion //Finger Segment


    }//function - Awake


    /// <summary>
    /// Is called at the beginning of program, will instantial objects
    /// </summary>
    void Start()
    {
        //Initialize Contact Settings
        #region Contact Sensor Initialization
        m_triggered = false;
        m_loopcnt = 0;
        m_signalCnt = 0;
        m_baseSignal = BaseSignal(out m_nDip, out m_nPeak);
        m_signalLen = m_nDip + m_nPeak;
        #endregion //Contact Sensor Initialization

        //Check the fixed time step for Unity
        #region Fixed Time Step Setup
        m_skipNumUpdates = (int)(0.02f / Time.fixedDeltaTime);
		if(0.02f / m_skipNumUpdates != Time.fixedDeltaTime)
		{
			throw new ApplicationException(
				"Fixed Timestep must be a factor of 1/50.");
        }
        #endregion //Fixed Time Step Setup


        #region Commented Out
        //Functionality to create log file for contact sensor output - currently turned off for release
        //if (String.Compare("rIndMedial", this.transform.parent.gameObject.name, true) == 0)
        //    fileWriter = File.CreateText("IndexContact.txt");
        #endregion //Commented Out
        

        //OBJECT ASSIGNMENT
        #region Object Assignment

        //Find location where sensor is attached to vMPL (also sets m_name - call before other initilization functions)
        //GetSensorLocation(); 

        #endregion //Object Assignment

    }//function - Start

    private void Update()
    {
        Debug.Log(string.Format("Contact Value: {0}", FullValue));
    }


    /// <summary>
    /// Called before each physics time step.  
    /// </summary>
    void FixedUpdate()
    {
        //Contact Sensor Event Handling
        #region Contact Sensor Sensing

        //let's only perform this at 50Hz
        if (++m_loopcnt < m_skipNumUpdates)
            return;

        m_loopcnt = 0;

        //did we just enter or exit a contact?
        if (m_triggered)
        {
            //start a new signal
            lock (m_lock)
            {
                m_triggered = false;
            }
            m_signalCnt = 1;

            //compute amplitudes
            m_peakAmp = m_minAmp + ((m_velocity - m_minVel) / m_maxVel) * m_diffAmp;
            m_dipAmp = m_peakAmp * (((m_velocity - m_minVel) / m_maxVel) * m_diffDipToPeak + m_minDipToPeak);
        }//if - check to see if entered or exited a contact/collision

        //are we currently generating a contact signal?
        if (m_signalCnt == 0)       //no
        {
            //create random noise
            m_signal = (short)Math.Round(m_ampOffset + Noise);

            #region Commented Out
            //Functionality to create log file for contact sensor output - currently turned off for release
            //if (String.Compare("rIndMedial", this.transform.parent.gameObject.name, true) == 0)
            //    fileWriter.WriteLine(m_signal);
            #endregion //Commented Out

            return;
        }//if - check to see if signal counts > 0

        //are we in the dip?
        if (m_signalCnt <= m_nDip)
        {
            m_signal = (short)(m_dipAmp * m_baseSignal[m_signalCnt - 1] + m_ampOffset + Noise);
        }
        else if ((m_signalCnt > m_nDip) && (m_signalCnt <= m_nDip + m_nPeak))
        {
            m_signal = (short)(m_peakAmp * m_baseSignal[m_signalCnt - 1] + m_ampOffset + Noise);
        }
        else
        {
            m_signal = (short)Math.Round(m_ampOffset + Noise);
        }

        m_signal = Math.Min(m_signal, (short)(m_ampOffset + m_maxAmp));

        #region Commented Out
        //Functionality to create log file for contact sensor output - currently turned off for release
        //if (String.Compare("rIndMedial", this.transform.parent.gameObject.name, true) == 0)
        //{
        //    fileWriter.WriteLine(m_signal);
        //}
        #endregion //Commented Out

        if (++m_signalCnt > m_signalLen)
            m_signalCnt = 0;    //finished

        #endregion //Contact Sensor Sensing

    }//function - FixedUpdate


    /// <summary>
    /// Called when object destroyed
    /// </summary>
    void OnDestroy()
    {
        #region Commented Out
        //Functionality to create log file for contact sensor output - currently turned off for release
        //
        //if (String.Compare("rIndMedial", this.transform.parent.gameObject.name, true) == 0)
        //    fileWriter.Close();
        #endregion //Commented Out

    }//function - OnDestroy


    #endregion //Unity methods


    //---------------------------------------
    // FUNCTIONS - INITIALIZATION
    //---------------------------------------
    #region INITIALIZATION FUNCTIONS

    /// <summary>
    /// Will assign/reassign WIF ID based on the object name
    /// </summary>
    private void SetWIFID()
    {
        try
        {
            //Assume that the go_AttachedObject has been reset and is valid
            WorldObject wo_Temp = this.GetComponent<WorldObject>(); //Points to World Object object

            //Assign the current object name to the World Object
            wo_Temp.m_id = m_name;
        }
        catch
        {

        }//try - attempt to find World Object and set name

    }//function - SetWIFID


    private void AssignMPLObjects()
    {
        //Pointer to current gameobject
        go_ThisObject = this.gameObject;

        //Right or Left Arm
        #region Right or Left Arm
            
        if (go_ThisObject != null)
        {
            //RIGHT or LEFT Arm
            layer_ThisObject = go_ThisObject.layer;

            if (GameObject.Find("rPalm") != null)
            {
                if ((go_ThisObject.layer == LayerMask.NameToLayer("RightArmSensor")) || (go_ThisObject.layer == GameObject.Find("rPalm").layer))
                {
                    i_WhichArm = (int)whichArm.RIGHT;
                }
            }
            if (GameObject.Find("lPalm") != null)
            {
                //else if (String.Equals(go_AttachedObject.layer, "LeftArm"))
                if ((go_ThisObject.layer == LayerMask.NameToLayer("LeftArmSensor")) || (go_ThisObject.layer == GameObject.Find("lPalm").layer))
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

        if(i_WhichArm == (int)whichArm.RIGHT)
        {
            //RIGHT
            go_FingerSegmentAttachedTo = this.transform.parent.gameObject; //which element in finger it is attached to
            //Debug.Log("Contact Sensor: (right, " + go_FingerSegmentAttachedTo.name + ") - " + this.name);

        }
        else
        {
            //LEFT
            go_FingerSegmentAttachedTo = this.transform.parent.parent.gameObject; //which element in finger it is attached to (left must travel additional step up)
            //Debug.Log("Contact Sensor: (left, " + go_FingerSegmentAttachedTo.name + ") - " + this.name);

        }//if - check for left/right limb


    }//function - AssignMPLObjects


    #endregion //INITIALIZATION FUNCTIONS


    //---------------------------------------
    // FUNCTIONS - COLLISION DETECTION 
    //---------------------------------------
    #region Collision Methods

    /// <summary>
    /// Detects when a collideable object enters the collider space (when collisions setting is turned off)
    /// </summary>
    void OnTriggerEnter(Collider otherCol)
    {
        lock (m_lock)
        {
            m_triggered = true;
            m_haveContact = true;
            m_value = 1;

            m_velocity = go_FingerSegmentAttachedTo.GetComponent<Rigidbody>().velocity.magnitude;
            //m_velocity = this.transform.parent.gameObject.rigidbody.velocity.magnitude;
        }

#if UNITY_EDITOR
        m_gotContact = true;
#endif
    }//function - OnTriggerEnter


    /// <summary>
    /// Detects when a collideable object exits the collider space (when collisions setting is turned off)
    /// </summary>
    void OnTriggerExit(Collider otherCol)
    {
        lock (m_lock)
        {
            m_triggered = true;
            m_haveContact = false;
            m_value = 0;
            m_velocity = go_FingerSegmentAttachedTo.GetComponent<Rigidbody>().velocity.magnitude;
            //m_velocity = this.transform.parent.gameObject.rigidbody.velocity.magnitude;
        }

#if UNITY_EDITOR
        m_gotContact = false;
#endif
    }//function - OnTriggerExit


    /// <summary>
    /// If the triggering event continues - output will be displayed visually on scene window
    /// </summary>
    void OnTriggerStay(Collider col_this)
    {

        //DRAW FORCE VECTORS
        #region Visual Depiction of Force (Magnitude/Direction)
        
        //Draw the Magnitude and Direction of the Forces calculated
        //Debug.DrawRay(this.gameObject.transform.TransformPoint(0, 0, 0), col_this.transform.forward * 2f, Color.yellow, .5f, false);
        Debug.DrawRay(this.gameObject.transform.TransformPoint(0, 0, 0), this.gameObject.transform.forward * 2f, Color.yellow, .5f, false); 

        #endregion //Visual Depiction of Force (Magnitude/Direction)

    }//function - OnTriggerStay


    #endregion //Collision Methods


    //---------------------------------------
    // FUNCTIONS - COMMUNICATION
    //---------------------------------------
    #region Communication

    /// <summary>
    /// Will convert the detection of contacts into a signal similar to that 
    /// outputted by the physical MPL contact sensor (low-high spike pulses, not on/off or v=0/1 square waves)
    /// </summary>
    private double[] BaseSignal(out int nDip, out int nPeak)
    {
        double sampRate = 50.0F;
        double dipFreq = 1.0F / 0.3F;       //tuned in Matlab
        double peakFreq = 1.0F / 0.21F;     //tuned in Matlab
        nDip = (int)(0.3F * sampRate);  //duration of dip is ~.3 secs
        nPeak = (int)(0.23 * sampRate); //duration of peak is ~.23 secs
        double[] signal = new double[nDip + nPeak];

        //generate dip
        int indx = 0;
        double sclr = Math.PI * dipFreq / sampRate;
        for (int i = nDip - (int)(nDip / 2) + 1; i <= nDip + (int)(nDip / 2) + 1; i++)
        {
            signal[indx++] = Math.Cos((double)i * sclr) - 1.0F;
        }

        //generate peak
        sclr = Math.PI * peakFreq / sampRate;
        for (int i = 1; i <= nPeak; i++)
        {
            signal[indx++] = (1.0F - Math.Cos((double)i * 2.0F * sclr)) / 2.0F;
        }

        return signal;
    }//function - BaseSignal


    #endregion //Communication


}//class - Contact Sensor
