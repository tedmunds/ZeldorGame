using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerController))]
public class PlayerGUI : MonoBehaviour {

    // Data for a single kicker event
    protected class KickerEvent {
        public Color col;       // what color is it
        public string text;     // what text is it
        public float lifeTime;  // how long until it is removed
        public Vector3 loc;     // world location
        public Vector2 screenLoc; // Screen location
    }

    // constant section
    private const float KICKER_LIFESPAN = 1.0f;
    private const float KICKER_SPEED = 3.0f;
    private const float KICKER_VERT_OFFSET = 0.5f;

    [SerializeField]
    private Font baseFont;

    [SerializeField]
    private Texture screenFlashTexture;

    PlayerController owner;

    // Game state info
    private int numLives;
    private int score;
    private int highScore;
    private float gameTime;

    private Rect baseRect;
    private GUIStyle mainStyle;
    
	private Rect scoreRect;
    private Rect livesRect;
    private Rect highScoreRect;
    private Rect gameTimeRect;

    private float screenFlashLength;
    private float startScreenFlashTime;
    private bool bDoScreenCover;

    // Kicker system vars
    private List<KickerEvent> kickers;

    private Camera mainCamera;

	void Start () {
	    owner = GetComponent<PlayerController>();
        mainStyle = new GUIStyle();

        kickers = new List<KickerEvent>();

        mainCamera = FindObjectOfType<Camera>();

        float w = 250.0f;
        float h = 50.0f;
        float fromTop = 50.0f;

        baseRect = new Rect(Screen.width / 2.0f - w / 2.0f, fromTop, w, h);

        scoreRect = new Rect(baseRect.x+ 10.0f, baseRect.y + 5.0f, baseRect.width, baseRect.height);
        livesRect = new Rect(baseRect.x + w / 2.0f + 10.0f, baseRect.y + 5.0f, baseRect.width, baseRect.height);
        highScoreRect = new Rect(25.0f, fromTop + 5.0f, baseRect.width, baseRect.height);
        gameTimeRect = new Rect(25.0f, highScoreRect.y + 25.0f, baseRect.width, baseRect.height);
	}
	
	
	void Update () {
        numLives = owner.NumHitsLeft();
        score = owner.GetPoints();
        highScore = owner.GetHighScore();
        gameTime = owner.GetLongestGameTime();

        // Update all kickers, reverse iteration so that they can be removed
        for(int i = kickers.Count - 1; i >= 0; i--) {
            // Velocity magnitude gets lower as the kicker approaches end of its life time
            float velMag = kickers[i].lifeTime / KICKER_LIFESPAN;

            kickers[i].loc.y += velMag * KICKER_SPEED * Time.deltaTime;

            // We keep the kicker moving in world space so that it doesnt float wierdly with the screen
            kickers[i].screenLoc = mainCamera.WorldToScreenPoint(kickers[i].loc);
            kickers[i].screenLoc.y = Screen.height - kickers[i].screenLoc.y;

            kickers[i].lifeTime -= Time.deltaTime;
            if(kickers[i].lifeTime <= 0.0f) {
                kickers.RemoveAt(i);
            }
        }

        if(bDoScreenCover) {
            float flashTime = Time.time - startScreenFlashTime;
            if(flashTime > screenFlashLength) {
                bDoScreenCover = false;
            }
        }
	}


    void OnGUI() {
        mainStyle.font = baseFont;
        mainStyle.normal.textColor = Color.white;
        mainStyle.fontSize = 24;

        GUI.Label(scoreRect, "Score: "+score, mainStyle);
        GUI.Label(livesRect, "Lives: " + numLives, mainStyle);
        GUI.Label(highScoreRect, "High Score: " + highScore, mainStyle);
        GUI.Label(gameTimeRect, "Longest Game: " + FormatTimeString(gameTime), mainStyle);

        // Draw all active kickers
        foreach(KickerEvent kevent in kickers) {
            mainStyle.fontSize = 18;
            mainStyle.normal.textColor = kevent.col;

            Rect kickerRect = new Rect(kevent.screenLoc.x, kevent.screenLoc.y, 50.0f, 25.0f);
            GUI.Label(kickerRect, kevent.text, mainStyle);
        }

        mainStyle.normal.textColor = Color.white;

        // Draw a full screen cover
        if(bDoScreenCover) {
            Rect fullScreenRect = new Rect(0.0f, 0.0f, Screen.width, Screen.height);
            GUI.DrawTexture(fullScreenRect, screenFlashTexture);
        }
    }


    // Interface to the kicker number system: Must come off of an actor of any sort
    public void AddKickerNumber(Transform source, int displayVal) {
        KickerEvent newKicker = new KickerEvent();
        newKicker.col = Color.white;
        newKicker.text = "" + displayVal;
        newKicker.lifeTime = KICKER_LIFESPAN;
        newKicker.loc = source.position;
        newKicker.loc.y += KICKER_VERT_OFFSET;

        kickers.Add(newKicker);
    }


    // Starts a sreen flash primarily for when player takes damage
    public void DoScreenFlash(float flashTime) {
        startScreenFlashTime = Time.time;
        screenFlashLength = flashTime;
        bDoScreenCover = true;
    }



    public string FormatTimeString(float seconds) {
        return string.Format("{0}:{1:00}", (int)seconds / 60, (int)seconds % 60);
    }

}
