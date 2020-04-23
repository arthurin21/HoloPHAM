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

public class ProximalContact : MonoBehaviour 
{
    private Transform m_transform;
    private float m_zOffset;
    private float m_tipYOffset;
    private float m_centerZ;
    private bool m_haveContact;
    private bool m_lostContact;
    private int m_lostContactCounter;

    private const int m_CONTACT_COUNTER_START = 300;

    public bool HaveContact
    {
        get { return m_haveContact; }
    }

    public bool LostContact
    {
        get { return m_lostContact; }
    }

#if UNITY_EDITOR
    [field: System.NonSerialized]
    public bool m_gotContact;
#endif
    
    void Start() 
    {
        m_transform = transform;
        CapsuleCollider cap = GetComponent<Collider>() as CapsuleCollider;
        m_zOffset = cap.center.z - cap.radius * 0.75f;
        m_tipYOffset = cap.center.y - cap.height / 2.0f + cap.radius / 2.0f;
        m_centerZ = cap.center.z;
    }

    void FixedUpdate()
    {
//        if (Input.GetMouseButton(0))
//        {
//            m_haveContact = true;
//#if UNITY_EDITOR
//            m_gotContact = true;
//#endif
//        }
//        else
//        {
//            if (m_haveContact)
//            {
//                m_haveContact = false;
//#if UNITY_EDITOR
//                m_gotContact = false;
//#endif
//                m_lostContact = true;
//                m_lostContactCounter = m_CONTACT_COUNTER_START;
//            }
//        }

        if (m_lostContact)
        {
            m_lostContactCounter--;
            if (m_lostContactCounter <= 0)
            {
                m_lostContact = false;
            }
        }
    }
	
	void OnCollisionEnter(Collision colInfo) 
    {
        foreach (ContactPoint p in colInfo.contacts)
        {
            Vector3 localPoint = m_transform.InverseTransformPoint(p.point);
            if (localPoint.z < m_zOffset || localPoint.y < m_tipYOffset &&
                localPoint.z < m_centerZ)
            {
                m_haveContact = true;
#if UNITY_EDITOR
                m_gotContact = true;
#endif
                m_lostContact = false;
                break;
            }
        }
	}

    void OnCollisionExit(Collision colInfo)
    {
        m_haveContact = false;
#if UNITY_EDITOR
        m_gotContact = false;
#endif
        m_lostContact = true;
        m_lostContactCounter = m_CONTACT_COUNTER_START;
    }
}
