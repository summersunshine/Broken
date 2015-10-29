using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TINVoronoi;

public class Window : MonoBehaviour {

    public enum RandomType
    {
        circle = 0,
        rect = 1,
    }

    public List<Vector3> vertexs;

    bool isBroken = false;
    GameObject scene;
    GameObject plane;
    GameObject prefab;

    int realnum = 0;

    Delaynay D_TIN = new Delaynay(); //核心功能类


    void Awake()
    {
        plane = GameObject.Find("Plane");
        scene = GameObject.Find("Scene");
        prefab = Resources.Load("Cube", typeof(GameObject)) as GameObject;
    }

   
	// Use this for initialization
	void Start () {
        D_TIN.DS.BBOX.XLeft = -5;
        D_TIN.DS.BBOX.YTop = -5;
        D_TIN.DS.BBOX.XRight = 5;
        D_TIN.DS.BBOX.YBottom = 5;

        //Mesh mesh = plane.GetComponent<MeshFilter>().mesh;

        //
        //addVertex(new Vector2(50, 50));
        //addVertex(new Vector2(50, -50));
        //addVertex(new Vector2(-50, 50));
        //addVertex(new Vector2(-50, -50));
       // 
	}

    void Update()
    {
         if(Input.GetMouseButton(0) && !isBroken)
         {
             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);//从摄像机发出到点击坐标的射线
             RaycastHit hitInfo;
             if(Physics.Raycast(ray,out hitInfo))
             {
                 Debug.DrawLine(ray.origin,hitInfo.point);//划出射线，只有在scene视图中才能看到
                 GameObject gameObj = hitInfo.collider.gameObject;
                 Debug.Log(gameObj.transform.worldToLocalMatrix);
                 Debug.Log(hitInfo.point);
                 

                 GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                 go.transform.position = hitInfo.point;
                 go.transform.SetParent(plane.transform);
                 isBroken = true;
                 Vector3 locoal = go.transform.localPosition;
                 Debug.Log(locoal);
                 createRandomVertexs(new Vector2(locoal.x,locoal.z), 5, RandomType.circle, 1f);
                 ShowTriangle();
             }
         }

    }


    public void createRandomVertexs(Vector2 center, int num,RandomType randomType,float size)
    {
        while (realnum < num)
        {
            Vector2 pos = Vector2.zero;
            if (randomType == RandomType.circle)
            {
                pos = center + size * UnityEngine.Random.insideUnitCircle;
            }
            else if (randomType == RandomType.rect)
            {
                pos = center + new Vector2(UnityEngine.Random.Range(-size, size), UnityEngine.Random.Range(-size, size));
            }
            Debug.Log(pos);
            addVertex(pos);
        }



    }

 

	

    public void addVertex(Vector2 e)
    {
        for (int i = 0; i < D_TIN.DS.VerticesNum; i++)
        {
            float diffx = D_TIN.DS.Vertex[i].x - e.x;
            float diffy = D_TIN.DS.Vertex[i].y - e.y;
            if(Mathf.Sqrt(diffx*diffx+diffy*diffy)<0.1)
            {
                return;
            }
            //if ((int)e.x == D_TIN.DS.Vertex[i].x && (int)e.y == D_TIN.DS.Vertex[i].y)
            //    return;  //若该点已有则不再加入
        }

        //加点            
        D_TIN.DS.Vertex[D_TIN.DS.VerticesNum].x = e.x;
        D_TIN.DS.Vertex[D_TIN.DS.VerticesNum].y = e.y;
        D_TIN.DS.Vertex[D_TIN.DS.VerticesNum].ID = D_TIN.DS.VerticesNum;
        D_TIN.DS.VerticesNum++;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = scene.transform;
        sphere.transform.localPosition = new Vector3(e.x, 0, e.y);
        sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        realnum++;
        //Debug.Log(e);
    }

    private void ShowTriangle()
    {
        if (D_TIN.DS.VerticesNum > 2)  //构建三角网
            D_TIN.CreateTIN();

        //Mesh mesh = plane.GetComponent<MeshFilter>().mesh;
        //mesh.SetVertices(getVertics(D_TIN.DS.Vertex));

        //int[] triangles = new int[D_TIN.DS.TriangleNum * 3];
        //for (int i = 0; i < D_TIN.DS.TriangleNum; i++)
        //{
        //    triangles[3 * i + 0] = (int)D_TIN.DS.Triangle[i].V1Index;
        //    triangles[3 * i + 1] = (int)D_TIN.DS.Triangle[i].V2Index;
        //    triangles[3 * i + 2] = (int)D_TIN.DS.Triangle[i].V3Index;
        //}

        //mesh.triangles = triangles;
        
        //plane.GetComponent<MeshCollider>().sharedMesh = mesh;

        //print(mesh);

        //splitMesh(mesh);

        D_TIN.CalculateBC();
        D_TIN.CreateVoronoi(scene);
        Destroy(plane);
        for (int i = 0; i < D_TIN.polygons.Count; i++)
        {
            setMeshByPolygon(D_TIN.polygons[i]);
        }
       
       
    }

    public void setMeshByPolygon(Polygon polygon)
    {
        if (polygon.points.Count <= 2)
            return;


        Mesh subMesh = new Mesh();
        subMesh.vertices = getVerticesByPolygon(polygon);
        subMesh.triangles = getTrianglesByPolygon(polygon);
        subMesh.uv = getUVByPolygon(polygon);
        subMesh.RecalculateNormals();
        GameObject go = Instantiate(prefab);

        go.GetComponent<MeshFilter>().mesh = subMesh;
        go.GetComponent<MeshCollider>().sharedMesh = subMesh;
        go.GetComponent<MeshCollider>().convex = true;
        go.AddComponent<Rigidbody>();
        go.transform.parent = scene.transform;
        go.transform.localPosition = new Vector3(0, 0, 0);
    }

    private Vector2[] getUVByPolygon(Polygon polygon)
    {
        int verticCount = polygon.points.Count * 2;
        Vector2[] uv = new Vector2[verticCount];
        for (int i = 0; i < polygon.points.Count; i++)
        {
            uv[2 * i] = new Vector2((5f - polygon.points[i].X)/10f, (5f - polygon.points[i].Y)/10f);
            uv[2 * i + 1] = new Vector2((5f - polygon.points[i].X) / 10f, (5f - polygon.points[i].Y) / 10f);
        }
        return uv;
    }


    public Vector3[] getVerticesByPolygon(Polygon polygon)
    {
        polygon.addVertex(getCenter(polygon));
        int verticCount = polygon.points.Count * 2;

        Vector3[] vertics = new Vector3[verticCount];
        for (int i = 0; i < polygon.points.Count; i++)
        {
            vertics[2 * i] = new Vector3(polygon.points[i].X*10, 2, polygon.points[i].Y*10);
            vertics[2 * i + 1] = new Vector3(polygon.points[i].X*10, -2, polygon.points[i].Y*10);
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



    private PointF getCenter(Polygon polygon)
    {
        float x = 0;
        float y = 0;
        for (int i = 0; i < polygon.points.Count; i++)
        {
            x += polygon.points[i].X;
            y += polygon.points[i].Y;
        }
        x /= polygon.points.Count;
        y /= polygon.points.Count;
        return new PointF(x, y);
    }

    public List<Vector3> getVertics(Vertex[] Vertex)
    {
        vertexs = new List<Vector3>();
        for (int i = 0; i < Vertex.Length; i++)
        {
            vertexs.Add(new Vector3( Vertex[i].x,0,Vertex[i].y));
        }
        return vertexs;
    }

    public void printMesh(Mesh mesh)
    {
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Debug.Log(mesh.vertices[i]);
        }


        for (int i = 0; i < mesh.triangles.Length; i++)
        {
            Debug.Log(mesh.triangles[i]);
        }
    }

    public void splitMesh(Mesh mesh)
    {
       
        for (int i = 0; i < mesh.triangles.Length;i+=3)
        {
            Vector3 [] vertices = new Vector3[6];
            vertices[0] = mesh.vertices[mesh.triangles[i + 0]] + Vector3.up;
            vertices[1] = mesh.vertices[mesh.triangles[i + 1]] + Vector3.up;
            vertices[2] = mesh.vertices[mesh.triangles[i + 2]] + Vector3.up;
            vertices[3] = mesh.vertices[mesh.triangles[i + 0]] - Vector3.up;
            vertices[4] = mesh.vertices[mesh.triangles[i + 1]] - Vector3.up;
            vertices[5] = mesh.vertices[mesh.triangles[i + 2]] - Vector3.up;
            //Debug.Log(vertices[0]);
            //Debug.Log(vertices[1]);
            //Debug.Log(vertices[2]);
            //Debug.Log(vertices[3]);
            //Debug.Log(vertices[4]);
            //Debug.Log(vertices[5]);

            //Debug.Log(mesh.triangles[i]);
            Mesh subMesh = new Mesh();
            subMesh.vertices = vertices;
            subMesh.triangles = getTriangles();
            subMesh.RecalculateNormals();
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
          //  go.AddComponent<MeshFilter>();
            go.GetComponent<MeshFilter>().mesh =  subMesh;
            //go.AddComponent<MeshCollider>();
            go.GetComponent<MeshCollider>().sharedMesh = subMesh;
            go.GetComponent<MeshCollider>().convex = true;
            go.AddComponent<Rigidbody>();
            
            go.transform.parent = scene.transform;

        }

    }

    int[] getTriangles()
    {
        int[] triangles = new int[24];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        triangles[3] = 5;
        triangles[4] = 4;
        triangles[5] = 3;
        
        triangles[6] = 3;
        triangles[7] = 1;
        triangles[8] = 0;
        
        triangles[9] = 3;
        triangles[10] = 4;
        triangles[11] = 1;
       
        triangles[12] = 0;
        triangles[13] = 2;
        triangles[14] = 3;
        
        triangles[15] = 2;
        triangles[16] = 5;
        triangles[17] = 3;

        triangles[18] = 1;
        triangles[19] = 5;
        triangles[20] = 2;
        

        triangles[21] = 1;
        triangles[22] = 4;
        triangles[23] = 5;
        return triangles;
    }

    //生成并显示凸壳
    private void ShowConvex(int interval)
    {
        D_TIN.CreateConvex();

        for (int i = 0; i < D_TIN.HullPoint.Count; i++)
        {
           // g.DrawLine(Pens.Black, D_TIN.DS.Vertex[D_TIN.HullPoint[i]].x, D_TIN.DS.Vertex[D_TIN.HullPoint[i]].y,
           //  D_TIN.DS.Vertex[D_TIN.HullPoint[(i + 1) % D_TIN.HullPoint.Count]].x,
            // D_TIN.DS.Vertex[D_TIN.HullPoint[(i + 1) % D_TIN.HullPoint.Count]].y);
           // Thread.Sleep(interval);
        }
    }




}
