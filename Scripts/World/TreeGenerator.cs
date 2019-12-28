using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTreeGenerator : MonoBehaviour
{
    static Texture2D heightmap;
    static int width, height;

    int[,,] blockData;
    int[,,] blockDataTransparent;

    public Transform tree;
    public GameObject treeChunk, leafChunk;
    private List<Vector3> vertices;
    private List<Vector2> uvs;
    private List<Vector3> normals;
    List<GameObject> chunks = new List<GameObject>();
    private int alphaSizeX, alphaSizeY;

    public bool teleport;
    private List<int> triangles;

        void resetVars()
    {
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();
        triangles = new List<int>();
    }

        Mesh getMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.normals = normals.ToArray();
        return mesh;
    }

    float GetNoise(int x, int y, float freq, float gain)
    {
        return Mathf.PerlinNoise(x * freq, y * freq) * gain;
    }

    float GetRidgedNoise(int x, int y, float freq = 0.05f, float gain = 1) // Rivers - 0.01, 1 && op - 0.95
    {
        return 2 * (0.5f - Mathf.Abs(0.5f - GetNoise(x, y, freq, gain)));
    }

    float GetRidgedNoise3D(int x, int y, int z)
    {
        float xy = GetRidgedNoise(x, y);
        float xz = GetRidgedNoise(x, z);
        
        float yx = GetRidgedNoise(y, x);
        float yz = GetRidgedNoise(y, z);

        float zx = GetRidgedNoise(z, x);
        float zy = GetRidgedNoise(z, y);

        return (xy + xz + yx + yz + zx + zy) / 6.0f;
    }

    float GetFractalNoise3D(int x, int y, int z)
    {
        float xy = GetFractalNoise(x, y);
        float xz = GetFractalNoise(x, z);
        
        float yx = GetFractalNoise(y, x);
        float yz = GetFractalNoise(y, z);

        float zx = GetFractalNoise(z, x);
        float zy = GetFractalNoise(z, y);

        return (xy + xz + yx + yz + zx + zy) / 6.0f;
    }

    float GetFractalNoise(int x, int y, float freq = 0.05f, float gain = 0.5f, int octaves = 16)
    {   
        float total = 0;
        float amp = gain;
        for(int o = 0; o < octaves; o++){
            total += Mathf.PerlinNoise(x * freq, y * freq) * amp;
            freq *= 2.0f;
            amp *= gain;
        }
        return total;
    }
    void GenerateRidgetTerrain3D()
    {
        for(int x = 0; x < alphaSizeX; x++)
        for(int y = 0; y < alphaSizeY; y++)
        for(int z = 0; z < alphaSizeX; z++)
            blockData[x, y, z] = GetFractalNoise3D(x, y, z) < 0.45f ? 1 : 0;
    }

    void Generate3D()
    {
        if(chunks.Count > 0)
            for(int i = 0; i < chunks.Count; i++) Destroy(chunks[i]);
        chunks = new List<GameObject>();

        int thickness = (int)(Mathf.PerlinNoise(transform.position.x * 0.08f, transform.position.z * 0.08f) * 10) + 1,
            length    = (int)(Mathf.PerlinNoise(transform.position.x * 0.05f, transform.position.z * 0.05f) * 15) + 1;
        
        alphaSizeX = 10 * length;
        alphaSizeY = 10 * length;
        int radius = length / 6 + thickness / 5;
        if(radius > 5) radius = 5;
        if(radius < 2) radius = 2;
        Debug.Log(radius);
        blockData = new int[alphaSizeX, alphaSizeY, alphaSizeX];
        blockDataTransparent = new int[alphaSizeX, alphaSizeY, alphaSizeX];

        //for(int x = 0; x < alphaSize; x++)
        //for(int y = 0; y < alphaSize * length; y++)
        //for(int z = 0; z < alphaSize; z++)
            //blockData[x,y,z] = false;
        
        int rotX = Mathf.RoundToInt(Mathf.PerlinNoise(transform.position.x * 0.2f, transform.position.z * 0.2f) * 6) - 3;
        int rotZ = Mathf.RoundToInt(Mathf.PerlinNoise(transform.position.x * 0.3f, transform.position.z * 0.3f) * 6) - 3;
        Debug.Log(rotX + ":" + rotZ);
        MakeBranch3D(Vector3.zero, Vector3.up * length + Vector3.left * rotX + Vector3.forward * rotZ, thickness / 2, length, radius);
        //GenerateRidgetTerrain3D();

        Mesh m;
        for(int cx = 0; cx < alphaSizeX / 16; cx++)
        for(int cz = 0; cz < alphaSizeX / 16; cz++)
        for(int cy = 0; cy < alphaSizeY / 16; cy++)
        {
            resetVars();
            generateMeshData(ref blockData, cx, cy, cz);

            if(vertices.Count > 0){
                m = getMesh();
                GameObject chunk = (GameObject)Instantiate(treeChunk, Vector3.zero, treeChunk.transform.rotation);
                chunk.name = cx + ":" + cy + ":" + cz;
                chunk.GetComponent<MeshFilter>().mesh = m;
                chunk.transform.parent = transform;
                chunk.transform.position = transform.position;
                chunks.Add(chunk);
            }
            

            resetVars();
            generateLeafMeshData(ref blockDataTransparent, cx, cy, cz);

            if(vertices.Count > 0){
                GameObject trChunk = (GameObject)Instantiate(leafChunk, Vector3.zero, leafChunk.transform.rotation);
                trChunk.name = "Trans:" + cx + ":" + cy + ":" + cz;
                m = getMesh();
                trChunk.GetComponent<MeshFilter>().mesh = m;
                trChunk.transform.parent = transform;
                trChunk.transform.position = transform.position;
                chunks.Add(trChunk);
            }
        }
    }

    void MakeBranch3D(Vector3 startAngle, Vector3 moveTowards, int thickness, int length, int radius, int iteration = 1)
    {
        if(thickness < 0) thickness = 0;
        if(length <= 0 || iteration > 6) return;

        tree.transform.position = startAngle;
        tree.LookAt(moveTowards);
        int x, y, z;
        Vector3 l;
        while(Vector3.Distance(tree.position, moveTowards) > 0.5f)
        {
            for(int rx = -thickness; rx <= thickness; rx++){
                for(int rz = -thickness; rz <= thickness; rz++){
                    if(rx * rx + rz * rz <= thickness * thickness){
                        l = tree.position;
                        l += tree.right * rx;
                        l += tree.up * rz;

                        x = Mathf.RoundToInt(l.x) + alphaSizeX / 2;
                        y = Mathf.RoundToInt(l.y);
                        z = Mathf.RoundToInt(l.z) + alphaSizeX / 2;
                        if(x < 0 || y < 0 || z < 0) continue;
                        blockData[x, y, z] = 1;

                        // Add leafs
                        
                        if(iteration == 6){
                            int nx, ny, nz;
                            for(int lx = -radius; lx <= radius; lx++){
                                nx = x + lx;
                                for(int lz = -radius; lz <= radius; lz++){
                                    nz = z + lz;
                                    for(int ly = -radius; ly <= radius; ly++){
                                        if(lx * lx + lz * lz + ly * ly > radius * radius) continue;
                                        ny = y + ly;
                                        if(nx < 0 || ny < 0 || nz < 0) continue;
                                        if(nx >= alphaSizeX || ny >= alphaSizeY || nz >= alphaSizeX) continue;
                                        if(blockData[nx, ny, nz] == 1) continue;
                                        //if(Perlin3D(nx, ny, nz) < 0.8f)
                                            blockDataTransparent[nx, ny, nz] = 2;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            tree.position += tree.forward;
        }
        int newLength = (int)(length * 0.8f);
        Vector3 memoPos = tree.position;
        Vector3 memoRot = tree.localEulerAngles;

        int bCount = Mathf.RoundToInt(Mathf.PerlinNoise(memoPos.x * 0.1f, memoPos.z * 0.1f) * 12);

        if(iteration <= 2 || bCount <= 3 || bCount > 9 )
            MakeBranch3D(tree.position, tree.position + tree.right * newLength / 2 + tree.up * newLength / 2 + tree.forward * newLength / 2, thickness - 1, newLength, radius, iteration+1);

        if(iteration <= 3 || bCount == 1 || bCount > 9 || bCount > 3 && bCount <= 6){
            tree.position = memoPos;
            tree.localEulerAngles = memoRot;
            MakeBranch3D(tree.position, tree.position - tree.right * newLength / 2 + tree.up * newLength / 2 + tree.forward * newLength / 2, thickness - 1, newLength, radius, iteration+1);
        }
        if(iteration <= 2 || bCount == 2 || bCount == 5 || bCount == 7 || bCount == 8|| bCount > 9){
            tree.position = memoPos;
            tree.localEulerAngles = memoRot;
            MakeBranch3D(tree.position, tree.position + tree.right * newLength / 2 - tree.up * newLength / 2 + tree.forward * newLength / 2, thickness - 1, newLength, radius, iteration+1);
        }

        if(iteration <= 3 ||bCount == 3 || bCount == 6 || bCount > 7){
            tree.position = memoPos;
            tree.localEulerAngles = memoRot;
            MakeBranch3D(tree.position, tree.position - tree.right * newLength / 2 - tree.up * newLength / 2 + tree.forward * newLength / 2, thickness - 1, newLength, radius, iteration+1);
        }
    }

float Perlin(float x, float y)
    {
        return 
            Mathf.PerlinNoise(x * 0.05f, y * 0.05f) 
            + 0.5f * Mathf.PerlinNoise(x * 0.1f, y * 0.1f)
            + 0.25f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f)
            ;
    }
    float Perlin3D(float x, float y, float z)
    {
        float XY = Perlin(x, y);
        float YZ = Perlin(y, z);
        float XZ = Perlin(x, z);

        float YX = Perlin(y, x);
        float ZY = Perlin(z, y);
        float ZX = Perlin(z, x);

        float XYZ = XY + YZ + XZ + YX + ZY + ZX;
        return XYZ / 6F;
    }
    void generateMeshData(ref int[,,] blockData, int chunkX, int chunkY, int chunkZ)
    {
        int startX = chunkX * 16;
        int startZ = chunkZ * 16;
        int startY = chunkY * 16;

        for(int x = startX; x < alphaSizeX && x < startX + 16; x++)
        for(int z = startZ; z < alphaSizeX && z < startZ + 16; z++)
        for(int y = startY; y < alphaSizeY && y < startY + 16; y++)
        {

            if(blockData[x, y, z] == 0) continue;

            Vector3 v = new Vector3(x - alphaSizeX / 2, y, z - alphaSizeX / 2);
            // Left
            if(x == 0 || blockData[x-1,y,z] == 0){
                makePlane(
                    v + Vector3.up,
                    v + Vector3.up + Vector3.forward,
                    v,
                    v + Vector3.forward,
                    Vector3.left,
                    false //blockData[x,y + 1,z] == 2
                );
            }
            // Right
            if(x == alphaSizeX - 1 || blockData[x+1,y,z] == 0){
                makePlane(
                    v + Vector3.right + Vector3.up + Vector3.forward,
                    v + Vector3.right + Vector3.up,
                    v + Vector3.right + Vector3.forward,
                    v + Vector3.right,
                    Vector3.right,
                    false //blockData[x,y + 1,z] == 2
                );
            }
            // Front
            if(z == 0 || blockData[x,y,z-1] == 0){
                makePlane(
                    v + Vector3.right + Vector3.up,
                    v + Vector3.up,
                    v + Vector3.right,
                    v,
                    Vector3.forward,
                    false //blockData[x,y + 1,z] == 2
                );
            }
            // Back
            if(z == alphaSizeX - 1  || blockData[x,y,z+1] == 0){
                makePlane(
                    v + Vector3.up + Vector3.forward,
                    v + Vector3.right + Vector3.up + Vector3.forward,
                    v + Vector3.forward,
                    v + Vector3.right + Vector3.forward,
                    Vector3.back,
                    false //blockData[x,y + 1,z] == 2
                );
            }
            // Top
            if(y == alphaSizeY - 1 || blockData[x,y + 1,z] == 0){
                makePlane(
                    v + Vector3.up,
                    v + Vector3.right + Vector3.up,
                    v + Vector3.up + Vector3.forward,
                    v + Vector3.right + Vector3.up + Vector3.forward,
                    Vector3.up,
                    true //blockData[x,y + 1,z] == 2
                );
            }
            // Bottom
            if(y == 0 || blockData[x,y - 1,z] == 0){
                makePlane(
                    v + Vector3.right,
                    v,
                    v + Vector3.right + Vector3.forward,
                    v + Vector3.forward,
                    Vector3.down,
                    true //blockData[x,y + 1,z] == 2
                );
            }
        }
    }
    
    void generateLeafMeshData(ref int[,,] blockData, int chunkX, int chunkY, int chunkZ)
    {
        int startX = chunkX * 16;
        int startZ = chunkZ * 16;
        int startY = chunkY * 16;

        for(int x = startX; x < alphaSizeX && x < startX + 16; x++)
        for(int z = startZ; z < alphaSizeX && z < startZ + 16; z++)
        for(int y = startY; y < alphaSizeY && y < startY + 16; y++)
        {

            if(blockData[x, y, z] == 0) continue;

            Vector3 v = new Vector3(x - alphaSizeX / 2, y, z - alphaSizeX / 2);
            // Left
            //if(x == 0 || blockData[x-1,y,z] == 0 ){
                makePlane(
                    v + Vector3.up + Vector3.right * 0.5f,
                    v + Vector3.up + Vector3.forward  + Vector3.right * 0.5f,
                    v  + Vector3.right * 0.5f,
                    v + Vector3.forward + Vector3.right * 0.5f,

                    Vector3.left,
                    false //blockData[x,y + 1,z] == 2
                );
            //}
            // Front
            //if(z == 0 || blockData[x,y,z-1] == 0){
                makePlane(
                    v + Vector3.right + Vector3.up + Vector3.forward * 0.5f,
                    v + Vector3.up + Vector3.forward * 0.5f,
                    v + Vector3.right + Vector3.forward * 0.5f,
                    v + Vector3.forward * 0.5f,

                    Vector3.forward,
                    false //blockData[x,y + 1,z] == 2
                );
            //}
            // Top
            //if(y == alphaSizeY - 1 || blockData[x,y + 1,z] == 0){
                makePlane(
                    v + Vector3.up * 0.5f,
                    v + Vector3.right + Vector3.up * 0.5f,
                    v + Vector3.up * 0.5f + Vector3.forward,
                    v + Vector3.right + Vector3.up * 0.5f + Vector3.forward,

                    Vector3.up,
                    true //blockData[x,y + 1,z] == 2
                );
            //}
        }
    }
    void makePlane(Vector3 tl, Vector3 tr, Vector3 bl, Vector3 br, Vector3 normalDir, bool top = false)
    {
        int vc = vertices.Count;
        
        // Vertices
        vertices.Add(tl);
        vertices.Add(tr);
        vertices.Add(bl);
        vertices.Add(br);
        
        // Triangles
        makeTriangles(vc, vc + 1, vc + 2, vc + 3);

        // Normals
        for(int n = 0; n < 4; n++) normals.Add(normalDir);
        
        // UV
        uvs.AddRange(getUVTexture(tl.y, top));
    }
    void makeTriangles(int tl, int tr, int bl, int br)
    {
        triangles.Add(tl);
        triangles.Add(bl);
        triangles.Add(br);

        triangles.Add(tl);
        triangles.Add(br);
        triangles.Add(tr);
    }
Vector2[] getUVTexture(float y, bool isTop)
    {
       // int height = Mathf.RoundToInt(y);
       // float top = (int)(10f / 75 * height) / 10f;
        if(isTop){
            return new Vector2[]{ 
                new Vector2(0.5f, 0), 
                new Vector2 (0.5f, 1), 
                new Vector2(1, 0), 
                new Vector2 (1,1), 
            };
        }
        return new Vector2[]{ 
                new Vector2(0, 0), 
                new Vector2 (0, 1), 
                new Vector2(0.5f, 0), 
                new Vector2 (0.5f,1), 
            };
        
        /* 
        float offset = isTop ? 0.6f : 0;
        return new Vector2[]{ 
            new Vector2(offset, top), 
            new Vector2 (offset, top + 0.1f), 
            new Vector2(offset + 0.3f, top), 
            new Vector2 (offset + 0.3f, top + 0.1f), 
        }; */
    }

    void Generate()
    {
            Material m = GetComponent<MeshRenderer>().material;
            width = 200;
            height = 200;
            MakeTexture();
            m.mainTexture = heightmap;
    }
    void OnGUI()
    {
        if(GUILayout.Button("Generate")){
            Generate3D();
            if(teleport)
                transform.parent.position = new Vector3(Random.Range(0, 9999), 0 ,Random.Range(0, 9999));
        }
    }

    static void MakeTexture()
    {
        heightmap = new Texture2D(width, height);
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++){
                heightmap.SetPixel(x, y, new Color(0, 1, 0, 1));
            }
        int thickness = Random.Range(2, 11), length = Random.RandomRange(10, 30);
        AddBranch(0, 0, height / 2 - 10, length, thickness);
        AddLeafs(thickness, length, 10, 0.6f, 0.1f);
        heightmap.filterMode = FilterMode.Point;
        heightmap.Apply();
    }

    static void AddLeafs(int thickness, int length, int distance = 3, float density = 0.45f, float frequency = 0.05f)
    {
         Color c, b = new Color(0, 0, 1);
         int rx, ry, relX, relY;
         int radius = (thickness + 4) / 2;
        for(int x = 0; x < width; x++){
            for(int y = 0; y < height; y++){
                if(y > height / 2 - 10 - radius && y < height / 2 - 10 + radius && x < length + 3 ) {
                    continue;
                }
                c = heightmap.GetPixel(x, y);
                if(c.r != 1) continue;
                for(rx = - distance; rx <= distance; rx++){
                    relX = rx + x;
                    if(relX < 0) continue;
                    else if(relX >= width) break;
                    for(ry = - distance; ry <= distance; ry++){
                        relY = ry + y;
                        if(relY < 0) continue;
                        else if(relY >= height) break;
                        c = heightmap.GetPixel(relX, relY);
                        if(c.r != 1 && c.b != 1 && Mathf.PerlinNoise(relX * frequency, relY * frequency) < density)
                            heightmap.SetPixel(relX, relY, b);
                    }
                }
            }
        }
    }
    // Update is called once per frame
    static void AddBranch(int angle, int pointX, int pointY, int length, int thickens)
    {
        if(thickens < 0) thickens = 0;
        if(length < 1|| pointX < 0 || pointY < 0 || pointX >= width || pointY >= height)
            return;

        float radians = Mathf.PI * angle / 180f;
        int radius = thickens / 2;
        Color c;

        int x, y;
        x = pointX;
        y = pointY;
        
        for(int l = 0; l < length; l++){
            x = Mathf.RoundToInt(pointX + l * Mathf.Cos(radians));
            y = Mathf.RoundToInt(pointY + l * Mathf.Sin(radians));
            if(x < 0 || y < 0 || x >= width || y >= height) break;

            // Draws square - thicknes
            for(int cx = x - radius; cx <= x + radius; cx++){
                if(cx < 0 || cx >= width) continue;
                for(int cy = y - radius; cy <= y + radius; cy++)
                {
                    if(cy < 0 || cy >= height) break;
                    c = heightmap.GetPixel(cx,cy);
                    if(c.r != 0) continue; 
                    heightmap.SetPixel(cx, cy, new Color(c.g, 0, 0, 1));
               }
            }
        }

        int newLength = (int)(length / 1.25f);
        int newThickness = thickens - 1;
        AddBranch( angle + /*55,*/Random.Range(15, 56),   x, y,  newLength,  newThickness );
        AddBranch( angle + /*-55,*/Random.Range(-55, -15),  x, y,  newLength,  newThickness );
    }
}
