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

public class TableTennisAdjustments : MonoBehaviour 
{
    public Transform m_rMPL;
    public Transform m_paddle;
    public Rigidbody m_palmBody;
    public float m_speed = 10.0f;

    private Vector3 m_paddleInertiaTensor;

	void Start() 
    {
        m_paddleInertiaTensor = m_palmBody.inertiaTensor;
	}
	
	void Update() 
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                m_rMPL.Rotate(0, -m_speed * 2 *Time.deltaTime, 0, Space.World);
            }
            else
                m_rMPL.Translate(new Vector3(0, 0, -m_speed * Time.deltaTime), Space.World);
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                m_rMPL.Rotate(0, m_speed * 2 * Time.deltaTime, 0, Space.World);
            }
            else
                m_rMPL.Translate(new Vector3(0, 0, m_speed * Time.deltaTime), Space.World);
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            m_rMPL.Translate(new Vector3(0, m_speed * Time.deltaTime, 0), Space.World);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            m_rMPL.Translate(new Vector3(0, -m_speed * Time.deltaTime, 0), Space.World);
        }
        else if (Input.GetKey(KeyCode.LeftBracket))
        {
            Vector3 scale = m_paddle.transform.localScale;
            scale.x -= m_speed * Time.deltaTime;
            scale.y -= m_speed * Time.deltaTime;
            m_paddle.transform.localScale = scale;
            m_palmBody.inertiaTensor = m_paddleInertiaTensor;
        }
        else if (Input.GetKey(KeyCode.RightBracket))
        {
            Vector3 scale = m_paddle.transform.localScale;
            scale.x += m_speed * Time.deltaTime;
            scale.y += m_speed * Time.deltaTime;
            m_paddle.transform.localScale = scale;
            m_palmBody.inertiaTensor = m_paddleInertiaTensor;
        }
    }
}
