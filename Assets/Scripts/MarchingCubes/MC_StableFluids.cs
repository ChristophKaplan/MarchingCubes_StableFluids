using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

//
// this implements the "Stable Fluids" algortithm as a Unity Job with Burst.
// reference: https://mikeash.com/pyblog/fluid-simulation-for-dummies.html
//
// So far i played with a "scaler" to reduce the point count and somehow make the costly algortithm faster.
// I added a "projectArray()" function to project the lesser points to the MC_Canvas array length.
// If anyone knows how to improve here let me know.
//


public static class MC_StableFluids {

    public static JobHandle current;
    public static FluidJOB currentJob;

    public static JobHandle SetUp(MC_FluidCube fluidCube,MC_Canvas canvas,JobHandle dep = default) {


        float visc = fluidCube.visc;
        float diff = fluidCube.diff;
        float dt = fluidCube.dt;
        NativeArray<float> Vx = new NativeArray<float>(fluidCube.Vx, Allocator.TempJob);
        NativeArray<float> Vy = new NativeArray<float>(fluidCube.Vy, Allocator.TempJob);
        NativeArray<float> Vz = new NativeArray<float>(fluidCube.Vz, Allocator.TempJob);
        NativeArray<float> Vx0 = new NativeArray<float>(fluidCube.Vx0, Allocator.TempJob);
        NativeArray<float> Vy0 = new NativeArray<float>(fluidCube.Vy0, Allocator.TempJob);
        NativeArray<float> Vz0 = new NativeArray<float>(fluidCube.Vz0, Allocator.TempJob);
        NativeArray<float> s = new NativeArray<float>(fluidCube.s, Allocator.TempJob);
        NativeArray<float> density = new NativeArray<float>(fluidCube.density, Allocator.TempJob);

        int sizeX = fluidCube.spaceX / fluidCube.scaler;
        int sizeY = fluidCube.spaceY / fluidCube.scaler;
        int sizeZ = fluidCube.spaceZ / fluidCube.scaler;


        float3[] pp = new float3[canvas.points.Length];
        float[] pv = new float[canvas.points.Length];
        for (int i = 0; i < canvas.points.Length; i++) {
            pp[i] = canvas.points[i].realPos;
            pv[i] = canvas.points[i].pointValue;
        }
        NativeArray<float3> pntPos = new NativeArray<float3>(pp, Allocator.TempJob);
        NativeArray<float> pntValue = new NativeArray<float>(pv, Allocator.TempJob);

        currentJob = new FluidJOB {
            scaler = fluidCube.scaler,
            smallLength = Vx.Length,
            bigLength = canvas.points.Length,
            worldPosition = canvas.worldPosition,
            voxelSize = canvas.voxelSize,
            spaceX = sizeX,
            spaceY = sizeY,
            spaceZ = sizeZ,
            iter = fluidCube.iterations,
            visc = fluidCube.visc,
            diff = fluidCube.diff,
            dt = fluidCube.dt,
            Vx = Vx,
            Vy = Vy,
            Vz = Vz,
            Vx0 = Vx0,
            Vy0 = Vy0,
            Vz0 = Vz0,
            s = s,
            density = density,
            pointPos = pntPos,
            pointValue = pntValue
        };

        current = currentJob.Schedule(dep);
        return current;

    }
    public static void CleanUp(MC_FluidCube fluidCube, MC_Canvas canvas) {
        //current.Complete();

        //READ
        fluidCube.dt = currentJob.dt;
        currentJob.Vx.CopyTo(fluidCube.Vx);
        currentJob.Vy.CopyTo(fluidCube.Vy);
        currentJob.Vz.CopyTo(fluidCube.Vz);
        currentJob.Vx0.CopyTo(fluidCube.Vx0);
        currentJob.Vy0.CopyTo(fluidCube.Vy0);
        currentJob.Vz0.CopyTo(fluidCube.Vz0);
        currentJob.s.CopyTo(fluidCube.s);
        currentJob.density.CopyTo(fluidCube.density);

        float3[] pp = new float3[currentJob.pointPos.Length]; currentJob.pointPos.CopyTo(pp);
        float[] pv = new float[currentJob.pointValue.Length]; currentJob.pointValue.CopyTo(pv);

        for (int i = 0; i < canvas.points.Length; i++) {
            MC_Point p = canvas.points[i];
            p.realPos = pp[i];
            p.pointValue = pv[i];
            if (p.isBorder) p.pointValue = 0f;
        }

        //DISPOSE
        currentJob.Vx.Dispose();
        currentJob.Vy.Dispose();
        currentJob.Vz.Dispose();
        currentJob.Vx0.Dispose();
        currentJob.Vy0.Dispose();
        currentJob.Vz0.Dispose();

        currentJob.s.Dispose();
        currentJob.density.Dispose();

        currentJob.pointValue.Dispose();
        currentJob.pointPos.Dispose();
    }



}


