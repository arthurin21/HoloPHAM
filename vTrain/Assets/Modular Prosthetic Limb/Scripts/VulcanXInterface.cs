
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

#region Inheritances
using UnityEngine;
using System.Collections;
using System.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO.IsolatedStorage;
#endregion //Inheritances

/// <summary>
/// Only one instance of this script should be used in a scenario, however
/// most variables and many methods are static to allow for misuse of this
/// script.
/// </summary>
public class VulcanXInterface : MonoBehaviour 
{

    //---------------------------------------
    // VARIABLE DECLARATIONS
    //---------------------------------------
    #region Variable Declarations
    

    //FLAGS
    #region Flags
    public bool m_haveRightMPL = true;
    public bool m_haveLeftMPL = false;

    private bool m_MPL_Movement_Enabled_Right = true;
    private bool m_MPL_Movement_Enabled_Left = true;
    private bool m_MPL_MUD_Movement_Enabled_Right = true;
    private bool m_MPL_MUD_Movement_Enabled_Left = true;

    private string str_MPL_CollisionDetectionMode = "default"; //default/discrete/continuous/dynamic

    public bool m_logRightPerceptsEnabled = true;
    public bool m_logLeftPerceptsEnabled = true;

    static protected bool ms_initialized = false;

    public const bool flag_ShowForceBall = true;

    #endregion //Flags


    //COMMUNICATION
    #region VulcanX Communication
    //protected WorldInterface m_worldIface;

    /// <summary>
    /// Saves time of last call to FixedUpdate().  The physics time step 
    /// should be a factor of 1/50s so that VulcanX messages are serviced
    /// at 50Hz.
    /// </summary>
    static protected float ms_lastUpdate = -1.0f;

    /// <summary>
    /// This variable is used solely by FixedUpdate() to maintain a 50Hz 
    /// command rate from VulcanX and to send percepts back at 50Hz.
    /// </summary>
    //private bool m_update = false;
    private int m_updateCounter = 0;

	static protected int ms_numUpdatesPerVulcanXMsg;

    /// <summary>
    /// Saves time of last read of VulcanX port.  The ICD states that Unity3D
    /// will accept messages at 50Hz.  Since the physics engine is probably
    /// set to run faster than 50Hz (current 100Hz), this variable is used to
    /// check when to read the VulcanX port.
    /// </summary>
    //static protected float ms_lastRead = -1.0f;

    /// <summary>
    /// Buffer for receiving commands for the right MPL.
    /// </summary>
    static private byte[] ms_rBuffer;

    /// <summary>
    /// Buffer for receiving commands for the left MPL.
    /// </summary>
    static private byte[] ms_lBuffer;

    protected const string m_CONFIG_FILENAME = "VulcanXInterfaceConfig.xml";
    protected string m_rightPerceptIp = "255.255.255.255";
    protected string m_leftPerceptIp = "255.255.255.255";

    /// <summary>
    /// VulcanX command message types.
    /// </summary>
    private enum VulcanXToUnityE
    {
        NONE, //ENDPOINT_V6_FINGERS_PV,
        ENDPOINT_DOM_POS_VEL, //ALL_JOINTS_PV,
        ENDPOINT_DOM_POS, //ENDPOINT_P6_FINGERS_PV,
        ENDPOINT_DOM_VEL, //ENDPOINT_V6_HAND_ROC_GRASPS,
        ENDPOINT_6DOF_POS, //ENDPOINT_P6_HAND_ROC_GRASPS,
        ENDPOINT_6DOF_VEL, //ENDPOINT_V6_FINGERS_V,
        XXXX //ARM_PV_HAND_ROC_GRASPS
    }//enum - VulcanXToUnityE

    #endregion //VulcanX Communication


    //PID CONTROLLER VALUES
    #region PID Control Setup and Values

    // Filter values used by PID controllers.
    //protected float[] m_filterNum;
    //protected float[] m_filterDen;
    protected float[] m_armFilterNum;
    protected float[] m_armFilterDen;
    protected float[] m_wristFilterNum;
    protected float[] m_wristFilterDen;
    protected float[] m_fingerFilterNum;
    protected float[] m_fingerFilterDen;

    public const float f_K_NonGravityConstant = 0.5f; //Will reduce the velocity applied to motors to stabilize the limb during collisions and when commanded to be still


    /// <summary>
    /// Convenience class for storing values used for PID control of a
    /// joint.  
    ///
    /// Currently, PID is used for the delta position value.  P only is used
    /// for the delta velocity value.
    /// </summary>
    public class PIDValues
    {
        public float m_lastReqPos;
        public float m_prevError;
        public float m_integral;
        public float m_Kp;
        public float m_Ki;
        public float m_Kd;
        public float m_maxV;
        public float m_desiredTargVel;

        /// <summary>
        /// Holds the last n requested angles.
        /// </summary>
        public float[] m_pIn;

        /// <summary>
        /// Holds the last n angles set by the PID controller.
        /// </summary>
        public float[] m_pOut;

        /// <summary>
        /// P term for velocity.
        /// </summary>
        public float m_KpVel;

        public PIDValues()
        {
            InitCommonValues();
        }

        public PIDValues(float p)
        {
            InitCommonValues();
            m_Kp = p;
        }

        public PIDValues(float p, float i)
        {
            InitCommonValues();
            m_Kp = p;
            m_Ki = i;
        }

        public PIDValues(float p, float i, float d)
        {
            InitCommonValues();
            m_Kp = p;
            m_Ki = i;
            m_Kd = d;
        }

        public PIDValues(float p, float i, float d, float pVel)
        {
            InitCommonValues();
            m_Kp = p;
            m_Ki = i;
            m_Kd = d;
            m_KpVel = pVel;
        }

        public PIDValues(float p, float i, float d, float pVel, float maxV)
        {
            InitCommonValues();
            m_Kp = p;
            m_Ki = i;
            m_Kd = d;
            m_maxV = maxV;
            m_KpVel = pVel;
        }

        /// <summary>
        /// This is the default instantiation of the class.  All constructors
        /// call this initially and then replace the variables that they take
        /// as parameters.
        /// </summary>
        private void InitCommonValues()
        {
            m_Kp = 0.005f;
            m_Ki = 0.0f;
            m_Kd = 0.0f;
            m_maxV = 120.0f;
            m_KpVel = 1.0f;
            m_lastReqPos = 0f; // float.MaxValue;
            m_prevError = 0;
            m_integral = 0;
            m_desiredTargVel = 0;
            m_pIn = new float[4];
            m_pIn[0] = 0;
            m_pIn[1] = 0;
            m_pIn[2] = 0;
            m_pIn[3] = 0;
            m_pOut = new float[4];
            m_pOut[0] = 0;
            m_pOut[1] = 0;
            m_pOut[2] = 0;
            m_pOut[3] = 0;
        }
    }//class - PIDValues

    #endregion //PID Control Setup and Values


    //MPL VARIABLES
    #region MPL Variables

    //Public Var for damping - will be used for assignment
    public float f_Damping = 0;
    public bool b_Gravity = false;

    //const string MPL_PREFAB_NAME_RIGHT = "rMPL Prefab";
    //const string MPL_PREFAB_NAME_LEFT = "lMPL Prefab";
    const string MPL_PREFAB_NAME_RIGHT = "rMPL Contact Prefab FTSN14";
    const string MPL_PREFAB_NAME_LEFT = "lMPL Contact Prefab FTSN14";

    //LEFT MPL
    #region Left MPL

    static protected Socket ms_leftRecvSock;
    static protected int ms_leftRecvPort = 25100;
    static protected EndPoint ms_leftVulcanXEndPt;

#if UNITY_EDITOR
    static protected UdpClient ms_leftPerceptUdp;
#endif
    static protected int ms_leftPerceptPort = 25101;
    static protected IPEndPoint ms_leftBroadcastAddr;
    static protected byte[] ms_lPerceptData;

    // GameObjects representing the left MPL.
    #region Left MPL GameObjects and String Constants

    // Expected names of corresponding GameObjects.
    public const string ms_LEFT_SHOULDER_ROOT_STR = "lShoulderRoot";
    public const string ms_LEFT_SHOULDER_FLEX_ASSEMBLY_STR =
        "lShoulderFlexAssembly";
    public const string ms_LEFT_SHOULDER_SHELL_STR = "lShoulderShell";
    public const string ms_LEFT_HUMERAL_ROTATOR_ELBOW_STR =
        "lHumeralRotator_Elbow";
    public const string ms_LEFT_FORE_ARM_STR = "lForeArm";
    public const string ms_LEFT_WRIST_SHELL_STR = "lWristShell";
    public const string ms_LEFT_WRIST_DEV_STR = "lWristDev";
    public const string ms_LEFT_PALM_STR = "lPalm";
    public const string ms_LEFT_PLANETARY_ASM_STR = "lPlanetaryAsm";
    public const string ms_LEFT_THUMB_PROXIMAL1_STR = "lThProximal1";
    public const string ms_LEFT_THUMB_PROXIMAL2_STR = "lThProximal2";
    public const string ms_LEFT_THUMB_DISTAL_STR = "lThDistal";
    public const string ms_LEFT_IND_METACARPAL_STR = "lIndMetaCarpal";
    public const string ms_LEFT_IND_PROXIMAL_STR = "lIndProximal";
    public const string ms_LEFT_IND_MEDIAL_STR = "lIndMedial";
    public const string ms_LEFT_IND_DISTAL_STR = "lIndDistal";
    public const string ms_LEFT_MID_METACARPAL_STR = "lMidMetaCarpal";
    public const string ms_LEFT_MID_PROXIMAL_STR = "lMidProximal";
    public const string ms_LEFT_MID_MEDIAL_STR = "lMidMedial";
    public const string ms_LEFT_MID_DISTAL_STR = "lMidDistal";
    public const string ms_LEFT_RING_METACARPAL_STR = "lRingMetaCarpal";
    public const string ms_LEFT_RING_PROXIMAL_STR = "lRingProximal";
    public const string ms_LEFT_RING_MEDIAL_STR = "lRingMedial";
    public const string ms_LEFT_RING_DISTAL_STR = "lRingDistal";
    public const string ms_LEFT_LITTLE_METACARPAL_STR = "lLittleMetaCarpal";
    public const string ms_LEFT_LITTLE_PROXIMAL_STR = "lLittleProximal";
    public const string ms_LEFT_LITTLE_MEDIAL_STR = "lLittleMedial";
    public const string ms_LEFT_LITTLE_DISTAL_STR = "lLittleDistal";

    static protected GameObject ms_lShoulderRoot;
    static protected GameObject ms_lShoulderFlexAssembly;
    static protected GameObject ms_lShoulderShell;
    static protected GameObject ms_lHumeralRotatorElbow;
    static protected GameObject ms_lForeArm;
    static protected GameObject ms_lWristShell;
    static protected GameObject ms_lWristDev;
    static protected GameObject ms_lPalm;
    static protected GameObject ms_lPlanetaryAsm;
    static protected GameObject ms_lThProximal1;
    static protected GameObject ms_lThProximal2;
    static protected GameObject ms_lThDistal;
    static protected GameObject ms_lIndMetaCarpal;
    static protected GameObject ms_lIndProximal;
    static protected GameObject ms_lIndMedial;
    static protected GameObject ms_lIndDistal;
    static protected GameObject ms_lMidMetaCarpal;
    static protected GameObject ms_lMidProximal;
    static protected GameObject ms_lMidMedial;
    static protected GameObject ms_lMidDistal;
    static protected GameObject ms_lRingMetaCarpal;
    static protected GameObject ms_lRingProximal;
    static protected GameObject ms_lRingMedial;
    static protected GameObject ms_lRingDistal;
    static protected GameObject ms_lLittleMetaCarpal;
    static protected GameObject ms_lLittleProximal;
    static protected GameObject ms_lLittleMedial;
    static protected GameObject ms_lLittleDistal;

    static protected HingeJoint ms_lPalmToPlanetaryAsm;
    static protected HingeJoint ms_lPalmToLittleMetaCarpal;
    static protected HingeJoint ms_lPalmToRingMetaCarpal;
    static protected HingeJoint ms_lPalmToMidMetaCarpal;
    static protected HingeJoint ms_lPalmToIndMetaCarpal;

    static protected ConfigurableJoint ms_lElbowJoint;

    static protected PIDValues ms_lShoulderFEPid;
    static protected PIDValues ms_lShoulderAAPid;
    static protected PIDValues ms_lHumeralRotPid;
    static protected PIDValues ms_lElbowFEPid;
    static protected PIDValues ms_lWristRotPid;
    static protected PIDValues ms_lWristDevPid;
    static protected PIDValues ms_lWristFEPid;
    static protected PIDValues ms_lThAAPid;
    static protected PIDValues ms_lThFEPid;
    static protected PIDValues ms_lThMCPPid;
    static protected PIDValues ms_lThDistalPid;
    static protected PIDValues ms_lIndAAPid;
    static protected PIDValues ms_lIndMCPPid;
    static protected PIDValues ms_lIndProximalPid;
    static protected PIDValues ms_lIndDistalPid;
    static protected PIDValues ms_lMidAAPid;
    static protected PIDValues ms_lMidMCPPid;
    static protected PIDValues ms_lMidProximalPid;
    static protected PIDValues ms_lMidDistalPid;
    static protected PIDValues ms_lRingAAPid;
    static protected PIDValues ms_lRingMCPPid;
    static protected PIDValues ms_lRingProximalPid;
    static protected PIDValues ms_lRingDistalPid;
    static protected PIDValues ms_lLittleAAPid;
    static protected PIDValues ms_lLittleMCPPid;
    static protected PIDValues ms_lLittleProximalPid;
    static protected PIDValues ms_lLittleDistalPid;

    //static SensorArray ms_lSegPercepts;
    static FTSN14SensorArray ms_lSegPercepts;


    #endregion

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
    // Accessors that return the requested joint's position in degrees.
    //
    #region Left MPL Position Accessor Methods

    #region Left Upper Arm Joints
    static public float LeftShoulderFE()
    {
        return ms_leftShoulderFE;
    }

    static public float LeftShoulderAA()
    {
        return ms_leftShoulderAA;
    }

    static public float LeftHumeralRot()
    {
        return ms_leftHumeralRot;
    }

    static public float LeftElbowFE()
    {
        return ms_leftElbowFE;
    }

    static public float LeftWristRot()
    {
        return ms_leftWristRot;
    }

    static public float LeftWristDev()
    {
        return ms_leftWristDev;
    }

    static public float LeftWristFE()
    {
        return ms_leftWristFE;
    }
    #endregion //Left Upper Arm Joints

    #region Finger/Thumb Joints

    #region Index
    static public float LeftIndexAA()
    {
        return ms_leftIndexAA;
    }

    static public float LeftIndexMCP()
    {
        return ms_leftIndexMCP;
    }

    static public float LeftIndexPIP()
    {
        return ms_leftIndexPIP;
    }

    static public float LeftIndexDIP()
    {
        return ms_leftIndexDIP;
    }
    #endregion //Index

    #region Middle
    static public float LeftMiddleAA()
    {
        return ms_leftMiddleAA;
    }

    static public float LeftMiddleMCP()
    {
        return ms_leftMiddleMCP;
    }

    static public float LeftMiddlePIP()
    {
        return ms_leftMiddlePIP;
    }

    static public float LeftMiddleDIP()
    {
        return ms_leftMiddleDIP;
    }
    #endregion //Middle

    #region Ring
    static public float LeftRingAA()
    {
        return ms_leftRingAA;
    }

    static public float LeftRingMCP()
    {
        return ms_leftRingMCP;
    }

    static public float LeftRingPIP()
    {
        return ms_leftRingPIP;
    }

    static public float LeftRingDIP()
    {
        return ms_leftRingDIP;
    }
    #endregion //Ring

    #region Little
    static public float LeftLittleAA()
    {
        return ms_leftLittleAA;
    }

    static public float LeftLittleMCP()
    {
        return ms_leftLittleMCP;
    }

    static public float LeftLittlePIP()
    {
        return ms_leftLittlePIP;
    }

    static public float LeftLittleDIP()
    {
        return ms_leftLittleDIP;
    }
    #endregion //Little

    #region Thumb
    static public float LeftThumbAA()
    {
        return ms_leftThumbAA;
    }

    static public float LeftThumbFE()
    {
        return ms_leftThumbFE;
    }

    static public float LeftThumbMCP()
    {
        return ms_leftThumbMCP;
    }

    static public float LeftThumbDIP()
    {
        return ms_leftThumbDIP;
    }
    #endregion //Thumb

    #endregion //Finger/Thumb Joints

    #endregion //Left MPL Position Accessor Methods

    #endregion  // Left MPL

    //RIGHT MPL
    #region Right MPL

    static protected Socket ms_rightRecvSock;
    static protected int ms_rightRecvPort = 25000;
    static protected EndPoint ms_rightVulcanXEndPt;

#if UNITY_EDITOR
    static protected UdpClient ms_rightPerceptUdp;
#endif
    static protected int ms_rightPerceptPort = 25001;
    static protected IPEndPoint ms_rightBroadcastAddr;
    static protected byte[] ms_rPerceptData = null;

    // GameObjects representing the right MPL.
    #region Right MPL GameObjects and String Constants

    // Expected names of corresponding GameObjects.
    public const string ms_RIGHT_SHOULDER_ROOT_STR = "rShoulderRoot";
    public const string ms_RIGHT_SHOULDER_FLEX_ASSEMBLY_STR =
        "rShoulderFlexAssembly";
    public const string ms_RIGHT_SHOULDER_SHELL_STR = "rShoulderShell";
    public const string ms_RIGHT_HUMERAL_ROTATOR_ELBOW_STR =
        "rHumeralRotator_Elbow";
    public const string ms_RIGHT_FORE_ARM_STR = "rForeArm";
    public const string ms_RIGHT_WRIST_SHELL_STR = "rWristShell";
    public const string ms_RIGHT_WRIST_DEV_STR = "rWristDev";
    public const string ms_RIGHT_PALM_STR = "rPalm";
    public const string ms_RIGHT_PLANETARY_ASM_STR = "rPlanetaryAsm";
    public const string ms_RIGHT_THUMB_PROXIMAL1_STR = "rThProximal1";
    public const string ms_RIGHT_THUMB_PROXIMAL2_STR = "rThProximal2";
    public const string ms_RIGHT_THUMB_DISTAL_STR = "rThDistal";
    public const string ms_RIGHT_IND_METACARPAL_STR = "rIndMetaCarpal";
    public const string ms_RIGHT_IND_PROXIMAL_STR = "rIndProximal";
    public const string ms_RIGHT_IND_MEDIAL_STR = "rIndMedial";
    public const string ms_RIGHT_IND_DISTAL_STR = "rIndDistal";
    public const string ms_RIGHT_MID_METACARPAL_STR = "rMidMetaCarpal";
    public const string ms_RIGHT_MID_PROXIMAL_STR = "rMidProximal";
    public const string ms_RIGHT_MID_MEDIAL_STR = "rMidMedial";
    public const string ms_RIGHT_MID_DISTAL_STR = "rMidDistal";
    public const string ms_RIGHT_RING_METACARPAL_STR = "rRingMetaCarpal";
    public const string ms_RIGHT_RING_PROXIMAL_STR = "rRingProximal";
    public const string ms_RIGHT_RING_MEDIAL_STR = "rRingMedial";
    public const string ms_RIGHT_RING_DISTAL_STR = "rRingDistal";
    public const string ms_RIGHT_LITTLE_METACARPAL_STR = "rLittleMetaCarpal";
    public const string ms_RIGHT_LITTLE_PROXIMAL_STR = "rLittleProximal";
    public const string ms_RIGHT_LITTLE_MEDIAL_STR = "rLittleMedial";
    public const string ms_RIGHT_LITTLE_DISTAL_STR = "rLittleDistal";

    static protected GameObject ms_rShoulderRoot;
    static protected GameObject ms_rShoulderFlexAssembly;
    static protected GameObject ms_rShoulderShell;
    static protected GameObject ms_rHumeralRotatorElbow;
    static protected GameObject ms_rForeArm;
    static protected GameObject ms_rWristShell;
    static protected GameObject ms_rWristDev;
    static protected GameObject ms_rPalm;
    static protected GameObject ms_rPlanetaryAsm;
    static protected GameObject ms_rThProximal1;
    static protected GameObject ms_rThProximal2;
    static protected GameObject ms_rThDistal;
    static protected GameObject ms_rIndMetaCarpal;
    static protected GameObject ms_rIndProximal;
    static protected GameObject ms_rIndMedial;
    static protected GameObject ms_rIndDistal;
    static protected GameObject ms_rMidMetaCarpal;
    static protected GameObject ms_rMidProximal;
    static protected GameObject ms_rMidMedial;
    static protected GameObject ms_rMidDistal;
    static protected GameObject ms_rRingMetaCarpal;
    static protected GameObject ms_rRingProximal;
    static protected GameObject ms_rRingMedial;
    static protected GameObject ms_rRingDistal;
    static protected GameObject ms_rLittleMetaCarpal;
    static protected GameObject ms_rLittleProximal;
    static protected GameObject ms_rLittleMedial;
    static protected GameObject ms_rLittleDistal;

    static protected HingeJoint ms_rPalmToPlanetaryAsm;
    static protected HingeJoint ms_rPalmToLittleMetaCarpal;
    static protected HingeJoint ms_rPalmToRingMetaCarpal;
    static protected HingeJoint ms_rPalmToMidMetaCarpal;
    static protected HingeJoint ms_rPalmToIndMetaCarpal;

    static protected ConfigurableJoint ms_rElbowJoint;

    static protected PIDValues ms_rShoulderFEPid;
    static protected PIDValues ms_rShoulderAAPid;
    static protected PIDValues ms_rHumeralRotPid;
    static protected PIDValues ms_rElbowFEPid;
    static protected PIDValues ms_rWristRotPid;
    static protected PIDValues ms_rWristDevPid;
    static protected PIDValues ms_rWristFEPid;
    static protected PIDValues ms_rThAAPid;
    static protected PIDValues ms_rThFEPid;
    static protected PIDValues ms_rThMCPPid;
    static protected PIDValues ms_rThDistalPid;
    static protected PIDValues ms_rIndAAPid;
    static protected PIDValues ms_rIndMCPPid;
    static protected PIDValues ms_rIndProximalPid;
    static protected PIDValues ms_rIndDistalPid;
    static protected PIDValues ms_rMidAAPid;
    static protected PIDValues ms_rMidMCPPid;
    static protected PIDValues ms_rMidProximalPid;
    static protected PIDValues ms_rMidDistalPid;
    static protected PIDValues ms_rRingAAPid;
    static protected PIDValues ms_rRingMCPPid;
    static protected PIDValues ms_rRingProximalPid;
    static protected PIDValues ms_rRingDistalPid;
    static protected PIDValues ms_rLittleAAPid;
    static protected PIDValues ms_rLittleMCPPid;
    static protected PIDValues ms_rLittleProximalPid;
    static protected PIDValues ms_rLittleDistalPid;

    //static SensorArray ms_rSegPercepts;
    static FTSN14SensorArray ms_rSegPercepts;
    

    static protected ProximalContact ms_rIndProxContact;
    static protected ProximalContact ms_rMidProxContact;
    static protected DistalContact ms_rIndDistContact;
    static protected DistalContact ms_rMidDistContact;
    static protected ProximalContact ms_lIndProxContact;
    static protected ProximalContact ms_lMidProxContact;

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

