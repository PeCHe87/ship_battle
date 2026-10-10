using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchEndUI : MonoBehaviour
{
    const float DeathDelaySeconds = 1f;
    const int RestartCountdownSeconds = 10;

    [SerializeField] TMP_Text winnerText;
    [SerializeField] TMP_Text restartText;

    GameObject _panelRoot;
    bool _matchEnded;
    ShipHealth[] _ships;

    void Start()
    {
        EnsureUi();
        SetVisible(false);

        _ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < _ships.Length; i++)
            _ships[i].Destroyed += OnShipDestroyed;
    }

    void OnDestroy()
    {
        if (_ships == null)
            return;

        for (int i = 0; i < _ships.Length; i++)
        {
            if (_ships[i] != null)
                _ships[i].Destroyed -= OnShipDestroyed;
        }
    }

    void OnShipDestroyed(ShipHealth destroyedShip)
    {
        if (_matchEnded)
            return;

        _matchEnded = true;
        StartCoroutine(EndSequence(FindWinnerShipId(destroyedShip)));
    }

    int FindWinnerShipId(ShipHealth destroyedShip)
    {
        for (int i = 0; i < _ships.Length; i++)
        {
            ShipHealth ship = _ships[i];
            if (ship == null || ship == destroyedShip || ship.IsDestroyed)
                continue;

            ShipIdentity identity = ship.GetComponent<ShipIdentity>();
            if (identity != null)
                return identity.Id;
        }

        ShipIdentity destroyedIdentity = destroyedShip.GetComponent<ShipIdentity>();
        if (destroyedIdentity != null)
            return destroyedIdentity.Id == 1 ? 2 : 1;

        return 0;
    }

    IEnumerator EndSequence(int winnerShipId)
    {
        yield return new WaitForSeconds(DeathDelaySeconds);

        SetVisible(true);
        if (winnerText != null)
            winnerText.text = $"Team {TeamLabel(winnerShipId)} Wins!";

        for (int seconds = RestartCountdownSeconds; seconds >= 1; seconds--)
        {
            if (restartText != null)
                restartText.text = $"Restart in {seconds}...";
            yield return new WaitForSeconds(1f);
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static string TeamLabel(int shipId)
    {
        if (shipId >= 1 && shipId <= 26)
            return ((char)('A' + shipId - 1)).ToString();

        return shipId.ToString();
    }

    void SetVisible(bool visible)
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(visible);
            return;
        }

        if (winnerText != null)
            winnerText.gameObject.SetActive(visible);
        if (restartText != null)
            restartText.gameObject.SetActive(visible);
    }

    void EnsureUi()
    {
        if (winnerText != null && restartText != null)
            return;

        RectTransform canvasRoot = transform as RectTransform;
        if (canvasRoot == null)
            canvasRoot = GetComponentInParent<Canvas>()?.transform as RectTransform;
        if (canvasRoot == null)
            return;

        _panelRoot = new GameObject("MatchEndPanel", typeof(RectTransform));
        RectTransform panel = _panelRoot.GetComponent<RectTransform>();
        panel.SetParent(canvasRoot, false);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        Image dim = _panelRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.45f);
        dim.raycastTarget = false;

        if (winnerText == null)
            winnerText = CreateCenteredText(panel, "WinnerText", 96f, 80f);

        if (restartText == null)
            restartText = CreateCenteredText(panel, "RestartText", 48f, -40f);
    }

    static TMP_Text CreateCenteredText(RectTransform parent, string name, float fontSize, float anchoredY)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, anchoredY);
        rect.sizeDelta = new Vector2(1400f, 160f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
