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
            Debug.Log("Slime hit");
            GameManager.ScoreManager.AddScore(10);
            snakeGame.MoveSpeed += 0.2f;
            Destroy(other.gameObject);
            snakeGame.SpawnSlime();
            
        }
    }

 
}
