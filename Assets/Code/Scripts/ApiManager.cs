using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ApiManager : MonoBehaviour
{
    [Header("Fake API")]
    [SerializeField] private string fakeApiUrl = "https://my-json-server.typicode.com/jacobo1304/sistemas-interactivos-dist/players";

    [Header("Jikan API")]
    private string jikanUrl = "https://api.jikan.moe/v4/characters/";
    [Header("Requests")]
    [SerializeField] private int requestMaxRetries = 3;
    [SerializeField] private float requestInitialDelay = 0.5f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI userNumberText;
    [SerializeField] private TMP_Dropdown userDropdown;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;

    private List<GameObject> spawnedCards = new List<GameObject>();

    void Start()
    {
        if (userDropdown != null)
        {
            userDropdown.onValueChanged.AddListener(OnUserDropdownChanged);
            userDropdown.SetValueWithoutNotify(0);
        }

        ChangeUser(1);
    }

    void Awake()
    {
        // Ensure URLs are normalized
        if (!string.IsNullOrEmpty(jikanUrl) && !jikanUrl.EndsWith("/"))
            jikanUrl += "/";

        if (!string.IsNullOrEmpty(fakeApiUrl) && fakeApiUrl.EndsWith("/"))
            fakeApiUrl = fakeApiUrl.TrimEnd('/'); // GetPlayer will add the slash
    }

    private void OnDestroy()
    {
        if (userDropdown != null)
        {
            userDropdown.onValueChanged.RemoveListener(OnUserDropdownChanged);
        }
    }

    private void OnUserDropdownChanged(int dropdownIndex)
    {
        int userId = dropdownIndex + 1;
        ChangeUser(userId);
    }

    public void ChangeUser(int id)
    {
        StopAllCoroutines();
        ClearCards();
        UpdateUserNumber(id);
        StartCoroutine(GetPlayer(id));
    }

    private void UpdateUserNumber(int id)
    {
        if (userNumberText != null)
        {
            userNumberText.text = id.ToString();
        }
    }

    IEnumerator GetPlayer(int playerId)
    {
        if (string.IsNullOrEmpty(fakeApiUrl))
        {
            Debug.LogError("Fake API URL is not set.");
            yield break;
        }

        string playerUrl = fakeApiUrl + "/" + playerId;

        UnityWebRequest www = null;
        yield return StartCoroutine(SendRequestWithRetries(playerUrl, (r) => www = r, requestMaxRetries, requestInitialDelay));
        if (www == null)
        {
            Debug.LogError("Error Player: request failed after retries.");
            yield break;
        }

        Player player = null;
        try
        {
            player = JsonUtility.FromJson<Player>(www.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse player JSON: " + ex.Message);
            yield break;
        }

        if (player == null)
        {
            Debug.LogError("Player data is null.");
            yield break;
        }

        if (playerNameText != null)
        {
            playerNameText.text = "Player: " + (player.name ?? "(unknown)");
        }

        if (player.cards == null)
        {
            Debug.LogWarning("Player has no cards.");
            yield break;
        }

        foreach (int cardId in player.cards)
        {
            yield return StartCoroutine(GetCharacter(cardId));
            yield return new WaitForSeconds(0.5f); // evitar rate limit
        }
    }

    IEnumerator GetCharacter(int characterId)
    {
        string url = jikanUrl + characterId;

        UnityWebRequest www = null;
        yield return StartCoroutine(SendRequestWithRetries(url, (r) => www = r, requestMaxRetries, requestInitialDelay));
        if (www == null)
        {
            Debug.LogError("Error Character: request failed after retries.");
            yield break;
        }

        CharacterResponse response = null;
        try
        {
            response = JsonUtility.FromJson<CharacterResponse>(www.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to parse character JSON: " + ex.Message);
            yield break;
        }

        if (response == null || response.data == null)
        {
            Debug.LogError("Character data is null.");
            yield break;
        }

        CreateCard(response.data);
    }

    IEnumerator SendRequestWithRetries(string url, System.Action<UnityWebRequest> onComplete, int maxAttempts = 3, float initialDelay = 0.5f)
    {
        int attempt = 0;
        float delay = initialDelay;
        UnityWebRequest lastRequest = null;

        while (attempt < maxAttempts)
        {
            attempt++;
            UnityWebRequest www = UnityWebRequest.Get(url);
            lastRequest = www;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(www);
                yield break;
            }

            Debug.LogWarning($"Request failed ({attempt}/{maxAttempts}): {www.error}. Retrying in {delay}s");
            yield return new WaitForSeconds(delay);
            delay *= 2f;
        }

        onComplete?.Invoke(null);
    }

    void CreateCard(CharacterData character)
    {
        GameObject card = Instantiate(cardPrefab, cardContainer);
        spawnedCards.Add(card);

        Transform nameTransform = card.transform.Find("NombreCarta");
        Transform imageTransform = card.transform.Find("CardImage");

        if (nameTransform == null)
        {
            Debug.LogError("Card prefab missing child 'NombreCarta'.");
            return;
        }

        TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
        if (nameText == null)
        {
            Debug.LogError("Child 'NombreCarta' is missing a TMP_Text component.");
            return;
        }

        RawImage image = null;
        if (imageTransform != null)
        {
            image = imageTransform.GetComponent<RawImage>();
        }

        // Fallback: search any RawImage component in prefab children
        if (image == null)
        {
            image = card.GetComponentInChildren<RawImage>();
            if (image == null)
            {
                Debug.LogError("Card prefab missing child 'CardImage' and no RawImage found in children.");
                return;
            }
        }

        nameText.text = character.name;
        StartCoroutine(LoadImage(character.images.jpg.image_url, image));
    }

    IEnumerator LoadImage(string url, RawImage image)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image error: " + www.error);
            yield break;
        }

        Texture texture = DownloadHandlerTexture.GetContent(www);
        image.texture = texture;
    }

    public void Salir()
    {
        Application.Quit();
    }
    void ClearCards()
    {
        foreach (GameObject card in spawnedCards)
        {
            Destroy(card);
        }
        spawnedCards.Clear();
    }
}