[BurstCompile]
public struct FluidJOB : IJob {
    [ReadOnly] public int scaler;
    [ReadOnly] public int smallLength;
    [ReadOnly] public int bigLength;
    [ReadOnly] public float3 worldPosition;
    [ReadOnly] public float3 voxelSize;
    public NativeArray<float3> pointPos;

    [WriteOnly] public NativeArray<float> pointValue;
    [ReadOnly] public int spaceX;
    [ReadOnly] public int spaceY;
    [ReadOnly] public int spaceZ;
    [ReadOnly] public int iter;
    [ReadOnly] public float visc;
    [ReadOnly] public float diff;
    [ReadOnly] public float dt;
    public NativeArray<float> Vx;
    public NativeArray<float> Vy;
    public NativeArray<float> Vz;
    public NativeArray<float> Vx0;
    public NativeArray<float> Vy0;
    public NativeArray<float> Vz0;
    public NativeArray<float> s;
    public NativeArray<float> density;

    public void Execute() {
        //Fluid Step

        diffuse(1, Vx0, Vx, visc);
        diffuse(2, Vy0, Vy, visc);
        diffuse(3, Vz0, Vz, visc);

        project(Vx0, Vy0, Vz0, Vx, Vy);

        advect(1, Vx, Vx0);
        advect(2, Vy, Vy0);
        advect(3, Vz, Vz0);

        project(Vx, Vy, Vz, Vx0, Vy0);

        diffuse(0, s, density, diff);
        advect(0, density, s);

        projectArray();
    }

    int index(int x, int y, int z) {
        return (x + y * spaceX + z * spaceX * spaceY);
    }

    void set_bounds(int b, NativeArray<float> at) {
        for (int y = 1; y < spaceY - 1; y++) {
            for (int x = 1; x < spaceX - 1; x++) {
                at[index(x, y, 0)] = b == 3 ? -at[index(x, y, 1)] : at[index(x, y, 1)];
                at[index(x, y, spaceZ - 1)] = b == 3 ? -at[index(x, y, spaceZ - 2)] : at[index(x, y, spaceZ - 2)];
            }
        }
        for (int z = 1; z < spaceZ - 1; z++) {
            for (int x = 1; x < spaceX - 1; x++) {
                at[index(x, 0, z)] = b == 2 ? -at[index(x, 1, z)] : at[index(x, 1, z)];
                at[index(x, spaceY - 1, z)] = b == 2 ? -at[index(x, spaceY - 2, z)] : at[index(x, spaceY - 2, z)];
            }
        }
        for (int z = 1; z < spaceZ - 1; z++) {
            for (int y = 1; y < spaceY - 1; y++) {
                at[index(0, y, z)] = b == 1 ? -at[index(1, y, z)] : at[index(1, y, z)];
                at[index(spaceX - 1, y, z)] = b == 1 ? -at[index(spaceX - 2, y, z)] : at[index(spaceX - 2, y, z)];
            }
        }

        at[index(0, 0, 0)] = 0.33f * (at[index(1, 0, 0)] + at[index(0, 1, 0)] + at[index(0, 0, 1)]);
        at[index(0, spaceY - 1, 0)] = 0.33f * (at[index(1, spaceY - 1, 0)] + at[index(0, spaceY - 2, 0)] + at[index(0, spaceY - 1, 1)]);
        at[index(0, 0, spaceZ - 1)] = 0.33f * (at[index(1, 0, spaceZ - 1)] + at[index(0, 1, spaceZ - 1)] + at[index(0, 0, spaceZ - 2)]);//add -2
        at[index(0, spaceY - 1, spaceZ - 1)] = 0.33f * (at[index(1, spaceY - 1, spaceZ - 1)] + at[index(0, spaceY - 2, spaceZ - 1)] + at[index(0, spaceY - 1, spaceZ - 2)]);
        at[index(spaceX - 1, 0, 0)] = 0.33f * (at[index(spaceX - 2, 0, 0)] + at[index(spaceX - 1, 1, 0)] + at[index(spaceX - 1, 0, 1)]);
        at[index(spaceX - 1, spaceY - 1, 0)] = 0.33f * (at[index(spaceX - 2, spaceY - 1, 0)] + at[index(spaceX - 1, spaceY - 2, 0)] + at[index(spaceX - 1, spaceY - 1, 1)]);
        at[index(spaceX - 1, 0, spaceZ - 1)] = 0.33f * (at[index(spaceX - 2, 0, spaceZ - 1)] + at[index(spaceX - 1, 1, spaceZ - 1)] + at[index(spaceX - 1, 0, spaceZ - 2)]);
        at[index(spaceX - 1, spaceY - 1, spaceZ - 1)] = 0.33f * (at[index(spaceX - 2, spaceY - 1, spaceZ - 1)] + at[index(spaceX - 1, spaceY - 2, spaceZ - 1)] + at[index(spaceX - 1, spaceY - 1, spaceZ - 2)]);
    }

