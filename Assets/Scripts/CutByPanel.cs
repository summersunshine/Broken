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
            //if (Time.time - lastTime < 1)
            //{
            //    return;
            //}
            endPosition = Input.mousePosition;
            Debug.Log(startPosition);
            Debug.Log(endPosition);
            Ray ray = Camera.main.ScreenPointToRay(startPosition);//从摄像机发出到点击坐标的射线
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo))
            {
                startPosition = hitInfo.point;
                //GetComponent<LineRenderer>().SetPosition(0, hitInfo.point);
            }
            ray = Camera.main.ScreenPointToRay(endPosition);
            if (Physics.Raycast(ray, out hitInfo))
            {
                endPosition = hitInfo.point;
                //GetComponent<LineRenderer>().SetPosition(1, hitInfo.point);
            }
            
            Vector3 diff = endPosition - startPosition;
            Vector3 dir = new Vector3(diff.y, -diff.x, 0);
            startPosition = transform.InverseTransformPoint(startPosition);
            dir = transform.InverseTransformDirection(dir);
            CutMesh(startPosition, dir);
        }
    }


    public void CutMesh(Vector3 o, Vector3 normal)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        List<PositionType> verticeTypeList = new List<PositionType>();
        List<PositionType> traingleTypeList = new List<PositionType>();
        List<int> leftTriangles = new List<int>();
        List<int> rightTriangles = new List<int>();
        List<Vector3> leftVertics = new List<Vector3>(mesh.vertices);
        List<Vector3> rightVertics = new List<Vector3>(mesh.vertices);
        List<Vector2> leftUVs = new List<Vector2>(mesh.uv);
        List<Vector2> rightUVs = new List<Vector2>(mesh.uv);
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            float value = getValue(mesh.vertices[i], o, normal);
            if (value > 0)
            {
                verticeTypeList.Add(PositionType.Left);
            }
            else
            {
                verticeTypeList.Add(PositionType.Right);
            }
        }

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
                if (positionTypes[0] == PositionType.Left)
                {
                    leftTriangles.AddRange(triangles);
                }
                else
                {
                    rightTriangles.AddRange(triangles);
                }
                continue;
            }

            int otherIndex = getIndexOnOtherSide(positionTypes);
            PositionType otherSideType = positionTypes[otherIndex];

            Vector3 intersect1 = getIntersectPoint(vertics[otherIndex],vertics[(otherIndex+1)%3],o,normal);
            Vector3 intersect2 = getIntersectPoint(vertics[otherIndex],vertics[(otherIndex+2)%3],o,normal);
            float inter1 = Vector3.Distance(vertics[otherIndex],intersect1)/Vector3.Distance(vertics[otherIndex],vertics[(otherIndex+1)%3]);
            float inter2 = Vector3.Distance(vertics[otherIndex],intersect2)/Vector3.Distance(vertics[otherIndex],vertics[(otherIndex+2)%3]);
            Vector2 uv1 = uvs[otherIndex] * (1 - inter1) + uvs[(otherIndex + 1) % 3] * inter1;
            Vector2 uv2 = uvs[otherIndex] * (1 - inter2) + uvs[(otherIndex + 2) % 3] * inter2;

            leftVertics.Add(intersect1);
            leftVertics.Add(intersect2);
            rightVertics.Add(intersect1);
            rightVertics.Add(intersect2);

            leftUVs.Add(uv1);
            leftUVs.Add(uv2);
            rightUVs.Add(uv1);
            rightUVs.Add(uv2);

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

            otherSideTriangles.Add(triangles[otherIndex]);
            otherSideTriangles.Add(otherSideVerticCount - 2);
            otherSideTriangles.Add(otherSideVerticCount - 1);

            thisSideTriangles.Add(triangles[(otherIndex + 1) % 3]);
            thisSideTriangles.Add(thisSideVerticCount - 1);
            thisSideTriangles.Add(thisSideVerticCount - 2);
            

            thisSideTriangles.Add(triangles[(otherIndex + 2) % 3]);
            thisSideTriangles.Add(thisSideVerticCount - 1);
            thisSideTriangles.Add(triangles[(otherIndex + 1) % 3]);
         
     
            if (otherSideType == PositionType.Left)
            {
                leftTriangles.AddRange(otherSideTriangles);
                rightTriangles.AddRange(thisSideTriangles);
            }
            else
            {
                rightTriangles.AddRange(otherSideTriangles);
                leftTriangles.AddRange(thisSideTriangles);
            }


        }

        createBrokenChild(rightVertics, rightUVs, rightTriangles);
        createBrokenChild(leftVertics, leftUVs, leftTriangles);
        Destroy(this.gameObject);

        //mesh.Clear();
        //mesh.vertices = rightVertics.ToArray();
        //mesh.uv = rightUVs.ToArray();
        //mesh.triangles = rightTriangles.ToArray();
        //mesh.RecalculateNormals();
        //GetComponent<MeshCollider>().sharedMesh = mesh;
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
            //go.GetComponent<MeshCollider>().convex = true;
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
