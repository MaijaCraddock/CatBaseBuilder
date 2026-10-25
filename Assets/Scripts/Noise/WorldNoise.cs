using UnityEngine;

//generate a "World" noise/heightmap for biome generation
//then, a cell gives its position, and the getBiome function returns the biome at that position
public class WorldNoise : MonoBehaviour
{

    public float scale = 20f;
    private float offsetX = 1f;
    private float offsetY = 1f;

    public static WorldNoise Instance { get; private set; }

    void Awake() { Instance = this; }

    void Start()
    {
        SetOffsets();
    }

    public void SetOffsets()
    {
        offsetX = UnityEngine.Random.Range(0f, 8 * WorldSettings.Instance.worldSize);
        offsetY = UnityEngine.Random.Range(0f, 8 * WorldSettings.Instance.worldSize);
    }

    public Biome GetBiomeHeight(Vector2 pos)
    {
        Biome biome = new Biome();
        biome.noiseFunction = 0;

        float perlinCoordX = pos.x / WorldSettings.Instance.worldSize * scale + offsetX;
        float perlinCoordY = pos.y / WorldSettings.Instance.worldSize * scale + offsetY;
        float height = Mathf.PerlinNoise(perlinCoordX, perlinCoordY);
        

        //IN PRODUCTION, JUST RETURN BIOME HEIGHT
        //IN THIS VERSION, WE SET BIOME COLOR ITSELF
        if (height < 0.3) {biome.color = new Color32(76, 129, 204, 255); } //WATER
        else if (height < 0.4) {biome.color = new Color32(201, 181, 135, 255);} //BEACH
        else if (height < 0.6) {biome.color = new Color32(122, 145, 100, 255);} //FOREST;
        else if (height < 0.75) {biome.color = new Color32(44, 110, 65, 255);}//JUNGLE;
        else if (height < 0.9) {biome.color = new Color32(247, 225, 183, 255);}//DESERT;
        else {biome.color = new Color32(252, 247, 237, 255);} //SNOW;

        return biome;
    }
}
