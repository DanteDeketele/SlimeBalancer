using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SnakeGame : BaseGame
{
    public GameObject player;
    public GameObject SlimePrefab;

    public GameObject ParticleEffect;

    public float MoveSpeed = 1f;
    public Vector2 PlayAreaSize = new Vector2(10f, 10f);

    private Vector3 moveDirection = Vector3.forward;
    private Vector3 lastMoveDirection = Vector3.forward;

    private Vector3 cellStart;
    private Vector3 cellEnd;
    private float cellProgress;

    private List<GameObject> snakeSegments = new();
    private Queue<Vector3> positionHistory = new();

    // Pending segments to add after the head moves to the next cell
    private List<GameObject> pendingSegments = new List<GameObject>();

    private Vector3 CurrentCell =>
        new Vector3(
            Mathf.Round(player.transform.position.x),
            player.transform.position.y,
            Mathf.Round(player.transform.position.z)
        );

    private float waveTimer = 0f;
    private float waveDuration = 0.5f; // Duration of the wave effect
    private float waveScaleFactor = 1.3f; // How much the segments scale up during the wave
    private bool isWaveActive = false; // Tracks if the wave effect is active
    private bool canMove = true; // Boolean to track if the snake can move

    public override void StartGame()
    {
        base.StartGame();
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.SnakeMainTheme, true, true);

        Vector3 startCell = CurrentCell;
        cellStart = startCell;
        cellEnd = startCell + moveDirection;
        cellProgress = 0f;

        positionHistory.Enqueue(startCell);

        SpawnSlime();
    }

    public void TriggerWaveEffect()
    {
        isWaveActive = true;
        waveTimer = 0f; // Reset the wave timer
    }

    public override void UpdateGame()
    {
        Vector2 input = GameManager.InputManager.InputVector;
        if (input.magnitude > 0.4f && canMove)
        {
            OnInput(input);
            canMove = false; // Prevent further moves until the next cell is reached
        }

        cellProgress += MoveSpeed * Time.deltaTime;

        // Adjust wave duration to scale with the snake's length
        float adjustedWaveDuration = waveDuration + snakeSegments.Count * 0.05f;

        Vector3[] history = positionHistory.ToArray();
        for (int i = 0; i < snakeSegments.Count; i++)
        {
            int index = history.Length - i - 2;
            if (index < 0) break;

            Vector3 from = history[index];
            Vector3 to = history[index + 1];

            snakeSegments[i].transform.position = Vector3.Lerp(from, to, cellProgress);

            // Apply wave effect to the scale of the segment only if wave is active
            if (isWaveActive)
            {
                float waveOffset = (i / (float)snakeSegments.Count) * adjustedWaveDuration; // Spread wave across all segments
                float scale = 1f + Mathf.Sin((waveTimer - waveOffset) * Mathf.PI * 2 / adjustedWaveDuration) * (waveScaleFactor - 1f);
                snakeSegments[i].transform.localScale = Vector3.one * Mathf.Max(1f, scale); // Ensure scale doesn't go below 1
            }
        }

        // Ensure the wave effect finishes smoothly for all segments
        if (isWaveActive)
        {
            float waveSpeed = snakeSegments.Count / adjustedWaveDuration;
            float waveWidth = 2f; // Controls smoothness of the wave

            for (int i = 0; i < snakeSegments.Count; i++)
            {
                // Distance from wave center
                float distance = Mathf.Abs(i - waveTimer);

                // Smooth bell-shaped curve
                float influence = Mathf.Exp(-(distance * distance) / (waveWidth * waveWidth));

                float scale = Mathf.Lerp(1f, waveScaleFactor, influence);
                snakeSegments[i].transform.localScale = Vector3.one * scale * 0.9f;
            }

            // Head reacts slightly stronger
            float headInfluence = Mathf.Exp(-(waveTimer * waveTimer) / (waveWidth * waveWidth));
            float headScale = Mathf.Lerp(1f, waveScaleFactor, headInfluence);
            player.transform.localScale = Vector3.one * headScale;

            // Move wave forward
            waveTimer += Time.deltaTime * waveSpeed;

            // End once wave passed the tail
            if (waveTimer > snakeSegments.Count + waveWidth)
            {
                isWaveActive = false;
                waveTimer = 0f;

                player.transform.localScale = Vector3.one;
                foreach (var seg in snakeSegments)
                    seg.transform.localScale = Vector3.one * 0.9f;
            }
        }

        player.transform.position = Vector3.Lerp(cellStart, cellEnd, cellProgress);
        player.transform.rotation = Quaternion.Lerp(
            player.transform.rotation,
            Quaternion.LookRotation(lastMoveDirection),
            Time.deltaTime * 10f
        );

        if (cellProgress >= 1f)
        {
            cellProgress = 0f;
            cellStart = cellEnd;
            cellEnd = cellStart + lastMoveDirection;

            positionHistory.Enqueue(cellStart);

            foreach (var seg in pendingSegments)
            {
                // Place the new segment at the last segment's position
                Vector3 tailPos = snakeSegments.Count > 0
                    ? snakeSegments[^1].transform.position
                    : positionHistory.Peek(); // If no segments, use the tail position

                seg.transform.position = tailPos;
                snakeSegments.Add(seg);
            }

            // Clear pending segments after adding them
            pendingSegments.Clear();

            while (positionHistory.Count > snakeSegments.Count + 2)
                positionHistory.Dequeue();

            canMove = true; // Allow movement again after completing the cell
        }

        if (Mathf.Abs(player.transform.position.x - 0.5f) > PlayAreaSize.x / 2 ||
            Mathf.Abs(player.transform.position.z - 0.5f) > PlayAreaSize.y / 2)
        {
            EndGame(false);
        }
        //add if the head hits any of its segments game over
        foreach (var segment in snakeSegments)
        {
            if (Vector3.Distance(player.transform.position, segment.transform.position) < 0.5f)
            {
                EndGame(false);
            }
        }

        base.UpdateGame();
    }

    private void OnInput(Vector2 direction)
    {
        // make input into normilised into x or y direction
        
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            direction.y = 0;
        }
        else
        {
            direction.x = 0;
        }
        direction.Normalize();

        // Only allow perpendicular direction changes
        if (lastMoveDirection == Vector3.forward || lastMoveDirection == Vector3.back)
        {
            if (direction.x > 0) moveDirection = Vector3.right;
            else if (direction.x < 0) moveDirection = Vector3.left;
        }
        else
        {
            if (direction.y > 0) moveDirection = Vector3.forward;
            else if (direction.y < 0) moveDirection = Vector3.back;
        }

        lastMoveDirection = moveDirection;
    }

    // Pickup becomes a snake segment (delayed until next cell commit)
    public void GrowSnake(GameObject slime)
    {
        // Get slime color
        Renderer slimeRenderer = slime.GetComponent<Renderer>();
        Color slimeColor = slimeRenderer.material.GetColor("_MainColor");

        // Instantiate particle effect
        GameObject particleInstance = Instantiate(
            ParticleEffect,
            slime.transform.position + Vector3.up * 0.5f,
            Quaternion.identity
        );

        // Apply color to the INSTANCE material
        Renderer particleRenderer = particleInstance.GetComponent<Renderer>();
        particleRenderer.material.SetColor("_MainColor", slimeColor);


        StartCoroutine(GameManager.InputManager.LedBlink(Color.green, 2, .25f, endEffect: InputManager.LightingEffect.Rainbow));
        slime.SetActive(true);
        if (slime.TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }

        slime.tag = "SnakeSegment";

        // remove all children
        foreach (Transform child in slime.transform)
        {
            if (child.gameObject.name != "outline")
                Destroy(child.gameObject);
        }

        // Add to pending list instead of immediately to snakeSegments
        pendingSegments.Add(slime);
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.SnakeGrowSound);


        TriggerWaveEffect(); // Trigger the wave effect when the snake grows
    }

    public void SpawnSlime()
    {
        Bounds bounds = new Bounds(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(PlayAreaSize.x, 0, PlayAreaSize.y)
        );

        Vector3 spawn;
        do
        {
            spawn = new Vector3(
                Mathf.Round(Random.Range(bounds.min.x, bounds.max.x)),
                0.5f,
                Mathf.Round(Random.Range(bounds.min.z, bounds.max.z))
            );
        } while (positionHistory.Contains(spawn));

        GameObject slime = Instantiate(SlimePrefab, spawn, Quaternion.identity, transform);
        slime.tag = "Slimes";

        // Assign unique material
        Renderer r = slime.GetComponent<Renderer>();
        Material mat = new Material(r.sharedMaterial);
        mat.SetColor("_MainColor", Random.ColorHSV(0, 1, .8f, 1f, .8f, 1f));
        mat.SetColor("_SideColor", Random.ColorHSV(0, 1, .8f, 1f, .8f, 1f));
        mat.SetFloat("_Seed", Random.Range(0f, 100f));
        r.sharedMaterial = mat;
    }

    public override void EndGame(bool won = false)
    {
        Instantiate(ParticleEffect, player.transform.position + Vector3.up * 1.5f, Quaternion.identity);
        MoveSpeed = 0;
        GameManager.SoundManager.ChangeVolumeMusic(GameManager.SoundManager.SnakeMainTheme, 0.5f);
        GameManager.SoundManager.PlaySound(GameManager.SoundManager.SnakeWallHitSound);
        base.EndGame(won);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(PlayAreaSize.x, 0, PlayAreaSize.y)
        );

        // draw gizmos for segment list and queue

        foreach (var seg in snakeSegments)
        {
            Gizmos.color = Color.hotPink;
            Gizmos.DrawSphere(seg.transform.position, 1f);
            Handles.color = Color.hotPink;
            Handles.Label(seg.transform.position + Vector3.up * 1.5f, "Segment" + snakeSegments.IndexOf(seg));
        }
        foreach (var pos in positionHistory)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(pos, Vector3.up + Vector3.one * 0.1f);
            Handles.color = Color.cyan;
            Handles.Label(pos + Vector3.up * 1.5f, "Pos" + positionHistory.ToArray().ToList().IndexOf(pos));
        }
    }
#endif
}
