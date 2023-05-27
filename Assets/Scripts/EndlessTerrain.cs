using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float scale = 1f;

    const float viewMoveThresholdForChunkUpdate = 25f;
    const float sqrViewMoveThresholdForChunkUpdate = viewMoveThresholdForChunkUpdate * viewMoveThresholdForChunkUpdate;

    //public const float maxViewDistance = 450;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;

        if ((viewerPositionOld-viewerPosition).sqrMagnitude > sqrViewMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int chunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int chunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(chunkCoordX + xOffset, chunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    TerrainChunk chunk = terrainChunkDictionary[viewedChunkCoord];
                    chunk.UpdateTerrainChunk();
                    //if (chunk.IsVisible())
                    //{
                    //    terrainChunksVisibleLastUpdate.Add(chunk);
                    //}
                } else
                {
                    TerrainChunk chunk = new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial);
                    terrainChunkDictionary.Add(viewedChunkCoord, chunk);
                    terrainChunksVisibleLastUpdate.Add(chunk);
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] detailMeshes;

        MapData mapData;
        bool mapDataReceived;
        int prevLevelOfDetailIndex = -1;

        public TerrainChunk(Vector2 coordinate, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            detailMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailMeshes.Length; i++)
            {
                detailMeshes[i] = new LODMesh(detailLevels[i].levelOfDetail, UpdateTerrainChunk);

            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
            //mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        //void OnMeshDataReceived(MeshData meshData)
        //{
        //    meshFilter.mesh = meshData.CreateMesh();
        //}

        // make sure meshObject is enabled only when within the view distance from the viewer
        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

                if (visible)
                {
                    int levelOfDetailIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            levelOfDetailIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (levelOfDetailIndex != prevLevelOfDetailIndex)
                    {
                        LODMesh detailMesh = detailMeshes[levelOfDetailIndex];
                        if (detailMesh.hasMesh)
                        {
                            prevLevelOfDetailIndex = levelOfDetailIndex;
                            meshFilter.mesh = detailMesh.mesh;
                        }
                        else if (!detailMesh.hasRequesteedMesh)
                        {
                            detailMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequesteedMesh;
        public bool hasMesh;
        int levelOfDetail;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.levelOfDetail = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequesteedMesh = true;
            mapGenerator.RequestMeshData(mapData, levelOfDetail, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int levelOfDetail;
        public float visibleDistanceThreshold;
    }
}
