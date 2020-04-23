using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceMeasurement : MonoBehaviour
{
    private float force = 0.0f;
    private bool contact = false;

    private Color32 init_color;
    private Color32 contact_color;

    // Start is called before the first frame update
    void Start() {
        // save initial color of parent object
        init_color = gameObject.GetComponent<Renderer>().material.color;
        contact_color = Color.yellow;
    }

    // Update is called once per frame
    void Update() {
        
    }

    // Collision Logic
    void OnCollisionEnter( Collision collision ) {
        // Debug.Log("ENTERED COLLISION...");
        contact = true;
        gameObject.GetComponent<Renderer>().material.color = contact_color;
    }

    void OnCollisionStay( Collision collision ) {
        // Debug.Log("IN COLLISION...");
        force = ( collision.impulse / Time.fixedDeltaTime ).magnitude; 
    }

    void OnCollisionExit( Collision collision ) {
        // Debug.Log("EXITED COLLISION...");
        contact = false;
        force = 0.0f;
        gameObject.GetComponent<Renderer>().material.color = init_color;
    }

    // Getters
    public bool GetContact() {
        return contact;
    }

    public float GetForce() {
        return force;
    }
}
