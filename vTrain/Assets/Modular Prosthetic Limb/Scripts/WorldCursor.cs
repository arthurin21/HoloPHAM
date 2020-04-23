using UnityEngine;

public class WorldCursor : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Renderer thisRenderer;

    private Color color_Original;

    //ObjectOnCursorEventManager m_ObjectOnCursorTemp;

//    bool b_ObjectOnCursor = false;

    // Use this for initialization
    void Start()
    {
        // Grab the mesh renderer that's on the same object as this script.
        meshRenderer = this.gameObject.GetComponentInChildren<MeshRenderer>();

        thisRenderer = this.gameObject.GetComponentInChildren<Renderer>();
        color_Original = thisRenderer.material.color;

    }

    // Update is called once per frame
    void Update()
    {
        // Do a raycast into the world based on the user's
        // head position and orientation.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;


        //Check for Cursor hitting/on Objects
        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo))
        {
            // If the raycast hit a hologram...

            #region Cursor On Object
            // Display the cursor mesh.
            meshRenderer.enabled = true;

            // Move the cursor to the point where the raycast hit.
            this.transform.position = hitInfo.point;

            // Rotate the cursor to hug the surface of the hologram.
            this.transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);

            /*
            //Modify Object On Cursor (change objet that cursor hits)
            #region Modify Object On Cursor
            try
            {

                if(hitInfo.transform.gameObject.GetComponent<ObjectOnCursorEventManager>() != null)
                {
                    if (!hitInfo.transform.gameObject.GetComponent<ObjectOnCursorEventManager>().GetMouseOver())
                    {
                        //Found new object - set mouse over on last object to false
                        if(m_ObjectOnCursorTemp != null)
                        {
                            m_ObjectOnCursorTemp.SetMouseOver(false);
                        }//if - check for null

                        //Assign Pointer to new object so can disable later
                        m_ObjectOnCursorTemp = hitInfo.transform.gameObject.GetComponent<ObjectOnCursorEventManager>();

                        //Set Mouse Over - 
                        m_ObjectOnCursorTemp.SetMouseOver(true);

                    }
                    else
                    {
                        

                    }//if - check for new object type

                }//Check if Right kind of Object
                else
                {
                    //Found new object - set mouse over on last object to false
                    if (m_ObjectOnCursorTemp != null)
                    {
                        m_ObjectOnCursorTemp.SetMouseOver(false);
                    }//if - check for null

                }//Check if right kind of object

            }
            catch
            {
                //Found new object - set mouse over on last object to false
                if (m_ObjectOnCursorTemp != null)
                {
                    m_ObjectOnCursorTemp.SetMouseOver(false);
                }//if - check for null

            }//try - See if can access Object Information

            
            #endregion //Modify Object On Cursor
            */
            thisRenderer.material.color = Color.yellow;

            #endregion //Cursor On Object
        }
        else
        {
            // Off of any objects

            #region Cursor Off Object

            // If the raycast did not hit a hologram, hide the cursor mesh.
            meshRenderer.enabled = false;

            thisRenderer.material.color = color_Original;

            /*
            try
            {
                if (m_ObjectOnCursorTemp != null)
                {
                    m_ObjectOnCursorTemp.SetMouseOver(false);
                    
                }//Check if Right kind of Object
                
            }
            catch
            {

            }//try - See if can access Object Information

            */
            #endregion //Cursor Off Object

        }//if - check if cursor is hitting objects

    }//Update

}//Class - WorldCursor
