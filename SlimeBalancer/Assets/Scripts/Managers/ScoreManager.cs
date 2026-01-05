using UnityEngine;

public class ScoreManager : BaseManager
{
    public void AddScore(int points)
    {
        Debug.Log($"Added {points} points to the score.");
    }
}
