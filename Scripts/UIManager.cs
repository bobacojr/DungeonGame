using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;

    [Header("UI Elements")]
    public Button startButton;

    public static UIManager Instance;

    public TextMeshProUGUI waveText;
    public TextMeshProUGUI enemyCountText;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void UpdateWave(int wave)
    {
        waveText.text = $"Wave: {wave}";
    }

    public void UpdateEnemyCount(int count)
    {
        enemyCountText.text = $"Enemies Left: {count}";
    }

    void Start()
    {

        startPanel.SetActive(true);
        startButton.gameObject.SetActive(true);

        startButton.onClick.AddListener(OnStartClicked);
    }

    public void HideStartScreen()
    {
        startPanel.SetActive(false);
    }

    public void ShowStartScreen()
    {
        startPanel.SetActive(true);
        startButton.gameObject.SetActive(true);
    }

    public void OnStartClicked()
    {
        Debug.Log("hello");
        HideStartScreen();
        Generator.Instance.BeginGame();
        UpdateWave(1);
    }
}
