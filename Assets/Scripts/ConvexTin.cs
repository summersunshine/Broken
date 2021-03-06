using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TINVoronoi
{
    public partial class Delaynay
    {
       
        public List<int> HullPoint;  //凸壳顶点链表
        private struct PntV_ID
        {
            public float Value;
            public int ID;
        }
        
        //建立凸壳
        public void CreateConvex()
        {
            //初始化凸壳顶点链表
            if (HullPoint == null)
                HullPoint = new List<int>();
            else
            {
                for (int i = 0; i < HullPoint.Count; i++)
                    DS.Vertex[HullPoint[i]].isHullEdge = 0;  //去除凸壳标记
                HullPoint.Clear();
            }

            #region 计算x-y和x+y的最大、最小点
            PntV_ID MaxMinus, MinMinus, MaxAdd, MinAdd;
            MaxMinus.ID = MinMinus.ID = MaxAdd.ID = MinAdd.ID = DS.Vertex[0].ID;//用第一点初始化
            MaxMinus.Value = MinMinus.Value = DS.Vertex[0].x - DS.Vertex[0].y;
            MaxAdd.Value = MinAdd.Value = DS.Vertex[0].x + DS.Vertex[0].y;

            float temp;
            for (int i = 1; i < DS.VerticesNum; i++)
            {
                temp = DS.Vertex[i].x - DS.Vertex[i].y;
                if (temp > MaxMinus.Value)
                {
                    MaxMinus.Value = temp;
                    MaxMinus.ID = DS.Vertex[i].ID;
                }
                if (temp < MinMinus.Value)
                {
                    MinMinus.Value = temp;
                    MinMinus.ID = DS.Vertex[i].ID;
                }

                temp = DS.Vertex[i].x + DS.Vertex[i].y;
                if (temp > MaxAdd.Value)
                {
                    MaxAdd.Value = temp;
                    MaxAdd.ID = DS.Vertex[i].ID;
                }
                if (temp < MinAdd.Value)
                {
                    MinAdd.Value = temp;
                    MinAdd.ID = DS.Vertex[i].ID;
                }
            }
            #endregion

            //加入链表
            HullPoint.Add(MinMinus.ID);
            HullPoint.Add(MaxAdd.ID);
            HullPoint.Add(MaxMinus.ID); 
            HullPoint.Add(MinAdd.ID);
            //还要去除重复点……
            for (int i = 0; i < HullPoint.Count; i++)
            {
                if (HullPoint[i] == HullPoint[(i + 1) % HullPoint.Count])
                    HullPoint.RemoveAt(i);
            }

            //将点标记，下一步不再处理
            for (int i = 0; i < HullPoint.Count; i++)
                DS.Vertex[HullPoint[i]].isHullEdge = -1;  

            #region 插入其他点
            for (int i = 0; i < DS.VerticesNum; i++)
            {
                if (DS.Vertex[i].isHullEdge == -1)
                    continue;

                //判断点i与每条边的关系
                float isOnRight;
                for (int j = 0; j < HullPoint.Count; j++)
                {
                    Vector2 pnt1 = new Vector2(Convert.ToSingle(DS.Vertex[HullPoint[j]].x),
                            Convert.ToSingle(DS.Vertex[HullPoint[j]].y));
                    Vector2 pnt2 = new Vector2(Convert.ToSingle(DS.Vertex[HullPoint[(j + 1) % HullPoint.Count]].x),
                            Convert.ToSingle(DS.Vertex[HullPoint[(j + 1) % HullPoint.Count]].y));
                    Vector2 pnt3 = new Vector2(Convert.ToSingle(DS.Vertex[i].x), Convert.ToSingle(DS.Vertex[i].y));
                    isOnRight = VectorXMultiply(pnt1, pnt2, pnt3);// >0则位于外侧(设备坐标系，逆时针)

                    //如果点在边外侧则修改凸壳
                    if (isOnRight >= 0)
                    {
                        //点插入凸边链表
                        HullPoint.Insert((j + 1) % (HullPoint.Count + 1), DS.Vertex[i].ID);

                        //判断添加点后是否出现内凹
                        pnt1 = new Vector2(Convert.ToSingle(DS.Vertex[HullPoint[(j + 3) % HullPoint.Count]].x),
                            Convert.ToSingle(DS.Vertex[HullPoint[(j + 3) % HullPoint.Count]].y));
                        isOnRight = VectorXMultiply(pnt3, pnt2, pnt1);
                        if (isOnRight > 0)    //删除内凹点p2(即v[j+2])
                        {
                            int index = (j + 2) % HullPoint.Count;    //点在凸壳链表中的索引
                            DS.Vertex[HullPoint[index]].isHullEdge = 0;
                            HullPoint.RemoveAt(index);
                        }

                        break;
                    }
                }//for
            }//for
            #endregion
           
            //将顶点的凸壳标记设为1
            for (int i = 0; i < HullPoint.Count; i++)
                DS.Vertex[HullPoint[i]].isHullEdge = 1;
        }

        //凸壳三角剖分
        public void HullTriangulation()
        {
            DS.TriangleNum = 0;

            //凸壳为点或线的情况
            //多点共边的情况

            //复制凸壳顶点
            List<int> points = new List<int>();
            for (int i = 0; i < HullPoint.Count; i++)
                points.Add(HullPoint[i]);

            //构网
            int id1, id2, id3;
            while (points.Count >= 3)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    //△为干净的则加入网中
                    id1 = points[i];
                    id2 = points[(i + 1) % points.Count];
                    id3 = points[(i + 2) % points.Count];
                    if (IsClean(id1, id2, id3))
                    {                        
                        DS.Triangle[DS.TriangleNum].V1Index = id1;
                        DS.Triangle[DS.TriangleNum].V2Index = id2;
                        DS.Triangle[DS.TriangleNum].V3Index = id3;
                        DS.TriangleNum++;
                        DS.Vertex[id2].isHullEdge = 2;  //标记已构网点
                        points.Remove(id2);

                        break;
                    }
                }//for
            }//while
        }

        //三角形外接圆中不含其他凸壳顶点
        private bool IsClean(int p1ID, int p2ID, int p3ID)
        {
            for (int i = 0; i < HullPoint.Count; i++)
            {
                //跳过已构网的点和△顶点
                if (DS.Vertex[HullPoint[i]].isHullEdge == 2 || HullPoint[i] == p1ID || HullPoint[i] == p2ID || HullPoint[i] == p3ID)
                    continue;
                
                //如果点i位于△外接圆内
                if (InTriangleExtCircle(DS.Vertex[HullPoint[i]].x, DS.Vertex[HullPoint[i]].y, DS.Vertex[p1ID].x,
                    DS.Vertex[p1ID].y, DS.Vertex[p2ID].x, DS.Vertex[p2ID].y, DS.Vertex[p3ID].x, DS.Vertex[p3ID].y))
                    return false;
            }

            return true;
        }

    }
}