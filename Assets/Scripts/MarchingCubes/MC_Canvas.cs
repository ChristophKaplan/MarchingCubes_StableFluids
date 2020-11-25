using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;


public class MC_Canvas {
    public bool loading;
    public float isoLevel = 0.5f;
    public MC_Point[] points;

    public GameObject[,,] chunkObjs;
    public MC_Chunk[,,] chunks;

    public Vector3 worldPosition;

    public int worldSizeX;
    public int worldSizeY;
    public int worldSizeZ;
    public int chunkSizeX;
    public int chunkSizeY;
    public int chunkSizeZ;
    public int chunkAmountX;
    public int chunkAmountY;
    public int chunkAmountZ;
    public Vector3 voxelSize;
    public Vector3 actualWorldSize;
    public Vector3 actualChunkSize;



    public MC_Canvas(string seed, Vector3 pos, Vector3Int cAmount, Vector3Int cSize, Vector3 vSize) {
        RandomNumberGenerator.SetSeed(seed);

        worldPosition = pos;
        voxelSize = vSize;
        chunkSizeX = cSize.x; chunkSizeY = cSize.y; chunkSizeZ = cSize.z;
        chunkAmountX = cAmount.x; chunkAmountY = cAmount.y; chunkAmountZ = cAmount.z;

        worldSizeX = chunkAmountX * chunkSizeX;
        worldSizeY = chunkAmountY * chunkSizeY;
        worldSizeZ = chunkAmountZ * chunkSizeZ;

        actualChunkSize = new Vector3(chunkSizeX * voxelSize.x, chunkSizeY * voxelSize.y, chunkSizeZ * voxelSize.z);
        actualWorldSize = new Vector3(actualChunkSize.x * chunkAmountX, actualChunkSize.y * chunkAmountY, actualChunkSize.z * chunkAmountZ);
    }

    public IEnumerator LoadWorld(bool objInit = true, bool fillNoise = true) {
        //Using Coroutine since it can be costly with high sizes.

        loading = true;
        InitPoints();
        yield return null;
        NeighbouringPoints();
        yield return null;
        FillPoints(fillNoise);
        yield return null;
        InitChunks();
        yield return null;
        if (objInit) InitChunkObjs();
        yield return null;
        RenderChunks();
        yield return null;
        loading = false;
        Debug.Log("done!");
    }
    public bool IsPointWithinWorldBounds(int x, int y, int z, int border = 0) {
        return x >= 0 + border && y >= 0 + border && z >= 0 + border && x < worldSizeX - border && y < worldSizeY - border && z < worldSizeZ - border;
    }
    public int GetPointIndex(int x, int y, int z) {
        return (x + y * worldSizeX + z * worldSizeX * worldSizeY);
    }

    public MC_Point GetPointAtWorldCoord(int x, int y, int z, bool makeEmpty = false) {
        if (!IsPointWithinWorldBounds(x, y, z)) {
            if (makeEmpty) return new MC_Point(this, 0, x, y, z, 0, true);
            return null;
        }
        return points[GetPointIndex(x, y, z)];
    }
    public MC_Chunk GetChunkAt(int x, int y, int z) {
        if (x >= 0 && y >= 0 && z >= 0 && x < chunkAmountX && y < chunkAmountY && z < chunkAmountZ) return chunks[x, y, z];
        return null;
    }
    public Vector3 CalculateRealPosAt(int x, int y, int z) {
        return new Vector3(x * this.voxelSize.x, y * this.voxelSize.y, z * this.voxelSize.z);
    }

    public MC_Chunk GetChunkByWorldCoords(int x, int y, int z) {
        if (!IsPointWithinWorldBounds(x, y, z)) return null;
        return chunks[Mathf.FloorToInt(x / chunkSizeX), Mathf.FloorToInt(y / chunkSizeY), Mathf.FloorToInt(z / chunkSizeZ)];
    }

    public MC_Point GetWorldCoordByRealPos(Vector3 realPos) {
        int x = Mathf.FloorToInt((realPos.x + worldPosition.x) / voxelSize.x);
        int y = Mathf.FloorToInt((realPos.y + worldPosition.y) / voxelSize.y);
        int z = Mathf.FloorToInt((realPos.z + worldPosition.z) / voxelSize.z);
        MC_Point p = GetPointAtWorldCoord(x, y, z);
        if (p == null) return null;
        return p;
    }


    float CreateValueFrom(Vector3 realPos) {
        return (RandomNumberGenerator.PerlinNoise_SN_3D(realPos.x, realPos.y, realPos.z, 60, 60, 2.0f, 100f));
    }


    void InitPoints() {
        //Create canvas points
        points = new MC_Point[worldSizeX * worldSizeY * worldSizeZ];

        for (int x = 0; x < worldSizeX; x++) {
            for (int y = 0; y < worldSizeY; y++) {
                for (int z = 0; z < worldSizeZ; z++) {
                    int index = GetPointIndex(x, y, z);
                    points[index] = new MC_Point(this, index, x, y, z, 2);
                }
            }
        }

        Debug.Log("points " + points.Length);
    }

    void FillPoints(bool fillNoise) {
        for (int i = 0; i < points.Length; i++) {
            MC_Point cur = points[i];

            //Border
            if (!this.IsPointWithinWorldBounds(cur.x, cur.y, cur.z, 1)) {
                cur.isBorder = true;
                continue;
            }
            //Ground
            if (cur.y < 2) {
                cur.pointValue = 1f;
                continue;
            }

            //fill with perlin noise
            if (fillNoise) cur.pointValue = this.CreateValueFrom(cur.realPos);
        }
    }


