using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingController : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;      // assign in Inspector
    public Slider progressBar;      
    public Text   progressText;

    [Header("Next Scene")]
    public string sceneToLoad = "MainGameScene";

    void Awake()
    {
        // hide the progress UI until the player clicks
        progressBar.gameObject.SetActive(false);
        progressText.gameObject.SetActive(false);

        // wire up the button
        startButton.onClick.AddListener(OnStartButtonPressed);
    }

    void OnStartButtonPressed()
    {
        // disable the button so you can't click it again
        startButton.gameObject.SetActive(false);

        // show progress UI
        progressBar.gameObject.SetActive(true);
        progressText.gameObject.SetActive(true);

        // start the async load
        StartCoroutine(LoadAsync());
    }

    IEnumerator LoadAsync()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            progressBar.value = p;
            progressText.text = $"{(int)(p * 100)}%";
            yield return null;
        }

        progressBar.value = 1f;
        progressText.text  = "100%";
        yield return new WaitForSeconds(0.5f);

        op.allowSceneActivation = true;
    }
}
