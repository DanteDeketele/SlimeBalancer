using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class TiltGame : BaseGame
{
    public GameObject player;
    public override void StartGame()
    {
        base.StartGame();
    }

    public void Update()
    {
        
    }

    public override void UpdateGame()
    {
        base.UpdateGame();
        Quaternion rotation = GameManager.InputManager.InputRotation;
        rotation = Quaternion.Lerp(player.transform.rotation, rotation, Time.deltaTime * 5);
        player.transform.rotation = rotation;

    }

    public override void EndGame()
    {
        base.EndGame();
    }

}
