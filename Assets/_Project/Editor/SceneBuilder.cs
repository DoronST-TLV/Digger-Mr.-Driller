using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Strata.EditorTools
{
    /// <summary>
    /// Builds the config asset, the prefabs and Game.unity from code, so the whole scene wiring is
    /// reproducible and reviewable. Safe to run again: it overwrites the prefabs and the scene, keeps the config.
    /// Menu: Strata → Build Game Scene.   Batch: -executeMethod Strata.EditorTools.SceneBuilder.Build
    /// </summary>
    public static class SceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ArtDir = Root + "/Art";
        private const string AudioDir = Root + "/Audio";
        private const string PrefabDir = Root + "/Prefabs";
        private const string ConfigDir = Root + "/Config";
        private const string SceneDir = Root + "/Scenes";
        private const string ScenePath = SceneDir + "/Game.unity";
        private const string ConfigPath = ConfigDir + "/GameConfig.asset";

        private static readonly Color Ink = new Color(0.13f, 0.15f, 0.18f);
        private static readonly Color Paper = new Color(0.95f, 0.94f, 0.91f);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color Cyan = new Color(0.18f, 0.90f, 0.84f);
        private static readonly Color Red = new Color(0.90f, 0.28f, 0.30f);
        private static readonly Color Blue = new Color(0.24f, 0.48f, 0.98f);

        [MenuItem("Strata/Build Game Scene")]
        public static void Build()
        {
            EnsureFolders();
            ImportArt();
            GameConfig config = GetOrCreateConfig();
            GameObject blockPrefab = BuildBlockPrefab();
            GameObject playerPrefab = BuildPlayerPrefab();
            GameObject burstPrefab = BuildBurstPrefab();
            BuildScene(config, blockPrefab, playerPrefab, burstPrefab);
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Strata] Game scene built at " + ScenePath);
        }

        // ------------------------------------------------------------------ assets

        private static void EnsureFolders()
        {
            foreach (string dir in new[] { ArtDir, AudioDir, PrefabDir, ConfigDir, SceneDir })
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            AssetDatabase.Refresh();
        }

        /// <summary>Force our import rules even if the postprocessor was not compiled yet when the PNGs first imported.</summary>
        private static void ImportArt()
        {
            foreach (string path in Directory.GetFiles(ArtDir, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = assetPath.EndsWith("background.png") ? 30 : 120;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");
            if (sprite == null) Debug.LogError($"[Strata] Missing sprite {ArtDir}/{name}.png");
            return sprite;
        }

        private static AudioClip LoadClip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{name}.wav");
            if (clip == null) Debug.LogWarning($"[Strata] Missing audio clip {AudioDir}/{name}.wav");
            return clip;
        }

        private static GameConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static GameObject BuildBlockPrefab()
        {
            var go = new GameObject("Block");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("block");
            sr.sortingOrder = 0;
            var block = go.AddComponent<Block>();
            SetRef(block, "spriteRenderer", sr);
            return SavePrefab(go, PrefabDir + "/Block.prefab");
        }

        private static GameObject BuildPlayerPrefab()
        {
            var go = new GameObject("Player");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("player");
            sr.sortingOrder = 10;
            var player = go.AddComponent<PlayerController>();
            SetRef(player, "spriteRenderer", sr);
            return SavePrefab(go, PrefabDir + "/Player.prefab");
        }

        private static GameObject BuildBurstPrefab()
        {
            var go = new GameObject("ClearBurst");
            var ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.45f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.5f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)14) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            renderer.sortingOrder = 20;
            return SavePrefab(go, PrefabDir + "/ClearBurst.prefab");
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ------------------------------------------------------------------ scene

        private static void BuildScene(GameConfig config, GameObject blockPrefab, GameObject playerPrefab, GameObject burstPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // camera rig → camera (+ shake) → background
            var rig = new GameObject("CameraRig");
            rig.transform.position = new Vector3(0f, -3f, 0f);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.078f, 0.094f, 0.129f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();
            var shake = camGo.AddComponent<ScreenShake>();
            var background = new GameObject("Background");
            background.transform.SetParent(camGo.transform, false);
            background.transform.localPosition = new Vector3(0f, 0f, 20f);
            background.transform.localScale = new Vector3(4f, 3f, 1f);
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = LoadSprite("background");
            backgroundRenderer.sortingOrder = -10;

            // systems
            var systems = new GameObject("Systems");
            var gameManager = systems.AddComponent<GameManager>();
            var grid = systems.AddComponent<GridManager>();
            var generator = systems.AddComponent<LevelGenerator>();
            var air = systems.AddComponent<AirMeter>();
            var input = systems.AddComponent<InputHandler>();
            var juice = systems.AddComponent<JuiceController>();

            var poolGo = new GameObject("BlockPool");
            var pool = poolGo.AddComponent<BlockPool>();
            var blocksContainer = new GameObject("Blocks");
            blocksContainer.transform.SetParent(poolGo.transform, false);

            var burstGo = new GameObject("BurstPool");
            var bursts = burstGo.AddComponent<BurstPool>();
            var burstsContainer = new GameObject("Bursts");
            burstsContainer.transform.SetParent(burstGo.transform, false);

            var audioGo = new GameObject("AudioManager");
            var audio = audioGo.AddComponent<AudioManager>();
            var source = audioGo.AddComponent<AudioSource>();
            source.playOnAwake = false;

            // player
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            var playerController = player.GetComponent<PlayerController>();

            var follow = rig.AddComponent<CameraFollow>();

            // UI
            UIManager ui = BuildUI();

            // wiring
            SetRef(gameManager, "config", config);
            SetRef(gameManager, "grid", grid);
            SetRef(gameManager, "player", playerController);
            SetRef(gameManager, "air", air);

            SetRef(grid, "config", config);
            SetRef(grid, "generator", generator);
            SetRef(grid, "player", playerController);
            SetRef(grid, "gameCamera", cam);
            SetRef(grid, "colorBlockSprite", LoadSprite("block"));
            SetRef(grid, "hardBlockSprite", LoadSprite("hard"));
            SetRef(grid, "capsuleSprite", LoadSprite("capsule"));

            SetRef(generator, "config", config);
            SetRef(air, "config", config);

            SetRef(input, "player", playerController);
            SetRef(input, "gameCamera", cam);

            SetRef(playerController, "config", config);
            SetRef(playerController, "grid", grid);
            SetRef(playerController, "air", air);

            SetRef(pool, "prefab", blockPrefab.GetComponent<Block>());
            SetRef(pool, "container", blocksContainer.transform);

            SetRef(bursts, "prefab", burstPrefab.GetComponent<ParticleSystem>());
            SetRef(bursts, "container", burstsContainer.transform);

            SetRef(audio, "source", source);
            SetClips(audio, "dig", "land", "clear", "crush", "capsule", "gameover");

            SetRef(follow, "config", config);
            SetRef(follow, "target", player.transform);
            SetRef(follow, "cam", cam);

            SetRef(juice, "config", config);
            SetRef(juice, "grid", grid);
            SetRef(juice, "player", playerController);
            SetRef(juice, "gameManager", gameManager);
            SetRef(juice, "screenShake", shake);

            SetRef(ui, "config", config);
            SetRef(ui, "gameManager", gameManager);
            SetRef(ui, "grid", grid);
            SetRef(ui, "air", air);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ------------------------------------------------------------------ UI

        private static UIManager BuildUI()
        {
            var canvasGo = new GameObject("UICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            RectTransform safeArea = MakeRect("SafeArea", canvasGo.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeArea>();

            // HUD (no background image, so taps reach the grid)
            RectTransform hud = MakeRect("HudPanel", safeArea);
            Stretch(hud);
            Text depth = MakeText("DepthText", hud, "0 m", 64, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(500f, 80f), Paper);
            Text score = MakeText("ScoreText", hud, "0", 64, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(500f, 80f), Paper);
            RectTransform barBackground = MakeRect("AirBarBackground", hud);
            barBackground.anchorMin = new Vector2(0f, 1f);
            barBackground.anchorMax = new Vector2(1f, 1f);
            barBackground.pivot = new Vector2(0.5f, 1f);
            barBackground.anchoredPosition = new Vector2(0f, -140f);
            barBackground.sizeDelta = new Vector2(-80f, 30f);
            var barBackgroundImage = barBackground.gameObject.AddComponent<Image>();
            barBackgroundImage.color = new Color(0.16f, 0.19f, 0.25f);
            barBackgroundImage.raycastTarget = false;
            RectTransform bar = MakeRect("AirBar", barBackground);
            Stretch(bar);
            var barImage = bar.gameObject.AddComponent<Image>();
            barImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            barImage.type = Image.Type.Filled;
            barImage.fillMethod = Image.FillMethod.Horizontal;
            barImage.fillOrigin = 0;
            barImage.fillAmount = 1f;
            barImage.color = Cyan;
            barImage.raycastTarget = false;
            Text chain = MakeText("ChainText", hud, "x2", 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(400f, 100f), Cyan);
            chain.gameObject.SetActive(false);

            // Title
            RectTransform title = MakePanel("TitlePanel", safeArea, Dim);
            MakeText("TitleText", title, "STRATA", 170, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 320f), new Vector2(1000f, 220f), Paper);
            MakeText("TaglineText", title, "dig down. dodge. chain.", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1000f, 80f), Cyan);
            MakeText("TapText", title, "TAP TO DIG", 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1000f, 100f), Paper);
            Text titleBest = MakeText("BestText", title, "BEST 0 m", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -240f), new Vector2(1000f, 80f), Paper);
            MakeText("HintText", title, "arrows / WASD on PC  ·  tap left, right or below the digger on phone", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(1000f, 60f), new Color(0.7f, 0.73f, 0.8f));

            // Pause
            RectTransform pause = MakePanel("PausePanel", safeArea, Dim);
            MakeText("PausedText", pause, "PAUSED", 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000f, 160f), Paper);
            Button resume = MakeButton("ResumeButton", pause, "RESUME", new Vector2(0f, 60f));
            Button restart = MakeButton("RestartButton", pause, "RESTART", new Vector2(0f, -80f));
            Button mute = MakeButton("MuteButton", pause, "SOUND: ON", new Vector2(0f, -220f));
            Text muteLabel = mute.GetComponentInChildren<Text>();

            // Game over
            RectTransform over = MakePanel("GameOverPanel", safeArea, Dim);
            Text cause = MakeText("CauseText", over, "CRUSHED", 120, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1000f, 160f), Red);
            Text finalDepth = MakeText("FinalDepthText", over, "DEPTH  0 m", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1000f, 80f), Paper);
            Text finalScore = MakeText("FinalScoreText", over, "SCORE  0", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1000f, 80f), Paper);
            Text finalBest = MakeText("FinalBestText", over, "BEST  0 m", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1000f, 70f), Paper);
            Text newBest = MakeText("NewBestText", over, "NEW BEST", 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(1000f, 90f), Blue);
            Text retry = MakeText("RetryText", over, "TAP TO RETRY", 56, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f), new Vector2(1000f, 80f), Paper);

            title.gameObject.SetActive(true);
            hud.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            over.gameObject.SetActive(false);

            var ui = canvasGo.AddComponent<UIManager>();
            SetRef(ui, "titlePanel", title.gameObject);
            SetRef(ui, "hudPanel", hud.gameObject);
            SetRef(ui, "pausePanel", pause.gameObject);
            SetRef(ui, "gameOverPanel", over.gameObject);
            SetRef(ui, "titleBestText", titleBest);
            SetRef(ui, "depthText", depth);
            SetRef(ui, "scoreText", score);
            SetRef(ui, "chainText", chain);
            SetRef(ui, "airBar", barImage);
            SetRef(ui, "resumeButton", resume);
            SetRef(ui, "restartButton", restart);
            SetRef(ui, "muteButton", mute);
            SetRef(ui, "muteLabel", muteLabel);
            SetRef(ui, "causeText", cause);
            SetRef(ui, "finalDepthText", finalDepth);
            SetRef(ui, "finalScoreText", finalScore);
            SetRef(ui, "finalBestText", finalBest);
            SetRef(ui, "newBestText", newBest);
            SetRef(ui, "retryText", retry);
            return ui;
        }

        private static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static RectTransform MakeRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Full-screen panel with a dim background. raycastTarget is off so a tap on the title still starts the game.</summary>
        private static RectTransform MakePanel(string name, Transform parent, Color color)
        {
            RectTransform rt = MakeRect(name, parent);
            Stretch(rt);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        private static Text MakeText(string name, Transform parent, string content, int size, TextAnchor alignment,
            Vector2 anchor, Vector2 position, Vector2 sizeDelta, Color color)
        {
            RectTransform rt = MakeRect(name, parent);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = sizeDelta;
            var text = rt.gameObject.AddComponent<Text>();
            text.font = UiFont;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.text = content;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button MakeButton(string name, Transform parent, string label, Vector2 position)
        {
            RectTransform rt = MakeRect(name, parent);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(540f, 110f);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = Paper;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            MakeText("Label", rt, label, 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 110f), Ink);
            return button;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Assigns a [SerializeField] private field by name through SerializedObject.</summary>
        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Strata] {target.GetType().Name} has no serialized field '{field}'");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetClips(AudioManager audio, params string[] names)
        {
            var so = new SerializedObject(audio);
            SerializedProperty clips = so.FindProperty("clips");
            clips.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                clips.GetArrayElementAtIndex(i).objectReferenceValue = LoadClip(names[i]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Company / product / orientation / window size / Android settings (GDD §7). Also run before every build.</summary>
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "Strata";
            PlayerSettings.productName = "Strata";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.strata.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            SetActiveInputHandlingToLegacy();
        }

        /// <summary>
        /// The game reads UnityEngine.Input (legacy), which throws if Active Input Handling is "Input System Package (New)".
        /// There is no public API for that setting, so we edit ProjectSettings.asset: 0 = Input Manager (Old).
        /// </summary>
        private static void SetActiveInputHandlingToLegacy()
        {
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings == null || settings.Length == 0) return;
            var so = new SerializedObject(settings[0]);
            SerializedProperty property = so.FindProperty("activeInputHandler");
            if (property == null) return;
            if (property.intValue == 0) return;
            property.intValue = 0;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[Strata] Active Input Handling set to Input Manager (Old); takes effect after the editor restarts");
        }
    }
}