    //
    // Accessors that return the requested joint's position in degrees.
    //
    #region Right MPL Position Accessor Methods

    #region Right Upper Arm Joints
    static public float RightShoulderFE()
    {
        return ms_rightShoulderFE;
    }

    static public float RightShoulderAA()
    {
        return ms_rightShoulderAA;
    }

    static public float RightHumeralRot()
    {
        return ms_rightHumeralRot;
    }

    static public float RightElbowFE()
    {
        return ms_rightElbowFE;
    }

    static public float RightWristRot()
    {
        return ms_rightWristRot;
    }

    static public float RightWristDev()
    {
        return ms_rightWristDev;
    }

    static public float RightWristFE()
    {
        return ms_rightWristFE;
    }
    #endregion //Right Upper Arm Joints

    #region Finger/Thumb Joints
    #region Index
    static public float RightIndexAA()
    {
        return ms_rightIndexAA;
    }

    static public float RightIndexMCP()
    {
        return ms_rightIndexMCP;
    }

    static public float RightIndexPIP()
    {
        return ms_rightIndexPIP;
    }

    static public float RightIndexDIP()
    {
        return ms_rightIndexDIP;
    }
    #endregion //Index

    #region Middle
    static public float RightMiddleAA()
    {
        return ms_rightMiddleAA;
    }

    static public float RightMiddleMCP()
    {
        return ms_rightMiddleMCP;
    }

    static public float RightMiddlePIP()
    {
        return ms_rightMiddlePIP;
    }

    static public float RightMiddleDIP()
    {
        return ms_rightMiddleDIP;
    }
    #endregion //Middle

    #region Ring
    static public float RightRingAA()
    {
        return ms_rightRingAA;
    }

    static public float RightRingMCP()
    {
        return ms_rightRingMCP;
    }

    static public float RightRingPIP()
    {
        return ms_rightRingPIP;
    }

    static public float RightRingDIP()
    {
        return ms_rightRingDIP;
    }
    #endregion //Ring

    #region Little
    static public float RightLittleAA()
    {
        return ms_rightLittleAA;
    }

    static public float RightLittleMCP()
    {
        return ms_rightLittleMCP;
    }

    static public float RightLittlePIP()
    {
        return ms_rightLittlePIP;
    }

    static public float RightLittleDIP()
    {
        return ms_rightLittleDIP;
    }
    #endregion //Little

    #region Thumb
    static public float RightThumbAA()
    {
        return ms_rightThumbAA;
    }

    static public float RightThumbFE()
    {
        return ms_rightThumbFE;
    }

    static public float RightThumbMCP()
    {
        return ms_rightThumbMCP;
    }

    static public float RightThumbDIP()
    {
        return ms_rightThumbDIP;
    }
    #endregion //Thumb
    #endregion //Finger/Thumb Joints

    #endregion

    #endregion  // Right MPL

    //MPL-Related Enums
    #region MPL Enums

    //MPL Right/Left - TOADD
    public enum MPLE
    {
        RIGHT,
        LEFT
    }//enum - MPL Left/Right
    

    //FINGER
    protected enum FingerE
    {
        INDEX,
        MIDDLE,
        RING,
        LITTLE
    }//enum - FingerE


    protected int NUMBER_FTSN_PADS = 14;

    #endregion //MPL Enums

    #endregion //MPL Variables


    #endregion //Variable Declarations
    

    //---------------------------------------
    // FUNCTIONS - UNITY3D (awake, start, reset, Update, FixedUpdate)
    //---------------------------------------
    #region Unity3D Functions

    /// <summary>
    /// Script initialization.  Static members are initialized, so 
    /// ms_initialized used to ensure they're initialized only once.
    /// </summary>
    void Awake()
    {
        if (!ms_initialized)
        {
            //OBJECT CONSTRUCTION
            #region OBJECT CONSTRUCTION

            // Ensure this instance can survive a reset.
            UnityEngine.Object.DontDestroyOnLoad(this);


            // Initialize static members.
            ms_initialized = true;

            #endregion //OBJECT CONSTRUCTION


            //CONFIGURATION FILE
            #region Configuration File Reading

            //Initialize MPL PID Controller structs and Values (default values) prior to loading new values from config file
            InitDefaultFilterValues();

            // Set percept ip address and PID filter values.
            ReadXmlConfiguration();

            #endregion //Configuration File Reading


            //VULCANX COMMUNICATION
            #region VULCANX COMMUNICATION

            //Initialize UDP Communication
            #region UDP Communication Initialization
            IPEndPoint ipRightEndPt = new IPEndPoint(IPAddress.Any, ms_rightRecvPort);
            ms_rightRecvSock = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            ms_rightRecvSock.Bind(ipRightEndPt);
            ms_rBuffer = new byte[2048];

            IPEndPoint ipLeftEndPt = new IPEndPoint(IPAddress.Any, ms_leftRecvPort);
            ms_leftRecvSock = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            ms_leftRecvSock.Bind(ipLeftEndPt);
            ms_lBuffer = new byte[2048];

            IPEndPoint armRightEndPt = new IPEndPoint(IPAddress.Any, 0);
            ms_rightVulcanXEndPt = (EndPoint)armRightEndPt;

            IPEndPoint armLeftEndPt = new IPEndPoint(IPAddress.Any, 0);
            ms_leftVulcanXEndPt = (EndPoint)armLeftEndPt;
            #endregion //UDP Communication Initialization


            //Initialize Percept Stream
            #region Percept Initialization
            InitializePerceptData();

            //Right MPL Percept Stream
            #region Right MPL Percept Stream

#if UNITY_EDITOR
            ms_rightPerceptUdp = new UdpClient();
#endif            
            //ms_rightBroadcastAddr = new IPEndPoint(IPAddress.Broadcast, ms_rightPerceptPort);
            IPAddress rtPerIp = null;
            try
            {
                rtPerIp = IPAddress.Parse(m_rightPerceptIp);
            }
            catch (FormatException)
            {
                Debug.LogError(
                    "Bad IP address given for right percept port: " + 
                    m_rightPerceptIp + ", broadcasting instead.");
                rtPerIp = IPAddress.Broadcast;
            }
            ms_rightBroadcastAddr = new IPEndPoint(rtPerIp, ms_rightPerceptPort);
            
            //Check to see if vMPL Percepts are enabled
            if (m_logRightPerceptsEnabled)
            {
                Debug.Log("Right Percepts Enabled - Sending to IP " + rtPerIp.ToString());
            }
            else
            {
                Debug.Log("Right Percepts Disabled");
            }//if - check to see if percepts reported is enabled

            #endregion //Right MPL Percept Stream


            //Left MPL Percept Stream
            #region Left MPL Percept Stream

#if UNITY_EDITOR
            ms_leftPerceptUdp = new UdpClient();
#endif
            IPAddress lPerIp = null;
            try
            {
                lPerIp = IPAddress.Parse(m_leftPerceptIp);
            }
            catch (FormatException)
            {
                Debug.LogError(
                    "Bad IP address given for left percept port: " +
                    m_leftPerceptIp + ", broadcasting instead.");
                lPerIp = IPAddress.Broadcast;
            }
            ms_leftBroadcastAddr = new IPEndPoint(lPerIp, ms_leftPerceptPort);
            
            //Check to see if vMPL Percepts are enabled
            if (m_logLeftPerceptsEnabled)
            {
                Debug.Log("Left Percepts Enabled - Sending to IP " + lPerIp.ToString());
            }
            else
            {
                Debug.Log("Left Percepts Disabled");
            }//if - check to see if percepts reported is enabled

            #endregion //Left MPL Percept Stream
                        
            #endregion //Percept Initialization


            //Initialize VulcanX Timing
            //This will be a function of the Unity Time - will then perform a check/modify to ensure that it runs properly with the VulcanX clock
            #region VulcanX Timing
            ms_numUpdatesPerVulcanXMsg = (int)(0.02f / Time.fixedDeltaTime + 0.5f); //200 Hz for FixedDeltaTime
            float check = 0.02f / ms_numUpdatesPerVulcanXMsg;
            const float TOLERANCE = 0.00001f;
            if (check > Time.fixedDeltaTime + TOLERANCE ||
                check < Time.fixedDeltaTime - TOLERANCE)
            {
                throw new ApplicationException(
                    "Fixed Timestep must be a factor of 1/50.");
            }
            #endregion //VulcanX Timing

            #endregion //VULCANX COMMUNICATION


            //MPL SETUP
            #region MPL SETUP

            //Initialize Bimanual Collisions (default values)
            #region Bimanual Collision
            //Bimanual Collisions Setting
            if (m_haveLeftMPL && m_haveRightMPL)
            {
                //Default Setting is to turn collisions off
                try
                {
                    Physics.IgnoreLayerCollision(GameObject.Find("lPalm").layer, GameObject.Find("rPalm").layer, true);
                }
                catch
                {
                    //Both left and right vMPL prefabs might not be loaded into scene
                    Debug.Log("Could not find both right and left vMPL prefabs in scene");
                }//try - make sure that both prefabs are present in scene

            }//if - check to make sure both right and left MPL are present
            #endregion //Bimanual Collision


            //Output Filter Value
            #region Debug log message showing filter values used.
            Debug.Log("Arm filter numerator: " +
                m_armFilterNum[0].ToString() + ", " +
                m_armFilterNum[1].ToString() + ", " +
                m_armFilterNum[2].ToString() + ", " +
                m_armFilterNum[3].ToString() + ", " +
                m_armFilterNum[4].ToString() + "\nArm filter denominator: " +
                m_armFilterDen[0].ToString() + ", " +
                m_armFilterDen[1].ToString() + ", " +
                m_armFilterDen[2].ToString() + ", " +
                m_armFilterDen[3].ToString() + ", " +
                m_armFilterDen[4].ToString() + "\nWrist filter numerator: " +
                m_wristFilterNum[0].ToString() + ", " +
                m_wristFilterNum[1].ToString() + ", " +
                m_wristFilterNum[2].ToString() + ", " +
                m_wristFilterNum[3].ToString() + ", " +
                m_wristFilterNum[4].ToString() + "\nWrist filter denominator: " +
                m_wristFilterDen[0].ToString() + ", " +
                m_wristFilterDen[1].ToString() + ", " +
                m_wristFilterDen[2].ToString() + ", " +
                m_wristFilterDen[3].ToString() + ", " +
                m_wristFilterDen[4].ToString() + "\nFinger filter numerator: " +
                m_fingerFilterNum[0].ToString() + ", " +
                m_fingerFilterNum[1].ToString() + ", " +
                m_fingerFilterNum[2].ToString() + ", " +
                m_fingerFilterNum[3].ToString() + ", " +
                m_fingerFilterNum[4].ToString() + "\nArm filter denominator: " +
                m_fingerFilterDen[0].ToString() + ", " +
                m_fingerFilterDen[1].ToString() + ", " +
                m_fingerFilterDen[2].ToString() + ", " +
                m_fingerFilterDen[3].ToString() + ", " +
                m_fingerFilterDen[4].ToString());
            #endregion


            //Initialize MPL Objects
            #region MPL Objects Initialization
            // Find GameObjects.
            AssignRightMPLGameObjects();
//            AssignLeftMPLGameObjects();
                                                        
            #endregion //MPL Objects Initialization


            //Physics Properties
            #region Physics Property Assginment

            // Initialize Hinges
            #region Hinge Properties
//            SetupLeftMPLHingeJoints();
            SetupRightMPLHingeJoints();
            #endregion //Hinge Properties

            //AssignPhysicsValuesToVMPL();

            #endregion Physics Property Assginment


            //Collision Detection Type
            #region Collision Detection Type Property Assginment
            //Set the Collision Detection Mode
            InitializevMPLCollisionType();
            #endregion //Collision Detection Type Property Assginment


            //Color
            #region Color the vMPL
//            InitializevMPLColor();
            #endregion //Color the vMPL


            //Physic Material
            #region Physic Material
//            InitializevMPLPhysicMaterials();

            #endregion //Physic Material


            #endregion //MPL SETUP


        } // end if (!ms_initialized)
        else
        {
            Debug.LogWarning(
                "Multiple instances of VulcanXInterface script in use.");

        }//if - check to make sure only single instance of VulcanXInterface is loaded/running

    }//function - Awake


    /// <summary>
    /// Is called at the beginning of program, will instantial objects
    /// </summary>
    void Start()
    {
        //m_worldIface = WorldInterface.Instance();

        
    }//function - Start


    /// <summary>
    /// Call after a scenario reset (Application.LoadLevel() ).  This rebuilds
    /// the references to the vMPL joints, resets the PID values, and then
    /// commands the arm to return to its pre-reset position.
    /// </summary>
    public void Reset()
    {
        //Reset the MPL Objects
        AssignRightMPLGameObjects();
        if (m_haveRightMPL)
        {
            CommandRightArm();
        }//if - check for limb

        AssignLeftMPLGameObjects();
        if (m_haveLeftMPL)
        {
            CommandLeftArm();
        }//if - check for limb

    }//function - Reset


    /// <summary>
    /// Called once before each rendered frame.
    /// </summary>
    void Update()
    {
        //WIF
        #region World Interface
        // Continue to remove VulcanX packets while paused.
//        if (m_worldIface.Paused)
//        {
//            ReadRightPort();
//            ReadLeftPort();
//        }

        #endregion //World Interface

        //Keystrokes
        #region Keystrokes

        //COLLISIONS 
        #region B - Collision between left and right
        //COLLISIONS (Left/Right vMPL)
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (m_haveLeftMPL && m_haveRightMPL)
            {

                try
                {
                    //Toggle the Enabling of Collisions between left and right vMPLs
                    if (Physics.GetIgnoreLayerCollision(GameObject.Find("lPalm").layer, GameObject.Find("rPalm").layer))
                    {
                        //Turn collisions on (disable ignore)
                        Physics.IgnoreLayerCollision(GameObject.Find("lPalm").layer, GameObject.Find("rPalm").layer, false);

                    }
                    else
                    {
                        //Turn collisions off (ignore)
                        Physics.IgnoreLayerCollision(GameObject.Find("lPalm").layer, GameObject.Find("rPalm").layer, true); //IGNORE

                    }//if - check for collisions on/off 
                }
                catch
                {
                    //Both limbs are not present
                }//try - check for both limbs
            
            }//if - check for both limbs

        }//if - Check for KeyDown (B)
        #endregion B


        //GRAVITY (MPL)
        #region G - Gravity
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            //bool b_showColor = true; //show the property change with color change

            //b_Gravity = !b_Gravity;

            ////Toggles the current gravity mode (color change will show which
            //if (m_haveRightMPL)
            //{
            //    AssignGravityToVMPL(b_Gravity, b_showColor); //currently green
            //}
            //else
            //{
            //    AssignGravityToVMPL(b_Gravity, b_showColor);
            //}//if - test for arm availability

        }//if - Check for KeyDown (G)

        #endregion //G - Gravity


        //DAMPER 
        #region D - Damper
        if (Input.GetKeyDown(KeyCode.D))
        {
            //bool b_showColor = true; //show the property change with color change

            //if (f_Damping == 0)
            //{
            //    f_Damping = 1e15f;
            //}//assign Damping
            //else
            //{
            //    f_Damping = 0;
            //}//
            
            ////Toggles the current gravity mode (color change will show which
            //AssignDampingToVMPL(f_Damping, b_showColor); //currently green
            
        }//if - Check for KeyDown (D)
        #endregion //D - Damping

        
        //COLLISIONS DETECTION MODE
        #region V - Collision Detection Type
        if (Input.GetKeyDown(KeyCode.V))
        {
            //bool b_showColor = true; //show the property change with color change
            //CollisionDetectionMode coldectmode_Temp = CollisionDetectionMode.Discrete; //Define default mode if determination of current mode fails

            ////Determine the current collision detection method, and from that cycle to next in line (because there are 3 types)
            //if (m_haveRightMPL)
            //{
            //    coldectmode_Temp = ms_rPalm.rigidbody.collisionDetectionMode;
            //}
            //else
            //{
            //    //right arm not in play, use left
            //    coldectmode_Temp = ms_lPalm.rigidbody.collisionDetectionMode;
            //}//if - test for arm availability


            //if (coldectmode_Temp == CollisionDetectionMode.Discrete)
            //{
            //    //Toggle/rotate between continous dynamic, continuous, discrete
            //    AssignCollisionTypeToVMPL(CollisionDetectionMode.Continuous, b_showColor);

            //}
            //else if (coldectmode_Temp == CollisionDetectionMode.Continuous)
            //{
            //    //Toggle/rotate between continous dynamic, continuous, discrete
            //    AssignCollisionTypeToVMPL(CollisionDetectionMode.ContinuousDynamic, b_showColor);

            //}
            //else if (coldectmode_Temp == CollisionDetectionMode.ContinuousDynamic)
            //{
            //    //Toggle/rotate between continous dynamic, continuous, discrete
            //    AssignCollisionTypeToVMPL(CollisionDetectionMode.Discrete, b_showColor);
                
            //}//if - test for current type, then rotate


        }//if - Check for KeyDown (V)
        #endregion //V - Collision Detection Type


        //COLLISIONS DETECTION MODE
        #region Period - Show/Hide Cursor

        if (Input.GetKeyDown(KeyCode.Period))
        {
            //Toggle Cursor
            Cursor.visible = !Cursor.visible;

        }//if - Check for KeyDown (Period)

        #endregion //Period - Show/Hide Cursor


        //Quit
        #region Esc

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //Toggle Cursor
            Application.Quit();

        }//if - Check for KeyDown (Period)

        #endregion //Period - Show/Hide Cursor



        #region Commented Out Code
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.H))
        {
            SetRightUpperArmAngles(new float[7] { 0f, 0f, 0f, 90f, 0f, 0f, 0f });
        }
        else if (Input.GetKey(KeyCode.D))
        {
            SetRightUpperArmAngles(new float[7] { 0f, 0f, 0f, 0f, 0f, 0f, 0f });
        }
        else
        if (Input.GetKeyDown(KeyCode.F12))
        {
            //Debug.Log("Saving screenshot to capture" + Time.frameCount.ToString() + ".png");
            //Application.CaptureScreenshot("capture" + Time.frameCount.ToString() + ".png");
        }
#endif
        #endregion //Commented Out Code

        #endregion //Keystrokes

    }//function - Update


    /// <summary>
    /// Called before each physics time step.  Because VulcanX command
    /// messages should be serviced at 50Hz, the physics time step should be
    /// a multiple of 1/50s.
    /// </summary>
    void FixedUpdate()
    {


        if (m_haveRightMPL)
        {
            //SendFakePercepts();
            SendRightPercepts();
            //ReadRightPort();
            CommandRightArm();
        }

        if (m_haveLeftMPL)
        {
            //SendLeftPercepts();
            //ReadLeftPort();
            CommandLeftArm();
        }


        #region UDP Commands
        /*
        if (Time.fixedTime > ms_lastUpdate)
        {
            ms_lastUpdate = Time.fixedTime;

            // Read VulcanX port and send percept data at 50Hz.
            //if (Time.fixedTime >= ms_lastRead + 0.02f)
            //{
            //    ms_lastRead = Time.fixedTime;
            if (m_updateCounter == 0)
            {
                if (m_haveRightMPL)
                {
                    //SendFakePercepts();
                    //SendRightPercepts();
                    //ReadRightPort();
                    CommandRightArm();
                }

                if (m_haveLeftMPL)
                {
                    //SendLeftPercepts();
                    //ReadLeftPort();
                    CommandLeftArm();
                }
                m_updateCounter++;
                //m_update = false;
            }
            else
            {
                if (m_haveRightMPL)
                    CommandRightArm();
                if (m_haveLeftMPL)
                    CommandLeftArm();
                m_updateCounter++;
                if (m_updateCounter >= ms_numUpdatesPerVulcanXMsg)
                {
                    m_updateCounter = 0;
                    //m_update = true;
                }
            }

            //}
        }
        */
        #endregion //UDP Commands

    }//function - FixedUpdate

    #endregion //Unity3D Functions


    //---------------------------------------
    // FUNCTIONS - INITIALIZATION
    //---------------------------------------
    #region Initialization and Assignment Functions (Percept Data, GameObjects, Object Properties)

    #region Commented Out Code - Public Enum Joints
#if UNITY_EDITOR
    //public enum Joints
    //{
    //    ShoulderFE,
    //    ShoulderAA,
    //    HumeralRot,
    //    ElbowFE,
    //    WristRot,
    //    WristDev,
    //    WristFE
    //}

    //protected Joints m_currentDebugJoint;
    //public PIDValues m_debugJointPID;
    //public string m_debugJointName;


    //private void SetCurrentDebugJoint()
    //{
    //    switch (m_currentDebugJoint)
    //    {
    //        case Joints.ShoulderFE:
    //            m_debugJointPID = ms_rShoulderFEPid;
    //            m_debugJointName = "ShoulderFE";
    //            break;
    //        case Joints.ShoulderAA:
    //            m_debugJointPID = ms_rShoulderAAPid;
    //            m_debugJointName = "ShoulderAA";
    //            break;
    //        case Joints.HumeralRot:
    //            m_debugJointPID = ms_rHumeralRotPid;
    //            m_debugJointName = "HumeralRot";
    //            break;
    //        case Joints.ElbowFE:
    //            m_debugJointPID = ms_rElbowFEPid;
    //            m_debugJointName = "ElbowFE";
    //            break;
    //        case Joints.WristRot:
    //            m_debugJointPID = ms_rWristRotPid;
    //            m_debugJointName = "WristRot";
    //            break;
    //        case Joints.WristDev:
    //            m_debugJointPID = ms_rWristDevPid;
    //            m_debugJointName = "WristDev";
    //            break;
    //        case Joints.WristFE:
    //            m_debugJointPID = ms_rWristFEPid;
    //            m_debugJointName = "WristFE";
    //            break;
    //    }
    //}

