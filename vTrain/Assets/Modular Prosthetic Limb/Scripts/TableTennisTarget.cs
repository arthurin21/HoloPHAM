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

public class TableTennisTarget : MonoBehaviour 
{
    public Transform m_ballTransform;
    public float minZ = -50;
    public float maxZ = 50;
    public float minY = -30;
    public float maxY = 30;

    private GameObject m_ball;

	void Start() 
    {
        if (m_ballTransform != null)
            m_ball = m_ballTransform.gameObject;
	}
	
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == m_ball)
        {
            float x = transform.position.x;
            transform.position = new Vector3(
                x, Random.Range(minY, maxY), Random.Range(minZ, maxZ));
        }
    }
}
