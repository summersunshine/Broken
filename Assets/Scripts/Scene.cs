using UnityEngine;
using System.Collections;

public class Scene : MonoBehaviour {
    bool isBroken = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButton(0) && !isBroken)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);//从摄像机发出到点击坐标的射线
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                Vector3 dir = hitInfo.point - transform.position;
                GetComponent<Rigidbody>().AddForce(dir/3, ForceMode.VelocityChange);

                //isBroken = true;

                //Debug.DrawLine(ray.origin, hitInfo.point);//划出射线，只有在scene视图中才能看到
                //GameObject gameObj = hitInfo.collider.gameObject;
                //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //go.transform.position = hitInfo.point;
                //go.transform.SetParent(plane.transform);
                //Vector3 locoal = go.transform.localPosition;
                //go.SetActive(false);


                //ShowBroken(locoal);
            }
        }
	}
}
