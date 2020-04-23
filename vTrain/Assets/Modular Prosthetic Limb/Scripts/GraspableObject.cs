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

/// <summary>
/// Attach this script to objects that can be picked up by the vMPL. Set the 
/// public member variables in the Unity Editor to configure the object's
/// "grasp" properties.
/// </summary>
public class GraspableObject : MonoBehaviour
{
    // ***********************************************************************
    // Begin variables that should be set in the Unity Editor during the 
    // design phase. After Start() completes, these variables are ignored
    // because their values are copied to private variables.

    //---------------------------------------
    // PROPERTIES
    //---------------------------------------
    #region Properties

    //USER DEFINED and PUBLIC VARIBALES
    #region Public Variables

    //PUBLIC ACCESS / COMMUNICATION
    #region Public Access / Communication
    /// <summary>
    /// Provides a public view of the current grasp state
    /// </summary>
    public bool m_GraspInEffectR = false;
    public bool m_GraspInEffectL = false;

    //Collider Name (public, passable)
    public string str_ColliderName = ""; //will be used to determine current collider for object (TODO - compensate for multiples)

    #endregion //Public Access / Communication

    //USER DEFINED VARIABLES
    #region User Defined Variables (Grasp Logic / Requirements)
    /// <summary>
    /// Successful grasp requires either thumb or palm contact.  This setting
    /// overrides m_requiresThumb and m_requiresPalm.
    /// </summary>
    public bool m_requiresThumbOrPalm = true;

    /// <summary>
    /// Successful grasp requires the thumb.
    /// </summary>
    public bool m_requiresThumb;

    /// <summary>
    /// Successful grasp requires contact with the palm.
    /// </summary>
    public bool m_requiresPalm;

    /// <summary>
    /// Successful grasp requires contact with a finger.
    /// </summary>
    public bool m_requiresFinger = true;

    /// <summary>
    /// Successful grasp requires contact with the proximal part of the
    /// finger(s).
    /// </summary>
    public int m_requiresNProximalContacts;

    /// <summary>
    /// Successful grasp requires contact with the medial part of the
    /// finger(s).
    /// </summary>
    public int m_requiresNMedialContacts;
    

    /// <summary>
    /// Successful grasp requires this many fingers, not including the 
    /// thumb.  If proximal contact required, this count considers proximal
    /// contacts only.
    /// </summary>
    public int m_requiresNDistalContacts;

    /// <summary>
    /// Successful grasp requires that collisions occur in directions on the palmer
    /// side of the fingers and thumbs (if set to true) - if not, then collisions 
    /// in any direction are adequete for grasp confirmation
    /// 
    /// Applies to distal, proximal, and thumb elements (palmer will maintain 
    /// directionality toward the palmer side)
    /// </summary>
    public bool m_requiresContactDirectionality = true;

    // End variables set by user during desing phase.
    // ***********************************************************************
    #endregion //User Defined Variables (Grasp Logic / Requirements)

    #endregion //Public Variables


    //HAND Properties
    #region HAND Properties (Element Names)
    // Names of finger objects.  These come from GameObject.name as opposed to
    // WorldObject.m_id.

    #region Right Hand Element Names
    //Thumb Contacts
    private const string ms_rTHUMB_DISTAL = "rThDistColl";
    private const string ms_rTHUMB_PROXIMAL1 = "rThProx1Coll";
    private const string ms_rTHUMB_PROXIMAL2 = "rThProx2Coll";

    //Distal Contacts
    private const string ms_rIND_DISTAL = "rIndDistal";
    private const string ms_rMID_DISTAL = "rMidDistal";
    private const string ms_rRING_DISTAL = "rRingDistal";
    private const string ms_rLITTLE_DISTAL = "rLittleDistal";

    //Proximal Contacts
    private const string ms_rIND_PROXIMAL = "rIndProximal";
    private const string ms_rMID_PROXIMAL = "rMidProximal";
    private const string ms_rRING_PROXIMAL = "rRingProximal";
    private const string ms_rLITTLE_PROXIMAL = "rLittleProximal";

    //Medial Contacts
    private const string ms_rIND_MEDIAL = "rIndMedial";
    private const string ms_rMID_MEDIAL = "rMidMedial";
    private const string ms_rRING_MEDIAL = "rRingMedial";
    private const string ms_rLITTLE_MEDIAL = "rLittleMedial";

    //Palm
    private const string ms_rPALM = "rPalm";
    
    #endregion //Right Hand Element Names

    #region Left Hand Element Names
    //Thumb Contacts
    private const string ms_lTHUMB_DISTAL = "lThDistColl";
    private const string ms_lTHUMB_PROXIMAL1 = "lThProx1Coll";
    private const string ms_lTHUMB_PROXIMAL2 = "lThProx2Coll";

    //Distal Contacts
    private const string ms_lIND_DISTAL = "lIndDistal";
    private const string ms_lMID_DISTAL = "lMidDistal";
    private const string ms_lRING_DISTAL = "lRingDistal";
    private const string ms_lLITTLE_DISTAL = "lLittleDistal";

    //Proximal Contacts
    private const string ms_lIND_PROXIMAL = "lIndProximal";
    private const string ms_lMID_PROXIMAL = "lMidProximal";
    private const string ms_lRING_PROXIMAL = "lRingProximal";
    private const string ms_lLITTLE_PROXIMAL = "lLittleProximal";

    //Medial Contacts
    private const string ms_lIND_MEDIAL = "lIndMedial";
    private const string ms_lMID_MEDIAL = "lMidMedial";
    private const string ms_lRING_MEDIAL = "lRingMedial";
    private const string ms_lLITTLE_MEDIAL = "lLittleMedial";

    //Palm
    private const string ms_lPALM = "lPalm";
    
    #endregion //Left Hand Element Names
    
    #endregion //HAND Properties

    #endregion //Properties


    //---------------------------------------
    // CLASSES
    //---------------------------------------
    #region Class - ContactState
    /// <summary>
    /// This class manages contact state and decides if the grasp parameters 
    /// for the object have been met.  If parameters are met, the object will
    /// be attached to the palm.
    /// </summary>
    private class ContactState
    {

        // VARIABLES - useable for local functions and pointing
        #region Variables - Collision and Attachment
        private Collider m_collider;
        private FixedJoint m_fixedJoint;
        private FixedJoint m_preExistingFixedJoint;
        private Rigidbody m_rPalmRigidBody;
        private Rigidbody m_lPalmRigidBody;
        #endregion //Variables - Collision and Attachment


        //USER DEFINED CONTACT FLAGS and REQUIREMENTS 
        #region User Defined Requirement Flags (and get functions)

        //FLAGS (GET-SET)
        #region Contact Flags
        // Flag for user-defined requirement for Thumb/Palm
        private bool m_requiresThumbOrPalm;
        public bool RequiresThumbOrPalm
        {
            get { return m_requiresThumbOrPalm; }
        }

        // Flag for user-defined requirement for Thumb
        private bool m_requiresThumb;
        public bool RequiresThumb
        {
            get { return m_requiresThumb; }
        }

        // Flag for user-defined requirement for Palm
        private bool m_requiresPalm;
        public bool RequiresPalm
        {
            get { return m_requiresPalm; }
        }

        // Flag for user-defined requirement for Finger Contacts
        private bool m_requiresFinger;
        public bool RequiresFinger
        {
            get { return m_requiresFinger; }
        }
        
        // Flag for user-defined requirement for N Proximal Contacts
        private int m_requiresNProximalContacts;
        public int RequiresNProximalContacts
        {
            get { return m_requiresNProximalContacts; }
        }

        // Flag for user-defined requirement for N Medial Contacts
        private int m_requiresNMedialContacts;
        public int RequiresNMedialContacts
        {
            get { return m_requiresNMedialContacts; }
        }

        // Flag for user-defined requirement for N Distal Contacts
        private int m_requiresNDistalContacts;
        public int RequiresNDistalContacts
        {
            get { return m_requiresNDistalContacts; }
        }

        // Flag for user-defined requirement for Directionality
        private bool m_requiresContactDirectionality;
        public bool RequiresContactDirectionality
        {
            get { return m_requiresContactDirectionality; }
        }
        #endregion //Contact Flags


        //CONTACT ASSESSMENTS
        #region L/R Contact Assessments (Thumb, Palm, Fingers) - GET/SET Functions

        //THUMB CONTACTS
        #region Thumb Contacts

