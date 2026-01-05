using UnityEngine;

public class SkiGame : BaseGame
{
    public Transform Player;
    public override void StartGame()
    {
        base.StartGame();
    }

    public override void UpdateGame()
    {
        Vector2 input = GameManager.InputManager.Input;
        Player.Translate(Vector3.right * input.x * Time.deltaTime * 5f);
    }

    public override void EndGame()
    {
        base.EndGame();
    }


}
