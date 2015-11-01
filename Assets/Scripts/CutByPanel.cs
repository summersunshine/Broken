using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CutByPanel : MonoBehaviour
{
    public enum PositionType
    {
        Left,
        Right,
        Intersect,
    }

    public float lastTime = -1;
    public Vector3 startPosition;
    public Vector3 endPosition;
    public GameObject prefab;


    //切割平面过的点
    public Vector3 cutOrigional;
    //切割平面的法线
    public Vector3 cutNormal;

    Dictionary<int, int> offset = new Dictionary<int, int>();
    List<PositionType> verticeTypeList = new List<PositionType>();
    List<int> leftTriangles = new List<int>();
    List<int> rightTriangles = new List<int>();
    List<Vector3> leftVertics = new List<Vector3>();
    List<Vector3> rightVertics = new List<Vector3>();
    List<Vector2> leftUVs = new List<Vector2>();
    List<Vector2> rightUVs = new List<Vector2>();
    List<int> leftCutTriangles = new List<int>();
    List<int> rightCutTriangles = new List<int>();
    List<Vector3> intersectVertices = new List<Vector3>();
    List<Vector2> intersectUVs = new List<Vector2>();
    Vector3 cutPanelCenter = new Vector3();
   

    void Start()
    {
        prefab = Resources.Load("Cube", typeof(GameObject)) as GameObject;

    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (lastTime == -1)
            {
                lastTime = Time.time;
                startPosition = Input.mousePosition;

                return;
            }
            endPosition = Input.mousePosition;
            Debug.Log(startPosition);
            Debug.Log(endPosition);
            Ray ray = Camera.main.ScreenPointToRay(startPosition);//从摄像机发出到点击坐标的射线
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo))
            {
                startPosition = hitInfo.point;
            }
            ray = Camera.main.ScreenPointToRay(endPosition);
            if (Physics.Raycast(ray, out hitInfo))
            {
                endPosition = hitInfo.point;
            }
            
            Vector3 diff = endPosition - startPosition;
            Vector3 dir = new Vector3(diff.y, -diff.x, 0);
            startPosition = transform.InverseTransformPoint(startPosition);
            dir = transform.InverseTransformDirection(dir);
            cutOrigional = startPosition;
            cutNormal = dir;

            CutMesh();
        }
    }

    public bool divideToTwoPart(Mesh mesh)
    {
        //初始化vertices和uvs
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            float value = getValue(mesh.vertices[i], cutOrigional, cutNormal);
            if (value > 0)
            {
                verticeTypeList.Add(PositionType.Left);
                leftVertics.Add(mesh.vertices[i]);
                leftUVs.Add(mesh.uv[i]);
                offset.Add(i, rightVertics.Count);
            }
            else
            {
                verticeTypeList.Add(PositionType.Right);
                rightVertics.Add(mesh.vertices[i]);
                rightUVs.Add(mesh.uv[i]);
                offset.Add(i, leftVertics.Count);
            }
        }
        return leftVertics.Count > 0 && rightVertics.Count > 0;
    }

    public void CutMesh()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;

        //如果被分层了两部分就继续
        //否则就别扯犊子
        if (!divideToTwoPart(mesh))
            return;



        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int[] triangles = new int[3];
            Vector3[] vertics = new Vector3[3];
            Vector2[] uvs = new Vector2[3];
            PositionType[] positionTypes = new PositionType[3];

            for (int j = 0; j < 3; j++)
            {
                triangles[j] = mesh.triangles[i + j];
                vertics[j] = mesh.vertices[triangles[j]];
                uvs[j] = mesh.uv[triangles[j]];
                positionTypes[j] = verticeTypeList[triangles[j]];

            }

            if(isAllInOneSide(positionTypes))
            {
                handleTriangleInOneSide(triangles, positionTypes[0]);
                continue;
            }

            int otherIndex = getIndexOnOtherSide(positionTypes);
            PositionType otherSideType = positionTypes[otherIndex];

            calVerticsAndUvInOtherSide(otherIndex, vertics, uvs);
            calTiranglesInOtherSide(otherIndex, otherSideType, triangles);


        }

        //算中心点
        addCutCenter();

        //算平均uv
        calAverageUv();

        //更改切割面的三角形
        updateCutPanelTriangle();

        createBrokenChild(rightVertics, rightUVs, rightTriangles);
        createBrokenChild(leftVertics, leftUVs, leftTriangles);

        //销毁本体
        Destroy(this.gameObject);
    }

    public void handleTriangleInOneSide(int[] triangles, PositionType positionType)
    {
        if (positionType == PositionType.Left)
        {
            for (int j = 0; j < 3; j++)
            {
                leftTriangles.Add(triangles[j] - offset[triangles[j]]);
            }
        }
        else
        {
            for (int j = 0; j < 3; j++)
            {
                rightTriangles.Add(triangles[j] - offset[triangles[j]]);
            }
        }
    }

    public void calVerticsAndUvInOtherSide(int otherIndex, Vector3[] vertics, Vector2[] uvs)
    {

        Vector3 intersect1 = getIntersectPoint(vertics[otherIndex], vertics[(otherIndex + 1) % 3], cutOrigional, cutNormal);
        Vector3 intersect2 = getIntersectPoint(vertics[otherIndex], vertics[(otherIndex + 2) % 3], cutOrigional, cutNormal);

        intersectVertices.Add(intersect1);
        intersectVertices.Add(intersect2);

        //利用差值计算uv值
        float inter1 = Vector3.Distance(vertics[otherIndex], intersect1) / Vector3.Distance(vertics[otherIndex], vertics[(otherIndex + 1) % 3]);
        float inter2 = Vector3.Distance(vertics[otherIndex], intersect2) / Vector3.Distance(vertics[otherIndex], vertics[(otherIndex + 2) % 3]);
        Vector2 uv1 = uvs[otherIndex] * (1 - inter1) + uvs[(otherIndex + 1) % 3] * inter1;
        Vector2 uv2 = uvs[otherIndex] * (1 - inter2) + uvs[(otherIndex + 2) % 3] * inter2;



        leftVertics.Add(intersect1);
        leftVertics.Add(intersect2);
        rightVertics.Add(intersect1);
        rightVertics.Add(intersect2);

        intersectUVs.Add(uv1);
        intersectUVs.Add(uv2);

        leftUVs.Add(uv1);
        leftUVs.Add(uv2);
        rightUVs.Add(uv1);
        rightUVs.Add(uv2);

    }

    public void calTiranglesInOtherSide(int otherIndex,PositionType otherSideType, int[] triangles)
    {
        #region 初始化网格数据
        List<int> otherSideTriangles = new List<int>();
        List<int> thisSideTriangles = new List<int>();
        int otherSideVerticCount = 0;
        int thisSideVerticCount = 0;

        if (otherSideType == PositionType.Left)
        {
            otherSideVerticCount = leftVertics.Count;
            thisSideVerticCount = rightVertics.Count;
        }
        else
        {
            otherSideVerticCount = rightVertics.Count;
            thisSideVerticCount = leftVertics.Count;
        }
        #endregion

        #region 原始网格与切割面交叉部分
        otherSideTriangles.Add(triangles[otherIndex] - offset[triangles[otherIndex]]);
        otherSideTriangles.Add(otherSideVerticCount - 2);
        otherSideTriangles.Add(otherSideVerticCount - 1);

        thisSideTriangles.Add(triangles[(otherIndex + 1) % 3] - offset[triangles[(otherIndex + 1) % 3]]);
        thisSideTriangles.Add(thisSideVerticCount - 1);
        thisSideTriangles.Add(thisSideVerticCount - 2);


        thisSideTriangles.Add(triangles[(otherIndex + 2) % 3] - offset[triangles[(otherIndex + 2) % 3]]);
        thisSideTriangles.Add(thisSideVerticCount - 1);
        thisSideTriangles.Add(triangles[(otherIndex + 1) % 3] - offset[triangles[(otherIndex + 1) % 3]]);
        #endregion

        #region 切割面
        if (otherSideType == PositionType.Left)
        {
            leftTriangles.AddRange(otherSideTriangles);
            rightTriangles.AddRange(thisSideTriangles);

            leftCutTriangles.Add(otherSideVerticCount - 1);
            leftCutTriangles.Add(otherSideVerticCount - 2);
            leftCutTriangles.Add(0);

            rightCutTriangles.Add(thisSideVerticCount - 2);
            rightCutTriangles.Add(thisSideVerticCount - 1);
            rightCutTriangles.Add(0);

        }
        else
        {
            rightTriangles.AddRange(otherSideTriangles);
            leftTriangles.AddRange(thisSideTriangles);

            leftCutTriangles.Add(thisSideVerticCount - 2);
            leftCutTriangles.Add(thisSideVerticCount - 1);
            leftCutTriangles.Add(0);

            rightCutTriangles.Add(otherSideVerticCount - 1);
            rightCutTriangles.Add(otherSideVerticCount - 2);
            rightCutTriangles.Add(0);

        }
        #endregion

    }


    public void addCutCenter()
    {
        for (int i = 0; i < intersectVertices.Count; i++)
        {
            cutPanelCenter += intersectVertices[i];
        }
        cutPanelCenter /= intersectVertices.Count;


        leftVertics.Add(cutPanelCenter);
        rightVertics.Add(cutPanelCenter);
    }

    public void calAverageUv()
    {
        #region 计算所有点到切割面中点的距离
        List<float> distanceList = new List<float>();
        float totalDistance = 0;
        for (int i = 0; i < intersectVertices.Count; i++)
        {
            float dis = Vector3.Distance(cutPanelCenter, intersectVertices[i]);
            totalDistance += dis;
            distanceList.Add(dis);
        }
        #endregion

        #region 计算加权平均的uv值
        Vector2 uv = new Vector2();
        for (int i = 0; i < intersectVertices.Count; i++)
        {
            uv += distanceList[i] * (intersectUVs[i]);
        }
        uv /= totalDistance;
        #endregion

        leftUVs.Add(uv);
        rightUVs.Add(uv);
    }


    public void updateCutPanelTriangle()
    {
        for (int i = 0; i < leftCutTriangles.Count; i += 3)
        {
            leftCutTriangles[i + 2] = leftVertics.Count - 1;
        }
        leftTriangles.AddRange(leftCutTriangles);
        for (int i = 0; i < rightCutTriangles.Count; i += 3)
        {
            rightCutTriangles[i + 2] = rightVertics.Count - 1;
        }
        rightTriangles.AddRange(rightCutTriangles);
    }


    public void createBrokenChild(List<Vector3> v,List<Vector2> uv,List<int> t)
    {
        
        GameObject go = Instantiate(prefab);
        Mesh mesh = new Mesh();
        mesh.vertices = v.ToArray();
        mesh.uv = uv.ToArray();
        mesh.triangles = t.ToArray();
        mesh.RecalculateNormals();
        if (go.GetComponent<MeshFilter>())
        {
            go.GetComponent<MeshFilter>().mesh = mesh;
        }

        if (go.GetComponent<MeshCollider>())
        {
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            go.GetComponent<MeshCollider>().convex = true;
        }
        go.transform.localPosition = transform.localPosition;
        go.transform.localRotation = transform.localRotation;
        go.transform.localScale = transform.localScale;

    }

    public bool isAllInOneSide(PositionType[] positionTypes)
    {
        for (int i = 0; i < 3; i++)
        {
            if (positionTypes[i] != positionTypes[(i + 1) % 3])
            {
                return false;
            }
        }
        return true;
    }

    public int getIndexOnOtherSide(PositionType[] positionTypes)
    {
        if (positionTypes[0] == positionTypes[1])
            return 2;
        else if (positionTypes[0] == positionTypes[2])
            return 1;
        else
            return 0;
    }


    public Vector3 getIntersectPoint(Vector3 pointA,Vector3 pointB,Vector3 o,Vector3 normal)
    {
        float t = Vector3.Dot(normal, (o - pointA)) / Vector3.Dot(normal, pointB - pointA);
        return pointA + t * (pointB - pointA);
    }



    public float getValue(Vector3 point, Vector3 o, Vector3 normal)
    {
        return normal.x * (point.x - o.x) + normal.y * (point.y - o.y) + normal.z * (point.z - o.z);
    }
}
