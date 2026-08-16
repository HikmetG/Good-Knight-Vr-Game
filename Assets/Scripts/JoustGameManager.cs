using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;
using TMPro; 
using UnityEngine.SceneManagement; 

public enum JoustState { MainMenu, BeforeCharge, Countdown, Charging, Resetting, Paused, GameOver }

public class JoustGameManager : MonoBehaviour
{
    public static JoustGameManager Instance { get; private set; }

    [Header("VR Input Action Binding")]
    [SerializeField] private InputAction startChargeAction; 
    [SerializeField] private InputAction pauseAction; 

    [Header("Tournament State Tracker")]
    public JoustState currentState = JoustState.MainMenu; 
    public int currentRound = 1;
    [SerializeField] private int maxRoundsInMatch = 6; 
    
    [HideInInspector] public bool playerIsAtGate = false; 

    [Header("Actor Connection")]
    [SerializeField] private JoustingAI aiHorse; 

    [Header("Gate Highlights")]
    [SerializeField] private JoustReadyZone forwardGateZone;
    [SerializeField] private JoustReadyZone returnGateZone;

    [Header("VR HUD Connections")]
    [SerializeField] private TextMeshProUGUI hudStatusText;
    [SerializeField] private TextMeshProUGUI hudRoundText;

    [Header("Panel Visibility System")]
    [SerializeField] private GameObject panelMainMenu;   
    [SerializeField] private GameObject panelPauseMenu;  
    [SerializeField] private GameObject panelGameplayHUD; 
    [SerializeField] private GameObject panelGameOver;   

    [Header("Audio Settings")]
    [Tooltip("Drag the AudioSource component carrying your main menu music loop here.")]
    [SerializeField] private AudioSource mainMenuMusicSource;
    [Tooltip("How long (in seconds) the music should take to fade away smoothly.")]
    [SerializeField] private float musicFadeDuration = 2.0f;

    [Space]
    [Tooltip("Drag the second separate AudioSource here to play win/loss music.")]
    [SerializeField] private AudioSource endMatchMusicSource;
    [SerializeField] private AudioClip victoryMusicClip;
    [SerializeField] private AudioClip defeatMusicClip;

    [Header("Arena Ambiance Settings")]
    [Tooltip("Drag your third separate AudioSource component carrying the Arena Ambience loop here.")]
    [SerializeField] private AudioSource arenaAmbianceSource;
    [Tooltip("The lowered volume level (0.0 to 1.0) the ambiance drops to on Game Over so win/loss music is clearly heard.")]
    [SerializeField] private float ambianceGameOverVolume = 0.2f;

