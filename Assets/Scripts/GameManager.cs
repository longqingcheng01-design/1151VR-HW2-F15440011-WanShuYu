using System;
using UnityEngine;
using UnityEngine.UI;

public enum GameState { Playing, Paused, Won, Failed }
public class GameManager : MonoBehaviour
{
    public PlayerController player;
    public Vector2 spawn = new Vector2(-9.5f,-1.2f);
    public Transform goal;
    public SpriteRenderer checkpointMarker;
    public Text statusText, timerText, hintText, resultTitle, resultBody;
    public Font uiFont;
    public GameObject resultPanel;
    public Button restartButton, pauseButton, muteButton, resultButton;
    public AudioSource audioSource;
    public AudioClip jumpSound, dropSound, landSound, checkpointSound, winSound, failSound, clickSound;
    public Sprite particleSprite;
    public Color accent = new Color(.25f,.88f,.74f);
    public GameState State { get; private set; } = GameState.Playing;
    public float Elapsed { get; private set; }
    public bool Checkpoint { get; private set; }
    public bool Muted { get; private set; }
    public int CueCount { get; private set; }
    public event Action<string> CuePlayed;
    float notification;
    void Awake()
    {
        restartButton.onClick.AddListener(Restart);
        pauseButton.onClick.AddListener(TogglePause);
        muteButton.onClick.AddListener(ToggleMute);
        resultButton.onClick.AddListener(() => { if (State == GameState.Paused) TogglePause(); else Restart(); });
        Font.textureRebuilt += PixelFontTexture;
        PixelFontTexture(uiFont);
        resultPanel.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
        if (Input.GetKeyDown(KeyCode.M)) ToggleMute();
        AdvanceClock(Time.deltaTime);
        UpdateHud();
    }
    public void AdvanceClock(float dt) { if (State == GameState.Playing) Elapsed += dt; }
    public void CheckFinish()
    {
        if (State != GameState.Playing) return;
        if (!Checkpoint && player.Body.position.x > 4.2f && player.Body.position.y > 1.4f && player.Grounded)
        { Checkpoint = true; if(checkpointMarker)checkpointMarker.color=accent; PlayCue("checkpoint"); Emit(player.transform.position, accent, 16); }
        if (player.Grounded && player.Body.position.x > goal.position.x - .6f && player.Body.position.y < -1.1f)
        {
            if (player.JumpCount > 0 && player.DropCount > 0 && Checkpoint)
            {
                State = GameState.Won; player.Body.velocity = Vector2.zero; player.Body.simulated = false;
                PlayCue("win"); Emit(goal.position, new Color(1,.8f,.38f), 36);
                resultPanel.SetActive(true); resultTitle.text = "送達啦！"; resultButton.GetComponentInChildren<Text>().text = "再來";
                resultBody.text = "用時 " + Elapsed.ToString("0.0") + " 秒";
            }
            else notification = Time.unscaledTime + 2.5f;
        }
    }
    public void Fail()
    {
        if (State != GameState.Playing) return;
        State = GameState.Failed; player.Body.velocity = Vector2.zero; player.Body.simulated = false;
        PlayCue("fail"); resultPanel.SetActive(true); resultTitle.text = "掉下去了"; resultButton.GetComponentInChildren<Text>().text = "重來";
        resultBody.text = "重來吧";
    }
    public void Restart()
    {
        Time.timeScale = 1; State = GameState.Playing; Elapsed = 0; Checkpoint = false; notification = 0;
        player.ResetPlayer(spawn); resultPanel.SetActive(false); PlayCue("click"); UpdateHud();
        if(checkpointMarker)checkpointMarker.color=new Color(1,.79f,.4f);
    }
    public void TogglePause()
    {
        if (State == GameState.Won || State == GameState.Failed) return;
        State = State == GameState.Paused ? GameState.Playing : GameState.Paused;
        Time.timeScale = State == GameState.Paused ? 0 : 1;
        resultPanel.SetActive(State == GameState.Paused);
        resultTitle.text = "暫停中"; resultBody.text = "P 继續"; resultButton.GetComponentInChildren<Text>().text = "继續";
        PlayCue("click"); UpdateHud();
    }
    public void ToggleMute() { Muted = !Muted; audioSource.mute = Muted; UpdateHud(); }
    public void UpdateHud()
    {
        statusText.text = State == GameState.Playing ? "" : State == GameState.Paused ? "已暫停" : State == GameState.Won ? "已送達" : "再試一次";
        timerText.text = Elapsed.ToString("0.0") + "秒";
        muteButton.GetComponentInChildren<Text>().text = Muted ? "靜音" : "音效";
        pauseButton.GetComponentInChildren<Text>().text = State == GameState.Paused ? "继續" : "暫停";
        hintText.text = State == GameState.Won ? "完成配送"
            : notification > Time.unscaledTime ? "先跳躍，再下跳"
            : !Checkpoint ? "跳上平台"
            : player.DropCount == 0 ? "按 S 下跳"
            : "走到旗幟";
    }
    void PixelFontTexture(Font changedFont)
    {
        if(changedFont == uiFont && changedFont.material != null && changedFont.material.mainTexture != null)
            changedFont.material.mainTexture.filterMode = FilterMode.Point;
    }

    public void PlayCue(string cue)
    {
        AudioClip clip = cue == "jump" ? jumpSound : cue == "drop" ? dropSound : cue == "land" ? landSound
            : cue == "checkpoint" ? checkpointSound : cue == "win" ? winSound : cue == "fail" ? failSound : clickSound;
        if (clip != null) { audioSource.PlayOneShot(clip,.65f); CueCount++; CuePlayed?.Invoke(cue); }
    }
    public void Emit(Vector3 position, Color color, int count)
    {
        // Small original sprite particles; all temporary effects clean themselves up.
        for (int i=0; i<count; i++)
        {
            GameObject dot = new GameObject("Dust"); dot.transform.position = position;
            SpriteRenderer renderer = dot.AddComponent<SpriteRenderer>(); renderer.sprite = particleSprite;
            renderer.color = color; renderer.sortingOrder = 15; dot.transform.localScale = Vector3.one * UnityEngine.Random.Range(.08f,.15f);
            DustParticle dust = dot.AddComponent<DustParticle>();
            dust.velocity = new Vector2(UnityEngine.Random.Range(-2.5f,2.5f),UnityEngine.Random.Range(.6f,3f));
        }
    }
    void OnDestroy() { Font.textureRebuilt -= PixelFontTexture; Time.timeScale = 1; }
}
