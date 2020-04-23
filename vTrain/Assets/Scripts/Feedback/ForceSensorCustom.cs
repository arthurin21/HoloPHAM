using System;
using UnityEngine.UI;
using UnityEngine;

using System.Net;
using System.Net.Sockets;
// using System.Threading;

public class ForceSensorCustom : MonoBehaviour {
    private const float MAGNITUDE_SCALAR = 0.5f;

    public GraspingLogic GraspLogic;

    // sensor variables
    // private Vector3 force = Vector3.zero;
    private float force_magnitude = 0.0f;

    // streaming variables
    private string rhost = "127.0.0.1";
    private int rport = 52001;

    IPEndPoint remoteEndPoint;
    UdpClient client;

    public Text hud;

	// Use this for initialization
	void Start () {
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(rhost), rport);
        client = new UdpClient();
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Connect(rhost, rport);
    }
	
	// Update is called once per frame
	void Update () {
        if ( GraspLogic.Grasping )
        {
            force_magnitude = Mathf.Min(1.0f, 25.0f * force_magnitude);
        } else
        {
            force_magnitude = Mathf.Max(0.0f, 0.1f * force_magnitude);
        }
        // force_magnitude = 1.0f - force_magnitude;
        byte[] data_packet = BitConverter.GetBytes(force_magnitude);
        int num_bytes = client.Send(data_packet, sizeof(float), remoteEndPoint);
        hud.text = string.Format( "ForceSensor: {0}", force_magnitude.ToString("F3") );
        // Debug.Log(string.Format("Sending over UDP: {0} ({1} bytes)", force_magnitude, num_bytes));
    }

    private void OnCollisionEnter(Collision collision) {
        // force = collision.impactForceSum;
        force_magnitude = collision.impactForceSum.magnitude / MAGNITUDE_SCALAR;
    }

    private void OnCollisionStay(Collision collision)
    {
        // force = collision.impactForceSum;
        force_magnitude = collision.impactForceSum.magnitude / MAGNITUDE_SCALAR;
    }

    private void OnApplicationQuit()
    {
        client.Close();
    }
}
