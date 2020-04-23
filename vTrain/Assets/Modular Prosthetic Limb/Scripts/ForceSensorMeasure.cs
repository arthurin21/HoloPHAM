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

public class ForceSensorMeasure : MonoBehaviour {

    protected Dictionary<Collider, string> m_validContacts;

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

    //cached joint values
    protected float[] m_actVal = new float[11];
    protected float[] m_cmdVal = new float[11];

    //delegate mothods
    protected delegate float ValueAA();
    protected delegate float ValueMCP();
    protected delegate float ValuePIP();
    protected delegate float ValueDIP();
    protected ValueAA m_valAA;
    protected ValueMCP m_valMCP;
    protected ValuePIP m_valPIP;
    protected ValueDIP m_valDIP;

    protected static readonly object m_valLock = new object();

    protected int m_sliderGroupID = 0;
    protected string[] m_sliderGroupLabel = { "Thumb", "Index", "Middle" };
    protected int[] m_sliderOffset = { 170, 290, 410 };

    private GUIStyle m_labelStyle = null;

    private Collision m_lastCollision = null;

    private string m_name;
    private Vector3 m_normal = new Vector3();
    private Vector3 m_relvel = new Vector3();

    /// <summary>
    /// Simulated contact force.
    /// </summary>
    protected Vector3 m_contactForce;
    protected bool m_contact = false;
    public bool InContact { get { return m_contact; } }

    
    #region initializers

    private void MapHingeJoints()
    {
        UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(HingeJoint));
        List<HingeJoint> hinges = new List<HingeJoint>();
        foreach (UnityEngine.Object o in objects)
        {
            hinges.Add(o as HingeJoint);
        }

        m_medFE = FindHingeJoint(hinges, GetComponent<Rigidbody>());
        m_proxFE = FindHingeJoint(hinges, m_medFE.GetComponent<Rigidbody>());
        m_mcpFE = FindHingeJoint(hinges, m_proxFE.GetComponent<Rigidbody>());
        m_finAA = FindHingeJoint(hinges, m_mcpFE.GetComponent<Rigidbody>());
        m_wrFE = FindHingeJoint(hinges, m_finAA.GetComponent<Rigidbody>());
        m_wrDev = FindHingeJoint(hinges, m_wrFE.GetComponent<Rigidbody>());
        m_wrRot = FindHingeJoint(hinges, m_wrDev.GetComponent<Rigidbody>());
        m_elbFE = FindHingeJoint(hinges, m_wrRot.GetComponent<Rigidbody>());
        m_humRot = FindHingeJoint(hinges, m_elbFE.GetComponent<Rigidbody>());
        m_shAA = FindHingeJoint(hinges, m_humRot.GetComponent<Rigidbody>());
        m_shFE = FindHingeJoint(hinges, m_shAA.GetComponent<Rigidbody>());
    }

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

        // Hinge found, so remove it from the list.
        if (hj != null)
            hinges.Remove(hj);

        return hj;
    }

    private void MapDelegates()
    {
        switch (m_name)
        {
            case "rThDistal":
                m_sliderGroupID = 0;
                m_valAA = delegate() { return VulcanXInterface.RightThumbAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightThumbFE(); };
                m_valPIP = delegate() { return VulcanXInterface.RightThumbMCP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightThumbDIP(); };
                break;

            case "rIndDistal":
                m_sliderGroupID = 1;
                m_valAA = delegate() { return VulcanXInterface.RightIndexAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightIndexMCP(); };
                m_valPIP = delegate() { return VulcanXInterface.RightIndexPIP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightIndexDIP(); };
                break;

            case "rMidDistal":
                m_sliderGroupID = 2;
                m_valAA = delegate() { return VulcanXInterface.RightMiddleAA(); };
                m_valMCP = delegate() { return VulcanXInterface.RightMiddleMCP(); };
                m_valPIP = delegate() { return VulcanXInterface.RightMiddlePIP(); };
                m_valDIP = delegate() { return VulcanXInterface.RightMiddleDIP(); };
                break;
        }
    }

    #endregion

    #region Unity methods
    void Start()
    {
        m_name = name;
        m_validContacts = new Dictionary<Collider, string>();
        MapHingeJoints();
        MapDelegates();
        m_contactForce = Vector3.zero;
        m_contact = false;
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        //config label style
        if (m_labelStyle == null)
        {
            m_labelStyle = new GUIStyle(GUI.skin.label);
            m_labelStyle.normal.textColor = Color.white;
        }

        lock (m_valLock)
        {
            //display arm sliders
            GUI.Label(new Rect(5, 10, 45, 20), "shFE");
            GUI.HorizontalSlider(new Rect(55, 15, 100, 20),
                Mathf.Abs(m_cmdVal[0] - m_actVal[0]), 0, 50);
            GUI.Label(new Rect(5, 30, 45, 20), "shAA");
            GUI.HorizontalSlider(new Rect(55, 35, 100, 20),
                Mathf.Abs(m_cmdVal[1] - m_actVal[1]), 0, 50);
            GUI.Label(new Rect(5, 50, 45, 20), "humRot");
            GUI.HorizontalSlider(new Rect(55, 55, 100, 20),
                Mathf.Abs(m_cmdVal[2] - m_actVal[2]), 0, 50);
            GUI.Label(new Rect(5, 70, 45, 20), "elbFE");
            GUI.HorizontalSlider(new Rect(55, 75, 100, 20),
                Mathf.Abs(m_cmdVal[3] - m_actVal[3]), 0, 50);
            GUI.Label(new Rect(5, 90, 45, 20), "wrRot");
            GUI.HorizontalSlider(new Rect(55, 95, 100, 20),
                Mathf.Abs(m_cmdVal[4] - m_actVal[4]), 0, 50);
            GUI.Label(new Rect(5, 110, 45, 20), "wrDev");
            GUI.HorizontalSlider(new Rect(55, 115, 100, 20),
                Mathf.Abs(m_cmdVal[5] - m_actVal[5]), 0, 50);
            GUI.Label(new Rect(5, 130, 45, 20), "wrFE");
            GUI.HorizontalSlider(new Rect(55, 135, 100, 20),
                Mathf.Abs(m_cmdVal[6] - m_actVal[6]), 0, 50);

            //display finger sliders
            int offset = m_sliderOffset[m_sliderGroupID];
            GUI.Label(new Rect(55, offset + 0, 60, 20), m_sliderGroupLabel[m_sliderGroupID], m_labelStyle);
            GUI.Label(new Rect(5, offset + 20, 45, 20), "finAA");
            GUI.HorizontalSlider(new Rect(55, offset + 25, 100, 20),
                Mathf.Abs(m_cmdVal[7] - m_actVal[7]), 0, 50);
            GUI.Label(new Rect(5, offset + 40, 45, 20), "mcpFE");
            GUI.HorizontalSlider(new Rect(55, offset + 45, 100, 20),
                Mathf.Abs(m_cmdVal[8] - m_actVal[8]), 0, 50);
            GUI.Label(new Rect(5, offset + 60, 45, 20), "proxFE");
            GUI.HorizontalSlider(new Rect(55, offset + 65, 100, 20),
                Mathf.Abs(m_cmdVal[9] - m_actVal[9]), 0, 50);
            GUI.Label(new Rect(5, offset + 80, 45, 20), "medFE");
            GUI.HorizontalSlider(new Rect(55, offset + 85, 100, 20),
                Mathf.Abs(m_cmdVal[10] - m_actVal[10]), 0, 50);
        }


    }

