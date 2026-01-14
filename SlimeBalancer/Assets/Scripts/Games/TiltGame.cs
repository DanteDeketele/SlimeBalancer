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

        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.blue, BluetoothClient.BoardSide.Left);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.yellow, BluetoothClient.BoardSide.Bottom);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.green, BluetoothClient.BoardSide.Right);
        GameManager.InputManager.SetLightingEffect(InputManager.LightingEffect.Custom, Color.red, BluetoothClient.BoardSide.Top);
    }

    public void FixedUpdate()
    {
        Vector3 rotation = GameManager.InputManager.InputEulerRotation;
        rotation.x *= 1.5f;
        rotation.z *= 1.5f;
        Quaternion quaternion = Quaternion.Euler(rotation.x, 0, rotation.z);
        quaternion = Quaternion.Lerp(player.transform.rotation, quaternion, Time.deltaTime * 15);
        player.transform.rotation = quaternion;
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

    
        float heightAbovePlayer = 15f; 

        Vector3 spawnPoint = new Vector3(
            playerposition.x + Random.Range(-1.5f, 1.5f),
            playerposition.y + heightAbovePlayer,
            playerposition.z + Random.Range(-1.5f, 1.5f)
        );
        GameObject ob = Instantiate(PrefabSlime[Random.Range(0, PrefabSlime.Length)], spawnPoint, Quaternion.identity, transform);
        Destroy(ob, 20f);
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
