using System;
using System.Collections.Generic;
using System.Text;

namespace TINVoronoi
{

    public struct PointF
    {
        private float x, y;

        //
        // 摘要: 
        //     用指定坐标初始化 System.Drawing.PointF 类的新实例。
        //
        // 参数: 
        //   x:
        //     该点的水平位置。
        //
        //   y:
        //     该点的垂直位置。
        public PointF(float x = 0, float y = 0)
        {
            this.x = x;
            this.y = y;
        }



        //
        // 摘要: 
        //     获取或设置此 System.Drawing.PointF 的 x 坐标。
        //
        // 返回结果: 
        //     此 System.Drawing.PointF 的 x 坐标。
        public float X { get { return x; } set { x = value; } }
        //
        // 摘要: 
        //     获取或设置此 System.Drawing.PointF 的 y 坐标。
        //
        // 返回结果: 
        //     此 System.Drawing.PointF 的 y 坐标。
        public float Y { get { return y; } set { y = value; } }


        public bool Equals(PointF obj)
        {
            return this.X == obj.X && this.Y == obj.Y;
        }

        public static PointF zero
        {
            get
            {
                return new PointF(0, 0);
            }
        }

        public string ToString()
        {
            return " " + x + "," + y;
        }

    }

    //离散点
    public struct Vertex
    {
        public long x;
        public long y;
        public long ID;
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
        public long Vertex1ID;   //点索引
        public long Vertex2ID;
        public Boolean NotHullEdge;  //非凸壳边
        public long AdjTriangle1ID;
        public long AdjacentT1V3;    //△1的第三顶点在顶点数组的索引
        public long AdjTriangle2ID;

        public Edge(long iV1, long iV2)
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
        public long V1Index; //点在链表中的索引值
        public long V2Index;
        public long V3Index;
    }

    //外接圆心
    public struct Barycenter
    {
        public double X;
        public double Y;
        public PointF point
        {
            get
            {
                return new PointF((float)X, (float)Y);
            }
        }
    }


    public class BoundaryBox
    {
        public long XLeft;
        public long YTop;
        public long XRight;
        public long YBottom;
    }

    public class Polygon
    {
        //public int VertexNum;
        public List<PointF> points = new List<PointF>();

        public Polygon()
        {

        }
        public void addVertex(PointF vertex)
        {
            points.Add(vertex);
        }
    }


    public class DataStruct
    {
        public static int MaxVertices = 500;
        public static int MaxEdges = 2000;
        public static int MaxTriangles = 100;
        public static int MaxPolygons = 100;
        public Vertex[] Vertex = new Vertex[MaxVertices];
        public Triangle[] Triangle = new Triangle[MaxTriangles];
        public Barycenter[] Barycenters = new Barycenter[MaxTriangles]; //外接圆心
        public Edge[] TinEdges = new Edge[MaxEdges];
        public BoundaryBox BBOX = new BoundaryBox();  //图副边界框
        public Polygon[] Polygon = new Polygon[MaxPolygons];
        //public PointF [] pointList = new PointF[];
        public bool[,] connectMap = new bool[MaxTriangles, MaxTriangles];
        public int VerticesNum = 0;
        public int TinEdgeNum = 0;
        public int TriangleNum = 0;
        public int PolygonNum = 0;
    }

}
