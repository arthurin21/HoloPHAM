using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateHUD : MonoBehaviour
{
    // information sources
    private vMPLMovementArbiter arbiter = null;
    private FingerTipForceSensors sensors = null;

    // hud
    private TMPro.TextMeshProUGUI hud = null;

    // Start is called before the first frame update
    void Start()
    {
        arbiter = GameObject.Find( "vMPLMovementArbiter" ).GetComponent<vMPLMovementArbiter>();
        sensors = (FingerTipForceSensors) GameObject.FindObjectOfType<FingerTipForceSensors>();
        hud = GameObject.Find( "HUD" ).GetComponent<TMPro.TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        float [] joints = arbiter.GetRightUpperArmAngles();
        float [] force = sensors.GetForceValues();

        string new_text = string.Format( "SHOULDER FLEXION: {0}\n", joints[0].ToString("F1") ) +
                          string.Format( "SHOULDER ABDUCTION: {0}\n", joints[1].ToString("F1") ) +
                          string.Format( "HUMERAL ROTATION: {0}\n", joints[2].ToString("F1") ) +
                          string.Format( "ELBOW FLEXION: {0}\n", joints[3].ToString("F1") ) +
                          string.Format( "WRIST ROTATION: {0}\n", joints[4].ToString("F1") ) +
                          string.Format( "WRIST DEVIATION: {0}\n", joints[5].ToString("F1") ) +
                          string.Format( "WRIST FLEXION: {0}\n", joints[6].ToString("F1") ) +
                          "\n-----------------------------------------\n\n" +
                          string.Format( "THUMB FORCE: {0}\n", force[0].ToString("F1") ) +
                          string.Format( "INDEX FORCE: {0}\n", force[1].ToString("F1") ) +
                          string.Format( "MIDDLE FORCE: {0}\n", force[2].ToString("F1") ) +
                          string.Format( "RING FORCE: {0}\n", force[3].ToString("F1") ) +
                          string.Format( "LITTLE FORCE: {0}\n", force[4].ToString("F1") );
        hud.text = new_text;
    }
}