        #region Right Thumb Contacts
        //THUMB DISTAL
        private bool m_rHaveThumbDistalContact = false;
        public bool HaveRightThumbDistalContact
        {
            get { return m_rHaveThumbDistalContact; }
            set
            {
                if (m_rHaveThumbDistalContact != value)
                {
                    m_rHaveThumbDistalContact = value;
                    RightCheckGraspRequirements(); //After Thumb Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveRightThumbDistalContact

        //THUMB PROXIMAL1
        private bool m_rHaveThumbProximal1Contact = false;
        public bool HaveRightThumbProximal1Contact
        {
            get { return m_rHaveThumbProximal1Contact; }
            set
            {
                if (m_rHaveThumbProximal1Contact != value)
                {
                    m_rHaveThumbProximal1Contact = value;
                    RightCheckGraspRequirements(); //After Thumb Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveRightThumbProximal1Contact

        //THUMB PROXIMAL2
        private bool m_rHaveThumbProximal2Contact = false;
        public bool HaveRightThumbProximal2Contact
        {
            get { return m_rHaveThumbProximal2Contact; }
            set
            {
                if (m_rHaveThumbProximal2Contact != value)
                {
                    m_rHaveThumbProximal2Contact = value;
                    RightCheckGraspRequirements(); //After Thumb Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveRightThumbProximal2Contact
        #endregion //Right Thumb Contacts

        #region Left Thumb Contacts
        //THUMB DISTAL
        private bool m_lHaveThumbDistalContact = false;
        public bool HaveLeftThumbDistalContact
        {
            get { return m_lHaveThumbDistalContact; }
            set
            {
                if (m_lHaveThumbDistalContact != value)
                {
                    m_lHaveThumbDistalContact = value;
                    LeftCheckGraspRequirements(); //After Thumb Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveLeftThumbDistalContact

        //THUMB PROXIMAL1
        private bool m_lHaveThumbProximal1Contact = false;
        public bool HaveLeftThumbProximal1Contact
        {
            get { return m_lHaveThumbProximal1Contact; }
            set
            {
                if (m_lHaveThumbProximal1Contact != value)
                {
                    m_lHaveThumbProximal1Contact = value;
                    LeftCheckGraspRequirements(); //After Thumb Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveLeftThumbProximal1Contact

        //THUMB PROXIMAL2
        private bool m_lHaveThumbProximal2Contact = false;
        public bool HaveLeftThumbProximal2Contact
        {
            get { return m_lHaveThumbProximal2Contact; }
            set
            {
                if (m_lHaveThumbProximal2Contact != value)
                {
                    m_lHaveThumbProximal2Contact = value;
                    LeftCheckGraspRequirements(); //After Thumb Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveLeftThumbProximal2Contact
        #endregion //Left Thumb Contacts

        #endregion //Thumb Contacts


        //PALM CONTACTS
        #region Palm Contacts
        private bool m_rHavePalmContact;
        public bool HaveRightPalmContact
        {
            get { return m_rHavePalmContact; }
            set
            {
                if (m_rHavePalmContact != value)
                {
                    m_rHavePalmContact = value;
                    RightCheckGraspRequirements(); //After Palm Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveRightPalmContact

        private bool m_lHavePalmContact;
        public bool HaveLeftPalmContact
        {
            get { return m_lHavePalmContact; }
            set
            {
                if (m_lHavePalmContact != value)
                {
                    m_lHavePalmContact = value;
                    LeftCheckGraspRequirements();  //After Palm Contact Flag adjusted - check for grasp (for instant update)
                }
            }
        }//Get-Set - HaveLeftPalmContact
        #endregion //Palm Contacts


        //FINGER CONTACTS
        #region Finger Contacts

        //Define the constants for each finger
        public const int INDEX_IND = 0;
        public const int MIDDLE_IND = 1;
        public const int RING_IND = 2;
        public const int LITTLE_IND = 3;

        //PROXIMAL CONTACTS
        #region Right Proximal Contacts (Index, Middle, Ring, Little)

        private bool[] m_rFingerProximalContacts;

        public bool RightIndexProximalContact
        {
            get { return m_rFingerProximalContacts[INDEX_IND]; }
            set
            {
                if (value != m_rFingerProximalContacts[INDEX_IND])
                {
                    m_rFingerProximalContacts[INDEX_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightMiddleProximalContact
        {
            get { return m_rFingerProximalContacts[MIDDLE_IND]; }
            set
            {
                if (value != m_rFingerProximalContacts[MIDDLE_IND])
                {
                    m_rFingerProximalContacts[MIDDLE_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightRingProximalContact
        {
            get { return m_rFingerProximalContacts[RING_IND]; }
            set
            {
                if (value != m_rFingerProximalContacts[RING_IND])
                {
                    m_rFingerProximalContacts[RING_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightLittleProximalContact
        {
            get { return m_rFingerProximalContacts[LITTLE_IND]; }
            set
            {
                if (value != m_rFingerProximalContacts[LITTLE_IND])
                {
                    m_rFingerProximalContacts[LITTLE_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        #endregion // Right Distal Contacts.

        #region Left Proximal Contacts  (Index, Middle, Ring, Little)

        private bool[] m_lFingerProximalContacts;

        public bool LeftIndexProximalContact
        {
            get { return m_lFingerProximalContacts[INDEX_IND]; }
            set
            {
                if (value != m_lFingerProximalContacts[INDEX_IND])
                {
                    m_lFingerProximalContacts[INDEX_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftMiddleProximalContact
        {
            get { return m_lFingerProximalContacts[MIDDLE_IND]; }
            set
            {
                if (value != m_lFingerProximalContacts[MIDDLE_IND])
                {
                    m_lFingerProximalContacts[MIDDLE_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftRingProximalContact
        {
            get { return m_lFingerProximalContacts[RING_IND]; }
            set
            {
                if (value != m_lFingerProximalContacts[RING_IND])
                {
                    m_lFingerProximalContacts[RING_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftLittleProximalContact
        {
            get { return m_lFingerProximalContacts[LITTLE_IND]; }
            set
            {
                if (value != m_lFingerProximalContacts[LITTLE_IND])
                {
                    m_lFingerProximalContacts[LITTLE_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        #endregion // Left Proximal Contacts.

        //DISTAL CONTACTS
        #region Right Distal Contacts (Index, Middle, Ring, Little)

        private bool[] m_rFingerDistalContacts;

        public bool RightIndexDistalContact
        {
            get { return m_rFingerDistalContacts[INDEX_IND]; }
            set
            {
                if (m_rFingerDistalContacts[INDEX_IND] != value)
                {
                    m_rFingerDistalContacts[INDEX_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightMiddleDistalContact
        {
            get { return m_rFingerDistalContacts[MIDDLE_IND]; }
            set
            {
                if (m_rFingerDistalContacts[MIDDLE_IND] != value)
                {
                    m_rFingerDistalContacts[MIDDLE_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightRingDistalContact
        {
            get { return m_rFingerDistalContacts[RING_IND]; }
            set
            {
                if (m_rFingerDistalContacts[RING_IND] != value)
                {
                    m_rFingerDistalContacts[RING_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightLittleDistalContact
        {
            get { return m_rFingerDistalContacts[LITTLE_IND]; }
            set
            {
                if (m_rFingerDistalContacts[LITTLE_IND] != value)
                {
                    m_rFingerDistalContacts[LITTLE_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        #endregion // Right Distal Contacts.

        #region Left Distal Contacts (Index, Middle, Ring, Little)

        private bool[] m_lFingerDistalContacts;

        public bool LeftIndexDistalContact
        {
            get { return m_lFingerDistalContacts[INDEX_IND]; }
            set
            {
                if (m_lFingerDistalContacts[INDEX_IND] != value)
                {
                    m_lFingerDistalContacts[INDEX_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftMiddleDistalContact
        {
            get { return m_lFingerDistalContacts[MIDDLE_IND]; }
            set
            {
                if (m_lFingerDistalContacts[MIDDLE_IND] != value)
                {
                    m_lFingerDistalContacts[MIDDLE_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftRingDistalContact
        {
            get { return m_lFingerDistalContacts[RING_IND]; }
            set
            {
                if (m_lFingerDistalContacts[RING_IND] != value)
                {
                    m_lFingerDistalContacts[RING_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftLittleDistalContact
        {
            get { return m_lFingerDistalContacts[LITTLE_IND]; }
            set
            {
                if (m_lFingerDistalContacts[LITTLE_IND] != value)
                {
                    m_lFingerDistalContacts[LITTLE_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        #endregion // Left Distal Contacts.

        //MEDIAL CONTACTS
        #region Right Medial Contacts (Index, Middle, Ring, Little)

        private bool[] m_rFingerMedialContacts;

        public bool RightIndexMedialContact
        {
            get { return m_rFingerMedialContacts[INDEX_IND]; }
            set
            {
                if (m_rFingerMedialContacts[INDEX_IND] != value)
                {
                    m_rFingerMedialContacts[INDEX_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightMiddleMedialContact
        {
            get { return m_rFingerMedialContacts[MIDDLE_IND]; }
            set
            {
                if (m_rFingerMedialContacts[MIDDLE_IND] != value)
                {
                    m_rFingerMedialContacts[MIDDLE_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightRingMedialContact
        {
            get { return m_rFingerMedialContacts[RING_IND]; }
            set
            {
                if (m_rFingerMedialContacts[RING_IND] != value)
                {
                    m_rFingerMedialContacts[RING_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        public bool RightLittleMedialContact
        {
            get { return m_rFingerMedialContacts[LITTLE_IND]; }
            set
            {
                if (m_rFingerMedialContacts[LITTLE_IND] != value)
                {
                    m_rFingerMedialContacts[LITTLE_IND] = value;
                    RightCheckGraspRequirements();
                }
            }
        }

        #endregion // Right Medial Contacts.

        #region Left Medial Contacts (Index, Middle, Ring, Little)

        private bool[] m_lFingerMedialContacts;

        public bool LeftIndexMedialContact
        {
            get { return m_lFingerMedialContacts[INDEX_IND]; }
            set
            {
                if (m_lFingerMedialContacts[INDEX_IND] != value)
                {
                    m_lFingerMedialContacts[INDEX_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftMiddleMedialContact
        {
            get { return m_lFingerMedialContacts[MIDDLE_IND]; }
            set
            {
                if (m_lFingerMedialContacts[MIDDLE_IND] != value)
                {
                    m_lFingerMedialContacts[MIDDLE_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftRingMedialContact
        {
            get { return m_lFingerMedialContacts[RING_IND]; }
            set
            {
                if (m_lFingerMedialContacts[RING_IND] != value)
                {
                    m_lFingerMedialContacts[RING_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        public bool LeftLittleMedialContact
        {
            get { return m_lFingerMedialContacts[LITTLE_IND]; }
            set
            {
                if (m_lFingerMedialContacts[LITTLE_IND] != value)
                {
                    m_lFingerMedialContacts[LITTLE_IND] = value;
                    LeftCheckGraspRequirements();
                }
            }
        }

        #endregion // Left Medial Contacts.
        #endregion //Finger Contacts

        #endregion //Contact Assessments


        //USER DEFINED GRASP REQUIREMENTS (GET-SET)
        #region User-Defined Grasp Requirements (#contacts req, etc.)

        //Right Hand
        #region Right Hand Grasp Requirements
        //Number of Proximal Contacts Required
        private int m_rNumProximalContacts;
        public int RightNumProximalContacts
        {
            get { return m_rNumProximalContacts; }
        }

        //Number of Medial Contacts Required
        private int m_rNumMedialContacts;
        public int RightNumMedialContacts
        {
            get { return m_rNumMedialContacts; }
        }

        //Number of Distal Contacts Required
        private int m_rNumDistalContacts;
        public int RightNumDistalContacts
        {
            get { return m_rNumDistalContacts; }
        }
        #endregion //Right Hand Grasp Requirements

        //Left Hand
        #region Left Hand Grasp Requirements
        //Number of Proximal Contacts Required
        private int m_lNumProximalContacts;
        public int LeftNumProximalContacts
        {
            get { return m_lNumProximalContacts; }
        }

        //Number of Medial Contacts Required
        private int m_lNumMedialContacts;
        public int LeftNumMedialContacts
        {
            get { return m_lNumMedialContacts; }
        }

        //Number of Distal Contacts Required
        private int m_lNumDistalContacts;
        public int LeftNumDistalContacts
        {
            get { return m_lNumDistalContacts; }
        }
        #endregion //Left Hand Grasp Requirements

        //Specific Combination of Fingers and Thumbs (ARAT Test - pinch sub-tests where use thumb-3rd-finger, etc.)
        //TODOTODO

        #endregion //User-Defined Grasp Requirements

        #endregion // Requirements.


        //FUNCTIONS
        #region Functions

        //CONSTRUCTOR
        #region Constructor
        public ContactState(Collider collider, FixedJoint preExistingFixedJoint,
            bool requiresThumbOrPalm, bool requiresThumb, bool requiresPalm, bool requiresFinger, bool requiresContactDirectionality,
            int requiresNProximalContacts, int requiresNMedialContacts, int requiresNDistalContacts)
        {
            m_collider = collider;
            m_preExistingFixedJoint = preExistingFixedJoint;

            //Palm Assignment (Right/Left Determination)
            #region PALM (R/L) definition
            GameObject palm = GameObject.Find(ms_rPALM);
            if (palm == null)
            {
                Debug.LogWarning(ms_rPALM + 
                    " not found.  Objects cannot be picked up without it.");
            }
            else if (palm.GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning(ms_rPALM +
                    " does not have a rigid body.  Objects cannot be picked up without it.");
            }
            else
                m_rPalmRigidBody = palm.GetComponent<Rigidbody>();

            GameObject lPalm = GameObject.Find(ms_lPALM);

            if (lPalm == null)
            {
//                Debug.LogWarning(ms_lPALM + " not found.  Objects cannot be picked up without it.");
            }
            else if (lPalm.GetComponent<Rigidbody>() == null)
            {
                Debug.LogWarning(ms_lPALM +
                    " does not have a rigid body.  Objects cannot be picked up without it.");
            }
            else
                m_lPalmRigidBody = lPalm.GetComponent<Rigidbody>();
            #endregion //PALM (R/L) definition

            //Grasp Requirements
            #region Grasp Requirements
            m_requiresThumbOrPalm = requiresThumbOrPalm;
            m_requiresThumb = requiresThumb;
            m_requiresPalm = requiresPalm;
            m_requiresFinger = requiresFinger;
            m_requiresNProximalContacts = requiresNProximalContacts;
            m_requiresNDistalContacts = requiresNDistalContacts;
            m_requiresNMedialContacts = requiresNMedialContacts;
            m_requiresContactDirectionality = requiresContactDirectionality;
            #endregion //Grasp Requirements

            //Contact State Arrays
            #region Contact State Arrays
            //Instantiate with no collisions/contacts
            m_rFingerDistalContacts = new bool[4] { false, false, false, false};
            m_rFingerProximalContacts = new bool[4] { false, false, false, false };
            m_rFingerMedialContacts = new bool[4] { false, false, false, false };
            
            m_lFingerDistalContacts = new bool[4] { false, false, false, false };
            m_lFingerProximalContacts = new bool[4] { false, false, false, false };
            m_lFingerMedialContacts = new bool[4] { false, false, false, false };
            #endregion //Contact State Arrays

            //Debug.Log("Created Contact State");
        }//function - ContactState (Constructor)
        #endregion //Constructor


        //DETERMINE GRASP
        #region Grasp Check
        
        //RIGHT - Check the flags to determine the grasp type and allowance
        private void RightCheckGraspRequirements()
        {
            // Set Flag to true, and then check conditionals to see if grasp is in place
            bool grasped = true;
            int numContacts = 0;

//TODO - Add Thumb, no Palm
//TODO - Add Thumb-Finger Combo-Only

            //COLLECT CONTACT DATA
            #region CONTACT DATA

            //THUMB and PALM CONTACTS
            #region Requires Thumb Or Palm
            if (!m_requiresThumbOrPalm)
            {
                // Will check to see if thumb or palm is in contact, if not, then will set flag to false

                //if (m_requiresThumb && !m_rHaveThumbContact)
                if (m_requiresThumb && !m_rHaveThumbDistalContact && !m_rHaveThumbProximal1Contact && !m_rHaveThumbProximal2Contact)
                {
                    grasped = false;
                }//if - check for thumb contact

                // The UNITY_EDITOR dependent compile flag doesn't shortcut the if
                // statements so the debug variables are always updated.

#if UNITY_EDITOR
                if (m_requiresPalm && !m_rHavePalmContact)
#else
                if (grasped && m_requiresPalm && !m_rHavePalmContact)
#endif
                {
                    grasped = false;
                }
            } // end if (!m_requiresThumbOrPalm)
                //else if(!m_rHavePalmContact && !m_rHaveThumbContact)
            //Insinuates that m_requiresThumbOfPalm is set to TRUE moving forward
            else if (!m_rHavePalmContact && !m_rHaveThumbDistalContact && !m_rHaveThumbProximal1Contact && !m_rHaveThumbProximal2Contact)
            {
                // Thumb and palm are not in contact, set flag to false
                grasped = false;
            }//if - check to requires Thumb or Palm
            #endregion //Requires Thumb Or Palm


            //PROXIMAL CONTACTS
            #region Proximal Contact Check
            
            //PROXIMAL CONTACT - TALLY
            #region Proximal Contact Tally
            numContacts = 0; //reset number of contacts
            
            // Will check to see if any of the proximal contacts are collided
            foreach (bool contact in m_rFingerProximalContacts)
            {
                if (contact)
                {
                    numContacts++; //Increments the # of collisions/in-contact
                }//if - check for contact
            }//foreach - #Finger Contacts

            m_rNumProximalContacts = numContacts; //Sets the number of proximal collisions
            #endregion //Proximal Contact Tally

            //PROXIMAL CONTACT - CHECK REQUIREMENTS
            #region Requires N Proximal Contacts
#if UNITY_EDITOR
            if (m_requiresNProximalContacts > 0)
#else
            if (grasped && m_requiresNProximalContacts > 0)
#endif
            {
                //Checks to see if current # of proximal collisions matchs the requisite # for grasp
                if (m_rNumProximalContacts < m_requiresNProximalContacts)
                {
                    grasped = false;
                }//if - check if met requirements
            }//if - check for proximal contacts
            #endregion //Requires N Proximal Contacts

            #endregion //Proximal Contact Check


            //MEDIAL CONTACTS
            #region Medial Contact Check

            //MEDIAL CONTACT - TALLY
            #region Medial Contact Tally
            numContacts = 0; //reset number of contacts

            // Will check to see if any of the contacts are collided
            foreach (bool contact in m_rFingerMedialContacts)
            {
                if (contact)
                {
                    numContacts++; //Increments the # of collisions/in-contact
                }//if - check for contact
            }//foreach - #Finger Contacts

            m_rNumMedialContacts = numContacts; //Sets the number of proximal collisions
            #endregion //Medial Contact Tally

            //MEDIAL CONTACT - CHECK REQUIREMENTS
            #region Requires N Medial Contacts
#if UNITY_EDITOR
            if (m_requiresNMedialContacts > 0)
#else
            if (grasped && m_requiresNMedialContacts > 0)
#endif
            {
                //Checks to see if current # of medial collisions matchs the requisite # for grasp
                if (m_rNumMedialContacts < m_requiresNMedialContacts)
                {
                    grasped = false;
                }//if - check if met requirements
            }//if - check for contacts
            #endregion //Requires N Medial Contacts

            #endregion //Medial Contact Check


            //DISTAL CONTACTS
            #region Distal Contact Check

            //DISTAL CONTACT - TALLY
            #region Distal Contact Tally
            numContacts = 0; //reset number of contacts

            // Will check to see if any of the distal contacts are collided
            foreach (bool contact in m_rFingerDistalContacts)
            {
                if (contact)
                {
                    numContacts++; //Increments the # of collisions/in-contact
                }//if - check for contact
            }//foreach - #Finger Distal Contacts

            m_rNumDistalContacts = numContacts; //Sets the number of collisions
            #endregion //Distal Contact Tally

            //DISTAL CONTACT - CHECK REQUIREMENTS
            #region Requires N Distal Contacts
#if UNITY_EDITOR
            if (m_requiresNDistalContacts > 0)
#else
            if (grasped && m_requiresNDistalContacts > 0)
#endif
            {
                //Checks to see if current # of  collisions matchs the requisite # for grasp
                if (m_rNumDistalContacts < m_requiresNDistalContacts)
                {
                    grasped = false;
                }//if - check if met requirements
            }//if - check if Distal Contacts required
            #endregion //Requires N Distal Contacts

            #endregion //Distal Contact Check
            
            #endregion //CONTACT DATA

//TODOTODO - Check for at least X Distal/Proximal/Etc. Contacts - in conjunction with thumb/palm - to qualify the grasp (if 0, then no grasp)

            //CHECK FOR CURRENT GRASP CONDITION
            #region GRASP CONDITION
            
            // Check requires finger condition.
            if (grasped && m_requiresFinger && m_rNumDistalContacts < 1 &&
                m_rNumMedialContacts < 1 && m_rNumProximalContacts < 1)
            {
                grasped = false;
            }

            #endregion //GRASP CONDITION

            
            //CHECK FOR PREVIOUS GRASP CONDITION
            #region Check Previous Grasps
            if (grasped && m_fixedJoint == null)
            {
                // CHeck for preexisting grasped objects
                if (m_preExistingFixedJoint)
                {
//TODO - BIMANUAL CONSIDERATION HERE - if both left and right hand are grasping
                    Object.Destroy(m_preExistingFixedJoint);

                    m_preExistingFixedJoint = null;
                }

                grasped = true; //TODO - Is this redundant? - 9/12/12

                //GRASP - ATTACH OBJECT TO HAND
                m_fixedJoint = 
                    m_collider.gameObject.AddComponent<FixedJoint>();
                m_fixedJoint.connectedBody = m_rPalmRigidBody;

                //Turn off gravity for object 
                m_collider.gameObject.GetComponent<Rigidbody>().useGravity = false;

//                WorldInterface.Instance().QueueGraspState(WorldInterface.GraspState.ESTABLISHED);
            }
            else if (!grasped && m_fixedJoint != null)
            {
                //Had grasp and lost it, need to remove grasp connection and object instances
                grasped = false; //TODO - Is this redundant? - 9/12/12

                m_fixedJoint.gameObject.GetComponent<Rigidbody>().useGravity = true;
                Object.Destroy(m_fixedJoint);

//                WorldInterface.Instance().QueueGraspState(WorldInterface.GraspState.RELEASED);
            }//Check for preexisting graps, over-write (grasp=true) or eliminate (grasp=false)
            #endregion //Check Previous Grasps

            //Evaluated Grasp State for Object
            b_graspState_R = grasped; //FLAG for external polling
            
        }//function - RightCheckGraspRequirements


        //LEFT - Check the flags to determine the grasp type and allowance
        private void LeftCheckGraspRequirements()
        {
            // Set Flag to true, and then check conditionals to see if grasp is in place
            bool grasped = true;
            int numContacts = 0;

//TODO - Add Thumb, no Palm
//TODO - Add Thumb-Finger Combo-Only

            //COLLECT CONTACT DATA
            #region CONTACT DATA

            //THUMB and PALM CONTACTS
            #region Requires Thumb Or Palm
            if (!m_requiresThumbOrPalm)
            {
                // Will check to see if thumb or palm is in contact, if not, then will set flag to false

                if (m_requiresThumb && !m_lHaveThumbDistalContact && !m_lHaveThumbProximal1Contact && !m_lHaveThumbProximal2Contact)
                {
                    grasped = false;
                }//if - check for thumb contact

                // The UNITY_EDITOR dependent compile flag doesn't shortcut the if
                // statements so the debug variables are always updated.
#if UNITY_EDITOR
                if (m_requiresPalm && !m_lHavePalmContact)
#else
                if (grasped && m_requiresPalm && !m_lHavePalmContact)
#endif
                {
                    grasped = false;
                }
            } // end if (!m_requiresThumbOrPalm)
            //Insinuates that m_requiresThumbOfPalm is set to TRUE moving forward
            else if (!m_lHavePalmContact && !m_lHaveThumbDistalContact && !m_lHaveThumbProximal1Contact && !m_lHaveThumbProximal2Contact)
            {
                // Thumb and palm are not in contact, set flag to false
                grasped = false;
            }//if - check to requires Thumb or Palm
            #endregion //Requires Thumb Or Palm


            //PROXIMAL CONTACTS
            #region Proximal Contact Check

            //PROXIMAL CONTACT - TALLY
            #region Proximal Contact Tally
            numContacts = 0; //reset number of contacts

            // Will check to see if any of the proximal contacts are collided
            foreach (bool contact in m_lFingerProximalContacts)
            {
                if (contact)
                {
                    numContacts++; //Increments the # of collisions/in-contact
                }//if - check for contact
            }//foreach - #Finger Contacts

            m_lNumProximalContacts = numContacts; //Sets the number of proximal collisions
            #endregion //Proximal Contact Tally

            //PROXIMAL CONTACT - CHECK REQUIREMENTS
            #region Requires N Proximal Contacts
#if UNITY_EDITOR
            if (m_requiresNProximalContacts > 0)
#else
            if (grasped && m_requiresNProximalContacts > 0)
#endif
            {
                //Checks to see if current # of proximal collisions matchs the requisite # for grasp
                if (m_lNumProximalContacts < m_requiresNProximalContacts)
                {
                    grasped = false;
                }//if - check if met requirements
            }//if - check for proximal contacts
            #endregion //Requires N Proximal Contacts

            #endregion //Proximal Contact Check


            //MEDIAL CONTACTS
            #region Medial Contact Check

            //MEDIAL CONTACT - TALLY
            #region Medial Contact Tally
            numContacts = 0; //reset number of contacts

            // Will check to see if any of the contacts are collided
            foreach (bool contact in m_lFingerMedialContacts)
            {
                if (contact)
                {
                    numContacts++; //Increments the # of collisions/in-contact
                }//if - check for contact
            }//foreach - #Finger Contacts

            m_lNumMedialContacts = numContacts; //Sets the number of proximal collisions
            #endregion //Medial Contact Tally

            //MEDIAL CONTACT - CHECK REQUIREMENTS
            #region Requires N Medial Contacts
#if UNITY_EDITOR
            if (m_requiresNMedialContacts > 0)
#else
            if (grasped && m_requiresNMedialContacts > 0)
#endif
            {
                //Checks to see if current # of medial collisions matchs the requisite # for grasp
                if (m_lNumMedialContacts < m_requiresNMedialContacts)
                {
                    grasped = false;
                }//if - check if met requirements
            }//if - check for contacts
            #endregion //Requires N Medial Contacts

            #endregion //Medial Contact Check


            //DISTAL CONTACTS
            #region Distal Contact Check

            //DISTAL CONTACT - TALLY
            #region Distal Contact Tally
            numContacts = 0; //reset number of contacts

            // Will check to see if any of the distal contacts are collided
            foreach (bool contact in m_lFingerDistalContacts)
            {
                if (contact)
                {
                    numContacts++; //Increments the # of collisions/in-contact
                }//if - check for contact
            }//foreach - #Finger Distal Contacts

            m_lNumDistalContacts = numContacts; //Sets the number of collisions
            #endregion //Distal Contact Tally

            //DISTAL CONTACT - CHECK REQUIREMENTS
            #region Requires N Distal Contacts
#if UNITY_EDITOR
            if (m_requiresNDistalContacts > 0)
#else
            if (grasped && m_requiresNDistalContacts > 0)
#endif
            {
                //Checks to see if current # of  collisions matchs the requisite # for grasp
                if (m_lNumDistalContacts < m_requiresNDistalContacts)
                {
                    grasped = false;
                }//if - check if met requirements
            }//if - check if Distal Contacts required
            #endregion //Requires N Distal Contacts

            #endregion //Distal Contact Check

            #endregion //CONTACT DATA

//TODOTODO - Check for at least X Distal/Proximal/Etc. Contacts - in conjunction with thumb/palm - to qualify the grasp (if 0, then no grasp)

            //CHECK FOR CURRENT GRASP CONDITION
            #region GRASP CONDITION
            
            // Check requires finger condition.
            if (grasped && m_requiresFinger && m_lNumDistalContacts < 1 &&
                m_lNumMedialContacts < 1 && m_lNumProximalContacts < 1)
            {
                grasped = false;
            }

            #endregion //GRASP CONDITION


            //CHECK FOR PREVIOUS GRASP CONDITION
            #region Check Previous Grasps
            if (grasped && m_fixedJoint == null)
            {
                // CHeck for preexisting grasped objects
                if (m_preExistingFixedJoint)
                {
                    Object.Destroy(m_preExistingFixedJoint);
                    m_preExistingFixedJoint = null;
                }

                grasped = true; //TODO - Is this redundant? - 9/12/12

                //GRASP - ATTACH OBJECT TO HAND
                m_fixedJoint =
                    m_collider.gameObject.AddComponent<FixedJoint>();
                m_fixedJoint.connectedBody = m_lPalmRigidBody; //LEFT

                //Turn off gravity for object 
                m_collider.gameObject.GetComponent<Rigidbody>().useGravity = false;

//                WorldInterface.Instance().QueueGraspState(WorldInterface.GraspState.ESTABLISHED);
            }
            else if (!grasped && m_fixedJoint != null)
            {
                //Had grasp and lost it, need to remove grasp connection and object instances
                grasped = false; //TODO - Is this redundant? - 9/12/12

                m_fixedJoint.gameObject.GetComponent<Rigidbody>().useGravity = true;
                Object.Destroy(m_fixedJoint);

//                WorldInterface.Instance().QueueGraspState(WorldInterface.GraspState.RELEASED);
            }//Check for preexisting graps, over-write (grasp=true) or eliminate (grasp=false)
            #endregion //Check Previous Grasps


            #region OLD Left Grasp Check
            //if (grasped && m_fixedJoint == null)
            //{
            //    if (m_preExistingFixedJoint)
            //    {
            //        Object.Destroy(m_preExistingFixedJoint);
            //        m_preExistingFixedJoint = null;
            //    }

            //    grasped = true;

            //    //GRASP - ATTACH OBJECT TO HAND
            //    m_fixedJoint = 
            //        m_collider.gameObject.AddComponent<FixedJoint>();
            //    m_fixedJoint.connectedBody = m_lPalmRigidBody;
            //    m_collider.gameObject.rigidbody.useGravity = false;

            //    WorldInterface.Instance().QueueGraspState(
            //        WorldInterface.GraspState.ESTABLISHED);
            //}
            //else if (!grasped && m_fixedJoint != null)
            //{
            //    grasped = false;
            //    m_fixedJoint.gameObject.rigidbody.useGravity = true;
            //    Object.Destroy(m_fixedJoint);
            //    WorldInterface.Instance().QueueGraspState(
            //        WorldInterface.GraspState.RELEASED);
            //}

            #endregion //OLD Left Grasp Check

            //Evaluated Grasp State for Object
            b_graspState_L = grasped; //FLAG for external polling

        }//function - LeftCheckGraspRequirements
        #endregion //Grasp Check


        //PUBLIC ACCESS - Variables
        #region Public Access (Grasp States)
        private bool b_graspState_R = false; //allows public access to determine if grasp accomplished on this object
        private bool b_graspState_L = false; //allows public access to determine if grasp accomplished on this object

        //PUBLIC GET Function for R/L Grasp
        public bool getGraspState(string strWhich)
        {
            if (strWhich == "R")
            {
                return b_graspState_R;
            }
            else if (strWhich == "L")
            {
                return b_graspState_L;
            }
            else
            {
                return false;
            }//if - check for which handidness

        }//function - getGraspState


        /// <summary>
        /// This function will set the current state of grasp logic.
        /// </summary>
        /// <param name="requiresThumbOrPalm"></param>
        /// <param name="requiresThumb"></param>
        /// <param name="requiresPalm"></param>
        /// <param name="requiresFinger"></param>
        /// <param name="requiresNProximalContacts"></param>
        /// <param name="requiresNMedialContacts"></param>
        /// <param name="requiresNDistalContacts"></param>
        /// <param name="requiresContactDirectionality"></param>
        public void setGraspLogic(bool requiresThumbOrPalm, bool requiresThumb, bool requiresPalm, bool requiresFinger, int requiresNProximalContacts, int requiresNMedialContacts, int requiresNDistalContacts, bool requiresContactDirectionality)
        {

            //Grasp Requirements
            #region Grasp Requirements
            m_requiresThumbOrPalm = requiresThumbOrPalm;
            m_requiresThumb = requiresThumb;
            m_requiresPalm = requiresPalm;
            m_requiresFinger = requiresFinger;
            m_requiresNProximalContacts = requiresNProximalContacts;
            m_requiresNDistalContacts = requiresNDistalContacts;
            m_requiresNMedialContacts = requiresNMedialContacts;
            m_requiresContactDirectionality = requiresContactDirectionality;
            #endregion //Grasp Requirements
                        
        }//function - setGraspLogic

        #endregion //Public Access

        #endregion //Functions

    }//class - ContactState

    private ContactState m_contactState;
    #endregion //Class - ContactState


    //---------------------------------------
    // FUNCTIONS - UNITY3D (awake, start, reset, Update, FixedUpdate)
    //---------------------------------------
    #region Unity3D Functions
    #region Unity Debuggers
    //***********************************************************************
    // Debug variables to display in the editor.
    //
    public int m_rNumProximalContacts;
    public int m_rNumMedialContacts;
    public int m_rNumDistalContacts;

    //public bool m_rGotThumbContact;
    public bool m_rGotThumbDistalContact;
    public bool m_rGotThumbProximal1Contact;
    public bool m_rGotThumbProximal2Contact;
    public bool m_rGotPalmContact;

    public int m_lNumProximalContacts;
    public int m_lNumMedialContacts;
    public int m_lNumDistalContacts;

    //public bool m_lGotThumbContact;
    public bool m_lGotThumbDistalContact;
    public bool m_lGotThumbProximal1Contact;
    public bool m_lGotThumbProximal2Contact;
    public bool m_lGotPalmContact;

    public float distOffset;
    public float distY;
    public float distTipX;
    public float distX;
    //***********************************************************************

#if UNITY_EDITOR

    IEnumerator UpdateDebugVars()
    {
        while (true)
        {
            //FINGERS
    #region Right Hand Debug Vars
            m_rNumProximalContacts = m_contactState.RightNumProximalContacts;
            m_rNumMedialContacts = m_contactState.RightNumMedialContacts;
            m_rNumDistalContacts = m_contactState.RightNumDistalContacts;

            //PALM
            m_rGotPalmContact = m_contactState.HaveRightPalmContact;

            //THUMB
            //m_rGotThumbContact = m_contactState.HaveRightThumbContact;
            m_rGotThumbDistalContact = m_contactState.HaveRightThumbDistalContact;
            m_rGotThumbProximal1Contact = m_contactState.HaveRightThumbProximal1Contact;
            m_rGotThumbProximal2Contact = m_contactState.HaveRightThumbProximal2Contact;
            #endregion //Right Hand Debug Vars

            //LEFT HAND
    #region Left Hand Debug Vars
            //FINGERS
            m_lNumProximalContacts = m_contactState.LeftNumProximalContacts;
            m_lNumMedialContacts = m_contactState.LeftNumMedialContacts;
            m_lNumDistalContacts = m_contactState.LeftNumDistalContacts;

            //PALM
            m_lGotPalmContact = m_contactState.HaveLeftPalmContact;

            //THUMB
            //m_lGotThumbContact = m_contactState.HaveLeftThumbContact;
            m_lGotThumbDistalContact = m_contactState.HaveLeftThumbDistalContact;
            m_lGotThumbProximal1Contact = m_contactState.HaveLeftThumbProximal1Contact;
            m_lGotThumbProximal2Contact = m_contactState.HaveLeftThumbProximal2Contact;
            #endregion //Left Hand Debug Vars

            yield return new WaitForSeconds(1);
        }
    }

#endif

    #endregion //Unity Debuggers

    #region Start
    /// <summary>
    /// Is called at the beginning of program, will instantial objects
    /// </summary>
    void Start()
    {
        // Check for fixed joint that helps stabilize object to be grasped.
        FixedJoint preExistingFixedJoint = GetComponent<FixedJoint>();

        // Copies of these public member variables are saved inside 
        // m_contactState.  The public versions are not used again.
        m_contactState = new ContactState(GetComponent<Collider>(), preExistingFixedJoint,
            m_requiresThumbOrPalm, m_requiresThumb, m_requiresPalm, m_requiresFinger, m_requiresContactDirectionality,
            m_requiresNProximalContacts, m_requiresNMedialContacts, m_requiresNDistalContacts);

#if UNITY_EDITOR
        StartCoroutine(UpdateDebugVars());
#endif
    }//function - Start

    #endregion //Start

    #endregion //Unity3D Functions


    //---------------------------------------
    // FUNCTIONS - ACCESS / COMMUNICATION
    //---------------------------------------
    #region Access/Communication Function

    //PUBLIC ACCESS - Check for Current Grasp State
    #region Public Access Functions
    /// <summary>
    /// This function will allow external methods to access the 
    /// current state of grasp.
    /// </summary>
    /// <param name="strWhich">Set to "R" or "L" to specify which hand, otherwise returns false</param>
    public bool getGraspState(string strWhich)
    {
        try
        {
            if (strWhich == "R")
            {
                //Update Grasp State
                m_GraspInEffectR = m_contactState.getGraspState(strWhich);
                //return m_contactState.getGraspState(strWhich);

                return m_GraspInEffectR;
            }
            else if (strWhich == "L")
            {
                //Update Grasp State
                m_GraspInEffectL = m_contactState.getGraspState(strWhich);
                //return m_contactState.getGraspState(strWhich);

                return m_GraspInEffectL;
            }
            else
            {
                return false;
            }

            //Debug.Log("Reporting Grasp State");
            //return tempGraspState;
        }
        catch
        {
            //Update Grasp State
            m_GraspInEffectR = false;
            m_GraspInEffectL = false;

            return false;
        }//try - try to access the contactState object (might not be instantiated yet)

    }//function - getGraspState

    /// <summary>
    /// This function will allow external methods to access the 
    /// current state of grasp.
    /// </summary>
    /// <param name="requiresThumbOrPalm"></param>
    /// <param name="requiresThumb"></param>
    /// <param name="requiresPalm"></param>
    /// <param name="requiresFinger"></param>
    /// <param name="requiresContactDirectionality"></param>
    /// <param name="requiresNProximalContacts"></param>
    /// <param name="requiresNMedialContacts"></param>
    /// <param name="requiresNDistalContacts"></param>
    public void setGraspLogicPublic(bool requiresThumbOrPalm, bool requiresThumb, bool requiresPalm, bool requiresFinger, int requiresNProximalContacts, int requiresNMedialContacts, int requiresNDistalContacts, bool requiresContactDirectionality)
    {
        //Assign Grast Logic
        try
        {
            //Attempt to call the contactState object's set grasp logic method
            m_contactState.setGraspLogic(requiresThumbOrPalm, requiresThumb, requiresPalm, requiresFinger, requiresNProximalContacts, requiresNMedialContacts, requiresNDistalContacts, requiresContactDirectionality);
        }
        catch
        {
            //If m_contactState is not instantiated at load-up (when this method is called after ready grasp logic values from XML file), 
            //then assign global variables, which will be applied to m_contactState when instantiated

            //Assign Values to World Variables
            m_requiresThumbOrPalm = requiresThumbOrPalm;
            m_requiresThumb = requiresThumb;
            m_requiresPalm = requiresPalm;
            m_requiresFinger = requiresFinger;
            m_requiresNProximalContacts = requiresNProximalContacts;
            m_requiresNDistalContacts = requiresNDistalContacts;
            m_requiresNMedialContacts = requiresNMedialContacts;
            m_requiresContactDirectionality = requiresContactDirectionality;

        }//try - assign Grasp Logic Variables

    }//function - setGraspLogicPublic


    #endregion //Public Access Functions

    #endregion //Access/Communication Function


    //---------------------------------------
    // FUNCTIONS - GRASP RESOLUTION
    //---------------------------------------
    #region Collision Check Methods

    #region Palm Collisions
    //Function will test to see if collision object is at the surface of palm, if so, then confirmable collision with palm
    private void CheckRightPalmCollision(Collision colInfo)
    {
        Transform thTrans = colInfo.collider.gameObject.transform;
        BoxCollider box = (BoxCollider)colInfo.collider;
        float zOffset = box.center.z - box.size.z / 2 + 0.05f; // Get Z-Offset / Location of Collision

//TODO - make tolerance a changeable value (configurable)

        //Determine which object, from the list of all objects currently in collision with Palm, is which, and point to it
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = thTrans.InverseTransformPoint(p.point); //Get location of collision
            if (localPoint.z < zOffset) //Check offset, see if discrepency (tolerance of 0.05f)
            {
                // Found contact with proper side of palm.
                m_contactState.HaveRightPalmContact = true;
                break;
            }//if - check for Offset Range (at object surface)

        }//foreach - Each Collision

    }//function - CheckRightPalmCollision


    //Function will test to see if collision object is at the surface of palm, if so, then confirmable collision with palm
    private void CheckLeftPalmCollision(Collision colInfo)
    {
        Transform thTrans = colInfo.collider.gameObject.transform;
        BoxCollider box = (BoxCollider)colInfo.collider;
        float zOffset = box.center.z - box.size.z / 2 + 0.05f;

        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = thTrans.InverseTransformPoint(p.point);
            if (localPoint.z < zOffset)
            {
                // Found contact with proper side of palm.
                m_contactState.HaveLeftPalmContact = true;
                break;
            }//if - check for Offset Range (at object surface)

        }//foreach - Each Collision

    }//function - CheckLeftPalmCollision

    #endregion //Palm Collisions

    #region Distal Collisions
    //Function will test to see if collision object is at the surface of distal finger, if so, then confirmable collision
    private void CheckRightDistalCollision(Collision colInfo, int which)
    {
        Transform fingTrans = colInfo.collider.gameObject.transform;

        CapsuleCollider cap = colInfo.collider as CapsuleCollider;

//TODO - make the 0.5f hardcoded value into
 
        //Z - Direction Collisions
        float zOffset = cap.center.z - cap.radius * 0.5f;
        float centerZ = cap.center.z;

        //Y - Direction Collisions
        float tipYOffset = cap.center.y - cap.height / 2.0f + cap.radius / 2.0f;

        //NEW - adding X-direction collisions
        float xOffset = cap.center.x - cap.radius * 0.5f;
        //float centerX = cap.center.x;
        

#if UNITY_EDITOR
        distOffset = zOffset;
        distTipX = tipYOffset;
#endif

        //Traverse all detected collisions
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);

#if UNITY_EDITOR
            distY = localPoint.z;
            distX = localPoint.y;
#endif

//**********************************************************************************
            if (m_requiresContactDirectionality)
            {
                //only accept collisions in the palmer side of fingers
                if (localPoint.z < zOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proper side of proximal finger.
                    switch (which)
                    {
                        case ContactState.INDEX_IND:
                            m_contactState.RightIndexDistalContact = true;
                            break;
                        case ContactState.MIDDLE_IND:
                            m_contactState.RightMiddleDistalContact = true;
                            break;
                        case ContactState.RING_IND:
                            m_contactState.RightRingDistalContact = true;
                            break;
                        case ContactState.LITTLE_IND:
                            m_contactState.RightLittleDistalContact = true;
                            break;
                        default:
                            throw new System.ApplicationException(
                                "Bad 2nd argument (int which): " + which.ToString());
                    }

                    break;
                }//if - check to see if offset corresponds to surface location of palmer side of finger
            }//if - check to see if user specified palmer-side-only collisions
            else
            {
                //Accept collisions on all sides of fingers
                if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proper side of proximal finger.
                    switch (which)
                    {
                        case ContactState.INDEX_IND:
                            m_contactState.RightIndexDistalContact = true;
                            break;
                        case ContactState.MIDDLE_IND:
                            m_contactState.RightMiddleDistalContact = true;
                            break;
                        case ContactState.RING_IND:
                            m_contactState.RightRingDistalContact = true;
                            break;
                        case ContactState.LITTLE_IND:
                            m_contactState.RightLittleDistalContact = true;
                            break;
                        default:
                            throw new System.ApplicationException(
                                "Bad 2nd argument (int which): " + which.ToString());
                    }

                    break;
                }//if - check to see if offset corresponds to surface of finger
            }//if - check to see if user specified palmer-side-only collisions
            
        }//foreach - traversing all detected collisions
    }//function - CheckRightDistalCollision


    //Function will test to see if collision object is at the surface of distal finger, if so, then confirmable collision
    private void CheckLeftDistalCollision(Collision colInfo, int which)
    {
        Transform fingTrans = colInfo.collider.gameObject.transform;

        CapsuleCollider cap = colInfo.collider as CapsuleCollider;
        //Z
        float zOffset = cap.center.z - cap.radius * 0.5f;
        float centerZ = cap.center.z;

        //Y
        float tipYOffset = cap.center.y + cap.height / 2.0f + cap.radius / 2.0f;
        
        //NEW - adding X-direction collisions
        float xOffset = cap.center.x - cap.radius * 0.5f;
        //float centerX = cap.center.x;
        
#if UNITY_EDITOR
        distOffset = zOffset;
        distTipX = tipYOffset;
#endif

        //Traverse all detected collisions
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);

#if UNITY_EDITOR
            distY = localPoint.z;
            distX = localPoint.y;
#endif

            if (m_requiresContactDirectionality)
            {
                //only accept collisions in the palmer side of fingers
                if (localPoint.z < zOffset || localPoint.y > tipYOffset &&
                localPoint.z < centerZ)
                {
                // Found contact with proper side of proximal finger.
                switch (which)
                {
                    case ContactState.INDEX_IND:
                        m_contactState.LeftIndexDistalContact = true;
                        break;
                    case ContactState.MIDDLE_IND:
                        m_contactState.LeftMiddleDistalContact = true;
                        break;
                    case ContactState.RING_IND:
                        m_contactState.LeftRingDistalContact = true;
                        break;
                    case ContactState.LITTLE_IND:
                        m_contactState.LeftLittleDistalContact = true;
                        break;
                    default:
                        throw new System.ApplicationException(
                            "Bad 2nd argument (int which): " + which.ToString());
                }

                break;
                }//if - check to see if offset corresponds to surface location of palmer side of finger
            }//if - check to see if user specified palmer-side-only collisions
            else
            {
                //Accept collisions on all sides of fingers
                if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proper side of proximal finger.
                    switch (which)
                    {
                        case ContactState.INDEX_IND:
                            m_contactState.LeftIndexDistalContact = true;
                            break;
                        case ContactState.MIDDLE_IND:
                            m_contactState.LeftMiddleDistalContact = true;
                            break;
                        case ContactState.RING_IND:
                            m_contactState.LeftRingDistalContact = true;
                            break;
                        case ContactState.LITTLE_IND:
                            m_contactState.LeftLittleDistalContact = true;
                            break;
                        default:
                            throw new System.ApplicationException(
                                "Bad 2nd argument (int which): " + which.ToString());
                    }

                    break;
                }//if - check to see if offset corresponds to surface of finger
            }//if - check to see if user specified palmer-side-only collisions
            
        }//foreach - traversing all detected collisions

    }//function - CheckLeftDistalCollision
    
    #endregion //Distal Collisions

    #region Proximal Collisions
    //Function will test to see if collision object is at the surface of proximal finger, if so, then confirmable collision
    private void CheckRightProximalCollision(Collision colInfo, int which)
    {
        Transform fingTrans = colInfo.collider.gameObject.transform;

        CapsuleCollider cap = colInfo.collider as CapsuleCollider;

        //Z
        float zOffset = cap.center.z - cap.radius * 0.75f;
        float centerZ = cap.center.z;

        //Y
        float tipYOffset = cap.center.y + cap.height / 2.0f + cap.radius / 2.0f;

        //NEW - adding X-direction collisions
        float xOffset = cap.center.x - cap.radius * 0.75f;
        //float centerX = cap.center.x;

        //Traverse all detected collisions
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);


            if (m_requiresContactDirectionality)
            {
                //only accept collisions in the palmer side of fingers
                if (localPoint.z < zOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proper side of proximal finger.
                    switch (which)
                    {
                        case ContactState.INDEX_IND:
                            m_contactState.RightIndexProximalContact = true;
                            break;
                        case ContactState.MIDDLE_IND:
                            m_contactState.RightMiddleProximalContact = true;
                            break;
                        case ContactState.RING_IND:
                            m_contactState.RightRingProximalContact = true;
                            break;
                        case ContactState.LITTLE_IND:
                            m_contactState.RightLittleProximalContact = true;
                            break;
                        default:
                            throw new System.ApplicationException(
                                "Bad 2nd argument (int which): " + which.ToString());
                    }

                    break;
                }//if - check to see if offset corresponds to surface location of palmer side of finger
            }//if - check to see if user specified palmer-side-only collisions
            else
            {
                //Accept collisions on all sides of fingers
                if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proximal finger.
                    switch (which)
                    {
                        case ContactState.INDEX_IND:
                            m_contactState.RightIndexProximalContact = true;
                            break;
                        case ContactState.MIDDLE_IND:
                            m_contactState.RightMiddleProximalContact = true;
                            break;
                        case ContactState.RING_IND:
                            m_contactState.RightRingProximalContact = true;
                            break;
                        case ContactState.LITTLE_IND:
                            m_contactState.RightLittleProximalContact = true;
                            break;
                        default:
                            throw new System.ApplicationException(
                                "Bad 2nd argument (int which): " + which.ToString());
                    }

                    break;
                }//if - check to see if offset corresponds to surface of finger
            }//if - check to see if user specified palmer-side-only collisions

        }//foreach - traversing all detected collisions

    }//function - CheckRightProximalCollision


    //Function will test to see if collision object is at the surface of proximal finger, if so, then confirmable collision
    private void CheckLeftProximalCollision(Collision colInfo, int which)
    {
        if (which == ContactState.INDEX_IND)
        {
            // The left index proximal model is not scaled/rotated the same 
            // as the other proximal segments, so it needs special handling.
            CheckLeftIndexProximal(colInfo);
        }
        else
        {
            Transform fingTrans = colInfo.collider.gameObject.transform;

            CapsuleCollider cap = colInfo.collider as CapsuleCollider;

            //Z
            float zOffset = cap.center.z - cap.radius * 0.75f;
            float centerZ = cap.center.z;

            //Y
            float tipYOffset = cap.center.y + cap.height / 2.0f + cap.radius / 2.0f;

            //NEW - adding X-direction collisions
            float xOffset = cap.center.x - cap.radius * 0.75f;
            //float centerX = cap.center.x;


            //Traverse all detected collisions
            foreach (ContactPoint p in colInfo.contacts)
            {
                Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);

                if (m_requiresContactDirectionality)
                {
                    //only accept collisions in the palmer side of fingers

                    if (localPoint.z < zOffset || localPoint.y > tipYOffset &&
                        localPoint.z < centerZ)
                    {
                        // Found contact with proper side of proximal finger.
                        switch (which)
                        {
                            //case ContactState.INDEX_IND:
                            //    m_contactState.LeftIndexProximalContact = true;
                            //    break;
                            case ContactState.MIDDLE_IND:
                                m_contactState.LeftMiddleProximalContact = true;
                                break;
                            case ContactState.RING_IND:
                                m_contactState.LeftRingProximalContact = true;
                                break;
                            case ContactState.LITTLE_IND:
                                m_contactState.LeftLittleProximalContact = true;
                                break;
                            default:
                                throw new System.ApplicationException(
                                    "Bad 2nd argument (int which): " + which.ToString());
                        }

                        break;
                    }//if - check to see if offset corresponds to surface location of palmer side of finger
                }//if - check to see if user specified palmer-side-only collisions
                else
                {
                    //Accept collisions on all sides of fingers
                    if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                        localPoint.z < centerZ)
                    {
                        // Found contact with proximal finger.
                        switch (which)
                        {
                            //case ContactState.INDEX_IND:
                            //    m_contactState.LeftIndexProximalContact = true;
                            //    break;
                            case ContactState.MIDDLE_IND:
                                m_contactState.LeftMiddleProximalContact = true;
                                break;
                            case ContactState.RING_IND:
                                m_contactState.LeftRingProximalContact = true;
                                break;
                            case ContactState.LITTLE_IND:
                                m_contactState.LeftLittleProximalContact = true;
                                break;
                            default:
                                throw new System.ApplicationException(
                                    "Bad 2nd argument (int which): " + which.ToString());
                        }

                        break;
                    }//if - check to see if offset corresponds to surface of finger
                }//if - check to see if user specified palmer-side-only collisions

            }//foreach - traversing all detected collisions

        }//if - check for contact state (special handling of left proximal finger)

    }//function - CheckLeftProximalCollision


    //Function will test to see if collision object is at the surface of proximal finger, if so, then confirmable collision
    private void CheckLeftIndexProximal(Collision colInfo)
    {
        Transform fingTrans = colInfo.collider.gameObject.transform;

        CapsuleCollider cap = colInfo.collider as CapsuleCollider;

        //Z
        float zOffset = cap.center.z - cap.radius * 0.75f;
        float centerZ = cap.center.z;

        //Y
        float tipYOffset = cap.center.y + cap.height / 2.0f + cap.radius / 2.0f;

        //NEW - adding X-direction collisions
        float xOffset = cap.center.x - cap.radius * 0.75f;
        //float centerX = cap.center.x;


        //Traverse all detected collisions
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);

            if (m_requiresContactDirectionality)
            {
                //only accept collisions in the palmer side of fingers
                if (localPoint.z > zOffset || localPoint.y < tipYOffset &&
                    localPoint.z > centerZ)
                {
                    // Found contact with proper side of proximal finger.
                    m_contactState.LeftIndexProximalContact = true;
                    break;
                }//if - check to see if offset corresponds to surface location of palmer side of finger
            }//if - check to see if user specified palmer-side-only collisions
            else
            {
                //Accept collisions on all sides of fingers
                if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proximal finger.
                    m_contactState.LeftIndexProximalContact = true;
                    break;

                }//if - check to see if offset corresponds to surface of finger
            }//if - check to see if user specified palmer-side-only collisions

        }//foreach - traversing all detected collisions

    }//function - CheckLeftIndexProximal
    #endregion //Proximal Collisions

    #region Medial Collisions
    //Function will test to see if collision object is at the surface of medial finger, if so, then confirmable collision
    private void CheckRightMedialCollision(Collision colInfo, int which)
    {
        Transform fingTrans = colInfo.collider.gameObject.transform;

        CapsuleCollider cap = colInfo.collider as CapsuleCollider; //finger element is capsule

        //Z
        float zOffset = cap.center.z - cap.radius * 0.75f;
        float centerZ = cap.center.z;

        //Y
        float tipYOffset = cap.center.y + cap.height / 2.0f + cap.radius / 2.0f;

        //NEW - adding X-direction collisions
        float xOffset = cap.center.x - cap.radius * 0.75f;
        //float centerX = cap.center.x;

        //Traverse all detected collisions
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);


            if (m_requiresContactDirectionality)
            {
                //only accept collisions in the palmer side of fingers
                if (localPoint.z < zOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proper side of proximal finger.
                    switch (which)
                    {
                        case ContactState.INDEX_IND:
                            m_contactState.RightIndexMedialContact = true;
                            break;
                        case ContactState.MIDDLE_IND:
                            m_contactState.RightMiddleMedialContact = true;
                            break;
                        case ContactState.RING_IND:
                            m_contactState.RightRingMedialContact = true;
                            break;
                        case ContactState.LITTLE_IND:
                            m_contactState.RightLittleMedialContact = true;
                            break;
                        default:
                            throw new System.ApplicationException(
                                "Bad 2nd argument (int which): " + which.ToString());
                    }

                    break;
                }//if - check to see if offset corresponds to surface location of palmer side of finger
            }//if - check to see if user specified palmer-side-only collisions
            else
            {
                //Accept collisions on all sides of fingers
                if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proximal finger.
                    switch (which)
                    {
                        case ContactState.INDEX_IND:
                            m_contactState.RightIndexMedialContact = true;
                            break;
                        case ContactState.MIDDLE_IND:
                            m_contactState.RightMiddleMedialContact = true;
                            break;
                        case ContactState.RING_IND:
                            m_contactState.RightRingMedialContact = true;
                            break;
                        case ContactState.LITTLE_IND:
                            m_contactState.RightLittleMedialContact = true;
                            break;
                        default:
                            throw new System.ApplicationException(
                                "Bad 2nd argument (int which): " + which.ToString());
                    }

                    break;
                }//if - check to see if offset corresponds to surface of finger
            }//if - check to see if user specified palmer-side-only collisions

        }//foreach - traversing all detected collisions

    }//function - CheckRightMedialCollision


    //Function will test to see if collision object is at the surface of medial finger, if so, then confirmable collision
    private void CheckLeftMedialCollision(Collision colInfo, int which)
    {
        if (which == ContactState.INDEX_IND)
        {
            // The left index proximal model is not scaled/rotated the same 
            // as the other proximal segments, so it needs special handling.
            CheckLeftIndexMedial(colInfo);
        }
        else
        {
            Transform fingTrans = colInfo.collider.gameObject.transform;

            CapsuleCollider cap = colInfo.collider as CapsuleCollider;

            //Z
            float zOffset = cap.center.z - cap.radius * 0.75f;
            float centerZ = cap.center.z;

            //Y
            float tipYOffset = cap.center.y + cap.height / 2.0f + cap.radius / 2.0f;

            //NEW - adding X-direction collisions
            float xOffset = cap.center.x - cap.radius * 0.75f;
            //float centerX = cap.center.x;


            //Traverse all detected collisions
            foreach (ContactPoint p in colInfo.contacts)
            {
                Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);

                if (m_requiresContactDirectionality)
                {
                    //only accept collisions in the palmer side of fingers

                    if (localPoint.z < zOffset || localPoint.y > tipYOffset &&
                        localPoint.z < centerZ)
                    {
                        // Found contact with proper side of proximal finger.
                        switch (which)
                        {
                            //case ContactState.INDEX_IND:
                            //    m_contactState.LeftIndexProximalContact = true;
                            //    break;
                            case ContactState.MIDDLE_IND:
                                m_contactState.LeftMiddleMedialContact = true;
                                break;
                            case ContactState.RING_IND:
                                m_contactState.LeftRingMedialContact = true;
                                break;
                            case ContactState.LITTLE_IND:
                                m_contactState.LeftLittleMedialContact = true;
                                break;
                            default:
                                throw new System.ApplicationException(
                                    "Bad 2nd argument (int which): " + which.ToString());
                        }

                        break;
                    }//if - check to see if offset corresponds to surface location of palmer side of finger
                }//if - check to see if user specified palmer-side-only collisions
                else
                {
                    //Accept collisions on all sides of fingers
                    if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                        localPoint.z < centerZ)
                    {
                        // Found contact with proximal finger.
                        switch (which)
                        {
                            //case ContactState.INDEX_IND:
                            //    m_contactState.LeftIndexProximalContact = true;
                            //    break;
                            case ContactState.MIDDLE_IND:
                                m_contactState.LeftMiddleMedialContact = true;
                                break;
                            case ContactState.RING_IND:
                                m_contactState.LeftRingMedialContact = true;
                                break;
                            case ContactState.LITTLE_IND:
                                m_contactState.LeftLittleMedialContact = true;
                                break;
                            default:
                                throw new System.ApplicationException(
                                    "Bad 2nd argument (int which): " + which.ToString());
                        }

                        break;
                    }//if - check to see if offset corresponds to surface of finger
                }//if - check to see if user specified palmer-side-only collisions

            }//foreach - traversing all detected collisions

        }//if - check for contact state (special handling of left proximal finger)

    }//function - CheckLeftMedialCollision


    //Function will test to see if collision object is at the surface of medial finger, if so, then confirmable collision
    private void CheckLeftIndexMedial(Collision colInfo)
    {
        Transform fingTrans = colInfo.collider.gameObject.transform;

        CapsuleCollider cap = colInfo.collider as CapsuleCollider;

        //Z
        float zOffset = cap.center.z - cap.radius * 0.75f;
        float centerZ = cap.center.z;

        //Y
        float tipYOffset = cap.center.y + cap.height / 2.0f + cap.radius / 2.0f;

        //NEW - adding X-direction collisions
        float xOffset = cap.center.x - cap.radius * 0.75f;
        //float centerX = cap.center.x;


        //Traverse all detected collisions
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = fingTrans.InverseTransformPoint(p.point);

            if (m_requiresContactDirectionality)
            {
                //only accept collisions in the palmer side of fingers
                if (localPoint.z > zOffset || localPoint.y < tipYOffset &&
                    localPoint.z > centerZ)
                {
                    // Found contact with proper side of proximal finger.
                    m_contactState.LeftIndexMedialContact = true;
                    break;
                }//if - check to see if offset corresponds to surface location of palmer side of finger
            }//if - check to see if user specified palmer-side-only collisions
            else
            {
                //Accept collisions on all sides of fingers
                if (localPoint.z < zOffset || localPoint.x < xOffset || localPoint.y < tipYOffset &&
                    localPoint.z < centerZ)
                {
                    // Found contact with proximal finger.
                    m_contactState.LeftIndexMedialContact = true;
                    break;

                }//if - check to see if offset corresponds to surface of finger
            }//if - check to see if user specified palmer-side-only collisions

        }//foreach - traversing all detected collisions

    }//function - CheckLeftIndexMedial

    #endregion //Medial Collisions

    #region Thumb Collisions

    //Function will test to see if collision object is at the surface of thumb, if so, then confirmable collision
    private void CheckRightThumbCollision(Collision colInfo)
    {
        CheckThumbCollision(colInfo, true);
    }//function - CheckRightThumbCollision


    //Function will test to see if collision object is at the surface of thumb, if so, then confirmable collision
    private void CheckLeftThumbCollision(Collision colInfo)
    {
        CheckThumbCollision(colInfo, false);
    }//function - CheckLeftThumbCollision


    //Function will test to see if collision object is at the surface of thumb, if so, then confirmable collision
    private void CheckThumbCollision(Collision colInfo, bool isRight)
    {
        //CHECK SURFACE CONTACT
        //Determine based on object collider and distance from if contact on surface
        Transform thTrans = colInfo.collider.gameObject.transform;
        CapsuleCollider cap = colInfo.collider as CapsuleCollider;
        
        // Cache name property.
        string name = colInfo.collider.name;

        //Y
        float yOffset = cap.center.y - cap.radius * 0.75f;
        float centerY = cap.center.y;

        //X
        float tipX = cap.center.x - cap.height / 2.0f + cap.radius / 2.0f;
        
        //Z - NEW
        float zOffset = cap.center.z - cap.radius * 0.75f;
        //float centerZ = cap.center.z;
        

        //Traverse all detected collisions
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = thTrans.InverseTransformPoint(p.point);

            #region Check Contact Requirements
            if (m_requiresContactDirectionality)
            {
                //only accept collisions in the palmer side of fingers

                if (localPoint.y < yOffset || localPoint.x < tipX && localPoint.y < centerY)
                {
                    //Debug.Log("Thumb Collision - Distal, ");
                    
                    // Found contact with proper side of thumb surface.
                    
                    #region Update Proper Contact Flag
                    //Check for Right/Left Hand
                    if (isRight) //RIGHT
                    {
                        //Check for which thumb segment
                        if (name == ms_rTHUMB_DISTAL)
                        {
                            m_contactState.HaveRightThumbDistalContact = true;
                        }
                        else if (name == ms_rTHUMB_PROXIMAL1)
                        {
                            m_contactState.HaveRightThumbProximal1Contact = true;
                        }
                        else if (name == ms_rTHUMB_PROXIMAL2)
                        {
                            m_contactState.HaveRightThumbProximal2Contact = true;
                        }//if - check for thumb surface
                        
                    }
                    else //LEFT
                    {
                        //Check for which thumb segment
                        if (name == ms_lTHUMB_DISTAL)
                        {
                            m_contactState.HaveLeftThumbDistalContact = true;
                        }
                        else if (name == ms_lTHUMB_PROXIMAL1)
                        {
                            m_contactState.HaveLeftThumbProximal1Contact = true;
                        }
                        else if (name == ms_lTHUMB_PROXIMAL2)
                        {
                            m_contactState.HaveLeftThumbProximal2Contact = true;
                        }//if - check for thumb surface
                        
                    }//if - check for right/left hand
                    #endregion //Update Proper Contact Flag
                    
                    break;
                }//if - check to see if offset corresponds to surface location of palmer side of finger

            }//if - check to see if user specified palmer-side-only collisions
            else
            {
                //Accept collisions on all sides of fingers
                if (localPoint.y < yOffset || localPoint.z < zOffset || localPoint.x < tipX && localPoint.y < centerY)
                {
                    // Found contact with ANY side of thumb surface.

                    #region Update Proper Contact Flag
                    //Check for Right/Left Hand
                    if (isRight) //RIGHT
                    {
                        //Check for which thumb segment
                        if (name == ms_rTHUMB_DISTAL)
                        {
                            m_contactState.HaveRightThumbDistalContact = true;
                        }
                        else if (name == ms_rTHUMB_PROXIMAL1)
                        {
                            m_contactState.HaveRightThumbProximal1Contact = true;
                        }
                        else if (name == ms_rTHUMB_PROXIMAL2)
                        {
                            m_contactState.HaveRightThumbProximal2Contact = true;
                        }//if - check for thumb surface

                    }
                    else //LEFT
                    {
                        //Check for which thumb segment
                        if (name == ms_lTHUMB_DISTAL)
                        {
                            m_contactState.HaveLeftThumbDistalContact = true;
                        }
                        else if (name == ms_lTHUMB_PROXIMAL1)
                        {
                            m_contactState.HaveLeftThumbProximal1Contact = true;
                        }
                        else if (name == ms_lTHUMB_PROXIMAL2)
                        {
                            m_contactState.HaveLeftThumbProximal2Contact = true;
                        }//if - check for thumb surface

                    }//if - check for right/left hand
                    #endregion //Update Proper Contact Flag
                    
                    
                    // Found contact with proper side of distal thumb.
                    //if (isRight)
                    //    m_contactState.HaveRightThumbContact = true;
                    //else
                    //    m_contactState.HaveLeftThumbContact = true;

                    break;
                }//if - check to see if offset corresponds to surface of finger

            }//if - check to see if user specified palmer-side-only collisions
            #endregion //Check Contact Requirements

        }//foreach - traversing all detected collisions

    }//function - CheckThumbCollision

    #endregion //Thumb Collisions

    #endregion //Collision Check Methods


    //---------------------------------------
    // FUNCTIONS - COLLISION DETECTION 
    //---------------------------------------
    #region Collision Handlers (Enter/Exit)

    // COLLISIONS EVENT HANDLER

    // ON-COLLISION
    #region On Collision (Enter)
    //Function that is called when collision is detected
    void OnCollisionEnter(Collision colInfo)
    {
        // Cache name property.
        string name = colInfo.collider.name;

        try
        {
            //THUMB
            #region Thumb Contact
            if (m_contactState.RequiresThumb || m_contactState.RequiresThumbOrPalm)
            {
                //Distal Only
                //if (name == ms_rTHUMB_DISTAL)

                //Any Part of Thumb
                if ((name == ms_rTHUMB_DISTAL) || (name == ms_rTHUMB_PROXIMAL1) || (name == ms_rTHUMB_PROXIMAL2))
                {
                    CheckRightThumbCollision(colInfo);
                }//if - check for right thumb surface
                //else if (name == ms_lTHUMB_DISTAL)
                else if ((name == ms_lTHUMB_DISTAL) || (name == ms_lTHUMB_PROXIMAL1) || (name == ms_lTHUMB_PROXIMAL2))
                {
                    CheckLeftThumbCollision(colInfo);
                }//if - check for left thumb surface
            }
            #endregion //Thumb Contact

            //PALM
            #region Palm Contact
            if (m_contactState.RequiresPalm || m_contactState.RequiresThumbOrPalm)
            {
                if (name == ms_rPALM)
                    CheckRightPalmCollision(colInfo);
                else if (name == ms_lPALM)
                    CheckLeftPalmCollision(colInfo);
            }
            #endregion //Palm Contact

            //PROXIMAL
            #region Proximal Contact
            if ((m_contactState.RequiresNProximalContacts > 0) || m_contactState.RequiresFinger)
            {
                if (name == ms_rIND_PROXIMAL)
                    CheckRightProximalCollision(colInfo, ContactState.INDEX_IND);
                else if (name == ms_rMID_PROXIMAL)
                    CheckRightProximalCollision(colInfo, ContactState.MIDDLE_IND);
                else if (name == ms_rRING_PROXIMAL)
                    CheckRightProximalCollision(colInfo, ContactState.RING_IND);
                else if (name == ms_rLITTLE_PROXIMAL)
                    CheckRightProximalCollision(colInfo, ContactState.LITTLE_IND);
                else if (name == ms_lIND_PROXIMAL)
                    CheckLeftProximalCollision(colInfo, ContactState.INDEX_IND);
                else if (name == ms_lMID_PROXIMAL)
                    CheckLeftProximalCollision(colInfo, ContactState.MIDDLE_IND);
                else if (name == ms_lRING_PROXIMAL)
                    CheckLeftProximalCollision(colInfo, ContactState.RING_IND);
                else if (name == ms_lLITTLE_PROXIMAL)
                    CheckLeftProximalCollision(colInfo, ContactState.LITTLE_IND);
            }
            #endregion //Proximal Contact

            //MEDIAL
            #region Medial Contact
            if ((m_contactState.RequiresNMedialContacts > 0) || m_contactState.RequiresFinger)
            {
                if (name == ms_rIND_MEDIAL)
                    CheckRightMedialCollision(colInfo, ContactState.INDEX_IND);
                else if (name == ms_rMID_MEDIAL)
                    CheckRightMedialCollision(colInfo, ContactState.MIDDLE_IND);
                else if (name == ms_rRING_MEDIAL)
                    CheckRightMedialCollision(colInfo, ContactState.RING_IND);
                else if (name == ms_rLITTLE_MEDIAL)
                    CheckRightMedialCollision(colInfo, ContactState.LITTLE_IND);
                else if (name == ms_lIND_MEDIAL)
                    CheckLeftMedialCollision(colInfo, ContactState.INDEX_IND);
                else if (name == ms_lMID_MEDIAL)
                    CheckLeftMedialCollision(colInfo, ContactState.MIDDLE_IND);
                else if (name == ms_lRING_MEDIAL)
                    CheckLeftMedialCollision(colInfo, ContactState.RING_IND);
                else if (name == ms_lLITTLE_MEDIAL)
                    CheckLeftMedialCollision(colInfo, ContactState.LITTLE_IND);
            }
            #endregion //Medial Contact

            //DISTAL
            #region Distal Contact
            if ((m_contactState.RequiresNDistalContacts > 0) || m_contactState.RequiresFinger)
            {
                if (name == ms_rIND_DISTAL)
                    CheckRightDistalCollision(colInfo, ContactState.INDEX_IND);
                else if (name == ms_rMID_DISTAL)
                    CheckRightDistalCollision(colInfo, ContactState.MIDDLE_IND);
                else if (name == ms_rRING_DISTAL)
                    CheckRightDistalCollision(colInfo, ContactState.RING_IND);
                else if (name == ms_rLITTLE_DISTAL)
                    CheckRightDistalCollision(colInfo, ContactState.LITTLE_IND);
                else if (name == ms_lIND_DISTAL)
                    CheckLeftDistalCollision(colInfo, ContactState.INDEX_IND);
                else if (name == ms_lMID_DISTAL)
                    CheckLeftDistalCollision(colInfo, ContactState.MIDDLE_IND);
                else if (name == ms_lRING_DISTAL)
                    CheckLeftDistalCollision(colInfo, ContactState.RING_IND);
                else if (name == ms_lLITTLE_DISTAL)
                    CheckLeftDistalCollision(colInfo, ContactState.LITTLE_IND);
            }//if - check for specific element
            #endregion //Distal Contact

            //Debug.Log("Collider: " + name);
            str_ColliderName = name; //SET PUBLIC VARIABLE

        }
        catch
        {
            //If the surface doesn't have a proper collider, or if neither is instantiated (collision occurs at beginning of program) - won't work

        }//try - attempt to detect collisions
    }//function - OnCollisionEnter

    #endregion //On Collision (Enter)

    // OFF-COLLISION
    #region On Collision (Exit)
    void OnCollisionExit(Collision colInfo)
    {
        string name = colInfo.collider.name; //Get the name of collider
        if (string.IsNullOrEmpty(name)) //Check to see if name is void
            return;

        //Set the flag/attribute check-list for each contact-able CAD element
        #region Right Hand
        if (name[0] == 'r') //Common attribute to the right-arm physical CAD elements ('r_...')
        {
            if (name == ms_rTHUMB_DISTAL)
                m_contactState.HaveRightThumbDistalContact = false;
            else if (name == ms_rTHUMB_PROXIMAL1)
                m_contactState.HaveRightThumbProximal1Contact = false;
            else if (name == ms_rTHUMB_PROXIMAL2)
                m_contactState.HaveRightThumbProximal2Contact = false; 
            else if (name == ms_rIND_DISTAL)
                m_contactState.RightIndexDistalContact = false;
            else if (name == ms_rIND_PROXIMAL)
                m_contactState.RightIndexProximalContact = false;
            else if (name == ms_rMID_DISTAL)
                m_contactState.RightMiddleDistalContact = false;
            else if (name == ms_rMID_PROXIMAL)
                m_contactState.RightMiddleProximalContact = false;
            else if (name == ms_rRING_DISTAL)
                m_contactState.RightRingDistalContact = false;
            else if (name == ms_rRING_PROXIMAL)
                m_contactState.RightRingProximalContact = false;
            else if (name == ms_rLITTLE_DISTAL)
                m_contactState.RightLittleDistalContact = false;
            else if (name == ms_rLITTLE_PROXIMAL)
                m_contactState.RightLittleProximalContact = false;
            else if (name == ms_rPALM)
                m_contactState.HaveRightPalmContact = false;
            else if (name == ms_rIND_MEDIAL)
                m_contactState.RightIndexMedialContact = false;
            else if (name == ms_rMID_MEDIAL)
                m_contactState.RightMiddleMedialContact = false;
            else if (name == ms_rRING_MEDIAL)
                m_contactState.RightRingMedialContact = false;
            else if (name == ms_rLITTLE_MEDIAL)
                m_contactState.RightLittleMedialContact = false;

        }
        #endregion //Right Hand

        #region Left Hand
        else if (name[0] == 'l') //Common attribute to the left-arm physical CAD elements ('r_...')
        {
            if (name == ms_lTHUMB_DISTAL)
                m_contactState.HaveLeftThumbDistalContact = false;
            else if (name == ms_lTHUMB_PROXIMAL1)
                m_contactState.HaveLeftThumbProximal1Contact = false;
            else if (name == ms_lTHUMB_PROXIMAL2)
                m_contactState.HaveLeftThumbProximal2Contact = false; 
            else if (name == ms_lIND_DISTAL)
                m_contactState.LeftIndexDistalContact = false;
            else if (name == ms_lIND_PROXIMAL)
                m_contactState.LeftIndexProximalContact = false;
            else if (name == ms_lMID_DISTAL)
                m_contactState.LeftMiddleDistalContact = false;
            else if (name == ms_lMID_PROXIMAL)
                m_contactState.LeftMiddleProximalContact = false;
            else if (name == ms_lRING_DISTAL)
                m_contactState.LeftRingDistalContact = false;
            else if (name == ms_lRING_PROXIMAL)
                m_contactState.LeftRingProximalContact = false;
            else if (name == ms_lLITTLE_DISTAL)
                m_contactState.LeftLittleDistalContact = false;
            else if (name == ms_lLITTLE_PROXIMAL)
                m_contactState.LeftLittleProximalContact = false;
            else if (name == ms_lPALM)
                m_contactState.HaveLeftPalmContact = false;

            else if (name == ms_lIND_MEDIAL)
                m_contactState.LeftIndexMedialContact = false;
            else if (name == ms_lMID_MEDIAL)
                m_contactState.LeftMiddleMedialContact = false;
            else if (name == ms_lRING_MEDIAL)
                m_contactState.LeftRingMedialContact = false;
            else if (name == ms_lLITTLE_MEDIAL)
                m_contactState.LeftLittleMedialContact = false;
            
            
        }//if - check for specific element
        #endregion //Left Hand

    }//function - OnCollisionExit

    #endregion //On Collision (Exit)

    #endregion //Collision Handlers (On/Off)



}//class - GraspableObject