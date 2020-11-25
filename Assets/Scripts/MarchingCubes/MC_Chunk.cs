using Unity.Mathematics;
using UnityEngine;


public class MC_Chunk {
    public int chunkCoordX, chunkCoordY, chunkCoordZ;
    public Vector3 chunkPosition;
    int fromX, toX, fromY, toY, fromZ, toZ;

    MC_Canvas canvas;
    MC_Point[] pointRef;

    public MC_Chunk(MC_Canvas canvas, int x, int y, int z) {
        this.canvas = canvas;
        this.chunkCoordX = x;
        this.chunkCoordY = y;
        this.chunkCoordZ = z;

        this.chunkPosition = new Vector3(chunkCoordX * canvas.actualChunkSize.x, chunkCoordY * canvas.actualChunkSize.y, chunkCoordZ * canvas.actualChunkSize.z) + canvas.worldPosition;

        fromX = chunkCoordX * canvas.chunkSizeX; toX = fromX + canvas.chunkSizeX;
        fromY = chunkCoordY * canvas.chunkSizeY; toY = fromY + canvas.chunkSizeY;
        fromZ = chunkCoordZ * canvas.chunkSizeZ; toZ = fromZ + canvas.chunkSizeZ;

        InitChunk();
    }

    bool inrange(int x, int y, int z) {
        return x >= 0 && y >= 0 && z >= 0 && x < (canvas.chunkSizeX + 2) && y < (canvas.chunkSizeY + 2) && z < (canvas.chunkSizeZ + 2);
    }
    int index(int x, int y, int z) {
        return (x + y * (canvas.chunkSizeX + 2) + z * (canvas.chunkSizeX + 2) * (canvas.chunkSizeY + 2));
    }


    public void InitChunk(int border = 1) {
        //collect points from canvas with some extra outisde layer/border (needed for the marching cubes)

        int length = (canvas.chunkSizeX + 2) * (canvas.chunkSizeY + 2) * (canvas.chunkSizeZ + 2);
        pointRef = new MC_Point[length];

        int nx = 0; int ny = 0; int nz = 0;
        int newindex;

        nx = 0;
        for (int x = (fromX - border); x < (toX + border); x++) {
            ny = 0;
            for (int y = (fromY - border); y < (toY + border); y++) {
                nz = 0;
                for (int z = (fromZ - border); z < (toZ + border); z++) {
                    MC_Point p = canvas.GetPointAtWorldCoord(x, y, z, true);
                    newindex = index(nx, ny, nz);
                    pointRef[newindex] = p;
                    nz++;
                }
                ny++;
            }
            nx++;
        }
    }

    public void grabChunk(out float[] pointValue, out float3[] pointRealPos) {
        //prepare arrays

        pointRealPos = new float3[pointRef.Length];
        pointValue = new float[pointRef.Length];

        for (int i = 0; i < pointRef.Length; i++) {
            pointValue[i] = pointRef[i].pointValue;
            pointRealPos[i] = pointRef[i].realPos;
        }
    }


}


