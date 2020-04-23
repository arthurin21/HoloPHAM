using UnityEngine;
using System.Collections;

public class WorldInterfaceWrapper : MonoBehaviour {

    // Provide way to not start paused when running in the editor.
    public bool m_startPaused = true;
    private bool m_initialized = false;

    // Time value used to prevent multiple keypress events when a user presses a key (to pause, for example, which is a toggle)
    private float realTime;

    protected WorldInterface m_worldIface;

   // protected GUIText m_guiText;

    // Use this for initialization
    void Start () {

        if (m_initialized)
        {
            throw new System.ApplicationException(
                "Multiple instances of WorldInterfaceWrapper detected.");
        }

        m_initialized = true;

        m_worldIface = WorldInterface.Instance();
    }
	
	// Update is called once per frame
	void Update () {
        // disabled the pause functionality
        m_worldIface.Step();
    }
}
