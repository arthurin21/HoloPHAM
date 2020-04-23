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

public class TableTennisBall : MonoBehaviour 
{
    private Transform m_transform;
    private Rigidbody m_body;
    private Vector3 m_originalPos;
    private AudioSource m_audio;

    public float m_maxVelocity = 300;
    public float m_magnitude;

    //public Transform m_table;
    //private Rigidbody m_tableBody;

    IEnumerator CheckStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.5f);
            if (m_body.velocity.magnitude < 0.1f || m_body.position.y < -100)
            {
                // Reset ball.
                m_body.position = new Vector3(m_originalPos.x, 
                    m_originalPos.y + Random.Range(-5, 5), 
                    m_originalPos.z + Random.Range(-15, 15));
                m_body.velocity = new Vector3();
                m_body.isKinematic = true;
                m_transform.position = m_body.position;

                yield return new WaitForSeconds(2);
                m_body.isKinematic = false;
                //m_body.AddForce(Random.Range(30f, 50f), 0, 0, ForceMode.Impulse);
                m_body.AddForce(Random.Range(10f, 20f), 0, 0, ForceMode.Impulse);
            }
        }
    }

	void Start()
    {
        m_transform = gameObject.transform;
        m_body = GetComponent<Rigidbody>();
        m_originalPos = m_body.position;
        m_audio = GetComponent<AudioSource>();
        StartCoroutine(CheckStatus());
        Physics.gravity = new Vector3(0, -200f, 0);
        //if (m_table != null)
        //    m_tableBody = m_table.rigidbody;
	}

    void FixedUpdate()
    {
        if (m_body.velocity.magnitude > m_maxVelocity)
        {
            Vector3 vel = Vector3.Normalize(m_body.velocity);
            vel.Scale(new Vector3(m_maxVelocity, m_maxVelocity, m_maxVelocity));
            m_body.velocity = vel;
            m_magnitude = vel.magnitude;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        m_audio.Play();
        //if (collision.rigidbody == m_tableBody && m_tableBody != null)
        //{

        //}
    }
}
