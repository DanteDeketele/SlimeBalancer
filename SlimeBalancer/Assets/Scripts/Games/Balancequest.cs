using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;


public class BalanceQuest : BaseGame
{
    public GameObject player;

    public GameObject[] PrefabSlime;
    private GameObject Slime;

    private float timer;

    [SerializeField] private float Delay = 5f;
    private Rigidbody playerRigidbody;




    public override void Awake()
    {
        playerRigidbody = player.GetComponent<Rigidbody>();
        base.Awake();
    }

    public override void StartGame()
    {
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.BalanceQuestMainTheme, true, true);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, new Color(48f / 255f, 213f / 255f, 150f / 255f));
        base.StartGame();
        spawnSlime();

    }

    public void FixedUpdate()
    {
        Vector3 rotation = GameManager.InputManager.InputEulerRotation;
        Quaternion quaternion = Quaternion.Euler(rotation.x, 0, rotation.z);
        quaternion = Quaternion.Lerp(player.transform.rotation, quaternion, Time.deltaTime * 15f);
        playerRigidbody.rotation = quaternion;
    }

    public override void UpdateGame()
    {
        SlimeOutOfBound();
        base.UpdateGame();

        timer += Time.deltaTime;
        if (timer >= Delay)
        {
            Correct();
            timer = 0f;
            StartCoroutine(GameManager.InputManager.LedBlink(new Color(48f / 255f, 213f / 255f, 150f / 255f), 2, .15f, BluetoothClient.BoardSide.All, new Color(48f / 255f, 213f / 255f, 150f / 255f)));
        }

    }





    public override void EndGame(bool won = false)
    {
        base.EndGame(won);

    }

    //spawn slime in the center above the player
    public void spawnSlime()
    {
        Vector3 playerposition = player.transform.position;


        float heightAbovePlayer = 10f;

        Vector3 spawnPoint = new Vector3(
            playerposition.x,
            playerposition.y + heightAbovePlayer,
            playerposition.z
        );
        //write a switch case to spawn different slime based on difficulty
       switch (GameManager.CurrentDifficulty)
        {
            case GameManager.Difficulty.Easy:
                Slime = Instantiate(PrefabSlime[0], spawnPoint, Quaternion.identity, transform);
                break;

            case GameManager.Difficulty.Medium:
                Slime = Instantiate(PrefabSlime[1], spawnPoint, Quaternion.identity, transform);
                break;

            case GameManager.Difficulty.Hard:
                Slime = Instantiate(PrefabSlime[2], spawnPoint, Quaternion.identity, transform);
                break;
        }




    }


    public void SlimeOutOfBound()
    {
        Vector3 playerPosition = player.transform.position;
        Vector3 slimePosition = Slime.transform.position;

        if (slimePosition.y < playerPosition.y - 10f)
        {
            Destroy(Slime);
            Wrong();
        }
    }



    public void Correct()
    {
        GameManager.ScoreManager.AddScore(10);
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.TiltGameScoreSound);
    }

    public void Wrong()
    {
        EndGame();
        GameManager.SoundManager.ChangeVolumeMusic(GameManager.SoundManager.BalanceQuestMainTheme, 0.5f);
    }
}
