using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TINVoronoi
{
    partial class Delaynay
    {
        public DataStruct DS = new DataStruct();  //数据结构
        //List<PointF> pointsList  = new List<PointF>();
        public List<Vector2> startIndexs = new List<Vector2>();
        public List<Polygon> polygons = new List<Polygon>();

        //构建并显示Voronoi图
        public void CreateVoronoi()
        {

            //可以用来起始搜索的

            for (int i = 0; i < DS.TinEdgeNum; i++)
            {
                if (!DS.TinEdges[i].NotHullEdge) //△边为凸壳边
                {
                    Vector2 endPnt = getEndPntVorEdge(i);
                    //Debug.Log(endPnt.ToString());
                    if (!endPnt.Equals(Vector2.zero))
                    {
                        //起始
                        int index = DS.TinEdges[i].AdjTriangle1ID;

                        DS.connectMap[index, DS.TriangleNum] = true;
                        DS.connectMap[DS.TriangleNum, index] = true;
                        startIndexs.Add(new Vector2(DS.TriangleNum, index));
                        Barycenter barycenter = new Barycenter();
                        barycenter.X = endPnt.x;
                        barycenter.Y = endPnt.y;

                        DS.Barycenters[DS.TriangleNum] = barycenter;
                        DS.TriangleNum++;
                    }

                }
            }

            //for (int i = 0; i < DS.VerticesNum; i++)
            //{
            //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //    go.transform.localScale = new Vector3(5, 5, 5);
            //    go.transform.localPosition = new Vector3(DS.Vertex[i].x * 10, 0, DS.Vertex[i].y * 10);
            //    go.GetComponent<MeshRenderer>().material.color = Color.white;
            //    go.transform.SetParent(scene.transform);

            //}

            //for (int i = 0; i < DS.TriangleNum; i++)
            //{
            //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //    go.transform.localScale = new Vector3(5, 5, 5);
            //    go.transform.localPosition = new Vector3(DS.Barycenters[i].X*10, 0, DS.Barycenters[i].Y*10);
            //    go.GetComponent<MeshRenderer>().material.color = Color.black;
            //    go.transform.SetParent(scene.transform);
            //}

            for (int i = 0; i < startIndexs.Count; i++)
            {
                Polygon polygon = new Polygon();
                int lastIndex = (int)startIndexs[i].x;
                int currIndex = (int)startIndexs[i].y;

                Vector2 lastPoint = DS.Barycenters[lastIndex].point;
                Vector2 currPoint = DS.Barycenters[currIndex].point;
                polygon.addVertex(lastPoint);
                polygon.addVertex(currPoint);
                polygons.Add(polygon);
                DS.connectMap[lastIndex, currIndex] = false;
                searchMap(lastIndex, lastIndex, currIndex);
            }

            for (int i = 0; i < DS.TriangleNum; i++)
            {
                for (int j = 0; j < DS.TriangleNum; j++)
                {
                    if (DS.connectMap[i, j])
                    {
                        Vector2 lastPoint = DS.Barycenters[i].point;
                        Vector2 currPoint = DS.Barycenters[j].point;
                        if (PointInBox(lastPoint) && PointInBox(currPoint))
                        {
                            Polygon polygon = new Polygon();
                            polygon.addVertex(lastPoint);
                            polygon.addVertex(currPoint);
                            polygons.Add(polygon);
                            DS.connectMap[i, j] = false;
                            searchMap(i, i, j);
                        }

                    }
                }
            }
            for (int i = 0; i < polygons.Count; i++)
            {
                Polygon polygon = polygons[i];
                modifyPolygon(ref polygon);
            }
        }



        public void searchMap(int startIndex, int lastIndex, int currIndex)
        {
            Vector2 lastPoint = DS.Barycenters[lastIndex].point;
            Vector2 currPoint = DS.Barycenters[currIndex].point;

            //Debug.Log(currPoint);
            for (int nextIndex = 0; nextIndex < DS.TriangleNum; nextIndex++)
            {

                Vector2 nextPoint = DS.Barycenters[nextIndex].point;
                if (DS.connectMap[currIndex, nextIndex] &&//如果是联通的
                    lastIndex != nextIndex && PointInBox(nextPoint)) //不是前面一个index
                {
                    //搜到起始点了
                    if (nextIndex == startIndex)
                        return;

                    if (VectorXMultiply(lastPoint, currPoint, nextPoint)<0)
                    {
                        DS.connectMap[currIndex, nextIndex] = false;
                        polygons[polygons.Count - 1].addVertex(nextPoint);
                        searchMap(startIndex, currIndex, nextIndex);
                    }
                }
            }
        }




        private void modifyPolygon(ref Polygon polygon)
        {
            Vector2 first = polygon.points[0];
            Vector2 last = polygon.points[polygon.points.Count - 1];
            int firstIndex = getEdgeIndex(first);
            int lastIndex = getEdgeIndex(last);


            if (firstIndex == lastIndex)
                return;

            if (firstIndex % 2 == 1 && lastIndex % 2 == 1)
            {
                Vector2 point = new Vector2();
                point.x = getBoundary(0);
                point.y = getBoundary(3);
                if (VectorXMultiply(point, first, polygon.points[2])<0)
                {
                    polygon.addVertex(new Vector2(getBoundary(0), getBoundary(1)));
                    polygon.addVertex(new Vector2(getBoundary(0), getBoundary(3)));
                }
                else
                {
                    polygon.addVertex(new Vector2(getBoundary(2), getBoundary(3)));
                    polygon.addVertex(new Vector2(getBoundary(2), getBoundary(1)));
                }
            }
            if (firstIndex % 2 == 0 && lastIndex % 2 == 0)
            {
                Vector2 point = new Vector2();
                point.x = getBoundary(2);
                point.y = getBoundary(3);
                if (VectorXMultiply(point, first, polygon.points[2])<0)
                {
                    polygon.addVertex(new Vector2(getBoundary(0), getBoundary(3)));
                    polygon.addVertex(new Vector2(getBoundary(2), getBoundary(3)));

                }
                else
                {
                    polygon.addVertex(new Vector2(getBoundary(2), getBoundary(1)));
                    polygon.addVertex(new Vector2(getBoundary(0), getBoundary(1)));
                }
            }

            if (firstIndex % 2 == 0 && lastIndex % 2 == 1)
            {
                Vector2 point = new Vector2();
                point.x = getBoundary(firstIndex);
                point.y = getBoundary(lastIndex);
                polygon.addVertex(point);
            }

            if (firstIndex % 2 == 1 && lastIndex % 2 == 0)
            {
                Vector2 point = new Vector2();
                point.x = getBoundary(lastIndex);
                point.y = getBoundary(firstIndex);
                polygon.addVertex(point);
            }
        }


        float getBoundary(int index)
        {
            if (index == 0)
            {
                return DS.BBOX.XLeft;
            }
            else if (index == 1)
            {
                return DS.BBOX.YTop;
            }
            else if (index == 2)
            {
                return DS.BBOX.XRight;
            }
            else
            {
                return DS.BBOX.YBottom;
            }
        }


        int getEdgeIndex(Vector2 point)
        {
            int edgeIndex = -1;
            if (Math.Abs(point.x - DS.BBOX.XLeft) < 0.00001)
            {
                edgeIndex = 0;
            }
            else if (Math.Abs(point.y - DS.BBOX.YTop) < 0.00001)
            {
                edgeIndex = 1;
            }
            else if (Math.Abs(point.x - DS.BBOX.XRight) < 0.00001)
            {
                edgeIndex = 2;
            }
            else if (Math.Abs(point.y - DS.BBOX.YBottom) < 0.00001)
            {
                edgeIndex = 3;
            }
            return edgeIndex;
        }

  


        //增量法生成Delaunay三角网
        public void CreateTIN()
        {
            //建立凸壳并三角剖分
            CreateConvex();

            HullTriangulation();

            //逐点插入
            PlugInEveryVertex();

            //建立边的拓扑结构
            TopologizeEdge();
        }

        //逐点加入修改TIN
        private void PlugInEveryVertex()
        {
            Edge[] EdgesBuf = new Edge[DataStruct.MaxTriangles];  //△边缓冲区

            bool IsInCircle;
            int i, j, k;
            int EdgeCount;
            for (i = 0; i < DS.VerticesNum; i++)    //逐点加入
            {
                //跳过凸壳顶点
                if (DS.Vertex[i].isHullEdge != 0)
                    continue;

                EdgeCount = 0;
                for (j = 0; j < DS.TriangleNum; j++) //定位待插入点影响的所有△
                {
                    IsInCircle = InTriangleExtCircle(DS.Vertex[i].x, DS.Vertex[i].y, DS.Vertex[DS.Triangle[j].V1Index].x, DS.Vertex[DS.Triangle[j].V1Index].y,
                        DS.Vertex[DS.Triangle[j].V2Index].x, DS.Vertex[DS.Triangle[j].V2Index].y,
                        DS.Vertex[DS.Triangle[j].V3Index].x, DS.Vertex[DS.Triangle[j].V3Index].y);
                    if (IsInCircle)    //△j在影响范围内
                    {
                        Edge[] eee ={new Edge(DS.Triangle[j].V1Index, DS.Triangle[j].V2Index),
                            new Edge(DS.Triangle[j].V2Index, DS.Triangle[j].V3Index),
                            new Edge(DS.Triangle[j].V3Index, DS.Triangle[j].V1Index)};  //△的三边

                        #region 存储除公共边外的△边
                        bool IsNotComnEdge;
                        for (k = 0; k < 3; k++)
                        {
                            IsNotComnEdge = true;
                            for (int n = 0; n < EdgeCount; n++)
                            {
                                if (Edge.Compare(eee[k], EdgesBuf[n]))   //此边为公共边
                                {
                                    //删除已缓存的公共边
                                    IsNotComnEdge = false;
                                    EdgesBuf[n] = EdgesBuf[EdgeCount - 1];
                                    EdgeCount--;
                                    break;
                                }
                            }

                            if (IsNotComnEdge)
                            {
                                EdgesBuf[EdgeCount] = eee[k];    //边加入Buffer
                                EdgeCount++;
                            }
                        }
                        #endregion

                        //删除△j, 表尾△前移插入
                        DS.Triangle[j].V1Index = DS.Triangle[DS.TriangleNum - 1].V1Index;
                        DS.Triangle[j].V2Index = DS.Triangle[DS.TriangleNum - 1].V2Index;
                        DS.Triangle[j].V3Index = DS.Triangle[DS.TriangleNum - 1].V3Index;
                        j--;
                        DS.TriangleNum--;
                    }
                }//for 定位点

                #region 构建新△
                for (j = 0; j < EdgeCount; j++)
                {
                    DS.Triangle[DS.TriangleNum].V1Index = EdgesBuf[j].Vertex1ID;
                    DS.Triangle[DS.TriangleNum].V2Index = EdgesBuf[j].Vertex2ID;
                    DS.Triangle[DS.TriangleNum].V3Index = i;
                    DS.TriangleNum++;
                }
                #endregion
            }//逐点加入for
        }

        public enum RandomType
        {
            circle = 0,
            rect = 1,
        }

        public Vector3 pos;

        public void ShowBroken(Vector3 pos)
        {
            this.pos = pos;
            createRandomVertexs(new Vector2(pos.x, pos.z), 6, Delaynay.RandomType.circle, 20f);
            CreateTIN();
            CalculateBC();
        }

        public void createRandomVertexs(Vector2 center, int num, RandomType randomType, float size)
        {
            while (DS.VerticesNum < num)
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
                //Debug.Log(pos);
                addVertex(pos);
            }

        }


        public void addVertex(Vector2 e)
        {
            for (int i = 0; i < DS.VerticesNum; i++)
            {
                float diffx = DS.Vertex[i].x - e.x;
                float diffy = DS.Vertex[i].y - e.y;
                if (Mathf.Sqrt(diffx * diffx + diffy * diffy) < 0.1 ||
                    !PointInBox(e))
                {
                    return;
                }
            }

            //加点            
            DS.Vertex[DS.VerticesNum].x = e.x;
            DS.Vertex[DS.VerticesNum].y = e.y;
            DS.Vertex[DS.VerticesNum].ID = DS.VerticesNum;
            DS.VerticesNum++;
        }


        //计算外接圆圆心
        public void CalculateBC()
        {
            float x1, y1, x2, y2, x3, y3;
            for (int i = 0; i < DS.TriangleNum; i++)
            {
                //计算△的外接圆心
                x1 = DS.Vertex[DS.Triangle[i].V1Index].x;
                y1 = DS.Vertex[DS.Triangle[i].V1Index].y;
                x2 = DS.Vertex[DS.Triangle[i].V2Index].x;
                y2 = DS.Vertex[DS.Triangle[i].V2Index].y;
                x3 = DS.Vertex[DS.Triangle[i].V3Index].x;
                y3 = DS.Vertex[DS.Triangle[i].V3Index].y;
               
                GetTriangleBarycnt(x1, y1, x2, y2, x3, y3, ref DS.Barycenters[i].X, ref DS.Barycenters[i].Y);
                if(!PointInBox(DS.Barycenters[i].point))
                {
                    Debug.Log("重来");
                    DS = new DataStruct();
                    createRandomVertexs(pos, 6, Delaynay.RandomType.circle, 2f);
                    CreateTIN();
                    CalculateBC();
                }
            }

        }
    
        public void clearConnectWithOutBoxPoint()
        {
            for(int i = 0; i < DS.TriangleNum; i++)
            {
                if (!PointInBox(DS.Barycenters[i].point))
                {
                    for (int j = 0; j < DS.TriangleNum; j++)
                    {
                        DS.connectMap[i, j] = false;
                        DS.connectMap[j, i] = false;
                    }
                }
            }
        }

        //求△的外接圆心
        private void GetTriangleBarycnt(float x1, float y1, float x2, float y2, float x3, float y3, ref float bcX, ref float bcY)
        {
            float precision = 0.000001f;
            float k1, k2;   //两条中垂线斜率

            //三点共线
            if (Math.Abs(y1 - y2) < precision && Math.Abs(y2 - y3) < precision)
            {
                Debug.Log("三点共线");
            }

            //边的中点
            float MidX1 = (x1 + x2) / 2;
            float MidY1 = (y1 + y2) / 2;
            float MidX2 = (x3 + x2) / 2;
            float MidY2 = (y3 + y2) / 2;

            if (Math.Abs(y2 - y1) < precision)  //p1p2平行于X轴
            {
                k2 = -(x3 - x2) / (y3 - y2);
                bcX = MidX1;
                bcY = k2 * (bcX - MidX2) + MidY2;
                if (!PointInBox(new Vector2(bcX, bcY)))
                {
                    if (bcY > DS.BBOX.YBottom)
                        bcY = DS.BBOX.YBottom;
                    if (bcY < DS.BBOX.YTop)
                        bcY = DS.BBOX.YTop;
                }
            }
            else if (Math.Abs(y3 - y2) < precision)   //p2p3平行于X轴
            {
                k1 = -(x2 - x1) / (y2 - y1);
                bcX = MidX2;
                bcY = k1 * (bcX - MidX1) + MidY1;
                if (bcY > DS.BBOX.YBottom)
                    bcY = DS.BBOX.YBottom;
                if (bcY < DS.BBOX.YTop)
                    bcY = DS.BBOX.YTop;

            }
            else
            {
                k1 = -(x2 - x1) / (y2 - y1);
                k2 = -(x3 - x2) / (y3 - y2);
                bcX = (k1 * MidX1 - k2 * MidX2 + MidY2 - MidY1) / (k1 - k2);
                bcY = k1 * (bcX - MidX1) + MidY1;
            }
            return;

            //if (!PointInBox(new Vector2(bcX, bcY)))
            //{
            //    Debug.Log("adjust");
            //    float d1 = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
            //    float d2 = Vector2.Distance(new Vector2(x2, y2), new Vector2(x3, y3));
            //    float d3 = Vector2.Distance(new Vector2(x3, y3), new Vector2(x1, y1));

            //    float k = 0;
            //    float midX =0;
            //    float midY = 0;
            //    if (d1 > d2 && d1 > d3)
            //    {
            //        midX = (x2 + x3) / 2;
            //        midY = (y3 + y3) / 2;
            //        k = -(x2 - x3) / (y2 - y3);
            //    }
            //    else if (d2 > d3 && d2 > d1)
            //    {
            //        midX = (x1 + x3) / 2;
            //        midY = (y1 + y3) / 2;
            //    }
            //    else
            //    {
            //        midX = (x1 + x2) / 2;
            //        midY = (y1 + y2) / 2;
            //    }

            //    float y = k * (DS.BBOX.XLeft - midX) + midY;
            //    if (y >= DS.BBOX.YTop && y <= DS.BBOX.YBottom){
            //        bcX = DS.BBOX.XLeft;
            //        bcY = y;
            //        Debug.Log("" + bcX + ":" +  bcY);
            //        return;
            //    }
            //    y = k * (DS.BBOX.XRight - midX) + midY;
            //    if (y >= DS.BBOX.YTop && y <= DS.BBOX.YBottom)
            //    {
            //        bcX = DS.BBOX.XRight;
            //        bcY = y;
            //        Debug.Log("" + bcX + ":" + bcY);
            //        return;
            //    }

            //    float x = (DS.BBOX.YTop - midY) / k + midX;
            //    if (x >= DS.BBOX.XLeft && x <= DS.BBOX.XRight)
            //    {
            //        bcX = x;
            //        bcY = DS.BBOX.YTop;
            //        Debug.Log("" + bcX + ":" + bcY);
            //        return;
            //    }
            //    x = (DS.BBOX.YBottom - midY) / k + midX;
            //    if (x >= DS.BBOX.XLeft && x <= DS.BBOX.XRight)
            //    {
            //        bcX = x;
            //        bcY = DS.BBOX.YBottom;
            //        Debug.Log("" + bcX + ":" + bcY);
            //        return;
            //    }
            //}

            //Debug.Log("BC:" + bcX + "," + bcY);
        }

        //判断点是否在△的外接圆中
        private Boolean InTriangleExtCircle(float xp, float yp, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            float RadiusSquare;    //半径的平方
            float DisSquare;  //距离的平方
            float BaryCntX = 0, BaryCntY = 0;
            GetTriangleBarycnt(x1, y1, x2, y2, x3, y3, ref  BaryCntX, ref BaryCntY);

            RadiusSquare = (x1 - BaryCntX) * (x1 - BaryCntX) + (y1 - BaryCntY) * (y1 - BaryCntY);
            DisSquare = (xp - BaryCntX) * (xp - BaryCntX) + (yp - BaryCntY) * (yp - BaryCntY);

            if (DisSquare <= RadiusSquare)
                return true;
            else
                return false;
        }


        public void TopologizeEdge()
        {
            DS.TinEdgeNum = 0;
            DS.TinEdges = new Edge[DataStruct.MaxEdges];   //清除旧数据
            int[] Vindex = new int[3]; //3个顶点索引

            //遍历每个△的三条边
            for (int i = 0; i < DS.TriangleNum; i++)
            {
                Vindex[0] = DS.Triangle[i].V1Index;
                Vindex[1] = DS.Triangle[i].V2Index;
                Vindex[2] = DS.Triangle[i].V3Index;

                for (int j = 0; j < 3; j++)   //每条边
                {
                    Edge e = new Edge(Vindex[j], Vindex[(j + 1) % 3]);

                    //判断边在数组中是否已存在
                    int k;
                    for (k = 0; k < DS.TinEdgeNum; k++)
                    {
                        if (Edge.Compare(e, DS.TinEdges[k]))   //此边已构造
                        {
                            DS.TinEdges[k].AdjTriangle2ID = i;
                            DS.TinEdges[k].NotHullEdge = true;
                            int index1 = DS.TinEdges[k].AdjTriangle1ID;
                            int index2 = DS.TinEdges[k].AdjTriangle2ID;
                            DS.connectMap[index1, index2] = true;
                            DS.connectMap[index2, index1] = true;

                            break;
                        }
                    }//for

                    if (k == DS.TinEdgeNum)   //此边为新边
                    {
                        DS.TinEdges[DS.TinEdgeNum].Vertex1ID = e.Vertex1ID;
                        DS.TinEdges[DS.TinEdgeNum].Vertex2ID = e.Vertex2ID;
                        DS.TinEdges[DS.TinEdgeNum].AdjTriangle1ID = i;
                        DS.TinEdges[DS.TinEdgeNum].AdjacentT1V3 = Vindex[(j + 2) % 3];
                        DS.TinEdgeNum++;
                    }

                }//for,每条边
            }//for,每个△
        }



        private Vector2 getEndPntVorEdge(int i)
        {
            Vector2 pnt1 = new Vector2(Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex1ID].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex1ID].y));
            Vector2 pnt2 = new Vector2(Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex2ID].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex2ID].y));
            Vector2 pnt3 = new Vector2(Convert.ToSingle(DS.Vertex[DS.TinEdges[i].AdjacentT1V3].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[i].AdjacentT1V3].y));    //边对应的△顶点
            Vector2 MidPnt = new Vector2((pnt1.x + pnt2.x) / 2, (pnt2.y + pnt2.y) / 2);  //TinEdge中点
            Vector2 BaryCnt = new Vector2(Convert.ToSingle(DS.Barycenters[DS.TinEdges[i].AdjTriangle1ID].X),
                Convert.ToSingle(DS.Barycenters[DS.TinEdges[i].AdjTriangle1ID].Y));  //外接圆心
            Vector2 EndPnt = new Vector2();   //圆心连接于此点构成VEdge

            //Debug.Log(BaryCnt.ToString());

            //圆心在box外则直接跳过
            //if (!PointInBox(BaryCnt))
            //    return EndPnt;

            //求斜率
            float k = 0;  //斜率
            bool KExist = true;
            if (Math.Abs(pnt1.y - pnt2.y) < 0.000001)
                KExist = false;     //k不存在
            else
                k = (pnt1.x - pnt2.x) / (pnt2.y - pnt1.y);

            //该凸壳边是△的钝角边则外接圆心在△外
            bool obtEdge = IsObtuseEdge(i);

            #region 根据△圆心在凸壳内还是在外求VEdge
            //圆心在边右则往左延伸，在左则往右

            if (!obtEdge)   //圆心在凸壳内(或边界上)/////////////////////////////////
            {
                if (!KExist)    //k不存在
                {
                    // MessageBox.Show("斜率不存在的△-"+DS.TinEdges[i].AdjTriangle1ID.ToString());
                    if (BaryCnt.y > MidPnt.y || BaryCnt.y < pnt3.y)// BaryCnt<y3 ->圆心与中点重合
                        EndPnt.y = DS.BBOX.YTop;
                    else if (BaryCnt.y < MidPnt.y || BaryCnt.y > pnt3.y)
                        EndPnt.y = DS.BBOX.YBottom;

                    EndPnt.x = BaryCnt.x;
                }
                else      //K存在
                {
                    if (BaryCnt.x > MidPnt.x || (BaryCnt.x == MidPnt.x && BaryCnt.x < pnt3.x))
                        EndPnt.x = DS.BBOX.XLeft;
                    else if (BaryCnt.x < MidPnt.x || (BaryCnt.x == MidPnt.x && BaryCnt.x > pnt3.x))
                        EndPnt.x = DS.BBOX.XRight;

                    EndPnt.y = k * (EndPnt.x - BaryCnt.x) + BaryCnt.y;
                }

            }
            else    //圆心在凸壳外/////////////////////////////////////////////
            {
                if (!KExist)    //k不存在
                {
                    if (BaryCnt.y < MidPnt.y || BaryCnt.y < pnt3.y)
                        EndPnt.y = DS.BBOX.YTop;
                    else if (BaryCnt.y > MidPnt.y || BaryCnt.y > pnt3.y)
                        EndPnt.y = DS.BBOX.YBottom;

                    EndPnt.x = BaryCnt.x;
                }
                else   //K存在
                {
                    if (BaryCnt.x < MidPnt.x)
                        EndPnt.x = DS.BBOX.XLeft;
                    else if (BaryCnt.x > MidPnt.x)
                        EndPnt.x = DS.BBOX.XRight;

                    EndPnt.y = k * (EndPnt.x - BaryCnt.x) + BaryCnt.y;
                }

            }//else 在△外

            //与外框交点在边界外的处理
            if (k != 0 && KExist)
            {
                if (EndPnt.y < DS.BBOX.YTop)
                    EndPnt.y = DS.BBOX.YTop;
                else if (EndPnt.y > DS.BBOX.YBottom)
                    EndPnt.y = DS.BBOX.YBottom;

                EndPnt.x = (EndPnt.y - BaryCnt.y) / k + BaryCnt.x;
            }

            #endregion

            // g.DrawLine(new Pen(Color.Blue, 2), BaryCnt, EndPnt);
            return EndPnt;
        }


        //index为TinEdge的索引号,若为钝角边则返回true
        private bool IsObtuseEdge(int index)
        {
            Vector2 EdgePnt1 = new Vector2(Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex1ID].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex1ID].y));
            Vector2 EdgePnt2 = new Vector2(Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex2ID].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex2ID].y));
            Vector2 Pnt3 = new Vector2(Convert.ToSingle(DS.Vertex[DS.TinEdges[index].AdjacentT1V3].x),
                 Convert.ToSingle(DS.Vertex[DS.TinEdges[index].AdjacentT1V3].y));

            Vector2 V1 = new Vector2((EdgePnt1.x - Pnt3.x), (EdgePnt1.y - Pnt3.y));
            Vector2 V2 = new Vector2((EdgePnt2.x - Pnt3.x), (EdgePnt2.y - Pnt3.y));
            return (V1.x * V2.x + V1.y * V2.y) < 0; //a·b的值<0则为钝角
        }

        //？？？？？？Unused？点在△内则返回true
        private bool PointInTriganle(int PntIndex, int index)
        {
            Vector2 pnt1 = new Vector2(Convert.ToSingle(DS.Vertex[DS.Triangle[index].V1Index].x),
                Convert.ToSingle(DS.Vertex[DS.Triangle[index].V1Index].y));
            Vector2 pnt2 = new Vector2(Convert.ToSingle(DS.Vertex[DS.Triangle[index].V2Index].x),
                Convert.ToSingle(DS.Vertex[DS.Triangle[index].V2Index].y));
            Vector2 pnt3 = new Vector2(Convert.ToSingle(DS.Vertex[DS.Triangle[index].V3Index].x),
                 Convert.ToSingle(DS.Vertex[DS.Triangle[index].V3Index].y));
            Vector2 JudgePoint = new Vector2(Convert.ToSingle(DS.Barycenters[index].X), Convert.ToSingle(DS.Barycenters[index].Y));  //外接圆心

            int IsPositive;    //正则等于1，负则等于-1
            float result = VectorXMultiply(JudgePoint, pnt1, pnt2);
            if (result > 0)
                IsPositive = 1;
            else
                IsPositive = -1;

            result = VectorXMultiply(JudgePoint, pnt2, pnt3);
            if ((IsPositive == 1 && result < 0) || (IsPositive == -1 && result > 0))
                return false;

            result = VectorXMultiply(JudgePoint, pnt3, pnt1);
            if ((IsPositive == 1 && result > 0) || (IsPositive == -1 && result < 0))
                return true;
            else
                return false;
        }

        private float VectorXMultiply(Vector2 BaryCnt, Vector2 pnt1, Vector2 pnt2)
        {
            Vector2 V1 = new Vector2((pnt1.x - BaryCnt.x), (pnt1.y - BaryCnt.y));
            Vector2 V2 = new Vector2((pnt2.x - BaryCnt.x), (pnt2.y - BaryCnt.y));
            return (V1.x * V2.y - V2.x * V1.y);
        }
        public bool PointInBox(Vector2 point)
        {
            //Debug.Log(point);
            return (point.x >= DS.BBOX.XLeft && point.x <= DS.BBOX.XRight &&
                    point.y >= DS.BBOX.YTop && point.y <= DS.BBOX.YBottom);
        }


    }
}


