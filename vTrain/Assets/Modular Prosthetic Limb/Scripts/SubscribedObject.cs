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
/// This script is dynamically added to a GameObject by the WorldInterface.
/// It does book keeping for the WorldInterface and lets it know when the
/// object's transform changes.  The WorldInterface sends a report to the
/// host only when a subscribed object's transform changes.
/// </summary>
public class SubscribedObject : MonoBehaviour
{
    protected Vector3 m_lastPos;
    protected Vector3 m_lastEulers;
    protected Transform m_transform;

    private bool m_changed;

    public bool Changed
    {
        get { return m_changed; }
    }

	void Start()
    {
        m_transform = transform;
        m_lastPos = m_transform.position;
        m_lastEulers = m_transform.eulerAngles;
        m_changed = false;
	}

    /// <summary>
    /// Check for changes before each physics time step.
    /// </summary>
    void FixedUpdate()
    {
        if (m_lastPos == m_transform.position &&
            m_lastEulers == m_transform.eulerAngles)
        {
            m_changed = false;
        }
        else
        {
            m_changed = true;
            m_lastPos = m_transform.position;
            m_lastEulers = m_transform.eulerAngles;
        }
    }
}