#endif
    #endregion // Commented Out Code - Public Enum Joints


    //XML READER FUNCTIONS
    #region XML Reader Methods

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
            Debug.Log("Using VulcanXInterface config: " + m_CONFIG_FILENAME);
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
                            //PERCEPTS SETTINGS
                            #region Percepts Settings

                            //PERCEPTS IP SETTINGS
                            #region Percepts IP Setting (rightperceptsip, leftperceptsip)
                            case "rightperceptsip":
                                if (reader.Read())
                                {
                                    string ip = ReadString(reader);
                                    if (!string.IsNullOrEmpty(ip))
                                        m_rightPerceptIp = ip;
                                }
                                break;
                            case "leftperceptsip":
                                if (reader.Read())
                                {
                                    string ip = ReadString(reader);
                                    if (!string.IsNullOrEmpty(ip))
                                        m_leftPerceptIp = ip;
                                }
                                break;
                            #endregion //Percepts IP Setting

                            //PERCEPTS ENABLE SETTINGS
                            #region Log Percepts Enable/Disable (logrightpercepts, logleftpercepts)
                            case "logrightpercepts":
                                if (reader.Read())
                                {
                                    string str_percepts_flag = ReadString(reader);
                                    if (!string.IsNullOrEmpty(str_percepts_flag))
                                    {
                                        if (str_percepts_flag.ToLower() == "true")
                                        {
                                            m_logRightPerceptsEnabled = true;
                                        }
                                        else
                                        {
                                            m_logRightPerceptsEnabled = false;
                                        }
                                    }//if - check for valid string
                                    
                                    //Debug.Log("Right Percepts: " + str_percepts_flag + ", " + m_logRightPerceptsEnabled);

                                }//case - Flag for turning on/off Percepts - Right MPL
                                break;
                            case "logleftpercepts":
                                if (reader.Read())
                                {
                                    string str_percepts_flag = ReadString(reader);
                                    if (!string.IsNullOrEmpty(str_percepts_flag))
                                    {
                                        if (str_percepts_flag.ToLower() == "true")
                                        {
                                            m_logLeftPerceptsEnabled = true;
                                        }
                                        else
                                        {
                                            m_logLeftPerceptsEnabled = false;
                                        }
                                    }//if - check for valid string

                                    //m_logLeftPerceptsEnabled = XMLReadBoolean(reader);
                                    //Debug.Log("Left Percepts: " + str_percepts_flag + ", " + m_logLeftPerceptsEnabled);

                                }//case - Flag for turning on/off Percepts - Left MPL
                                break;
                            #endregion //Log Percepts Enable/Disable

                            #endregion //Percepts Settings

                            //MPL SETTINGS
                            #region MPL Settings (mplenableright, mplenableleft, mpllocationright, mpllocationleft, showendpointright, showendpointleft, showworkspaceright, showworkspaceleft, bimanualcollisions)

                            //MPL Enable/Disable (turn on/off right/left arm)
                            #region MPL Enable/Disable (turn on/off right/left arm at start-up)
                            case "mplenableright":
                                if (reader.Read())
                                {
                                    string str_percepts_flag = ReadString(reader);
                                    if (!string.IsNullOrEmpty(str_percepts_flag))
                                    {
                                        if (str_percepts_flag.ToLower() == "true")
                                        {

                                            try
                                            {
                                                //Enable/Disable MPL
                                                //GameObject.Find("rMPL Prefab").SetActiveRecursively(true);//.active = true;
                                                GameObject.Find(MPL_PREFAB_NAME_RIGHT).SetActive(true);
                                                //Will enable Endpoint and LimbWorkspace, which can be enabled/disabled later in config file

                                                //Turn on/off Right MPL in VulcanXInterface
                                                m_haveRightMPL = true;
                                            }
                                            catch
                                            {
                                                //unable to find MPL
                                            }//try - attempt to read MPL position and assign
                                        }
                                        else
                                        {
                                            try
                                            {
                                                //Enable/Disable MPL
                                                //GameObject.Find("rMPL Prefab").SetActiveRecursively(false);//.active = false;
                                                GameObject.Find(MPL_PREFAB_NAME_RIGHT).SetActive(false);//.active = false;

                                                //Turn on/off Right MPL in VulcanXInterface

                                                //??check if other MPL is turned off - if so, keep this one
                                                m_haveRightMPL = false;
                                            }
                                            catch
                                            {
                                                //unable to find MPL
                                            }//try - attempt to read MPL position and assign
                                        }//if - check for valid string

                                        Debug.Log("MPL Right Enable: " + m_haveRightMPL.ToString());

                                    }//if - valid string

                                }//if - reader
                                break;
                            case "mplenableleft": 
                                if (reader.Read())
                                {
                                    string str_percepts_flag = ReadString(reader);
                                    if (!string.IsNullOrEmpty(str_percepts_flag))
                                    {
                                        if (str_percepts_flag.ToLower() == "true")
                                        {

                                            try
                                            {
                                                //Enable/Disable MPL
                                                //.Find("lMPL Prefab").SetActiveRecursively(true);// = true;
                                                GameObject.Find(MPL_PREFAB_NAME_LEFT).SetActive(true);// = true;

                                                //Turn on/off Left MPL in VulcanXInterface
//                                                m_haveLeftMPL = true;
                                            }
                                            catch
                                            {
                                                //unable to find MPL
                                            }//try - attempt to read MPL position and assign
                                        }
                                        else
                                        {
                                            try
                                            {
                                                //check if other MPL is turned off - if so, keep this one
                                                if (m_haveRightMPL)
                                                {
                                                    //Enable/Disable MPL
                                                    //GameObject.Find("lMPL Prefab").SetActiveRecursively(false);//.active = false;
                                                    GameObject.Find(MPL_PREFAB_NAME_LEFT).SetActive(false);//.active = false;

                                                    //Turn on/off Left MPL in VulcanXInterface
                                                    m_haveLeftMPL = false;

                                                }//if - check if other MPL present, if so, then can turn this one off
                                                else
                                                {
                                                    //Right limb enable is first in config file - will be presented first
                                                    Debug.LogWarning("Attempting to deactivate both right AND left MPLs - leaving left MPL active");

                                                    //Enable/Disable MPL
                                                    GameObject.Find(MPL_PREFAB_NAME_LEFT).SetActive(true);// = true;

                                                    //Turn on/off Left MPL in VulcanXInterface
//                                                    m_haveLeftMPL = true;

                                                }//if - check to see if both MPLs were set to deactivate, if so leave one on
                                            }
                                            catch
                                            {
                                                //unable to find MPL
                                            }//try - attempt to read MPL position and assign
                                        }//if - check for valid string

                                        Debug.Log("MPL Left Enable: " + m_haveLeftMPL.ToString());

                                    }//if - valid string

                                }//if - reader
                                break;

                            #endregion //MPL Location (at start-up)

                            //MPL Location
                            #region MPL Location (at start-up)
                            case "mpllocationright": //multiples

                                try
                                {
                                    //Read the MPL Position Value
                                    Vector3 v3_Position = XMLReadVector3(reader.ReadSubtree());

                                    //Move the MPL position to read value
                                    GameObject.Find(MPL_PREFAB_NAME_RIGHT).transform.position = v3_Position;
                                }
                                catch
                                {
                                    //unable to read vector 3 or find MPL
                                }//try - attempt to read MPL position and assign
                                break;
                            case "mpllocationleft": //multiples

                                try
                                {
                                    //Read the MPL Position Value
                                    Vector3 v3_Position = XMLReadVector3(reader.ReadSubtree());

                                    //Move the MPL position to read value
                                    GameObject.Find(MPL_PREFAB_NAME_LEFT).transform.position = v3_Position;
                                }
                                catch
                                {
                                    //unable to read vector 3 or find MPL
                                }//try - attempt to read MPL position and assign
                                break;
                            #endregion //MPL Location (at start-up)

                            //Endpoint Visibility
                            #region Show/Hide Endpoint (showendpointright, showendpointleft)
                            case "showendpointright":
                                if (reader.Read())
                                {
                                    //Will set whether the left and right arms can collide with one another
                                    string str_flag = ReadString(reader);
                                    if (!string.IsNullOrEmpty(str_flag))
                                    {
                                        if (str_flag.ToLower() == "true")
                                        {
                                            //Show Endpoint
                                            try
                                            {
                                                //GameObject.Find("lPalm/Endpoint").active = true;
                                                GameObject.Find("rPalm/Endpoint").SetActive(true);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                            }//try - attempt to show/hide

                                        }
                                        else
                                        {
                                            //Hide Endpoint
                                            try
                                            {
                                                //GameObject.Find("lPalm/Endpoint").active = false;
                                                GameObject.Find("rPalm/Endpoint").SetActive(false);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                            }//try - attempt to show/hide

                                        }//if - check for bool

                                    }//if - check for valid string

                                }//case - Flag for turning on/off Endpoint - Right MPL
                                break;
                            case "showendpointleft":
                                if (reader.Read())
                                {
                                    //Will set whether the left and right arms can collide with one another
                                    string str_flag = ReadString(reader);
                                    if (!string.IsNullOrEmpty(str_flag))
                                    {
                                        if (str_flag.ToLower() == "true")
                                        {
                                            //Show Endpoint
                                            try
                                            {
                                                GameObject.Find("lPalm/Endpoint").SetActive(true);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                            }//try - attempt to show/hide

                                        }
                                        else
                                        {
                                            //Hide Endpoint
                                            try
                                            {
                                                GameObject.Find("lPalm/Endpoint").SetActive(false);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                            }//try - attempt to show/hide

                                        }//if - check for bool

                                    }//if - check for valid string

                                }//case - Flag for turning on/off Endpoint - Left MPL
                                break;
                            #endregion //Show/Hide Endpoint

                            //Limb Workspace Visibility (clear halo around arm)
                            #region Show/Hide Limb Workspace (showworkspaceright, showworkspaceleft)
                            case "showlimbworkspaceright":
                                if (reader.Read())
                                {
                                    //Will set whether limb workspace is visible
                                    string str_flag = ReadString(reader);
                                    if (m_haveRightMPL && !string.IsNullOrEmpty(str_flag))
                                    {
                                        if (str_flag.ToLower() == "true")
                                        {
                                            //Show 
                                            try
                                            {
                                                GameObject.Find(MPL_PREFAB_NAME_RIGHT + "/LimbWorkspace/ID2").SetActive(true);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                                //Debug.Log("Fail in assignment - LimbWorkspace R: " + str_flag);                                    
                                            }//try - attempt to show/hide 

                                        }
                                        else
                                        {
                                            //Hide 
                                            try
                                            {
                                                GameObject.Find(MPL_PREFAB_NAME_RIGHT + "/LimbWorkspace/ID2").SetActive(false);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                                //Debug.Log("Fail in assignment - LimbWorkspace R: " + str_flag);
                                        }//try - attempt to show/hide 

                                        }//if - check for bool

                                    }//if - check for valid string

                                }//case - Flag for turning on/off Limb Workspace - Right MPL
                                break;
                            case "showlimbworkspaceleft":
                                if (reader.Read())
                                {
                                    //Will set whether limb workspace is visible
                                    string str_flag = ReadString(reader);
                                    if (m_haveLeftMPL && !string.IsNullOrEmpty(str_flag))
                                    {
                                        if (str_flag.ToLower() == "true")
                                        {
                                            //Show 
                                            try
                                            {
                                                GameObject.Find(MPL_PREFAB_NAME_LEFT + "/LimbWorkspace/ID2").SetActive(true);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                                //Debug.Log("Fail in assignment - LimbWorkspace L: " + str_flag);
                                            }//try - attempt to show/hide

                                        }
                                        else
                                        {
                                            //Hide 
                                            try
                                            {
                                                GameObject.Find(MPL_PREFAB_NAME_LEFT + "/LimbWorkspace/ID2").SetActive(false);
                                            }
                                            catch
                                            {
                                                //MPL R/L Limb not in scene
                                                //Debug.Log("Fail in assignment - LimbWorkspace L: " + str_flag);
                                            }//try - attempt to show/hide

                                        }//if - check for bool

                                    }//if - check for valid string

                                }//case - Flag for turning on/off Limb Workspace - Left MPL
                                break;
                            #endregion //Show/Hide Limb Workspace
                                
                            //MPL Physiscs
                            #region MPL Physics
                            case "bimanualcollisions":
                                if (reader.Read())
                                {
                                    if (m_haveLeftMPL && m_haveRightMPL)
                                    {
                                        //Try to see if objects present
                                        try
                                        {

                                            //Will set whether the left and right arms can collide with one another
                                            string str_flag = ReadString(reader);
                                            if (!string.IsNullOrEmpty(str_flag))
                                            {
                                                if (str_flag.ToLower() == "true")
                                                {
                                                    //Turn collisions on (disable ignore)
                                                    Physics.IgnoreLayerCollision(GameObject.Find("lPalm").layer, GameObject.Find("rPalm").layer, false);

                                                }
                                                else
                                                {
                                                    //Turn collisions off (ignore)
                                                    Physics.IgnoreLayerCollision(GameObject.Find("lPalm").layer, GameObject.Find("rPalm").layer, true); //IGNORE

                                                }//if - check for collisions on/off

                                            }//if - check for valid string

                                        }
                                        catch
                                        {
                                            //Objects not present
                                        }//catch - try for object reference
                                    }//if - check for both right and left MPL

                                }//case - Flag for changing properties - collision enabled for Left/Right vMPL
                                break;

                            case "bimanualcollisiondetectionmethod":
                                if (reader.Read())
                                {
                                    //Will assign property to vMPL components (either/both, independent of bimanual)
                                    if (m_haveLeftMPL || m_haveRightMPL)
                                    {
                                        //Try to see if objects present
                                        try
                                        {

                                            //Will set whether the left and right arms can collide with one another
                                            string str_flag = ReadString(reader);
                                            if (!string.IsNullOrEmpty(str_flag))
                                            {
                                                //This property cannot be assigned in prior to "Start" being called, 
                                                //  therefore property will have to be assigned in Start function, so flag set here
                                                str_MPL_CollisionDetectionMode = str_flag;

                                            }//if - check for valid string

                                        }
                                        catch
                                        {
                                            //Objects not present
                                        }//catch - try for object reference

                                    }//if - check for both right and left MPL

                                }//case - Flag for changing properties - collision detection type
                                break;

                            #endregion //MPL Physics

                            //MPL CONTROLLER SETTINGS
                            #region MPL Control Settings (filterarmnumerator, filterarmdenominator, filterwristnumerator, filterwristdenominator, filterfingernumerator, filterfingerdenominator)
                            case "filterarmnumerator":
                                ReadFilterValues(reader, m_armFilterNum);
                                break;
                            case "filterarmdenominator":
                                ReadFilterValues(reader, m_armFilterDen);
                                break;
                            case "filterwristnumerator":
                                ReadFilterValues(reader, m_wristFilterNum);
                                break;
                            case "filterwristdenominator":
                                ReadFilterValues(reader, m_wristFilterDen);
                                break;
                            case "filterfingernumerator":
                                ReadFilterValues(reader, m_fingerFilterNum);
                                break;
                            case "filterfingerdenominator":
                                ReadFilterValues(reader, m_fingerFilterDen);
                                break;
                            #endregion //MPL Control Settings

                            //MPL Grasp Logic
                            #region Grasp Logic
                            case "grasplogic": //multiples
                                XMLReadGraspLogic(reader.ReadSubtree());
                                break;
                            #endregion //Grasp Logic

                            #endregion //MPL Settings

                            //USER INTERFACE
                            #region User Interface
                            
                            // Cursor Interface
                            case "showcursor": //multiples
                                if (reader.Read())
                                {
                                    //Will set whether to show cursor on screen
                                    string str_flag = ReadString(reader);
                                    if (!string.IsNullOrEmpty(str_flag))
                                    {
                                        if (str_flag.ToLower() == "true")
                                        {
                                            //Show Cursor
                                            Cursor.visible = true;
                                        }
                                        else
                                        {
                                            //Hide Cursor
                                            Cursor.visible = false;

                                        }//if - check for cursor on/off

                                    }//if - check for valid string

                                }//case - Flag for turning on/off Percepts - Left MPL
                                break;
                            #endregion //User Interface

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


    /// <summary>
    /// Read the PID Controller Filter values for the Hinge settings on the MPL
    /// </summary>
    static private char[] ms_commaSeparator = new char[] { ',' };
    private static void ReadFilterValues(XmlReader reader, float[] filter)
    {
        try
        {
            if (reader.Read())
            {
                string allValues = reader.ReadContentAsString();
                if (!string.IsNullOrEmpty(allValues))
                {
                    string[] values = allValues.Split(
                        ms_commaSeparator, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < values.Length && i < filter.Length; i++)
                    {
                        try
                        {
                            filter[i] = float.Parse(values[i]);
                        }
                        catch (OverflowException)
                        {
                            Debug.LogError("Overflow parsing filter value in " +
                                m_CONFIG_FILENAME);
                        }
                        catch (FormatException)
                        {
                            Debug.LogError("Error parsing individual filter value in " +
                                m_CONFIG_FILENAME);
                        }
                    }
                }
            } // end if
        }
        catch (InvalidCastException)
        {
            Debug.LogError(
                "Error reading filter string from " + m_CONFIG_FILENAME);
        }
        catch (FormatException)
        {
            Debug.LogError(
                "Error reading filter string from " + m_CONFIG_FILENAME);
        }
        catch (XmlException ex)
        {
            Debug.LogError("Problem reading filter value from " + m_CONFIG_FILENAME +
                "\n" + ex.Message);
        }
    }//function - ReadFilterValues
        
    /// <summary>
    /// XML reader - Positions are in centimeters.
    /// </summary>
    /// <param name="reader"></param>
    private void XMLReadGraspLogic(XmlReader xmlReader)
    {
        //Variables for Grasp Logic
        #region Struct Definitions
        
        bool b_requiresThumbOrPalm = true;
        bool b_requiresThumb = false;
        bool b_requiresPalm = false;
        bool b_requiresFinger = true;
        int int_requiresNProximalContacts = 0;
        int int_requiresNDistalContacts = 0;
        int int_requiresNMedialContacts = 0;
        bool b_requiresContactDirectionality = false;

        #endregion //Struct Definitions


        //XML Reading
        #region Import Struct from XML
        while (xmlReader.Read())
        {
            switch (xmlReader.NodeType)
            {
                case XmlNodeType.Element:
                    switch (xmlReader.Name.ToLower())
                    {
                        case "requiresthumborpalm":
                            if (xmlReader.Read())
                            {
                                string str_flag = ReadString(xmlReader);
                                if (!string.IsNullOrEmpty(str_flag))
                                {
                                    if (str_flag.ToLower() == "true")
                                    {
                                        b_requiresThumbOrPalm = true;
                                    }
                                    else
                                    {
                                        b_requiresThumbOrPalm = false;
                                    }//if - check for valid string
                                }//if - valid string
                            }//if - reader
                            break;
                        case "requiresthumb":
                            if (xmlReader.Read())
                            {
                                string str_flag = ReadString(xmlReader);
                                if (!string.IsNullOrEmpty(str_flag))
                                {
                                    if (str_flag.ToLower() == "true")
                                    {
                                        b_requiresThumb = true;
                                    }
                                    else
                                    {
                                        b_requiresThumb = false;
                                    }//if - check for valid string
                                }//if - valid string
                            }//if - reader
                            break;
                        case "requirespalm":
                            if (xmlReader.Read())
                            {
                                string str_flag = ReadString(xmlReader);
                                if (!string.IsNullOrEmpty(str_flag))
                                {
                                    if (str_flag.ToLower() == "true")
                                    {
                                        b_requiresPalm = true;
                                    }
                                    else
                                    {
                                        b_requiresPalm = false;
                                    }//if - check for valid string
                                }//if - valid string
                            }//if - reader
                            break;
                        case "requiresfinger":
                            if (xmlReader.Read())
                            {
                                string str_flag = ReadString(xmlReader);
                                if (!string.IsNullOrEmpty(str_flag))
                                {
                                    if (str_flag.ToLower() == "true")
                                    {
                                        b_requiresFinger = true;
                                    }
                                    else
                                    {
                                        b_requiresFinger = false;
                                    }//if - check for valid string
                                }//if - valid string
                            }//if - reader
                            break;
                        case "requiresnproximalcontacts":
                            int_requiresNProximalContacts = XMLReadInteger(xmlReader);
                            break;
                        case "requiresndistalcontacts":
                            int_requiresNDistalContacts = XMLReadInteger(xmlReader);
                            break;
                        case "requiresnmedialcontacts":
                            int_requiresNMedialContacts = XMLReadInteger(xmlReader);
                            break;
                        case "requirescontactdirectionality":
                            if (xmlReader.Read())
                            {
                                string str_flag = ReadString(xmlReader);
                                if (!string.IsNullOrEmpty(str_flag))
                                {
                                    if (str_flag.ToLower() == "true")
                                    {
                                        b_requiresContactDirectionality = true;
                                    }
                                    else
                                    {
                                        b_requiresContactDirectionality = false;
                                    }//if - check for valid string
                                }//if - valid string
                            }//if - reader
                            break;
                    }//switch - Node Element
                    break;
                case XmlNodeType.EndElement:
                    break;

            }//switch - Node Type

        }//while - building struct, traversing nodes
        #endregion //Import Struct from XML

        //When finished reading the grasp logic, assign in a single call to each object with a GraspableObject script attached

        //Find all instances of GraspableObject script (on objects)
        #region Assign Grasp Logic
        //GraspableObject[] o_GraspableObjectScripts = FindObjectsOfType(typeof(GraspableObject)) as GraspableObject[]; //Reaches all ACTIVE objects
        GraspableObject[] o_GraspableObjectScripts = Resources.FindObjectsOfTypeAll(typeof(GraspableObject)) as GraspableObject[]; //Reaches all ACTIVE and INACTIVE objects

        if (o_GraspableObjectScripts.Length > 0)
        {
            Debug.Log("Setting Grasp Logic for Graspable Objects: n=" + o_GraspableObjectScripts.Length);

            for (int ii = 0; ii < o_GraspableObjectScripts.Length; ii++)
            {
                //setGraspLogic(b_requiresThumbOrPalm, b_requiresThumb, b_requiresPalm, b_requiresFinger, int_requiresContactDirectionality, int_requiresNProximalContacts, int_requiresNMedialContacts, b_requiresNDistalContacts);
                o_GraspableObjectScripts[ii].setGraspLogicPublic(b_requiresThumbOrPalm, b_requiresThumb, b_requiresPalm, b_requiresFinger, int_requiresNProximalContacts, int_requiresNMedialContacts, int_requiresNDistalContacts, b_requiresContactDirectionality);

            }//for - traverse each script (attached to an object)

        }//if - check for more than 0 objects
        #endregion //Assign Grasp Logic

    }//function -XMLReadGraspLogic

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

    #endregion //XML Reader Methods


    //VULCANX COMMUNICATION
    #region Percept Buffer Initialization

    /// <summary>
    /// Will Initialize the Percept stream (set default values that
    /// aren't normally set, will save computation time)
    /// </summary>
    private void InitializePerceptData()
    {
        //int len = 324 + Enum.GetValues(typeof(SensorArray.CONTACT_SENSOR_ID)).Length * sizeof(short) + Enum.GetValues(typeof(SensorArray.SegmentPerceptFTSNIdType)).Length * 3 * sizeof(float);
        int len = 324 + Enum.GetValues(typeof(SensorArray.CONTACT_SENSOR_ID)).Length * sizeof(short) + Enum.GetValues(typeof(SensorArray.SegmentPerceptFTSNIdType)).Length * NUMBER_FTSN_PADS * sizeof(float); //324 + 74 + 280

        //Debug.Log("Length of percepts array: " + len.ToString() + " (324 + " + (Enum.GetValues(typeof(SensorArray.CONTACT_SENSOR_ID)).Length * sizeof(short)).ToString() + " + " + (Enum.GetValues(typeof(SensorArray.SegmentPerceptFTSNIdType)).Length * NUMBER_FTSN_PADS * sizeof(float)).ToString() + ")");

        ms_rPerceptData = new byte[len];    //pvt percepts: 0-323,  followed by seg percepts
        ms_lPerceptData = new byte[len];

        byte[] nan = BitConverter.GetBytes(float.NaN);

        // Unity3D doesn't provide torque data, so assign NaN to all torque 
        // fields.
        for (int i = 8; i < 324; i += 12)
        {
            ms_rPerceptData[i] = nan[0];
            ms_rPerceptData[i + 1] = nan[1];
            ms_rPerceptData[i + 2] = nan[2];
            ms_rPerceptData[i + 3] = nan[3];

            ms_lPerceptData[i] = nan[0];
            ms_lPerceptData[i + 1] = nan[1];
            ms_lPerceptData[i + 2] = nan[2];
            ms_lPerceptData[i + 3] = nan[3];
        }
    }//function - InitializePerceptData

    #endregion //Percept Buffer Initialization


    //MPL OBJECTS AND PROPERTIES
    #region MPL Objects and Properties

    //MPL OBJECTS
    #region Assign MPL Objects
    /// <summary>
    /// Will set pointer variables to game objects in realtime
    /// - which prevents the need to do so in the Scene/Unity-Interface 
    /// </summary>
    private void AssignRightMPLGameObjects()
    {
        float f_itScalar = 0.01f;

        if (m_haveRightMPL)
        {
            ms_rShoulderRoot = GameObject.Find(ms_RIGHT_SHOULDER_ROOT_STR);
            if (ms_rShoulderRoot == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_SHOULDER_ROOT_STR + " not found.");
            }
            ms_rShoulderRoot.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);


            ms_rShoulderFlexAssembly =
                GameObject.Find(ms_RIGHT_SHOULDER_FLEX_ASSEMBLY_STR);
            if (ms_rShoulderFlexAssembly == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_SHOULDER_FLEX_ASSEMBLY_STR + " not found.");
            }
            ms_rShoulderFlexAssembly.AddComponent<WorldObject>().m_id =
                ms_RIGHT_SHOULDER_FLEX_ASSEMBLY_STR;
            ms_rShoulderFlexAssembly.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);

            ms_rShoulderShell = GameObject.Find(ms_RIGHT_SHOULDER_SHELL_STR);
            if (ms_rShoulderShell == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_SHOULDER_SHELL_STR + " not found.");
            }
            ms_rShoulderShell.AddComponent<WorldObject>().m_id =
                ms_RIGHT_SHOULDER_SHELL_STR;
            ms_rShoulderShell.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);

            ms_rHumeralRotatorElbow =
                GameObject.Find(ms_RIGHT_HUMERAL_ROTATOR_ELBOW_STR);
            if (ms_rHumeralRotatorElbow == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_HUMERAL_ROTATOR_ELBOW_STR + " not found.");
            }
            ms_rHumeralRotatorElbow.AddComponent<WorldObject>().m_id =
                ms_RIGHT_HUMERAL_ROTATOR_ELBOW_STR;
            ms_rHumeralRotatorElbow.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);

            ms_rForeArm = GameObject.Find(ms_RIGHT_FORE_ARM_STR);
            if (ms_rForeArm == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_FORE_ARM_STR + " not found.");
            }
            ms_rForeArm.AddComponent<WorldObject>().m_id =
                ms_RIGHT_FORE_ARM_STR;
            ms_rForeArm.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);

            ms_rWristShell = GameObject.Find(ms_RIGHT_WRIST_SHELL_STR);
            if (ms_rWristShell == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_WRIST_SHELL_STR + " not found.");
            }
            ms_rWristShell.AddComponent<WorldObject>().m_id =
                ms_RIGHT_WRIST_SHELL_STR;
            ms_rWristShell.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);

            ms_rWristDev = GameObject.Find(ms_RIGHT_WRIST_DEV_STR);
            if (ms_rWristDev == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_WRIST_DEV_STR + " not found.");
            }
            ms_rWristDev.AddComponent<WorldObject>().m_id =
                ms_RIGHT_WRIST_DEV_STR;
            ms_rWristDev.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);


            ms_rPalm = GameObject.Find(ms_RIGHT_PALM_STR);
            if (ms_rPalm == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_PALM_STR + " not found.");
            }
            ms_rPalm.AddComponent<WorldObject>().m_id =
                ms_RIGHT_PALM_STR;
            ms_rPalm.GetComponent<Rigidbody>().inertiaTensor = new Vector3(1.0f, 1.0f, 1.0f);


            ms_rPlanetaryAsm = GameObject.Find(ms_RIGHT_PLANETARY_ASM_STR);
            if (ms_rPlanetaryAsm == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_PLANETARY_ASM_STR + " not found.");
            }
            //ms_rPlanetaryAsm.rigidbody.inertiaTensor = new Vector3(36, 16, 21);
            //ms_rPlanetaryAsm.GetComponent<Rigidbody>().inertiaTensor = new Vector3(7.2f, 3.2f, 4.2f);
            ms_rPlanetaryAsm.GetComponent<Rigidbody>().inertiaTensor = new Vector3(7.2f*f_itScalar, 3.2f * f_itScalar, 4.2f * f_itScalar);
            
            //ms_rPlanetaryAsm.rigidbody.inertiaTensorRotation = Quaternion.AngleAxis(13.66934f, new Vector3(0, 0, 1));
            ms_rPlanetaryAsm.AddComponent<WorldObject>().m_id =
                ms_RIGHT_PLANETARY_ASM_STR;

            ms_rThProximal1 = GameObject.Find(ms_RIGHT_THUMB_PROXIMAL1_STR);
            if (ms_rThProximal1 == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_THUMB_PROXIMAL1_STR + " not found.");
            }
            //ms_rThProximal1.rigidbody.inertiaTensor = new Vector3(31, 11, 11);
            ms_rThProximal1.GetComponent<Rigidbody>().inertiaTensor = new Vector3(6.2f * f_itScalar, 2.2f * f_itScalar, 2.2f * f_itScalar);
            //ms_rThProximal1.rigidbody.inertiaTensorRotation = Quaternion.AngleAxis(15.3f, new Vector3(0, 0, 1));
            ms_rThProximal1.AddComponent<WorldObject>().m_id =
                ms_RIGHT_THUMB_PROXIMAL1_STR;

            ms_rThProximal2 = GameObject.Find(ms_RIGHT_THUMB_PROXIMAL2_STR);
            if (ms_rThProximal2 == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_THUMB_PROXIMAL2_STR + " not found.");
            }
            //ms_rThProximal2.rigidbody.inertiaTensor = new Vector3(31, 11, 11);
            ms_rThProximal2.GetComponent<Rigidbody>().inertiaTensor = new Vector3(6.2f * f_itScalar, 2.2f * f_itScalar, 2.2f * f_itScalar);
            //ms_rThProximal2.rigidbody.inertiaTensorRotation = Quaternion.AngleAxis(15.3f, new Vector3(0, 0, 1));
            ms_rThProximal2.AddComponent<WorldObject>().m_id =
                ms_RIGHT_THUMB_PROXIMAL2_STR;

            ms_rThDistal = GameObject.Find(ms_RIGHT_THUMB_DISTAL_STR);
            if (ms_rThDistal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_THUMB_DISTAL_STR + " not found.");
            }
            ms_rThDistal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_THUMB_DISTAL_STR;

            ms_rIndMetaCarpal = GameObject.Find(ms_RIGHT_IND_METACARPAL_STR);
            if (ms_rIndMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_IND_METACARPAL_STR + " not found.");
            }
            ms_rIndMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_IND_METACARPAL_STR;

            ms_rIndProximal = GameObject.Find(ms_RIGHT_IND_PROXIMAL_STR);
            if (ms_rIndProximal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_IND_PROXIMAL_STR + " not found.");
            }
            ms_rIndProximal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_IND_PROXIMAL_STR;
            ms_rIndProxContact = ms_rIndProximal.GetComponent<ProximalContact>();
            //ms_rIndProximal.rigidbody.inertiaTensor = new Vector3(10, 50, 10);
            ms_rIndProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_rIndMedial = GameObject.Find(ms_RIGHT_IND_MEDIAL_STR);
            if (ms_rIndMedial == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_IND_MEDIAL_STR + " not found.");
            }

