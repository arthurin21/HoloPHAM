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
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using VIEUtil;

#if UNITY_EDITOR
using System.Net;
using System.Net.Sockets;
#endif

#if !UNITY_EDITOR
using Windows.Networking.Sockets;
#endif

/// <summary>
/// This class implements the World Interface ICD for the virtual world.  The
/// Step() method is the main worker of this class.  Each call to Step() 
/// processes one command from the host if commands are waiting. 
/// </summary>
public class WorldInterface
{

    static private WorldInterface ms_instance;
	
	protected System.Diagnostics.Stopwatch m_Stopwatch;
	protected long m_StartTicks;	

    protected bool m_paused;
    public bool Paused 
    { 
        get { return m_paused; } 
		set { PauseWorld(value); }
    }
	
	protected bool resetFlag;
	public bool WasReset
	{
		get { return resetFlag; }
	}
	
	private byte m_busyCmd;

    /// <summary>
    /// This is called by ResetHandler when a reset completes.
    /// </summary>
    internal void ResetComplete()
    {
        // No longer busy resetting.
        m_busyCmd = (byte)CmdMessageIdE.UNUSED;
    }

	protected byte m_busyFeatureType;
		
	// public Dictionary<string, DisplayMessage> m_DisplayMessages;	

    protected Dictionary<string, SubscribedObject> m_subscribedObjects;
    /// <summary>
    /// This dictionary holds all objects that may be controlled via the
    /// WorldInterface.  
    /// These are all the game objects that have the WorldObject script 
    /// attached.  Unfortunately, game objects cannot have multiple tags or
    /// belong to multiple layers, so the WorldObject script is used to 
    /// indicate that an object is addressible from the world interface.
    /// </summary>
    protected Dictionary<string, GameObject> m_allWorldObjects;

    public enum GraspState
    {
        RELEASED,
        ESTABLISHED
    }

    protected Queue<GraspState> m_graspQueue;

    #region Socket and UDP related members

    /// <summary>
    /// For commands from the host.
    /// </summary>
    protected const int m_recvPort = 26000;

    /// <summary>
    /// World state is broadcast on this port.
    /// </summary>
    protected const int m_streamPort = 26001;

    /// <summary>
    /// Response messages are broadcast on this port.
    /// </summary>
    protected const int m_respPort = 26002;

#if !UNITY_EDITOR
    DatagramSocket m_recvSock;
#endif
#if UNITY_EDITOR
    protected Socket m_recvSock;
    protected UdpClient m_streamUdp;
    protected UdpClient m_respUdp;
#endif
    /// <summary>
    /// Use this buffer for storing commands received from the host.
    /// Commands are asynchronously enqueued, 
    /// Everything is dequeued during the step function
    /// </summary>    
    private byte[] m_recvBuffer;
    private Queue<byte[]> m_recvBufferQueue = new Queue<byte[]>();

    /// <summary>
    /// Use this buffer for building world state messages.
    /// </summary>
    private byte[] m_streamBuffer;
	private int m_streamPtr;
	private const int STR_PTR_START = 12;
	private byte m_numWorldStateMessages;

    /// <summary>
    /// Use this buffer for building messages (mainly Nack messages) sent via
    /// the response port.
    /// </summary>
    private byte[] m_respBuffer;

    private byte[] m_graspBuffer;

#if UNITY_EDITOR
    /// <summary>
    /// This maps back to the host that issues commands to the virtual world.
    /// </summary>
    protected IPEndPoint m_cmdHost;

    /// <summary>
    /// This is actually the same instance that m_cmdHost points to.  
    /// Socket.ReceiveFrom() requires a reference to an EndPoint instance, so
    /// this avoids bothersome type casting.
    /// </summary>
    protected EndPoint m_interfaceEndPt;

    private IPEndPoint m_streamBroadcast;
    private IPEndPoint m_respBroadcast;
#endif

#endregion

    #region World Interface ICD enumerations
    public enum CmdMessageIdE : byte
    {
        UNUSED,
        UPDATE_WORLD_STATE,
        REPORT_OBJECT_IDS,
		REPORT_OBJECT_STATE
    }
	
	public enum MplStreamingMessageIdType : byte
	{
   		WORLD_STATE = 200
	}


    public enum FeatureTypeE : byte
    {
        TRANSFORM,
        POSITION,
        ROTATION,
        SCALE,
        COLOR,
        TRANSPARENCY,
        VISIBILITY,
        PHYSICS,
        COLLIDEABLE,
        ATTACH_TO,
        DETACH_FROM,
        SUBSCRIBE,
        SUBSCRIBE_COLLISION_BEGIN,
        SUBSCRIBE_COLLISION_END,
        //
        // System level commands.
        //
        START,
        PAUSE,
        TERMINATE,
        RESET,
        DISPLAY_MESSAGE,
		DESTROY_MESSAGE
    }
	
	public enum ReportType : byte
	{
	   TRANSFORM = 1,
	   COLLISION_BEGIN,
	   COLLISION_END,
	   GRASP_ESTABLISHED,
	   GRASP_RELEASED
	}	

    public enum ResponseMessageIdE : byte
    {
        NACK = 100,
        REPORT_OBJECT_IDS = CmdMessageIdE.REPORT_OBJECT_IDS,
		REPORT_OBJECT_STATE = CmdMessageIdE.REPORT_OBJECT_STATE
    }

    public enum NackTypeE : byte
    {
        MSG_FRAGMENT,       // Message less than 4 bytes.
        EXE_ERROR,          // Currently unused.
        BUSY,               // Currently unused while receive port isn't in
                            // its own thread.
        BAD_CHECKSUM,
        BAD_MSG_ID,
        BAD_OBJ_ID,
        BAD_FEATURE_TYPE
    }
    #endregion

    protected const string m_VIESYS_ID = "VIESYS";

    /// <summary>
    /// Cached copy of VIESYS game object.
    /// </summary>
    protected GameObject m_viesysObj;

    static public WorldInterface Instance()
    {
        if (ms_instance == null)
            ms_instance = new WorldInterface();
		
		// If ms_instance exists but an Application.LoadLevel command has caused m_allWorldObjects to be empty then populate it
		// if ( ms_instance.m_allWorldObjects.Count == 0 )
		//	ms_instance.AddWorldObjects();

        if (ms_instance.m_viesysObj == null)
            ms_instance.m_viesysObj = GameObject.Find(m_VIESYS_ID);
		
        return ms_instance;
    }

    protected WorldInterface()
    {
        // Set up command receive socket.	
#if UNITY_EDITOR
        IPEndPoint ipEndPt = new IPEndPoint(IPAddress.Any, m_recvPort);

        if ( m_recvSock == null )
        	m_recvSock = new Socket(AddressFamily.InterNetwork,
            	SocketType.Dgram, ProtocolType.Udp);
		if( !m_recvSock.IsBound )
        	m_recvSock.Bind(ipEndPt);

        m_cmdHost = new IPEndPoint(IPAddress.Any, 0);
        m_interfaceEndPt = (EndPoint)m_cmdHost;

        // Set up UDP broadcasts for streaming and response messages.
        m_streamUdp = new UdpClient();
        m_respUdp = new UdpClient();
        m_streamBroadcast = new IPEndPoint(IPAddress.Broadcast, m_streamPort);
        m_respBroadcast = new IPEndPoint(IPAddress.Broadcast, m_respPort);
#endif
        m_recvBuffer = new byte[4096];
        m_respBuffer = new byte[2048];
        m_streamBuffer = new byte[4096];
        m_graspBuffer = new byte[1];

        // [uint16(length) uint8(WORLD_STATE) int64(timestamp) 
        //  uint8(numMessages) messages uint8(chksum)];
        m_streamPtr = STR_PTR_START;
		m_numWorldStateMessages = 0;

        m_subscribedObjects = new Dictionary<string, SubscribedObject>();
        m_allWorldObjects = new Dictionary<string, GameObject>();
        m_graspQueue = new Queue<GraspState>();

		// m_DisplayMessages = new Dictionary<string, DisplayMessage>();
		PauseWorld(true);	// start with the world paused		
        AddWorldObjects();
		
		m_StartTicks = DateTime.Now.Ticks;	// Get the number of ticks at the start of the simulation
		m_Stopwatch = new System.Diagnostics.Stopwatch();
		m_Stopwatch.Start();	
        	
//    }

#if !UNITY_EDITOR
    //async void Start()
    //{
        Debug.Log("Waiting for a connection...");

        m_recvSock = new DatagramSocket();
        m_recvSock.MessageReceived += Socket_MessageReceived;

        try
        {
            //await m_recvSock.BindEndpointAsync(null, "26000");
            bindEndpointWrapper(26000);
            //await m_recvSock.BindEndpointAsync(null, m_recvPort.ToString());
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            Debug.Log(SocketError.GetStatus(e.HResult).ToString());
            return;
        }

        Debug.Log("exit start");
#endif
    }

#if !UNITY_EDITOR
    private async void bindEndpointWrapper(int port)
    {
        await m_recvSock.BindEndpointAsync(null, "26000");        
    }

