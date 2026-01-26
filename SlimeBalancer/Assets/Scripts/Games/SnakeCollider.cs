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
        if (other.CompareTag("Slimes"))
        {
            if (other.gameObject.activeSelf == false) return;
            Debug.Log("Slime hit");
            other.gameObject.SetActive(false);
            GameManager.ScoreManager.AddScore(10);
            snakeGame.MoveSpeed += 0.01f;
            snakeGame.GrowSnake(other.gameObject);
            snakeGame.SpawnSlime();
            
        }else if (other.CompareTag("SnakeSegment"))
        {
            Debug.Log("Hit own body");
            snakeGame.EndGame(false);
        }
    }

 
}
