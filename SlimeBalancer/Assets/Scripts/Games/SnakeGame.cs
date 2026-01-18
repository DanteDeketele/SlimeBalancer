
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

public class SnakeGame : BaseGame
{
    public GameObject player;


    public GameObject SlimePrefab;

    private GameObject CurrentSlime;

    public float MoveSpeed = 5f;

    public BoxCollider playArea;

    private float SpawnDelay = 20f;
    private float SpawnTimer = 0f;

    

    private Vector3 MoveDirection = Vector3.forward; // default direction

    

    
    public override void StartGame()
    {
        base.StartGame();
        SpawnSlime();
        

        // GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.blue, BluetoothClient.BoardSide.Left);
        // GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.yellow, BluetoothClient.BoardSide.Bottom);
        // GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.green, BluetoothClient.BoardSide.Right);
        // GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.red, BluetoothClient.BoardSide.Top);
    }

    public void Update()
    {
        Vector2 inputVector = GameManager.InputManager.InputVector;

        if (inputVector.x > 0)
            MoveDirection = Vector3.right;
        else if (inputVector.x < 0)
            MoveDirection = Vector3.left;
        else if (inputVector.y > 0)
            MoveDirection = Vector3.forward;
        else if (inputVector.y < 0)
            MoveDirection = Vector3.back;

        // Always move forward
        player.transform.Translate(MoveDirection * MoveSpeed * Time.deltaTime);

        SpawnTimer += Time.deltaTime;
        if (SpawnTimer >= SpawnDelay)
        {
            SpawnSlime();
            SpawnTimer = 0f;
        }
    }

    public void SpawnSlime()
    {
        UnityEngine.Debug.Log("Spawning slime");
        Bounds bounds = playArea.bounds;

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        Vector3 spawnPoint = new Vector3(
            randomX,
            0.5f,
            randomZ
        );
        
        CurrentSlime = Instantiate(SlimePrefab, spawnPoint, Quaternion.identity, transform);
    }



    public override void UpdateGame()
    {
        base.UpdateGame();

       

    }

    public override void EndGame(bool won = false)
    {
        //stop movement
        MoveSpeed = 0;
        base.EndGame(won);
    }


   

    

}