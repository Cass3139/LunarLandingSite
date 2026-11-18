using System;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance;
    public static LandingSiteData currentLandingSiteData {get; private set;}
    [SerializeField] private Terrain mainTerrain;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        Instance = this;
    }
    public void ChangeTerrain(LandingSiteData landingSiteData)
    {
        currentLandingSiteData = landingSiteData;
        mainTerrain.terrainData = landingSiteData.landingSite.terrainData;
        mainTerrain.GetComponent<TerrainCollider>().terrainData = landingSiteData.landingSite.terrainData;
    }
}

