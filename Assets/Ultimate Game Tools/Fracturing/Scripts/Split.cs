using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UltimateFracturing
{
    public static partial class Fracturer
    {
        public class SplitOptions
        {
            public static SplitOptions Default = new SplitOptions();

            public SplitOptions()
            {
                bForceNoProgressInfo         = false;
                bForceNoIslandGeneration     = false;
                bForceNoChunkConnectionInfo  = false;
                bForceNoIslandConnectionInfo = false;
                bForceNoCap                  = false;
                bForceCapVertexSoup          = false;
                bIgnoreNegativeSide          = false;
                bVerticesAreLocal            = false;
                nForceMeshConnectivityHash   = -1;
            }

            public bool bForceNoProgressInfo;
            public bool bForceNoIslandGeneration;
            public bool bForceNoChunkConnectionInfo;
            public bool bForceNoIslandConnectionInfo;
            public bool bForceNoCap;
            public bool bForceCapVertexSoup;
            public bool bIgnoreNegativeSide;
            public bool bVerticesAreLocal;
            public int  nForceMeshConnectivityHash;
        }

        public static bool SplitMeshUsingPlane(GameObject gameObjectIn, FracturedObject fracturedComponent, SplitOptions splitOptions, Transform transformPlaneSplit, out List<GameObject> listGameObjectsPosOut, out List<GameObject> listGameObjectsNegOut, ProgressDelegate progress = null)
        {
            listGameObjectsPosOut = new List<GameObject>();
            listGameObjectsNegOut = new List<GameObject>();

            MeshFilter meshfIn = gameObjectIn.GetComponent<MeshFilter>();

            if(meshfIn == null)
            {
                return false;
            }

            foreach(FracturedChunk chunk in fracturedComponent.ListFracturedChunks)
            {
                if(chunk != null)
                {
                    UnityEngine.Object.DestroyImmediate(chunk.gameObject);
                }
            }

            fracturedComponent.ListFracturedChunks.Clear();
            fracturedComponent.DecomposeRadius = (meshfIn.sharedMesh.bounds.max - meshfIn.sharedMesh.bounds.min).magnitude;
            Random.seed = fracturedComponent.RandomSeed;

//            Debug.Log("In: " + gameObjectIn.name + ": " + meshfIn.sharedMesh.subMeshCount + " submeshes, " + ": " + (meshfIn.sharedMesh.triangles.Length / 3) + " triangles, " + meshfIn.sharedMesh.vertexCount + " vertices, " + (meshfIn.sharedMesh.uv != null ? meshfIn.sharedMesh.uv.Length : 0) + " uaVertex[0], " + (meshfIn.sharedMesh.uaVertex[1] != null ? meshfIn.sharedMesh.uaVertex[1].Length : 0) + " uaVertex[1]");

            // Check if the input object already has been split, to get its split closing submesh

            FracturedChunk fracturedChunk = gameObjectIn.GetComponent<FracturedChunk>();

            int nSplitCloseSubMesh = fracturedChunk != null ? fracturedChunk.SplitSubMeshIndex : -1;

            if(nSplitCloseSubMesh == -1 && gameObjectIn.GetComponent<Renderer>())
            {
                // Check if its material is the same as the split material

                if(gameObjectIn.GetComponent<Renderer>().sharedMaterial == fracturedComponent.SplitMaterial)
                {
                    nSplitCloseSubMesh = 0;
                }
            }

            List<MeshData> listMeshDatasPos;
            List<MeshData> listMeshDatasNeg;

            Material[] aMaterials = fracturedComponent.gameObject.GetComponent<Renderer>() ? fracturedComponent.gameObject.GetComponent<Renderer>().sharedMaterials : null;

            MeshData meshDataIn = new MeshData(meshfIn.transform, meshfIn.sharedMesh, aMaterials, meshfIn.transform.localToWorldMatrix, !splitOptions.bVerticesAreLocal, nSplitCloseSubMesh, true);

            if(SplitMeshUsingPlane(meshDataIn, fracturedComponent, splitOptions, transformPlaneSplit.up, transformPlaneSplit.right, transformPlaneSplit.position, out listMeshDatasPos, out listMeshDatasNeg, progress) == false)
            {
                return false;
            }

            // Set the mesh properties and add objects to list

            if(listMeshDatasPos.Count > 0)
            {
                for(int nMeshCount = 0; nMeshCount < listMeshDatasPos.Count; nMeshCount++)
                {
                    GameObject goPos = CreateNewSplitGameObject(gameObjectIn, fracturedComponent, gameObjectIn.name + "0" + (listMeshDatasPos.Count > 1 ? ("(" + nMeshCount + ")") : ""), !splitOptions.bVerticesAreLocal, listMeshDatasPos[nMeshCount]);
                    listGameObjectsPosOut.Add(goPos);
                }
            }

            if(listMeshDatasNeg.Count > 0)
            {
                for(int nMeshCount = 0; nMeshCount < listMeshDatasNeg.Count; nMeshCount++)
                {
                    GameObject goNeg = CreateNewSplitGameObject(gameObjectIn, fracturedComponent, gameObjectIn.name + "1" + (listMeshDatasNeg.Count > 1 ? ("(" + nMeshCount + ")") : ""), !splitOptions.bVerticesAreLocal, listMeshDatasNeg[nMeshCount]);
                    listGameObjectsNegOut.Add(goNeg);
                }
            }

            return true;
        }



        private static int getRemapNewIndex(int index,MeshData meshDataIn,
                              List<VertexData> plistVertexData,   
                            Dictionary<int, int> pdicRemappedIndices)
        {
            int newIndex = -1;
            if (pdicRemappedIndices.ContainsKey(index))
            {
                newIndex = pdicRemappedIndices[index];
            }
            if (newIndex == -1)
            {
                newIndex = plistVertexData.Count;
                plistVertexData.Add(meshDataIn.aVertexData[index].Copy());
                pdicRemappedIndices[index] = newIndex;
            }
            return newIndex;
        }

        private static bool SplitMeshUsingPlane(MeshData meshDataIn, FracturedObject fracturedComponent, SplitOptions splitOptions, Vector3 v3PlaneNormal, Vector3 v3PlaneRight, Vector3 v3PlanePoint, out List<MeshData> listMeshDatasPosOut, out List<MeshData> listMeshDatasNegOut, ProgressDelegate progress = null)
        {
            Plane planeSplit = new Plane(v3PlaneNormal, v3PlanePoint);

            listMeshDatasPosOut = new List<MeshData>();
            listMeshDatasNegOut = new List<MeshData>();

            // Check if the input object already has been split, to get its split closing submesh

            bool bNeedsNewSplitSubMesh = meshDataIn.nSplitCloseSubMesh == -1;
            int  nSplitCloseSubMesh    = meshDataIn.nSplitCloseSubMesh;

            // Here we are going to store our output vertex/index data

            int nCurrentVertexHash = meshDataIn.nCurrentVertexHash; // We will use this to identify vertices with same coordinates but different vertex data. They will share the same vertex hash

            List<VertexData>     listVertexDataPos   = new List<VertexData>();
            List<VertexData>     listVertexDataNeg   = new List<VertexData>();
            List<int>[]          alistIndicesPos     = new List<int>[meshDataIn.nSubMeshCount + (meshDataIn.nSplitCloseSubMesh == -1 ? 1 : 0)];
            List<int>[]          alistIndicesNeg     = new List<int>[meshDataIn.nSubMeshCount + (meshDataIn.nSplitCloseSubMesh == -1 ? 1 : 0)];
            MeshFaceConnectivity faceConnectivityPos = new MeshFaceConnectivity();
            MeshFaceConnectivity faceConnectivityNeg = new MeshFaceConnectivity();
            MeshDataConnectivity meshConnectivityPos = new MeshDataConnectivity();
            MeshDataConnectivity meshConnectivityNeg = new MeshDataConnectivity();

            listVertexDataPos.Capacity = meshDataIn.aVertexData.Length / 2;
            listVertexDataNeg.Capacity = meshDataIn.aVertexData.Length / 2;

            if(bNeedsNewSplitSubMesh)
            {
                // Make room for the split closing submesh

                nSplitCloseSubMesh = meshDataIn.nSubMeshCount;

                alistIndicesPos[nSplitCloseSubMesh] = new List<int>();
                alistIndicesNeg[nSplitCloseSubMesh] = new List<int>();
            }

            // Our vertices that form the clipped cap

            Dictionary<EdgeKeyByHash, int>     dicClipVerticesHash = new Dictionary<EdgeKeyByHash, int>    (new EdgeKeyByHash.EqualityComparer());
            Dictionary<EdgeKeyByHash, CapEdge> dicCapEdges         = new Dictionary<EdgeKeyByHash, CapEdge>(new EdgeKeyByHash.EqualityComparer());

            // A hash table with our clipped edges, to reuse clipped vertices

            Dictionary<EdgeKeyByIndex, ClippedEdge> dicClippedEdgesPos = new Dictionary<EdgeKeyByIndex, ClippedEdge>(new EdgeKeyByIndex.EqualityComparer());
            Dictionary<EdgeKeyByIndex, ClippedEdge> dicClippedEdgesNeg = new Dictionary<EdgeKeyByIndex, ClippedEdge>(new EdgeKeyByIndex.EqualityComparer());

            int nClippedCacheHits   = 0;
            int nClippedCacheMisses = 0;

            // A hash table with the remapped indices, to reuse non-clipped vertices

            Dictionary<int, int> dicRemappedIndicesPos = new Dictionary<int, int>();
            Dictionary<int, int> dicRemappedIndicesNeg = new Dictionary<int, int>();

            for(int nSubMesh = 0; nSubMesh < meshDataIn.nSubMeshCount; nSubMesh++)
            {
                // Index list

                alistIndicesPos[nSubMesh] = new List<int>();
                alistIndicesNeg[nSubMesh] = new List<int>();
                List<int> listIndicesPos = alistIndicesPos[nSubMesh];
                List<int> listIndicesNeg = alistIndicesNeg[nSubMesh];

                alistIndicesPos[nSubMesh].Capacity = meshDataIn.aaIndices[nSubMesh].Length / 2;
                alistIndicesNeg[nSubMesh].Capacity = meshDataIn.aaIndices[nSubMesh].Length / 2;

                // A reference to the output arrays/lists (it will be switching between positive/negative side along the algorithm)

                List<VertexData>     plistVertexData    = listVertexDataPos;
                List<int>            plistObjectIndices = listIndicesPos;
                MeshFaceConnectivity pFaceConnectivity  = faceConnectivityPos;
                MeshDataConnectivity pMeshConnectivity  = meshConnectivityPos;

                Dictionary<EdgeKeyByIndex, ClippedEdge> pdicClippedEdges    = dicClippedEdgesPos;
                Dictionary<int, int>                    pdicRemappedIndices = dicRemappedIndicesPos;

                // Iterate through all submesh faces:

                for(int i = 0; i < meshDataIn.aaIndices[nSubMesh].Length / 3; i++)
                {
                    #region data declaration and initialization
                    plistVertexData     = listVertexDataPos;
                    plistObjectIndices  = listIndicesPos;
                    pFaceConnectivity   = faceConnectivityPos;
                    pMeshConnectivity   = meshConnectivityPos;
                    pdicClippedEdges    = dicClippedEdgesPos;
                    pdicRemappedIndices = dicRemappedIndicesPos;

                    int[] aIndex = new int[3];
                    aIndex[0] = meshDataIn.aaIndices[nSubMesh][i * 3 + 0];
                    aIndex[1] = meshDataIn.aaIndices[nSubMesh][i * 3 + 1];
                    aIndex[2] = meshDataIn.aaIndices[nSubMesh][i * 3 + 2];

                    int[] aHashVertex = new int[3];
                    aHashVertex[0] = meshDataIn.aVertexData[aIndex[0]].nVertexHash;
                    aHashVertex[1] = meshDataIn.aVertexData[aIndex[1]].nVertexHash;
                    aHashVertex[2] = meshDataIn.aVertexData[aIndex[2]].nVertexHash;

                    Vector3[] aVertex = new Vector3[3];
                    aVertex[0] = meshDataIn.aVertexData[aIndex[0]].v3Vertex;
                    aVertex[1] = meshDataIn.aVertexData[aIndex[1]].v3Vertex;
                    aVertex[2] = meshDataIn.aVertexData[aIndex[2]].v3Vertex;

                    // Classify vertices depending on the side of the plane they lay on, then clip if necessary.
                    float[] aSide = new float[3];
                    aSide[0] = aVertex[0].x * planeSplit.normal.x + aVertex[0].y * planeSplit.normal.y + aVertex[0].z * planeSplit.normal.z + planeSplit.distance;
                    aSide[1] = aVertex[1].x * planeSplit.normal.x + aVertex[1].y * planeSplit.normal.y + aVertex[1].z * planeSplit.normal.z + planeSplit.distance;
                    aSide[2] = aVertex[2].x * planeSplit.normal.x + aVertex[2].y * planeSplit.normal.y + aVertex[2].z * planeSplit.normal.z + planeSplit.distance;

                    bool[] aBAlomstInPanel = new bool[3];
                    aBAlomstInPanel[0] = false;
                    aBAlomstInPanel[1] = false;
                    aBAlomstInPanel[2] = false;

                    bool  bForceSameSide  = false;
                    int   nAlmostInPlane  = 0;
                    float fFurthest       = 0.0f;
                     
                    if(Mathf.Abs(aSide[0]) < UltimateFracturing.Parameters.EPSILONDISTANCEPLANE) { aBAlomstInPanel[0] = true; nAlmostInPlane++; }
                    if(Mathf.Abs(aSide[1]) < UltimateFracturing.Parameters.EPSILONDISTANCEPLANE) { aBAlomstInPanel[1] = true; nAlmostInPlane++; }
                    if(Mathf.Abs(aSide[2]) < UltimateFracturing.Parameters.EPSILONDISTANCEPLANE) { aBAlomstInPanel[2] = true; nAlmostInPlane++; }

                    if(Mathf.Abs(aSide[0]) > Mathf.Abs(fFurthest)) fFurthest = aSide[0];
                    if(Mathf.Abs(aSide[1]) > Mathf.Abs(fFurthest)) fFurthest = aSide[1];
                    if(Mathf.Abs(aSide[2]) > Mathf.Abs(fFurthest)) fFurthest = aSide[2];
                    #endregion

                    #region Look if the other two vertices are on the same side. If so, we'll skip the clipping too.
                    if (nAlmostInPlane == 1)
                    {
                        if(aBAlomstInPanel[0] && (aSide[1] * aSide[2] > 0.0f)) bForceSameSide = true;
                        if(aBAlomstInPanel[1] && (aSide[0] * aSide[2] > 0.0f)) bForceSameSide = true;
                        if(aBAlomstInPanel[2] && (aSide[0] * aSide[1] > 0.0f)) bForceSameSide = true;
                    }
                    #endregion
                    #region Look if there are more than 1 vertices almonst in plane
                    else if(nAlmostInPlane > 1)
                    {
                        bForceSameSide = true;
                        #region If 3 vertices are all in plane
                        if(nAlmostInPlane == 3)
                        {
                            // Coplanar
                            continue;
                        }
                        #endregion
                    }
                    #endregion

                    #region All on the same side, no clipping needed
                    if ((aSide[0] * aSide[1] > 0.0f && aSide[1] * aSide[2] > 0.0f) || bForceSameSide)
                    {
                        

                        if(fFurthest < 0.0f)
                        {
                            plistVertexData     = listVertexDataNeg;
                            plistObjectIndices  = listIndicesNeg;
                            pFaceConnectivity   = faceConnectivityNeg;
                            pMeshConnectivity   = meshConnectivityNeg;
                            pdicClippedEdges    = dicClippedEdgesNeg;
                            pdicRemappedIndices = dicRemappedIndicesNeg;
                        }


                        #region Find vertices in remapped indices list and add vertex data if not present

                        int[] aNewIndex = new int[3];
                        aNewIndex[0] = getRemapNewIndex(aIndex[0], meshDataIn, plistVertexData, pdicRemappedIndices);
                        aNewIndex[1] = getRemapNewIndex(aIndex[1], meshDataIn, plistVertexData, pdicRemappedIndices);
                        aNewIndex[2] = getRemapNewIndex(aIndex[2], meshDataIn, plistVertexData, pdicRemappedIndices);

                        #endregion

                        #region Add triangle indices

                        if (fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                        {
                            pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                        }

                        plistObjectIndices.Add(aNewIndex[0]);
                        plistObjectIndices.Add(aNewIndex[1]);
                        plistObjectIndices.Add(aNewIndex[2]);

                        if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                        {
                            pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], aVertex[1], aHashVertex[0], aHashVertex[1], aNewIndex[0], aNewIndex[1]);
                            pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], aVertex[2], aHashVertex[1], aHashVertex[2], aNewIndex[1], aNewIndex[2]);
                            pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], aVertex[0], aHashVertex[2], aHashVertex[0], aNewIndex[2], aNewIndex[0]);
                        }
                        #endregion

                        #region Add cap edges only if an edge is lying on the plane

                        if (nAlmostInPlane == 2)
                        {
                            if(fFurthest > 0.0f)
                            {
                                if(aBAlomstInPanel[0] && aBAlomstInPanel[1] && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[0], aHashVertex[1], aVertex[0], aVertex[1]);
                                if(aBAlomstInPanel[1] && aBAlomstInPanel[2] && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[1], aHashVertex[2], aVertex[1], aVertex[2]);
                                if(aBAlomstInPanel[2] && aBAlomstInPanel[0] && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[2], aHashVertex[0], aVertex[2], aVertex[0]);
                            }
                            else
                            {
                                if(aBAlomstInPanel[0] && aBAlomstInPanel[1] && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[1], aHashVertex[0], aVertex[1], aVertex[0]);
                                if(aBAlomstInPanel[1] && aBAlomstInPanel[2] && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[2], aHashVertex[1], aVertex[2], aVertex[1]);
                                if(aBAlomstInPanel[2] && aBAlomstInPanel[0] && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[0], aHashVertex[2], aVertex[0], aVertex[2]);
                            }
                        }
                        #endregion

                    }
                    #endregion
                    #region Special treatment clipping for one vertex laying on the clipping plane and the other 2 on different sides
                    else if(nAlmostInPlane == 1)
                    {
                        int [] aNewIndex = new int[4];
                        aNewIndex[0] = -1;
                        aNewIndex[1] = -1;
                        aNewIndex[2] = -1;
                        aNewIndex[3] = -1;
                        int  nHashV4    = -1;
                        bool bEdge      = false;

                        EdgeKeyByIndex clippedEdgeKey;
                        EdgeKeyByHash clippedEdgeKeyHash;
                        #region aVertex[0] almost on the clipping plane
                        if (aBAlomstInPanel[0])
                        {

                            if (aSide[1] < 0.0f)
                            {
                                plistVertexData = listVertexDataNeg;
                                plistObjectIndices = listIndicesNeg;
                                pFaceConnectivity = faceConnectivityNeg;
                                pMeshConnectivity = meshConnectivityNeg;
                                pdicClippedEdges = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }


                            {
                                clippedEdgeKey = new EdgeKeyByIndex(aIndex[1], aIndex[2]);
                                if (pdicClippedEdges.ContainsKey(clippedEdgeKey))
                                {
                                    nClippedCacheHits++;
                                    bEdge = true;
                                    aNewIndex[1] = pdicClippedEdges[clippedEdgeKey].GetFirstIndex(aIndex[1]);
                                    aNewIndex[3] = pdicClippedEdges[clippedEdgeKey].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if (pdicRemappedIndices.ContainsKey(aIndex[1])) aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                                }


                            }

                            // Clip if not present in clipped edge list

                            clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[1], aHashVertex[2]);

                            if (dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                            {
                                nHashV4 = dicClipVerticesHash[clippedEdgeKeyHash];
                            }
                            else
                            {
                                nHashV4 = nCurrentVertexHash++;
                                dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV4);
                            }

                            VertexData vd4 = new VertexData(nHashV4);

                            if (bEdge == false)
                            {
                                if (VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[1], aIndex[2], aVertex[1], aVertex[2], planeSplit, ref vd4) == false)
                                {
                                    return false;
                                }
                            }

                            // Add geometry of one side

                            // Add vertex data for all data not present in remapped list

                            if (aNewIndex[0] == -1)
                            {
                                if (pdicRemappedIndices.ContainsKey(aIndex[0]))
                                {
                                    aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                                }
                            }
                            if (aNewIndex[0] == -1)
                            {
                                aNewIndex[0] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[0]].Copy());
                                pdicRemappedIndices[aIndex[0]] = aNewIndex[0];
                            }

                            if (aNewIndex[1] == -1)
                            {
                                aNewIndex[1] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[1]].Copy());
                                pdicRemappedIndices[aIndex[1]] = aNewIndex[1];
                            }

                            if (aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if (fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[0]);
                            plistObjectIndices.Add(aNewIndex[1]);
                            plistObjectIndices.Add(aNewIndex[3]);

                            Vector3 v4 = plistVertexData[aNewIndex[3]].v3Vertex;

                            if (fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], aVertex[1], aHashVertex[0], aHashVertex[1], aNewIndex[0], aNewIndex[1]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], v4, aHashVertex[1], nHashV4, aNewIndex[1], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[0], nHashV4, aHashVertex[0], aNewIndex[3], aNewIndex[0]);
                            }

                            // Update cap edges and cache

                            if (plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV4, aHashVertex[0], plistVertexData[aNewIndex[3]].v3Vertex, plistVertexData[aNewIndex[0]].v3Vertex);

                            if (bEdge == false) pdicClippedEdges.Add(clippedEdgeKey, new ClippedEdge(aIndex[1], aIndex[2], aNewIndex[1], aNewIndex[2], aNewIndex[3]));

                            // Add geometry of other side

                            if (aSide[2] < 0.0f)
                            {
                                plistVertexData = listVertexDataNeg;
                                plistObjectIndices = listIndicesNeg;
                                pFaceConnectivity = faceConnectivityNeg;
                                pMeshConnectivity = meshConnectivityNeg;
                                pdicClippedEdges = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }
                            else
                            {
                                plistVertexData = listVertexDataPos;
                                plistObjectIndices = listIndicesPos;
                                pFaceConnectivity = faceConnectivityPos;
                                pMeshConnectivity = meshConnectivityPos;
                                pdicClippedEdges = dicClippedEdgesPos;
                                pdicRemappedIndices = dicRemappedIndicesPos;
                            }

                            aNewIndex[0] = -1;
                            aNewIndex[1] = -1;
                            aNewIndex[2] = -1;
                            aNewIndex[3] = -1;
                            bEdge = false;

                            // Find edges in cache

                            if (pdicClippedEdges.ContainsKey(clippedEdgeKey))
                            {
                                nClippedCacheHits++;
                                bEdge = true;
                                aNewIndex[2] = pdicClippedEdges[clippedEdgeKey].GetSecondIndex(aIndex[2]);
                                aNewIndex[3] = pdicClippedEdges[clippedEdgeKey].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if (pdicRemappedIndices.ContainsKey(aIndex[2])) aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                            }

                            // Add vertex data for all data not present in remapped list

                            if (aNewIndex[0] == -1)
                            {
                                if (pdicRemappedIndices.ContainsKey(aIndex[0]))
                                {
                                    aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                                }
                            }
                            if (aNewIndex[0] == -1)
                            {
                                aNewIndex[0] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[0]].Copy());
                                pdicRemappedIndices[aIndex[0]] = aNewIndex[0];
                            }

                            if (aNewIndex[2] == -1)
                            {
                                aNewIndex[2] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[2]].Copy());
                                pdicRemappedIndices[aIndex[2]] = aNewIndex[2];
                            }

                            if (aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if (fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[0]);
                            plistObjectIndices.Add(aNewIndex[3]);
                            plistObjectIndices.Add(aNewIndex[2]);

                            if (fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], v4, aHashVertex[0], nHashV4, aNewIndex[0], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[2], nHashV4, aHashVertex[2], aNewIndex[3], aNewIndex[2]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], aVertex[0], aHashVertex[2], aHashVertex[0], aNewIndex[2], aNewIndex[0]);
                            }

                            // Update cap edges and cache

                            if (plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[0], nHashV4, plistVertexData[aNewIndex[0]].v3Vertex, plistVertexData[aNewIndex[3]].v3Vertex);

                            if (bEdge == false) pdicClippedEdges.Add(clippedEdgeKey, new ClippedEdge(aIndex[1], aIndex[2], aNewIndex[1], aNewIndex[2], aNewIndex[3]));
                        }
                        #endregion
                        #region aVertex[1] almost on the clipping plane
                        else if(aBAlomstInPanel[1])
                        {
                            // aVertex[1] almost on the clipping plane

                            if(aSide[2] < 0.0f)
                            {
                                plistVertexData     = listVertexDataNeg;
                                plistObjectIndices  = listIndicesNeg;
                                pFaceConnectivity   = faceConnectivityNeg;
                                pMeshConnectivity   = meshConnectivityNeg;
                                pdicClippedEdges    = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }

                            clippedEdgeKey = new EdgeKeyByIndex(aIndex[2], aIndex[0]);

                            if(pdicClippedEdges.ContainsKey(clippedEdgeKey))
                            {
                                nClippedCacheHits++;
                                bEdge      = true;
                                aNewIndex[2] = pdicClippedEdges[clippedEdgeKey].GetFirstIndex(aIndex[2]);
                                aNewIndex[3] = pdicClippedEdges[clippedEdgeKey].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[2])) aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                            }

                            // Clip if not present in clipped edge list

                            clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[2], aHashVertex[0]);

                            if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                            {
                                nHashV4 = dicClipVerticesHash[clippedEdgeKeyHash];
                            }
                            else
                            {
                                nHashV4 = nCurrentVertexHash++;
                                dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV4);
                            }

                            VertexData vd4 = new VertexData(nHashV4);

                            if(bEdge == false)
                            {
                                if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[2], aIndex[0], aVertex[2], aVertex[0], planeSplit, ref vd4) == false)
                                {
                                    return false;
                                }
                            }

                            // Add geometry of one side

                            // Add vertex data for all data not present in remapped list

                            if(aNewIndex[1] == -1)
                            {
                                if(pdicRemappedIndices.ContainsKey(aIndex[1]))
                                {
                                    aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                                }
                            }
                            if(aNewIndex[1] == -1)
                            {
                                aNewIndex[1] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[1]].Copy());
                                pdicRemappedIndices[aIndex[1]] = aNewIndex[1];
                            }

                            if(aNewIndex[2] == -1)
                            {
                                aNewIndex[2] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[2]].Copy());
                                pdicRemappedIndices[aIndex[2]] = aNewIndex[2];
                            }

                            if(aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[1]);
                            plistObjectIndices.Add(aNewIndex[2]);
                            plistObjectIndices.Add(aNewIndex[3]);

                            Vector3 v4 = plistVertexData[aNewIndex[3]].v3Vertex;

                            if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], aVertex[2], aHashVertex[1], aHashVertex[2], aNewIndex[1], aNewIndex[2]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], v4, aHashVertex[2], nHashV4, aNewIndex[2], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[1], nHashV4, aHashVertex[1], aNewIndex[3], aNewIndex[1]);
                            }

                            // Update cap edges and cache

                            if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV4, aHashVertex[1], plistVertexData[aNewIndex[3]].v3Vertex, plistVertexData[aNewIndex[1]].v3Vertex);

                            if(bEdge == false) pdicClippedEdges.Add(clippedEdgeKey, new ClippedEdge(aIndex[2], aIndex[0], aNewIndex[2], aNewIndex[0], aNewIndex[3]));

                            // Add geometry of other side

                            if(aSide[0] < 0.0f)
                            {
                                plistVertexData     = listVertexDataNeg;
                                plistObjectIndices  = listIndicesNeg;
                                pFaceConnectivity   = faceConnectivityNeg;
                                pMeshConnectivity   = meshConnectivityNeg;
                                pdicClippedEdges    = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }
                            else
                            {
                                plistVertexData     = listVertexDataPos;
                                plistObjectIndices  = listIndicesPos;
                                pFaceConnectivity   = faceConnectivityPos;
                                pMeshConnectivity   = meshConnectivityPos;
                                pdicClippedEdges    = dicClippedEdgesPos;
                                pdicRemappedIndices = dicRemappedIndicesPos;
                            }

                            aNewIndex[0] = -1;
                            aNewIndex[1] = -1;
                            aNewIndex[3] = -1;
                            bEdge      = false;

                            // Find edges in cache

                            if(pdicClippedEdges.ContainsKey(clippedEdgeKey))
                            {
                                nClippedCacheHits++;
                                bEdge      = true;
                                aNewIndex[0] = pdicClippedEdges[clippedEdgeKey].GetSecondIndex(aIndex[0]);
                                aNewIndex[3] = pdicClippedEdges[clippedEdgeKey].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[0])) aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                            }

                            // Add vertex data for all data not present in remapped list

                            if(aNewIndex[0] == -1)
                            {
                                aNewIndex[0] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[0]].Copy());
                                pdicRemappedIndices[aIndex[0]] = aNewIndex[0];
                            }

                            if(aNewIndex[1] == -1)
                            {
                                if(pdicRemappedIndices.ContainsKey(aIndex[1]))
                                {
                                    aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                                }
                            }
                            if(aNewIndex[1] == -1)
                            {
                                aNewIndex[1] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[1]].Copy());
                                pdicRemappedIndices[aIndex[1]] = aNewIndex[1];
                            }

                            if(aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[1]);
                            plistObjectIndices.Add(aNewIndex[3]);
                            plistObjectIndices.Add(aNewIndex[0]);

                            if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], v4, aHashVertex[1], nHashV4, aNewIndex[1], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[0], nHashV4, aHashVertex[0], aNewIndex[3], aNewIndex[0]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], aVertex[1], aHashVertex[0], aHashVertex[1], aNewIndex[0], aNewIndex[1]);
                            }

                            // Update cap edges and cache

                            if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[1], nHashV4, plistVertexData[aNewIndex[1]].v3Vertex, plistVertexData[aNewIndex[3]].v3Vertex);

                            if(bEdge == false) pdicClippedEdges.Add(clippedEdgeKey, new ClippedEdge(aIndex[2], aIndex[0], aNewIndex[2], aNewIndex[0], aNewIndex[3]));
                        }
                        #endregion
                        #region aVertex[2] almost on the clipping plane
                        else if(aBAlomstInPanel[2])
                        {
                            // v3 almost on the clipping plane

                            if(aSide[0] < 0.0f)
                            {
                                plistVertexData     = listVertexDataNeg;
                                plistObjectIndices  = listIndicesNeg;
                                pFaceConnectivity   = faceConnectivityNeg;
                                pMeshConnectivity   = meshConnectivityNeg;
                                pdicClippedEdges    = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }

                            clippedEdgeKey = new EdgeKeyByIndex(aIndex[0], aIndex[1]);

                            if(pdicClippedEdges.ContainsKey(clippedEdgeKey))
                            {
                                nClippedCacheHits++;
                                bEdge      = true;
                                aNewIndex[0] = pdicClippedEdges[clippedEdgeKey].GetFirstIndex(aIndex[0]);
                                aNewIndex[3] = pdicClippedEdges[clippedEdgeKey].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[0])) aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                            }

                            // Clip if not present in clipped edge list

                            clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[0], aHashVertex[1]);

                            if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                            {
                                nHashV4 = dicClipVerticesHash[clippedEdgeKeyHash];
                            }
                            else
                            {
                                nHashV4 = nCurrentVertexHash++;
                                dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV4);
                            }

                            VertexData vd4 = new VertexData(nHashV4);

                            if(bEdge == false)
                            {
                                if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[0], aIndex[1], aVertex[0], aVertex[1], planeSplit, ref vd4) == false)
                                {
                                    return false;
                                }
                            }

                            // Add geometry of one side

                            // Add vertex data for all data not present in remapped list

                            if(aNewIndex[0] == -1)
                            {
                                aNewIndex[0] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[0]].Copy());
                                pdicRemappedIndices[aIndex[0]] = aNewIndex[0];
                            }

                            if(aNewIndex[2] == -1)
                            {
                                if(pdicRemappedIndices.ContainsKey(aIndex[2]))
                                {
                                    aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                                }
                            }
                            if(aNewIndex[2] == -1)
                            {
                                aNewIndex[2] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[2]].Copy());
                                pdicRemappedIndices[aIndex[2]] = aNewIndex[2];
                            }

                            if(aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[0]);
                            plistObjectIndices.Add(aNewIndex[3]);
                            plistObjectIndices.Add(aNewIndex[2]);

                            Vector3 v4 = plistVertexData[aNewIndex[3]].v3Vertex;

                            if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], v4, aHashVertex[0], nHashV4, aNewIndex[0], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[2], nHashV4, aHashVertex[2], aNewIndex[3], aNewIndex[2]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], aVertex[0], aHashVertex[2], aHashVertex[0], aNewIndex[2], aNewIndex[0]);
                            }

                            // Update cap edges and cache

                            if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV4, aHashVertex[2], plistVertexData[aNewIndex[3]].v3Vertex, plistVertexData[aNewIndex[2]].v3Vertex);

                            if(bEdge == false) pdicClippedEdges.Add(clippedEdgeKey, new ClippedEdge(aIndex[0], aIndex[1], aNewIndex[0], aNewIndex[1], aNewIndex[3]));

                            // Add geometry of other side

                            if(aSide[1] < 0.0f)
                            {
                                plistVertexData     = listVertexDataNeg;
                                plistObjectIndices  = listIndicesNeg;
                                pFaceConnectivity   = faceConnectivityNeg;
                                pMeshConnectivity   = meshConnectivityNeg;
                                pdicClippedEdges    = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }
                            else
                            {
                                plistVertexData     = listVertexDataPos;
                                plistObjectIndices  = listIndicesPos;
                                pFaceConnectivity   = faceConnectivityPos;
                                pMeshConnectivity   = meshConnectivityPos;
                                pdicClippedEdges    = dicClippedEdgesPos;
                                pdicRemappedIndices = dicRemappedIndicesPos;
                            }

                            aNewIndex[1] = -1;
                            aNewIndex[2] = -1;
                            aNewIndex[3] = -1;
                            bEdge      = false;

                            // Find edges in cache

                            if(pdicClippedEdges.ContainsKey(clippedEdgeKey))
                            {
                                nClippedCacheHits++;
                                bEdge      = true;
                                aNewIndex[1] = pdicClippedEdges[clippedEdgeKey].GetSecondIndex(aIndex[1]);
                                aNewIndex[3] = pdicClippedEdges[clippedEdgeKey].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[1])) aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                            }

                            // Add vertex data for all data not present in remapped list

                            if(aNewIndex[1] == -1)
                            {
                                aNewIndex[1] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[1]].Copy());
                                pdicRemappedIndices[aIndex[1]] = aNewIndex[1];
                            }

                            if(aNewIndex[2] == -1)
                            {
                                if(pdicRemappedIndices.ContainsKey(aIndex[2]))
                                {
                                    aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                                }
                            }
                            if(aNewIndex[2] == -1)
                            {
                                aNewIndex[2] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[2]].Copy());
                                pdicRemappedIndices[aIndex[2]] = aNewIndex[2];
                            }

                            if(aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[1]);
                            plistObjectIndices.Add(aNewIndex[2]);
                            plistObjectIndices.Add(aNewIndex[3]);

                            if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], aVertex[2], aHashVertex[1], aHashVertex[2], aNewIndex[1], aNewIndex[2]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], v4, aHashVertex[2], nHashV4, aNewIndex[2], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[1], nHashV4, aHashVertex[1], aNewIndex[3], aNewIndex[1]);
                            }

                            // Update cap edges and cache

                            if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, aHashVertex[2], nHashV4, plistVertexData[aNewIndex[2]].v3Vertex, plistVertexData[aNewIndex[3]].v3Vertex);

                            if(bEdge == false) pdicClippedEdges.Add(clippedEdgeKey, new ClippedEdge(aIndex[0], aIndex[1], aNewIndex[0], aNewIndex[1], aNewIndex[3]));
                        }
                        #endregion
                    }
                    #endregion
                    #region one vertex laying on one side of the clipping plane an the other 2 on the other side
                    else
                    {
                        EdgeKeyByHash clippedEdgeKeyHash;
                        #region aVertex[0] and aVertex[1] on different sides
                        if (aSide[0] * aSide[1] < 0.0f)
                        {
                            #region ... and aVertex[2] on same side as aVertex[0]
                            if (aSide[1] * aSide[2] < 0.0f)
                            {
                                

                                if(aSide[0] < 0.0f)
                                {
                                    plistVertexData     = listVertexDataNeg;
                                    plistObjectIndices  = listIndicesNeg;
                                    pFaceConnectivity   = faceConnectivityNeg;
                                    pMeshConnectivity   = meshConnectivityNeg;
                                    pdicClippedEdges    = dicClippedEdgesNeg;
                                    pdicRemappedIndices = dicRemappedIndicesNeg;
                                }

                                int [] aNewIndex = new int[5];
                                aNewIndex[0] = -1;
                                aNewIndex[1] = -1;
                                aNewIndex[2] = -1;
                                aNewIndex[3] = -1;
                                aNewIndex[4] = -1;
                                int  nHashV4    = -1;
                                int  nHashV5    = -1;
                                bool bEdgeKey1  = false;
                                bool bEdgeKey2  = false;

                                EdgeKeyByIndex edgeKey1 = new EdgeKeyByIndex(aIndex[0], aIndex[1]);
                                EdgeKeyByIndex edgeKey2 = new EdgeKeyByIndex(aIndex[1], aIndex[2]);

                                // Find edges in cache

                                if(pdicClippedEdges.ContainsKey(edgeKey1))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey1  = true;
                                    aNewIndex[0] = pdicClippedEdges[edgeKey1].GetFirstIndex(aIndex[0]);
                                    aNewIndex[3] = pdicClippedEdges[edgeKey1].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[0])) aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                                }

                                if(pdicClippedEdges.ContainsKey(edgeKey2))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey2  = true;
                                    aNewIndex[2] = pdicClippedEdges[edgeKey2].GetSecondIndex(aIndex[2]);
                                    aNewIndex[4] = pdicClippedEdges[edgeKey2].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[2])) aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                                }

                                // Clip if not present in clipped edge list

                                clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[0], aHashVertex[1]);

                                if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                                {
                                    nHashV4 = dicClipVerticesHash[clippedEdgeKeyHash];
                                }
                                else
                                {
                                    nHashV4 = nCurrentVertexHash++;
                                    dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV4);
                                }

                                clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[1], aHashVertex[2]);

                                if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                                {
                                    nHashV5 = dicClipVerticesHash[clippedEdgeKeyHash];
                                }
                                else
                                {
                                    nHashV5 = nCurrentVertexHash++;
                                    dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV5);
                                }

                                VertexData vd4 = new VertexData(nHashV4), vd5 = new VertexData(nHashV5);

                                if(bEdgeKey1 == false)
                                {
                                    if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[0], aIndex[1], aVertex[0], aVertex[1], planeSplit, ref vd4) == false)
                                    {
                                        return false;
                                    }
                                }

                                if(bEdgeKey2 == false)
                                {
                                    if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[1], aIndex[2], aVertex[1], aVertex[2], planeSplit, ref vd5) == false)
                                    {
                                        return false;
                                    }
                                }

                                // Add geometry of one side

                                // Add vertex data for all data not present in remapped list

                                if(aNewIndex[0] == -1)
                                {
                                    aNewIndex[0] = plistVertexData.Count;
                                    plistVertexData.Add(meshDataIn.aVertexData[aIndex[0]].Copy());
                                    pdicRemappedIndices[aIndex[0]] = aNewIndex[0];
                                }

                                if(aNewIndex[2] == -1)
                                {
                                    aNewIndex[2] = plistVertexData.Count;
                                    plistVertexData.Add(meshDataIn.aVertexData[aIndex[2]].Copy());
                                    pdicRemappedIndices[aIndex[2]] = aNewIndex[2];
                                }

                                if(aNewIndex[3] == -1)
                                {
                                    aNewIndex[3] = plistVertexData.Count;
                                    plistVertexData.Add(vd4);
                                }

                                if(aNewIndex[4] == -1)
                                {
                                    aNewIndex[4] = plistVertexData.Count;
                                    plistVertexData.Add(vd5);
                                }

                                if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                                {
                                    pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                                }

                                plistObjectIndices.Add(aNewIndex[0]);
                                plistObjectIndices.Add(aNewIndex[3]);
                                plistObjectIndices.Add(aNewIndex[4]);

                                if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                                {
                                    pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                                }

                                plistObjectIndices.Add(aNewIndex[0]);
                                plistObjectIndices.Add(aNewIndex[4]);
                                plistObjectIndices.Add(aNewIndex[2]);

                                Vector3 v4 = plistVertexData[aNewIndex[3]].v3Vertex;
                                Vector3 v5 = plistVertexData[aNewIndex[4]].v3Vertex;

                                if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                                {
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], v4, aHashVertex[0], nHashV4, aNewIndex[0], aNewIndex[3]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v4, v5, nHashV4, nHashV5, aNewIndex[3], aNewIndex[4]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v5, aVertex[0], nHashV5, aHashVertex[0], aNewIndex[4], aNewIndex[0]);
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], v5, aHashVertex[0], nHashV5, aNewIndex[0], aNewIndex[4]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v5, aVertex[2], nHashV5, aHashVertex[2], aNewIndex[4], aNewIndex[2]);
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], aVertex[0], aHashVertex[2], aHashVertex[0], aNewIndex[2], aNewIndex[0]);
                                }

                                // Update cap edges and cache

                                if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV4, nHashV5, plistVertexData[aNewIndex[3]].v3Vertex, plistVertexData[aNewIndex[4]].v3Vertex);

                                if(pdicClippedEdges.ContainsKey(edgeKey1) == false) pdicClippedEdges.Add(edgeKey1, new ClippedEdge(aIndex[0], aIndex[1], aNewIndex[0], aNewIndex[1], aNewIndex[3]));
                                if(pdicClippedEdges.ContainsKey(edgeKey2) == false) pdicClippedEdges.Add(edgeKey2, new ClippedEdge(aIndex[1], aIndex[2], aNewIndex[1], aNewIndex[2], aNewIndex[4]));

                                // Add geometry of other side

                                if(aSide[1] < 0.0f)
                                {
                                    plistVertexData     = listVertexDataNeg;
                                    plistObjectIndices  = listIndicesNeg;
                                    pFaceConnectivity   = faceConnectivityNeg;
                                    pMeshConnectivity   = meshConnectivityNeg;
                                    pdicClippedEdges    = dicClippedEdgesNeg;
                                    pdicRemappedIndices = dicRemappedIndicesNeg;
                                }
                                else
                                {
                                    plistVertexData     = listVertexDataPos;
                                    plistObjectIndices  = listIndicesPos;
                                    pFaceConnectivity   = faceConnectivityPos;
                                    pMeshConnectivity   = meshConnectivityPos;
                                    pdicClippedEdges    = dicClippedEdgesPos;
                                    pdicRemappedIndices = dicRemappedIndicesPos;
                                }

                                aNewIndex[0] = -1;
                                aNewIndex[1] = -1;
                                aNewIndex[2] = -1;
                                aNewIndex[3] = -1;
                                aNewIndex[4] = -1;
                                bEdgeKey1  = false;
                                bEdgeKey2  = false;

                                // Find edges in cache

                                if(pdicClippedEdges.ContainsKey(edgeKey1))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey1  = true;
                                    aNewIndex[1] = pdicClippedEdges[edgeKey1].GetSecondIndex(aIndex[1]);
                                    aNewIndex[3] = pdicClippedEdges[edgeKey1].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[1])) aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                                }

                                if(pdicClippedEdges.ContainsKey(edgeKey2))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey2  = true;
                                    aNewIndex[1] = pdicClippedEdges[edgeKey2].GetFirstIndex(aIndex[1]);
                                    aNewIndex[4] = pdicClippedEdges[edgeKey2].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[1])) aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                                }

                                // Add vertex data for all data not present in remapped list

                                if(aNewIndex[1] == -1)
                                {
                                    aNewIndex[1] = plistVertexData.Count;
                                    plistVertexData.Add(meshDataIn.aVertexData[aIndex[1]].Copy());
                                    pdicRemappedIndices[aIndex[1]] = aNewIndex[1];
                                }

                                if(aNewIndex[3] == -1)
                                {
                                    aNewIndex[3] = plistVertexData.Count;
                                    plistVertexData.Add(vd4);
                                }

                                if(aNewIndex[4] == -1)
                                {
                                    aNewIndex[4] = plistVertexData.Count;
                                    plistVertexData.Add(vd5);
                                }

                                if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                                {
                                    pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                                }

                                plistObjectIndices.Add(aNewIndex[3]);
                                plistObjectIndices.Add(aNewIndex[1]);
                                plistObjectIndices.Add(aNewIndex[4]);

                                if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                                {
                                    pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[1], nHashV4, aHashVertex[1], aNewIndex[3], aNewIndex[1]);
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], v5, aHashVertex[1], nHashV5, aNewIndex[1], aNewIndex[4]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v5, v4, nHashV5, nHashV4, aNewIndex[4], aNewIndex[3]);
                                }

                                // Update cap edges and cache

                                if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV5, nHashV4, plistVertexData[aNewIndex[4]].v3Vertex, plistVertexData[aNewIndex[3]].v3Vertex);

                                if(pdicClippedEdges.ContainsKey(edgeKey1) == false) pdicClippedEdges.Add(edgeKey1, new ClippedEdge(aIndex[0], aIndex[1], aNewIndex[0], aNewIndex[1], aNewIndex[3]));
                                if(pdicClippedEdges.ContainsKey(edgeKey2) == false) pdicClippedEdges.Add(edgeKey2, new ClippedEdge(aIndex[1], aIndex[2], aNewIndex[1], aNewIndex[2], aNewIndex[4]));
                            }
                            #endregion
                            #region... and aVertex[2] on same side as aVertex[1]
                            else
                            {
                                if(aSide[0] < 0.0f)
                                {
                                    plistVertexData     = listVertexDataNeg;
                                    plistObjectIndices  = listIndicesNeg;
                                    pFaceConnectivity   = faceConnectivityNeg;
                                    pMeshConnectivity   = meshConnectivityNeg;
                                    pdicClippedEdges    = dicClippedEdgesNeg;
                                    pdicRemappedIndices = dicRemappedIndicesNeg;
                                }

                                int [] aNewIndex = new int[5];
                                aNewIndex[0] = -1;
                                aNewIndex[1] = -1;
                                aNewIndex[2] = -1;
                                aNewIndex[3] = -1;
                                aNewIndex[4] = -1;
                                int  nHashV4    = -1;
                                int  nHashV5    = -1;
                                bool bEdgeKey1  = false;
                                bool bEdgeKey3  = false;

                                EdgeKeyByIndex edgeKey1 = new EdgeKeyByIndex(aIndex[0], aIndex[1]);
                                EdgeKeyByIndex edgeKey3 = new EdgeKeyByIndex(aIndex[0], aIndex[2]);

                                // Find edges in cache

                                if(pdicClippedEdges.ContainsKey(edgeKey1))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey1  = true;
                                    aNewIndex[0] = pdicClippedEdges[edgeKey1].GetFirstIndex(aIndex[0]);
                                    aNewIndex[3] = pdicClippedEdges[edgeKey1].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[0])) aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                                }

                                if(pdicClippedEdges.ContainsKey(edgeKey3))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey3  = true;
                                    aNewIndex[0] = pdicClippedEdges[edgeKey3].GetFirstIndex(aIndex[0]);
                                    aNewIndex[4] = pdicClippedEdges[edgeKey3].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[0])) aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                                }

                                // Clip if not present in clipped edge list

                                clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[0], aHashVertex[1]);

                                if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                                {
                                    nHashV4 = dicClipVerticesHash[clippedEdgeKeyHash];
                                }
                                else
                                {
                                    nHashV4 = nCurrentVertexHash++;
                                    dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV4);
                                }

                                clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[0], aHashVertex[2]);

                                if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                                {
                                    nHashV5 = dicClipVerticesHash[clippedEdgeKeyHash];
                                }
                                else
                                {
                                    nHashV5 = nCurrentVertexHash++;
                                    dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV5);
                                }

                                VertexData vd4 = new VertexData(nHashV4), vd5 = new VertexData(nHashV5);

                                if(bEdgeKey1 == false)
                                {
                                    if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[0], aIndex[1], aVertex[0], aVertex[1], planeSplit, ref vd4) == false)
                                    {
                                        return false;
                                    }
                                }

                                if(bEdgeKey3 == false)
                                {
                                    if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[0], aIndex[2], aVertex[0], aVertex[2], planeSplit, ref vd5) == false)
                                    {
                                        return false;
                                    }
                                }

                                // Add geometry of one side

                                // Add vertex data for all data not present in remapped list

                                if(aNewIndex[0] == -1)
                                {
                                    aNewIndex[0] = plistVertexData.Count;
                                    plistVertexData.Add(meshDataIn.aVertexData[aIndex[0]].Copy());
                                    pdicRemappedIndices[aIndex[0]] = aNewIndex[0];
                                }

                                if(aNewIndex[3] == -1)
                                {
                                    aNewIndex[3] = plistVertexData.Count;
                                    plistVertexData.Add(vd4);
                                }

                                if(aNewIndex[4] == -1)
                                {
                                    aNewIndex[4] = plistVertexData.Count;
                                    plistVertexData.Add(vd5);
                                }

                                if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                                {
                                    pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                                }

                                plistObjectIndices.Add(aNewIndex[0]);
                                plistObjectIndices.Add(aNewIndex[3]);
                                plistObjectIndices.Add(aNewIndex[4]);

                                Vector3 v4 = plistVertexData[aNewIndex[3]].v3Vertex;
                                Vector3 v5 = plistVertexData[aNewIndex[4]].v3Vertex;

                                if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                                {
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], v4, aHashVertex[0], nHashV4, aNewIndex[0], aNewIndex[3]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v4, v5, nHashV4, nHashV5, aNewIndex[3], aNewIndex[4]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v5, aVertex[0], nHashV5, aHashVertex[0], aNewIndex[4], aNewIndex[0]);
                                }

                                // Update cap edges and cache

                                if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV4, nHashV5, plistVertexData[aNewIndex[3]].v3Vertex, plistVertexData[aNewIndex[4]].v3Vertex);

                                if(pdicClippedEdges.ContainsKey(edgeKey1) == false) pdicClippedEdges.Add(edgeKey1, new ClippedEdge(aIndex[0], aIndex[1], aNewIndex[0], aNewIndex[1], aNewIndex[3]));
                                if(pdicClippedEdges.ContainsKey(edgeKey3) == false) pdicClippedEdges.Add(edgeKey3, new ClippedEdge(aIndex[0], aIndex[2], aNewIndex[0], aNewIndex[2], aNewIndex[4]));

                                // Add geometry of other side

                                if(aSide[1] < 0.0f)
                                {
                                    plistVertexData     = listVertexDataNeg;
                                    plistObjectIndices  = listIndicesNeg;
                                    pFaceConnectivity   = faceConnectivityNeg;
                                    pMeshConnectivity   = meshConnectivityNeg;
                                    pdicClippedEdges    = dicClippedEdgesNeg;
                                    pdicRemappedIndices = dicRemappedIndicesNeg;
                                }
                                else
                                {
                                    plistVertexData     = listVertexDataPos;
                                    plistObjectIndices  = listIndicesPos;
                                    pFaceConnectivity   = faceConnectivityPos;
                                    pMeshConnectivity   = meshConnectivityPos;
                                    pdicClippedEdges    = dicClippedEdgesPos;
                                    pdicRemappedIndices = dicRemappedIndicesPos;
                                }

                                aNewIndex[0] = -1;
                                aNewIndex[1] = -1;
                                aNewIndex[2] = -1;
                                aNewIndex[3] = -1;
                                aNewIndex[4] = -1;
                                bEdgeKey1  = false;
                                bEdgeKey3  = false;

                                // Find edges in cache

                                if(pdicClippedEdges.ContainsKey(edgeKey1))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey1  = true;
                                    aNewIndex[1] = pdicClippedEdges[edgeKey1].GetSecondIndex(aIndex[1]);
                                    aNewIndex[3] = pdicClippedEdges[edgeKey1].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[1])) aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                                }

                                if(pdicClippedEdges.ContainsKey(edgeKey3))
                                {
                                    nClippedCacheHits++;
                                    bEdgeKey3  = true;
                                    aNewIndex[2] = pdicClippedEdges[edgeKey3].GetSecondIndex(aIndex[2]);
                                    aNewIndex[4] = pdicClippedEdges[edgeKey3].nClippedIndex;
                                }
                                else
                                {
                                    nClippedCacheMisses++;
                                    if(pdicRemappedIndices.ContainsKey(aIndex[2])) aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                                }

                                // Add vertex data for all data not present in remapped list

                                if(aNewIndex[1] == -1)
                                {
                                    aNewIndex[1] = plistVertexData.Count;
                                    plistVertexData.Add(meshDataIn.aVertexData[aIndex[1]].Copy());
                                    pdicRemappedIndices[aIndex[1]] = aNewIndex[1];
                                }

                                if(aNewIndex[2] == -1)
                                {
                                    aNewIndex[2] = plistVertexData.Count;
                                    plistVertexData.Add(meshDataIn.aVertexData[aIndex[2]].Copy());
                                    pdicRemappedIndices[aIndex[2]] = aNewIndex[2];
                                }

                                if(aNewIndex[3] == -1)
                                {
                                    aNewIndex[3] = plistVertexData.Count;
                                    plistVertexData.Add(vd4);
                                }

                                if(aNewIndex[4] == -1)
                                {
                                    aNewIndex[4] = plistVertexData.Count;
                                    plistVertexData.Add(vd5);
                                }

                                if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                                {
                                    pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                                }

                                plistObjectIndices.Add(aNewIndex[3]);
                                plistObjectIndices.Add(aNewIndex[1]);
                                plistObjectIndices.Add(aNewIndex[2]);

                                if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                                {
                                    pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                                }

                                plistObjectIndices.Add(aNewIndex[3]);
                                plistObjectIndices.Add(aNewIndex[2]);
                                plistObjectIndices.Add(aNewIndex[4]);

                                if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                                {
                                    pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[1], nHashV4, aHashVertex[1], aNewIndex[3], aNewIndex[1]);
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], aVertex[2], aHashVertex[1], aHashVertex[2], aNewIndex[1], aNewIndex[2]);
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], v4, aHashVertex[2], nHashV4, aNewIndex[2], aNewIndex[3]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[2], nHashV4, aHashVertex[2], aNewIndex[3], aNewIndex[2]);
                                    pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], v5, aHashVertex[2], nHashV5, aNewIndex[2], aNewIndex[4]);
                                    pFaceConnectivity.AddEdge(nSubMesh, v5, v4, nHashV5, nHashV4, aNewIndex[4], aNewIndex[3]);
                                }

                                // Update cap edges and cache

                                if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV5, nHashV4, plistVertexData[aNewIndex[4]].v3Vertex, plistVertexData[aNewIndex[3]].v3Vertex);

                                if(pdicClippedEdges.ContainsKey(edgeKey1) == false) pdicClippedEdges.Add(edgeKey1, new ClippedEdge(aIndex[0], aIndex[1], aNewIndex[0], aNewIndex[1], aNewIndex[3]));
                                if(pdicClippedEdges.ContainsKey(edgeKey3) == false) pdicClippedEdges.Add(edgeKey3, new ClippedEdge(aIndex[0], aIndex[2], aNewIndex[0], aNewIndex[2], aNewIndex[4]));
                            }
                            #endregion
                        }
                        #endregion
                        #region aVertex[0] and aVertex[1] on same side, and v3 on different side
                        else if(aSide[1] * aSide[2] < 0.0f)
                        {
                            

                            if(aSide[0] < 0.0f)
                            {
                                plistVertexData     = listVertexDataNeg;
                                plistObjectIndices  = listIndicesNeg;
                                pFaceConnectivity   = faceConnectivityNeg;
                                pMeshConnectivity   = meshConnectivityNeg;
                                pdicClippedEdges    = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }

                            int [] aNewIndex = new int[5];
                            aNewIndex[0] = -1;
                            aNewIndex[1] = -1;
                            aNewIndex[2] = -1;
                            aNewIndex[3] = -1;
                            aNewIndex[4] = -1;
                            int  nHashV4    = -1;
                            int  nHashV5    = -1;
                            bool bEdgeKey2  = false;
                            bool bEdgeKey3  = false;

                            EdgeKeyByIndex edgeKey2 = new EdgeKeyByIndex(aIndex[1], aIndex[2]);
                            EdgeKeyByIndex edgeKey3 = new EdgeKeyByIndex(aIndex[0], aIndex[2]);

                            // Find edges in cache

                            if(pdicClippedEdges.ContainsKey(edgeKey2))
                            {
                                nClippedCacheHits++;
                                bEdgeKey2  = true;
                                aNewIndex[1] = pdicClippedEdges[edgeKey2].GetFirstIndex(aIndex[1]);
                                aNewIndex[4] = pdicClippedEdges[edgeKey2].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[1])) aNewIndex[1] = pdicRemappedIndices[aIndex[1]];
                            }

                            if(pdicClippedEdges.ContainsKey(edgeKey3))
                            {
                                nClippedCacheHits++;
                                bEdgeKey3  = true;
                                aNewIndex[0] = pdicClippedEdges[edgeKey3].GetFirstIndex(aIndex[0]);
                                aNewIndex[3] = pdicClippedEdges[edgeKey3].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[0])) aNewIndex[0] = pdicRemappedIndices[aIndex[0]];
                            }

                            // Clip if not present in clipped edge list

                            clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[0], aHashVertex[2]);

                            if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                            {
                                nHashV4 = dicClipVerticesHash[clippedEdgeKeyHash];
                            }
                            else
                            {
                                nHashV4 = nCurrentVertexHash++;
                                dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV4);
                            }

                            clippedEdgeKeyHash = new EdgeKeyByHash(aHashVertex[1], aHashVertex[2]);

                            if(dicClipVerticesHash.ContainsKey(clippedEdgeKeyHash))
                            {
                                nHashV5 = dicClipVerticesHash[clippedEdgeKeyHash];
                            }
                            else
                            {
                                nHashV5 = nCurrentVertexHash++;
                                dicClipVerticesHash.Add(clippedEdgeKeyHash, nHashV5);
                            }

                            VertexData vd4 = new VertexData(nHashV4), vd5 = new VertexData(nHashV5);

                            if(bEdgeKey2 == false)
                            {
                                if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[1], aIndex[2], aVertex[1], aVertex[2], planeSplit, ref vd5) == false)
                                {
                                    return false;
                                }
                            }

                            if(bEdgeKey3 == false)
                            {
                                if(VertexData.ClipAgainstPlane(meshDataIn.aVertexData, aIndex[0], aIndex[2], aVertex[0], aVertex[2], planeSplit, ref vd4) == false)
                                {
                                    return false;
                                }
                            }

                            // Add geometry of one side

                            // Add vertex data for all data not present in remapped list

                            if(aNewIndex[0] == -1)
                            {
                                aNewIndex[0] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[0]].Copy());
                                pdicRemappedIndices[aIndex[0]] = aNewIndex[0];
                            }

                            if(aNewIndex[1] == -1)
                            {
                                aNewIndex[1] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[1]].Copy());
                                pdicRemappedIndices[aIndex[1]] = aNewIndex[1];
                            }

                            if(aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if(aNewIndex[4] == -1)
                            {
                                aNewIndex[4] = plistVertexData.Count;
                                plistVertexData.Add(vd5);
                            }

                            if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[1]);
                            plistObjectIndices.Add(aNewIndex[4]);
                            plistObjectIndices.Add(aNewIndex[3]);

                            if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[1]);
                            plistObjectIndices.Add(aNewIndex[3]);
                            plistObjectIndices.Add(aNewIndex[0]);

                            Vector3 v4 = plistVertexData[aNewIndex[3]].v3Vertex;
                            Vector3 v5 = plistVertexData[aNewIndex[4]].v3Vertex;

                            if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], v5, aHashVertex[1], nHashV5, aNewIndex[1], aNewIndex[4]);
                                pFaceConnectivity.AddEdge(nSubMesh, v5, v4, nHashV5, nHashV4, aNewIndex[4], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[1], nHashV4, aHashVertex[1], aNewIndex[3], aNewIndex[1]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[1], v4, aHashVertex[1], nHashV4, aNewIndex[1], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, aVertex[0], nHashV4, aHashVertex[0], aNewIndex[3], aNewIndex[0]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[0], aVertex[1], aHashVertex[0], aHashVertex[1], aNewIndex[0], aNewIndex[1]);
                            }

                            // Update cap edges and cache

                            if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV5, nHashV4, plistVertexData[aNewIndex[4]].v3Vertex, plistVertexData[aNewIndex[3]].v3Vertex);

                            if(pdicClippedEdges.ContainsKey(edgeKey2) == false) pdicClippedEdges.Add(edgeKey2, new ClippedEdge(aIndex[1], aIndex[2], aNewIndex[1], aNewIndex[2], aNewIndex[4]));
                            if(pdicClippedEdges.ContainsKey(edgeKey3) == false) pdicClippedEdges.Add(edgeKey3, new ClippedEdge(aIndex[0], aIndex[2], aNewIndex[0], aNewIndex[2], aNewIndex[3]));

                            // Add geometry of other side

                            if(aSide[2] < 0.0f)
                            {
                                plistVertexData     = listVertexDataNeg;
                                plistObjectIndices  = listIndicesNeg;
                                pFaceConnectivity   = faceConnectivityNeg;
                                pMeshConnectivity   = meshConnectivityNeg;
                                pdicClippedEdges    = dicClippedEdgesNeg;
                                pdicRemappedIndices = dicRemappedIndicesNeg;
                            }
                            else
                            {
                                plistVertexData     = listVertexDataPos;
                                plistObjectIndices  = listIndicesPos;
                                pFaceConnectivity   = faceConnectivityPos;
                                pMeshConnectivity   = meshConnectivityPos;
                                pdicClippedEdges    = dicClippedEdgesPos;
                                pdicRemappedIndices = dicRemappedIndicesPos;
                            }

                            aNewIndex[0] = -1;
                            aNewIndex[1] = -1;
                            aNewIndex[2] = -1;
                            aNewIndex[3] = -1;
                            aNewIndex[4] = -1;
                            bEdgeKey2  = false;
                            bEdgeKey3  = false;

                            // Find edges in cache

                            if(pdicClippedEdges.ContainsKey(edgeKey2))
                            {
                                nClippedCacheHits++;
                                bEdgeKey2  = true;
                                aNewIndex[2] = pdicClippedEdges[edgeKey2].GetSecondIndex(aIndex[2]);
                                aNewIndex[4] = pdicClippedEdges[edgeKey2].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[2])) aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                            }

                            if(pdicClippedEdges.ContainsKey(edgeKey3))
                            {
                                nClippedCacheHits++;
                                bEdgeKey3  = true;
                                aNewIndex[2] = pdicClippedEdges[edgeKey3].GetSecondIndex(aIndex[2]);
                                aNewIndex[3] = pdicClippedEdges[edgeKey3].nClippedIndex;
                            }
                            else
                            {
                                nClippedCacheMisses++;
                                if(pdicRemappedIndices.ContainsKey(aIndex[2])) aNewIndex[2] = pdicRemappedIndices[aIndex[2]];
                            }

                            // Add vertex data for all data not present in remapped list

                            if(aNewIndex[2] == -1)
                            {
                                aNewIndex[2] = plistVertexData.Count;
                                plistVertexData.Add(meshDataIn.aVertexData[aIndex[2]].Copy());
                                pdicRemappedIndices[aIndex[2]] = aNewIndex[2];
                            }

                            if(aNewIndex[3] == -1)
                            {
                                aNewIndex[3] = plistVertexData.Count;
                                plistVertexData.Add(vd4);
                            }

                            if(aNewIndex[4] == -1)
                            {
                                aNewIndex[4] = plistVertexData.Count;
                                plistVertexData.Add(vd5);
                            }

                            if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoChunkConnectionInfo == false)
                            {
                                pMeshConnectivity.NotifyNewClippedFace(meshDataIn, nSubMesh, i, nSubMesh, plistObjectIndices.Count / 3);
                            }

                            plistObjectIndices.Add(aNewIndex[4]);
                            plistObjectIndices.Add(aNewIndex[2]);
                            plistObjectIndices.Add(aNewIndex[3]);

                            if(fracturedComponent.GenerateIslands && splitOptions.bForceNoIslandGeneration == false)
                            {
                                pFaceConnectivity.AddEdge(nSubMesh, v5, aVertex[2], nHashV5, aHashVertex[2], aNewIndex[4], aNewIndex[2]);
                                pFaceConnectivity.AddEdge(nSubMesh, aVertex[2], v4, aHashVertex[2], nHashV4, aNewIndex[2], aNewIndex[3]);
                                pFaceConnectivity.AddEdge(nSubMesh, v4, v5, nHashV4, nHashV5, aNewIndex[3], aNewIndex[4]);
                            }

                            // Update cap edges and cache

                            if(plistVertexData == listVertexDataPos && splitOptions.bForceNoCap == false) AddCapEdge(dicCapEdges, nHashV4, nHashV5, plistVertexData[aNewIndex[3]].v3Vertex, plistVertexData[aNewIndex[4]].v3Vertex);

                            if(pdicClippedEdges.ContainsKey(edgeKey2) == false) pdicClippedEdges.Add(edgeKey2, new ClippedEdge(aIndex[1], aIndex[2], aNewIndex[1], aNewIndex[2], aNewIndex[4]));
                            if(pdicClippedEdges.ContainsKey(edgeKey3) == false) pdicClippedEdges.Add(edgeKey3, new ClippedEdge(aIndex[0], aIndex[2], aNewIndex[0], aNewIndex[2], aNewIndex[3]));
                        }
                        #endregion
                    }
                    #endregion
                }
            }

