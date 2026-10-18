using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace HumPer.EditorTools
{
    /// <summary>
    /// Step-by-step scene setup, one idempotent menu item per step. Each step edits the OPEN scene only and never
    /// modifies the Meta Building Block objects (Camera Rig, Controller Tracking, Spatial Audio). Save the scene afterwards.
    ///
    /// Scene hierarchy (root groups, identity transforms):
    ///   XR           Meta Camera Rig + Controller Tracking building blocks
    ///   Audio        Meta Spatial Audio building blocks
    ///   Environment  Room (walls, floor, ceiling, light) and the Meta XR Audio shoebox model
    ///   UI           overlay canvases
    /// </summary>
    public static class HumPerSetup
    {
        public const string GroupXR = "XR";
        public const string GroupAudio = "Audio";
        public const string GroupEnvironment = "Environment";
        public const string GroupStimuli = "Stimuli";
        public const string GroupExperiment = "Experiment";
        public const string GroupUI = "UI";

        const string VisualBoxName = "Visual Stimulus (cardboard)";
        const string LegacyVisualBoxName = "Visual Stimulus (placeholder box)";
        const string LightingSettingsPath = "Assets/Settings/ExperimentLighting.lighting";
        const string MaterialsDir = "Assets/Materials";

        // ---- Step 1: global illumination instead of a directional light ---------------------------------------

        [MenuItem("Magic/Step 1 - Lighting: Global Illumination (remove directional light)", priority = 1)]
        public static void SetupLighting()
        {
            int removed = 0;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                Object.DestroyImmediate(light.gameObject);
                removed++;
            }

            // Soft, even light with no hard shadows: flat ambient environment lighting, baked GI (no realtime lights).
            RenderSettings.sun = null;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.7f);
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;

            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (ls == null)
            {
                ls = new LightingSettings();
                AssetDatabase.CreateAsset(ls, LightingSettingsPath);
            }
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.autoGenerate = false;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            ls.maxBounces = 4;
            ls.lightmapResolution = 20;
            ls.lightmapMaxSize = 1024;
            ls.directSampleCount = 64;
            ls.indirectSampleCount = 256;
            ls.environmentSampleCount = 128;
            EditorUtility.SetDirty(ls);
            Lightmapping.lightingSettings = ls;

            MarkSceneDirty();
            Debug.Log("[HumPer] Lighting: removed " + removed + " directional light(s); flat ambient + baked GI settings applied.");
        }

        // ---- Step 2: room (walls, floor, ceiling, light) + Meta XR Audio shoebox ------------------------------

        [MenuItem("Magic/Step 2 - Room Setup...", priority = 2)]
        public static void OpenRoomDialog() => RoomSetupDialog.Open();

        /// <summary>Size of the existing room, or the default (8.72 wide, 15 deep, 3.5 high).</summary>
        public static Vector3 CurrentRoomSize()
        {
            var info = Object.FindFirstObjectByType<RoomInfo>(FindObjectsInactive.Include);
            return info != null ? new Vector3(info.width, info.height, info.depth) : new Vector3(8.72f, 3.5f, 15f);
        }

        public static MetaXRAudioRoomAcousticProperties.MaterialPreset CurrentRoomMaterial()
        {
            var shoebox = Object.FindFirstObjectByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include);
            return shoebox != null ? shoebox.floorMaterial : MetaXRAudioRoomAcousticProperties.MaterialPreset.Concrete;
        }

        /// <param name="size">x = width, y = height, z = depth (meters). Used for BOTH the visual room and the shoebox model.</param>
        public static void BuildRoom(Vector3 size, MetaXRAudioRoomAcousticProperties.MaterialPreset acousticMaterial, bool bake)
        {
            float w = size.x, h = size.y, d = size.z, t = 0.1f;
            var env = Group(GroupEnvironment);
            DestroyChild(env, "Room");
            DestroyChild(env, "Shoebox Acoustics");

            // --- visual room: centered on the origin in x/z, floor at y = 0 ---
            var room = new GameObject("Room");
            room.transform.SetParent(env, false);
            var info = room.AddComponent<RoomInfo>();
            info.width = w; info.height = h; info.depth = d;

            Directory.CreateDirectory(MaterialsDir);
            var wallMat = Mat("Room_Wall", new Color(0.95f, 0.95f, 0.95f));
            var ceilMat = Mat("Room_Ceiling", new Color(0.95f, 0.95f, 0.95f));
            var floorMat = Mat("Room_Floor", new Color(0.75f, 0.75f, 0.75f)); // light gray

            Slab(room, "Floor", new Vector3(0, -t / 2, 0), new Vector3(w + 2 * t, t, d + 2 * t), floorMat);
            Slab(room, "Ceiling", new Vector3(0, h + t / 2, 0), new Vector3(w + 2 * t, t, d + 2 * t), ceilMat);
            Slab(room, "Wall_Left", new Vector3(-w / 2 - t / 2, h / 2, 0), new Vector3(t, h, d), wallMat);
            Slab(room, "Wall_Right", new Vector3(w / 2 + t / 2, h / 2, 0), new Vector3(t, h, d), wallMat);
            Slab(room, "Wall_Back", new Vector3(0, h / 2, -d / 2 - t / 2), new Vector3(w + 2 * t, h, t), wallMat);
            Slab(room, "Wall_Front", new Vector3(0, h / 2, d / 2 + t / 2), new Vector3(w + 2 * t, h, t), wallMat);

            // --- soft light: one large baked rectangular area light under the ceiling + light probes ---
            var lighting = new GameObject("Lighting");
            lighting.transform.SetParent(room.transform, false);
            var area = new GameObject("Ceiling Area Light").AddComponent<Light>();
            area.transform.SetParent(lighting.transform, false);
            area.transform.SetPositionAndRotation(new Vector3(0f, h - 0.05f, 0f), Quaternion.Euler(90f, 0f, 0f)); // faces down
            area.type = LightType.Rectangle;
            area.areaSize = new Vector2(Mathf.Max(0.5f, w - 1f), Mathf.Max(0.5f, d - 1f));
            area.lightmapBakeType = LightmapBakeType.Baked;
            area.intensity = 2f;
            area.shadows = LightShadows.None;

            var probes = new GameObject("Light Probes").AddComponent<LightProbeGroup>();
            probes.transform.SetParent(lighting.transform, false);
            var positions = new List<Vector3>();
            foreach (float fx in new[] { -0.35f, 0f, 0.35f })
                foreach (float fz in new[] { -0.35f, 0f, 0.35f })
                    foreach (float y in new[] { 0.3f, 1.2f, 2.2f })
                        positions.Add(new Vector3(fx * w, Mathf.Min(y, h - 0.2f), fz * d));
            probes.probePositions = positions.ToArray();

            // --- Meta XR Audio shoebox model: same dimensions, empty room (no clutter), one material for all six surfaces ---
            var shoeboxGo = new GameObject("Shoebox Acoustics");
            shoeboxGo.transform.SetParent(env, false);
            var shoebox = shoeboxGo.AddComponent<MetaXRAudioRoomAcousticProperties>();
            shoebox.lockPositionToListener = false;   // room center = this object's position (room center, mid-height)
            shoeboxGo.transform.position = new Vector3(0f, h / 2f, 0f);
            shoebox.width = w;
            shoebox.height = h;
            shoebox.depth = d;
            shoebox.clutterFactor = 0f;
            shoebox.leftMaterial = shoebox.rightMaterial = shoebox.frontMaterial = shoebox.backMaterial =
                shoebox.ceilingMaterial = shoebox.floorMaterial = acousticMaterial;

            if (Object.FindObjectsByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 1)
                Debug.LogWarning("[HumPer] More than one MetaXRAudioRoomAcousticProperties in the scene; Meta allows only one.");

            MarkSceneDirty();
            Debug.Log("[HumPer] Room built: " + w + " x " + d + " m, height " + h + " m (shoebox material " + acousticMaterial +
                      "). Mean absorption needed for RT60 0.7 s: " +
                      HumPerSetup.AbsorptionForRt60(w, d, h, 0.7f).ToString("0.000") + " (Sabine).");

            if (bake) BakeLighting();
        }

        public static void BakeLighting()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning("[HumPer] Save the scene first, then bake (Window > Rendering > Lighting > Generate Lighting).");
                return;
            }
            EditorSceneManager.SaveScene(scene);
            if (Lightmapping.Bake()) EditorSceneManager.SaveScene(scene);
            else Debug.LogWarning("[HumPer] Lightmap bake failed.");
        }

        // ---- Step 3: GUI (menu, language, practice instructions, head gate, practice result, HUD) -------------

        [MenuItem("Magic/Step 3 - GUI: Menu, language, practice screens, HUD (OVROverlayCanvas)", priority = 3)]
        public static void AddGui()
        {
            EnsureLayerName(GuiPanelPlacer.HiddenCanvasLayer, "HiddenOverlayCanvas");

            var ui = Group(GroupUI);
            DestroyEverywhere("GUI");
            DestroyEverywhere("HUD");
            DestroyEverywhere("GUI Test Panel");
            DestroyEverywhere("UI Pointer");

            var go = new GameObject("GUI", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(ui, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1000, 700);
            rt.localScale = Vector3.one * 0.002f; // 1000 px = 2 m, 700 px = 1.4 m
            go.transform.position = new Vector3(0f, 1.2f, 2f); // editor preview; GuiPanelPlacer moves it at runtime
            var refs = go.AddComponent<GuiRefs>();
            var dark = new Color(0.09f, 0.1f, 0.14f, 1f);
            var green = new Color(0.13f, 0.42f, 0.24f, 1f);
            var gray = new Color(0.22f, 0.25f, 0.32f, 1f);

            // --- stage 1: menu (experimenter, English only) ---
            var stage = Panel(go.transform, "Stage 1 - Menu", dark);
            Label(stage, "Title", "Participant ID", 44, new Vector2(0, 305), new Vector2(800, 60));
            var idDisplay = new GameObject("ID Display", typeof(RectTransform), typeof(Image));
            idDisplay.transform.SetParent(stage, false);
            ((RectTransform)idDisplay.transform).anchoredPosition = new Vector2(0, 235);
            ((RectTransform)idDisplay.transform).sizeDelta = new Vector2(620, 80);
            idDisplay.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.24f, 1f);
            idDisplay.GetComponent<Image>().raycastTarget = false;
            var idLabel = Label(idDisplay.transform, "Label", "", 44, Vector2.zero, new Vector2(600, 70));
            var idField = idDisplay.AddComponent<ParticipantIdField>();
            idField.label = idLabel;
            // number pad: two rows of five digits, then backspace / clear
            var pad = new Color(0.22f, 0.25f, 0.32f, 1f);
            for (int slot = 0; slot < 10; slot++)
            {
                int digit = (slot + 1) % 10; // 1 2 3 4 5 / 6 7 8 9 0
                var position = new Vector2((slot % 5 - 2) * 125f, slot < 5 ? 135f : 45f);
                idField.digitButtons[digit] = MakeButton(stage, "Key " + digit, digit.ToString(), 44, position, new Vector2(110, 80), pad, out _);
            }
            idField.backspaceButton = MakeButton(stage, "Key Backspace", "<", 44, new Vector2(-135, -45), new Vector2(250, 80),
                                                 new Color(0.42f, 0.32f, 0.12f, 1f), out _);
            idField.clearButton = MakeButton(stage, "Key Clear", "Clear", 36, new Vector2(135, -45), new Vector2(250, 80),
                                             new Color(0.42f, 0.18f, 0.18f, 1f), out _);
            var start = MakeButton(stage, "Start Experiment", "Start Experiment", 36, new Vector2(-220, -165), new Vector2(400, 100), green, out _);
            var debug = MakeButton(stage, "Debug", "Debug", 36, new Vector2(220, -165), new Vector2(400, 100),
                                   new Color(0.42f, 0.32f, 0.12f, 1f), out _);
            var status = Label(stage, "Status", "", 28, new Vector2(0, -280), new Vector2(900, 70));
            var menu = stage.gameObject.AddComponent<MenuStage>();
            menu.idField = idField;
            menu.startButton = start;
            menu.debugButton = debug;
            menu.statusText = status;
            refs.menu = menu;

            // --- stage 2: language ---
            var lang = Panel(go.transform, "Stage 2 - Language", dark);
            LocalizedLabel(lang, "Title", "lang_title", 56, new Vector2(0, 150), new Vector2(800, 200));
            refs.chineseButton = MakeButton(lang, "Chinese (Taiwan)", "中文（台灣）", 48, new Vector2(-230, -100), new Vector2(400, 140), gray, out var zhLabel);
            zhLabel.gameObject.AddComponent<LocalizedText>().alwaysCjk = true;
            refs.englishButton = MakeButton(lang, "English", "English", 48, new Vector2(230, -100), new Vector2(400, 140), gray, out _);
            refs.languagePanel = lang.gameObject;

            // --- practice instructions ---
            var intro = Panel(go.transform, "Practice - Instructions", dark);
            LocalizedLabel(intro, "Text", "practice_intro", 40, new Vector2(0, 70), new Vector2(880, 460));
            refs.startPracticeButton = MakeButton(intro, "Start Practice", "", 42, new Vector2(0, -270), new Vector2(460, 100), green, out var startLabel);
            LocalizeLabel(startLabel, "practice_start");
            refs.introPanel = intro.gameObject;

            // --- setup instructions before the practice (recenter, IPD, look at the dot); one panel, the text key changes per step ---
            var setup = Panel(go.transform, "Practice - Setup Instructions", dark);
            var setupText = LocalizedLabel(setup, "Text", "setup_recenter", 40, new Vector2(0, 70), new Vector2(880, 460));
            refs.setupText = setupText.GetComponent<LocalizedText>();
            refs.setupContinueButton = MakeButton(setup, "Continue", "", 42, new Vector2(0, -270), new Vector2(400, 100), green, out var setupContinueLabel);
            LocalizeLabel(setupContinueLabel, "continue");
            refs.setupPanel = setup.gameObject;

            // --- head alignment gate: no background, just a fixation dot at the center (straight ahead) ---
            var gate = new GameObject("Gate - Look Straight Ahead", typeof(RectTransform));
            gate.transform.SetParent(go.transform, false);
            var grt = (RectTransform)gate.transform;
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = grt.offsetMax = Vector2.zero;
            var dotGo = new GameObject("Fixation Dot", typeof(RectTransform), typeof(Image));
            dotGo.transform.SetParent(gate.transform, false);
            ((RectTransform)dotGo.transform).sizeDelta = new Vector2(26, 26);
            var dot = dotGo.GetComponent<Image>();
            dot.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            dot.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            dot.raycastTarget = false;
            var hint = LocalizedLabel(gate.transform, "Hint", "gate_hint", 40, new Vector2(0, -70), new Vector2(800, 80));
            hint.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            refs.gatePanel = gate;
            refs.gateHint = hint.gameObject;

            // --- practice result: again / ready ---
            var result = Panel(go.transform, "Practice - Result", dark);
            LocalizedLabel(result, "Text", "practice_result", 46, new Vector2(0, 130), new Vector2(880, 200));
            refs.againButton = MakeButton(result, "Practice Again", "", 38, new Vector2(-250, -130), new Vector2(430, 150), gray, out var againLabel);
            LocalizeLabel(againLabel, "practice_again");
            refs.readyButton = MakeButton(result, "Ready", "", 34, new Vector2(250, -130), new Vector2(430, 150), green, out var readyLabel);
            LocalizeLabel(readyLabel, "ready");
            refs.resultPanel = result.gameObject;

            // --- rest screen between the three conditions ---
            var block = Panel(go.transform, "Experiment - Rest Between Conditions", dark);
            var blockText = LocalizedLabel(block, "Text", "block_done", 48, new Vector2(0, 100), new Vector2(880, 300));
            refs.blockDoneText = blockText.GetComponent<LocalizedText>();
            refs.continueButton = MakeButton(block, "Continue", "", 42, new Vector2(0, -200), new Vector2(400, 110), green, out var continueLabel);
            LocalizeLabel(continueLabel, "continue");
            refs.blockDonePanel = block.gameObject;

            // --- end of the experiment ---
            var thanks = Panel(go.transform, "Experiment - Thank You", dark);
            LocalizedLabel(thanks, "Text", "thanks", 64, Vector2.zero, new Vector2(880, 300));
            refs.thanksPanel = thanks.gameObject;

            // --- experimenter: unfinished session found for the entered ID (English) ---
            var resumeP = Panel(go.transform, "Experimenter - Resume Session", dark);
            refs.resumeText = Label(resumeP, "Text", "", 40, new Vector2(0, 150), new Vector2(900, 320));
            refs.resumeButton = MakeButton(resumeP, "Resume", "Resume", 40, new Vector2(-230, -130), new Vector2(400, 120), green, out _);
            refs.startNewButton = MakeButton(resumeP, "Start New", "Start new", 40, new Vector2(230, -130), new Vector2(400, 120),
                                             new Color(0.42f, 0.18f, 0.18f, 1f), out _);
            refs.resumeBackButton = MakeButton(resumeP, "Back", "Back", 34, new Vector2(0, -270), new Vector2(300, 80), gray, out _);
            refs.resumePanel = resumeP.gameObject;

            // --- Debug test bench (shown between trials) ---
            var bench = Panel(go.transform, "Debug - Test Bench", dark);
            var benchPanel = bench.gameObject.AddComponent<DebugPanel>();
            var orange = new Color(0.42f, 0.32f, 0.12f, 1f);
            Label(bench, "Title", "Debug test bench", 40, new Vector2(0, 318), new Vector2(800, 50));
            benchPanel.statusText = Label(bench, "Status", "", 22, new Vector2(0, 268), new Vector2(960, 56));
            // reverb time = condition (one-to-one): 0.3 s Quiet, 0.7 s Noisy (+ background), 2.2 s Reverb (+ background); Meta's preset
            benchPanel.rtQuietButton = MakeButton(bench, "RT60 0.3 Quiet", "0.3 s  Quiet", 28, new Vector2(-345, 190), new Vector2(220, 80), gray, out _);
            benchPanel.rtNoisyButton = MakeButton(bench, "RT60 0.7 Noisy", "0.7 s  Noisy", 28, new Vector2(-115, 190), new Vector2(220, 80), gray, out _);
            benchPanel.rtReverbButton = MakeButton(bench, "RT60 2.2 Reverb", "2.2 s  Reverb", 28, new Vector2(115, 190), new Vector2(220, 80), gray, out _);
            benchPanel.presetButton = MakeButton(bench, "Reverb Preset", "Preset", 24, new Vector2(345, 190), new Vector2(220, 80), gray, out benchPanel.presetLabel);
            benchPanel.reverbReadout = Label(bench, "Reverb Readout", "", 24, new Vector2(0, 110), new Vector2(940, 60));
            // playback tests
            benchPanel.stimulusToggleButton = MakeButton(bench, "Play Stimulus", "", 26, new Vector2(-250, 30), new Vector2(480, 70), gray, out benchPanel.stimulusToggleLabel);
            benchPanel.backgroundToggleButton = MakeButton(bench, "Play Background", "", 26, new Vector2(250, 30), new Vector2(480, 70), gray, out benchPanel.backgroundToggleLabel);
            benchPanel.sphereToggleButton = MakeButton(bench, "Sphere Toggle", "", 24, new Vector2(-250, -50), new Vector2(480, 70), gray, out benchPanel.sphereToggleLabel);
            benchPanel.measureButton = MakeButton(bench, "Measure RT60", "Measure RT60 (5 s)", 26, new Vector2(250, -50), new Vector2(480, 70), orange, out _);
            benchPanel.runTrialButton = MakeButton(bench, "Run Trial", "Run trial", 34, new Vector2(-250, -145), new Vector2(480, 85), green, out _);
            benchPanel.practiceTrialButton = MakeButton(bench, "Practice Trial", "Practice-style trial", 30, new Vector2(250, -145), new Vector2(480, 85), gray, out _);
            benchPanel.backButton = MakeButton(bench, "Back", "Back to menu", 30, new Vector2(0, -250), new Vector2(400, 75),
                                               new Color(0.42f, 0.2f, 0.2f, 1f), out _);
            benchPanel.SetSphereAlways(false);
            benchPanel.SetStimulusPlaying(false);
            benchPanel.SetBackgroundState(false, false);
            benchPanel.SetReverb("", 1);
            refs.debugPanel = benchPanel;

            // --- Y button: cancel this participant? Upper region, on top of every screen, English (experimenter) ---
            var cancel = new GameObject("Dialog - Cancel Participant", typeof(RectTransform), typeof(Image));
            cancel.transform.SetParent(go.transform, false);
            var crt = (RectTransform)cancel.transform;
            crt.anchoredPosition = new Vector2(0, 235);
            crt.sizeDelta = new Vector2(940, 230);
            cancel.GetComponent<Image>().color = new Color(0.30f, 0.08f, 0.08f, 1f);
            refs.cancelText = Label(crt, "Text", "", 30, new Vector2(0, 55), new Vector2(900, 110));
            refs.cancelConfirmButton = MakeButton(crt, "Cancel Participant", "Cancel participant", 30, new Vector2(-230, -55), new Vector2(400, 80),
                                                  new Color(0.65f, 0.18f, 0.18f, 1f), out _);
            refs.cancelKeepButton = MakeButton(crt, "Keep Going", "Keep going", 30, new Vector2(230, -55), new Vector2(400, 80), gray, out _);
            refs.cancelPanel = cancel;
            cancel.SetActive(false);

            var fontGuid = AssetDatabase.FindAssets("t:Font", new[] { "Assets/Fonts" }).FirstOrDefault();
            if (fontGuid != null) refs.cjkFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(fontGuid));
            else Debug.LogWarning("[HumPer] No font in Assets/Fonts: Chinese text will use an OS font if the headset has one. Add Noto Sans TC (.ttf/.otf) to be safe.");

            go.AddComponent<OVROverlayCanvas>();
            var mainPlacer = go.AddComponent<GuiPanelPlacer>();
            refs.mainPlacer = mainPlacer;

            // --- HUD: a small strip above the stimuli (practice hints, Debug feedback) ---
            var hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
            hud.transform.SetParent(ui, false);
            hud.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var hrt = (RectTransform)hud.transform;
            hrt.sizeDelta = new Vector2(1000, 110);
            hrt.localScale = Vector3.one * 0.002f; // 2 m x 0.22 m
            hud.transform.position = new Vector3(0f, 2.3f, 2f);
            Panel(hud.transform, "Background", new Color(0f, 0f, 0f, 0.6f));
            var hudText = LocalizedLabel(hud.transform, "Text", "practice_hud", 34, Vector2.zero, new Vector2(960, 100));
            hud.AddComponent<OVROverlayCanvas>();
            var hudPlacer = hud.AddComponent<GuiPanelPlacer>();
            hudPlacer.heightOffsetM = 1.05f; // ~28 deg above eye level: above the figure's head (24 deg at 1.2 m)
            refs.hudRoot = hud;
            refs.hudPlacer = hudPlacer;
            refs.hudText = hudText;
            refs.hudLocalized = hudText.GetComponent<LocalizedText>();

            // The source canvases live on a layer that the eye cameras do not render; only the overlays show them.
            go.GetComponent<OVROverlayCanvas>().SetCanvasLayer(GuiPanelPlacer.HiddenCanvasLayer, true);
            hud.GetComponent<OVROverlayCanvas>().SetCanvasLayer(GuiPanelPlacer.HiddenCanvasLayer, true);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                cam.cullingMask &= ~((1 << GuiPanelPlacer.HiddenCanvasLayer) | (1 << GuiPanelPlacer.OverlayRenderLayer));

            var pointer = new GameObject("UI Pointer");
            pointer.transform.SetParent(ui, false);
            Directory.CreateDirectory(MaterialsDir);
            pointer.AddComponent<UiPointer>().laserMaterial = Mat("Laser", Color.white, unlit: true); // URP Unlit: renders in both eyes

            // Step 4 refers to this GUI; re-wire it automatically if it already exists.
            if (Object.FindFirstObjectByType<ExperimentFlow>(FindObjectsInactive.Include) != null) SetupPractice();

            MarkSceneDirty();
            Debug.Log("[HumPer] GUI built under '" + GroupUI + "' (menu, language, practice screens, HUD).");
        }

        static Text LocalizedLabel(Transform parent, string name, string key, int size, Vector2 pos, Vector2 box)
        {
            var t = Label(parent, name, "", size, pos, box);
            LocalizeLabel(t, key);
            return t;
        }

        static void LocalizeLabel(Text t, string key)
        {
            var l = t.gameObject.GetComponent<LocalizedText>() ?? t.gameObject.AddComponent<LocalizedText>();
            l.key = key;
            t.text = Localization.Get(key);
        }

        static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        static Text Label(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(Transform parent, string name, string text, int size, Vector2 pos, Vector2 box, Color color, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var image = go.GetComponent<Image>();
            image.color = Color.white;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.35f);
            colors.pressedColor = Color.Lerp(color, Color.white, 0.6f);
            colors.selectedColor = color;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            label = Label(go.transform, "Label", text, size, Vector2.zero, box - new Vector2(20, 10));
            return button;
        }

        // ---- Step 4: practice stage wiring (stimulus controller + experiment flow) ------------------------------

        const string StimulusClipSearch = "SFX-C-01";

        [MenuItem("Magic/Step 4 - Practice: stimulus controller + experiment flow", priority = 4)]
        public static void SetupPractice()
        {
            var refs = Object.FindFirstObjectByType<GuiRefs>(FindObjectsInactive.Include);
            var rig = Object.FindFirstObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
            var box = FindByName(VisualBoxName);
            var audio = FindByName("[BuildingBlock] Spatial Audio Stimuli");
            if (refs == null) { Debug.LogError("[HumPer] Run Step 3 (GUI) first."); return; }
            if (rig == null) { Debug.LogError("[HumPer] No OVRCameraRig: add the Camera Rig building block."); return; }
            if (box == null || audio == null) { Debug.LogError("[HumPer] Run the Temp step first (visual box + spatial audio placement)."); return; }

            var group = Group(GroupExperiment);
            DestroyEverywhere("Stimulus Controller");
            DestroyEverywhere("Experiment Flow");
            DestroyEverywhere("Debug Audio Sphere");

            // Debug sphere: marks where the audio stimulus ended up; only shown in Debug mode, hidden at the start of each trial.
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Debug Audio Sphere";
            sphere.transform.SetParent(Group(GroupStimuli), false);
            sphere.transform.localScale = Vector3.one * 0.15f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            sphere.GetComponent<Renderer>().sharedMaterial = Mat("Debug_Sphere", new Color(1f, 0.45f, 0.1f), unlit: true);
            sphere.SetActive(false);

            var clip = FindClip(StimulusClipSearch);
            if (clip == null) Debug.LogWarning("[HumPer] No audio clip named '" + StimulusClipSearch + "*' in Assets/Audio; the stimulus keeps the building block's clip.");
            else ForceDecompressOnLoad(clip);

            var controller = new GameObject("Stimulus Controller").AddComponent<StimulusController>();
            controller.transform.SetParent(group, false);
            controller.visual = box.transform;
            controller.audioSource = audio.GetComponent<AudioSource>();
            controller.audioMeta = audio.GetComponent<MetaXRAudioSource>();
            controller.debugSphere = sphere.transform;
            controller.clip = clip;

            // Acoustic conditions: reverb = EnableAcoustics on the stimulus; noisy = the three Spatial Audio background blocks
            // (left, right, back) with the three background recordings (SFX-C-02..04, sorted by name).
            // keep the PO's arrangement (which clip / azimuth belongs to which background source) when this step is run again
            var previous = Object.FindFirstObjectByType<AcousticConditions>(FindObjectsInactive.Include);
            var keptClips = previous != null ? previous.backgroundClips : null;
            var keptAzimuths = previous != null ? previous.backgroundAzimuthsDeg : null;
            var keptRadius = previous != null ? previous.backgroundRadiusM : 2f;
            DestroyEverywhere("Acoustic Conditions");
            var conditions = new GameObject("Acoustic Conditions").AddComponent<AcousticConditions>();
            conditions.transform.SetParent(group, false);
            conditions.stimulusMeta = audio.GetComponent<MetaXRAudioSource>();
            var backgroundObjects = new[]
            {
                FindByName("[BuildingBlock] Spatial Audio Background Left"),
                FindByName("[BuildingBlock] Spatial Audio Background Right"),
                FindByName("[BuildingBlock] Spatial Audio Background Back"),
            };
            var backgroundClips = new[] { FindClip("SFX-C-02"), FindClip("SFX-C-04"), FindClip("SFX-C-03") }; // left, right, back (PO arrangement)
            conditions.backgroundSources = backgroundObjects.Select(o => o != null ? o.GetComponent<AudioSource>() : null).ToArray();
            conditions.backgroundMeta = backgroundObjects.Select(o => o != null ? o.GetComponent<MetaXRAudioSource>() : null).ToArray();
            if (keptClips != null && keptClips.Length == 3 && keptClips.All(c => c != null)) backgroundClips = keptClips;
            if (keptAzimuths != null && keptAzimuths.Length == 3) conditions.backgroundAzimuthsDeg = keptAzimuths;
            conditions.backgroundRadiusM = keptRadius;
            conditions.backgroundClips = backgroundClips;
            for (int i = 0; i < 3; i++)
            {
                if (backgroundObjects[i] == null) Debug.LogError("[HumPer] Spatial Audio background block " + i + " not found.");
                if (backgroundClips[i] == null) Debug.LogWarning("[HumPer] Background clip SFX-C-0" + (i + 2) + " not found in Assets/Audio.");
                else ForceDecompressOnLoad(backgroundClips[i]);
            }

            // Route all four sources through the mixer that carries the Meta reflection effect (the room is rendered there).
            var mixerGroup = EnsureMixerGroup();
            if (mixerGroup != null)
            {
                controller.audioSource.outputAudioMixerGroup = mixerGroup;
                foreach (var s in conditions.backgroundSources) if (s != null) s.outputAudioMixerGroup = mixerGroup;
            }

            var flow = new GameObject("Experiment Flow").AddComponent<ExperimentFlow>();
            flow.transform.SetParent(group, false);
            flow.gui = refs;
            flow.stimuli = controller;
            flow.acoustics = conditions;
            flow.rig = rig;
            var tuner = flow.gameObject.AddComponent<ReverbTuner>();
            tuner.room = Object.FindFirstObjectByType<MetaXRAudioRoomAcousticProperties>(FindObjectsInactive.Include);
            tuner.roomInfo = Object.FindFirstObjectByType<RoomInfo>(FindObjectsInactive.Include);
            if (tuner.room == null) Debug.LogWarning("[HumPer] No shoebox (Step 2) found: the Debug reverb buttons will not work.");
            flow.reverb = tuner;
            conditions.reverb = tuner;

            MarkSceneDirty();
            Debug.Log("[HumPer] Practice stage wired (stimulus clip: " + (clip != null ? clip.name : "none") + ").");
        }

        /// <summary>Logs every unassigned object reference on the HumPer components. Menu: Magic/Validate Setup.</summary>
        [MenuItem("Magic/Validate Setup", priority = 101)]
        public static void ValidateSetup()
        {
            int problems = 0;
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.GetType().Namespace != "HumPer") continue;
                var so = new SerializedObject(behaviour);
                for (var p = so.GetIterator(); p.NextVisible(true);)
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.propertyPath == "m_Script") continue;
                    if (p.objectReferenceValue != null) continue;
                    // optional references
                    if (p.name == "cjkFont" || p.name == "clip" || p.name == "hint" || p.name == "laserMaterial" || p.name == "rt60Meter") continue;
                    Debug.LogError("[HumPer] Validate: " + behaviour.GetType().Name + "." + p.propertyPath + " is not assigned (" + behaviour.name + ")", behaviour);
                    problems++;
                }
            }
            foreach (var meta in Object.FindObjectsByType<MetaXRAudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var source = meta.GetComponent<AudioSource>();
                if (source == null || source.outputAudioMixerGroup != null) continue;
                Debug.LogError("[HumPer] Validate: " + meta.name + " is not routed to the Meta reflection mixer (room acoustics would be silent). Run Step 4.", meta);
                problems++;
            }
            Debug.Log("[HumPer] Validate: " + (problems == 0 ? "OK, all references assigned." : problems + " problem(s)."));
        }

        // ---- Audio mixer with the "Meta XR Audio Reflection" effect ----------------------------------------------
        // Meta's room acoustics (early reflections and late reverb of the shoebox) are rendered by this mixer effect; without it
        // sources with Enable Acoustics are completely dry. The mixer API is internal to the editor, hence reflection.

        const string MixerPath = "Assets/Audio/HumPerMixer.mixer";
        const string ReflectionEffectName = "Meta XR Audio Reflection";

        /// <summary>The Master group of Assets/Audio/HumPerMixer.mixer, creating the mixer with the reflection effect if needed.</summary>
        public static AudioMixerGroup EnsureMixerGroup()
        {
            bool hasEffect = File.Exists(MixerPath) && File.ReadAllText(MixerPath).Contains(ReflectionEffectName);
            if (!hasEffect)
            {
                if (File.Exists(MixerPath)) AssetDatabase.DeleteAsset(MixerPath);
                CreateMixerWithReflection();
            }
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var groups = mixer != null ? mixer.FindMatchingGroups("Master") : new AudioMixerGroup[0];
            if (groups.Length == 0) Debug.LogError("[HumPer] Could not create/load " + MixerPath + ". Create an Audio Mixer by hand and add the 'Meta XR Audio Reflection' effect to Master.");
            return groups.Length > 0 ? groups[0] : null;
        }

        static void CreateMixerWithReflection()
        {
            const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                                                       System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
            var editorAssembly = typeof(Editor).Assembly;
            var controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            var effectType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerEffectController");
            if (controllerType == null || effectType == null) return;

            var controller = (Object)controllerType.GetMethod("CreateMixerControllerAtPath", all).Invoke(null, new object[] { MixerPath });
            object master = controllerType.GetProperty("masterGroup", all).GetValue(controller);
            var effects = (System.Array)master.GetType().GetProperty("effects", all).GetValue(master);
            var effect = (Object)System.Activator.CreateInstance(effectType, all, null, new object[] { ReflectionEffectName }, null);
            effectType.GetMethod("PreallocateGUIDs", all).Invoke(effect, null);
            master.GetType().GetMethod("InsertEffect", all).Invoke(master, new object[] { effect, effects.Length });
            AssetDatabase.AddObjectToAsset(effect, controller);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HumPer] Created " + MixerPath + " with the '" + ReflectionEffectName + "' effect on Master.");
        }

        static GameObject FindByName(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.name == name).Select(t => t.gameObject).FirstOrDefault();

        static AudioClip FindClip(string nameContains)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return null;
        }

        /// <summary>The runtime measures the clip RMS (AudioClip.GetData), which needs Decompress On Load.</summary>
        static void ForceDecompressOnLoad(AudioClip clip)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as AudioImporter;
            if (importer == null) return;
            var s = importer.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.DecompressOnLoad) return;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }

        // ---- Hot Fix step: tidy the scene hierarchy, place the spatial audio, add the visual stimulus cardboard ----

        const float EyeLevelM = 1.2f;       // seated eye / ear height used for audio sources (runtime will use the real head height)
        const float StimulusRadiusM = 2f;     // experiment trials: audio, visual and background clips are all 2 m from the head

        [MenuItem("Magic/Hot Fix - Organize Hierarchy, Place Audio, Add Visual Cardboard", priority = 100)]
        public static void OrganizeHierarchy()
        {
            var scene = EditorSceneManager.GetActiveScene();
            int moved = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                string target = null;
                string n = root.name;
                if (IsGroup(n)) continue;
                if (root.GetComponentInChildren<OVRCameraRig>(true) != null || n.StartsWith("[BuildingBlock] Camera Rig")
                    || n.StartsWith("[BuildingBlock] Controller Tracking")) target = GroupXR;
                else if (n == "[BuildingBlock] Spatial Audio Stimuli") target = GroupStimuli;
                else if (n.StartsWith("[BuildingBlock] Spatial Audio")) target = GroupAudio;
                else if (n == "Room" || n.StartsWith("Shoebox")) target = GroupEnvironment;
                else if (n == "GUI" || n == "HUD" || n == "UI Pointer" || n == "GUI Test Panel") target = GroupUI;
                else if (n.StartsWith("Visual Stimulus") || n == "Debug Audio Sphere") target = GroupStimuli;
                else if (n == "Stimulus Controller" || n == "Experiment Flow" || n == "Acoustic Conditions") target = GroupExperiment;
                else if (n == "HumPer")
                {
                    // old container from an earlier step: move its children, then remove it
                    foreach (var child in root.transform.Cast<Transform>().ToList())
                    {
                        child.SetParent(Group(child.name.StartsWith("GUI") ? GroupUI : GroupEnvironment), true);
                        moved++;
                    }
                    if (root.transform.childCount == 0) Object.DestroyImmediate(root);
                    continue;
                }
                if (target == null) continue;
                root.transform.SetParent(Group(target), true);
                moved++;
            }

            PlaceSpatialAudio();
            AddVisualStimulusBox();

            // Stable order of the groups.
            int i = 0;
            foreach (var g in new[] { GroupXR, GroupAudio, GroupStimuli, GroupExperiment, GroupEnvironment, GroupUI })
            {
                var go = FindRoot(g);
                if (go != null) go.transform.SetSiblingIndex(i++);
            }

            MarkSceneDirty();
            Debug.Log("[HumPer] Hierarchy organized (" + moved + " object(s) moved).");
        }

        /// <summary>
        /// Spatial Audio building blocks: stimulus at +60 deg (first trial position), backgrounds left (-90), right (+90) and back (180),
        /// all at eye level and 1 m from the origin. Azimuth 0 = rig forward (+Z), negative = left (-X).
        /// </summary>
        static void PlaceSpatialAudio()
        {
            var placements = new (string name, float azimuthDeg)[]
            {
                ("[BuildingBlock] Spatial Audio Stimuli", 60f),
                ("[BuildingBlock] Spatial Audio Background Left", -90f),
                ("[BuildingBlock] Spatial Audio Background Right", 90f),
                ("[BuildingBlock] Spatial Audio Background Back", 180f),
            };
            foreach (var (name, az) in placements)
            {
                var t = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(x => x.name == name);
                if (t == null) { Debug.LogWarning("[HumPer] Not found: " + name); continue; }
                float a = az * Mathf.Deg2Rad;
                t.position = new Vector3(StimulusRadiusM * Mathf.Sin(a), EyeLevelM, StimulusRadiusM * Mathf.Cos(a));
                t.rotation = Quaternion.identity;

                // Sounds are started by the experiment, not on scene start.
                var source = t.GetComponent<AudioSource>();
                if (source != null)
                {
                    source.playOnAwake = false;
                    source.Stop();
                    EditorUtility.SetDirty(source);
                }
            }
        }

        /// <summary>Placeholder cardboard (image to be added later): 0.25 m wide x 0.2 m high, centered at eye level, straight ahead, facing the participant.</summary>
        static void AddVisualStimulusBox()
        {
            DestroyEverywhere(VisualBoxName);
            DestroyEverywhere(LegacyVisualBoxName);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = VisualBoxName;
            box.transform.SetParent(Group(GroupStimuli), false);
            box.transform.position = new Vector3(0f, EyeLevelM, StimulusRadiusM);
            box.transform.localScale = new Vector3(0.25f, 0.2f, 0.01f);
            box.GetComponent<Renderer>().sharedMaterial = Mat("Visual_Cardboard", new Color(0.78f, 0.65f, 0.45f));
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.SetActive(false); // hidden at the beginning; the experiment shows it per trial
        }

        // ---- helpers ------------------------------------------------------------------------------------------

        /// <summary>Mean wall absorption coefficient needed for a target RT60 in a shoebox (Sabine).</summary>
        public static float AbsorptionForRt60(float w, float d, float h, float rt60) =>
            0.161f * (w * d * h) / (2f * (w * d + w * h + d * h) * rt60);

        static bool IsGroup(string name) => name == GroupXR || name == GroupAudio || name == GroupStimuli || name == GroupExperiment || name == GroupEnvironment || name == GroupUI;

        static GameObject FindRoot(string name) =>
            EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);

        /// <summary>Root group object with identity transform (created if missing).</summary>
        static Transform Group(string name)
        {
            var go = FindRoot(name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            return go.transform;
        }

        static void DestroyChild(Transform parent, string name)
        {
            var c = parent.Find(name);
            if (c != null) Object.DestroyImmediate(c.gameObject);
            var root = FindRoot(name); // an old, un-grouped copy
            if (root != null) Object.DestroyImmediate(root);
        }

        static void DestroyEverywhere(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t != null && t.name == name) Object.DestroyImmediate(t.gameObject);
        }

        static Material Mat(string name, Color color, bool unlit = false)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            if (!unlit) m.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Slab(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        static void EnsureLayerName(int layer, string name)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var element = tagManager.FindProperty("layers").GetArrayElementAtIndex(layer);
            if (element.stringValue == name) return;
            if (!string.IsNullOrEmpty(element.stringValue))
            {
                Debug.LogWarning("[HumPer] Layer " + layer + " is already named '" + element.stringValue + "'; using it as the hidden canvas layer.");
                return;
            }
            element.stringValue = name;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void MarkSceneDirty() => EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        /// <summary>Headless entry for verification: runs every step on the Experiment scene and saves it.</summary>
        public static void RunAllHeadless()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ExperimentScene.unity");
            SetupLighting();
            AddGui();
            BuildRoom(new Vector3(8.72f, 3.5f, 15f), MetaXRAudioRoomAcousticProperties.MaterialPreset.Concrete, bake: true);
            OrganizeHierarchy();
            SetupPractice();
            ValidateSetup();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