    private void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
        Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
    {

        //Read the message that was received from the UDP echo client.
        Stream streamIn = args.GetDataStream().AsStreamForRead();

        byte[] buffer = new byte[4096];
        streamIn.Read(buffer, 0, buffer.Length);

        m_recvBufferQueue.Enqueue(buffer);               

        //SendStreamingMsg();
    }
#endif

    /// <summary>
    /// Clears all world objects from the WorldInterface as well as
    /// object subscription info.
    /// </summary>
    public void ClearWorldObjects()
    {
        ClearSubscribers();				
        m_allWorldObjects.Clear();
    }

    /// <summary>
    /// Removes all object subscriptions.
    /// </summary>
    public void ClearSubscribers()
    {		
        m_subscribedObjects.Clear();		
    }

    // TJL ms_allWorldObjects changed to m_allWorldObjects in the comments
    /// <summary>
    /// Iterates through all GameObjects and adds those containing the
    /// WorldObject script to m_allWorldObjects.
    /// </summary>
    public void AddWorldObjects()
    {
        UnityEngine.Object[] objs =
            GameObject.FindObjectsOfType(typeof(GameObject));

        foreach (UnityEngine.Object o in objs)
        {
            GameObject gObj = o as GameObject;            
            WorldObject wObjScript = gObj.GetComponent<WorldObject>();
            if (gObj != null &&
                (wObjScript = gObj.GetComponent<WorldObject>()) != null)
            {
                if (!string.IsNullOrEmpty(wObjScript.m_id))
                {
                    try
                    {
                        m_allWorldObjects.Add(wObjScript.m_id, gObj);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException(
                            "WorldInterface: found more than one world object with id: " +
                            wObjScript.m_id, ex);
                    }
                }
                else
                {
                    // Assign a random id.
                    bool done = false;
                    while (!done)
                    {
                        string rndId = VIEUtil.RandomId.Get();
                        try
                        {
                            m_allWorldObjects.Add(rndId, gObj);
                            done = true;
                        }
                        catch (ArgumentException)
                        {
                            // Name already exists, try again.
                        }
                    }
                } // end else
            } // end if(gObj != null ...
        } // end foreach
	}


    /// <summary>
    /// This is the main method of the WorldInterface.  It should be called
    /// at the physics step rate.
    /// </summary>
    public void Step()
    {

        byte[] buffer;
        while (m_recvBufferQueue.Count > 0)
        {
            buffer = m_recvBufferQueue.Dequeue();
            ReadUdpMsg(buffer, buffer.Length);
        }
            
        
#if UNITY_EDITOR
        // Read port.
        if (m_recvSock.Available > 0)
        {
            try
            {
                int len = m_recvSock.ReceiveFrom(
                    m_recvBuffer, ref m_interfaceEndPt);
				if( m_busyCmd == (byte)CmdMessageIdE.UNUSED )
                	ReadUdpMsg(m_recvBuffer, len);                
				else
				{
					// Busy Nack
					byte[] nackBuf = new byte[12];
					
	                GenerateBusyNack(nackBuf, m_recvBuffer[2], m_busyCmd, m_busyFeatureType, ExtractIpAddress(m_cmdHost.Address));
					m_respUdp.Send(nackBuf, 12, m_respBroadcast);
				}                      
            }
            catch (SocketException ex)
            {
                Debug.LogError(
                    "Socket error reading message via World Interface: " +
                    ex.Message);
            }
        } // end if (ms_recvSock.Available > 0)		

        SendStreamingMsg();
#endif		
    }


    public void SendStreamingMsg()
    {
        try
        {
            // No need to send streaming messages while paused.
            if (!m_paused)
            {
                // Send transforms for all subscribed objects that have changed.
                // Collisions are already in the buffer at this point.			
                foreach (string key in m_subscribedObjects.Keys)
                {
                    SubscribedObject sub = m_subscribedObjects[key];

                    if (sub.Changed)
                    {
                        byte[] transformBuffer = new byte[6 * sizeof(float) + 2 + key.Length];

                        // Report Type TRANSFORM and length
                        transformBuffer[0] = Convert.ToByte(ReportType.TRANSFORM);
                        transformBuffer[1] = Convert.ToByte(key.Length);
                        for (int i = 0; i < key.Length; i++)
                            transformBuffer[2 + i] = Convert.ToByte(key[i]);
                        int ct = 2 + key.Length;
                        // Position with x negated to convert to right handed coordinate frame.
                        Array.Copy(BitConverter.GetBytes(-sub.transform.position.x), 0, transformBuffer, ct, sizeof(float));
                        Array.Copy(BitConverter.GetBytes(sub.transform.position.y), 0, transformBuffer, ct + sizeof(float), sizeof(float));
                        Array.Copy(BitConverter.GetBytes(sub.transform.position.z), 0, transformBuffer, ct + 2 * sizeof(float), sizeof(float));
                        // Rotation with y and z angles negated to convert to right handed coordinate frame.
                        Array.Copy(BitConverter.GetBytes(sub.transform.eulerAngles.x * Mathf.Deg2Rad), 0, transformBuffer, ct + 3 * sizeof(float), sizeof(float));
                        Array.Copy(BitConverter.GetBytes(-sub.transform.eulerAngles.y * Mathf.Deg2Rad), 0, transformBuffer, ct + 4 * sizeof(float), sizeof(float));
                        Array.Copy(BitConverter.GetBytes(-sub.transform.eulerAngles.z * Mathf.Deg2Rad), 0, transformBuffer, ct + 5 * sizeof(float), sizeof(float));

                        AppendWorldStateMessage(transformBuffer);
                    }
                }

                // Empty grasp queue.
                while (m_graspQueue.Count > 0)
                {
                    GraspState gState = m_graspQueue.Dequeue();
                    if (gState == GraspState.ESTABLISHED)
                    {
                        m_graspBuffer[0] = (byte)ReportType.GRASP_ESTABLISHED;
                        AppendWorldStateMessage(m_graspBuffer);
                    }
                    else if (gState == GraspState.RELEASED)
                    {
                        m_graspBuffer[0] = (byte)ReportType.GRASP_RELEASED;
                        AppendWorldStateMessage(m_graspBuffer);
                    }
                }

                if (m_streamPtr > STR_PTR_START)
                {
                    FinalizeWorldStateMessage();
                    // m_streamUdp.BeginSend(m_streamBuffer, m_streamPtr, m_streamBroadcast, StreamSendCallback, null);
#if UNITY_EDITOR
                    m_streamUdp.Send(m_streamBuffer, m_streamPtr + 1, m_streamBroadcast); // include checksum
#endif
                }

                // reset the stream pointer
                m_streamPtr = STR_PTR_START;
                m_numWorldStateMessages = 0;
            } // end if(!m_paused)
        }
#if UNITY_EDITOR
        catch (SocketException ex)
        {
            Debug.LogError(
                "Socket error streaming message via World Interface: " +
                ex.Message);
        }
#endif
#if !UNITY_EDITOR
        catch
        {
            Debug.LogError(
                "Socket error streaming message via World Interface");
        }    
#endif
    }

    /// <summary>
    /// Places the given grasp state in the queue. The queue will be emptied
    /// during execution of Step().  Grasps established and released messages
    /// will be generated as the queue is emptied.
    /// </summary>
    /// <param name="state"></param>
    public void QueueGraspState(GraspState state)
    {
        m_graspQueue.Enqueue(state);
    }

    /// <summary>
    /// Calculates the 8 bit checksum value for the given byte buffer.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="len">Number of bytes to read from the beginning of the
    /// buffer</param>
    /// <returns>Unsigned 8 bit checksum.</returns>
    protected byte ComputeChecksum(byte[] buffer, int len)
    {
        byte chksum = 0;
        for (int i = 0; i < len; i++)
            chksum += buffer[i];
        return chksum;
    }

