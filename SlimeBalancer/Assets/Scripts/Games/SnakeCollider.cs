using UnityEngine;

public class SnakeCollider : MonoBehaviour
{
    private SnakeGame snakeGame;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        snakeGame = GameObject.FindAnyObjectByType<SnakeGame>();
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            Debug.Log("Wall hit");
            snakeGame.EndGame();
        }
        else if (other.CompareTag("Slimes"))
        {
            Debug.Log("Slime hit");
            GameManager.ScoreManager.AddScore(10);
            snakeGame.MoveSpeed += 2f;
            Destroy(other.gameObject);
            snakeGame.SpawnSlime();
        }
    }

 
}
