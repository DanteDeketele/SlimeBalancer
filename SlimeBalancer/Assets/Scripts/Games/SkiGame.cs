using UnityEngine;

public class SkiGame : BaseGame
{
    public Transform Player;
    public GameObject Floor;
    public float Speed = 0.5f;
    Vector3 _floorDirection;
    GameObject _floorClone;
    float _distanceTravelled = 0f;
    public override void StartGame()
    {
        _floorDirection = Floor.transform.forward;
        _floorClone = Instantiate(Floor, Floor.transform.position + _floorDirection * 400f, Floor.transform.rotation);
        base.StartGame();
    }

    public override void UpdateGame()
    {
        Vector2 input = GameManager.InputManager.InputVector;
        Player.Translate(Vector3.right * input.x * Time.deltaTime * 5f);

        Speed += Time.deltaTime * 0.01f;

        _distanceTravelled -= Speed * Time.deltaTime;
        Floor.transform.position = _floorDirection * (_distanceTravelled % 400);
        _floorClone.transform.position = _floorDirection * ((_distanceTravelled + 200) % 400);
    }

    public override void EndGame()
    {
        base.EndGame();
    }


}
