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
using System;
using System.Collections;

public class CollisionHandler : MonoBehaviour {
	
	protected WorldInterface m_worldIface;
	public bool m_subscribeCollisionBegin;	// This is set from WorldInterface
	public bool m_subscribeCollisionEnd;	// This is set from WorldInterface
	public bool m_worldCoordinates;

    private string m_id;

    public const string m_UNKNOWN_OBJECT = "unknown object";

	// Use this for initialization
	void Start()
    {
        m_worldIface = WorldInterface.Instance();	// This is a singleton
        m_id = this.GetComponent<WorldObject>().m_id;
	}
	
	// [uint16(length) MessagePayload uint8(checksum)]
	// MessagePayload = { uint8(WORLD_STATE) {[R,V] ...} }
	
	void OnCollisionEnter(Collision collision)
	{
        if (m_subscribeCollisionBegin)
        {
            int ct = 0;
            byte[] temp;
            WorldObject wo = collision.gameObject.GetComponent<WorldObject>();
            string id2;
            if (wo != null)
                id2 = wo.m_id;
            else
                id2 = GetParentId(collision.gameObject.transform.parent);

            Vector3 point;
            Vector3 normal = collision.contacts[0].normal;
            Vector3 relativeVelocity = collision.relativeVelocity;

            // R = uint8(COLLISION_BEGIN)
            // V = [(len string) (len string) (numContacts = 1) 3*single 3*single]		
            temp = new byte[sizeof(byte) + (1 + m_id.Length) + (1 + id2.Length) + 2 * sizeof(byte) + 9 * sizeof(float)];

            temp[ct] = Convert.ToByte(WorldInterface.ReportType.COLLISION_BEGIN);
            ct++;

            temp[ct] = Convert.ToByte(m_id.Length);
            ct++;
            for (int i = 0; i < m_id.Length; i++)
                temp[ct + i] = Convert.ToByte(m_id[i]);
            ct = ct + m_id.Length;

            temp[ct] = Convert.ToByte(id2.Length);
            ct++;
            for (int i = 0; i < id2.Length; i++)
                temp[ct + i] = Convert.ToByte(id2[i]);
            ct = ct + id2.Length;

            temp[ct] = 1;	// number of contacts is fixed at 1
            ct = ct + sizeof(byte);

            if (m_worldCoordinates)
                point = collision.contacts[0].point;
            else
                point = this.transform.InverseTransformPoint(collision.contacts[0].point);

            // Negate x to convert to a right handed coordinate frame.
            Array.Copy(BitConverter.GetBytes(-point.x), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);
            Array.Copy(BitConverter.GetBytes(point.y), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);
            Array.Copy(BitConverter.GetBytes(point.z), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);

            temp[ct] = 1;	// number of contacts is fixed at 1
            ct = ct + sizeof(byte);

            // Negate x to convert to a right handed coordinate frame.
            Array.Copy(BitConverter.GetBytes(-normal.x), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);
            Array.Copy(BitConverter.GetBytes(normal.y), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);
            Array.Copy(BitConverter.GetBytes(normal.z), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);

            // Negate x to convert to a right handed coordinate frame.
            Array.Copy(BitConverter.GetBytes(-relativeVelocity.x), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);
            Array.Copy(BitConverter.GetBytes(relativeVelocity.y), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);
            Array.Copy(BitConverter.GetBytes(relativeVelocity.z), 0, temp, ct, sizeof(float));
            ct = ct + sizeof(float);

            m_worldIface.AppendWorldStateMessage(temp);
        }
	}
	
	void OnCollisionExit(Collision collisionInfo)
	{
        if (m_subscribeCollisionEnd)
        {
            int ct = 0;
            byte[] temp;

            string id2;
            WorldObject wo = collisionInfo.gameObject.GetComponent<WorldObject>();
            if (wo != null)
                id2 = wo.m_id;
            else
                id2 = GetParentId(collisionInfo.gameObject.transform.parent);

            // R = uint8(COLLISION_BEGIN)
            // V = [(len string) (len string)]
            temp = new byte[sizeof(byte) + (1 + m_id.Length) + (1 + id2.Length)];

            temp[ct] = Convert.ToByte(WorldInterface.ReportType.COLLISION_END);
            ct++;

            temp[ct] = Convert.ToByte(m_id.Length);
            ct++;
            for (int i = 0; i < m_id.Length; i++)
                temp[ct + i] = Convert.ToByte(m_id[i]);
            // Array.Copy(Array.ConvertAll<char,byte>(id1.ToCharArray(), new Converter<char,byte>(charToByte)),0,temp,ct,id1.Length);		
            ct = ct + m_id.Length;

            temp[ct] = Convert.ToByte(id2.Length);
            ct++;
            for (int i = 0; i < id2.Length; i++)
                temp[ct + i] = Convert.ToByte(id2[i]);
            // Array.Copy(Array.ConvertAll<char,byte>(id2.ToCharArray(), new Converter<char,byte>(charToByte)),0,temp,ct,id2.Length);				
            ct = ct + id2.Length;

            m_worldIface.AppendWorldStateMessage(temp);
        }
	}