//#if UNITY_EDITOR
//            Mesh mesh_IndMedial = ms_rIndMedial.GetComponent<MeshFilter>().mesh;

//            Debug.Log("Mesh Size (index medial): " + mesh_IndMedial.bounds.ToString() + ", Meshcount: " + mesh_IndMedial.subMeshCount + ", " + mesh_IndMedial.triangles.GetLength(0));
//#endif

            ms_rIndMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_rIndMedial.AddComponent<WorldObject>().m_id =
                ms_RIGHT_IND_MEDIAL_STR;

            ms_rIndDistal = GameObject.Find(ms_RIGHT_IND_DISTAL_STR);
            if (ms_rIndDistal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_IND_DISTAL_STR + " not found.");
            }
            ms_rIndDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_rIndDistal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_IND_DISTAL_STR;
            ms_rIndDistContact = ms_rIndDistal.GetComponent<DistalContact>();

            ms_rMidMetaCarpal = GameObject.Find(ms_RIGHT_MID_METACARPAL_STR);
            if (ms_rMidMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_MID_METACARPAL_STR + " not found.");
            }
            ms_rMidMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_MID_METACARPAL_STR;

            ms_rMidProximal = GameObject.Find(ms_RIGHT_MID_PROXIMAL_STR);
            if (ms_rMidProximal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_MID_PROXIMAL_STR + " not found.");
            }
            ms_rMidProximal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_MID_PROXIMAL_STR;
            ms_rMidProxContact = ms_rMidProximal.GetComponent<ProximalContact>();
            //ms_rMidProximal.rigidbody.inertiaTensor = new Vector3(10, 50, 10);
            ms_rMidProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_rMidMedial = GameObject.Find(ms_RIGHT_MID_MEDIAL_STR);
            if (ms_rMidMedial == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_MID_MEDIAL_STR + " not found.");
            }
            ms_rMidMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_rMidMedial.AddComponent<WorldObject>().m_id =
                ms_RIGHT_MID_MEDIAL_STR;

            ms_rMidDistal = GameObject.Find(ms_RIGHT_MID_DISTAL_STR);
            if (ms_rMidDistal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_MID_DISTAL_STR + " not found.");
            }
            ms_rMidDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_rMidDistal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_MID_DISTAL_STR;
            ms_rMidDistContact = ms_rMidDistal.GetComponent<DistalContact>();

            ms_rRingMetaCarpal = GameObject.Find(ms_RIGHT_RING_METACARPAL_STR);
            if (ms_rRingMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_RING_METACARPAL_STR + " not found.");
            }
            ms_rRingMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_RING_METACARPAL_STR;

            ms_rRingProximal = GameObject.Find(ms_RIGHT_RING_PROXIMAL_STR);
            if (ms_rRingProximal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_RING_PROXIMAL_STR + " not found.");
            }
            ms_rRingProximal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_RING_PROXIMAL_STR;
            //ms_rRingProximal.rigidbody.inertiaTensor = new Vector3(10, 50, 10);
            ms_rRingProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_rRingMedial = GameObject.Find(ms_RIGHT_RING_MEDIAL_STR);
            if (ms_rRingMedial == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_RING_MEDIAL_STR + " not found.");
            }
            ms_rRingMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_rRingMedial.AddComponent<WorldObject>().m_id =
                ms_RIGHT_RING_MEDIAL_STR;

            ms_rRingDistal = GameObject.Find(ms_RIGHT_RING_DISTAL_STR);
            if (ms_rRingDistal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_RING_DISTAL_STR + " not found.");
            }
            ms_rRingDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_rRingDistal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_RING_DISTAL_STR;

            ms_rLittleMetaCarpal = GameObject.Find(ms_RIGHT_LITTLE_METACARPAL_STR);
            if (ms_rLittleMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_LITTLE_METACARPAL_STR + " not found.");
            }
            ms_rLittleMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_LITTLE_METACARPAL_STR;

            ms_rLittleProximal = GameObject.Find(ms_RIGHT_LITTLE_PROXIMAL_STR);
            if (ms_rLittleProximal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_LITTLE_PROXIMAL_STR + " not found.");
            }
            ms_rLittleProximal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_LITTLE_PROXIMAL_STR;
            //ms_rLittleProximal.rigidbody.inertiaTensor = new Vector3(10, 50, 10);
            ms_rLittleProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_rLittleMedial = GameObject.Find(ms_RIGHT_LITTLE_MEDIAL_STR);
            if (ms_rLittleMedial == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_LITTLE_MEDIAL_STR + " not found.");
            }
            ms_rLittleMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_rLittleMedial.AddComponent<WorldObject>().m_id =
                ms_RIGHT_LITTLE_MEDIAL_STR;

            ms_rLittleDistal = GameObject.Find(ms_RIGHT_LITTLE_DISTAL_STR);
            if (ms_rLittleDistal == null)
            {
                throw new NullReferenceException(
                    ms_RIGHT_LITTLE_DISTAL_STR + " not found.");
            }
            ms_rLittleDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_rLittleDistal.AddComponent<WorldObject>().m_id =
                ms_RIGHT_LITTLE_DISTAL_STR;

            //ms_rElbowJoint = ms_rHumeralRotatorElbow.GetComponent<ConfigurableJoint>();
            //if (ms_rElbowJoint == null)
            //{
            //    throw new NullReferenceException(
            //        ms_RIGHT_HUMERAL_ROTATOR_ELBOW_STR + " ConfigurableJoint not found.");
            //}

            AssignRightPalmHingeJoints();
            SetupRightPIDValues();

            //get segment precepts source object
            //ms_rSegPercepts = (SensorArray)(GameObject.Find("rPalm").GetComponent(typeof(SensorArray)));
            ms_rSegPercepts = (FTSN14SensorArray)(GameObject.Find("rPalm").GetComponent(typeof(FTSN14SensorArray)));

        }
    }//function - AssignRightMPLGameObjects


    /// <summary>
    /// Will set pointer variables to game objects in realtime
    /// - which prevents the need to do so in the Scene/Unity-Interface 
    /// </summary>
    private void AssignLeftMPLGameObjects()
    {
        float f_itScalar = 0.01f;

        if (m_haveLeftMPL)
        {
            ms_lShoulderRoot = GameObject.Find(ms_LEFT_SHOULDER_ROOT_STR);
            if (ms_lShoulderRoot == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_SHOULDER_ROOT_STR + " not found.");
            }

            ms_lShoulderFlexAssembly =
                GameObject.Find(ms_LEFT_SHOULDER_FLEX_ASSEMBLY_STR);
            if (ms_lShoulderFlexAssembly == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_SHOULDER_FLEX_ASSEMBLY_STR + " not found.");
            }
            ms_lShoulderFlexAssembly.AddComponent<WorldObject>().m_id =
                ms_LEFT_SHOULDER_FLEX_ASSEMBLY_STR;

            ms_lShoulderShell = GameObject.Find(ms_LEFT_SHOULDER_SHELL_STR);
            if (ms_lShoulderShell == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_SHOULDER_SHELL_STR + " not found.");
            }
            ms_lShoulderShell.AddComponent<WorldObject>().m_id =
                ms_LEFT_SHOULDER_SHELL_STR;

            ms_lHumeralRotatorElbow =
                GameObject.Find(ms_LEFT_HUMERAL_ROTATOR_ELBOW_STR);
            if (ms_lHumeralRotatorElbow == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_HUMERAL_ROTATOR_ELBOW_STR + " not found.");
            }
            ms_lHumeralRotatorElbow.AddComponent<WorldObject>().m_id =
                ms_LEFT_HUMERAL_ROTATOR_ELBOW_STR;

            ms_lForeArm = GameObject.Find(ms_LEFT_FORE_ARM_STR);
            if (ms_lForeArm == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_FORE_ARM_STR + " not found.");
            }
            ms_lForeArm.AddComponent<WorldObject>().m_id =
                ms_LEFT_FORE_ARM_STR;

            ms_lWristShell = GameObject.Find(ms_LEFT_WRIST_SHELL_STR);
            if (ms_lWristShell == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_WRIST_SHELL_STR + " not found.");
            }
            ms_lWristShell.AddComponent<WorldObject>().m_id =
                ms_LEFT_WRIST_SHELL_STR;

            ms_lWristDev = GameObject.Find(ms_LEFT_WRIST_DEV_STR);
            if (ms_lWristDev == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_WRIST_DEV_STR + " not found.");
            }
            ms_lWristDev.AddComponent<WorldObject>().m_id =
                ms_LEFT_WRIST_DEV_STR;

            ms_lPalm = GameObject.Find(ms_LEFT_PALM_STR);
            if (ms_lPalm == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_PALM_STR + " not found.");
            }
            ms_lPalm.AddComponent<WorldObject>().m_id =
                ms_LEFT_PALM_STR;

            ms_lPlanetaryAsm = GameObject.Find(ms_LEFT_PLANETARY_ASM_STR);
            if (ms_lPlanetaryAsm == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_PLANETARY_ASM_STR + " not found.");
            }
            ms_lPlanetaryAsm.GetComponent<Rigidbody>().inertiaTensor = new Vector3(7.2f * f_itScalar, 3.2f * f_itScalar, 4.2f * f_itScalar);
            //ms_lPlanetaryAsm.rigidbody.inertiaTensorRotation = Quaternion.AngleAxis(13.66934f, new Vector3(0, 0, 1));
            ms_lPlanetaryAsm.AddComponent<WorldObject>().m_id =
                ms_LEFT_PLANETARY_ASM_STR;

            ms_lThProximal1 = GameObject.Find(ms_LEFT_THUMB_PROXIMAL1_STR);
            if (ms_lThProximal1 == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_THUMB_PROXIMAL1_STR + " not found.");
            }
            ms_lThProximal1.GetComponent<Rigidbody>().inertiaTensor = new Vector3(6.2f * f_itScalar, 2.2f * f_itScalar, 2.2f * f_itScalar);
            //ms_lThProximal1.rigidbody.inertiaTensorRotation = Quaternion.AngleAxis(15.3f, new Vector3(0, 0, 1));
            ms_lThProximal1.AddComponent<WorldObject>().m_id =
                ms_LEFT_THUMB_PROXIMAL1_STR;

            ms_lThProximal2 = GameObject.Find(ms_LEFT_THUMB_PROXIMAL2_STR);
            if (ms_lThProximal2 == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_THUMB_PROXIMAL2_STR + " not found.");
            }
            //ms_lThProximal2.rigidbody.inertiaTensor = new Vector3(31, 11, 11);
            ms_lThProximal2.GetComponent<Rigidbody>().inertiaTensor = new Vector3(6.2f * f_itScalar, 2.2f * f_itScalar, 2.2f * f_itScalar);
            //ms_lThProximal2.rigidbody.inertiaTensorRotation = Quaternion.AngleAxis(15.3f, new Vector3(0, 0, 1));
            ms_lThProximal2.AddComponent<WorldObject>().m_id =
                ms_LEFT_THUMB_PROXIMAL2_STR;

            ms_lThDistal = GameObject.Find(ms_LEFT_THUMB_DISTAL_STR);
            if (ms_lThDistal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_THUMB_DISTAL_STR + " not found.");
            }
            ms_lThDistal.AddComponent<WorldObject>().m_id =
                ms_LEFT_THUMB_DISTAL_STR;

            ms_lIndMetaCarpal = GameObject.Find(ms_LEFT_IND_METACARPAL_STR);
            if (ms_lIndMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_IND_METACARPAL_STR + " not found.");
            }
            ms_lIndMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_LEFT_IND_METACARPAL_STR;

            ms_lIndProximal = GameObject.Find(ms_LEFT_IND_PROXIMAL_STR);
            if (ms_lIndProximal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_IND_PROXIMAL_STR + " not found.");
            }
            ms_lIndProximal.AddComponent<WorldObject>().m_id =
                ms_LEFT_IND_PROXIMAL_STR;
            ms_lIndProxContact = ms_lIndProximal.GetComponent<ProximalContact>();
            ms_lIndProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_lIndMedial = GameObject.Find(ms_LEFT_IND_MEDIAL_STR);
            if (ms_lIndMedial == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_IND_MEDIAL_STR + " not found.");
            }
            ms_lIndMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_lIndMedial.AddComponent<WorldObject>().m_id =
                ms_LEFT_IND_MEDIAL_STR;

            ms_lIndDistal = GameObject.Find(ms_LEFT_IND_DISTAL_STR);
            if (ms_lIndDistal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_IND_DISTAL_STR + " not found.");
            }
            ms_lIndDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_lIndDistal.AddComponent<WorldObject>().m_id =
                ms_LEFT_IND_DISTAL_STR;
            //ms_lIndDistContact = ms_lIndDistal.GetComponent<DistalContact>();

            ms_lMidMetaCarpal = GameObject.Find(ms_LEFT_MID_METACARPAL_STR);
            if (ms_lMidMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_MID_METACARPAL_STR + " not found.");
            }
            ms_lMidMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_LEFT_MID_METACARPAL_STR;

            ms_lMidProximal = GameObject.Find(ms_LEFT_MID_PROXIMAL_STR);
            if (ms_lMidProximal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_MID_PROXIMAL_STR + " not found.");
            }
            ms_lMidProximal.AddComponent<WorldObject>().m_id =
                ms_LEFT_MID_PROXIMAL_STR;
            ms_lMidProxContact = ms_lMidProximal.GetComponent<ProximalContact>();
            ms_lMidProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_lMidMedial = GameObject.Find(ms_LEFT_MID_MEDIAL_STR);
            if (ms_lMidMedial == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_MID_MEDIAL_STR + " not found.");
            }
            ms_lMidMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_lMidMedial.AddComponent<WorldObject>().m_id =
                ms_LEFT_MID_MEDIAL_STR;

            ms_lMidDistal = GameObject.Find(ms_LEFT_MID_DISTAL_STR);
            if (ms_lMidDistal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_MID_DISTAL_STR + " not found.");
            }
            ms_lMidDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_lMidDistal.AddComponent<WorldObject>().m_id =
                ms_LEFT_MID_DISTAL_STR;
            //ms_lMidDistContact = ms_lMidDistal.GetComponent<DistalContact>();

            ms_lRingMetaCarpal = GameObject.Find(ms_LEFT_RING_METACARPAL_STR);
            if (ms_lRingMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_RING_METACARPAL_STR + " not found.");
            }
            ms_lRingMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_LEFT_RING_METACARPAL_STR;

            ms_lRingProximal = GameObject.Find(ms_LEFT_RING_PROXIMAL_STR);
            if (ms_lRingProximal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_RING_PROXIMAL_STR + " not found.");
            }
            ms_lRingProximal.AddComponent<WorldObject>().m_id =
                ms_LEFT_RING_PROXIMAL_STR;
            //ms_lRingProximal.rigidbody.inertiaTensor = new Vector3(10, 50, 10);
            ms_lRingProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_lRingMedial = GameObject.Find(ms_LEFT_RING_MEDIAL_STR);
            if (ms_lRingMedial == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_RING_MEDIAL_STR + " not found.");
            }
            ms_lRingMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_lRingMedial.AddComponent<WorldObject>().m_id =
                ms_LEFT_RING_MEDIAL_STR;

            ms_lRingDistal = GameObject.Find(ms_LEFT_RING_DISTAL_STR);
            if (ms_lRingDistal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_RING_DISTAL_STR + " not found.");
            }
            ms_lRingDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_lRingDistal.AddComponent<WorldObject>().m_id =
                ms_LEFT_RING_DISTAL_STR;

            ms_lLittleMetaCarpal = GameObject.Find(ms_LEFT_LITTLE_METACARPAL_STR);
            if (ms_lLittleMetaCarpal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_LITTLE_METACARPAL_STR + " not found.");
            }
            ms_lLittleMetaCarpal.AddComponent<WorldObject>().m_id =
                ms_LEFT_LITTLE_METACARPAL_STR;

            ms_lLittleProximal = GameObject.Find(ms_LEFT_LITTLE_PROXIMAL_STR);
            if (ms_lLittleProximal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_LITTLE_PROXIMAL_STR + " not found.");
            }
            ms_lLittleProximal.AddComponent<WorldObject>().m_id =
                ms_LEFT_LITTLE_PROXIMAL_STR;
            //ms_lLittleProximal.rigidbody.inertiaTensor = new Vector3(10, 50, 10);
            ms_lLittleProximal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(2f * f_itScalar, 10f * f_itScalar, 2f * f_itScalar);

            ms_lLittleMedial = GameObject.Find(ms_LEFT_LITTLE_MEDIAL_STR);
            if (ms_lLittleMedial == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_LITTLE_MEDIAL_STR + " not found.");
            }
            ms_lLittleMedial.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 7.6f * f_itScalar, 3.2f * f_itScalar);
            ms_lLittleMedial.AddComponent<WorldObject>().m_id =
                ms_LEFT_LITTLE_MEDIAL_STR;

            ms_lLittleDistal = GameObject.Find(ms_LEFT_LITTLE_DISTAL_STR);
            if (ms_lLittleDistal == null)
            {
                throw new NullReferenceException(
                    ms_LEFT_LITTLE_DISTAL_STR + " not found.");
            }
            ms_lLittleDistal.GetComponent<Rigidbody>().inertiaTensor = new Vector3(3.2f * f_itScalar, 5.2f * f_itScalar, 3.2f * f_itScalar);
            ms_lLittleDistal.AddComponent<WorldObject>().m_id =
                ms_LEFT_LITTLE_DISTAL_STR;

            //ms_lElbowJoint = ms_lHumeralRotatorElbow.GetComponent<ConfigurableJoint>();
            //if (ms_lElbowJoint == null)
            //{
            //    throw new NullReferenceException(
            //        ms_LEFT_HUMERAL_ROTATOR_ELBOW_STR + " ConfigurableJoint not found.");
            //}

            AssignLeftPalmHingeJoints();
            SetupLeftPIDValues();

            //get segment precepts source object
            //ms_lSegPercepts = (SensorArray)(GameObject.Find("lPalm").GetComponent(typeof(SensorArray)));
            ms_lSegPercepts = (FTSN14SensorArray)(GameObject.Find("lPalm").GetComponent(typeof(FTSN14SensorArray)));

        }
    }//function - AssignLeftMPLGameObjects

    #endregion //Assign MPL Objects


    //HINGE JOINTS
    #region Hinge Joints

    //Assign Hinge Joints Object
    #region Assign Hinge Joints Objects

    /// <summary>
    /// Will set the Hinge Joint Settings
    /// </summary>
    private void AssignRightPalmHingeJoints()
    {
        if (m_haveRightMPL)
        {
            HingeJoint[] joints = ms_rPalm.GetComponents<HingeJoint>();
            ms_rPalmToPlanetaryAsm =
                joints[FindJoint(joints, ms_rPlanetaryAsm.GetComponent<Rigidbody>())];
            ms_rPalmToLittleMetaCarpal =
                joints[FindJoint(joints, ms_rLittleMetaCarpal.GetComponent<Rigidbody>())];
            ms_rPalmToRingMetaCarpal =
                joints[FindJoint(joints, ms_rRingMetaCarpal.GetComponent<Rigidbody>())];
            ms_rPalmToMidMetaCarpal =
                joints[FindJoint(joints, ms_rMidMetaCarpal.GetComponent<Rigidbody>())];
            ms_rPalmToIndMetaCarpal =
                joints[FindJoint(joints, ms_rIndMetaCarpal.GetComponent<Rigidbody>())];
        }
    }//function - AssignRightPalmHingeJoints


    /// <summary>
    /// Will set the Hinge Joint Settings
    /// </summary>
    private void AssignLeftPalmHingeJoints()
    {
        if (m_haveLeftMPL)
        {
            HingeJoint[] joints = ms_lPalm.GetComponents<HingeJoint>();
            ms_lPalmToPlanetaryAsm =
                joints[FindJoint(joints, ms_lPlanetaryAsm.GetComponent<Rigidbody>())];
            ms_lPalmToLittleMetaCarpal =
                joints[FindJoint(joints, ms_lLittleMetaCarpal.GetComponent<Rigidbody>())];
            ms_lPalmToRingMetaCarpal =
                joints[FindJoint(joints, ms_lRingMetaCarpal.GetComponent<Rigidbody>())];
            ms_lPalmToMidMetaCarpal =
                joints[FindJoint(joints, ms_lMidMetaCarpal.GetComponent<Rigidbody>())];
            ms_lPalmToIndMetaCarpal =
                joints[FindJoint(joints, ms_lIndMetaCarpal.GetComponent<Rigidbody>())];
        }
    }//function - AssignLeftPalmHingeJoints

    #endregion //Assign Hinge Joints


    //Joint Limits
    #region Hinge Joint Limits
    /// <summary>
    /// Ensure that all hinge limits are within [-180, 180] as defined
    /// by the PhysX documentation.  The PID algorithm used in FixedUpdate()
    /// assumes the limits are within [-180, 180].
    /// </summary>
    private void SetupRightMPLLimits()
    {
        if (m_haveRightMPL)
        {
            AdjustHingeLimits(ms_rShoulderRoot.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rShoulderFlexAssembly.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rShoulderShell.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rHumeralRotatorElbow.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rForeArm.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rWristShell.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rWristDev.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rPalmToPlanetaryAsm);
            AdjustHingeLimits(ms_rPlanetaryAsm.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rThProximal1.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rThProximal2.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rPalmToLittleMetaCarpal);
            AdjustHingeLimits(ms_rLittleMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rLittleProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rLittleMedial.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rPalmToRingMetaCarpal);
            AdjustHingeLimits(ms_rRingMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rRingProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rRingMedial.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rPalmToMidMetaCarpal);
            AdjustHingeLimits(ms_rMidMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rMidProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rMidMedial.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rPalmToIndMetaCarpal);
            AdjustHingeLimits(ms_rIndMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rIndProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_rIndMedial.GetComponent<HingeJoint>());
        }
    }//function - SetupRightMPLLimits


    /// <summary>
    /// Ensure that all hinge limits are within [-180, 180] as defined
    /// by the PhysX documentation.  The PID algorithm used in FixedUpdate()
    /// assumes the limits are within [-180, 180].
    /// </summary>
    private void SetupLeftMPLLimits()
    {
        if (m_haveLeftMPL)
        {
            AdjustHingeLimits(ms_lShoulderRoot.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lShoulderFlexAssembly.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lShoulderShell.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lHumeralRotatorElbow.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lForeArm.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lWristShell.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lWristDev.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lPalmToPlanetaryAsm);
            AdjustHingeLimits(ms_lPlanetaryAsm.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lThProximal1.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lThProximal2.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lPalmToLittleMetaCarpal);
            AdjustHingeLimits(ms_lLittleMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lLittleProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lLittleMedial.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lPalmToRingMetaCarpal);
            AdjustHingeLimits(ms_lRingMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lRingProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lRingMedial.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lPalmToMidMetaCarpal);
            AdjustHingeLimits(ms_lMidMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lMidProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lMidMedial.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lPalmToIndMetaCarpal);
            AdjustHingeLimits(ms_lIndMetaCarpal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lIndProximal.GetComponent<HingeJoint>());
            AdjustHingeLimits(ms_lIndMedial.GetComponent<HingeJoint>());
        }
    }//function - SetupLeftMPLLimits


    /// <summary>
    /// Ensures that given hinge joint's limits are within [-180, 180] as
    /// defined by the PhysX documentation.  The PID algorithm used in 
    /// FixedUpdate() assumes that the limits are within [-180, 180].
    /// </summary>
    /// <param name="hingeJoint"></param>
    private void AdjustHingeLimits(HingeJoint hingeJoint)
    {
        // hingeJoint.limits is a struct, so have to assign it to a temp
        // variable and then assign hingeJoint.limits back to the the temp
        // variable.
        JointLimits limits = hingeJoint.limits;
        while (limits.min < -180)
            limits.min += 180;
        while (limits.min > 180)
            limits.min -= 180;
        while (limits.max < -180)
            limits.max += 180;
        while (limits.max > 180)
            limits.max -= 180;
        hingeJoint.limits = limits;
    }//function - AdjustHingeLimits

    #endregion //Hinge Joint Limits


    //Hinge Properties (spring, damper, motor)
    #region Hinge Properties
    /// <summary>
    /// Set up the hinge/spring values in the MPL
    /// </summary>
    private void SetupRightMPLHingeJoints()
    {
        if (m_haveRightMPL)
        {
            bool b_UseSpring = false;
            bool b_UseMotor = true;
            float f_defaultSpringValue = 0f;// 100000;
            float f_defaultDamperValue = 0f;// 100000;

            //Debug.Log("Hinge Properties: " + ms_rShoulderRoot.hingeJoint.useSpring + " (use spring), " + ms_rShoulderRoot.hingeJoint.spring.spring + " (spring), " + ms_rShoulderRoot.hingeJoint.spring.damper + " (damper)");


            SetupHingeProperties(ms_rShoulderRoot.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rShoulderFlexAssembly.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rShoulderShell.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rHumeralRotatorElbow.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rForeArm.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rWristShell.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rWristDev.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rPalmToPlanetaryAsm, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rPlanetaryAsm.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rThProximal1.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rThProximal2.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rPalmToLittleMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rLittleMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rLittleProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rLittleMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rPalmToRingMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rRingMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rRingProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rRingMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rPalmToMidMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rMidMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rMidProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rMidMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rPalmToIndMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rIndMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rIndProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_rIndMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);

            //Debug.Log("Hinge Properties: " + ms_rShoulderRoot.hingeJoint.useSpring + " (use spring), " + ms_rShoulderRoot.hingeJoint.spring.spring + " (spring), " + ms_rShoulderRoot.hingeJoint.spring.damper + " (damper)");


            #region Defaults 
            /*
             * 
            SetupHingeProperties(ms_rShoulderRoot.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rShoulderFlexAssembly.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rShoulderShell.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rHumeralRotatorElbow.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rForeArm.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rWristShell.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rWristDev.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rPalmToPlanetaryAsm, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rPlanetaryAsm.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rThProximal1.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rThProximal2.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rPalmToLittleMetaCarpal, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rLittleMetaCarpal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rLittleProximal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rLittleMedial.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rPalmToRingMetaCarpal, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rRingMetaCarpal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rRingProximal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rRingMedial.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rPalmToMidMetaCarpal, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rMidMetaCarpal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rMidProximal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rMidMedial.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rPalmToIndMetaCarpal, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rIndMetaCarpal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rIndProximal.hingeJoint, f_defaultSpringValue, 0f, true, true);
            SetupHingeProperties(ms_rIndMedial.hingeJoint, f_defaultSpringValue, 0f, true, true);

            SetupHingeProperties(ms_rShoulderRoot.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rShoulderFlexAssembly.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rShoulderShell.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rHumeralRotatorElbow.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rForeArm.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rWristShell.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rWristDev.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rPalmToPlanetaryAsm, 0f, 0f, true, true);
            SetupHingeProperties(ms_rPlanetaryAsm.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rThProximal1.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rThProximal2.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rPalmToLittleMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_rLittleMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rLittleProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rLittleMedial.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rPalmToRingMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_rRingMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rRingProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rRingMedial.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rPalmToMidMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_rMidMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rMidProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rMidMedial.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rPalmToIndMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_rIndMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rIndProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_rIndMedial.hingeJoint, 0f, 0f, true, true);
            */
            #endregion //Defaults 

        }//if - check for limb

    }//function - SetupRightMPLHingeJoints


    /// <summary>
    /// Set up the hinge/spring values in the MPL
    /// </summary>
    private void SetupLeftMPLHingeJoints()
    {
        if (m_haveLeftMPL)
        {
            bool b_UseSpring = false;
            bool b_UseMotor = true;
            float f_defaultSpringValue = 0f;// 100000;
            float f_defaultDamperValue = 0f;// 100000;

            SetupHingeProperties(ms_lShoulderRoot.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lShoulderFlexAssembly.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lShoulderShell.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lHumeralRotatorElbow.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lForeArm.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lWristShell.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lWristDev.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lPalmToPlanetaryAsm, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lPlanetaryAsm.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lThProximal1.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lThProximal2.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lPalmToLittleMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lLittleMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lLittleProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lLittleMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lPalmToRingMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lRingMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lRingProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lRingMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lPalmToMidMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lMidMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lMidProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lMidMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lPalmToIndMetaCarpal, f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lIndMetaCarpal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lIndProximal.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            SetupHingeProperties(ms_lIndMedial.GetComponent<HingeJoint>(), f_defaultSpringValue, f_defaultDamperValue, b_UseSpring, b_UseMotor);
            

            #region Defaults 
            /*
             * 
            SetupHingeProperties(ms_lShoulderRoot.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lShoulderFlexAssembly.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lShoulderShell.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lHumeralRotatorElbow.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lForeArm.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lWristShell.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lWristDev.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lPalmToPlanetaryAsm, 0f, 0f, true, true);
            SetupHingeProperties(ms_lPlanetaryAsm.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lThProximal1.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lThProximal2.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lPalmToLittleMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_lLittleMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lLittleProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lLittleMedial.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lPalmToRingMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_lRingMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lRingProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lRingMedial.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lPalmToMidMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_lMidMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lMidProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lMidMedial.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lPalmToIndMetaCarpal, 0f, 0f, true, true);
            SetupHingeProperties(ms_lIndMetaCarpal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lIndProximal.hingeJoint, 0f, 0f, true, true);
            SetupHingeProperties(ms_lIndMedial.hingeJoint, 0f, 0f, true, true);
            */
            #endregion //Defaults

        }//if - check for limb

    }//function - SetupLeftMPLHingeJoints


    /// <summary>
    /// This version of SetHingeValue is used with velocity control.  Springs
    /// are used when the velocity is zero.
    /// </summary>
    /// <param name="f_SpringValue">Spring value</param>
    /// <param name="f_DamperValue">Damper value</param>
    /// <param name="b_UseSpring">Whether to use spring in hinge joint</param>
    /// <param name="b_UseMotor">Whether to use motor in hinge joint</param>
    private void SetupHingeProperties(HingeJoint hinge, float f_SpringValue, float f_DamperValue, bool b_UseSpring, bool b_UseMotor)
    {
        //bool motorAlreadyZero = false;

        JointSpring spring = hinge.spring;
        //spring.spring = 4000000f;
        //spring.damper = 100000000.0f;
        
        //Assign Spring Properties
        spring.spring = f_SpringValue; // float.MaxValue;
        spring.damper = f_DamperValue; // 0.0f;
        
        //Assign Spring to Model
        hinge.spring = spring;

        //Assign Hinge Joint Properties
        hinge.useSpring = b_UseSpring;
        hinge.useMotor = b_UseMotor;
        
        //Debug.Log(hinge.gameObject.name + " hinge spring activated.");
        

    }//function - SetupHingeProperties


    #endregion // Hinge Properties

    #endregion //Hinge Joints


    //PHYSICS VALUES
    #region Physics Values

    //GRAVITY
    #region Gravity
    /// <summary>
    /// Changes gravity of both all vMPLs present
    /// </summary>
    /// <param name="b_useGravity">Gravity<param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private void AssignGravityToVMPL(bool b_useGravity, bool b_showColor)
    {

        //Change Property
        if (m_haveRightMPL)
        {
            //Assign Collision Type
            StartCoroutine(AssignGravityProperties(ms_rPalm.name, b_useGravity, b_showColor));
        }//if - check for limb

        if (m_haveLeftMPL)
        {
            //Assign Collision Type
            StartCoroutine(AssignGravityProperties(ms_lPalm.name, b_useGravity, b_showColor));
        }//if - check for limb

    }//function - AssignGravityToVMPL


    /// <summary>
    /// Changes default gravity enable/disable of the defined object and children
    /// </summary>
    /// <param name="strObjectName">The Unity object to assign material to</param>
    /// <param name="b_useGravity">Gravity boolean<param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private IEnumerator AssignGravityProperties(string strObjectName, bool b_useGravity, bool b_showColor)
    {
        // Walk back up the hierarchy from object to Model Offset Transform.
        GameObject o_gameObject = GameObject.Find(strObjectName);

        if (o_gameObject != null)
        {
            //MPL Object (i.e. Palm)

            Transform _o_gameObject = o_gameObject.transform.parent;
            if (_o_gameObject != null)
            {
                //Model Offset Transform (parent)

                Transform modelOffsetTrans = _o_gameObject.parent;
                if (modelOffsetTrans != null)
                {
                    //MPL Prefab (parent of parent)
                    AssignGravityToAllChildren(modelOffsetTrans, b_useGravity, b_showColor);
                }
                else
                {
                    Debug.LogError("Model Offset Transform not found.");
                }//if - check for object validity
            }
            else
            {
                Debug.LogError("Tranform of Object not found");
            }//if - check for Defined Game Object, Transform
        }
        else
        {
            Debug.LogError("Game Object Not Found");
        }//if - check for Defined Game Object

        yield return new WaitForSeconds(0);

    }//function - AssignGravityProperties
    

    /// <summary>
    /// Recursive function that assigns the given collision type to all children.
    /// </summary>
    /// <param name="obj">The transform of the object.</param>
    /// <param name="mat">The collision type to assign to the object and all its 
    /// children.</param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private void AssignGravityToAllChildren(Transform t_obj, bool b_useGravity, bool b_showColor)
    {
        //Assign properties to children
        foreach (Transform t_child in t_obj)
        {
            //Retrieve Rigidbody for property modification
            Rigidbody curRigidBody = t_child.GetComponent<Rigidbody>();

            //Check to make sure rigidbody exists for Transform
            if (curRigidBody != null)
            {
                //Assign Collision type
                //curRigidBody.useGravity = b_useGravity;

                if (b_useGravity)
                {
                    curRigidBody.mass = 0.001f;
                }
                else
                {
                    curRigidBody.mass = 0.1f;
                }//if - conditionally change mass

                /*
                //Assign Color to show collision took hold
                #region Color Assignment
                if ((b_showColor) && (curRigidBody.gameObject.GetComponent<Renderer>() != null))
                {

                
                    Color color_Temp = Color.black;


                    if (b_useGravity)
                    {
                        color_Temp = Color.green;
                    }
                    else
                    { 
                        color_Temp = Color.black;
                    }//if - check for current collision de  tection type

                    //Loop through all materials (some objects have multiple materials)
                    for (int i = 0; i < curRigidBody.gameObject.GetComponent<Renderer>().materials.Length; i++)
                        curRigidBody.gameObject.GetComponent<Renderer>().materials[i].color = color_Temp;
                    //curRenderer.materials = materials;

                
                }//if - check for renderer (only if material has rendered will color apply)
                #endregion //Color Assignment
                */

            }//if - check if rigidbody is valid
            else
            {
                //Debug.Log("Invalid Rigidbody: " + t_child.gameObject.name + ", parent: " + t_child.parent.name);

            }//if - invalid rigidbody check

            //Loop Through to Children - Recursive
            AssignGravityToAllChildren(t_child, b_useGravity, b_showColor);

        }//foreach - Loop through each tranform child

    }//function - AssignGravityToAllChildren
    #endregion //Gravity
    

    //DAMPING
    #region Damping
    /// <summary>
    /// Changes damping of both all vMPLs present
    /// </summary>
    /// <param name="f_Damping">Damping<param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private void AssignDampingToVMPL(float f_Damping, bool b_showColor)
    {
        //Acquire all hinge joints in the scene
        HingeJoint[] o_HingeJoint = Resources.FindObjectsOfTypeAll(typeof(HingeJoint)) as HingeJoint[]; //Reaches all ACTIVE and INACTIVE objects
        
        //Traverse the Hinge joints, assign properties
        for (int i = 0; i < o_HingeJoint.Length; i++)
        {
            JointSpring newSpringValue = new JointSpring();
            
            newSpringValue = o_HingeJoint[i].spring;
            newSpringValue.damper = f_Damping;
            newSpringValue.spring = 0;
            
            o_HingeJoint[i].spring = newSpringValue;
            o_HingeJoint[i].useSpring = true;

        }//for - traversing all hingejoints
		
        //Assign Properties 
        if (m_haveRightMPL)
        {
            //Assign Collision Type
            StartCoroutine(AssignDampingProperties(ms_rPalm.name, f_Damping, b_showColor));
        }//if - check for limb

        if (m_haveLeftMPL)
        {
            //Assign Collision Type
            StartCoroutine(AssignDampingProperties(ms_lPalm.name, f_Damping, b_showColor));
        }//if - check for limb

    }//function - AssignDampingToVMPL


    /// <summary>
    /// Changes default gravity enable/disable of the defined object and children
    /// </summary>
    /// <param name="strObjectName">The Unity object to assign material to</param>
    /// <param name="b_useGravity">Gravity boolean<param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private IEnumerator AssignDampingProperties(string strObjectName, float f_Damping, bool b_showColor)
    {
        // Walk back up the hierarchy from object to Model Offset Transform.
        GameObject o_gameObject = GameObject.Find(strObjectName);

        if (o_gameObject != null)
        {
            //MPL Object (i.e. Palm)

            Transform _o_gameObject = o_gameObject.transform.parent;
            if (_o_gameObject != null)
            {
                //Model Offset Transform (parent)

                Transform modelOffsetTrans = _o_gameObject.parent;
                if (modelOffsetTrans != null)
                {
                    //MPL Prefab (parent of parent)
                    AssignDampingToAllChildren(modelOffsetTrans, f_Damping, b_showColor);
                }
                else
                {
                    Debug.LogError("Model Offset Transform not found.");
                }//if - check for object validity
            }
            else
            {
                Debug.LogError("Tranform of Object not found");
            }//if - check for Defined Game Object, Transform
        }
        else
        {
            Debug.LogError("Game Object Not Found");
        }//if - check for Defined Game Object

        yield return new WaitForSeconds(0);

    }//function - AssignDampingProperties


    /// <summary>
    /// Recursive function that assigns the given collision type to all children.
    /// </summary>
    /// <param name="obj">The transform of the object.</param>
    /// <param name="mat">The collision type to assign to the object and all its 
    /// children.</param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private void AssignDampingToAllChildren(Transform t_obj, float f_Damping, bool b_showColor)
    {
        //Assign properties to children
        foreach (Transform t_child in t_obj)
        {
            //Retrieve Rigidbody for property modification
            Rigidbody curRigidBody = t_child.GetComponent<Rigidbody>();

            //Check to make sure rigidbody exists for Transform
            if (curRigidBody != null)
            {
                //Assign Collision type
                //curRigidBody.useGravity = f_Damping;

                //HingeJoint[] o_hinges = curRigidBody.hingeJoint;

                /*
                //Assign Color to show collision took hold
                #region Color Assignment
                if ((b_showColor) && (curRigidBody.gameObject.GetComponent<Renderer>() != null))
                {

                
                    Color color_Temp = Color.black;


                    if (f_Damping != 0f)
                    {
                        color_Temp = Color.yellow;
                    }
                    else
                    {
                        color_Temp = Color.grey;
                    }//if - check for current collision detection type

                    //Loop through all materials (some objects have multiple materials)
                    for (int i = 0; i < curRigidBody.gameObject.GetComponent<Renderer>().materials.Length; i++)
                        curRigidBody.gameObject.GetComponent<Renderer>().materials[i].color = color_Temp;
                    //curRenderer.materials = materials;

                
                }//if - check for renderer (only if material has rendered will color apply)
                #endregion //Color Assignment
                */

            }//if - check if rigidbody is valid
            else
            {
                //Debug.Log("Invalid Rigidbody: " + t_child.gameObject.name + ", parent: " + t_child.parent.name);

            }//if - invalid rigidbody check

            //Loop Through to Children - Recursive
            AssignDampingToAllChildren(t_child, f_Damping, b_showColor);

        }//foreach - Loop through each tranform child

    }//function - AssignDampingToAllChildren
    #endregion //Damping


    //SPRINGS
    #region Spring

    #endregion //Spring


    //PHYSIC MATERIAL (surface energy/type)


    //COLLISION TYPE
    #region Collision Type
    /// <summary>
    /// Will initialize the collision type for vMPL
    /// </summary>
    private void InitializevMPLCollisionType()
    {
        //Set the Collision Detection Mode
        //Debug.Log("Collision type: " + str_MPL_CollisionDetectionMode);

        if (str_MPL_CollisionDetectionMode == "default")
        {
            //Do not change properties - keep default collision detection type

            //Currently, the default is set to DISCRETE - will modify all components of vMPL
            AssignCollisionTypeToVMPL(CollisionDetectionMode.Discrete, false); // false = do not change vMPL color
        }
        else if (str_MPL_CollisionDetectionMode == "discrete")
        {
            //Change the collisions detection type (discrete/continuous/continuous-dynamic)
            AssignCollisionTypeToVMPL(CollisionDetectionMode.Discrete, false); // false = do not change vMPL color
        }
        else if (str_MPL_CollisionDetectionMode == "continuous")
        {
            //Change the collisions detection type (discrete/continuous/continuous-dynamic)
            AssignCollisionTypeToVMPL(CollisionDetectionMode.Continuous, false); // false = do not change vMPL color
        }
        else if (str_MPL_CollisionDetectionMode == "dynamic")
        {
            //Change the collisions detection type (discrete/continuous/continuous-dynamic)
            AssignCollisionTypeToVMPL(CollisionDetectionMode.ContinuousDynamic, false); // false = do not change vMPL color
        }
        else
        {
            //Do not change properties - keep default collision detection type

        }//if - check for collisions detection type
    }//function - InitializevMPLCollisionType


    /// <summary>
    /// Changes default collision detection type of both all vMPLs present
    /// </summary>
    /// <param name="coldecttype_CollisionType">Collision type, "discrete", "continuous", "continuous dynamic"<param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private void AssignCollisionTypeToVMPL(CollisionDetectionMode coldecttype_CollisionType, bool b_showColor)
    {
        if (m_haveRightMPL)
        {
            //Assign Collision Type
            StartCoroutine(AssignCollisionTypeProperties(ms_rPalm.name, coldecttype_CollisionType, b_showColor));
        }//if - check for limb

        if (m_haveLeftMPL)
        {
            //Assign Collision Type
            StartCoroutine(AssignCollisionTypeProperties(ms_lPalm.name, coldecttype_CollisionType, b_showColor));
        }//if - check for limb

    }//function - AssignCollisionTypeToVMPL


    /// <summary>
    /// Changes default collision detection type of the defined object and children
    /// </summary>
    /// <param name="strObjectName">The Unity object to assign material to</param>
    /// <param name="coldecttype_CollisionType">Collision type, "discrete", "continuous", "continuous dynamic"<param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private IEnumerator AssignCollisionTypeProperties(string strObjectName, CollisionDetectionMode coldecttype_CollisionType, bool b_showColor)
    {
        // Walk back up the hierarchy from object to Model Offset Transform.
        GameObject o_gameObject = GameObject.Find(strObjectName);

        if (o_gameObject != null)
        {
            //MPL Object (i.e. Palm)

            Transform _o_gameObject = o_gameObject.transform.parent;
            if (_o_gameObject != null)
            {
                //Model Offset Transform (parent)

                Transform modelOffsetTrans = _o_gameObject.parent;
                if (modelOffsetTrans != null)
                {
                    //Debug.Log("Collision Type Assignment (" + modelOffsetTrans.name + ", " + strObjectName + "): " + coldecttype_CollisionType.ToString());

                    //MPL Prefab (parent of parent)
                    AssignCollisionDetectionTypeToAllChildren(modelOffsetTrans, coldecttype_CollisionType, b_showColor);
                }
                else
                {
                    Debug.LogError("Model Offset Transform not found.");
                }//if - check for object validity
            }
            else
            {
                Debug.LogError("Tranform of Object not found");
            }//if - check for Defined Game Object, Transform
        }
        else
        {
            Debug.LogError("Game Object Not Found");
        }//if - check for Defined Game Object

        yield return new WaitForSeconds(0);

    }//function - AssignCollisionTypeProperties


    /// <summary>
    /// Recursive function that assigns the given collision type to all children.
    /// </summary>
    /// <param name="obj">The transform of the object.</param>
    /// <param name="mat">The collision type to assign to the object and all its 
    /// children.</param>
    /// <param name="b_showColor">Have graphic display of collision type change<param>
    private void AssignCollisionDetectionTypeToAllChildren(Transform t_obj, CollisionDetectionMode coldectmod_Temp, bool b_showColor)
    {
        //Assign properties to children
        foreach (Transform t_child in t_obj)
        {
            //Retrieve Rigidbody for property modification
            Rigidbody curRigidBody = t_child.GetComponent<Rigidbody>();

            //Check to make sure rigidbody exists for Transform
            if (curRigidBody != null)
            {
                //Debug.Log("Collision Type (" + t_child.name + "): " + curRigidBody.collisionDetectionMode + "->" + coldectmod_Temp.ToString());

                //Assign Collision type
                curRigidBody.collisionDetectionMode = coldectmod_Temp;

                t_child.GetComponent<Rigidbody>().collisionDetectionMode = coldectmod_Temp;// = curRigidBody;

                if (t_child.GetComponent<Rigidbody>().collisionDetectionMode != coldectmod_Temp)
                {
                    //Debug.Log("Collision Type (" + t_child.name + "): " + curRigidBody.collisionDetectionMode + "->" + coldectmod_Temp.ToString());
                }

                /*
                //Assign Color to show collision took hold
                #region Color Assignment
                if ((b_showColor) && (curRigidBody.gameObject.GetComponent<Renderer>() != null))
                {
                    
                    Color color_Temp = Color.black;


                    if (coldectmod_Temp == CollisionDetectionMode.Discrete)
                    {
                        color_Temp = Color.red;
                    }
                    else if (coldectmod_Temp == CollisionDetectionMode.Continuous)
                    {
                        color_Temp = Color.blue;
                    }
                    else if (coldectmod_Temp == CollisionDetectionMode.ContinuousDynamic)
                    {
                        color_Temp = Color.magenta;
                    }//if - test collision type - turn color
                    else
                    {
                        //curRigidBody.gameObject.renderer.material.color = Color.black;
                        color_Temp = Color.black;
                    }//if - check for current collision detection type

                    //Loop through all materials (some objects have multiple materials)
                    for (int i = 0; i < curRigidBody.gameObject.GetComponent<Renderer>().materials.Length; i++)
                        curRigidBody.gameObject.GetComponent<Renderer>().materials[i].color = color_Temp;
                    //curRenderer.materials = materials;
                    
                }//if - check for renderer (only if material has rendered will color apply)
                #endregion //Color Assignment
                */
            }//if - check if rigidbody is valid
            else
            {
                if (t_child.gameObject.name.Contains("_") || t_child.gameObject.name.Equals("SensorPad") || t_child.gameObject.name.Contains("FTSNPad") || t_child.gameObject.name.Contains("Contact") || t_child.gameObject.name.Contains("Endpoint") || t_child.gameObject.name.Contains("Coll") || t_child.gameObject.name.Contains("FTSN14") || t_child.gameObject.name.Contains("Bracket") || t_child.gameObject.name.Contains("Cylinder") || t_child.gameObject.name.Contains("Invert"))
                {

                }
                else
                {
                    //Debug.Log("Invalid Rigidbody: " + t_child.gameObject.name + ", parent: " + t_child.parent.name);
                }

            }//if - invalid rigidbody check

            //Loop Through to Children - Recursive
            AssignCollisionDetectionTypeToAllChildren(t_child, coldectmod_Temp, b_showColor);

        }//foreach - Loop through each tranform child

    }//function - AssignCollisionDetectionTypeToAllChildren

    #endregion //Collision Type


    //PID CONTROLLER VALUES ASSIGNMENTS
    #region PID Controller Values

    /// <summary>
    /// Will set the filter values that define 
    /// control-system dampening of MPL movement
    /// </summary>
    private void InitDefaultFilterValues()
    {
        //m_filterNum = new float[5];
        //m_filterNum[0] = 3.45734232259971e-005f;
        //m_filterNum[1] = 0.000138293692903989f;
        //m_filterNum[2] = 0.000207440539355983f;
        //m_filterNum[3] = 0.000138293692903989f;
        //m_filterNum[4] = 3.45734232259971e-005f;
        //m_filterDen = new float[5];
        //m_filterDen[0] = 1f;
        //m_filterDen[1] = -3.35349817878505f;
        //m_filterDen[2] = 4.21260313124799f;
        //m_filterDen[3] = -2.34938380707124f;
        //m_filterDen[4] = 0.490832029379911f;

        m_armFilterNum = new float[5];
        m_armFilterNum[0] = 3.45734232259971e-005f;
        m_armFilterNum[1] = 0.000138293692903989f;
        m_armFilterNum[2] = 0.000207440539355983f;
        m_armFilterNum[3] = 0.000138293692903989f;
        m_armFilterNum[4] = 3.45734232259971e-005f;

        m_armFilterDen = new float[5];
        m_armFilterDen[0] = 1f;
        m_armFilterDen[1] = -3.35349817878505f;
        m_armFilterDen[2] = 4.21260313124799f;
        m_armFilterDen[3] = -2.34938380707124f;
        m_armFilterDen[4] = 0.490832029379911f;

        m_wristFilterNum = new float[5];
        m_wristFilterNum[0] = 3.45734232259971e-005f;
        m_wristFilterNum[1] = 0.000138293692903989f;
        m_wristFilterNum[2] = 0.000207440539355983f;
        m_wristFilterNum[3] = 0.000138293692903989f;
        m_wristFilterNum[4] = 3.45734232259971e-005f;

        m_wristFilterDen = new float[5];
        m_wristFilterDen[0] = 1f;
        m_wristFilterDen[1] = -3.35349817878505f;
        m_wristFilterDen[2] = 4.21260313124799f;
        m_wristFilterDen[3] = -2.34938380707124f;
        m_wristFilterDen[4] = 0.490832029379911f;

        m_fingerFilterNum = new float[5];
        m_fingerFilterNum[0] = 3.45734232259971e-005f;
        m_fingerFilterNum[1] = 0.000138293692903989f;
        m_fingerFilterNum[2] = 0.000207440539355983f;
        m_fingerFilterNum[3] = 0.000138293692903989f;
        m_fingerFilterNum[4] = 3.45734232259971e-005f;

        m_fingerFilterDen = new float[5];
        m_fingerFilterDen[0] = 1f;
        m_fingerFilterDen[1] = -3.35349817878505f;
        m_fingerFilterDen[2] = 4.21260313124799f;
        m_fingerFilterDen[3] = -2.34938380707124f;
        m_fingerFilterDen[4] = 0.490832029379911f;
    }//function - InitDefaultFilterValues


    /// <summary>
    /// Will set the PID control values to the GameObjects
    /// </summary>
    private void SetupRightPIDValues()
    {
        if (m_haveRightMPL)
        {
            // Shoulder FE limited to 60 degrees per second to avoid whipping 
            // the thumb around.
            //ms_rShoulderFEPid       = new VulcanXInterface.PIDValues(40f, 10f, 10f, 0.0f, 60f);
            //ms_rShoulderAAPid       = new VulcanXInterface.PIDValues(40f, 10f, 50f, 0.0f, 120f);
            //ms_rHumeralRotPid       = new VulcanXInterface.PIDValues(25f, 10f, 50f, 0.0f, 120f);
            //ms_rElbowFEPid          = new VulcanXInterface.PIDValues(25f, 10f, 50f, 0.0f, 120f);
            //ms_rShoulderFEPid = new VulcanXInterface.PIDValues(30000f, 100f, 50f, 0.0f, 180f);  9/11/13
            //ms_rShoulderFEPid = new VulcanXInterface.PIDValues(24000f, 0.1f, 50f, 0.0f, 360f); //Fast Arms Build
            ms_rShoulderFEPid = new VulcanXInterface.PIDValues(240f, 0.1f, 50f, 0.0f, 180f);
            ms_rShoulderAAPid = new VulcanXInterface.PIDValues(85f, 10f, 50f, 0.0f, 180f);
            ms_rHumeralRotPid = new VulcanXInterface.PIDValues(85f, 10f, 50f, 0.0f, 180f);
            ms_rElbowFEPid = new VulcanXInterface.PIDValues(85f, 10f, 50f, 0.0f, 180f);
            ms_rWristRotPid = new VulcanXInterface.PIDValues(50f, 10f, 50f, 0.0f, 120f);
            ms_rWristDevPid = new VulcanXInterface.PIDValues(75f, 10f, 50f, 0.0f, 180f);
            ms_rWristFEPid = new VulcanXInterface.PIDValues(75f, 10f, 50f, 0.0f, 180f);
            ms_rThAAPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_rThFEPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_rThMCPPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_rThDistalPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_rIndAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rIndMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rIndProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rIndDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rMidDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rMidAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rMidMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rMidProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rMidDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rRingAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rRingMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rRingProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rRingDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rLittleAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rLittleMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rLittleProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_rLittleDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
        }
    }//function - SetupRightPIDValues


    /// <summary>
    /// Will set the PID control values to the GameObjects
    /// </summary>
    private void SetupLeftPIDValues()
    {
        if (m_haveLeftMPL)
        {
            // Shoulder FE limited to 60 degrees per second to avoid whipping 
            // the thumb around.
            //ms_lShoulderFEPid = new VulcanXInterface.PIDValues(24000f, 0.1f, 50f, 0.0f, 360f); //Fast Arms Build
            ms_lShoulderFEPid = new VulcanXInterface.PIDValues(240f, 0.1f, 50f, 0.0f, 180f);
            ms_lShoulderAAPid = new VulcanXInterface.PIDValues(85f, 10f, 50f, 0.0f, 180f);
            ms_lHumeralRotPid = new VulcanXInterface.PIDValues(85f, 10f, 50f, 0.0f, 180f);
            ms_lElbowFEPid = new VulcanXInterface.PIDValues(85f, 10f, 50f, 0.0f, 180f);
            ms_lWristRotPid = new VulcanXInterface.PIDValues(50f, 10f, 50f, 0.0f, 120f);
            ms_lWristDevPid = new VulcanXInterface.PIDValues(75f, 10f, 50f, 0.0f, 180f);
            ms_lWristFEPid = new VulcanXInterface.PIDValues(75f, 10f, 50f, 0.0f, 180f);
            ms_lThAAPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_lThFEPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_lThMCPPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_lThDistalPid = new VulcanXInterface.PIDValues(15f, 10f, 20f, 0.0f, 360f);
            ms_lIndAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lIndMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lIndProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lIndDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lMidDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lMidAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lMidMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lMidProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lMidDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lRingAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lRingMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lRingProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lRingDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lLittleAAPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lLittleMCPPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lLittleProximalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
            ms_lLittleDistalPid = new VulcanXInterface.PIDValues(35f, 10f, 20f, 0.0f, 360f);
        }
    }//function - SetupLeftPIDValues

    #endregion //PID Controller Values

    #endregion //Physics Values


    //MATERIALS AND COLOR
    #region Materials and Color

    #endregion //Materials and Color

    #endregion //MPL Objects and Properties


    #endregion //Initialization Functions (Percept Data, GameObjects, Object Properties)


    //---------------------------------------
    // FUNCTIONS - VULCANX COM and CMD/PERCEPTS FORMATTING
    //---------------------------------------
    #region VulcanX Comm and CMD/Percepts Formatting (From Obj Info)

    //PERCEPTS
    #region Sending Percepts

    /// <summary>
    /// Will send percepts for testing
    /// </summary>
    private void SendFakePercepts()
    {
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 0,
            ms_rightShoulderFE * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 4,
            ms_rShoulderRoot.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 12,
            ms_rightShoulderAA * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 16,
            ms_rShoulderFlexAssembly.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 24,
            ms_rightHumeralRot * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 28,
            ms_rShoulderShell.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 36,
            ms_rightElbowFE * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 40,
            ms_rHumeralRotatorElbow.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 48,
            ms_rightWristRot * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 52,
            ms_rForeArm.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 60,
            ms_rightWristDev * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 64,
            ms_rWristShell.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 72,
            ms_rightWristFE * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 76,
            ms_rWristDev.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 84,
            ms_rPalmToIndMetaCarpal.angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 88,
            ms_rPalmToIndMetaCarpal.velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 96,
            ms_rIndMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 100,
            ms_rIndMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 108,
            ms_rIndProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 112,
            ms_rIndProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 120,
            ms_rIndMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 124,
            ms_rIndMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 132,
            ms_rPalmToMidMetaCarpal.angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 136,
            ms_rPalmToMidMetaCarpal.velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 144,
            ms_rMidMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 148,
            ms_rMidMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 156,
            ms_rMidProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 160,
            ms_rMidProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 168,
            ms_rMidMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 172,
            ms_rMidMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 180,
            ms_rPalmToRingMetaCarpal.angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 184,
            ms_rPalmToRingMetaCarpal.velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 192,
            ms_rRingMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 196,
            ms_rRingMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 204,
            ms_rRingProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 208,
            ms_rRingProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 216,
            ms_rRingMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 220,
            ms_rRingMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 228,
            ms_rPalmToLittleMetaCarpal.angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 232,
            ms_rPalmToLittleMetaCarpal.velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 240,
            ms_rLittleMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 244,
            ms_rLittleMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 252,
            ms_rLittleProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 256,
            ms_rLittleProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 264,
            ms_rLittleMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 268,
            ms_rLittleMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 276,
            ms_rPalmToPlanetaryAsm.angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 280,
            ms_rPalmToPlanetaryAsm.angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 288,
            ms_rPlanetaryAsm.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 292,
            ms_rPlanetaryAsm.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 300,
            ms_rThProximal1.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 304,
            ms_rThProximal1.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 312,
            ms_rThProximal2.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);
        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 316,
            ms_rThProximal2.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

        if (ms_rSegPercepts != null)
        {
            // Segment contacts
            short[] contacts = ms_rSegPercepts.Contacts;
            int indx = 324;
            for (int i = 0; i < contacts.Length; i++)
            {
                VIEUtil.PacketUtils.PutShort(ms_rPerceptData, indx, contacts[i]);
                indx += sizeof(short);
            }
        }

#if UNITY_EDITOR
        ms_rightPerceptUdp.Send(ms_rPerceptData, ms_rPerceptData.Length,
            ms_rightBroadcastAddr);
#endif

    }//function - SendFakePercepts


    /// <summary>
    /// Will send percepts for RIGHT MPL
    /// </summary>
    private void SendRightPercepts()
    {
        if (m_haveRightMPL && m_logRightPerceptsEnabled)
        {
            // Debug.Log("Sending Percepts - Right");
            
            //Profiler.BeginSample("SendPercepts");
            //int i;
            //int j;
            //byte[] temp;

            //Upper Arm Joints = 7 Joints * 3 values per joint * 4 bytes per value = 84 bytes)... bytes 0-84
            #region Upper Arm Joints

            // Shoulder FE.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 0,
                ms_rShoulderRoot.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 4,
                ms_rShoulderRoot.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Shoulder AA.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 12,
                ms_rShoulderFlexAssembly.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 16,
                ms_rShoulderFlexAssembly.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Humeral rot.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 24,
                ms_rShoulderShell.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 28,
                ms_rShoulderShell.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Elbow FE.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 36,
                ms_rHumeralRotatorElbow.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 40,
                ms_rHumeralRotatorElbow.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Wrist rot.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 48,
                ms_rForeArm.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 52,
                ms_rForeArm.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Wrist AA.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 60,
                ms_rWristShell.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 64,
                ms_rWristShell.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Wrist FE.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 72,
                ms_rWristDev.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 76,
                ms_rWristDev.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            #endregion //Upper Arm Joints

            //Fingers and Thumb = 20 Joints * 3 values per joint * 4 bytes per value = 240 bytes)... bytes 85-324
            #region Fingers and Thumb

            // Index AA.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 84,
                ms_rPalmToIndMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 88,
                ms_rPalmToIndMetaCarpal.velocity * Mathf.Deg2Rad);

            // Index MCP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 96,
                ms_rIndMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 100,
                ms_rIndMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Index PIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 108,
                ms_rIndProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 112,
                ms_rIndProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Index DIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 120,
                ms_rIndMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 124,
                ms_rIndMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Middle AA.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 132,
                ms_rPalmToMidMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 136,
                ms_rPalmToMidMetaCarpal.velocity * Mathf.Deg2Rad);

            // Middle MCP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 144,
                ms_rMidMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 148,
                ms_rMidMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Middle PIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 156,
                ms_rMidProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 160,
                ms_rMidProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Middle DIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 168,
                ms_rMidMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 172,
                ms_rMidMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Ring AA.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 180,
                ms_rPalmToRingMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 184,
                ms_rPalmToRingMetaCarpal.velocity * Mathf.Deg2Rad);

            // Ring MCP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 192,
                ms_rRingMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 196,
                ms_rRingMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Ring PIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 204,
                ms_rRingProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 208,
                ms_rRingProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Ring DIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 216,
                ms_rRingMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 220,
                ms_rRingMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Little AA.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 228,
                ms_rPalmToLittleMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 232,
                ms_rPalmToLittleMetaCarpal.velocity * Mathf.Deg2Rad);

            // Little MCP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 240,
                ms_rLittleMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 244,
                ms_rLittleMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Little PIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 252,
                ms_rLittleProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 256,
                ms_rLittleProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Little DIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 264,
                ms_rLittleMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 268,
                ms_rLittleMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Thumb AA.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 276,
                ms_rPalmToPlanetaryAsm.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 280,
                ms_rPalmToPlanetaryAsm.velocity * Mathf.Deg2Rad);

            // Thumb FE.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 288,
                ms_rPlanetaryAsm.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 292,
                ms_rPlanetaryAsm.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Thumb MCP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 300,
                ms_rThProximal1.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 304,
                ms_rThProximal1.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Thumb DIP.
            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 312,
                ms_rThProximal2.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, 316,
                ms_rThProximal2.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            #endregion //Fingers and Thumb

            // Segment contacts
            if (ms_rSegPercepts != null)
            {
                //Reported by VulcanX to user:
                //   ContactPerceptsType     contact         74 (37 values (2 bytes each), so (2x37) enum for each of potential contact sensors)
                //   FtsnForcePerceptsType   force           60 (3-axis x 32-bit values (3 x 4 bytes) for each of 5 fingers, so (3x4x5 = 60)) - force sensors
                //   FtsnAccelPerceptsType   acceleration    60 (3-axis x 32-bit values (3 x 4 bytes) for each of 5 fingers, so (3x4x5 = 60)) - accel sensors
                //   FtsnTempPerceptsType    temperature     20 (1 x 32-bit value (1 x 4 bytes) for each of 5 fingers, so (1x4x5 = 20)) - temperature sensors

                //Segment contacts = 37 sensors * 1 value per sensor * 2 bytes per value = 74 bytes)... bytes 325-398
                #region Contact Sensors
                
                short[] contacts = ms_rSegPercepts.Contacts;
                int indx = 324;
                for (int i = 0; i < contacts.Length; i++)
                {
                    VIEUtil.PacketUtils.PutShort(ms_rPerceptData, indx, contacts[i]);
                    indx += sizeof(short);
                }

                #endregion //Contact Sensors
                
                //Segment forces (OLD) = 5 sensors * 3 axes/values per sensor * 4 bytes per value = 60 bytes)... bytes 399-458
                //Segment forces (NEW) = 5 sensors * 14 pads/values per sensor * 4 bytes per value = 280 bytes)... bytes 399-678
                #region Force Sensors (of FTSN)
                
                //Vector3[] forces = ms_rSegPercepts.Forces;
                float[,] forces = ms_rSegPercepts.Forces;
                foreach (int fingerIndx in Enum.GetValues(typeof(SensorArray.SegmentPerceptFTSNIdType)))
                {
                    for (int i = 0; i < NUMBER_FTSN_PADS; i++)
                    {
                        if (forces[fingerIndx, i] != 0)
                        {
                            //Debug.Log("Forced communicated from Array (: " + fingerIndx.ToString() + ", " + i.ToString() + "): " + forces[fingerIndx, i].ToString());
                        }//if - check for non-zero value

                        VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, indx, forces[fingerIndx, i]);
                        indx += sizeof(float);

                        //change coordinates
                        //VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, indx, forces[i].x);
                        //indx += sizeof(float);
                        //VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, indx, forces[i].z);
                        //indx += sizeof(float);
                        //VIEUtil.PacketUtils.PutFloat(ms_rPerceptData, indx, forces[i].y);
                        //indx += sizeof(float);

                    }//for - traverse # pads in each FTSN sensor

                }//foreach - Each Finger in hand including thumb

                #endregion //Force Sensors (of FTSN)

                //Segment Accelerometers (not supported) = 60 bytes

                //Segment Temperatures (not supported) = 20 bytes

            }//if - test for Stimulation Percepts enabled

