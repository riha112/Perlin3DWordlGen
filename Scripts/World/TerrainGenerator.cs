using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Range(1, 256)]
    public int mapWidth, mapHeight, mapLength;

    [Header("Data")]
    public int seed;
    [Range(1, 64)]
    public int chunkSize = 16;
    [Range(0, 1)]
    public float density = 0.45f;
    public int groundStart = 15;
    private List<Vector3> vertices;
    private List<Vector2> uvs;
    private List<Vector3> normals;
    private List<int> triangles;
    private bool[,,] mapData;

    public GameObject map;
    [Header("Tree")]
    public GameObject tree;
    public Texture2D blueNoise;
    public int treeRadius;
    void Start()
    {
        populateMap();

        for(int cx = 0; cx < mapWidth / chunkSize; cx++)
        for(int cz = 0; cz < mapLength / chunkSize; cz++)
        for(int cy = 0; cy < mapHeight / chunkSize; cy++)
        {
            resetVars();
            generateMeshData(cx, cy, cz);
            GameObject chunk = (GameObject)Instantiate(map, Vector3.zero, map.transform.rotation);
            chunk.name = cx + ":" + cy + ":" + cz;
            Mesh m = getMesh();
            chunk.GetComponent<MeshFilter>().mesh = m;
            chunk.GetComponent<MeshCollider>().sharedMesh = m;
        }
        
    }

    
    void resetVars()
    {
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();
        triangles = new List<int>();
    }

    void populateMap()
    {
        HeightMapGenerator.width = mapWidth;
        HeightMapGenerator.height = mapLength;
        HeightMapGenerator.Generate(seed);
        Texture2D heightMap = HeightMapGenerator.GetTexture();
        Color c;

        mapData = new bool[mapWidth, mapHeight, mapLength];
        for(int x = 0; x < mapWidth; x++)
            for(int z = 0; z < mapLength; z++){
                c = heightMap.GetPixel(x, z);
                for(int y = 0; y < mapHeight; y++){
                    //if(mapData[x,y,z]) continue;
                    mapData[x, y, z] = y < c.g * groundStart || Perlin3D(x,y,z) < density && !(c.r > 0 && y < 10 + c.r * groundStart);
                }
            }            
    }

    void generateMeshData(int chunkX, int chunkY, int chunkZ)
    {
        int startX = chunkX * chunkSize;
        int startZ = chunkZ * chunkSize;
        int startY = chunkY * chunkSize;

        for(int x = startX; x < mapWidth && x < startX + chunkSize; x++)
        for(int z = startZ; z < mapLength && z < startZ + chunkSize; z++)
        for(int y = startY; y < mapHeight && y < startY + chunkSize; y++)
        {
            if(!mapData[x, y, z]) continue;

            Vector3 v = new Vector3(x - mapWidth / 2, y, z - mapLength / 2);
            // Left
            if(x > 0 && !mapData[x-1,y,z]){
                makePlane(
                    v + Vector3.up,
                    v + Vector3.up + Vector3.forward,
                    v,
                    v + Vector3.forward,
                    Vector3.left
                );
            }
            // Right
            if(x < mapWidth - 1 && !mapData[x+1,y,z]){
                makePlane(
                    v + Vector3.right + Vector3.up + Vector3.forward,
                    v + Vector3.right + Vector3.up,
                    v + Vector3.right + Vector3.forward,
                    v + Vector3.right,
                    Vector3.right
                );
            }
            // Front
            if(z > 0 && !mapData[x,y,z-1]){
                makePlane(
                    v + Vector3.right + Vector3.up,
                    v + Vector3.up,
                    v + Vector3.right,
                    v,
                    Vector3.forward
                );
            }
            // Back
            if(z < mapLength - 1 && !mapData[x,y,z+1]){
                makePlane(
                    v + Vector3.up + Vector3.forward,
                    v + Vector3.right + Vector3.up + Vector3.forward,
                    v + Vector3.forward,
                    v + Vector3.right + Vector3.forward,
                    Vector3.back
                );
            }
            // Top
            if(y == mapHeight - 1 || !mapData[x,y + 1,z]){
                makePlane(
                    v + Vector3.up,
                    v + Vector3.right + Vector3.up,
                    v + Vector3.up + Vector3.forward,
                    v + Vector3.right + Vector3.up + Vector3.forward,
                    Vector3.up,
                    true
                );
            }
            // Bottom
            if(y == 0 || !mapData[x,y - 1,z]){
                makePlane(
                    v + Vector3.right,
                    v,
                    v + Vector3.right + Vector3.forward,
                    v + Vector3.forward,
                    Vector3.down
                );
            }
        }
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

    float Perlin(float x, float y)
    {
        return 
            Mathf.PerlinNoise((x + seed) * 0.06f, (y + seed) * 0.06f) 
            //+ 0.5f * Mathf.PerlinNoise(x * 0.1f + seed, y * 0.1f + seed) 
            //+ 0.25f * Mathf.PerlinNoise(x * 0.2f + seed, y * 0.2f + seed)
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
        int height = Mathf.RoundToInt(y);
        float top = (int)(10f / mapHeight * height) / 10f;
        //int rowCount = mapHeight;
        //float top = (rowCount - height <= 0) ? 0.9f : 1.0f / (rowCount - height) - 0.1f;
        float offset = isTop ? 0.6f : 0;
        return new Vector2[]{ 
            new Vector2(offset, top), 
            new Vector2 (offset, top + 0.1f), 
            new Vector2(offset + 0.3f, top), 
            new Vector2 (offset + 0.3f, top + 0.1f), 
        };
    }

}