    void OnTriggerEnter(Collider other)
    {
        if (m_subscribeCollisionBegin)
        {
            WorldObject wo = other.gameObject.GetComponent<WorldObject>();
            string id2;
            if (wo != null)
                id2 = wo.m_id;
            else
                id2 = GetParentId(other.gameObject.transform.parent);

            // Report 0 for a trigger.
            Vector3 relativeVelocity = new Vector3();

            int ct = 0;
            byte[] temp;

            // R = uint8(COLLISION_BEGIN)
            // V = [(len string) (len string) (numContacts = 0) (relative velocity) 3*single]		
            temp = new byte[sizeof(byte) + (1 + m_id.Length) + (1 + id2.Length) + 2 * sizeof(byte) + 3 * sizeof(float)];

            temp[ct] = (byte)WorldInterface.ReportType.COLLISION_BEGIN;
            ct++;

            temp[ct] = (byte)m_id.Length;
            ct++;
            for (int i = 0; i < m_id.Length; i++)
                temp[ct + i] = (byte)m_id[i];
            ct += m_id.Length;

            temp[ct] = (byte)id2.Length;
            ct++;
            for (int i = 0; i < id2.Length; i++)
                temp[ct + i] = (byte)id2[i];
            ct += id2.Length;

            temp[ct] = 0;	// No contact points for a trigger.
            ct += sizeof(byte);

            temp[ct] = 0;	// No contact normals for a trigger.
            ct += sizeof(byte);

            Array.Copy(BitConverter.GetBytes(relativeVelocity.x), 0, temp, ct, sizeof(float));
            ct += sizeof(float);
            Array.Copy(BitConverter.GetBytes(relativeVelocity.y), 0, temp, ct, sizeof(float));
            ct += sizeof(float);
            Array.Copy(BitConverter.GetBytes(relativeVelocity.z), 0, temp, ct, sizeof(float));
            ct += sizeof(float);

            m_worldIface.AppendWorldStateMessage(temp);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (m_subscribeCollisionEnd)
        {
            int ct = 0;
            byte[] temp;

            string id2;
            WorldObject wo = other.gameObject.GetComponent<WorldObject>();
            if (wo != null)
                id2 = wo.m_id;
            else
                id2 = GetParentId(other.gameObject.transform.parent);

            // R = uint8(COLLISION_BEGIN)
            // V = [(len string) (len string)]
            temp = new byte[sizeof(byte) + (1 + m_id.Length) + (1 + id2.Length)];

            temp[ct] = Convert.ToByte(WorldInterface.ReportType.COLLISION_END);
            ct++;

            temp[ct] = Convert.ToByte(m_id.Length);
            ct++;
            for (int i = 0; i < m_id.Length; i++)
                temp[ct + i] = Convert.ToByte(m_id[i]);
            // Array.Copy(Array.ConvertAll<char,byte>(id1.ToCharArray(), new Converter<char,byte>(charToByte)),0,temp,ct,id1.Length);		
            ct = ct + m_id.Length;

            temp[ct] = Convert.ToByte(id2.Length);
            ct++;
            for (int i = 0; i < id2.Length; i++)
                temp[ct + i] = Convert.ToByte(id2[i]);
            // Array.Copy(Array.ConvertAll<char,byte>(id2.ToCharArray(), new Converter<char,byte>(charToByte)),0,temp,ct,id2.Length);				
            ct = ct + id2.Length;

            m_worldIface.AppendWorldStateMessage(temp);
        }
    }

    /// <summary>
    /// Checks parent of object for a WorldObject id.
    /// </summary>
    /// <param name="parent">Reference to the parent object. Null is acceptable.</param>
    /// <returns>The WorldObject id of the parent or "unknown object".</returns>
    private string GetParentId(Transform parent)
    {
        string id = m_UNKNOWN_OBJECT;

        if (parent != null)
        {
            WorldObject wo = parent.gameObject.GetComponent<WorldObject>();
            if (wo != null)
                id = wo.m_id;
        }

        return id;
    }
}
