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
        List<PointF> startIndexs = new List<PointF>();
        public List<Polygon> polygons = new List<Polygon>();

        //构建并显示Voronoi图
        public void CreateVoronoi(GameObject scene)
        {

            //可以用来起始搜索的

            for (int i = 0; i < DS.TinEdgeNum; i++)
            {
                if (!DS.TinEdges[i].NotHullEdge) //△边为凸壳边
                {
                    PointF endPnt = getEndPntVorEdge(i);
                    //Debug.Log(endPnt.ToString());
                    if (!endPnt.Equals(new PointF(0,0)))
                    {
                        //起始
                        int index = DS.TinEdges[i].AdjTriangle1ID;

                        DS.connectMap[index, DS.TriangleNum] = true;
                        DS.connectMap[DS.TriangleNum, index] = true;
                        startIndexs.Add(new PointF(DS.TriangleNum, index));
                        Barycenter barycenter = new Barycenter();
                        barycenter.X = endPnt.X;
                        barycenter.Y = endPnt.Y;
                        barycenter.isOutBox = false;
                        DS.Barycenters[DS.TriangleNum] = barycenter;
                        DS.TriangleNum++;
                    }

                }
            }


            



            for (int i = 0; i < startIndexs.Count; i++)
            {
                Polygon polygon = new Polygon();
                int lastIndex = (int)startIndexs[i].X;
                int currIndex = (int)startIndexs[i].Y;

                PointF lastPoint = DS.Barycenters[lastIndex].point;
                PointF currPoint = DS.Barycenters[currIndex].point;
                polygon.addVertex(lastPoint);
                polygon.addVertex(currPoint);
                polygons.Add(polygon);
                searchMap(lastIndex, lastIndex, currIndex);
            }

            for (int i = 0; i < DS.TriangleNum; i++)
            {
                for (int j = 0; j < DS.TriangleNum; j++)
                {
                    if (DS.connectMap[i,j])
                    {
                        Polygon polygon = new Polygon();
                        PointF lastPoint = DS.Barycenters[i].point;
                        PointF currPoint = DS.Barycenters[j].point;
                        polygon.addVertex(lastPoint);
                        polygon.addVertex(currPoint);
                        polygons.Add(polygon);
                        searchMap(i, i, j);
                    }
                }
            }

                //for (int i = 0; i < DS.TriangleNum;i++ )
                //{
                //    int x = (int)DS.Barycenters[i].X;
                //    int y = (int)DS.Barycenters[i].Y;
                //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //    go.transform.localPosition = new Vector3(x, 0, y);
                //    go.transform.parent = scene.transform;
                //    go.name = i.ToString();
                //}

                for (int i = 0; i < polygons.Count; i++)
                {
                    Polygon polygon = polygons[i];
                    modifyPolygon(ref polygon);
                    //LineRenderer lineRender = scene.GetComponent<LineRenderer>();
                    //lineRender.SetVertexCount(polygons[i].points.Count + 1);
                    //PointF centerPoint = getCenter(polygons[i]);



                    //for (int j = 0; j < polygons[i].points.Count; j++)
                    //{
                    //    float x = (float)polygons[i].points[j % polygons[i].points.Count].X;
                    //    float y = (float)polygons[i].points[j % polygons[i].points.Count].Y;
                    //    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //    go.transform.localPosition = new Vector3(x, 0, y);
                    //    go.transform.parent = scene.transform;
                    //    go.name = i.ToString();
                    //    //Debug.Log()

                    //    //lineRender.SetPosition(j, new Vector3(x, 0, y));
                    //}

                }
        }

        private void modifyPolygon(ref Polygon polygon)
        {
            PointF first = polygon.points[0];
            PointF last = polygon.points[polygon.points.Count - 1];
            int firstIndex = getEdgeIndex(first);
            int lastIndex = getEdgeIndex(last);


            if (firstIndex == lastIndex)
                return;

            if (firstIndex % 2 == 1 && lastIndex % 2 == 1)
            {
                PointF point = new PointF();
                point.X = getBoundary(0);
                point.Y = getBoundary(3);
                if (isShunshizhen(point, first, polygon.points[2]))
                {
                    polygon.addVertex(new PointF(getBoundary(0), getBoundary(1)));
                    polygon.addVertex(new PointF(getBoundary(0), getBoundary(3)));
                }
                else
                {
                    polygon.addVertex(new PointF(getBoundary(2), getBoundary(3)));
                    polygon.addVertex(new PointF(getBoundary(2), getBoundary(1)));
                }
            }
            if (firstIndex % 2 == 0 && lastIndex % 2 == 0)
            {
                PointF point = new PointF();
                point.X = getBoundary(2);
                point.Y = getBoundary(3);
                if (isShunshizhen(point, first, polygon.points[2]))
                {
                    polygon.addVertex(new PointF(getBoundary(0), getBoundary(3)));
                    polygon.addVertex(new PointF(getBoundary(2), getBoundary(3)));

                }
                else
                {
                    polygon.addVertex(new PointF(getBoundary(2), getBoundary(1)));
                    polygon.addVertex(new PointF(getBoundary(0), getBoundary(1)));
                }
            }

            if (firstIndex % 2 == 0 && lastIndex % 2 == 1)
            {
                PointF point = new PointF();
                point.X = getBoundary(firstIndex);
                point.Y = getBoundary(lastIndex);
                polygon.addVertex(point);
            }

            if (firstIndex % 2 == 1 && lastIndex % 2 == 0)
            {
                PointF point = new PointF();
                point.X = getBoundary(lastIndex);
                point.Y = getBoundary(firstIndex);
                polygon.addVertex(point);
            }
        }


        int getBoundary(int index)
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


        int getEdgeIndex(PointF point)
        {
            int edgeIndex = -1;
            if (Math.Abs(point.X - DS.BBOX.XLeft) < 0.001)
            {
                edgeIndex = 0;
            }
            else if (Math.Abs(point.Y - DS.BBOX.YTop) < 0.001)
            {
                edgeIndex = 1;
            }
            else if (Math.Abs(point.X - DS.BBOX.XRight) < 0.001)
            {
                edgeIndex = 2;
            }
            else if (Math.Abs(point.Y - DS.BBOX.YBottom) < 0.001)
            {
                edgeIndex = 3;
            }
            return edgeIndex;
        }

        public void searchMap(int startIndex,int lastIndex,int currIndex)
        {
            PointF lastPoint = DS.Barycenters[lastIndex].point;
            PointF currPoint = DS.Barycenters[currIndex].point;
            for (int nextIndex = 0; nextIndex < DS.TriangleNum; nextIndex++)
            {

                PointF nextPoint = DS.Barycenters[nextIndex].point;
                if (//!DS.Barycenters[nextIndex].isOutBox &&
                    DS.connectMap[currIndex, nextIndex] &&//如果是联通的
                    lastIndex != nextIndex) //不是前面一个index
                {
                    //搜到起始点了
                    if (nextIndex == startIndex)
                        return;

                    if (isShunshizhen(lastPoint, currPoint, nextPoint))
                    {
                        DS.connectMap[currIndex, nextIndex] = false;
                        polygons[polygons.Count - 1].addVertex(nextPoint);
                        searchMap(startIndex, currIndex, nextIndex);
                    }
                }
            }
        }


        public bool isShunshizhen(PointF lasPoint, PointF currPoint, PointF nextPoint)
        {
            return (currPoint.X - lasPoint.X) * (nextPoint.Y - currPoint.Y) - (currPoint.Y - lasPoint.Y) * (nextPoint.X - currPoint.X) < 0;
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
                            for(int n=0; n<EdgeCount; n++)
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
                    DS.TriangleNum ++;
                }
                #endregion
            }//逐点加入for
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

                if (!(DS.Barycenters[i].X >= DS.BBOX.XLeft && DS.Barycenters[i].X <= DS.BBOX.XRight &&
               DS.Barycenters[i].Y >= DS.BBOX.YTop && DS.Barycenters[i].Y <= DS.BBOX.YBottom))
                {
                    DS.Barycenters[i].isOutBox = true;
                }
                else
                {
                    DS.Barycenters[i].isOutBox = false;
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
            }
            else if (Math.Abs(y3 - y2) < precision)   //p2p3平行于X轴
            {
                k1 = -(x2 - x1) / (y2 - y1);
                bcX = MidX2;
                bcY = k1 * (bcX - MidX1) + MidY1;
            }
            else
            {
                k1 = -(x2 - x1) / (y2 - y1);
                k2 = -(x3 - x2) / (y3 - y2);
                bcX = (k1 * MidX1 - k2 * MidX2 + MidY2 - MidY1) / (k1 - k2);
                bcY = k1 * (bcX - MidX1) + MidY1;
            }
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
                    Edge e = new Edge(Vindex[j],Vindex[(j + 1) % 3]);

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
 

        //i为TinEdge的ID号
        private PointF getEndPntVorEdge(int i)
        {
            
            PointF pnt1 = new PointF(Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex1ID].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex1ID].y));
            PointF pnt2 = new PointF(Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex2ID].x), 
                Convert.ToSingle(DS.Vertex[DS.TinEdges[i].Vertex2ID].y));
            PointF pnt3 = new PointF(Convert.ToSingle(DS.Vertex[DS.TinEdges[i].AdjacentT1V3].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[i].AdjacentT1V3].y));    //边对应的△顶点

            PointF MidPnt = new PointF((pnt1.X + pnt2.X) / 2, (pnt2.Y + pnt2.Y) / 2);  //TinEdge中点
            PointF BaryCnt = new PointF(Convert.ToSingle(DS.Barycenters[DS.TinEdges[i].AdjTriangle1ID].X),
                Convert.ToSingle(DS.Barycenters[DS.TinEdges[i].AdjTriangle1ID].Y));  //外接圆心
            PointF EndPnt = new PointF();   //圆心连接于此点构成VEdge

            //Debug.Log(BaryCnt.ToString());

            //圆心在box外则直接跳过
            if (!(BaryCnt.X >= DS.BBOX.XLeft && BaryCnt.X <= DS.BBOX.XRight &&
                BaryCnt.Y >= DS.BBOX.YTop && BaryCnt.Y <= DS.BBOX.YBottom))    
                return EndPnt;

            //求斜率
            float k = 0;  //斜率
            bool KExist = true;
            if (Math.Abs(pnt1.Y - pnt2.Y) < 0.000001)
                KExist = false;     //k不存在
            else
                k = (pnt1.X - pnt2.X) / (pnt2.Y - pnt1.Y);

            //该凸壳边是△的钝角边则外接圆心在△外
            bool obtEdge = IsObtuseEdge(i);  

            #region 根据△圆心在凸壳内还是在外求VEdge
            //圆心在边右则往左延伸，在左则往右

            if (!obtEdge)   //圆心在凸壳内(或边界上)/////////////////////////////////
            {
                if (!KExist)    //k不存在
                {
                    // MessageBox.Show("斜率不存在的△-"+DS.TinEdges[i].AdjTriangle1ID.ToString());
                    if (BaryCnt.Y > MidPnt.Y || BaryCnt.Y < pnt3.Y)// BaryCnt<y3 ->圆心与中点重合
                        EndPnt.Y = DS.BBOX.YTop;
                    else if (BaryCnt.Y < MidPnt.Y || BaryCnt.Y > pnt3.Y)
                        EndPnt.Y = DS.BBOX.YBottom;

                    EndPnt.X = BaryCnt.X;
                }
                else      //K存在
                {
                    if (BaryCnt.X > MidPnt.X || (BaryCnt.X == MidPnt.X && BaryCnt.X < pnt3.X))
                        EndPnt.X = DS.BBOX.XLeft;
                    else if (BaryCnt.X < MidPnt.X || (BaryCnt.X == MidPnt.X && BaryCnt.X > pnt3.X))
                        EndPnt.X = DS.BBOX.XRight;

                    EndPnt.Y = k * (EndPnt.X - BaryCnt.X) + BaryCnt.Y;
                }

            }
            else    //圆心在凸壳外/////////////////////////////////////////////
            {
                if (!KExist)    //k不存在
                {
                    if (BaryCnt.Y < MidPnt.Y || BaryCnt.Y < pnt3.Y)
                        EndPnt.Y = DS.BBOX.YTop;
                    else if (BaryCnt.Y > MidPnt.Y || BaryCnt.Y > pnt3.Y)
                        EndPnt.Y = DS.BBOX.YBottom;

                    EndPnt.X = BaryCnt.X;
                }
                else   //K存在
                {
                    if (BaryCnt.X < MidPnt.X)
                        EndPnt.X = DS.BBOX.XLeft;
                    else if (BaryCnt.X > MidPnt.X)
                        EndPnt.X = DS.BBOX.XRight;

                    EndPnt.Y = k * (EndPnt.X - BaryCnt.X) + BaryCnt.Y;
                }

            }//else 在△外

            //与外框交点在边界外的处理
            if (k != 0 && KExist)
            {
                if (EndPnt.Y < DS.BBOX.YTop)
                    EndPnt.Y = DS.BBOX.YTop;
                else if (EndPnt.Y > DS.BBOX.YBottom)
                    EndPnt.Y = DS.BBOX.YBottom;

                EndPnt.X = (EndPnt.Y - BaryCnt.Y) / k + BaryCnt.X;
            }

            #endregion

           // g.DrawLine(new Pen(Color.Blue, 2), BaryCnt, EndPnt);
            return EndPnt;
        }

        //index为TinEdge的索引号,若为钝角边则返回true
        private bool IsObtuseEdge(int index)
        {
            PointF EdgePnt1 = new PointF(Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex1ID].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex1ID].y));
            PointF EdgePnt2 = new PointF(Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex2ID].x),
                Convert.ToSingle(DS.Vertex[DS.TinEdges[index].Vertex2ID].y));
            PointF Pnt3 = new PointF(Convert.ToSingle(DS.Vertex[DS.TinEdges[index].AdjacentT1V3].x),
                 Convert.ToSingle(DS.Vertex[DS.TinEdges[index].AdjacentT1V3].y));

            PointF V1 = new PointF((EdgePnt1.X - Pnt3.X), (EdgePnt1.Y - Pnt3.Y));
            PointF V2 = new PointF((EdgePnt2.X - Pnt3.X), (EdgePnt2.Y - Pnt3.Y));
            return (V1.X * V2.X + V1.Y * V2.Y) < 0; //a·b的值<0则为钝角
        }

        //？？？？？？Unused？点在△内则返回true
        private bool PointInTriganle(int PntIndex, int index)
        {
            PointF pnt1 = new PointF(Convert.ToSingle(DS.Vertex[DS.Triangle[index].V1Index].x),
                Convert.ToSingle(DS.Vertex[DS.Triangle[index].V1Index].y));
            PointF pnt2 = new PointF(Convert.ToSingle(DS.Vertex[DS.Triangle[index].V2Index].x),
                Convert.ToSingle(DS.Vertex[DS.Triangle[index].V2Index].y));
            PointF pnt3 = new PointF(Convert.ToSingle(DS.Vertex[DS.Triangle[index].V3Index].x),
                 Convert.ToSingle(DS.Vertex[DS.Triangle[index].V3Index].y));
            PointF JudgePoint = new PointF(Convert.ToSingle(DS.Barycenters[index].X), Convert.ToSingle(DS.Barycenters[index].Y));  //外接圆心

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

        private float VectorXMultiply(PointF BaryCnt, PointF pnt1, PointF pnt2)
        {
            PointF V1 = new PointF((pnt1.X - BaryCnt.X), (pnt1.Y - BaryCnt.Y));
            PointF V2 = new PointF((pnt2.X - BaryCnt.X), (pnt2.Y - BaryCnt.Y));
            return (V1.X * V2.Y - V2.X * V1.Y);
        }
        private bool PointInBox(PointF point)
        {
            return (point.X >= DS.BBOX.XLeft && point.X <= DS.BBOX.XRight &&
                    point.Y >= DS.BBOX.YTop && point.Y <= DS.BBOX.YBottom);
        }


    }
}


