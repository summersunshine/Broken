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


    GameObject scene;
    GameObject plane;

    Delaynay D_TIN = new Delaynay(); //核心功能类


    void Awake()
    {
        plane = GameObject.Find("Plane");
        scene = GameObject.Find("Scene");
    }

   
	// Use this for initialization
	void Start () {
        D_TIN.DS.BBOX.XLeft = -50;
        D_TIN.DS.BBOX.YTop = -50;
        D_TIN.DS.BBOX.XRight = 50;
        D_TIN.DS.BBOX.YBottom = 50;

        Mesh mesh = plane.GetComponent<MeshFilter>().mesh;

        createRandomVertexs(5, RandomType.circle, 10);
        //addVertex(new Vector2(50, 50));
        //addVertex(new Vector2(50, -50));
        //addVertex(new Vector2(-50, 50));
        //addVertex(new Vector2(-50, -50));
        ShowTriangle();
	}

    public void createRandomVertexs(int num,RandomType randomType,int size)
    {
        for (int i = 0; i < num; i++)
        {
            Vector2 pos = Vector2.zero;
            if (randomType == RandomType.circle)
            {
                pos = size * UnityEngine.Random.insideUnitCircle;
            }
            else if(randomType == RandomType.rect)
            {
                pos = new Vector2(UnityEngine.Random.Range(-size, size), UnityEngine.Random.Range(-size, size));
            }
            addVertex(pos);
        }

    }

	

    public void addVertex(Vector2 e)
    {
        for (int i = 0; i < D_TIN.DS.VerticesNum; i++)
        {
            if ((long)e.x == D_TIN.DS.Vertex[i].x && (long)e.y == D_TIN.DS.Vertex[i].y)
                return;  //若该点已有则不再加入
        }

        //加点            
        D_TIN.DS.Vertex[D_TIN.DS.VerticesNum].x = (long)e.x;
        D_TIN.DS.Vertex[D_TIN.DS.VerticesNum].y = (long)e.y;
        D_TIN.DS.Vertex[D_TIN.DS.VerticesNum].ID = D_TIN.DS.VerticesNum;
        D_TIN.DS.VerticesNum++;

        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.parent = plane.transform;
        //sphere.transform.localPosition = new Vector3(e.x, 0, e.y);
        //sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //Debug.Log(e);
    }

    private void ShowTriangle()
    {
        if (D_TIN.DS.VerticesNum > 2)  //构建三角网
            D_TIN.CreateTIN();

        Mesh mesh = plane.GetComponent<MeshFilter>().mesh;
        mesh.SetVertices(getVertics(D_TIN.DS.Vertex));

        int[] triangles = new int[D_TIN.DS.TriangleNum * 3];
        for (int i = 0; i < D_TIN.DS.TriangleNum; i++)
        {
            triangles[3 * i + 0] = (int)D_TIN.DS.Triangle[i].V1Index;
            triangles[3 * i + 1] = (int)D_TIN.DS.Triangle[i].V2Index;
            triangles[3 * i + 2] = (int)D_TIN.DS.Triangle[i].V3Index;
        }

        mesh.triangles = triangles;
        
        plane.GetComponent<MeshCollider>().sharedMesh = mesh;

        print(mesh);

        splitMesh(mesh);

        D_TIN.CalculateBC();
        D_TIN.CreateVoronoi(scene);
       // plane.SetActive(false);
       
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
