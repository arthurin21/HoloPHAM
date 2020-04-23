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

public class PaddleJoystickControl : MonoBehaviour 
{
    private Vector3 m_movement;
    //private Transform m_transform;
    public float m_speed = 1;
    public Transform m_ball;

    private Rigidbody m_paddleBody;
    private Rigidbody m_ballBody;

	void Start()
    {
        m_movement = new Vector3();
        //m_transform = transform;
        m_paddleBody = GetComponent<Rigidbody>();
        if (m_ball != null)
            m_ballBody = m_ball.GetComponent<Rigidbody>();
        
        //REMOVE WARNING
        Debug.Log("" + m_ballBody.mass);
        //REMOVE WARNING

	}//function - Start
	
	void Update()
    {
        //m_movement.y = Input.GetAxis("Vertical");
        //m_movement.x = Input.GetAxis("Vertical2");
        m_movement.x = -Input.GetAxis("Vertical");
        m_movement.z = Input.GetAxis("Horizontal");
        //m_transform.Translate(m_movement * m_speed);
        m_paddleBody.MovePosition(m_paddleBody.position + m_movement * m_speed);

        Vector3 angles = m_paddleBody.rotation.eulerAngles;

        float inZ = -Input.GetAxis("Vertical2");
        float inY = Input.GetAxis("Horizontal2");

        float newZRot;
        if (inZ == 0)
            newZRot = 0;
        else
            newZRot = angles.z + inZ * m_speed;

        float newYRot;
        if (inY == 0)
            newYRot = 0;
        else
            newYRot = angles.y + inY * m_speed;

        m_paddleBody.MoveRotation(Quaternion.Euler(0, newYRot, newZRot));
    }//function - Update

    //void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject == m_ball.gameObject)
    //    {
    //        Debug.Log(collision.relativeVelocity.magnitude);
    //        m_ballBody.AddForce(m_paddleBody.velocity * 50, ForceMode.Impulse);
    //    }
    //}//function - OnCollisionEnter

}//class - PaddleJoystickControl
