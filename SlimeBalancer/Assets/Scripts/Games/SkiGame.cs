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
    private float flagDistance= 25;
    List<GameObject> _flags = new List<GameObject>();  
    List<GameObject> _obstacles = new List<GameObject>();
    float flagPositionX= 0f;
    Transform _camera;
    float _prevX;
    public override void StartGame()
    {
        _floorDirection = Floor.transform.forward;
        _floorClone = Instantiate(Floor, Floor.transform.position + _floorDirection * 400f, Floor.transform.rotation);
        base.StartGame();
        _camera = Camera.main.transform;
    }

    public override void UpdateGame()
    {
        Vector2 input = GameManager.InputManager.InputVector;
        Player.Translate(Vector3.right * -input.x * Time.deltaTime * 5f);

        Speed += Time.deltaTime * 0.01f;

        _distanceTravelled -= Speed * Time.deltaTime;
        Floor.transform.position = _floorDirection * (_distanceTravelled % 400);
        _floorClone.transform.position = _floorDirection * ((_distanceTravelled + 200) % 400);

        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = _obstacles[i];
            obstacle.transform.position += -_floorDirection * Speed * Time.deltaTime;
            if (obstacle.transform.position.z < 0f)
            {
                if (Mathf.Abs(obstacle.transform.position.x - Player.position.x) < 2f)
                {
                    // Hit obstacle
                    GameManager.ScoreManager.RemoveScore(20);
                }

                Destroy(obstacle);
                _obstacles.Remove(obstacle);
            }
        }

        if (-_distanceTravelled > flagDistance)
        {
            flagDistance += 20f;
            GameObject flag = Instantiate(FlagsPrefab, _floorDirection * 20, Quaternion.identity);
            flag.transform.position += new Vector3(flagPositionX + Random.Range(-10f, 10f), 0, 0);
            flagPositionX = flag.transform.position.x;
            _flags.Add(flag);

            // Spawn obstacles
            for (int i = 0; i < Random.Range(5, 20); i++)
            {
                GameObject obstacle = Instantiate(ObstaclePrefabs[Random.Range(0, ObstaclePrefabs.Length)], _floorDirection * (20 + Random.Range(0, 10)) + Vector3.right * (Random.Range(-50, 50) + Player.position.x), Quaternion.identity);
                
                if (flagPositionX - obstacle.transform.position.x < 4f)
                {
                    Destroy(obstacle);
                }   
                _obstacles.Add(obstacle);
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

    public override void EndGame()
    {
        base.EndGame();
    }


}