#endif


    void Update()
    {
    }

    void FixedUpdate()
    {
        lock (m_valLock)
        {
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
            m_cmdVal[7] = m_valAA();
            m_cmdVal[8] = m_valMCP();
            m_cmdVal[9] = m_valPIP();
            m_cmdVal[10] = m_valDIP();
        }

        m_contactForce.Normalize();
        // Unity 3.4+ call.
        //Debug.DrawRay(transform.position, m_contactForce, Color.yellow, 0.25f);
        //Debug.DrawRay(transform.position, m_contactForce*100, Color.yellow, 1.0f);
        // Multiply by position error of joints.

        // Send out summed force vector to VulcanXInterface.


        // Zero force vector in preparation for collision events after the 
        // current time step.
        //m_contactForce = Vector3.zero;
    }

    //void OnDrawGizmos()
    //{
    //    if (m_lastCollision == null)
    //        return;

    //    foreach (ContactPoint p in m_lastCollision.contacts)
    //    {
    //        Gizmos.DrawRay(p.point, m_contactForce * 10);
    //    }
    //}

    void OnCollisionEnter(Collision colInfo)
    {
        m_lastCollision = colInfo;
        
        //REMOVE WARNING ONLY
        m_contact = m_lastCollision.collider.enabled;
        //REMOVE WARNING ONLY
        
        m_contact = true;
        m_validContacts.Add(colInfo.collider, null);
        foreach (ContactPoint p in colInfo.contacts)
        {
            if (p.thisCollider.name == m_name)
            {
                m_normal = p.normal;
                m_relvel = colInfo.relativeVelocity;
            }
            m_contactForce += p.normal;
            Debug.DrawRay(p.point, p.normal*colInfo.relativeVelocity.magnitude, Color.yellow, 0.5f, false);

            
        }
        m_labelStyle.normal.textColor = Color.red;
    }

    void OnCollisionStay(Collision colInfo)
    {
        m_contact = true;
        foreach (ContactPoint p in colInfo.contacts)
        {
            if (p.thisCollider.name == m_name)
            {
                m_normal = p.normal;
                m_relvel = colInfo.relativeVelocity;

                
            }
            
            m_contactForce += p.normal;
            Debug.DrawRay(p.point, p.normal * colInfo.relativeVelocity.magnitude, Color.yellow, 0.5f, false);
        }
        m_labelStyle.normal.textColor = Color.red;
    }

    void OnCollisionExit(Collision colInfo)
    {
        m_normal = new Vector3();
        m_relvel = new Vector3();
        m_contact = false;
        m_validContacts.Remove(colInfo.collider);
        m_labelStyle.normal.textColor = Color.white;
    }

    #endregion

    #region Gets/Sets

    public float[] ArmJointsActual
    {
        get
        {
            float[] a = new float[7];
            for (int i = 0; i < 7; i++)
                a[i] = m_actVal[i];
            return a;
        }
    }

    public float[] ArmJointsCommanded
    {
        get
        {
            float[] a = new float[7];
            for (int i = 0; i < 7; i++)
                a[i] = m_cmdVal[i];
            return a;
        }
    }

    public float[] FingerJointsActual
    {
        get
        {
            float[] a = new float[4];
            for (int i = 0; i < 4; i++)
                a[i] = m_actVal[i+7];
            return a;
        }
    }

    public float[] FingerJointsCommanded
    {
        get
        {
            float[] a = new float[4];
            for (int i = 0; i < 4; i++)
                a[i] = m_cmdVal[i+7];
            return a;
        }
    }

    public float[] CollisionInfo
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
    }

    #endregion
}
