using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public class Temp
    {
        public void splitMesh(Mesh mesh)
        {

            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3[] vertices = new Vector3[6];
                vertices[0] = mesh.vertices[mesh.triangles[i + 0]] + Vector3.up;
                vertices[1] = mesh.vertices[mesh.triangles[i + 1]] + Vector3.up;
                vertices[2] = mesh.vertices[mesh.triangles[i + 2]] + Vector3.up;
                vertices[3] = mesh.vertices[mesh.triangles[i + 0]] - Vector3.up;
                vertices[4] = mesh.vertices[mesh.triangles[i + 1]] - Vector3.up;
                vertices[5] = mesh.vertices[mesh.triangles[i + 2]] - Vector3.up;
                Mesh subMesh = new Mesh();
                subMesh.vertices = vertices;
                subMesh.triangles = getTriangles();
                subMesh.RecalculateNormals();
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.GetComponent<MeshFilter>().mesh = subMesh;
                //go.AddComponent<MeshCollider
                go.GetComponent<MeshCollider>().sharedMesh = subMesh;
                go.GetComponent<MeshCollider>().convex = true;
                go.AddComponent<Rigidbody>();


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
    }
}
//public List<Vector3> getVertics(Vertex[] Vertex)
//{
//    vertexs = new List<Vector3>();
//    for (int i = 0; i < Vertex.Length; i++)
//    {
//        vertexs.Add(new Vector3( Vertex[i].x,0,Vertex[i].y));
//    }
//    return vertexs;
//}

//public void printMesh(Mesh mesh)
//{
//    for (int i = 0; i < mesh.vertices.Length; i++)
//    {
//        Debug.Log(mesh.vertices[i]);
//    }


//    for (int i = 0; i < mesh.triangles.Length; i++)
//    {
//        Debug.Log(mesh.triangles[i]);
//    }
//}

