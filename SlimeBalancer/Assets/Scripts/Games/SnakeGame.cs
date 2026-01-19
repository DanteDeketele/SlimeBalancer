using UnityEngine;

public class SnakeGame : BaseGame
{
    public GameObject player;


    public GameObject SlimePrefab;

    private GameObject CurrentSlime;

    public float MoveSpeed = 5f;

    public Vector2 PlayAreaSize = new Vector2(10f, 10f);

    private float SpawnDelay = 20f;
    private float SpawnTimer = 0f;

    

    private Vector3 MoveDirection = Vector3.forward; // default direction
    private Vector3 LastMoveDirection = Vector3.forward;

    

    
    public override void StartGame()
    {
        base.StartGame();
        SpawnSlime();
        
        GameManager.InputManager.OnAnyDirection.AddListener(OnInput);
    }

    public override void UpdateGame()
    {
        // Always move forward

        // First move to the center of the next grid cell before changing direction
        if (LastMoveDirection != MoveDirection && Vector3.Distance(player.transform.position, 
            new Vector3(
                Mathf.Round(player.transform.position.x),
                player.transform.position.y,
                Mathf.Round(player.transform.position.z)
            )) < 0.1f)
        {
            // Change direction
            LastMoveDirection = MoveDirection;
            // snap to grid
            player.transform.position = new Vector3(
                Mathf.Round(player.transform.position.x),
                player.transform.position.y,
                Mathf.Round(player.transform.position.z)
            );
          
        }

        player.transform.position += LastMoveDirection * MoveSpeed * Time.deltaTime;
        player.transform.rotation = Quaternion.Lerp(player.transform.rotation, 
            Quaternion.LookRotation(LastMoveDirection), 
            Time.deltaTime * 10f);

        // death condition: out of bounds
        if (Mathf.Abs(player.transform.position.x) > PlayAreaSize.x / 2 ||
            Mathf.Abs(player.transform.position.z) > PlayAreaSize.y / 2)
        {
            Debug.Log("Out of bounds! Game Over.");
            EndGame(false);
        }

        CheckTimer();

        base.UpdateGame();
    }

    private void CheckTimer()
    {
        SpawnTimer += Time.deltaTime;
        if (SpawnTimer >= SpawnDelay)
        {
            SpawnSlime();
            SpawnTimer = 0f;
        }
    }

    private void OnInput(Vector2 direction)
    {
        // Only allow direction change perpendicular to current movement
        if (MoveDirection == Vector3.forward || MoveDirection == Vector3.back)
        {
            // Moving vertically: only left/right allowed
            if (direction.x > 0)
            {
                MoveDirection = Vector3.right;
            }
            else if (direction.x < 0)
            {
                MoveDirection = Vector3.left;
            }
        }
        else if (MoveDirection == Vector3.right || MoveDirection == Vector3.left)
        {
            // Moving horizontally: only up/down allowed
            if (direction.y > 0)
            {
                MoveDirection = Vector3.forward;
            }
            else if (direction.y < 0)
            {
                MoveDirection = Vector3.back;
            }
        }
    }

    public void SpawnSlime()
    {
        UnityEngine.Debug.Log("Spawning slime");
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(PlayAreaSize.x, 0, PlayAreaSize.y));

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        // snap to grid
        randomX = Mathf.Round(randomX);
        randomZ = Mathf.Round(randomZ);

        Vector3 spawnPoint = new Vector3(
            randomX,
            0.5f,
            randomZ
        );
        
        CurrentSlime = Instantiate(SlimePrefab, spawnPoint, Quaternion.identity, transform);
    }

    public override void EndGame(bool won = false)
    {
        //stop movement
        MoveSpeed = 0;
        base.EndGame(won);
    }

# if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // handles drawing the play area in the editor so it is editable
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(PlayAreaSize.x, 0, PlayAreaSize.y));
    }
# endif




}