    /// <summary>
    /// Reads the byte buffer containing the message received over UDP.
    /// Checks message header and checksum value before passing the message
    /// to the appropriate message handler.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="len"></param>
    private void ReadUdpMsg(byte[] buffer, int len)
    {		
		byte[] nackBuf;
		byte [] respBuf;
		
        if (len < 4)
        {
            // Invalid packet length, send Nack.
			// 3 bytes isn't enough to read a featureType, and we cannot differentiate a longer fragment from a different invalid packet type            
			nackBuf = new byte[11];
#if UNITY_EDITOR
            GenerateMsgFragmentNack(nackBuf,buffer[2],255, 
                ExtractIpAddress(m_cmdHost.Address));

			m_respUdp.Send(nackBuf, nackBuf.Length, m_respBroadcast);
#endif
            return;
        }

        // msgLen is the length of the entire packet except for the first
        // two bytes that contain the message length.
        ushort msgLen = BitConverter.ToUInt16(buffer, 0);

        // Checksum is the last byte of the packet. When computing the 
        // checksum, check the entire packet except for the included checksum.
        // This makes the number of bytes msgLen + 2 for the msgLen bytes - 1
        // for the checksum byte (msgLen + 1).
        byte chksum = ComputeChecksum(buffer, msgLen + 1);
 //       if (chksum != buffer[msgLen + 1])
 //       {
            nackBuf = new byte[11];
#if UNITY_EDITOR
            GenerateBadChecksumNack(nackBuf,
                buffer[msgLen + 1], chksum,
                ExtractIpAddress(m_cmdHost.Address));            
			m_respUdp.Send(nackBuf, 11, m_respBroadcast);
#endif
//            return;
//        }
		
		m_busyCmd = buffer[2];		
        switch ((CmdMessageIdE)buffer[2])
        {
            case CmdMessageIdE.UPDATE_WORLD_STATE:				
                ReadWorldStateUpdatePackets(buffer, 3, msgLen - 3);			
                break;
            case CmdMessageIdE.REPORT_OBJECT_IDS:			
				m_busyFeatureType = 255;
				int numBytes = 10; // 2 byte length, 4 byte IP address, 1 byte MplRspMessageIdType, 2 bytes numObjects, 1 byte Checksum
				foreach( string key in m_allWorldObjects.Keys )
				{
					numBytes += (key.Length + 1);
				}				
				respBuf = new byte[numBytes];
#if UNITY_EDITOR
                GenerateReportObjectIdsResponse(respBuf, numBytes, ExtractIpAddress(m_cmdHost.Address));
				m_respUdp.Send(respBuf, numBytes, m_respBroadcast);
#endif
                m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
			case CmdMessageIdE.REPORT_OBJECT_STATE:				
				string id;
            	int ind;
			
				m_busyFeatureType = 255;
            	PacketUtils.GetString(buffer, 3, out id, out ind); // 2 byte length, CmdMessageIdE, then id
				
				if (!HasObject(id))
            	{
	                nackBuf = new byte[384];
#if UNITY_EDITOR
                    int length = GenerateBadObjIdNack(nackBuf, buffer[2], id, ExtractIpAddress(m_cmdHost.Address));
					m_respUdp.Send(nackBuf, length, m_respBroadcast );
#endif
                }
	            else
            	{
#if UNITY_EDITOR
                    GenerateReportObjectStateResponse(out respBuf, id, ExtractIpAddress(m_cmdHost.Address));
					m_respUdp.Send(respBuf, respBuf.Length, m_respBroadcast);
#endif
                }
			
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
				break;
            case CmdMessageIdE.UNUSED:
            default:
                nackBuf = new byte[10];
#if UNITY_EDITOR
                GenerateBadMsgIdNack(nackBuf, buffer[2],
                    ExtractIpAddress(m_cmdHost.Address));
				m_respUdp.Send(nackBuf, 10, m_respBroadcast);
#endif
                m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
        }
    }

    /// <summary>
    /// Has the host subscribed to position/orientation updates from the
    /// given object?
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool HasSubscriber(string id)
    {
        return m_subscribedObjects.ContainsKey(id);
    }

    /// <summary>
    /// Does the given object exist?
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool HasObject(string id)
    {		
        return m_allWorldObjects.ContainsKey(id) || String.Compare(id, m_VIESYS_ID, true) == 0;
    }

