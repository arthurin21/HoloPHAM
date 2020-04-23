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
using System;
using System.Collections.Generic;

/// <summary>
/// This class implements scenario reset.
/// </summary>
public class ResetHandler : MonoBehaviour {

    static private bool m_reset = false;
    static public bool Reset
    {
        get { return m_reset; }
    }

    /// <summary>
    /// Set of all game objects that were disabled.
    /// </summary>
    private List<GameObject> m_gameObjects;

    /// <summary>
    /// Stores original culling mask of each camera while cameras set to
    /// cull everything.
    /// </summary>
    private List<KeyValuePair<Camera, int>> m_cameraCullMask;

    void Start() 
    {
        m_gameObjects = new List<GameObject>();
        m_cameraCullMask = new List<KeyValuePair<Camera, int>>();

        // This is now configured in the Physics settings of the project.
        //
        //Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Arm"),
        //    LayerMask.NameToLayer("Arm"), true);
	}
	

    /// <summary>
    /// Uses Application.LoadLevel() resets the scenario.
    /// LoadLevel() calls Destroy() on all objects in scene.  
    /// Destruction doesn't happen until some time before the next
    /// frame is rendered.  See FinishReset() for description of the rest
    /// of the reset process.
    /// </summary>
    public void BeginReset()
    {
        m_reset = true;
        Time.timeScale = 0.0f;
        //Application.LoadLevel(Application.loadedLevel);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        // LoadLevel() calls Destroy() on all objects in scene.  
        // Destruction doesn't happen until some time before the next
        // frame is rendered.  Since On GUI() happens during a frame 
        // render, objects probably live until the next frame.
        StartCoroutine(FinishReset());
    }

    /// <summary>
    /// This method is called several times to complete the reset process.
    /// It uses yield to resume execution after two frames render.  It then
    /// sleeps for two seconds while the vMPL returns to its position prior
    /// to the reset while blanking the cameras.  Finally, it restores the
    /// cameras.
    /// </summary>
    /// <returns></returns>
    private IEnumerator FinishReset()
    {
        // Clear subscriber list now since transform updates don't need to
        // be sent while waiting for reset.
//        WorldInterface.Instance().ClearSubscribers();

        // Wait 2 frames to ensure that objects are destroyed.
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // It's safe to turn off the reset flag once old objects destroyed.
        m_reset = false;

        // Reset MPL via VulcanXInterface.
        VulcanXInterface vx = gameObject.GetComponent<VulcanXInterface>();
        if (vx == null)
        {
            throw new ApplicationException("VulcanXInterface not found.");
        }
        vx.Reset();

        // Reset WorldInterface internals.
//        WorldInterface wif = WorldInterface.Instance();		
//        wif.ClearWorldObjects();
//        wif.AddWorldObjects();
		
        // Disable game objects so nothing else moves when physics turned
        // back on to move arm back to its pre-reset position.
        DisableGameObjects();

        // Blank cameras while arm moves.
        BlankCameras();

        // Start physics simulation.
        Time.timeScale = 1;

        // Give arm 2 seconds to move back to its position.
        yield return new WaitForSeconds(2);

        // Pause simulation again and turn game objects back on.
//        WorldInterface.Instance().Paused = true;
        EnableGameObjects();
        RestoreCameras();
//		wif.ResetComplete();
    }

    /// <summary>
    /// Disables all game objects except for those belonging to the Arm
    /// layer or if it contains the VulcanXInterface, the 
    /// WorldInterfaceWrapper, or a camera.
    /// </summary>
    private void DisableGameObjects()
    {
        UnityEngine.Object[] objs =
            GameObject.FindObjectsOfType(typeof(GameObject));
        m_gameObjects.Clear();
        foreach (UnityEngine.Object o in objs)
        {
            GameObject gobj = o as GameObject;
            if (gobj != null)
            {
                if (gobj.layer != LayerMask.NameToLayer("RightArm") && 
                    gobj.layer != LayerMask.NameToLayer("LeftArm") )
                {
                    //if (gobj.GetComponent<VulcanXInterface>() == null &&
                    //    gobj.GetComponent<WorldInterfaceWrapper>() == null &&
                    //    gobj.GetComponent<Camera>() == null)
                    //{
                    if (gobj.GetComponent<VulcanXInterface>() == null &&
                        gobj.GetComponent<Camera>() == null)
                    {
                        m_gameObjects.Add(gobj);
                        gobj.SetActive(false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Re-enable game objects that were active before DisableGameObjects()
    /// called.
    /// </summary>
    private void EnableGameObjects()
    {
        foreach (GameObject gobj in m_gameObjects)
        {
            gobj.SetActive(true);
        }

        m_gameObjects.Clear();
    }

    /// <summary>
    /// Render nothing but preserve original culling mask for 
    /// RestoreCameras().
    /// </summary>
    private void BlankCameras()
    {
        m_cameraCullMask.Clear();
        foreach (Camera c in Camera.allCameras)
        {
            m_cameraCullMask.Add(
                new KeyValuePair<Camera, int>(c, c.cullingMask));
            c.cullingMask = 0;
        }
    }

    /// <summary>
    /// Restore the original culling mask to each camera.
    /// </summary>
    private void RestoreCameras()
    {
        foreach (KeyValuePair<Camera, int> pair in m_cameraCullMask)
        {
            pair.Key.cullingMask = pair.Value;
        }
        m_cameraCullMask.Clear();
    }
}
