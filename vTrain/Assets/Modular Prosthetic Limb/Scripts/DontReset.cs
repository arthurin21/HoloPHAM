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

/// <summary>
/// Attach this script to objects that should not be destroyed during a 
/// reset.
/// </summary>
public class DontReset : MonoBehaviour
{
    void Awake()
    {
        if (ResetHandler.Reset)
        {
            Debug.Log("Post-reset: destroying duplicate " + gameObject.name);
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    #region Commented Out
    //protected WorldInterface m_worldIface;
		
    //void Awake()
    //{
    //    m_worldIface = WorldInterface.Instance();	// This is a singleton
		
    //    // string name = this.gameObject.name;
		
    //    if ( !m_worldIface.WasReset )
    //    {
    //        // this is the first instance - make it persist
    //        // Transform [] childrenTransforms;
    //        // childrenTransforms = this.gameObject.GetComponentsInChildren<Transform>();
    //        // foreach( Transform childTransform in childrenTransforms )
    //        // {
    //        // 	DontDestroyOnLoad( childTransform.gameObject );
    //        // }
    //        DontDestroyOnLoad(this.gameObject);
    //    }
    //    else
    //    {
    //        WorldInterfaceWrapper wif = this.gameObject.GetComponent<WorldInterfaceWrapper>();
    //        if( wif != null )
    //        {				
    //            m_worldIface.ClearWorldObjects();
    //            m_worldIface.AddWorldObjects();
    //            m_worldIface.Paused = wif.m_startPaused;
    //        }
			
    //        // this must be a duplicate from a scene reload - DESTROY!
    //        // Transform [] childrenTransforms;
    //        // childrenTransforms = this.gameObject.GetComponentsInChildren<Transform>();
    //        // foreach( Transform childTransform in childrenTransforms )
    //        // {
    //        // 	Destroy( childTransform.gameObject );
    //        // }
    //        Destroy(this.gameObject);			
    //    }
    //}
    #endregion
}