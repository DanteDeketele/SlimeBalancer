using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkiGame : BaseGame
{
    public Transform Player;
    public GameObject[] ObstaclePrefabs;
    public GameObject Floor;
    public float Speed = 5f;
    Vector3 _floorDirection;
    GameObject _floorClone;
    float _distanceTravelled = 0f;
    public GameObject FlagsPrefab;
    public float SpawnAheadDistance = 120f; // distance ahead to spawn flags/obstacles
    private float flagDistance= 25;
    List<GameObject> _flags = new List<GameObject>();  
    List<GameObject> _obstacles = new List<GameObject>();
    float flagPositionX= 0f;
    Transform _camera;
    float _prevX;

    public void Start()
    {        
        _floorDirection = Floor.transform.forward;
        _camera = Camera.main.transform;

        // Pre-spawn flags (and obstacles) up to SpawnAheadDistance before the run starts
        PreSpawnAhead();
    }
    public override void StartGame()
    {
        _floorClone = Instantiate(Floor, Floor.transform.position + _floorDirection * 400f, Floor.transform.rotation, transform);

        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.white);

        base.StartGame();
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.SlimeSkieMainTheme, true, true);
    }

    private void PreSpawnAhead()
    {
        // Spawn flags every 20m up to SpawnAheadDistance
        if (FlagsPrefab == null) return;

        int segments = Mathf.FloorToInt(SpawnAheadDistance / 20f);
        for (int i = 1; i <= segments; i++)
        {
            float segDistance = i * 20f;
            GameObject flag = Instantiate(FlagsPrefab, _floorDirection * segDistance, Quaternion.identity, transform);
            flag.transform.position += new Vector3(flagPositionX + Random.Range(-10f, 10f), 0, 0);
            flagPositionX = flag.transform.position.x;
            _flags.Add(flag);

            // Spawn obstacles around this segment distance
            if (ObstaclePrefabs != null && ObstaclePrefabs.Length > 0)
            {
                int obsCount = Random.Range(5, 100);
                for (int j = 0; j < obsCount; j++)
                {
                    float xpos = Random.Range(-60, 60) + Player.position.x;
                    
                    int index = Random.Range(0, ObstaclePrefabs.Length);
                    if (Mathf.Abs(flagPositionX - xpos) > 13f || index == 1) // 1 is snowman    
                    {
                        GameObject obstacle = Instantiate(
                            ObstaclePrefabs[index],
                        _floorDirection * (segDistance + Random.Range(0, 20)) + Vector3.right * xpos,
                        Quaternion.Euler(0, Random.Range(0, 360), 0), transform);

                        _obstacles.Add(obstacle);
                    }
                }
            }
        }

        // Set next spawn threshold to continue every 20m after the farthest pre-spawned flag
        flagDistance = (segments+1) * 20f;
    }

    public override void UpdateGame()
    {
        Vector2 input = GameManager.InputManager.InputVector;
        Player.Translate(Vector3.right * input.x * Time.deltaTime * 5f);

        Speed += Time.deltaTime * 0.01f;

        _distanceTravelled -= Speed * Time.deltaTime;
        float currentTravel = -_distanceTravelled; // positive distance forward from start
        Floor.transform.position = _floorDirection * (_distanceTravelled % 400);
        _floorClone.transform.position = _floorDirection * ((_distanceTravelled + 200) % 400);

        if (ObstaclePrefabs.Length != 0)
        {
            for (int i = _obstacles.Count - 1; i >= 0; i--)
            {
                GameObject obstacle = _obstacles[i];
                if (obstacle == null)
                {
                    _obstacles.RemoveAt(i);
                    continue;
                }
                obstacle.transform.position += -_floorDirection * Speed * Time.deltaTime;
                if (obstacle.transform.position.z < 0f)
                {
                    if (Mathf.Abs(obstacle.transform.position.x - Player.position.x) < 2f)
                    {
                        // Hit obstacle
                        GameManager.SoundManager.PlaySound(GameManager.SoundManager.SkiGameCrashSound);
                        EndGame(false);
                    }

                    Destroy(obstacle);
                    _obstacles.Remove(obstacle);
                }
            }
        }

        // Keep the ahead buffer filled: spawn the next flag when we've travelled enough
        // so that a new flag at SpawnAheadDistance maintains the 20m cadence.
        while (currentTravel > (flagDistance - SpawnAheadDistance))
        {
            flagDistance += 20f;
            GameObject flag = Instantiate(FlagsPrefab, _floorDirection * SpawnAheadDistance, Quaternion.identity, transform);
            flag.transform.position += new Vector3(flagPositionX + Random.Range(-10f, 10f), 0, 0);
            flagPositionX = flag.transform.position.x;
            _flags.Add(flag);

            // Spawn obstacles further ahead around the flag spawn distance
            for (int i = 0; i < Random.Range(5, 100); i++)
            {
                float xPos = Random.Range(-60, 60) + Player.position.x;
                int index = Random.Range(0, ObstaclePrefabs.Length);
                if (Mathf.Abs(flagPositionX - xPos) > 13f || index == 1) // 1 is snowman    
                {
                    GameObject obstacle = Instantiate(
                        ObstaclePrefabs[index],
                        _floorDirection * (SpawnAheadDistance + Random.Range(0, 20)) + Vector3.right * xPos,
                        Quaternion.identity, transform);
                
                
                    _obstacles.Add(obstacle);
                }
            }
        }

        for (int i = _flags.Count - 1; i >= 0; i--)
        {
            GameObject flag = _flags[i];
            flag.transform.position += -_floorDirection * Speed * Time.deltaTime;
            Debug.Log("MovedFlag");
            if (flag.transform.position.z < 0f)
            {
                // check for score here 
                if (Mathf.Abs(flag.transform.position.x - Player.position.x) < 3f)
                {
                    GameManager.ScoreManager.AddScore(10);
                    GameManager.SoundManager.PlaySound(GameManager.SoundManager.SkiGameScoreSound);
                    StartCoroutine(GameManager.InputManager.LedBlink(new Color(48f/255f, 213f/255f, 150f/255f), 2, .25f, BluetoothClient.BoardSide.All, Color.white));
                }
                else
                {
                    //failed to pass through flag
                }

                Destroy(flag);
                _flags.Remove(flag);
            }
        }

        // _camera.rotation = GameManager.InputManager.InputRotation;
        _camera.rotation = Quaternion.Lerp(_camera.rotation, Quaternion.Euler(GameManager.InputManager.InputRotation.eulerAngles.x + 30, 0, GameManager.InputManager.InputRotation.eulerAngles.z), Time.deltaTime * 2f);
    }

    public override void EndGame(bool won = false)
    {
        base.EndGame(won);
    }


}