    /// <summary>
    /// Handles the world state update message.  It is assumed that the 
    /// message in the buffer has already passed its checksum test, so the
    /// length passed in should not include the checksum byte.
    /// </summary>
    /// <param name="buffer">Byte buffer containing the world state update
    /// message.</param>
    /// <param name="start">The starting point of the message in the buffer.</param>
    /// <param name="len">The length of the message from the starting point.
    /// The length should not include the checksum byte.</param>
    public void ReadWorldStateUpdatePackets(byte[] buffer, int start, int len)
    {
        // The first byte contains the number of feature updates.
        byte numPackets = buffer[start];
        int ind = start + 1;
        byte numPacketsRead = 0;
        while (ind < len && numPacketsRead < numPackets)
        {
            string id;
            int newInd;
            PacketUtils.GetString(buffer, ind, out id, out newInd);
			
            if (!HasObject(id))
            {
                byte[] nackBuf = new byte[384];
#if UNITY_EDITOR
                int length = GenerateBadObjIdNack(nackBuf, (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id, ExtractIpAddress(m_cmdHost.Address));
				m_respUdp.Send(nackBuf, length, m_respBroadcast );
#endif               
            }            
            ind = ReadFeatureType(buffer, id, newInd);	// even if id is not a valid ID the bytes still need to be parsed
            
            numPacketsRead++;
			
			if (ind < len && numPacketsRead < numPackets)
			{
				// There is another feature value pair
				if (m_busyCmd == (byte)CmdMessageIdE.UNUSED)
					// If not busy from previous feature, set the busy state for the current feature
					m_busyCmd = (byte)CmdMessageIdE.UPDATE_WORLD_STATE;
				else					
				{
					// If busy from previous feature then 
					// Busy Nack
					byte [] nackBuf = new byte[12];
#if UNITY_EDITOR
                    GenerateBusyNack(nackBuf, m_recvBuffer[2], m_busyCmd, m_busyFeatureType, ExtractIpAddress(m_cmdHost.Address));
					m_respUdp.Send(nackBuf, 12, m_respBroadcast);
#endif
                    break;
				}
			}
        }

        // Guard against a 0 length feature update packet.
        if (numPackets == 0)
            m_busyCmd = (byte)CmdMessageIdE.UNUSED;

        if (numPackets != numPacketsRead)
        {
            Debug.LogWarning("WorldInterface: " + numPackets +
                " reported, but " + numPacketsRead + " packets read.");
        }
		
		if (ind < len )
		{
			Debug.LogWarning("WorldInterface: " + ind +
                " bytes read, but " + len + " byte length.");
		}		
    }

    /// <summary>
    /// Read the feature command from buffer.
    /// </summary>
    /// <param name="buffer">Byte buffer that holds the command.</param>
    /// <param name="id">The object the feature command will be 
    /// applied to.</param>
    /// <param name="ind">The feature command starts at this point in the 
    /// buffer.</param>
    /// <returns>Index of the next unread byte in the buffer.</returns>
    private int ReadFeatureType(byte[] buffer, string id, int ind)
    {        
		m_busyFeatureType = buffer[ind];
        switch ((FeatureTypeE)buffer[ind])
        {
            case FeatureTypeE.TRANSFORM:
                ind = ReadTransformPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.POSITION:
                ind = ReadPositionPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.ROTATION:
                ind = ReadRotationPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.SCALE:
                ind = ReadScalePacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.COLOR:
                ind = ReadColorPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.TRANSPARENCY:
                ind = ReadTransparencyPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.VISIBILITY:
                ind = ReadVisibilityPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.PHYSICS:
                ind = ReadPhysicsPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.COLLIDEABLE:
                ind = ReadCollideablePacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.ATTACH_TO:
                ind = ReadAttachToPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.DETACH_FROM:
                ind = ReadDetachFromPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.SUBSCRIBE:
                ind = ReadSubscribePacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.SUBSCRIBE_COLLISION_BEGIN:
                ind = ReadSubscribeCollisionBeginPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.SUBSCRIBE_COLLISION_END:
                ind = ReadSubscribeCollisionEndPacket(id, buffer, ind + 1);
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            ////////////////////////////////////////////
            // Begin system commands.
            ////////////////////////////////////////////			
            case FeatureTypeE.START:				
                if (String.Compare(id, m_VIESYS_ID, true) == 0)
					PauseWorld(false);
				else
				{
					Debug.LogWarning("Start command issued with id: " + id + ", not id: viesys.");
                    //byte[] nackBuf = new byte[384];
                    //int length = GenerateBadObjIdNack(nackBuf, (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id, ExtractIpAddress(m_cmdHost.Address));
                    //// m_respUdp.BeginSend(nackBuf, length, ResponseSendCallback, null);
                    //m_respUdp.Send(nackBuf, length, m_respBroadcast);
				}
				ind++;
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.PAUSE:
				if (String.Compare(id, m_VIESYS_ID, true) == 0)                
                	PauseWorld(true);				
				else
				{
					Debug.LogWarning("Pause command issued with id: " + id + ", not id: viesys.");				
                    //byte[] nackBuf = new byte[384];
                    //int length = GenerateBadObjIdNack(nackBuf, (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id, ExtractIpAddress(m_cmdHost.Address));
                    //// m_respUdp.BeginSend(nackBuf, length, ResponseSendCallback, null);
                    //m_respUdp.Send(nackBuf, length, m_respBroadcast);
				}
				ind++;
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.TERMINATE:
				if (String.Compare(id, m_VIESYS_ID, true) == 0)                
                	Application.Quit();	// This doesn't work in the editor				
				else
				{
					Debug.LogWarning("Terminate command issued with id: " + id + ", not id: viesys.");
                    //byte[] nackBuf = new byte[384];
                    //int length = GenerateBadObjIdNack(nackBuf, (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id, ExtractIpAddress(m_cmdHost.Address));
                    //// m_respUdp.BeginSend(nackBuf, length, ResponseSendCallback, null);
                    //m_respUdp.Send(nackBuf, length, m_respBroadcast);
				}
                ind++;
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
            case FeatureTypeE.RESET:
				if (String.Compare(id, m_VIESYS_ID, true) == 0)
                {
					if( m_viesysObj != null )
					{
						ResetHandler resetHandlerScript = m_viesysObj.GetComponent<ResetHandler>();
						if( resetHandlerScript != null )
						{	
							WorldInterfaceWrapper wifw = m_viesysObj.GetComponent<WorldInterfaceWrapper>();
							if( wifw != null )
								PauseWorld( wifw.m_startPaused );
							else
								Debug.LogError("WorldInterfaceWrapper not attached to VIESYS object");
							
							resetHandlerScript.BeginReset();
						}
						else							
							Debug.LogError("ResetHandler not attached to VIESYS object");										
					}
					else
						Debug.LogError("VIESYS object not found");
				}
				else
				{
					Debug.LogWarning("Reset command issued with id: " + id + ", not id: viesys.");
                    //byte[] nackBuf = new byte[384];
                    //int length = GenerateBadObjIdNack(nackBuf, (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id, ExtractIpAddress(m_cmdHost.Address));
                    //// m_respUdp.BeginSend(nackBuf, length, ResponseSendCallback, null);
                    //m_respUdp.Send(nackBuf, length, m_respBroadcast);
				}
                ind++;
				// Do not set m_busyCmd here for reset
                break;
			case FeatureTypeE.DISPLAY_MESSAGE:
				if (String.Compare(id, m_VIESYS_ID, true) == 0)
				{
					ind = ReadDisplayMessagePacket(id, buffer, ind + 1);
				}
				else	
				{
					Debug.LogWarning("Display message command issued with id: " + id + ", not id: viesys.");
                    //byte[] nackBuf = new byte[384];
                    //int length = GenerateBadObjIdNack(nackBuf, (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id, ExtractIpAddress(m_cmdHost.Address));	                
                    //m_respUdp.Send(nackBuf, length, m_respBroadcast);
				}
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
				break;
			case FeatureTypeE.DESTROY_MESSAGE:
				if (String.Compare(id, m_VIESYS_ID, true) == 0)
				{
					ind = ReadDestroyMessagePacket(id, buffer, ind + 1);
				}
				else	
				{
					Debug.LogWarning("Destroy message command issued with id: " + id + ", not id: viesys.");
                    //byte[] nackBuf = new byte[384];
                    //int length = GenerateBadObjIdNack(nackBuf, (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id, ExtractIpAddress(m_cmdHost.Address));	                
                    //m_respUdp.Send(nackBuf, length, m_respBroadcast);
				}
				m_busyCmd = (byte)CmdMessageIdE.UNUSED;
				break;
            default:
                //Debug.LogWarning(
                //    "WorldInterface: got unknown feature type value of " +
                //    m_recvBuffer[ind].ToString());
                byte[] nackBuf2 = new byte[10];
#if UNITY_EDITOR
                GenerateBadFeatureTypeNack(nackBuf2, buffer[ind], 
                    ExtractIpAddress(m_cmdHost.Address));
                // m_respUdp.BeginSend(nackBuf2, 10, ResponseSendCallback, null);
				m_respUdp.Send(nackBuf2, 10, m_respBroadcast);
#endif
                m_busyCmd = (byte)CmdMessageIdE.UNUSED;
                break;
        }

        return ind;
    }
        
    /// <summary>
    /// Handles the transform feature command. Positions and orients the object.
    /// Should not be used for objects under the control of the physics engine.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the transform 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadTransformPacket(string id, byte[] buffer, int ind)
    {
        ind = ReadPositionPacket(id, buffer, ind);
        ind = ReadRotationPacket(id, buffer, ind);
        return ind;
    }
        
    /// <summary>
    /// Handles the position feature command. Should not be used for objects under the control of the physics engine.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the position 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadPositionPacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;        
        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            // Negate x to switch back to Unity's left handed coordinate frame.
            try
            {
                float x = -BitConverter.ToSingle(buffer, ind);
                float y = BitConverter.ToSingle(buffer, ind + 4);
                float z = BitConverter.ToSingle(buffer, ind + 8);
                gObj.transform.position = new Vector3(x, y, z);
				gObj.transform.eulerAngles = new Vector3(0, 0, 0); // gh edit
				if (gObj.GetComponent<Rigidbody>() != null)
					gObj.GetComponent<Rigidbody>().useGravity = false;
                //if (gObj.rigidbody != null && !gObj.rigidbody.isKinematic && gObj.active && !m_paused)
                if (gObj.GetComponent<Rigidbody>() != null && !gObj.GetComponent<Rigidbody>().isKinematic && gObj.activeSelf && !m_paused)
                {
                    Debug.LogWarning("WorldInterface: attempted to update position when " +
                        id + " was under the control of the physics engine");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
		}
		else
		{			
			Debug.LogWarning("Invalid object ID reached in ReadPositionPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer, 
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,

                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 3*sizeof(float);
    }

    /// <summary>
    /// Handles the position feature command. Should not be used for objects under the control of the physics engine.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the rotation 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadRotationPacket(string id, byte[] buffer, int ind)
    {
		// Unity wants degrees, but the buffer contains radians
        GameObject gObj;        
        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            try
            {
                // Unity uses a left handed coordinate system with y being up,
                // so using yaw, pitch, and roll doesn't match up with
                // "normal" usage.  Instead, for clarity, specify which axis 
                // we are rotating about.  Also negate y and z rotations to switch
                // back to Unity's left handed coordinate frame.
                float xRot = BitConverter.ToSingle(buffer, ind) * Mathf.Rad2Deg;
                ind += sizeof(float);
                float yRot = -BitConverter.ToSingle(buffer, ind) * Mathf.Rad2Deg;
                ind += sizeof(float);
                float zRot = -BitConverter.ToSingle(buffer, ind) * Mathf.Rad2Deg;
                ind += sizeof(float);
                gObj.transform.eulerAngles = new Vector3(xRot, yRot, zRot);

                //if (gObj.rigidbody != null && !gObj.rigidbody.isKinematic && gObj.active && !m_paused)
                if (gObj.GetComponent<Rigidbody>() != null && !gObj.GetComponent<Rigidbody>().isKinematic && gObj.activeSelf && !m_paused)
                {
                    Debug.LogWarning("WorldInterface: attempted to update rotation when " +
                        id + " was under the control of the physics engine");
                }
            }
            catch(Exception ex)
            {
                Debug.LogError(ex.Message);
            }

		}
		else
		{			
			Debug.LogWarning("Invalid object ID reached in ReadRotationPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
            ind += 3 * sizeof(float);
        }
        return ind;
    }
        
    /// <summary>
    /// Handles the scale feature command. Scales the object along its three axes.
    /// Scaling is not recommended for objects using collision detection or under
    /// control of the physics engine.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the scale 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadScalePacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;
        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            try
            {
                float xScaleFactor = BitConverter.ToSingle(buffer, ind);
                float yScaleFactor = BitConverter.ToSingle(buffer, ind + 4);
                float zScaleFactor = BitConverter.ToSingle(buffer, ind + 8);
                gObj.transform.localScale = new Vector3(xScaleFactor, yScaleFactor, zScaleFactor);

                //if ((gObj.rigidbody != null && !gObj.rigidbody.isKinematic && gObj.active && !m_paused) || gObj.rigidbody.detectCollisions)
                if ((gObj.GetComponent<Rigidbody>() != null && !gObj.GetComponent<Rigidbody>().isKinematic && gObj.activeSelf && !m_paused) || gObj.GetComponent<Rigidbody>().detectCollisions)
                {
                    Debug.LogWarning("WorldInterface: attempted to update scale when " +
                        id + " was under the control of the physics engine or collisions were enabled");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }
		else
		{			
			Debug.LogWarning("Invalid object ID reached in ReadScalePacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 12;
    }

    /// <summary>
    /// Handles the color feature command. Sets the current color of the object.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the color 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadColorPacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;
       
        byte R = buffer[ind];
        byte G = buffer[ind + 1];
        byte B = buffer[ind + 2];
        
        // get the color so that the alpha value is preserved
        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            if (gObj.GetComponent<Renderer>() != null)
            {
                Material[] mats = gObj.GetComponent<Renderer>().materials;
                if (mats != null)
                {
                    int i = 0;
                    foreach (Material m in mats)
                    {
                        Color myColor = m.GetColor("_Color");
                        myColor[0] = Convert.ToSingle(R) / 255.0f;
                        myColor[1] = Convert.ToSingle(G) / 255.0f;
                        myColor[2] = Convert.ToSingle(B) / 255.0f;

                        gObj.GetComponent<Renderer>().materials[i].SetColor("_Color", myColor);
                        i++;
                    }
                }
            }
            else
            {
                Debug.LogWarning(
                    "WorldInterface: cannot update color because " +
                    id + " doesn't contain a renderer.");
            }
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadColorPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }
        return ind + 3;
    }

    /// <summary>
    /// Handles the transparenct feature command. Sets the object's transparenct level to a given percentage.
    /// 0% indicates opaque; 100% is full transparency.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the transparency 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadTransparencyPacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;

        byte A = buffer[ind];

        // get the color so that only the alpha value is changed
        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            if (gObj.GetComponent<Renderer>() != null)
            {
                Material[] mats = gObj.GetComponent<Renderer>().materials;
                if (mats != null)
                {
                    for (int i = 0; i < mats.Length; i++)
                    {
                        Material mtl = mats[i];
                        Color myColor = mtl.GetColor("_Color");
                        float alpha = (float)A;
                        alpha = Mathf.Min(100, Mathf.Max(0, alpha));
                        myColor[3] = 1.0f - alpha / 100.0f;
                        mtl.SetColor("_Color", myColor);
                        mats[i] = mtl;
                    }
                    gObj.GetComponent<Renderer>().materials = mats;
                }
            }
            else
            {
                Debug.LogWarning(
                    "WorldInterface: cannot update transparency because " +
                    id + " doesn't contain a renderer.");
            }
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadTransparencyPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 1;
    }

    /// <summary>
    /// Handles the visibility feature command. Toggles visibility on and off
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the visibility 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadVisibilityPacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;

        bool isVisible = buffer[ind] == 1;

        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            if (gObj.GetComponent<Renderer>() != null)
            {
                gObj.GetComponent<Renderer>().enabled = isVisible;
            }
            else
            {
                Debug.LogWarning(
                    "WorldInterface: cannot update visibility because " +
                    id + " doesn't contain a renderer.");
            }
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadVisibilityPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }
        
        return ind + 1;
    }