//          Debug.Log("Clipped cache hits " + nClippedCacheHits + " clipped cache misses " + nClippedCacheMisses);

            // Compute transforms

            Vector3 v3CenterPos = Vector3.zero;

            if(listVertexDataPos.Count > 0)
            {
                Vector3 v3Min = Vector3.zero, v3Max = Vector3.zero;
                MeshData.ComputeMinMax(listVertexDataPos, ref v3Min, ref v3Max);
                v3CenterPos = (v3Min + v3Max) * 0.5f;
            }

            Matrix4x4 mtxToLocalPos = Matrix4x4.TRS(v3CenterPos, meshDataIn.qRotation, meshDataIn.v3Scale).inverse;

            if(splitOptions.bVerticesAreLocal)
            {
                mtxToLocalPos = Matrix4x4.TRS(v3CenterPos, Quaternion.identity, Vector3.one).inverse;
            }

            Vector3 v3CenterNeg = Vector3.zero;

            if(listVertexDataNeg.Count > 0)
            {
                Vector3 v3Min = Vector3.zero, v3Max = Vector3.zero;
                MeshData.ComputeMinMax(listVertexDataNeg, ref v3Min, ref v3Max);
                v3CenterNeg = (v3Min + v3Max) * 0.5f;
            }

            Matrix4x4 mtxToLocalNeg = Matrix4x4.TRS(v3CenterNeg, meshDataIn.qRotation, meshDataIn.v3Scale).inverse;

            if(splitOptions.bVerticesAreLocal)
            {
                mtxToLocalNeg = Matrix4x4.TRS(v3CenterNeg, Quaternion.identity, Vector3.one).inverse;
            }

            // Resolve cap outline and add its geometry

            List<List<Vector3>> listlistResolvedCapVertices   = new List<List<Vector3>>();
            List<List<int>>     listlistResolvedCapHashValues = new List<List<int>>();

            bool bNeedsConnectivityPostprocess = false;

            Matrix4x4 mtxPlane = Matrix4x4.TRS(v3PlanePoint, Quaternion.LookRotation(Vector3.Cross(v3PlaneNormal, v3PlaneRight), v3PlaneNormal), Vector3.one);

            if(dicCapEdges.Count > 0 && splitOptions.bForceNoCap == false)
            {
                if(ResolveCap(dicCapEdges, listlistResolvedCapVertices, listlistResolvedCapHashValues, fracturedComponent))
                {
                    if(listlistResolvedCapVertices.Count > 1)
                    {
                        // There's more than one closed cap. We need to postprocess the mesh because there may be more than one object on a side of the plane as a result of the clipping.
                        bNeedsConnectivityPostprocess = (fracturedComponent.GenerateIslands && (splitOptions.bForceNoIslandGeneration == false)) ? true : false;
                    }

                    TriangulateConstrainedDelaunay( listlistResolvedCapVertices, listlistResolvedCapHashValues, splitOptions.bForceCapVertexSoup, fracturedComponent, bNeedsConnectivityPostprocess, faceConnectivityPos, faceConnectivityNeg,
                                                    meshConnectivityPos, meshConnectivityNeg, splitOptions.nForceMeshConnectivityHash, nSplitCloseSubMesh,
                                                    mtxPlane, mtxToLocalPos, mtxToLocalNeg, v3CenterPos, v3CenterNeg,
                                                    alistIndicesPos, listVertexDataPos, alistIndicesNeg, listVertexDataNeg);
                }
                else
                {
                    if(fracturedComponent.Verbose) Debug.LogWarning("Error resolving cap");
                }
            }

            // Postprocess if necessary
            if(bNeedsConnectivityPostprocess)
            {
                // Search for multiple objects inside each meshes

                List<MeshData> listIslandsPos = MeshData.PostProcessConnectivity(meshDataIn, faceConnectivityPos, meshConnectivityPos, alistIndicesPos, listVertexDataPos, nSplitCloseSubMesh, nCurrentVertexHash, false);
                List<MeshData> listIslandsNeg = new List<MeshData>();

                if(splitOptions.bIgnoreNegativeSide == false)
                {
                    listIslandsNeg = MeshData.PostProcessConnectivity(meshDataIn, faceConnectivityNeg, meshConnectivityNeg, alistIndicesNeg, listVertexDataNeg, nSplitCloseSubMesh, nCurrentVertexHash, false);
                }

                // Sometimes we are feed a mesh with multiple islands as input. If this is the case, compute connectivity between islands at this point.

                List<MeshData> listTotalIslands = new List<MeshData>();
                listTotalIslands.AddRange(listIslandsPos);
                listTotalIslands.AddRange(listIslandsNeg);

                if(fracturedComponent.GenerateChunkConnectionInfo && splitOptions.bForceNoIslandConnectionInfo == false)
                {
                    for(int i = 0; i < listTotalIslands.Count; i++)
                    {
                        if(progress != null && listTotalIslands.Count > 10 && splitOptions.bForceNoProgressInfo == false)
                        {
                            progress("Fracturing", "Processing island connectivity...", i / (float)listTotalIslands.Count);
                            if(Fracturer.IsFracturingCancelled()) return false;
                        }

                        for(int j = 0; j < listTotalIslands.Count; j++)
                        {
                            if(i != j)
                            {
                                ComputeIslandsMeshDataConnectivity(fracturedComponent, splitOptions.bVerticesAreLocal, listTotalIslands[i], listTotalIslands[j]);
                            }
                        }
                    }
                }

                listMeshDatasPosOut.AddRange(listIslandsPos);
                listMeshDatasNegOut.AddRange(listIslandsNeg);
            }
            else
            {
                // Create new MeshDatas

                if(listVertexDataPos.Count > 0 && alistIndicesPos.Length > 0)
                {
                    MeshData newMeshData = new MeshData(meshDataIn.aMaterials, alistIndicesPos, listVertexDataPos, nSplitCloseSubMesh, v3CenterPos, meshDataIn.qRotation, meshDataIn.v3Scale, mtxToLocalPos, false, false);
                    newMeshData.meshDataConnectivity = meshConnectivityPos;
                    newMeshData.nCurrentVertexHash   = nCurrentVertexHash;
                    listMeshDatasPosOut.Add(newMeshData);
                }

                if(listVertexDataNeg.Count > 0 && alistIndicesNeg.Length > 0 && splitOptions.bIgnoreNegativeSide == false)
                {
                    MeshData newMeshData = new MeshData(meshDataIn.aMaterials, alistIndicesNeg, listVertexDataNeg, nSplitCloseSubMesh, v3CenterNeg, meshDataIn.qRotation, meshDataIn.v3Scale, mtxToLocalNeg, false, false);
                    newMeshData.meshDataConnectivity = meshConnectivityNeg;
                    newMeshData.nCurrentVertexHash   = nCurrentVertexHash;
                    listMeshDatasNegOut.Add(newMeshData);
                }
            }

            return true;
        }

        private static bool ComputeIslandsMeshDataConnectivity(FracturedObject fracturedComponent, bool bVerticesAreLocal, MeshData meshData1, MeshData meshData2)
        {
            float fMargin = fracturedComponent.ChunkIslandConnectionMaxDistance;

            // Vertices and min/max may be in local space. We want distance checks to be in world space

            Vector3 v3Min1 = meshData1.v3Min; if(bVerticesAreLocal) v3Min1 = Vector3.Scale(v3Min1, meshData1.v3Scale);
            Vector3 v3Max1 = meshData1.v3Max; if(bVerticesAreLocal) v3Max1 = Vector3.Scale(v3Max1, meshData1.v3Scale);
            Vector3 v3Min2 = meshData2.v3Min; if(bVerticesAreLocal) v3Min2 = Vector3.Scale(v3Min2, meshData2.v3Scale);
            Vector3 v3Max2 = meshData2.v3Max; if(bVerticesAreLocal) v3Max2 = Vector3.Scale(v3Max2, meshData2.v3Scale);

            if((v3Min1.x > (v3Max2.x + fMargin)) || (v3Min1.y > (v3Max2.y + fMargin)) || (v3Min1.z > (v3Max2.z + fMargin)))
            {
                return false;
            }

            if((v3Min2.x > (v3Max1.x + fMargin)) || (v3Min2.y > (v3Max1.y + fMargin)) || (v3Min2.z > (v3Max1.z + fMargin)))
            {
                return false;
            }

            bool  bConnected    = false;
            float fDistPlaneMax = fracturedComponent.ChunkIslandConnectionMaxDistance;

            for(int nSubMesh1 = 0; nSubMesh1 < meshData1.aaIndices.Length; nSubMesh1++)
            {
                for(int nFace1 = 0; nFace1 < meshData1.aaIndices[nSubMesh1].Length / 3; nFace1++)
                {
                    Vector3 v1 = meshData1.aVertexData[meshData1.aaIndices[nSubMesh1][nFace1 * 3 + 0]].v3Vertex;
                    Vector3 v2 = meshData1.aVertexData[meshData1.aaIndices[nSubMesh1][nFace1 * 3 + 1]].v3Vertex;
                    Vector3 v3 = meshData1.aVertexData[meshData1.aaIndices[nSubMesh1][nFace1 * 3 + 2]].v3Vertex;

                    if(bVerticesAreLocal)
                    {
                        v1 = Vector3.Scale(v1, meshData1.v3Scale);
                        v2 = Vector3.Scale(v2, meshData1.v3Scale);
                        v3 = Vector3.Scale(v3, meshData1.v3Scale);
                    }

                    Vector3 v3Forward = -Vector3.Cross(v2 - v1, v3 - v1);
                    float   fArea1    = v3Forward.magnitude;

                    if(fArea1 < Parameters.EPSILONCROSSPRODUCT)
                    {
                        continue;
                    }

                    Quaternion qFace     = Quaternion.LookRotation(v3Forward.normalized, (v2 - v1).normalized);
                    Matrix4x4  mtxToFace = Matrix4x4.TRS(v1, qFace, Vector3.one).inverse;

                    Plane planeFace1 = new Plane(v1, v2, v3);

                    for(int nSubMesh2 = 0; nSubMesh2 < meshData2.aaIndices.Length; nSubMesh2++)
                    {
                        for(int nFace2 = 0; nFace2 < meshData2.aaIndices[nSubMesh2].Length / 3; nFace2++)
                        {
                            Vector3 v3Other1 = meshData2.aVertexData[meshData2.aaIndices[nSubMesh2][nFace2 * 3 + 0]].v3Vertex;
                            Vector3 v3Other2 = meshData2.aVertexData[meshData2.aaIndices[nSubMesh2][nFace2 * 3 + 1]].v3Vertex;
                            Vector3 v3Other3 = meshData2.aVertexData[meshData2.aaIndices[nSubMesh2][nFace2 * 3 + 2]].v3Vertex;

                            if(bVerticesAreLocal)
                            {
                                v3Other1 = Vector3.Scale(v3Other1, meshData2.v3Scale);
                                v3Other2 = Vector3.Scale(v3Other2, meshData2.v3Scale);
                                v3Other3 = Vector3.Scale(v3Other3, meshData2.v3Scale);
                            }

                            // Compute distance from face1 to face2

                            float fDist1 = Mathf.Abs(planeFace1.GetDistanceToPoint(v3Other1)); if(fDist1 > fDistPlaneMax) continue;
                            float fDist2 = Mathf.Abs(planeFace1.GetDistanceToPoint(v3Other2)); if(fDist2 > fDistPlaneMax) continue;
                            float fDist3 = Mathf.Abs(planeFace1.GetDistanceToPoint(v3Other3)); if(fDist3 > fDistPlaneMax) continue;

                            // See if they intersect in 2D (face 1 local coordinates)

                            Vector3 v3OtherCenterLocal = (v3Other1 + v3Other2 + v3Other3) / 3.0f;
                            v3OtherCenterLocal = mtxToFace.MultiplyPoint3x4(v3OtherCenterLocal);

                            Vector3 v3Local1 = mtxToFace.MultiplyPoint3x4(v1);
                            Vector3 v3Local2 = mtxToFace.MultiplyPoint3x4(v2);
                            Vector3 v3Local3 = mtxToFace.MultiplyPoint3x4(v3);
                            Vector3 v3Edge2  = v3Local3 - v3Local2;
                            Vector3 v3Edge3  = v3Local1 - v3Local3;

                            bool bFaceConnected = false;

                            // Test the center

                            if(v3OtherCenterLocal.x >= 0.0f)
                            {
                                if(Vector3.Cross(v3Edge2, v3OtherCenterLocal - v3Local2).z <= 0.0f)
                                {
                                    if(Vector3.Cross(v3Edge3, v3OtherCenterLocal - v3Local3).z <= 0.0f)
                                    {
                                        bFaceConnected = true;
                                    }
                                }
                            }

                            if(bFaceConnected == false)
                            {
                                // Try intersecting lines

                                Vector3 v3OtherLocal1 = mtxToFace.MultiplyPoint3x4(v3Other1);
                                Vector3 v3OtherLocal2 = mtxToFace.MultiplyPoint3x4(v3Other2);
                                Vector3 v3OtherLocal3 = mtxToFace.MultiplyPoint3x4(v3Other3);

                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal1.x, v3OtherLocal1.y, v3OtherLocal2.x, v3OtherLocal2.y, v3Local1.x, v3Local1.y, v3Local2.x, v3Local2.y)) bFaceConnected = true;
                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal1.x, v3OtherLocal1.y, v3OtherLocal2.x, v3OtherLocal2.y, v3Local2.x, v3Local2.y, v3Local3.x, v3Local3.y)) bFaceConnected = true;
                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal1.x, v3OtherLocal1.y, v3OtherLocal2.x, v3OtherLocal2.y, v3Local3.x, v3Local3.y, v3Local1.x, v3Local1.y)) bFaceConnected = true;

                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal2.x, v3OtherLocal2.y, v3OtherLocal3.x, v3OtherLocal3.y, v3Local1.x, v3Local1.y, v3Local2.x, v3Local2.y)) bFaceConnected = true;
                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal2.x, v3OtherLocal2.y, v3OtherLocal3.x, v3OtherLocal3.y, v3Local2.x, v3Local2.y, v3Local3.x, v3Local3.y)) bFaceConnected = true;
                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal2.x, v3OtherLocal2.y, v3OtherLocal3.x, v3OtherLocal3.y, v3Local3.x, v3Local3.y, v3Local1.x, v3Local1.y)) bFaceConnected = true;

                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal3.x, v3OtherLocal3.y, v3OtherLocal1.x, v3OtherLocal1.y, v3Local1.x, v3Local1.y, v3Local2.x, v3Local2.y)) bFaceConnected = true;
                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal3.x, v3OtherLocal3.y, v3OtherLocal1.x, v3OtherLocal1.y, v3Local2.x, v3Local2.y, v3Local3.x, v3Local3.y)) bFaceConnected = true;
                                if(bFaceConnected == false) if(IntersectEdges2D(v3OtherLocal3.x, v3OtherLocal3.y, v3OtherLocal1.x, v3OtherLocal1.y, v3Local3.x, v3Local3.y, v3Local1.x, v3Local1.y)) bFaceConnected = true;
                            }

                            if(bFaceConnected)
                            {
                                int nHash = MeshDataConnectivity.GetNewHash(); // New hash value to identify the 2 shared faces
                                meshData1.meshDataConnectivity.NotifyNewCapFace(nHash, nSubMesh1, nFace1);
                                meshData2.meshDataConnectivity.NotifyNewCapFace(nHash, nSubMesh2, nFace2);
                                bConnected = true;
                            }
                        }
                    }
                }
            }

            return bConnected;
        }

        public static bool IntersectEdges2D(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            Vector2 p = new Vector2(x1, y1);
            Vector2 q = new Vector2(x3, y3);
            Vector2 r = new Vector2(x2 - x1, y2 - y1);
            Vector2 s = new Vector2(x4 - x3, y4 - y3);

            float fCross   = CrossProduct2D(r, s);

            if(fCross < Parameters.EPSILONCROSSPRODUCT)
            {
                return false;
            }

            float t = CrossProduct2D((q - p), s) / fCross;
            float u = CrossProduct2D((q - p), r) / fCross;

            float fNegativeMargin = Parameters.EPSILONINSIDETRIANGLE;

            if(t >= fNegativeMargin && t <= (1.0f - fNegativeMargin) && u >= fNegativeMargin && u <= (1.0f - fNegativeMargin))
            {
                return true;
            }

            return false;
        }

        private static float CrossProduct2D(Vector2 a, Vector2 b)
        {
            return (a.x * b.y) - (a.y * b.x);
        }
    }
}