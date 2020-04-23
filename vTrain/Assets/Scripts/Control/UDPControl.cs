using System;
using System.Text;

using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using UnityEngine;

public class UDPControl : MonoBehaviour
{
    // constants
    private const int NUM_MPL_ARM_ANGLES = 7;
    private const int NUM_MPL_FINGER_ANGLES = 20;

    // communication variables
    Thread udpThread;
    UdpClient client;

    public string remoteIP = "127.0.0.1";
    public int remotePort = 9027;

    // vMPL variables
    private vMPLMovementArbiter arbiter = null;
    private float [] arm_angles = new float[NUM_MPL_ARM_ANGLES];
    private float [] finger_angles = new float[NUM_MPL_FINGER_ANGLES];
    
    // synchronization variables
    bool update_joints = false;

    // Start is called before the first frame update
    void Start()
    {
        // movement
        arbiter = GameObject.Find( "vMPLMovementArbiter" ).GetComponent<vMPLMovementArbiter>();
    
        // multithreading
        udpThread = new Thread( new ThreadStart( Communicate ) );
        udpThread.IsBackground = true;
        udpThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if ( update_joints ) {
            arbiter.SetRightUpperArmAngles( arm_angles );
            arbiter.SetRightFingerAngles( finger_angles );
        }
    }

    void OnDispose() {
        if ( udpThread.IsAlive == true ) {
            udpThread.Abort();
            client.Close();
        }
    }

    private void Communicate() {
        client = new UdpClient( remotePort );
        while( true ) {
            try {
                // get any messages
                IPEndPoint remoteEndPoint = new IPEndPoint( IPAddress.Any, 0 );
                Byte[] receiveBytes = client.Receive( ref remoteEndPoint );

                // handle messages
                byte cmd = receiveBytes[0];
                switch( cmd ) {
                    case 0x6a: // (j)oint angles
                        // Debug.Log( "Setting Joint Angles..." );
                        int idx = 1;
                        for ( int i = 0; i < NUM_MPL_ARM_ANGLES; i++ ) {
                            arm_angles[i] = System.BitConverter.ToSingle( receiveBytes, idx );
                            idx = idx + 4;
                        }
                        for ( int i = 0; i < NUM_MPL_FINGER_ANGLES; i++ ) {
                            finger_angles[i] = System.BitConverter.ToSingle( receiveBytes, idx );
                            idx = idx + 4;
                        }
                        update_joints = true;
                        break;
                    default:
                        break;
                }
            } catch ( System.Exception ex ) {
                Debug.LogException( ex );
            }
        }
    }
}
