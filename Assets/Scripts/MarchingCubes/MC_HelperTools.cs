using System.Collections.Generic;
using UnityEngine;


public static class HelperTools {
    static List<Vector3> sphere = new List<Vector3>();
    public static Vector3[] GetRandomSphere(Vector3 pos, int max, float scale = 1f) {
        sphere.Clear();
        for (int i = 0; i < max; i++) {
            Vector3 v = pos + (Random.insideUnitSphere * scale);
            sphere.Add(v);
        }
        return sphere.ToArray();
    }

    static GameObject sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    static List<Vector3> sps = new List<Vector3>();
    static Vector3[] sphereVerts;
    public static void InitGetSphere() {
        Mesh mesh = sphereGO.GetComponent<MeshFilter>().mesh;
        sphereVerts = new Vector3[mesh.vertices.Length];
        for (int i = 0; i < mesh.vertices.Length; i++) sphereVerts[i] = mesh.vertices[i];
        GameObject.Destroy(sphereGO);
    }
    public static Vector3[] GetSphere(Vector3 pos, float scale = 1f) {
        sps.Clear();

        for (int i = 0; i < sphereVerts.Length; i++) {
            sps.Add((sphereVerts[i] * scale) + pos);
        }
        return sps.ToArray();
    }
}

public class RandomNumberGenerator {

    public static string Seed;
    public static int HashedSeed;
    public static float randomOffsetBasedOnSeed;
    public static UnityEngine.Random.State myOriginalState;

    static System.Random realRandom = new System.Random();

    //RANDOM GENS

    public static bool WeHaveASeed = false;

    public RandomNumberGenerator(string seed) {
        SetSeed(seed);
    }

    public static void SetSeed(string seed) {
        //Debug.Log("SetSeed:" + seed);
        Seed = seed;
        HashedSeed = seed.GetHashCode();

        UnityEngine.Random.InitState(HashedSeed);
        myOriginalState = UnityEngine.Random.state;
        randomOffsetBasedOnSeed = UnityEngine.Random.value * 100000f;

        WeHaveASeed = true;

        //Debug.Log("SetSeed: Seed:" + Seed + " Hash:" + HashedSeed +" offset:"+ randomOffsetBasedOnSeed);
        SetBack();
    }

    public static void SetBack() {
        if (!WeHaveASeed) {
            return;
        }
        UnityEngine.Random.state = myOriginalState;
    }


    public static int myRandomRange(Vector3Int coord, int start, int stop, int state = 1) {
        if (!WeHaveASeed) {
            return 0;
        }
        UnityEngine.Random.InitState(HashedSeed + coord.GetHashCode() + state.GetHashCode());
        int ra = UnityEngine.Random.Range(start, stop);
        SetBack();

        return ra;

    }
    public static float myRandomValue(int x, int y, int z, int state = 1) {
        if (!WeHaveASeed) {
            return 0f;
        }
        UnityEngine.Random.InitState(HashedSeed + (x + "" + y + "" + z).GetHashCode() + state.GetHashCode());
        float ra = UnityEngine.Random.value;
        SetBack();
        return ra;

    }
    public static float myRealRandomValue() {
        return realRandom.Next(1, 10) / 10f;
    }
    public static int myRealRandomRange(int a, int b) {
        return realRandom.Next(a, b);
    }


    public static float PerlinNoise_Regular(float x, float y, int width, int height, float scale, float extraOffset = 0f) {

        if (!WeHaveASeed) return 0f;

        float myX = x / width * scale + (randomOffsetBasedOnSeed + extraOffset);
        float myY = y / height * scale + (randomOffsetBasedOnSeed + extraOffset);

        return Mathf.PerlinNoise(myX, myY);
    }


    public static float PerlinNoise_SuperNoise(float x, float y, int width, int height, float scale, float extraOffset) {
        float noise1 = PerlinNoise_Regular(x, y, width, height, scale, extraOffset);
        float noise2 = PerlinNoise_Regular(x, y, width, height, scale * noise1, extraOffset * 2);
        float mix = (noise1 + noise2) / 2;

        float noise3 = PerlinNoise_Regular(x, y, width, height, scale * mix, extraOffset * 3);
        mix = (mix + noise3) / 2;
        return mix;
    }
    public static float PerlinNoise_SN_3D(float x, float y, float z, int width, int height, float scale, float extraOffset) {
        float noise1 = PerlinNoise_Regular(x, y, width, height, scale, extraOffset);
        float noise2 = PerlinNoise_Regular(x, z, width, height, scale, extraOffset);
        return (noise1 + noise2) / 2;
    }
}//