    void NeighbouringPoints() {
        for (int i = 0; i < points.Length; i++) points[i].InitNeighbours();
    }


    void InitChunks() {
        //Create chunks

        chunks = new MC_Chunk[chunkAmountX, chunkAmountY, chunkAmountZ];

        for (int x = 0; x < chunkAmountX; x++) {
            for (int y = 0; y < chunkAmountY; y++) {
                for (int z = 0; z < chunkAmountZ; z++) {
                    MC_Chunk c = new MC_Chunk(this, x, y, z);
                    chunks[x, y, z] = c;
                }
            }
        }
    }

    void InitChunkObjs() {
        //Set up GameObjects to hold the rendered chunks.

        chunkObjs = new GameObject[chunkAmountX, chunkAmountY, chunkAmountZ];

        for (int x = 0; x < chunkAmountX; x++) {
            for (int y = 0; y < chunkAmountY; y++) {
                for (int z = 0; z < chunkAmountZ; z++) {
                    GameObject g = new GameObject();
                    g.name = "chunk_" + x + "" + y + "" + z;
                    g.transform.SetParent(GameObject.Find("Controller").transform);//temp

                    MeshRenderer mr = g.AddComponent<MeshRenderer>();
                    mr.receiveShadows = true;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    mr.material = Resources.Load("Materials/OtherMat") as Material;

                    MeshFilter mf = g.AddComponent<MeshFilter>();
                    mf.mesh = null;

                    GameObject col = new GameObject();
                    col.name = "collider_" + x + "" + y + "" + z;
                    col.transform.SetParent(g.transform);
                    MeshCollider collider = col.AddComponent<MeshCollider>();
                    collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;

                    chunkObjs[x, y, z] = g;
                }
            }
        }
    }

    public void RenderChunks() {
        //Render all chunks

        for (int x = 0; x < chunkAmountX; x++) {
            for (int y = 0; y < chunkAmountY; y++) {
                for (int z = 0; z < chunkAmountZ; z++) {
                    RenderCallBack(x, y, z);
                }
            }
        }
    }

    void RenderCallBack(int x, int y, int z) {
        //Render specific chunk

        JobHandle jh = MarchingCubesSystem.SetUp(chunks[x, y, z], this);
        jh.Complete();
        Mesh mesh = MarchingCubesSystem.CleanUp();
        PlaceMesh(mesh, chunkObjs[x, y, z]);
    }

    public void PlaceMesh(Mesh mesh, GameObject chunkObj) {
        //Add new Mesh to Chunk GameObject

        MeshRenderer mr = chunkObj.GetComponent<MeshRenderer>();
        MeshFilter mf = chunkObj.GetComponent<MeshFilter>();
        mf.mesh.Clear();
        mf.mesh = mesh;

        MeshCollider mc = chunkObj.transform.GetChild(0).GetComponent<MeshCollider>();

        if (mc.sharedMesh != null) mc.sharedMesh.Clear();
        mc.sharedMesh = mesh;
    }




    List<MC_Chunk> chunksToUpdate = new List<MC_Chunk>();
    List<MC_Point> cloudAsCanvasPoints = new List<MC_Point>();
    public void SetPointCloudInWorld(Vector3[] pointCloud, int terrain, bool subtract = false, float[] strengthArray = null, float strenght = 0.05f) {
        //Add a pointcloud to the canvas

        chunksToUpdate.Clear();
        cloudAsCanvasPoints.Clear();

        for (int i = 0; i < pointCloud.Length; i++) {
            MC_Point p = GetWorldCoordByRealPos(pointCloud[i]);
            if (p == null || cloudAsCanvasPoints.Contains(p)) continue;
            cloudAsCanvasPoints.Add(p);

            if (strengthArray == null) if (subtract) p.pointValue = Mathf.Clamp(p.pointValue - strenght, 0f, 1f); else p.pointValue = Mathf.Clamp(p.pointValue + strenght, 0f, 1f);
            else
            if (subtract) p.pointValue = Mathf.Clamp(p.pointValue - strengthArray[i], 0f, 1f); else p.pointValue = Mathf.Clamp(p.pointValue + strengthArray[i], 0f, 1f);

            p.terrain = terrain;

        }

        for (int i = 0; i < cloudAsCanvasPoints.Count; i++) {
            MC_Point p = cloudAsCanvasPoints[i];
            if (p == null) continue;
            List<MC_Chunk> touched = p.ChunksDependendOnThisPoint();
            for (int k = 0; k < touched.Count; k++) {
                if (!chunksToUpdate.Contains(touched[k])) chunksToUpdate.Add(touched[k]);
            }
        }

        for (int i = 0; i < chunksToUpdate.Count; i++) {
            MC_Chunk chunkToUpdate = chunksToUpdate[i];
            this.RenderCallBack(chunkToUpdate.chunkCoordX, chunkToUpdate.chunkCoordY, chunkToUpdate.chunkCoordZ);
        }
    }


    public void AddMeshToWorld(GameObject gameObjectWithMesh, int terrain) {
        //This function adds a Mesh to the canvas.

        Mesh mesh = gameObjectWithMesh.GetComponent<MeshFilter>().mesh;
        Debug.Log("added " + mesh.vertices.Length);
        List<Vector3> v = new List<Vector3>();
        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 p = (mesh.vertices[i] + gameObjectWithMesh.transform.position);
            if (p != null) v.Add(p);

        }
        SetPointCloudInWorld(v.ToArray(), terrain, false, null, 1f);
    }


}