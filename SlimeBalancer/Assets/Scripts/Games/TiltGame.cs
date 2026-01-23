using UnityEngine;
using System.Collections;

public class TiltGame : BaseGame
{
    public GameObject player;

    public GameObject[] PrefabSlime;

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
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.TiltItMainTheme, true, true);
        base.StartGame();
        LedBasedOnPipe();
        spawnSlime();
        ChangeDelayBasedOnDifficulty();
    }

    public void ChangeDelayBasedOnDifficulty()
    {
        switch (GameManager.CurrentDifficulty)
        {
            case GameManager.Difficulty.Easy:
                Delay = 7f;
                break;

            case GameManager.Difficulty.Medium:
                Delay = 5f;
                break;

            case GameManager.Difficulty.Hard:
                Delay = 3f;
                break;
        }
    }

    public void FixedUpdate()
    {
        Vector3 rotation = GameManager.InputManager.InputEulerRotation;
        rotation.x *= 1.5f;
        rotation.z *= 1.5f;
        Quaternion quaternion = Quaternion.Euler(rotation.x, 0, rotation.z);
        quaternion = Quaternion.Lerp(player.transform.rotation, quaternion, Time.deltaTime * 15);
        playerRigidbody.MoveRotation(quaternion);
    }

    public override void UpdateGame()
    {
        base.UpdateGame();

        timer += Time.deltaTime;
        if (timer >= Delay)
        {
            spawnSlime();
            timer = 0f;
        }

    }

    public override void EndGame(bool won = false)
    {
        base.EndGame(won);

    }

    public void spawnSlime()
    {
        Vector3 playerposition = player.transform.position;


        float heightAbovePlayer = 12f;

        Vector3 spawnPoint = new Vector3(
            playerposition.x + Random.Range(-1f, 1f),
            playerposition.y + heightAbovePlayer,
            playerposition.z + Random.Range(-1f, 1f)
        );
        GameObject slime = Instantiate(PrefabSlime[Random.Range(0, PrefabSlime.Length)], spawnPoint, Quaternion.identity, transform);
        StartCoroutine(BlinkLedFromObject(slime));


        Destroy(slime, 20f);



    }

    IEnumerator BlinkLedFromObject(GameObject ob)
    {
        SlimeCollider slime = ob.GetComponent<SlimeCollider>();
        SlimeCollider.SlimeColor slimeColor = slime.slimeColor;
        Color color = Color.white;
        switch (slimeColor)
        {
            case SlimeCollider.SlimeColor.Blue:
                color = Color.blue;
                break;
            case SlimeCollider.SlimeColor.Yellow:
                color = Color.yellow;
                break;
            case SlimeCollider.SlimeColor.Green:
                color = Color.green;
                break;
            case SlimeCollider.SlimeColor.Red:
                color = Color.red;
                break;
        }

        yield return GameManager.InputManager.LedBlink(color, 2);
        LedBasedOnPipe();
    }


    public void Correct()
    {
        GameManager.ScoreManager.AddScore(10);
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.TiltGameScoreSound);
    }

    public void Wrong()
    {
        EndGame();
        GameManager.SoundManager.ChangeVolumeMusic(GameManager.SoundManager.TiltItMainTheme, 0.5f);
    }

    private void LedBasedOnPipe()
    {
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.blue, BluetoothClient.BoardSide.Left);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.yellow, BluetoothClient.BoardSide.Bottom);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.green, BluetoothClient.BoardSide.Right);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.red, BluetoothClient.BoardSide.Top);
    }
}
