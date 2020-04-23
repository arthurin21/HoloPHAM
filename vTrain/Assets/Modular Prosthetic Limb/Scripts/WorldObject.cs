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

/// <summary>
/// Add this script to any object that should be visible to the 
/// WorldInterface.  If the object is visible to the WorldInterface, then
/// the object may be manipulated over a UDP socket as specified in the
/// WorldInterface ICD.
/// </summary>
public class WorldObject : MonoBehaviour {

    /// <summary>
    /// If no id is assigned inside the editor, a random id will be assigned.
    /// Although a string is used to hold the id, the format should be 
    /// 2 characters followed by a 16 bit integer.  In IDR format:
    ///     Uint8Type componentType[2];
    ///     Int16Type componentNumber;
    /// </summary>
    public string m_id;

	void Start() 
    {
        
	}
	
}