#if UNITY_EDITOR
            // Debug.Log( string.Format( "Percept packet data length: {0}...", ms_rPerceptData.Length ) );
            ms_rightPerceptUdp.Send(ms_rPerceptData, ms_rPerceptData.Length,
                ms_rightBroadcastAddr);
#endif

            //Profiler.EndSample();

        }//if - check to see if MPL is present, and flag for reporting percepts is enabled

    }//function - SendRightPercepts


    /// <summary>
    /// Will send percepts for LEFT MPL
    /// </summary>
    private void SendLeftPercepts()
    {
        if (m_haveLeftMPL && m_logLeftPerceptsEnabled)
        {
            //Debug.Log("Sending Percepts - Left");

            //Profiler.BeginSample("SendPercepts");
            //int i;
            //int j;
            //byte[] temp;

            //Upper Arm Joints = 7 Joints * 3 values per joint * 4 bytes per value = 84 bytes)... bytes 0-84
            #region Upper Arm Joints

            // Shoulder FE.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 0,
                ms_lShoulderRoot.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 4,
                ms_lShoulderRoot.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Shoulder AA.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 12,
                ms_lShoulderFlexAssembly.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 16,
                ms_lShoulderFlexAssembly.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Humeral rot.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 24,
                ms_lShoulderShell.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 28,
                ms_lShoulderShell.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Elbow FE.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 36,
                ms_lHumeralRotatorElbow.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 40,
                ms_lHumeralRotatorElbow.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Wrist rot.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 48,
                ms_lForeArm.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 52,
                ms_lForeArm.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Wrist AA.

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 60,
                ms_lWristShell.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 64,
                ms_lWristShell.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Wrist FE.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 72,
                ms_lWristDev.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 76,
                ms_lWristDev.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            #endregion //Upper Arm Joints

            //Fingers and Thumb = 20 Joints * 3 values per joint * 4 bytes per value = 240 bytes)... bytes 85-324
            #region Fingers and Thumb

            // Index AA.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 84,
                ms_lPalmToIndMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 88,
                ms_lPalmToIndMetaCarpal.velocity * Mathf.Deg2Rad);

            // Index MCP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 96,
                ms_lIndMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 100,
                ms_lIndMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Index PIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 108,
                ms_lIndProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 112,
                ms_lIndProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Index DIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 120,
                ms_lIndMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 124,
                ms_lIndMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Middle AA.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 132,
                ms_lPalmToMidMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 136,
                ms_lPalmToMidMetaCarpal.velocity * Mathf.Deg2Rad);

            // Middle MCP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 144,
                ms_lMidMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 148,
                ms_lMidMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Middle PIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 156,
                ms_lMidProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 160,
                ms_lMidProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Middle DIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 168,
                ms_lMidMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 172,
                ms_lMidMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Ring AA.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 180,
                ms_lPalmToRingMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 184,
                ms_lPalmToRingMetaCarpal.velocity * Mathf.Deg2Rad);

            // Ring MCP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 192,
                ms_lRingMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 196,
                ms_lRingMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Ring PIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 204,
                ms_lRingProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 208,
                ms_lRingProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Ring DIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 216,
                ms_lRingMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 220,
                ms_lRingMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Little AA.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 228,
                ms_lPalmToLittleMetaCarpal.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 232,
                ms_lPalmToLittleMetaCarpal.velocity * Mathf.Deg2Rad);

            // Little MCP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 240,
                ms_lLittleMetaCarpal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 244,
                ms_lLittleMetaCarpal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Little PIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 252,
                ms_lLittleProximal.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 256,
                ms_lLittleProximal.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Little DIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 264,
                ms_lLittleMedial.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 268,
                ms_lLittleMedial.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Thumb AA.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 276,
                ms_lPalmToPlanetaryAsm.angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 280,
                ms_lPalmToPlanetaryAsm.velocity * Mathf.Deg2Rad);

            // Thumb FE.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 288,
                ms_lPlanetaryAsm.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 292,
                ms_lPlanetaryAsm.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Thumb MCP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 300,
                ms_lThProximal1.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 304,
                ms_lThProximal1.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            // Thumb DIP.
            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 312,
                ms_lThProximal2.GetComponent<HingeJoint>().angle * Mathf.Deg2Rad);

            VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, 316,
                ms_lThProximal2.GetComponent<HingeJoint>().velocity * Mathf.Deg2Rad);

            #endregion //Fingers and Thumb

            if (ms_lSegPercepts != null)
            {
                //Reported by VulcanX to user:
                //   ContactPerceptsType     contact         74 (37 values (2 bytes each), so (2x37) enum for each of potential contact sensors)
                //   FtsnForcePerceptsType   force           60 (3-axis x 32-bit values (3 x 4 bytes) for each of 5 fingers, so (3x4x5 = 60)) - force sensors
                //   FtsnAccelPerceptsType   acceleration    60 (3-axis x 32-bit values (3 x 4 bytes) for each of 5 fingers, so (3x4x5 = 60)) - accel sensors
                //   FtsnTempPerceptsType    temperature     20 (1 x 32-bit value (1 x 4 bytes) for each of 5 fingers, so (1x4x5 = 20)) - temperature sensors
	            
                //Segment contacts = 37 sensors * 1 value per sensor * 2 bytes per value = 74 bytes)... bytes 325-398
                #region Contact Sensors
                
                short[] contacts = ms_lSegPercepts.Contacts;
                int indx = 324;
                for (int i = 0; i < contacts.Length; i++) //currently alloted for 37 sensors 
                {
                    VIEUtil.PacketUtils.PutShort(ms_lPerceptData, indx, contacts[i]);
                    indx += sizeof(short); // bytes per short
                }

                #endregion //Contact Sensors

                //Segment forces (OLD) = 5 sensors * 3 axes/values per sensor * 4 bytes per value = 60 bytes)... bytes 399-458
                //Segment forces (NEW) = 5 sensors * 14 pads/values per sensor * 4 bytes per value = 280 bytes)... bytes 399-678
                #region Force Sensors (of FTSN)

                //Vector3[] forces = ms_lSegPercepts.Forces; //Grab the forces collected from the "SensorArray" object
                float[,] forces = ms_lSegPercepts.Forces; //Grab the forces collected from the "SensorArray" object
                foreach (int fingerIndx in Enum.GetValues(typeof(SensorArray.SegmentPerceptFTSNIdType)))
                {
                    for (int i = 0; i < NUMBER_FTSN_PADS; i++)
                    {
                        if (forces[fingerIndx, i] != 0)
                        {
                            //Debug.Log("Forced communicated from Array (: " + fingerIndx.ToString() + ", " + i.ToString() + "): " + forces[fingerIndx, i].ToString());
                        }//if - check for non-zero value
                        
                        VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, indx, forces[fingerIndx, i]);
                        indx += sizeof(float);
                        
                        //(OLD) = Sensors with 3 axes each
                        //change coordinates
                        //VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, indx, forces[i].x);
                        //indx += sizeof(float);
                        //VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, indx, forces[i].z);
                        //indx += sizeof(float);
                        //VIEUtil.PacketUtils.PutFloat(ms_lPerceptData, indx, forces[i].y);
                        //indx += sizeof(float);
                    }//for - traverse # pads in each FTSN sensor

                }//foreach - Each Finger in hand including thumb
                
                #endregion //Force Sensors (of FTSN)

                //Segment Accelerometers (not supported) = 60 bytes
                
                //Segment Temperatures (not supported) = 20 bytes

            }//if - test for Stimulation Percepts enabled

            //Test to see if RightMPL is present (bimanual) - if not, then report on the Right Percepts stream instead of left
            //if (!m_haveRightMPL)
            //{
            // There's no right MPL, but the right port is the default 
            // port, so still send percept data on the right port.
            //    ms_rightPerceptUdp.Send(ms_lPerceptData, ms_lPerceptData.Length,
            //        ms_rightBroadcastAddr);
            //}
            //else
            //{