    /// <summary>
    /// Handles the physics feature command. Toggles whether the object is controlled by the physics engine.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the physics 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadPhysicsPacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;

        bool hasPhysics = buffer[ind] == 1;

        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            if (gObj.GetComponent<Rigidbody>() != null)
            {
		        gObj.GetComponent<Rigidbody>().isKinematic = !hasPhysics;
				if(hasPhysics)
				{
					gObj.GetComponent<Rigidbody>().WakeUp();
				}
            }
            else
            {
                Debug.LogWarning(
                   "WorldInterface: cannot update physics because " +
                   id + " doesn't contain a rigidbody.");
            }
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadPhysicsPacket: " + id);
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 1;
    }

    /// <summary>
    /// Handles the collideable feature command.
    /// Toggles whether an object reports collisions. Note that an object 
    /// may be collideable, but not under control of the physics engine. Use 
    /// this configuration to detect contact. An invisible object configured 
    /// in this manner detects that the MPL is within a zone defined by the 
    /// object.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the collideable 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadCollideablePacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;

        bool isCollideable = buffer[ind] == 1;

        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            if (gObj.GetComponent<Rigidbody>() != null)
            {
                gObj.GetComponent<Rigidbody>().detectCollisions = isCollideable;
            }
            else
            {
                Debug.LogWarning(
                   "WorldInterface: cannot update collideable because " +
                   id + " doesn't contain a rigidbody.");
            }
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadCollideablePacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 1;
    }

    /// <summary>
    /// Handles the attach_to feature command.
    /// Creates a constraint that links two objects together at the given 
    /// attachment point. Both objects should have physics enabled. 
    /// Alternatively, to “stick” an object to a point in the world, send a 
    /// zero length string for objectId.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the attach_to 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadAttachToPacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;
        GameObject gObj2;

        string objectId;
        int newInd;
        PacketUtils.GetString(buffer, ind, out objectId, out newInd);
        ind = newInd;

        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            try
            {
                // Negate x to convert to Unity's left handed coordinate frame.
                float x = -BitConverter.ToSingle(buffer, ind);
                float y = BitConverter.ToSingle(buffer, ind + 4);
                float z = BitConverter.ToSingle(buffer, ind + 8);
                Vector3 attachPoint = new Vector3(x, y, z);

                if (objectId.Length == 0)
                {
                    // attach to world
                    FixedJoint fj = gObj.AddComponent<FixedJoint>();
                    fj.anchor = attachPoint;
                    fj.connectedBody = null;
                    if (gObj.GetComponent<Rigidbody>() == null || gObj.GetComponent<Rigidbody>().isKinematic)
                    {
                        Debug.LogWarning(
                            "WorldInterface: cannot update attach_to because " +
                            id + " (initial objec) does not contain a rigidbody or physics is disabled.");
                    }
                }
                else if (m_allWorldObjects.TryGetValue(objectId, out gObj2))
                {
                    // attach to another rigid body
                    FixedJoint fj = gObj.AddComponent<FixedJoint>();
                    fj.anchor = attachPoint;
                    fj.connectedBody = gObj2.GetComponent<Rigidbody>();
                    if (gObj.GetComponent<Rigidbody>() == null || gObj2.GetComponent<Rigidbody>() == null ||
                        (gObj.GetComponent<Rigidbody>().isKinematic && gObj2.GetComponent<Rigidbody>().isKinematic))
                    {
                        Debug.LogWarning(
                             "WorldInterface: cannot update attach_to because " +
                             id + " (initial objec) does not contain a rigidbody or physics is disabled or " +
                             objectId + " (attached objec) does not contain a rigidbody or physics is disabled.");
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "WorldInterface: cannot update attach_to because " +
                        objectId + " (attached object) is invalid.");
#if UNITY_EDITOR
                    int length = GenerateBadObjIdNack(m_respBuffer,
                        (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                        ExtractIpAddress(m_cmdHost.Address));
                    m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadAttachToPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 12;
    }

    /// <summary>
    /// Handles the detach_from feature command.
    /// Removes  a constraint  from an object. If the object was originally 
    /// attached to a point in the world, objectId should be a zero length
    /// string.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the detach_from 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadDetachFromPacket(string id, byte[] buffer, int ind)
    {
        GameObject gObj;
        GameObject gObj2;

        string objectId;
        int newInd;
        PacketUtils.GetString(buffer, ind, out objectId, out newInd);
        ind = newInd;         

        if (m_allWorldObjects.TryGetValue(id, out gObj))
        { 
            if( !m_allWorldObjects.TryGetValue(objectId, out gObj2) )   
            {
                if ( objectId.Length > 0 )
                {
                    Debug.LogWarning(
                        "WorldInterface: cannot update detach_from because " +
                        objectId + " (attached object) is invalid.");
                }
            }            
            
            FixedJoint [] myFixedJoints = gObj.GetComponents<FixedJoint>();
            int ct = 0;
            foreach (FixedJoint fj in myFixedJoints)
            {
                if (objectId.Length == 0 && fj.connectedBody == null || 
                    gObj2.GetComponent<Rigidbody>() != null && gObj2.GetComponent<Rigidbody>() == fj.connectedBody)
                {
                    UnityEngine.Object.Destroy( fj );
                    ct++;
                }                
            }
            if (ct == 0)
            {
                // Check if attach was done with opposite order.
				myFixedJoints = gObj2.GetComponents<FixedJoint>();
				foreach(FixedJoint fj in myFixedJoints)
				{
                    if (gObj.GetComponent<Rigidbody>() != null & gObj.GetComponent<Rigidbody>() == fj.connectedBody)
                    {
                        UnityEngine.Object.Destroy(fj);
                        ct++;
                    }
				}
                if (ct == 0)
                {
                    Debug.LogWarning(
                        "WorldInterface: Could not find the specified fixed joint between " +
                        id + " (initial objec) and " + objectId + " (attached object).");
                }
                else if (ct > 1)
                {
                    Debug.LogWarning(
                        "WorldInterface: Found multiple fixed joints between " +
                        id + " (initial objec) and " + objectId + " (attached object).");
                }
            }
            else if (ct > 1)
            {
                Debug.LogWarning(
                    "WorldInterface: Found multiple fixed joints between " +
                    id + " (initial objec) and " + objectId + " (attached object).");
            }
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadDetachFromPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind;
    }

    /// <summary>
    /// Pauses physics simulation.  FixedUpdate() is not called while paused.
    /// </summary>
    private void PauseWorld(bool enable)
    {        
        m_paused = enable;
        if (enable)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Handles the subscribe feature command.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the subscribe 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadSubscribePacket(string id, byte[] buffer, int ind)
    {
        bool subscribe = buffer[ind] == 1;
        GameObject gObj;
        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            if (subscribe && !m_subscribedObjects.ContainsKey(id))
            {
                SubscribedObject sub = gObj.AddComponent<SubscribedObject>();
                m_subscribedObjects.Add(id, sub);                
            }
            else if(!subscribe)
            {
                SubscribedObject sObj;
                if (m_subscribedObjects.TryGetValue(id, out sObj))
                {
                    m_subscribedObjects.Remove(id);
                    UnityEngine.Object.Destroy(sObj);
                }
            }
        }
        else
        {           
			Debug.LogWarning("Invalid object ID reached in ReadSubscribePacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 1;
    }

    /// <summary>
    /// Handles the subscribe_collision_begin feature command.
    /// Subscribe or unsubscribe to collision begin events for the object.
    /// When subscribed, a message will be sent from the virtual world to 
    /// the host when contact between the given object and another object 
    /// begins.  The object must be collideable to generate a collision
    /// event.  Collision contact point(s) are reported in local coordinates
    /// by default.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the subscribe_collision_begin 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadSubscribeCollisionBeginPacket(string id, byte[] buffer, int ind)
    {
        bool subscribe = buffer[ind] == 1;
        bool worldCoords = buffer[ind + 1] == 1;

        GameObject gObj;
        if (m_allWorldObjects.TryGetValue(id, out gObj))
		{
            CollisionHandler ch = gObj.GetComponent<CollisionHandler>();
            bool destroyed = false;

			if(subscribe && ch == null )
                ch = gObj.AddComponent<CollisionHandler>();
            else if (ch != null && !subscribe && !ch.m_subscribeCollisionEnd)
            {
                UnityEngine.Object.Destroy(gObj.GetComponent<CollisionHandler>());
                destroyed = true;
            }

			if(!destroyed && ch != null)
			{
            	ch.m_subscribeCollisionBegin = subscribe;
				ch.m_worldCoordinates = worldCoords;
			}
        }
        else
        {            
			Debug.LogWarning("Invalid object ID reached in ReadSubscribeCollisionBeginPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 2;
    }
    
    /// <summary>
    /// Handles the subscribe_collision_end feature command.
    /// Subscribe or unsubscribe to collision end events for the object.
    /// When subscribed, a message will be sent from the virtual world to 
    /// the host when contact between the given object and another object 
    /// ends.  The object must be collideable to generate a collision event.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the subscribe_collision_end 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadSubscribeCollisionEndPacket(string id, byte[] buffer, int ind)
    {
        bool subscribe = buffer[ind] == 1;
        		
        GameObject gObj;
        if (m_allWorldObjects.TryGetValue(id, out gObj))
        {
            CollisionHandler ch = gObj.GetComponent<CollisionHandler>();
            bool destroyed = false;
            if (subscribe && ch == null)
				ch = gObj.AddComponent<CollisionHandler>();
            else if (ch != null && !ch.m_subscribeCollisionBegin && !subscribe)
            {
                UnityEngine.Object.Destroy(gObj.GetComponent<CollisionHandler>());
                destroyed = true;
            }

            if (!destroyed && ch != null)
            	ch.m_subscribeCollisionEnd = subscribe;
        }
        else
        {         
			Debug.LogWarning("Invalid object ID reached in ReadSubscribeCollisionEndPacket");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }

        return ind + 1;
    }
	
    /// <summary>
    /// Handles the display_message feature command.
    /// Display a text message to the user in a window. The message is 
    /// displayed in a standard 2D window.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the display_message 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadDisplayMessagePacket(string id, byte[] buffer, int ind)
    {
	
		// typedef struct UserMsgType
		// {
		//    IdType id;		// Unique id that identifies this message.
		//    string mesg<255>;	// Message to display in window.
		//    MsgWindowType window;
		//    FontType font;
		//    Uint8Type style;	// FontStyleTypes bitwise OR'ed together.
		//    Uint8Type fontSize;  // Units are point size.
		//    ObjectColorType textColor;
		// };
		// 
		// typedef struct MsgWindowType
		// {
		//    Int16Type x;		// Screen coordinates of upper left corner of window.
		//    Int16Type y;		// Screen coordinates of upper left corner of window.
		//    Uint16Type width;
		//    Uint16Type height;
		//    BoolType drawBorder; // Draw border around window?
		// };

		string mesgId;
		string mesg;
        int newInd;
        PacketUtils.GetString(buffer, ind, out mesgId, out newInd);
		PacketUtils.GetString(buffer, newInd, out mesg, out newInd);
        ind = newInd;

        short x = 0;
        short y = 0;
        ushort width = 0;
        ushort height = 0;

        try
        {
            x = BitConverter.ToInt16(buffer, ind);
            y = BitConverter.ToInt16(buffer, ind + 2);
            width = BitConverter.ToUInt16(buffer, ind + 4);
            height = BitConverter.ToUInt16(buffer, ind + 6);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }

		bool drawBorder = Convert.ToBoolean(buffer[ind+8]);
		ind += 9;
		
		DisplayMessage.FontType fontType = (DisplayMessage.FontType)buffer[ind];		
		ind++;
		
		DisplayMessage.FontStyleType fontStyleType = (DisplayMessage.FontStyleType)buffer[ind];				
		ind++;
		
		byte fontSize = buffer[ind];		
		ind++;
		
		Color color = new Color( Convert.ToSingle(buffer[ind])/255f, Convert.ToSingle(buffer[ind+1])/255f, Convert.ToSingle(buffer[ind+2])/255f );
		ind += 3;
		
        // ToDo: Consider making DisplayMessage a container of all messages 
        // instead of a single message in the future.
		DisplayMessage [] displayMessages = m_viesysObj.GetComponents<DisplayMessage>();
		for( int i = 0; i < displayMessages.Length; i++ )
		{
			if( String.Compare(displayMessages[i].m_id, mesgId) == 0 )
			{				
				Debug.LogWarning("Message with id " + mesgId + " is already being displayed");
				UnityEngine.Object.Destroy( displayMessages[i] );
				break;
			}
		}
				
		m_viesysObj.AddComponent<DisplayMessage>().AssignValues(
			mesgId, mesg, x, y, width, height, drawBorder, fontType, fontStyleType, fontSize, color);		
		
        return ind;
    }
	
	// <summary>
    /// Handles the destroy_message feature command.
    /// Display a text message to the user in a window. The message is 
    /// displayed in a standard 2D window.
    /// </summary>
    /// <param name="id">Object Id.</param>
    /// <param name="buffer">The buffer containing the display_message 
    /// command.</param>
    /// <param name="ind">Index of first byte of the command.</param>
    /// <returns>Index of next unread byte in buffer.</returns>
    private int ReadDestroyMessagePacket(string id, byte[] buffer, int ind)
    {
		string mesgId;	
        int newInd;
		bool foundMessage = false;
        PacketUtils.GetString(buffer, ind, out mesgId, out newInd);
		ind = newInd;
		
		DisplayMessage [] displayMessages = m_viesysObj.GetComponents<DisplayMessage>();
		for( int i = 0; i < displayMessages.Length; i++ )
		{
			if( String.Compare(displayMessages[i].m_id, mesgId) == 0 )
			{
				UnityEngine.Object.Destroy( displayMessages[i] );
				foundMessage = true;
				break;
			}
		}

        if (!foundMessage)
        {
            Debug.LogWarning("Couldn't find message with id " + mesgId + " in order to destroy it");
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, mesgId,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }
		
		return ind;
	}
	
	public int GenerateReportObjectIdsResponse( byte[] buffer, int numBytes, UInt32 senderIp)
	{		
		int index = 9;
		//Array.Copy( BitConverter.GetBytes(Convert.ToUInt16(numBytes - sizeof(UInt16))), 0L, respBuf, Convert.ToInt64(index), sizeof(UInt16));
		//buffer[0] = Convert.ToByte(numBytes - sizeof(UInt16));     // Message length (LSB).
        //buffer[1] = 0;      // Message length (MSB).
		Array.Copy( BitConverter.GetBytes(numBytes - sizeof(UInt16)), buffer, sizeof(ushort) );
        byte[] temp = BitConverter.GetBytes(senderIp);
        for (int i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.REPORT_OBJECT_IDS;
        Array.Copy(BitConverter.GetBytes((UInt16)m_allWorldObjects.Count), 0, buffer, 7, 2);	
		
		foreach( string key in m_allWorldObjects.Keys )
		{
			buffer[index] = Convert.ToByte( key.Length );
			for( int i = 0 ; i < key.Length ; i++ )
				buffer[index + i + 1] = Convert.ToByte( key[i] );
			index += (key.Length + 1);
		}
	
		buffer[index] = ComputeChecksum(buffer, index);
		return index + 1;
	}
	
	public void GenerateReportObjectStateResponse( out byte [] respBuf, string id, uint senderIp )
	{
		int ind;
		List<string> attachedIds;					
		// Get the GameObject associated with the id
		GameObject gObj;
        if (m_allWorldObjects.TryGetValue(id, out gObj))	
        {
			// Get all of the fixed joints associated with the GameObject
			FixedJoint [] myFixedJoints = gObj.GetComponents<FixedJoint>();
			attachedIds = new List<string>(myFixedJoints.Length);			
            foreach (FixedJoint fj in myFixedJoints)
            {							
				if( fj.connectedBody == null )
				{
					// Attached to World								
					attachedIds.Add(String.Empty);
				}
				else
				{
					// Get the WorldObject script's id from the attached GameObjects
					WorldObject wObjScript = fj.connectedBody.gameObject.GetComponent<WorldObject>();
					if( wObjScript != null )								
						attachedIds.Add(wObjScript.m_id);																	
					else
						Debug.LogWarning("Attached GameObject doesn't have the WorldObject script");
				}
            }
		
			int numIdBytes = 0;
			foreach( string attachedId in attachedIds )
				numIdBytes += (1+attachedId.Length);
		
		    // objectId, translation/rotation/scale, RGBA/visible/physics/collideable/numAttaches/subscribed/collisionBegin/collisionEnd, attachedIds
			int numPayloadBytes = (1 + id.Length) + 9*sizeof(float) + 11*sizeof(byte) + numIdBytes;
			respBuf = new byte[numPayloadBytes+8 + 1];
		
			Array.Copy( BitConverter.GetBytes( Convert.ToUInt16( numPayloadBytes + 7 ) ), 0, respBuf, 0, sizeof(UInt16) ); // MplRspMessageIdType and Checksum are included in the length
			byte[] temp = BitConverter.GetBytes(senderIp);
		    for (int i = 0; i < 4; i++)
		        respBuf[i + 2] = temp[i];
			respBuf[6] = Convert.ToByte(CmdMessageIdE.REPORT_OBJECT_STATE);
			respBuf[7] = Convert.ToByte(id.Length);
			ind = 8;
			for( int i = 0; i < id.Length; i++ )
				respBuf[ind + i] = Convert.ToByte(id[i]);
			ind += id.Length;
		           
            // Negate x to convert to right handed coordinate frame.
			Array.Copy( BitConverter.GetBytes( -gObj.transform.position.x ), 0, respBuf, ind, sizeof(float) );
			Array.Copy( BitConverter.GetBytes( gObj.transform.position.y ), 0, respBuf, ind + sizeof(float), sizeof(float) );
			Array.Copy( BitConverter.GetBytes( gObj.transform.position.z ), 0, respBuf, ind + 2*sizeof(float), sizeof(float) );

            // Negate angles to convert to right handed coordinate frame.  
            // Also convert to radians.
            Array.Copy(BitConverter.GetBytes(gObj.transform.rotation.eulerAngles.x * Mathf.Deg2Rad), 0, respBuf, ind + 3 * sizeof(float), sizeof(float));
            Array.Copy(BitConverter.GetBytes(-gObj.transform.rotation.eulerAngles.y * Mathf.Deg2Rad), 0, respBuf, ind + 4 * sizeof(float), sizeof(float));
            Array.Copy(BitConverter.GetBytes(-gObj.transform.rotation.eulerAngles.z * Mathf.Deg2Rad), 0, respBuf, ind + 5 * sizeof(float), sizeof(float));
		
			Array.Copy( BitConverter.GetBytes( gObj.transform.localScale.x ), 0, respBuf, ind + 6*sizeof(float), sizeof(float) );
			Array.Copy( BitConverter.GetBytes( gObj.transform.localScale.y ), 0, respBuf, ind + 7*sizeof(float), sizeof(float) );
			Array.Copy( BitConverter.GetBytes( gObj.transform.localScale.z ), 0, respBuf, ind + 8*sizeof(float), sizeof(float) );
			ind += 9*sizeof(float);

            if (gObj.GetComponent<Renderer>() != null)
            {
                respBuf[ind] = Convert.ToByte(gObj.GetComponent<Renderer>().material.color.r * 255);
                respBuf[ind + 1] = Convert.ToByte(gObj.GetComponent<Renderer>().material.color.g * 255);
                respBuf[ind + 2] = Convert.ToByte(gObj.GetComponent<Renderer>().material.color.b * 255);
                respBuf[ind + 3] = Convert.ToByte((1.0 - gObj.GetComponent<Renderer>().material.color.a) * 100);
                respBuf[ind + 4] = Convert.ToByte(gObj.GetComponent<Renderer>().enabled);
            }
            else
            {
                respBuf[ind] = respBuf[ind + 1] = respBuf[ind + 2] = respBuf[ind + 3] = respBuf[ind + 4] = 0;
            }

            if (gObj.GetComponent<Rigidbody>() != null)
            {
                respBuf[ind + 5] = Convert.ToByte(gObj.GetComponent<Rigidbody>().isKinematic);
                respBuf[ind + 6] = Convert.ToByte(gObj.GetComponent<Rigidbody>().detectCollisions);
            }
            else
            {
                respBuf[ind + 5] = respBuf[ind + 6] = 0;
            }
			ind += 7;
		
			respBuf[ind] = Convert.ToByte( attachedIds.Count );
			ind++;
			foreach( string attachedId in attachedIds )
			{
				respBuf[ind] = Convert.ToByte(attachedId.Length);
				for( int i = 0; i < attachedId.Length; i++ )
					respBuf[ind+i+1] = Convert.ToByte(attachedId[i]);
				ind += (attachedId.Length + 1);
			}
		
			respBuf[ind] = Convert.ToByte( HasSubscriber( id ) );
		
			CollisionHandler ch = gObj.GetComponent<CollisionHandler>();
			if( ch == null )
			{
				respBuf[ind+1] = 0;
				respBuf[ind+2] = 0;
				respBuf[ind+3] = 0;
			}
			else
			{
				respBuf[ind+1] = Convert.ToByte( ch.m_subscribeCollisionBegin );						 	
				respBuf[ind+2] = Convert.ToByte( ch.m_worldCoordinates );
				respBuf[ind+3] = Convert.ToByte( ch.m_subscribeCollisionEnd );
			}
			ind += 4;
		
			respBuf[ind] = ComputeChecksum(respBuf, ind);
		
			
		
		}
		else
		{
			Debug.LogWarning("Invalid object ID reached in GenerateReportObjectStateResponse");
			respBuf = new byte[0];
#if UNITY_EDITOR
            int length = GenerateBadObjIdNack(m_respBuffer,
                (byte)CmdMessageIdE.UPDATE_WORLD_STATE, id,
                ExtractIpAddress(m_cmdHost.Address));
            m_respUdp.Send(m_respBuffer, length, m_respBroadcast);
#endif
        }
	}

    /// <summary>
    /// Produces a bad checksum Nack message and places it inside the given
    /// buffer.  Normally, m_respBuffer should be passed in as the buffer
    /// unless the message will be sent asynchronously.
    /// </summary>
    /// <param name="buffer">Buffer should be at least 11 bytes long.</param>
    /// <param name="expectedChecksum">Expected checksum from the host's 
    /// command message.</param>
    /// <param name="actualChecksum">Actual checksum calculated on the host's
    /// command message.</param>
    /// <param name="senderIp">IP of the sender of the bad message.</param>
    /// <returns>Number of bytes used by Nack message.</returns>
    public int GenerateBadChecksumNack(byte[] buffer, byte expectedChecksum,
        byte actualChecksum, UInt32 senderIp)
    {
        buffer[0] = 9;     // Message length (LSB).
        buffer[1] = 0;      // Message length (MSB).
        byte[] temp = BitConverter.GetBytes(senderIp);
        for (int i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.NACK;
        buffer[7] = (byte)NackTypeE.BAD_CHECKSUM;
        buffer[8] = actualChecksum;
        buffer[9] = expectedChecksum;
        buffer[10] = ComputeChecksum(buffer, 10);
        return 11;
    }

    /// <summary>
    /// Produces a bad message id Nack message and places it inside the given
    /// buffer.  Normally, m_respBuffer should be passed in as the buffer
    /// unless the message will be sent asynchronously.
    /// </summary>
    /// <param name="buffer">Buffer should be at least 10 bytes long.</param>
    /// <param name="badCmdId">Id of the unknown command.</param>
    /// <param name="senderIp">IP of the sender of the bad message.</param>
    /// <returns>Number of bytes used by Nack message.</returns>
    public int GenerateBadMsgIdNack(
        byte[] buffer, byte badCmdId, UInt32 senderIp)
    {
        buffer[0] = 8;     // Message length (LSB).
        buffer[1] = 0;      // Message length (MSB).
        byte[] temp = BitConverter.GetBytes(senderIp);
        for (int i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.NACK;
        buffer[7] = (byte)NackTypeE.BAD_MSG_ID;
        buffer[8] = badCmdId;
        buffer[9] = ComputeChecksum(buffer, 9);
        return 10;
    }

    /// <summary>
    /// Produces a message fragment Nack message and places it inside the 
    /// given buffer.  Normally, m_respBuffer should be passed in as the 
    /// buffer unless the message will be sent asynchronously.
    /// </summary>
    /// <param name="buffer">Buffer should be at least 11 bytes long.</param>
    /// <param name="senderIp"></param>
    /// <returns></returns>
    public int GenerateMsgFragmentNack(byte[] buffer, byte cmdId, byte featureType, UInt32 senderIp)
    {		
        buffer[0] = 9;      // Message length (LSB).
        buffer[1] = 0;      // Message length (MSB).
        byte[] temp = BitConverter.GetBytes(senderIp);
        for (int i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.NACK;
        buffer[7] = (byte)NackTypeE.MSG_FRAGMENT;
		buffer[8] = cmdId;
		buffer[9] = featureType;
        buffer[10] = ComputeChecksum(buffer, 10);
        return 11;
    }

    /// <summary>
    /// Produces a bad object id Nack message and places it inside the
    /// given buffer.  Normally, m_respBuffer should be passed in as the 
    /// buffer unless the message will be sent asynchronously.  cmdBuffer 
    /// should contain the bytes of bad id.
    /// </summary>
    /// <param name="buffer">Buffer should be at least len + 11 bytes 
    /// long.</param>
    /// <param name="cmdBuffer">This buffer contains the offending id.</param>
    /// <param name="start">The index of the starting byte of the offending
    /// id in cmdBuffer.</param>
    /// <param name="len">The number of bytes that comprises the offending
    /// id.</param>
    /// <param name="senderIp">IP of the sender of the bad message.</param>
    /// <returns>Number of bytes used by Nack message.</returns>
    public int GenerateBadObjIdNack(byte[] buffer, byte cmdId, string badObjId, UInt32 senderIp)
    {
        int i;
        byte[] temp = BitConverter.GetBytes( Convert.ToUInt16(badObjId.Length + 9) );
        buffer[0] = temp[0];    // Message length (LSB).
        buffer[1] = temp[1];    // Message length (MSB).
        temp = BitConverter.GetBytes(senderIp);
        for (i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.NACK;
        buffer[7] = (byte)NackTypeE.BAD_OBJ_ID;
		buffer[8] = cmdId;
		buffer[9] = Convert.ToByte(badObjId.Length);
        for (i = 0; i < badObjId.Length; i++)
            buffer[i + 10] = Convert.ToByte(badObjId[i]);
        buffer[i + 10] = ComputeChecksum(buffer, i + 10);
        return i + 11;
    }

    /// <summary>
    /// Produces a bad feature type Nack message and places it inside the
    /// given buffer.  Normally, m_respBuffer should be passed in as the 
    /// buffer unless the message will be sent asynchronously. 
    /// </summary>
    /// <param name="buffer">Should be at least 10 bytes.</param>
    /// <param name="featureType"></param>
    /// <param name="senderIp"></param>
    /// <returns>Number of bytes used by Nack message.</returns>
    public int GenerateBadFeatureTypeNack(
        byte[] buffer, byte featureType, UInt32 senderIp)
    {
        buffer[0] = 8;      // Message length (LSB).
        buffer[1] = 0;      // Message length (MSB).
        byte[] temp = BitConverter.GetBytes(senderIp);
        for (int i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.NACK;
        buffer[7] = (byte)NackTypeE.BAD_FEATURE_TYPE;
        buffer[8] = featureType;
        buffer[9] = ComputeChecksum(buffer, 9);
        return 10;
    }
	
	/// <summary>
    /// Produces a Busy Nack message and places it inside the
    /// given buffer.  Normally, m_respBuffer should be passed in as the 
    /// buffer unless the message will be sent asynchronously. 
    /// </summary>
    /// <param name="buffer">Should be at least 12 bytes.</param>
    /// <param name="featureType"></param>
    /// <param name="senderIp"></param>
    /// <returns>Number of bytes used by Nack message.</returns>
    public int GenerateBusyNack( byte[] buffer, byte cmdId, byte busyCmdId, byte busyFeatureType, UInt32 senderIp )
    {
        buffer[0] = 10;     // Message length (LSB).
        buffer[1] = 0;      // Message length (MSB).
        byte[] temp = BitConverter.GetBytes(senderIp);
        for (int i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.NACK;
        buffer[7] = (byte)NackTypeE.BUSY;
        buffer[8] = cmdId;
		buffer[9] = busyCmdId;
		buffer[10] = busyFeatureType;
        buffer[11] = ComputeChecksum(buffer, 11);
        return 12;
    }
	
	/// <summary>
    /// Produces an EXE Error Nack message and places it inside the
    /// given buffer.  Normally, m_respBuffer should be passed in as the 
    /// buffer unless the message will be sent asynchronously. 
    /// </summary>
    /// <param name="buffer">Should be at least 12 bytes.</param>
    /// <param name="featureType"></param>
    /// <param name="senderIp"></param>
    /// <returns>Number of bytes used by Nack message.</returns>
    public int GenerateExeErrorNack( byte[] buffer, byte cmdId, byte featureCmd, byte errorCode, UInt32 senderIp )
    {
        buffer[0] = 10;     // Message length (LSB).
        buffer[1] = 0;      // Message length (MSB).
        byte[] temp = BitConverter.GetBytes(senderIp);
        for (int i = 0; i < 4; i++)
            buffer[i + 2] = temp[i];
        buffer[6] = (byte)ResponseMessageIdE.NACK;
        buffer[7] = (byte)NackTypeE.BUSY;
        buffer[8] = cmdId;
		buffer[9] = featureCmd;
		buffer[10] = errorCode;
        buffer[11] = ComputeChecksum(buffer, 11);
        return 12;
    }
#if UNITY_EDITOR
    /// <summary>
    /// Used to asynchronously finish sending a Nack response message.
    /// </summary>
    /// <param name="ar"></param>
    protected void ResponseSendCallback(IAsyncResult ar)
    {
        m_respUdp.EndSend(ar);
    }
	
	/// <summary>
    /// Used to asynchronously finish sending a streaming message for a given timestep.
    /// </summary>
    /// <param name="ar"></param>
    protected void StreamSendCallback(IAsyncResult ar)
    {
        m_streamUdp.EndSend(ar);
    }
	

    /// <summary>
    /// Converts an IPAddress instance into a UInt32.  This method supports
    /// response messages.  They include the IP address of the sender encoded
    /// as a UInt32.
    /// </summary>
    /// <param name="address"></param>
    /// <returns>Zero is returned if IPv4 is not used.</returns>

    public UInt32 ExtractIpAddress(IPAddress address)
    {
        UInt32 addr = 0;
        byte[] addrBytes = address.GetAddressBytes();
        if (addrBytes.Length != 4)
        {
            Debug.LogError(
                "Only IPv4 addresses supported by World Interface ICD.");
        }
        else
            addr = BitConverter.ToUInt32(addrBytes, 0);

        return addr;
    }
#endif
    public void AppendWorldStateMessage(byte[] temp)
	{
		if( (m_streamPtr + temp.Length) > (m_streamBuffer.Length - 1) )
		{
			Debug.LogError("Streaming buffer is not large enough to hold all of the streaming messages.");
			return;
		}
		
		//if( m_streamPtr == STR_PTR_START )
		//	m_streamPtr++;	// Only leave room for the WORLD_STATE enumeration if there is at least one message
		for( int i = 0; i < temp.Length; i++ )
			m_streamBuffer[m_streamPtr+i] = temp[i];
		m_streamPtr += temp.Length;
		m_numWorldStateMessages++;
	}
	
	private void FinalizeWorldStateMessage()
	{
		// length doesn't indclude bytes 0 and 1, but does include the last byte, and the indexing is zero-based
		byte [] lengthBytes = BitConverter.GetBytes( Convert.ToUInt16(m_streamPtr-1) );
		m_streamBuffer[0] = lengthBytes[0];
		m_streamBuffer[1] = lengthBytes[1];
		m_streamBuffer[2] = Convert.ToByte(MplStreamingMessageIdType.WORLD_STATE);
		
		// Array.Copy(BitConverter.GetBytes( Time.fixedTime ), 0, m_streamBuffer, 3, sizeof(float));
		// Array.Copy(BitConverter.GetBytes( Time.fixedTime ), 0, m_streamBuffer, 7, sizeof(float));
		// Debug.Log( "Time: " + Time.fixedTime.ToString() );

		Array.Copy(BitConverter.GetBytes( m_StartTicks + m_Stopwatch.ElapsedTicks ), 0, m_streamBuffer, 3, sizeof(long));
		
		m_streamBuffer[11] = m_numWorldStateMessages;
		m_streamBuffer[m_streamPtr] = ComputeChecksum(m_streamBuffer,m_streamPtr);
	}
	
}
