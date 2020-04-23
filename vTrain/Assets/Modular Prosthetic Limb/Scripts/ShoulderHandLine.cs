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

public class ShoulderHandLine : MonoBehaviour 
{
    public GameObject m_palm;
    private GameObject m_sphere;

    void Start()
    {
        m_sphere = gameObject;
    }

	// Update is called once per frame
	void Update()
    {
        LineRenderer rend = m_sphere.GetComponent<LineRenderer>();
        rend.SetPosition(0, m_sphere.transform.position);
        rend.SetPosition(1, m_palm.transform.position);
	}
}