    void lin_solve(int b, NativeArray<float> at, NativeArray<float> at0, float a, float c) {
        float cRecip = 1.0f / c;

        for (int k = 0; k < iter; k++) {
            for (int z = 1; z < spaceZ - 1; z++) {
                for (int y = 1; y < spaceY - 1; y++) {
                    for (int x = 1; x < spaceX - 1; x++) {
                        at[index(x, y, z)] = (at0[index(x, y, z)] +
                            a * (at[index(x + 1, y, z)] + at[index(x - 1, y, z)] + at[index(x, y + 1, z)] + at[index(x, y - 1, z)] + at[index(x, y, z + 1)] + at[index(x, y, z - 1)]) // all ns
                            ) * cRecip;
                    }
                }
            }
            set_bounds(b, at);
        }
    }


    void diffuse(int b, NativeArray<float> at, NativeArray<float> at0, float diff) {
        float a = dt * diff * (spaceX - 2) * (spaceX - 2);  // X?
        lin_solve(b, at, at0, a, 1 + 6 * a);
    }

    void project(NativeArray<float> velocX, NativeArray<float> velocY, NativeArray<float> velocZ, NativeArray<float> p, NativeArray<float> div) {
        for (int z = 1; z < spaceZ - 1; z++) {
            for (int y = 1; y < spaceY - 1; y++) {
                for (int x = 1; x < spaceX - 1; x++) {
                    div[index(x, y, z)] = -0.5f * (
                             velocX[index(x + 1, y, z)]
                            - velocX[index(x - 1, y, z)]
                            + velocY[index(x, y + 1, z)]
                            - velocY[index(x, y - 1, z)]
                            + velocZ[index(x, y, z + 1)]
                            - velocZ[index(x, y, z - 1)]
                        ) / spaceX;                         // X?
                    p[index(x, y, z)] = 0;
                }
            }
        }
        set_bounds(0, div);
        set_bounds(0, p);
        lin_solve(0, p, div, 1, 6);

        for (int z = 1; z < spaceZ - 1; z++) {
            for (int y = 1; y < spaceY - 1; y++) {
                for (int x = 1; x < spaceX - 1; x++) {
                    velocX[index(x, y, z)] -= 0.5f * (p[index(x + 1, y, z)]
                                                    - p[index(x - 1, y, z)]) * spaceX;
                    velocY[index(x, y, z)] -= 0.5f * (p[index(x, y + 1, z)]
                                                    - p[index(x, y - 1, z)]) * spaceY;
                    velocZ[index(x, y, z)] -= 0.5f * (p[index(x, y, z + 1)]
                                                    - p[index(x, y, z - 1)]) * spaceZ;
                }
            }
        }

        set_bounds(1, velocX);
        set_bounds(2, velocY);
        set_bounds(3, velocZ);
    }

