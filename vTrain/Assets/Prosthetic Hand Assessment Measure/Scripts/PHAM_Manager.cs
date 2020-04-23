using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PHAM_Manager : MonoBehaviour {
	private static GameObject[] holders;
	public static int[] tasks;
	private static int current_task_indx;
    public static PHAM_Manager instance = null;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if(instance != this)
            Destroy(gameObject);
    }

    void Start() {
        holders = new GameObject[12];
        int counter = 0;

        //Add all the available holders to the manager
        for (int i = 0; i < gameObject.transform.childCount; i++)
            if (gameObject.transform.GetChild(i).name.Contains("Holder"))
            {
                holders[counter] = gameObject.transform.GetChild(i).gameObject;
                counter++;
            }

        tasks = new int[4];

        //Task Sequence Declaration
        // string numbers = "0123";
        tasks = new int[] {0, 1, 2, 3};
		//for (int i = 0; i < 4; i++) {
		//	int rng = Random.Range (0, numbers.Length);
		//	tasks[i] = numbers[rng] - 48;
		//	numbers = numbers.Remove(rng, 1);
		//	Debug.Log (numbers);
		//}
			
        //Run the tasks
        nextTask();
    }

    public static void ColorHolder()
    {
		if (current_task_indx < 5) {
			Vector3 inst_pos = new Vector3 (0, 0, 0);
			int target = 0;
			switch (tasks [current_task_indx - 1]) {
			case 0: //Horizontal Bottom Right to Horizontal Top Left
				inst_pos = holders[8].transform.position;
                GameObject.Find ("CylinderPrimitive").transform.rotation = Quaternion.Euler (0, 0, 0);
				target = 4;
				break;
			case 1: //Horizontal Top Right to Vertical Bottom Right
                holders[4].GetComponent<Renderer>().material.color = new Color(.8f, .8f, .8f);
                inst_pos = holders [5].transform.position;
				target = 8;
				break;
			case 2: //Horizontal Top Left to Horizontal Bottom Right
                holders[8].GetComponent<Renderer>().material.color = new Color(.8f, .8f, .8f);
                inst_pos = holders [4].transform.position;
				target = 1;
				break;
			case 3: //Horizontal Bottom Left to Vertical Top Left
                holders[1].GetComponent<Renderer>().material.color = new Color(.8f, .8f, .8f);
                inst_pos = holders [0].transform.position;
				target = 9;
				break;
			}
			//Choose a random holder that isn't currrently activated
			//        for(rng = Random.Range(0, 11); holders[rng].GetComponent<Holder>().isActivated(); rng = Random.Range(0,11));
			GameObject.Find ("CylinderPrimitive").transform.position = inst_pos + new Vector3(0, 0, -.75f);
			// GameObject.Find ("CylinderPrimitive").transform.rotation = Quaternion.Euler (0, 0, 90);

			//Change the color and activate the holder
			holders [target].GetComponent<Renderer> ().material.color = new Color (.8f, .03f, .02f);
			holders [target].GetComponent<Holder> ().activate ();
		}
    }
    public static void nextTask()
    {
        current_task_indx++;
        ColorHolder();
    }

}