    private bool isInputLockedOnStartup = true;
    private JoustState stateBeforePause; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (startChargeAction != null)
        {
            startChargeAction.Enable();
            startChargeAction.performed += OnVRReadyPressed;
        }

        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPauseTogglePressed;
        }
    }

    private void OnDisable()
    {
        if (startChargeAction != null) startChargeAction.Disable();
        if (pauseAction != null) pauseAction.Disable();
    }

    private void OnVRReadyPressed(InputAction.CallbackContext context)
    {
        TryPlayerReadySignal();
    }

    private void OnPauseTogglePressed(InputAction.CallbackContext context)
    {
        if (currentState == JoustState.MainMenu || currentState == JoustState.GameOver) return;

        if (currentState == JoustState.Paused) ResumeMatch();
        else PauseMatch();
    }

    void Start()
    {
        ConfigureUIPanels(true, false, false, false);
        Time.timeScale = 1f; 
        currentState = JoustState.MainMenu;

        // Ensure the menu music starts playing loop cycles instantly on startup
        if (mainMenuMusicSource != null)
        {
            mainMenuMusicSource.loop = true;
            mainMenuMusicSource.volume = 1f; 
            mainMenuMusicSource.Play();
        }

        // Keep ambient arena loops silent on the main menu screen
        if (arenaAmbianceSource != null)
        {
            arenaAmbianceSource.Stop();
        }
    }

    public void StartChampionshipFromMenu()
    {
        Time.timeScale = 1f; 
        ConfigureUIPanels(false, false, true, false); 
        
        // Starts the smooth audio fade controller instead of turning it off instantly
        if (mainMenuMusicSource != null && mainMenuMusicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusicRoutine());
        }

        // Start the arena background ambience at full blast when the game loop launches!
        if (arenaAmbianceSource != null)
        {
            arenaAmbianceSource.loop = true;
            arenaAmbianceSource.volume = 1.0f;
            arenaAmbianceSource.Play();
        }

        PrepareNextRound();
        StartCoroutine(StartupSafetyWindowRoutine());
    }

    // Gradually fades the menu audio source out frame-by-frame
    private IEnumerator FadeOutMusicRoutine()
    {
        float startVolume = mainMenuMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < musicFadeDuration)
        {
            elapsed += Time.deltaTime;
            mainMenuMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeDuration);
            yield return null; 
        }

        mainMenuMusicSource.Stop();
        
        // Reset the default volume field back to full so it plays normally on the next scene reload/restart!
        mainMenuMusicSource.volume = startVolume; 
    }

    // Gradually dims the background arena crowd sounds so end-game music can take over
    private IEnumerator FadeAmbianceToGameOverRoutine()
    {
        if (arenaAmbianceSource == null) yield break;

        float startVolume = arenaAmbianceSource.volume;
        float elapsed = 0f;
        float fadeDuration = 1.5f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            arenaAmbianceSource.volume = Mathf.Lerp(startVolume, ambianceGameOverVolume, elapsed / fadeDuration);
            yield return null;
        }

        arenaAmbianceSource.volume = ambianceGameOverVolume;
    }

    private IEnumerator StartupSafetyWindowRoutine()
    {
        isInputLockedOnStartup = true;
        yield return new WaitForSeconds(2f); 
        isInputLockedOnStartup = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryPlayerReadySignal();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (currentState == JoustState.MainMenu || currentState == JoustState.GameOver) return;
            if (currentState == JoustState.Paused) ResumeMatch();
            else PauseMatch();
        }
    }

    public void PauseMatch()
    {
        stateBeforePause = currentState;
        currentState = JoustState.Paused;

        // Keeps engine clocks running so VR Gaze components can process selections,
        // but manually freezes physical movement tracking logic instantly.
        Time.timeScale = 1f; 
        Physics.autoSimulation = false; 
        ConfigureUIPanels(false, true, false, false);
    }

    public void ResumeMatch()
    {
        if (currentState != JoustState.Paused) return;

        currentState = stateBeforePause;
        Time.timeScale = 1f; 
        Physics.autoSimulation = true; 

        ConfigureUIPanels(false, false, true, false);
    }

    public void TryPlayerReadySignal()
    {
        if (currentState == JoustState.Paused || currentState == JoustState.MainMenu) return;

        if (currentState == JoustState.GameOver)
        {
            RestartWholeMatch();
            return;
        }

        if (isInputLockedOnStartup) return;
        if (currentState != JoustState.BeforeCharge) return;

        if (!playerIsAtGate)
        {
            UpdateHUDText("RIDE TO THE GLOWING START GATE!", $"ROUND {currentRound} / {maxRoundsInMatch}");
            return;
        }

        StartCoroutine(StartCountdownRoutine());
    }

    private IEnumerator StartCountdownRoutine()
    {
        currentState = JoustState.Countdown;
        UpdateHUDText("3", "PREPARE YOUR LANCE");
        yield return new WaitForSeconds(1f);
        UpdateHUDText("2", "BRACE FOR IMPACT");
        yield return new WaitForSeconds(1f);
        UpdateHUDText("1", "CHARGE");
        yield return new WaitForSeconds(1f);

        TriggerTheCharge();
    }

    private void TriggerTheCharge()
    {
        currentState = JoustState.Charging;
        UpdateHUDText("", "");

        if (forwardGateZone != null) forwardGateZone.SetHighlightActive(false);
        if (returnGateZone != null) returnGateZone.SetHighlightActive(false);

        JoustLance[] allLances = Object.FindObjectsByType<JoustLance>(FindObjectsSortMode.None);
        foreach (JoustLance lance in allLances)
        {
            if (lance != null) lance.ResetLanceFlag();
        }

        if (aiHorse != null)
        {
            aiHorse.StartJoustCharge(currentRound);
        }
    }

    public void ProcessKnockout(bool didPlayerLose)
    {
        currentState = JoustState.GameOver;
        ConfigureUIPanels(false, false, true, true); 

        if (aiHorse != null)
        {
            aiHorse.StopAllCoroutines(); 
        }

        // Dim the background crowd ambience smoothly so the win/loss music pops
        StartCoroutine(FadeAmbianceToGameOverRoutine());

        // Play Victory or Defeat music loop
        if (endMatchMusicSource != null)
        {
            endMatchMusicSource.loop = true; 
            
            if (didPlayerLose)
            {
                if (defeatMusicClip != null)
                {
                    endMatchMusicSource.clip = defeatMusicClip;
                    endMatchMusicSource.Play();
                }
            }
            else
            {
                if (victoryMusicClip != null)
                {
                    endMatchMusicSource.clip = victoryMusicClip;
                    endMatchMusicSource.Play();
                }
            }
        }

        if (didPlayerLose)
        {
            UpdateHUDText("DEFEAT - Good Night", "PRESS VR Y TO RESTART");
        }
        else
        {
            UpdateHUDText("VICTORY - Good Knight", "PRESS Y TO PLAY AGAIN");
        }
    }

    public void RestartWholeMatch()
    {
        Time.timeScale = 1f; 
        Physics.autoSimulation = true; 
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void NotifyAIArrival()
    {
        if (currentState == JoustState.GameOver) return;

        currentState = JoustState.Resetting;
        UpdateHUDText("PASS COMPLETE", "RESETTING ARENA...");
        playerIsAtGate = false; 

        if (currentRound < maxRoundsInMatch)
        {
            if (currentRound % 2 != 0) 
            {
                if (aiHorse != null) aiHorse.PositionForReturnRound();
            }
            else
            {
                if (aiHorse != null) aiHorse.PositionForForwardRound();
            }
            
            currentRound++;
            PrepareNextRound();
        }
        else
        {
            // Tournament matches have ended without knockouts
            currentState = JoustState.GameOver;
            ConfigureUIPanels(false, false, true, true);

            // Dim the background crowd ambience smoothly on points evaluation end-match state
            StartCoroutine(FadeAmbianceToGameOverRoutine());

            float playerFinalHP = 0f;
            float aiFinalHP = 0f;

            // Find both health tracker scripts in our active hierarchy
            JoustHealth[] healthScripts = Object.FindObjectsByType<JoustHealth>(FindObjectsSortMode.None);
            foreach (JoustHealth hp in healthScripts)
            {
                if (hp != null)
                {
                    if (hp.IsPlayer()) playerFinalHP = hp.GetCurrentHealth();
                    else aiFinalHP = hp.GetCurrentHealth();
                }
            }

            // Compare final health percentages to crown the points victor
            if (playerFinalHP > aiFinalHP)
            {
                if (endMatchMusicSource != null && victoryMusicClip != null)
                {
                    endMatchMusicSource.loop = true;
                    endMatchMusicSource.clip = victoryMusicClip;
                    endMatchMusicSource.Play();
                }
                UpdateHUDText("TOURNAMENT CHAMPION", "VICTORY BY POINTS! PRESS Y TO RESTART");
            }
            else if (aiFinalHP > playerFinalHP)
            {
                if (endMatchMusicSource != null && defeatMusicClip != null)
                {
                    endMatchMusicSource.loop = true;
                    endMatchMusicSource.clip = defeatMusicClip;
                    endMatchMusicSource.Play();
                }
                UpdateHUDText("CHAMPIONSHIP DEFEAT", "THE AI HAD MORE HEALTH! PRESS Y TO RESTART");
            }
            else
            {
                if (endMatchMusicSource != null && defeatMusicClip != null)
                {
                    endMatchMusicSource.loop = true;
                    endMatchMusicSource.clip = defeatMusicClip; 
                    endMatchMusicSource.Play();
                }
                UpdateHUDText("DRAW MATCH", "AN EQUAL JOUST! PRESS Y TO TRY AGAIN");
            }
        }
    }

    private void PrepareNextRound()
    {
        currentState = JoustState.BeforeCharge;
        string instruction = (currentRound % 2 != 0) ? "RIDE TO FORWARD GATE" : "RIDE TO RETURN GATE";
        UpdateHUDText("AWAITING READY SIGNAL (Y on Left Controller)", instruction);

        bool isOddRound = (currentRound % 2 != 0);
        if (forwardGateZone != null) forwardGateZone.SetHighlightActive(isOddRound);
        if (returnGateZone != null) returnGateZone.SetHighlightActive(!isOddRound);
    }

    private void UpdateHUDText(string status, string roundInfo)
    {
        if (hudStatusText != null) hudStatusText.text = status;
        if (hudRoundText != null) hudRoundText.text = roundInfo;
    }

    private void ConfigureUIPanels(bool menu, bool pause, bool gameplay, bool over)
    {
        if (panelMainMenu != null) panelMainMenu.SetActive(menu);
        if (panelPauseMenu != null) panelPauseMenu.SetActive(pause);
        if (panelGameplayHUD != null) panelGameplayHUD.SetActive(gameplay);
        if (panelGameOver != null) panelGameOver.SetActive(over);
    }
}