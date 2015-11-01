using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Global  :MonoBehaviour{

    public static GameObject prefab;
    public static List<GameObject> brokenObjectList = new List<GameObject>();


    public static void load()
    {
        prefab = Resources.Load("Cube", typeof(GameObject)) as GameObject;
        for (int i = 0; i < 10; i++)
        {
            GameObject go = Instantiate(prefab);
            go.GetComponent<Rigidbody>().Sleep();
            brokenObjectList.Add(go);
        }
    }

    void Awake()
    {
        //Global.load();
    }

}