#if UNITY_EDITOR
            ms_leftPerceptUdp.Send(ms_lPerceptData, ms_lPerceptData.Length, ms_leftBroadcastAddr);
#endif            
            //}//if - check for Right MPL

            //Profiler.EndSample();

        }//if - check to see if MPL is present, and flag for reporting percepts is enabled

    }//function - SendLeftPercepts

    #endregion //Sending Percepts


    //COMMANDS
    #region Reading Commands

    /// <summary>
    /// Reads packet from VulcanX if available.
    /// </summary>
    private void ReadRightPort()
    {
        //Profiler.BeginSample("ReadRightPort");
        // Read port.

#if UNITY_EDITOR
        if (ms_rightRecvSock.Available > 0)
        {
			// Get the most recent packet.
			while(ms_rightRecvSock.Available > 0)
	            ms_rightRecvSock.ReceiveFrom(ms_rBuffer, ref ms_rightVulcanXEndPt);

            ms_rightShoulderFE =
                BitConverter.ToSingle(ms_rBuffer, 0) * Mathf.Rad2Deg;
            ms_rightShoulderAA =
                BitConverter.ToSingle(ms_rBuffer, 4) * Mathf.Rad2Deg;
            ms_rightHumeralRot =
                BitConverter.ToSingle(ms_rBuffer, 8) * Mathf.Rad2Deg;
            ms_rightElbowFE =
                BitConverter.ToSingle(ms_rBuffer, 12) * Mathf.Rad2Deg;
            ms_rightWristRot =
                BitConverter.ToSingle(ms_rBuffer, 16) * Mathf.Rad2Deg;
            ms_rightWristDev =
                BitConverter.ToSingle(ms_rBuffer, 20) * Mathf.Rad2Deg;
            ms_rightWristFE =
                BitConverter.ToSingle(ms_rBuffer, 24) * Mathf.Rad2Deg;
            ms_rightIndexAA =
                BitConverter.ToSingle(ms_rBuffer, 28) * Mathf.Rad2Deg;
            ms_rightIndexMCP =
                BitConverter.ToSingle(ms_rBuffer, 32) * Mathf.Rad2Deg;
            ms_rightIndexPIP =
                BitConverter.ToSingle(ms_rBuffer, 36) * Mathf.Rad2Deg;
            ms_rightIndexDIP =
                BitConverter.ToSingle(ms_rBuffer, 40) * Mathf.Rad2Deg;
            ms_rightMiddleAA =
                BitConverter.ToSingle(ms_rBuffer, 44) * Mathf.Rad2Deg;
            ms_rightMiddleMCP =
                BitConverter.ToSingle(ms_rBuffer, 48) * Mathf.Rad2Deg;
            ms_rightMiddlePIP =
                BitConverter.ToSingle(ms_rBuffer, 52) * Mathf.Rad2Deg;
            ms_rightMiddleDIP =
                BitConverter.ToSingle(ms_rBuffer, 56) * Mathf.Rad2Deg;
            ms_rightRingAA =
                BitConverter.ToSingle(ms_rBuffer, 60) * Mathf.Rad2Deg;
            ms_rightRingMCP =
                BitConverter.ToSingle(ms_rBuffer, 64) * Mathf.Rad2Deg;
            ms_rightRingPIP =
                BitConverter.ToSingle(ms_rBuffer, 68) * Mathf.Rad2Deg;
            ms_rightRingDIP =
                BitConverter.ToSingle(ms_rBuffer, 72) * Mathf.Rad2Deg;
            ms_rightLittleAA =
                BitConverter.ToSingle(ms_rBuffer, 76) * Mathf.Rad2Deg;
            ms_rightLittleMCP =
                BitConverter.ToSingle(ms_rBuffer, 80) * Mathf.Rad2Deg;
            ms_rightLittlePIP =
                BitConverter.ToSingle(ms_rBuffer, 84) * Mathf.Rad2Deg;
            ms_rightLittleDIP =
                BitConverter.ToSingle(ms_rBuffer, 88) * Mathf.Rad2Deg;
            ms_rightThumbAA =
                BitConverter.ToSingle(ms_rBuffer, 92) * Mathf.Rad2Deg;
            ms_rightThumbFE =
                BitConverter.ToSingle(ms_rBuffer, 96) * Mathf.Rad2Deg;
            ms_rightThumbMCP =
                BitConverter.ToSingle(ms_rBuffer, 100) * Mathf.Rad2Deg;
            ms_rightThumbDIP =
                BitConverter.ToSingle(ms_rBuffer, 104) * Mathf.Rad2Deg;
        } // end if (ms_rightRecvSock.Available > 0)
          //Profiler.EndSample();
#endif
    }//function - ReadRightPort


    /// <summary>
    /// Reads packet from VulcanX if available.
    /// </summary>
    private void ReadLeftPort()
    {
#if UNITY_EDITOR
        //Profiler.BeginSample("ReadLeftPort");

        // If there is only one arm in use, the default right port is still 
        // used.
        //bool haveBytes = false;

        //if (!m_haveRightMPL)
        //    haveBytes = (ms_rightRecvSock.Available > 0);
        //else
        //    haveBytes = (ms_leftRecvSock.Available > 0);

        //if(haveBytes)
        //{
        //    // Get the most recent packet.
        //    while (haveBytes)
        //    {
        //        if (!m_haveRightMPL)
        //        {
        //            ms_rightRecvSock.ReceiveFrom(ms_lBuffer, ref ms_rightVulcanXEndPt);
        //            haveBytes = (ms_rightRecvSock.Available > 0);
        //        }
        //        else
        //        {
        //            ms_leftRecvSock.ReceiveFrom(ms_lBuffer, ref ms_leftVulcanXEndPt);
        //            haveBytes = (ms_leftRecvSock.Available > 0);
        //        }
        //    }

        // Read port.
        if (ms_leftRecvSock.Available > 0)
        {
			// Get the most recent packet.
			while(ms_leftRecvSock.Available > 0)
                ms_leftRecvSock.ReceiveFrom(ms_lBuffer, ref ms_leftVulcanXEndPt);

            ms_leftShoulderFE =
                BitConverter.ToSingle(ms_lBuffer, 0) * Mathf.Rad2Deg;
            ms_leftShoulderAA =
                BitConverter.ToSingle(ms_lBuffer, 4) * Mathf.Rad2Deg;
            ms_leftHumeralRot =
                BitConverter.ToSingle(ms_lBuffer, 8) * Mathf.Rad2Deg;
            ms_leftElbowFE =
                BitConverter.ToSingle(ms_lBuffer, 12) * Mathf.Rad2Deg;
            ms_leftWristRot =
                BitConverter.ToSingle(ms_lBuffer, 16) * Mathf.Rad2Deg;
            ms_leftWristDev =
                BitConverter.ToSingle(ms_lBuffer, 20) * Mathf.Rad2Deg;
            ms_leftWristFE =
                BitConverter.ToSingle(ms_lBuffer, 24) * Mathf.Rad2Deg;
            ms_leftIndexAA =
                BitConverter.ToSingle(ms_lBuffer, 28) * Mathf.Rad2Deg;
            ms_leftIndexMCP =
                BitConverter.ToSingle(ms_lBuffer, 32) * Mathf.Rad2Deg;
            ms_leftIndexPIP =
                BitConverter.ToSingle(ms_lBuffer, 36) * Mathf.Rad2Deg;
            ms_leftIndexDIP =
                BitConverter.ToSingle(ms_lBuffer, 40) * Mathf.Rad2Deg;
            ms_leftMiddleAA =
                BitConverter.ToSingle(ms_lBuffer, 44) * Mathf.Rad2Deg;
            ms_leftMiddleMCP =
                BitConverter.ToSingle(ms_lBuffer, 48) * Mathf.Rad2Deg;
            ms_leftMiddlePIP =
                BitConverter.ToSingle(ms_lBuffer, 52) * Mathf.Rad2Deg;
            ms_leftMiddleDIP =
                BitConverter.ToSingle(ms_lBuffer, 56) * Mathf.Rad2Deg;
            ms_leftRingAA =
                BitConverter.ToSingle(ms_lBuffer, 60) * Mathf.Rad2Deg;
            ms_leftRingMCP =
                BitConverter.ToSingle(ms_lBuffer, 64) * Mathf.Rad2Deg;
            ms_leftRingPIP =
                BitConverter.ToSingle(ms_lBuffer, 68) * Mathf.Rad2Deg;
            ms_leftRingDIP =
                BitConverter.ToSingle(ms_lBuffer, 72) * Mathf.Rad2Deg;
            ms_leftLittleAA =
                BitConverter.ToSingle(ms_lBuffer, 76) * Mathf.Rad2Deg;
            ms_leftLittleMCP =
                BitConverter.ToSingle(ms_lBuffer, 80) * Mathf.Rad2Deg;
            ms_leftLittlePIP =
                BitConverter.ToSingle(ms_lBuffer, 84) * Mathf.Rad2Deg;
            ms_leftLittleDIP =
                BitConverter.ToSingle(ms_lBuffer, 88) * Mathf.Rad2Deg;
            ms_leftThumbAA =
                BitConverter.ToSingle(ms_lBuffer, 92) * Mathf.Rad2Deg;
            ms_leftThumbFE =
                BitConverter.ToSingle(ms_lBuffer, 96) * Mathf.Rad2Deg;
            ms_leftThumbMCP =
                BitConverter.ToSingle(ms_lBuffer, 100) * Mathf.Rad2Deg;
            ms_leftThumbDIP =
                BitConverter.ToSingle(ms_lBuffer, 104) * Mathf.Rad2Deg;
        } // end if (haveBytes)
        //Profiler.EndSample();
#endif
    }//function - ReadLeftPort

    #endregion Reading Commands

    #endregion //VulcanX Comm and CMD/Percepts Formatting (From Obj Info)


    //---------------------------------------
    // FUNCTIONS - MPL MOVEMENT - COMMANDS
    //---------------------------------------
    //MPL Movement Command and Control
    #region MPL Movement - Commands

    /// <summary>
    /// This will set the GameObject (rigidbody and hing) values 
    /// for the RIGHT MPL elements.  
    /// </summary>
    private void CommandRightArm()
    {
        //Check to make sure vMPL Movement hasn't been disabled (externally, by pause, etc.)
        if (GetRightMPLMovementEnable())
        {
            //Profiler.BeginSample("CmdArm");
            SetHingeValue(ms_rShoulderRoot.GetComponent<HingeJoint>(), ms_rightShoulderFE, ms_rShoulderFEPid,
                m_armFilterNum, m_armFilterDen);
            SetHingeValue(ms_rShoulderFlexAssembly.GetComponent<HingeJoint>(), ms_rightShoulderAA, ms_rShoulderAAPid,
                m_armFilterNum, m_armFilterDen);

            SetHingeValue(ms_rShoulderShell.GetComponent<HingeJoint>(), ms_rightHumeralRot, ms_rHumeralRotPid,
                m_armFilterNum, m_armFilterDen);
            SetHingeValue(ms_rHumeralRotatorElbow.GetComponent<HingeJoint>(), ms_rightElbowFE, ms_rElbowFEPid,
                m_armFilterNum, m_armFilterDen);
            //ms_rElbowJoint.targetAngularVelocity =
            //    new Vector3(-ms_rightElbowFE * Mathf.Deg2Rad, 0, 0);            

            SetHingeValue(ms_rForeArm.GetComponent<HingeJoint>(), ms_rightWristRot, ms_rWristRotPid,
                m_armFilterNum, m_armFilterDen);

            SetHingeValue(ms_rWristShell.GetComponent<HingeJoint>(), ms_rightWristDev, ms_rWristDevPid,
                m_wristFilterNum, m_wristFilterDen);
            // This was for testing:
            //ms_rWristDev.rigidbody.AddRelativeTorque(ms_rWristShell.hingeJoint.axis * ms_rightWristDev, ForceMode.VelocityChange);

            SetHingeValue(ms_rWristDev.GetComponent<HingeJoint>(), ms_rightWristFE, ms_rWristFEPid,
                m_wristFilterNum, m_wristFilterDen);

            #region Thumb
            SetHingeValue(ms_rPalmToPlanetaryAsm, ms_rightThumbAA, ms_rThAAPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rPlanetaryAsm.GetComponent<HingeJoint>(), ms_rightThumbFE, ms_rThFEPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rThProximal1.GetComponent<HingeJoint>(), ms_rightThumbMCP, ms_rThMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rThProximal2.GetComponent<HingeJoint>(), ms_rightThumbDIP, ms_rThDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Little finger
            SetHingeValue(ms_rPalmToLittleMetaCarpal, ms_rightLittleAA, ms_rLittleAAPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rLittleMetaCarpal.GetComponent<HingeJoint>(), ms_rightLittleMCP, ms_rLittleMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rLittleProximal.GetComponent<HingeJoint>(), ms_rightLittlePIP, ms_rLittleProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rLittleMedial.GetComponent<HingeJoint>(), ms_rightLittleDIP, ms_rLittleDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Ring finger
            SetHingeValue(ms_rPalmToRingMetaCarpal, ms_rightRingAA, ms_rRingAAPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rRingMetaCarpal.GetComponent<HingeJoint>(), ms_rightRingMCP, ms_rRingMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rRingProximal.GetComponent<HingeJoint>(), ms_rightRingPIP, ms_rRingProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rRingMedial.GetComponent<HingeJoint>(), ms_rightRingDIP, ms_rRingDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Middle finger
            SetHingeValue(ms_rPalmToMidMetaCarpal, ms_rightMiddleAA, ms_rMidAAPid,
                m_fingerFilterNum, m_fingerFilterDen);
        
            //float newMidMcp; //BAW - 12/10/12 - Commented out unused Variables
            //float newMidPip; //BAW - 12/10/12 - Commented out unused Variables
            //float newMidDip; //BAW - 12/10/12 - Commented out unused Variables

            //AdjustFingerCmds(FingerE.MIDDLE, ms_rightMiddleMCP, ms_rightMiddlePIP,
            //    ms_rightMiddleDIP, out newMidMcp, out newMidPip, out newMidDip);

            SetHingeValue(ms_rMidMetaCarpal.GetComponent<HingeJoint>(), ms_rightMiddleMCP, ms_rMidMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rMidProximal.GetComponent<HingeJoint>(), ms_rightMiddlePIP, ms_rMidProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rMidMedial.GetComponent<HingeJoint>(), ms_rightMiddleDIP, ms_rMidDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            //SetHingeValue(ms_rMidMetaCarpal.hingeJoint, newMidMcp, ms_rMidMCPPid,
            //    m_fingerFilterNum, m_fingerFilterDen);
            //SetHingeValue(ms_rMidProximal.hingeJoint, newMidPip, ms_rMidProximalPid,
            //    m_fingerFilterNum, m_fingerFilterDen);
            //SetHingeValue(ms_rMidMedial.hingeJoint, newMidDip, ms_rMidDistalPid,
            //    m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Index finger
            SetHingeValue(ms_rPalmToIndMetaCarpal, ms_rightIndexAA, ms_rIndAAPid,
                m_fingerFilterNum, m_fingerFilterDen);

            //float newIndMcp; //BAW - 12/10/12 - Commented out unused Variables
            //float newIndPip; //BAW - 12/10/12  - Commented out unused Variables
            //float newIndDip; //BAW - 12/10/12  - Commented out unused Variables

            //AdjustFingerCmds(FingerE.INDEX, ms_rightIndexMCP,
            //    ms_rightIndexPIP, ms_rightIndexDIP,
            //    out newIndMcp, out newIndPip, out newIndDip);

            SetHingeValue(ms_rIndMetaCarpal.GetComponent<HingeJoint>(), ms_rightIndexMCP, ms_rIndMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rIndProximal.GetComponent<HingeJoint>(), ms_rightIndexPIP, ms_rIndProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_rIndMedial.GetComponent<HingeJoint>(), ms_rightIndexDIP, ms_rIndDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            //SetHingeValue(ms_rIndMetaCarpal.hingeJoint, newIndMcp, ms_rIndMCPPid,
            //    m_fingerFilterNum, m_fingerFilterDen);
            //SetHingeValue(ms_rIndProximal.hingeJoint, newIndPip, ms_rIndProximalPid,
            //    m_fingerFilterNum, m_fingerFilterDen);
            //SetHingeValue(ms_rIndMedial.hingeJoint, newIndDip, ms_rIndDistalPid,
            //    m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            //Profiler.EndSample();

        }//if - check for MoveEnable
         
    }//function - CommandRightArm
    

    /// <summary>
    /// This will set the GameObject (rigidbody and hing) values 
    /// for the LEFT MPL elements.  
    /// </summary>
    private void CommandLeftArm()
    {
        if (GetLeftMPLMovementEnable())
        {
            //Profiler.BeginSample("CmdArm");
            SetHingeValue(ms_lShoulderRoot.GetComponent<HingeJoint>(), ms_leftShoulderFE, ms_lShoulderFEPid,
                m_armFilterNum, m_armFilterDen);
            SetHingeValue(ms_lShoulderFlexAssembly.GetComponent<HingeJoint>(), ms_leftShoulderAA, ms_lShoulderAAPid,
                m_armFilterNum, m_armFilterDen);

            SetHingeValue(ms_lShoulderShell.GetComponent<HingeJoint>(), ms_leftHumeralRot, ms_lHumeralRotPid,
                m_armFilterNum, m_armFilterDen);
            SetHingeValue(ms_lHumeralRotatorElbow.GetComponent<HingeJoint>(), ms_leftElbowFE, ms_lElbowFEPid,
                m_armFilterNum, m_armFilterDen);

            SetHingeValue(ms_lForeArm.GetComponent<HingeJoint>(), ms_leftWristRot, ms_lWristRotPid,
                m_armFilterNum, m_armFilterDen);

            SetHingeValue(ms_lWristShell.GetComponent<HingeJoint>(), ms_leftWristDev, ms_lWristDevPid,
                m_wristFilterNum, m_wristFilterDen);

            SetHingeValue(ms_lWristDev.GetComponent<HingeJoint>(), ms_leftWristFE, ms_lWristFEPid,
                m_wristFilterNum, m_wristFilterDen);

            #region Thumb
            SetHingeValue(ms_lPalmToPlanetaryAsm, ms_leftThumbAA, ms_lThAAPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lPlanetaryAsm.GetComponent<HingeJoint>(), ms_leftThumbFE, ms_lThFEPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lThProximal1.GetComponent<HingeJoint>(), ms_leftThumbMCP, ms_lThMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lThProximal2.GetComponent<HingeJoint>(), ms_leftThumbDIP, ms_lThDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Little finger
            SetHingeValue(ms_lPalmToLittleMetaCarpal, ms_leftLittleAA, ms_lLittleAAPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lLittleMetaCarpal.GetComponent<HingeJoint>(), ms_leftLittleMCP, ms_lLittleMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lLittleProximal.GetComponent<HingeJoint>(), ms_leftLittlePIP, ms_lLittleProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lLittleMedial.GetComponent<HingeJoint>(), ms_leftLittleDIP, ms_lLittleDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Ring finger
            SetHingeValue(ms_lPalmToRingMetaCarpal, ms_leftRingAA, ms_lRingAAPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lRingMetaCarpal.GetComponent<HingeJoint>(), ms_leftRingMCP, ms_lRingMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lRingProximal.GetComponent<HingeJoint>(), ms_leftRingPIP, ms_lRingProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lRingMedial.GetComponent<HingeJoint>(), ms_leftRingDIP, ms_lRingDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Middle finger
            SetHingeValue(ms_lPalmToMidMetaCarpal, ms_leftMiddleAA, ms_lMidAAPid,
                m_fingerFilterNum, m_fingerFilterDen);

            SetHingeValue(ms_lMidMetaCarpal.GetComponent<HingeJoint>(), ms_leftMiddleMCP, ms_lMidMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lMidProximal.GetComponent<HingeJoint>(), ms_leftMiddlePIP, ms_lMidProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lMidMedial.GetComponent<HingeJoint>(), ms_leftMiddleDIP, ms_lMidDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            #region Index finger
            SetHingeValue(ms_lPalmToIndMetaCarpal, ms_leftIndexAA, ms_lIndAAPid,
                m_fingerFilterNum, m_fingerFilterDen);

            SetHingeValue(ms_lIndMetaCarpal.GetComponent<HingeJoint>(), ms_leftIndexMCP, ms_lIndMCPPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lIndProximal.GetComponent<HingeJoint>(), ms_leftIndexPIP, ms_lIndProximalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            SetHingeValue(ms_lIndMedial.GetComponent<HingeJoint>(), ms_leftIndexDIP, ms_lIndDistalPid,
                m_fingerFilterNum, m_fingerFilterDen);
            #endregion

            //Profiler.EndSample();

        }//if - check for enabled movement

    }//function - CommandLeftArm


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

    // Added by Chris
    //public float[] GetRightFingerAngles()
    //{
    //    float[] angles = new float[20];

    //    angles[0] = ms_rightIndexAA;
    //    angles[1] = ms_rightIndexMCP;
    //    angles[2] = ms_rightIndexPIP;
    //    angles[3] = ms_rightIndexDIP;

    //    angles[4] = ms_rightMiddleAA;
    //    angles[5] = ms_rightMiddleMCP;
    //    angles[6] = ms_rightMiddlePIP;
    //    angles[7] = ms_rightMiddleDIP;

    //    angles[8] = ms_rightRingAA;
    //    angles[9] = ms_rightRingMCP;
    //    angles[10] = ms_rightRingPIP;
    //    angles[11] = ms_rightRingDIP;

    //    angles[12] = ms_rightLittleAA;
    //    angles[13] = ms_rightLittleMCP;
    //    angles[14] = ms_rightLittlePIP;
    //    angles[15] = ms_rightLittleDIP;

    //    angles[16] = ms_rightThumbAA;
    //    angles[17] = ms_rightThumbFE;
    //    angles[18] = ms_rightThumbMCP;
    //    angles[19] = ms_rightThumbDIP;

    //    return angles;
    //}

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


    #endregion //MPL Movement - Commands

    //MPL Movement Enable Functions
    #region MPL Movement/Frozen Flag Set/Get

    /// <summary>
    /// GET/SET Functions for enabling and disabling
    /// all movement for the MPL
    /// </summary>
    public void SetRightMPLMovementEnable(bool bMovementFlag)
    {
        m_MPL_Movement_Enabled_Right = bMovementFlag;

    }//function - SetRightMPLMovementEnable

    public bool GetRightMPLMovementEnable()
    {
        return m_MPL_Movement_Enabled_Right;

    }//function - GetRightMPLMovementEnable


    /// <summary>
    /// GET/SET Functions for enabling and disabling
    /// MUD-based movement for the MPL
    /// </summary>
    public void SetRightMPLMUDMovementEnable(bool bMovementFlag)
    {
        m_MPL_MUD_Movement_Enabled_Right = bMovementFlag;

    }//function - SetRightMPLMUDMovementEnable

    public bool GetRightMPLMUDMovementEnable()
    {
        return m_MPL_MUD_Movement_Enabled_Right;

    }//function - GetRightMPLMUDMovementEnable


    /// <summary>
    /// GET/SET Functions for enabling and disabling
    /// all movement for the MPL
    /// </summary>
    public void SetLeftMPLMovementEnable(bool bMovementFlag)
    {
        m_MPL_Movement_Enabled_Left = bMovementFlag;

    }//function - SetLeftMPLMovementEnable

    public bool GetLeftMPLMovementEnable()
    {
        return m_MPL_Movement_Enabled_Left;

    }//function - GetLeftMPLMovementEnable

    
    /// <summary>
    /// GET/SET Functions for enabling and disabling
    /// MUD-based movement for the MPL
    /// </summary>
    public void SetLeftMPLMUDMovementEnable(bool bMovementFlag)
    {
        m_MPL_MUD_Movement_Enabled_Left = bMovementFlag;

    }//function - SetLeftMPLMUDMovementEnable

    public bool GetLeftMPLMUDMovementEnable()
    {
        return m_MPL_MUD_Movement_Enabled_Left;

    }//function - GetLeftMPLMUDMovementEnable


    #endregion //MPL Movement/Frozen Flag Set/Get


    //---------------------------------------
    // FUNCTIONS - MPL MOVEMENT - HINGES
    //---------------------------------------
    #region MPL Movement - Physics

    //Joint Functions
    #region Joints

    /// <summary>
    /// Will return an int value of the joint pointer passed in
    /// </summary>
    private int FindJoint(HingeJoint[] joints, Rigidbody attached)
    {
        int ind = -1;
        for (int i = 0; i < joints.Length; i++)
        {
            if (joints[i].connectedBody == attached)
            {
                ind = i;
                break;
            }
        }

        if (ind == -1)
        {
            throw new NullReferenceException(
                "No hinge found that is connected to " +
                attached.gameObject.name + "'s rigid body.");
        }

        return ind;
    }//function - FindJoint
    
    #endregion //Joints


    //Hinge Movement
    #region Hinge Movement and Limit Functions

    /// <summary>
    /// If hinge's max limit is 180, may need to adjust the reported
    /// hinge angle in case the hinge has temporarily exceeded its max
    /// limit.  This could also be a problem if the hinge's min limit is
    /// -180.  Note that the hinge's min and max is passed in.  Although
    /// the limit can be obtained directly from the hinge, this is
    /// slower than caching the limits in floats.  It's assumed that the
    /// caller also uses the limits, so the caller can cache the limits
    /// before calling this method.
    /// 
    /// For example,
    /// if the hinge angle > 180, the hinge angle may be reported as -179.3.
    /// Negative angles at the 180 degree limit may result in the arm trying
    /// to move in the wrong direction and thus locking.
    /// </summary>
    /// <param name="hinge"></param>
    /// <param name="min">This should be hinge.limits.min.</param>
    /// <param name="max">This should be hinge.limits.max.</param>
    /// <returns></returns>
    private float AdjustHingeAngle(HingeJoint hinge, float min, float max)
    {
        float angle = hinge.angle;
        float retVal = angle;

        if(max == 180f && angle < min)
        {
            float posAngle = angle + 360f;
            float posDiff = posAngle - max; //equals "angle" + 360f - 180f = "angle" + 180f
            //float posDiff = angle + 180f;
            float negDiff = angle - min;
            if (posDiff < -negDiff) 
            {
                retVal = posAngle;
                Debug.Log("Flip to positive angle: " + min + " (min), " + max + " (max), " + angle + " (angle), " + retVal + " (returned value)");
            }//if - check to see if min value is so negative that 
        }
        else if (min == -180f && angle > max)
        {
            float negAngle = angle - 360f;
            float negDiff = negAngle - min;
            float posDiff = angle - max;
            if (-negDiff < posDiff)
            {
                retVal = negAngle;
                Debug.Log("Flip to positive angle: " + min + " (min), " + max + " (max), " + angle + " (angle), " + retVal + " (returned value)");
            }   
        }

        return retVal;
    } //function - AdjustHingeAngle


    /// <summary>
    /// This version of SetHingeValue is used with position control.  A PID
    /// algorithm is used to set the hinge motor's velocity.
    /// </summary>
    /// <param name="hinge"></param>
    /// <param name="angle">Commanded angle.</param>
    /// <param name="pid"></param>
    /// <param name="filterNum"></param>
    /// <param name="filterDen"></param>
    private void SetHingeValue(
        HingeJoint hinge, float angle, PIDValues pid, 
        float[] filterNum, float[] filterDen)
    {
        
        //---------------------------
        //  COMMANDED ANGLE SMOOTHING
        //---------------------------
        #region Smoothing / Filtering

        #region Commented Out
        //if (angle > pid.m_lastReqPos + 0.5f || 
        //    angle < pid.m_lastReqPos - 0.5f)
        //{
        //    pid.m_lastReqPos = angle;
        //    //pid.m_integral = 0.0f;
        //    //pid.m_prevError = 0.0f;
        //}

        // Matt's code
        //float maxV = pid.m_maxV / Time.fixedDeltaTime;
        //if (angle > pid.m_lastReqPos + maxV)
        //    angle = pid.m_lastReqPos + maxV;
        //if (angle < pid.m_lastReqPos - maxV)
        //    angle = pid.m_lastReqPos - maxV;
        //pid.m_lastReqPos = angle;
        // end Matt's code
        #endregion //Commented Out

        // Apply filtering. / smoothing of passing in angles (if angle changes rapidly, this spreads the sharp change over 4 time steps
        float new_angle = 0f;
        new_angle += filterNum[0] * angle;
        new_angle += filterNum[1] * pid.m_pIn[0];
        new_angle += filterNum[2] * pid.m_pIn[1];
        new_angle += filterNum[3] * pid.m_pIn[2];
        new_angle += filterNum[4] * pid.m_pIn[3]; //smooth over 4 time steps
        new_angle -= filterDen[1] * pid.m_pOut[0];
        new_angle -= filterDen[2] * pid.m_pOut[1];
        new_angle -= filterDen[3] * pid.m_pOut[2];
        new_angle -= filterDen[4] * pid.m_pOut[3];
        new_angle = new_angle / filterDen[0];

        //Shift values for next time step
        pid.m_pIn[3] = pid.m_pIn[2];
        pid.m_pIn[2] = pid.m_pIn[1];
        pid.m_pIn[1] = pid.m_pIn[0];
        pid.m_pIn[0] = angle; 

        pid.m_pOut[3] = pid.m_pOut[2];
        pid.m_pOut[2] = pid.m_pOut[1];
        pid.m_pOut[1] = pid.m_pOut[0];
        pid.m_pOut[0] = new_angle;

        angle = new_angle;

        
        // deltaAngle assigned to the "short" way of moving the joint to the
        // desired orientation.
        float deltaAngle = Mathf.DeltaAngle(hinge.angle, angle);

        #endregion //Smoothing / Filtering


        //------------------------
        //  HINGE LIMITS
        //------------------------
        #region Hinge Limit Resolution
        // Cache hinge limits.
        JointLimits hLimits = hinge.limits;
        float hMin = hLimits.min;
        float hMax = hLimits.max;
        float adjustedAngle = AdjustHingeAngle(hinge, hMin, hMax);
        float newAngle = adjustedAngle + deltaAngle; //Adjusted Hinge angle ("actual) plus difference in "commanded" and "actual" hinge..circuitous with before
        //Make sure new angle doesn't exceed previously defined limits (same issue as before with AdjustHingeAngle)
        if (newAngle < hMin - 1 || newAngle > hMax + 1)
        {
            // Chosen direction will move joint outside of its range of 
            // motion.  Reverse the direction and go the "long" way.
            while (angle < -180)
                angle += 360;
            while (angle > 180)
                angle -= 360;
            deltaAngle = angle - adjustedAngle;
            //Debug.Log("Commanded Value exceeds limit as currently measured (flipped from -180 to 180 or 180 to -180): " + hMin + " (min), " + hMax + " (max), " + angle + " (angle), " + deltaAngle + " (delta)");
        }

        #region Commented Out
        // Commented out because there's probably no need to check the
        // opposite limit (when min exceeded, checked max and vice versa).
        //if (hinge.angle + deltaAngle < hinge.limits.min)
        //{
        //    while (angle < -180)
        //        angle += 180;
        //    while (angle > 180)
        //        angle -= 180;
        //    if (angle <= hinge.limits.max)
        //    {
        //        deltaAngle = angle - hinge.angle;
        //    }
        //}
        //else if (hinge.angle + deltaAngle >
        //    hinge.limits.max)
        //{
        //    while (angle < -180)
        //        angle += 180;
        //    while (angle > 180)
        //        angle -= 180;
        //    if (angle >= hinge.limits.min)
        //        deltaAngle = angle - hinge.angle;
        //}
        #endregion

        #endregion //Hinge Limit Resolution


        //------------------------
        //  INTEGRAL CALCULATION
        //------------------------
        #region Integral Calculation
        //pid.m_integral += deltaAngle * Time.fixedDeltaTime; // - 9/11/13
        //pid.m_integral = Mathf.Min(20, Mathf.Max(pid.m_integral, -20)); // - 9/11/13
        
        //Integral Values have been found to polarize during collisions and when limb becomes 
        //  instable (propagating instability) - this increasing of the reduction rate of the 
        //  integral component over time by reducing its augmentation 
        if (Math.Sign(pid.m_integral) == Math.Sign(deltaAngle))
        {
            //Signs are the same - Augment Integral Factor
            pid.m_integral += (0.5f) * deltaAngle * Time.fixedDeltaTime; //"Reduce" augmentation by factor of 0.5
            pid.m_integral = Mathf.Min(20, Mathf.Max(pid.m_integral, -20)); //Cap integral factor from becoming unstable

        }
        else
        {
            //Signs are the different - Decrement Integral Factor
            pid.m_integral += deltaAngle * Time.fixedDeltaTime;
            pid.m_integral = Mathf.Min(20, Mathf.Max(pid.m_integral, -20)); //Cap integral factor from becoming unstable

        }//if - check for same sign of current integral value and current deltaAngle (reduced augmentation of integral component)
        #endregion //Integral Calculation


        //------------------------
        //  DERIVATIVE CALCULATION
        //------------------------
        #region Derivative Calculation
        float derivative = (deltaAngle - pid.m_prevError);
        pid.m_prevError = deltaAngle; //used to calculate the change in the (commanded to actual) angle from one time step to next
        #endregion //Derivative Calculation


        //-------------------------------
        //  PID TRANSFER FUNCTION CALCULATION
        //-------------------------------
        #region PID Controller Calculation

        //Compare the previous calculated velocity and current actual velocity to inform current error calculation
        float deltaVel = pid.m_desiredTargVel - hinge.velocity; //Value currently not used in error calculation for desired feedback/change

        pid.m_desiredTargVel = f_K_NonGravityConstant *
            (pid.m_Kp * deltaAngle + pid.m_Ki * pid.m_integral +
            pid.m_Kd * derivative * Math.Max(0.05f, Math.Abs(deltaAngle / 30)) + deltaVel * pid.m_KpVel); //Currently m_KpVel values are set to 0
        #endregion //PID Controller Calculation


        //-------------------------------
        //  ASSIGN ERROR CORRECTION
        //-------------------------------
        #region Apply Error Correction
        //Assign Target Velocity to the hinge motor velocity
        JointMotor motor = hinge.motor;
        
        motor.targetVelocity =
            Mathf.Min(pid.m_maxV, Mathf.Max(pid.m_desiredTargVel, -pid.m_maxV));

        //Assign motor values back to hinge
        hinge.motor = motor;

        #endregion //Apply Error Correction


    } //function - SetHingeValue

   
    /// <summary>
    /// This version of SetHingeValue is used with velocity control.  Springs
    /// are used when the velocity is zero.
    /// </summary>
    /// <param name="hinge"></param>
    /// <param name="velocity"></param>
    private void SetHingeValue(HingeJoint hinge, float velocity)
    {
        bool motorAlreadyZero = false;

        if (Time.fixedTime == 0)
        {
            JointSpring spring = hinge.spring;
            //spring.spring = 4000000f;
            spring.spring = float.MaxValue;
            //spring.damper = 100000000.0f;
            spring.damper = 0.0f;
            hinge.spring = spring;
            hinge.useSpring = true;
            hinge.useMotor = false;
            Debug.Log(hinge.gameObject.name + " hinge spring activated.");
        }

        if (hinge.motor.targetVelocity == 0)
        {
            motorAlreadyZero = true;
        }//if - check for targetVelocity = 0

        if (hinge.motor.targetVelocity != velocity)
        {
            // This looks unnecessary, but it is due to the way the JointMotor
            // structure is defined.
            JointMotor motor = hinge.motor;
            motor.targetVelocity = velocity;
            hinge.motor = motor;
        }

        if (velocity == 0)
        {
            if (!motorAlreadyZero)
            {
                JointSpring spring = hinge.spring;
                spring.targetPosition = hinge.angle;
                hinge.spring = spring;
                hinge.useMotor = false;
                hinge.useSpring = true;
            }
        }
        else
        {
            hinge.useSpring = false;
            hinge.useMotor = true;
        }
    }//function - SetHingeValue

    #endregion //Hinge Movement and Limit Functions

    #endregion //MPL Movement - Physics


}//class - VulcanXInterface 



