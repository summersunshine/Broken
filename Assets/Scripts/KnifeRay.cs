using UnityEngine;
using System.Collections;

public class KnifeRay : MonoBehaviour
{

    private LineRenderer lineRenderer;

    private Vector3 position1;
    private Vector3 position2;
    private Vector3 position3;

    private bool isClicked = false;

    // Use this for initialization
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        bool isMouseDown = Input.GetMouseButton(0);
        if (isMouseDown && !isClicked)
        {
            position1 = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward / 50);
            isClicked = true;
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetPosition(0, position1);
            lineRenderer.SetPosition(1, position1);
        }
        else if (isMouseDown)
        {
            position2 = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward / 50);
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetPosition(1, position2);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isClicked = false;
            //lineRenderer.SetVertexCount(0);
        }

    }
}
