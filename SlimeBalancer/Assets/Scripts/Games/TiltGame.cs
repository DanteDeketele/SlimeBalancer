using UnityEngine;

public class TiltGame : BaseGame
{
    public GameObject player;

    public GameObject[] PrefabSlime;

    private float timer;

    [SerializeField] private float Delay = 5f;

    public override void StartGame()
    {
        base.StartGame();

        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.red, BluetoothClient.BoardSide.Left);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.green, BluetoothClient.BoardSide.Bottom);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.blue, BluetoothClient.BoardSide.Right);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.yellow, BluetoothClient.BoardSide.Top);
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

    
        float heightAbovePlayer = 15f; 

        Vector3 spawnPoint = new Vector3(
            playerposition.x + Random.Range(-2.5f, 2.5f),
            playerposition.y + heightAbovePlayer,
            playerposition.z + Random.Range(-2.5f, 2.5f)
        );
        Instantiate(PrefabSlime[Random.Range(0, PrefabSlime.Length)], spawnPoint, Quaternion.identity, transform);
    }

    public void Correct()
    {
        GameManager.ScoreManager.AddScore(10);
    }

    public void Wrong()
    {
        EndGame();
    }

}