    void advect(int b, NativeArray<float> d, NativeArray<float> d0) {


        float i0, i1, j0, j1, k0, k1;

        float dtx = dt * (spaceX - 2);
        float dty = dt * (spaceY - 2);
        float dtz = dt * (spaceZ - 2);

        float s0, s1, t0, t1, u0, u1;
        float tmp1, tmp2, tmp3, x, y, z;

        float Nfloat = spaceX;
        float ifloat, jfloat, kfloat;
        int i, j, k;

        for (k = 1, kfloat = 1; k < spaceZ - 1; k++, kfloat++) {
            for (j = 1, jfloat = 1; j < spaceY - 1; j++, jfloat++) {
                for (i = 1, ifloat = 1; i < spaceX - 1; i++, ifloat++) {
                    tmp1 = dtx * Vx0[index(i, j, k)];
                    tmp2 = dty * Vy0[index(i, j, k)];
                    tmp3 = dtz * Vz0[index(i, j, k)];
                    x = ifloat - tmp1;
                    y = jfloat - tmp2;
                    z = kfloat - tmp3;

                    if (x < 0.5f) x = 0.5f;
                    if (x > Nfloat + 0.5f) x = Nfloat + 0.5f;
                    i0 = (int)(x);
                    i1 = i0 + 1.0f;
                    if (y < 0.5f) y = 0.5f;
                    if (y > Nfloat + 0.5f) y = Nfloat + 0.5f;
                    j0 = (int)(y);
                    j1 = j0 + 1.0f;
                    if (z < 0.5f) z = 0.5f;
                    if (z > Nfloat + 0.5f) z = Nfloat + 0.5f;
                    k0 = (int)(z);
                    k1 = k0 + 1.0f;

                    s1 = x - i0;
                    s0 = 1.0f - s1;
                    t1 = y - j0;
                    t0 = 1.0f - t1;
                    u1 = z - k0;
                    u0 = 1.0f - u1;

                    int i0i = (int)(i0);
                    int i1i = (int)(i1);
                    int j0i = (int)(j0);
                    int j1i = (int)(j1);
                    int k0i = (int)(k0);
                    int k1i = (int)(k1);

                    d[index(i, j, k)] =

                        s0 * (t0 * (u0 * d0[index(i0i, j0i, k0i)]
                                    + u1 * d0[index(i0i, j0i, k1i)])
                            + (t1 * (u0 * d0[index(i0i, j1i, k0i)]
                                    + u1 * d0[index(i0i, j1i, k1i)])))
                       + s1 * (t0 * (u0 * d0[index(i1i, j0i, k0i)]
                                    + u1 * d0[index(i1i, j0i, k1i)])
                            + (t1 * (u0 * d0[index(i1i, j1i, k0i)]
                                    + u1 * d0[index(i1i, j1i, k1i)])));
                }
            }
        }
        set_bounds(b, d);
    }



    int indexScaled(int x, int y, int z) {
        return (x + y * (spaceX * scaler) + z * (spaceX * scaler) * (spaceY * scaler));
    }
    bool inrangeScaled(int ind) {
        return ind >= 0 && ind < bigLength;
    }
    bool inrange(int ind) {
        return ind >= 0 && ind < smallLength;
    }
    void projectArray() {

        int lx = 0, ly = 0, lz = 0;
        float value = 0;
        float3 velo = 0;

        for (int x = 0; x < spaceX * scaler; x++) {
            for (int y = 0; y < spaceY * scaler; y++) {
                for (int z = 0; z < spaceZ * scaler; z++) {

                    //get the coordinates of the smaller array
                    if ((x % scaler == 0)) lx = x / scaler;
                    if ((y % scaler == 0)) ly = y / scaler;
                    if ((z % scaler == 0)) lz = z / scaler;

                    //get the index of the smaller array
                    int ind = index(lx, ly, lz);
                    if (!inrange(ind)) continue;

                    //get the velocity && density
                    velo = new float3(Vx[ind], Vy[ind], Vz[ind]);
                    value = density[ind];

                    //get index of the bigger array
                    int scaledIndex1 = indexScaled(x, y, z);
                    if (!inrangeScaled(scaledIndex1)) continue;

                    //get the position that the fluid has changed to
                    float3 thePosItsHappening = pointPos[scaledIndex1] + velo;

                    int changeX = Mathf.FloorToInt((thePosItsHappening.x + worldPosition.x) / voxelSize.x);
                    int changeY = Mathf.FloorToInt((thePosItsHappening.y + worldPosition.y) / voxelSize.y);
                    int changeZ = Mathf.FloorToInt((thePosItsHappening.z + worldPosition.z) / voxelSize.z);

                    //get index of the new position
                    int scaledIndex2 = indexScaled(changeX, changeY, changeZ);
                    if (!inrangeScaled(scaledIndex2)) continue;

                    //change the value/density
                    pointValue[scaledIndex2] = value;

                }
            }
        }


    }

}



