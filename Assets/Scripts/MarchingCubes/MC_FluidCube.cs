using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MC_FluidCube {

    public int spaceX;
    public int spaceY;
    public int spaceZ;
    public int iterations = 1; //costly
    public int scaler;

    public float dt;
    public float diff;
    public float visc;

    public float[] s;
    public float[] density;

    public float[] Vx;
    public float[] Vy;
    public float[] Vz;

    public float[] Vx0;
    public float[] Vy0;
    public float[] Vz0;

    public MC_FluidCube(int sizeX, int sizeY, int sizeZ, float diffusion, float viscosity, float _dt, int _scaler) {
        this.spaceX = sizeX;
        this.spaceY = sizeY;
        this.spaceZ = sizeZ;

        dt = _dt;
        diff = diffusion;
        visc = viscosity;
        scaler = _scaler;
        int length = ((spaceX / scaler) * (spaceY / scaler) * (spaceZ / scaler));
        s = new float[length];
        density = new float[length];

        Vx = new float[length];
        Vy = new float[length];
        Vz = new float[length];

        Vx0 = new float[length];
        Vy0 = new float[length];
        Vz0 = new float[length];

        Debug.Log("internal fluid space:" + spaceX + " " + spaceY + " " + spaceZ);
    }

    void FluidCubeAddDensity(int x, int y, int z, float amount) {
        density[index(x, y, z)] += amount;
    }
    void FluidCubeAddVelocity(int x, int y, int z, float amountX, float amountY, float amountZ) {
        int ind = index(x, y, z);

        Vx[ind] += amountX;
        Vy[ind] += amountY;
        Vz[ind] += amountZ;
    }
    int index(int x, int y, int z) {
        return ((x) + (y) * (spaceX / scaler) + (z) * (spaceX / scaler) * (spaceY / scaler));
    }

    bool InRange(int x, int y, int z) {
        return x >= 0 && y >= 0 && z >= 0 && x < spaceX / scaler && y < spaceY / scaler && z < spaceZ / scaler;
    }

    public void AddFluid(Vector3 where) {
        FluidCubeAddDensity(Mathf.FloorToInt(where.x / scaler), Mathf.FloorToInt(where.y / scaler), Mathf.FloorToInt(where.z / scaler), 200f);
    }

    public void AddVelo(Vector3 where) {
        float scale = 1000;
        FluidCubeAddVelocity(Mathf.FloorToInt(where.x / scaler), Mathf.FloorToInt(where.y / scaler), Mathf.FloorToInt(where.z / scaler), Random.value * scale, Random.value * scale, Random.value * scale);
    }
};
