using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SimonSays : MonoBehaviour
{
    public static SimonSays Instance { get; private set; }

    private enum GameState
    {
        Idle,
        ShowingSequence,
        PlayerTurn,
        Lost
    }

    [Header("Cubes")]
    [SerializeField] private SimonCube[] cubes = new SimonCube[4];

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionsText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI lostText;

    [Header("Timing")]
    [SerializeField] private float sequenceDisplayInterval = 0.8f;
    [SerializeField] private float sequenceDisplayDelay = 0.5f;

    [Header("Input")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private LayerMask cubeLayerMask = ~0;

    private readonly List<int> sequence = new List<int>();
    private GameState state = GameState.Idle;
    private int playerIndex;
    private int score;
    private int lastProcessedInputFrame = -1;

    public bool IsPlayerTurn => state == GameState.PlayerTurn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (lostText != null)
        {
            lostText.gameObject.SetActive(false);
        }

        score = 0;
        UpdateScoreText();

        if (instructionsText != null)
        {
            instructionsText.text = "Observa el patrón";
        }

        StartCoroutine(StartGameRoutine());
    }

    private void Update()
    {
        if (state != GameState.PlayerTurn)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        ProcessNewInputSystem();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        ProcessLegacyInputSystem();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void ProcessNewInputSystem()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectCubeFromScreen(Mouse.current.position.ReadValue(), "MouseClick");
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            TrySelectCubeFromScreen(Touchscreen.current.primaryTouch.position.ReadValue(), "TouchTap");
        }
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    private void ProcessLegacyInputSystem()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectCubeFromScreen(Input.mousePosition, "MouseClick");
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TrySelectCubeFromScreen(Input.GetTouch(0).position, "TouchTap");
        }
    }
#endif

    private void TrySelectCubeFromScreen(Vector2 screenPosition, string source)
    {
        Camera cam = inputCamera != null ? inputCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[SimonSays] No camera available for input raycast.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, cubeLayerMask))
        {
            SimonCube cube = hit.collider.GetComponentInParent<SimonCube>();
            if (cube == null)
            {
                Debug.Log($"[SimonSays] {source} ray hit '{hit.collider.name}', but no SimonCube component found.");
                return;
            }

            if (lastProcessedInputFrame == Time.frameCount)
            {
                return;
            }

            lastProcessedInputFrame = Time.frameCount;
            Debug.Log($"[SimonSays] {source} ray selected cube: {cube.gameObject.name}");
            OnCubeClicked(cube);
        }
        else
        {
            Debug.Log($"[SimonSays] {source} raycast did not hit any cube collider.");
        }
    }

    private IEnumerator StartGameRoutine()
    {
        state = GameState.Idle;

        if (instructionsText != null)
        {
            instructionsText.text = "Observa el patrón";
        }

        yield return new WaitForSeconds(1.5f);

        StartCoroutine(PlayRoundRoutine());
    }

    private IEnumerator PlayRoundRoutine()
    {
        if (state == GameState.Lost)
        {
            yield break;
        }

        if (cubes == null || cubes.Length == 0)
        {
            Debug.LogError("SimonSays: No cubes assigned.");
            yield break;
        }

        state = GameState.ShowingSequence;
        playerIndex = 0;

        if (instructionsText != null)
        {
            instructionsText.text = "Observa el patrón";
        }

        sequence.Add(Random.Range(0, cubes.Length));

        yield return new WaitForSeconds(sequenceDisplayDelay);

        for (int i = 0; i < sequence.Count; i++)
        {
            if (state == GameState.Lost)
            {
                yield break;
            }

            int cubeIndex = sequence[i];
            if (cubeIndex >= 0 && cubeIndex < cubes.Length && cubes[cubeIndex] != null)
            {
                cubes[cubeIndex].Flash();
            }

            yield return new WaitForSeconds(sequenceDisplayInterval);
        }

        if (state != GameState.Lost)
        {
            state = GameState.PlayerTurn;

            if (instructionsText != null)
            {
                instructionsText.text = "Repite el patrón";
            }
        }
    }

    public void OnCubeClicked(SimonCube cube)
    {
        if (state != GameState.PlayerTurn || cube == null)
        {
            Debug.LogWarning($"[SimonSays] Click ignored. State={state}, CubeNull={cube == null}");
            return;
        }

        int clickedIndex = System.Array.IndexOf(cubes, cube);
        if (clickedIndex < 0)
        {
            Debug.LogWarning("[SimonSays] Clicked cube is not in cubes array.");
            return;
        }

        int expectedIndex = playerIndex < sequence.Count ? sequence[playerIndex] : -1;
        Debug.Log($"[SimonSays] Input step {playerIndex + 1}/{sequence.Count}. Expected={expectedIndex}, Clicked={clickedIndex} ({cube.gameObject.name})");

        cube.Highlight();

        if (playerIndex >= sequence.Count)
        {
            Debug.LogWarning("[SimonSays] Extra input ignored: playerIndex exceeded sequence length.");
            return;
        }

        if (sequence[playerIndex] == clickedIndex)
        {
            playerIndex++;
            Debug.Log($"[SimonSays] Correct input. Next expected step index={playerIndex}");

            if (playerIndex >= sequence.Count)
            {
                Debug.Log("[SimonSays] Round completed successfully.");
                score++;
                UpdateScoreText();

                // Save score to Firebase
                if (FirebaseManager.Instance != null)
                {
                    FirebaseManager.Instance.SaveScoreIfHigher(score,
                        onSuccess: () =>
                        {
                            FirebaseManager.Instance.SaveScoreToLeaderboard(
                                FirebaseManager.Instance.Username, score);
                        },
                        onFailure: (error) => Debug.LogWarning($"Failed to save score: {error}")
                    );
                }

                state = GameState.Idle;
                StartCoroutine(AdvanceToNextRoundRoutine());
            }
        }
        else
        {
            Debug.LogWarning($"[SimonSays] Wrong input. Expected={sequence[playerIndex]}, Clicked={clickedIndex}. Losing game.");
            LoseGame();
        }
    }

    private IEnumerator AdvanceToNextRoundRoutine()
    {
        yield return new WaitForSeconds(1f);

        if (state == GameState.Lost)
        {
            yield break;
        }

        sequenceDisplayInterval = Mathf.Max(0.3f, sequenceDisplayInterval - 0.05f);
        StartCoroutine(PlayRoundRoutine());
    }

    private void LoseGame()
    {
        state = GameState.Lost;
        StopAllCoroutines();

        // Save final score to Firebase
        if (FirebaseManager.Instance != null && score > 0)
        {
            FirebaseManager.Instance.SaveScoreIfHigher(score,
                onSuccess: () =>
                {
                    FirebaseManager.Instance.SaveScoreToLeaderboard(
                        FirebaseManager.Instance.Username, score);
                },
                onFailure: (error) => Debug.LogWarning($"Failed to save final score: {error}")
            );
        }

        if (instructionsText != null)
        {
            instructionsText.text = string.Empty;
        }

        if (lostText != null)
        {
            lostText.gameObject.SetActive(true);
            lostText.text = $"¡Perdiste!\nPuntuación: {score}";
        }

        StartCoroutine(GoToLeaderboardRoutine());
    }

    private IEnumerator GoToLeaderboardRoutine()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Leaderboard");
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }
}