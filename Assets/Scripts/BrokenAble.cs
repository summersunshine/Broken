using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TINVoronoi;

public class BrokenAble : MonoBehaviour
{
    GameObject prefab;
    Delaynay D_TIN = new Delaynay(); //核心功能类
    bool isBroken = false;
    Vector3 force;
    Vector3 position;
    void Awake()
    {
        prefab = Resources.Load("Cube", typeof(GameObject)) as GameObject;
    }

  
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.collider.name);
        if (collision.collider.name == "Sphere" && !isBroken) 
        {
            ContactPoint point = collision.contacts[0];
            force = point.normal;
            position = transform.InverseTransformPoint(point.point) ;
            ShowBroken(position * 10);
            //StartCoroutine(ShowBroken(position*10));
            
            isBroken = true;
        }

    }



    void  ShowBroken(Vector3 pos)
    {
        Debug.Log(pos);
        GetComponent<MeshFilter>().mesh = null;
        GetComponent<BoxCollider>().enabled = false;
        D_TIN.ShowBroken(pos);
        //yield return new WaitForSeconds(0.1f);
        D_TIN.CreateVoronoi();
        //yield return new WaitForSeconds(0.1f);
        //plane.SetActive(false);
        for (int i = 0; i < D_TIN.polygons.Count; i++)
        {
            setMeshByPolygon(D_TIN.polygons[i],i);
            //yield return new WaitForSeconds(0.01f);
        }

    }

    public void setMeshByPolygon(Polygon polygon,int i)
    {
        if (polygon.points.Count <= 2)
            return;


        Mesh subMesh = new Mesh();
        subMesh.name = i.ToString();
        subMesh.vertices = getVerticesByPolygon(polygon);
        subMesh.triangles = getTrianglesByPolygon(polygon);
        subMesh.uv = getUVByPolygon(polygon);
        subMesh.RecalculateNormals();

        GameObject go = Instantiate(prefab);
        go.GetComponent<MeshFilter>().mesh = subMesh;
        go.GetComponent<MeshCollider>().sharedMesh = subMesh;
        go.GetComponent<MeshCollider>().convex = true;
        go.transform.localPosition = transform.localPosition;
        go.transform.localRotation = transform.localRotation;
    }

    private Vector2[] getUVByPolygon(Polygon polygon)
    {
        int verticCount = polygon.points.Count * 2;
        Vector2[] uv = new Vector2[verticCount];
        BoundaryBox box = D_TIN.DS.BBOX;
        for (int i = 0; i < polygon.points.Count; i++)
        {
            uv[2 * i] = new Vector2((box.XRight - polygon.points[i].x) / box.width, (box.YBottom - polygon.points[i].y) / box.height);
            uv[2 * i + 1] = new Vector2((box.XRight - polygon.points[i].x) / box.width, (box.YBottom - polygon.points[i].y) / box.height);
        }
        return uv;
    }


    public Vector3[] getVerticesByPolygon(Polygon polygon)
    {
        polygon.addVertex(getCenter(polygon));
        int verticCount = polygon.points.Count * 2;
        BoundaryBox box = D_TIN.DS.BBOX;
        Vector3[] vertics = new Vector3[verticCount];
        for (int i = 0; i < polygon.points.Count; i++)
        {
            vertics[2 * i] = new Vector3(polygon.points[i].x , 5, polygon.points[i].y );
            vertics[2 * i + 1] = new Vector3(polygon.points[i].x , -5, polygon.points[i].y );
            Debug.Log(vertics[2 * i]);
            Debug.Log(vertics[2 * i + 1]);

        }
        return vertics;
    }

    public int[] getTrianglesByPolygon(Polygon polygon)
    {
        int triangleCount = polygon.points.Count-1;
        int verticCount = 2*triangleCount;
        int[] triangles = new int[12 * triangleCount];
        for (int i = 0; i < triangleCount; i++)
        {
            triangles[i * 12 + 0] = (2 * i + 0) % verticCount;
            triangles[i * 12 + 1] = (2 * i + 2) % verticCount;
            triangles[i * 12 + 2] = verticCount;

            triangles[i * 12 + 3] = (2 * i + 3) % verticCount;
            triangles[i * 12 + 4] = (2 * i + 1) % verticCount;
            triangles[i * 12 + 5] = verticCount + 1;
            
            triangles[i * 12 + 6] = (2 * i + 0) % verticCount;
            triangles[i * 12 + 7] = (2 * i + 1) % verticCount;
            triangles[i * 12 + 8] = (2 * i + 3) % verticCount;
            
            triangles[i * 12 + 9] = (2 * i + 3) % verticCount;
            triangles[i * 12 + 10] = (2 * i + 2) % verticCount;
            triangles[i * 12 + 11] = (2 * i + 0) % verticCount;
        }
        return triangles;
    }



    private Vector2 getCenter(Polygon polygon)
    {
        float x = 0;
        float y = 0;
        for (int i = 0; i < polygon.points.Count; i++)
        {
            x += polygon.points[i].x;
            y += polygon.points[i].y;
        }
        x /= polygon.points.Count;
        y /= polygon.points.Count;
        return new Vector2(x, y);
    }



}
