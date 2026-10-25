using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    public int siteCount = 40;
    public float worldSize = 10f;
    public int seed = 1;

    public static WorldSettings Instance { get; private set; }

    private void Awake() {
        Instance = this;

        //set values to json or create new values if missing json

        //call required inits
        Random.InitState(seed);
    }

    //set to new random seed (in production, set to time instead of incrementing)
    public void SetSeed()
    {
        seed++;
        Random.InitState(seed);
    }

    //set to specific seed
    public void SetSeed(int newSeed)
    {
        seed = newSeed;
        Random.InitState(seed);
    }
}
