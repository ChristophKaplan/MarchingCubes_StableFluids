using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


//
// Controller to setup/initialize the canvas und simulation.
//

public enum SimMode {standart,fluid}

public class Controller : MonoBehaviour {
    static System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    public Vector3Int chunkAmount = new Vector3Int(3, 3, 3);
    public Vector3Int chunkSize = new Vector3Int(10, 10, 10);
    public Vector3 voxelSize = new Vector3(1f, 1f, 1f);
    public Vector3 positionCenter = new Vector3(0f, 0f, 0f);
    GameObject worldSizeCube;

    public static SimMode simMode = SimMode.fluid;  //switch between interaction modes
    static bool running = false; // if job is running

    public static MC_Canvas myCanvas;
    public static MC_FluidCube myFluidCube;

    void Awake() {
        
        HelperTools.InitGetSphere();

        //Initialize Canvas
        myCanvas = new MC_Canvas("marchingcubes123", positionCenter, chunkAmount, chunkSize, voxelSize);

        stopwatch.Reset();
        stopwatch.Start();

        StartCoroutine(myCanvas.LoadWorld(true,true));

        //messure the runtime
        stopwatch.Stop();
        Debug.Log("##### init completed in " + stopwatch.ElapsedMilliseconds / 1000f);


        //Initialize the fluid sim 
        myFluidCube = new MC_FluidCube (myCanvas.worldSizeX, myCanvas.worldSizeY, myCanvas.worldSizeZ, 0.01f, 0.9f, 0.5f,1);

        //Cube to show oure Cavas size.
        CreateCanvasSizeCube();

    }

    private void Start() {

    }

    void Update() {
        if (myCanvas.loading) return;
        if (simMode != SimMode.fluid) return;

        if (!running) {
            //Until i have a better Idea...
            //Run Stable Fluids every x Frame.
            if (Time.frameCount % 2 == 0) Fluidizer();
            //Run Marching Cubes every x+ frame.
            if (Time.frameCount % 3 == 0) Meshizer();
        }
    }

    //Run StableFluids
    public static void Fluidizer() {
        running = true;

        JobHandle jh1 = MC_StableFluids.SetUp(myFluidCube,myCanvas);
        jh1.Complete();
        MC_StableFluids.CleanUp(myFluidCube,myCanvas);

        running = false;
    }

    //Run MarchingCubes
    public void Meshizer() {
        running = true;

        myCanvas.RenderChunks();

        running = false;
    }




    //Create a cube to visualize the simulation area.
    public void CreateCanvasSizeCube() {
        worldSizeCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(worldSizeCube.GetComponent<BoxCollider>());
        worldSizeCube.transform.position = myCanvas.worldPosition + (myCanvas.actualWorldSize / 2f);
        worldSizeCube.transform.localScale = myCanvas.actualWorldSize;
        MeshRenderer mr = worldSizeCube.GetComponent<MeshRenderer>();
        mr.material = Resources.Load("Materials/worldSizeCube") as Material;
        mr.receiveShadows = false;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }


    //Quit Button
    public void Button1() {
        //Debug.Log("button");
        Application.Quit();
    }



    //Visualizer & Debugger
    void OnDrawGizmosSelected() {
        
        bool gizmoON = false;
        if (!gizmoON) {
            return;
        }

        Vector3 wOffset = myCanvas.actualWorldSize / 2f;
        Vector3 cOffset = myCanvas.actualChunkSize / 2f;
        float3 vOffset = myCanvas.voxelSize / 2f;


        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(myCanvas.worldPosition + wOffset, new Vector3(myCanvas.chunkAmountX * myCanvas.chunkSizeX * myCanvas.voxelSize.x,
                                                        myCanvas.chunkAmountY * myCanvas.chunkSizeY * myCanvas.voxelSize.y,
                                                        myCanvas.chunkAmountZ * myCanvas.chunkSizeZ * myCanvas.voxelSize.z));

        Gizmos.color = Color.yellow;
        for (int x = 0; x < myCanvas.chunkAmountX; x++) {
            for (int y = 0; y < myCanvas.chunkAmountY; y++) {
                for (int z = 0; z < myCanvas.chunkAmountZ; z++) {
                    //chunk
                    MC_Chunk c = myCanvas.GetChunkAt(x, y, z);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(c.chunkPosition + cOffset, new Vector3(myCanvas.chunkSizeX * myCanvas.voxelSize.x, myCanvas.chunkSizeY * myCanvas.voxelSize.y, myCanvas.chunkSizeZ * myCanvas.voxelSize.z));

                }
            }
        }

        for (int i = 0; i < myCanvas.points.Length; i++) {
            if (myCanvas.points[i] == null) { Debug.Log("should not be?"); continue; }
            Gizmos.color = Color.green;
            if (myCanvas.points[i].pointValue != 0f) { Gizmos.DrawWireCube(myCanvas.points[i].realPos + vOffset, myCanvas.voxelSize); }

            Gizmos.color = Color.red;
            if (myCanvas.points[i].isBorder ) { Gizmos.DrawWireCube(myCanvas.points[i].realPos + vOffset, myCanvas.voxelSize); }

        }
    }




}






