using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace TINVoronoi
{


    //离散点
    public struct Vertex
    {
        public float x;
        public float y;
        public int ID;
        public int isHullEdge; //凸壳顶点标记,系统初始化为0

        //相等则返回true
        public static bool Compare(Vertex a, Vertex b)
        {
            return a.x == b.x && a.y == b.y;
        }
    }

    //边
    public struct Edge
    {
        public int Vertex1ID;   //点索引
        public int Vertex2ID;
        public Boolean NotHullEdge;  //非凸壳边
        public int AdjTriangle1ID;
        public int AdjacentT1V3;    //△1的第三顶点在顶点数组的索引
        public int AdjTriangle2ID;

        public Edge(int iV1, int iV2)
        {
            Vertex1ID = iV1;
            Vertex2ID = iV2;
            NotHullEdge = false;
            AdjTriangle1ID = 0;
            AdjTriangle2ID = 0;
            AdjacentT1V3 = 0;
        }

        //相等则返回true
        public static bool Compare(Edge a, Edge b)
        {
            return ((a.Vertex1ID == b.Vertex1ID) && (a.Vertex2ID == b.Vertex2ID)) ||
                ((a.Vertex1ID == b.Vertex2ID) && (a.Vertex2ID == b.Vertex1ID));
        }

    }

    //三角形
    public struct Triangle
    {
        public int V1Index; //点在链表中的索引值
        public int V2Index;
        public int V3Index;
    }

    //外接圆心
    public struct Barycenter
    {
        public float X;
        public float Y;

        public Vector2 point
        {
            get
            {
                return new Vector2((float)X, (float)Y);
            }
        }
    }


    public class BoundaryBox
    {
        public float XLeft = -50f;
        public float YTop = -50f;
        public float XRight = 50f;
        public float YBottom = 50f;
        public float width = 100f;
        public float height = 100f;
    }

    public class Polygon
    {
        //public int VertexNum;
        public List<Vector2> points = new List<Vector2>();

        public Polygon()
        {

        }
        public void addVertex(Vector2 vertex)
        {
            points.Add(vertex);
        }
    }


    public class DataStruct
    {
        public static int MaxVertices = 500;
        public static int MaxEdges = 2000;
        public static int MaxTriangles = 20;
        public static int MaxPolygons = 100;
        public Vertex[] Vertex = new Vertex[MaxVertices];
        public Triangle[] Triangle = new Triangle[MaxTriangles];
        public Barycenter[] Barycenters = new Barycenter[MaxTriangles]; //外接圆心
        public Edge[] TinEdges = new Edge[MaxEdges];
        public BoundaryBox BBOX = new BoundaryBox();  //图副边界框
        public bool[,] connectMap = new bool[MaxTriangles, MaxTriangles];
        public int VerticesNum = 0;
        public int TinEdgeNum = 0;
        public int TriangleNum = 0;
    }

}
