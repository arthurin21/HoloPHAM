using System;

using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Threading;

using UnityEngine;

public class FingerTipForceSensors : MonoBehaviour
{
    // constants
    private const int NUM_FINGER_SENSORS = 5;

    // communication variables
    Thread udpThread;
    UdpClient client;

    public string remoteIP = "127.0.0.1";
    public int remotePort = 9028;

    // vMPL variables
    private GameObject [] fingers = new GameObject[NUM_FINGER_SENSORS];
    private ForceMeasurement [] sensors = new ForceMeasurement[NUM_FINGER_SENSORS];
    
    private float [] force_values = new float[NUM_FINGER_SENSORS];
    private byte [] force_bytes = new byte[4*NUM_FINGER_SENSORS];

    // synchronization variables
    private bool update = false;

    // Start is called before the first frame update
    void Start()
    {
        // find finger tips and add force measurements
        if  ( GameObject.Find( "rPalm" ) != null ) {
            fingers[0] = GameObject.Find( "rThDistal" );
            fingers[1] = GameObject.Find( "rIndDistal" );
            fingers[2] = GameObject.Find( "rMidDistal" );
            fingers[3] = GameObject.Find( "rRingDistal" );
            fingers[4] = GameObject.Find( "rLittleDistal" );

            for ( int i = 0; i < NUM_FINGER_SENSORS; i++ ) {
                sensors[i] = (ForceMeasurement) fingers[i].AddComponent<ForceMeasurement>();
            }       
        } else {
            fingers[0] = GameObject.Find( "lThDistal" );
            fingers[1] = GameObject.Find( "lIndDistal" );
            fingers[2] = GameObject.Find( "lMidDistal" );
            fingers[3] = GameObject.Find( "lRingDistal" );
            fingers[4] = GameObject.Find( "lLittleDistal" );

            for ( int i = 0; i < NUM_FINGER_SENSORS; i++ ) {
                sensors[i] = (ForceMeasurement) fingers[i].AddComponent<ForceMeasurement>();
            }
        }

        // multithreading
        udpThread = new Thread( new ThreadStart( Communicate ) );
        udpThread.IsBackground = true;
        udpThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if ( !update ) {
            for ( int i = 0; i < NUM_FINGER_SENSORS; i++ ) {
                force_values[i] = sensors[i].GetForce();
            }
            // Debug.Log( string.Format( "Force Measurements: {0}, {1}, {2}, {3}, {4}", 
            //                     force_values[0].ToString("F1"), force_values[1].ToString("F1"), 
            //                     force_values[2].ToString("F1"), force_values[3].ToString("F1"), 
            //                     force_values[4].ToString("F1") ) );
            Buffer.BlockCopy( force_values, 0, force_bytes, 0, force_bytes.Length );
            update = true;
        }
        
        // bool new_data = false;
        // for ( int i = 0; i < NUM_FINGER_SENSORS; i++ ) {
        //     if ( sensors[i].GetContact() ) {
        //         new_data = true;
        //         force_values[i] = sensors[i].GetForce();
        //     }
        // } 
        // if ( new_data ) {
        //     Debug.Log( string.Format( "Force Measurements: {0}, {1}, {2}, {3}, {4}", 
        //                         force_values[0].ToString("F1"), force_values[1].ToString("F1"), 
        //                         force_values[2].ToString("F1"), force_values[3].ToString("F1"), 
        //                         force_values[4].ToString("F1") ) );
        //     Buffer.BlockCopy( force_values, 0, force_bytes, 0, force_bytes.Length );
        //     update = true;
        // }
    }


    // Clean up resources on exit
    void OnDispose() {
        if ( udpThread.IsAlive == true ) {
            udpThread.Abort();
            client.Close();
        }
    }

    private void Communicate() {
        // connect to remote host
        IPEndPoint remoteEndPoint = new IPEndPoint( IPAddress.Parse( remoteIP ), remotePort );
        client = new UdpClient();
        // client.Client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );
        client.Client.Connect( remoteIP, remotePort );

        // broadcast sensor data
        while ( true ) {
            try {
                if ( update ) {
                    // Debug.Log( string.Format( "Force Bytes: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}", 
                    //                             force_bytes[0], force_bytes[1], force_bytes[2], force_bytes[3],
                    //                             force_bytes[4], force_bytes[5], force_bytes[6], force_bytes[7],
                    //                             force_bytes[8], force_bytes[9], force_bytes[10], force_bytes[11],
                    //                             force_bytes[12], force_bytes[13], force_bytes[14], force_bytes[15],
                    //                             force_bytes[16], force_bytes[17], force_bytes[18], force_bytes[19] ) );
                    client.Send( force_bytes, force_bytes.Length, remoteEndPoint );
                    update = false;
                }
            } catch ( System.Exception ex ) {
                Debug.LogException( ex );
            }
            Thread.Sleep( 10 ); // 100 Hz
        }
    }

    public float [] GetForceValues() {
        return force_values;
    }
}
