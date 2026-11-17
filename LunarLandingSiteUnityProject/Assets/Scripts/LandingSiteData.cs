using System;
using UnityEngine;

[CreateAssetMenu(fileName ="NewLandingSite", menuName ="Landing Site Data", order = 0)]
public class LandingSiteData : ScriptableObject
{
   [field: SerializeField] public LandingSite landingSite {get; private set;}
}


[Serializable]
public struct LandingSite
{
    public string site,slopeFile,lat,lon;
    public TerrainData terrainData;
    public LandingSite(string _site,string _file,string _lat,string _lon, TerrainData _terrainData)
    {
        site = _site;
        slopeFile = _file;
        lat = _lat;
        lon = _lon;
        terrainData = _terrainData;
    }
}