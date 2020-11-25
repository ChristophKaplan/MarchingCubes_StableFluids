using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;


public class MC_Point {
    public int terrain;
    public int x, y, z;
    int index;
    public float3 realPos;
    public MC_Point[] neighbours;
    public int nsAmount;

    public float pointValue;
    public bool isBorder;
    MC_Canvas canvas;

    public MC_Point(MC_Canvas canvas, int index, int x, int y, int z, int terrain, bool border = false) {
        this.canvas = canvas;
        this.index = index;
        this.x = x; this.y = y; this.z = z;
        this.realPos = canvas.CalculateRealPosAt(x, y, z);
        this.isBorder = border;
        this.pointValue = 0f;
        this.terrain = terrain;
        this.neighbours = new MC_Point[26];
    }

    public override bool Equals(object obj) {
        MC_Point other = (MC_Point)obj;
        return (this.index == other.index);
    }
    public override int GetHashCode() {
        return index.GetHashCode();
    }


    List<MC_Chunk> touchedChunks = new List<MC_Chunk>();
    public List<MC_Chunk> ChunksDependendOnThisPoint() {
        //grab chunks that have this point

        touchedChunks.Clear();
        for (int i = 0; i < nsAmount; i++) {
            if (neighbours[i] == null) continue;
            MC_Chunk c = canvas.GetChunkByWorldCoords(neighbours[i].x, neighbours[i].y, neighbours[i].z);
            if (c != null && !touchedChunks.Contains(c)) touchedChunks.Add(c);
        }
        return touchedChunks;
    }

    public void InitNeighbours(int size = 1) {

        nsAmount = 0;

        for (int cz = -size + z; cz <= (size + z); cz++) {
            for (int cy = -size + y; cy <= (size + y); cy++) {
                for (int cx = -size + x; cx <= (size + x); cx++) {
                    if (cx == x && cy == y && cz == z) continue;
                    MC_Point cur = canvas.GetPointAtWorldCoord(cx, cy, cz);
                    if (cur != null) {
                        neighbours[nsAmount] = cur;
                        nsAmount++;
                    }
                }
            }
        }
    }

    public string Print() {
        return x + "/" + y + "/" + z;
    }

}
