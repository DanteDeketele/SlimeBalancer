using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkiGame : BaseGame
{
    public Transform Player;
    public GameObject Floor;
    public float Speed = 5f;
    Vector3 _floorDirection;
    GameObject _floorClone;
    float _distanceTravelled = 0f;
    public GameObject FlagsPrefab;
    private float flagDistance= 25;
    List<GameObject> _flags = new List<GameObject>();
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
        Player.Translate(Vector3.right * input.x * Time.deltaTime * 5f);

        Speed += Time.deltaTime * 0.01f;

        _distanceTravelled -= Speed * Time.deltaTime;
        Floor.transform.position = _floorDirection * (_distanceTravelled % 400);
        _floorClone.transform.position = _floorDirection * ((_distanceTravelled + 200) % 400);

        if (-_distanceTravelled > flagDistance)
        {
            flagDistance += 20f;
            GameObject flag = Instantiate(FlagsPrefab, _floorDirection * 20, Quaternion.identity);
            flag.transform.position += new Vector3(flagPositionX + Random.Range(-10f, 10f), 0, 0);
            flagPositionX = flag.transform.position.x;
            _flags.Add(flag);
        }

        foreach (GameObject flag in _flags)
        {
            flag.transform.position += -_floorDirection * Speed * Time.deltaTime;
            Debug.Log("MovedFlag");
            if (flag.transform.position.z < -10f)
            {
                _flags.Remove(flag);
                Destroy(flag);
            }
        }


    }

    public override void EndGame()
    {
        base.EndGame();
    }


}
