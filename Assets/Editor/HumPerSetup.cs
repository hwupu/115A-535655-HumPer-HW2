using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        public const string GroupUI = "UI";

        const string VisualBoxName = "Visual Stimulus (placeholder box)";
        const string LightingSettingsPath = "Assets/Settings/ExperimentLighting.lighting";
        const string MaterialsDir = "Assets/Materials";

        // ---- Step 1: global illumination instead of a directional light ---------------------------------------

        [MenuItem("HumPer/Step 1 - Lighting: Global Illumination (remove directional light)", priority = 1)]
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

        [MenuItem("HumPer/Step 2 - Room Setup...", priority = 2)]
        public static void OpenRoomDialog() => RoomSetupDialog.Open();

        /// <summary>Size of the existing room, or the default (8 wide, 10 deep, 3.5 high).</summary>
        public static Vector3 CurrentRoomSize()
        {
            var info = Object.FindFirstObjectByType<RoomInfo>(FindObjectsInactive.Include);
            return info != null ? new Vector3(info.width, info.height, info.depth) : new Vector3(8f, 3.5f, 10f);
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

        // ---- Step 3: GUI (stage 1: participant ID, Start, Debug) ----------------------------------------------

        [MenuItem("HumPer/Step 3 - GUI: Stage 1 menu (OVROverlayCanvas)", priority = 3)]
        public static void AddGui()
        {
            EnsureLayerName(GuiPanelPlacer.HiddenCanvasLayer, "HiddenOverlayCanvas");
            EnableSystemKeyboard();

            var ui = Group(GroupUI);
            DestroyEverywhere("GUI");
            DestroyEverywhere("GUI Test Panel");
            DestroyEverywhere("UI Pointer");

            var go = new GameObject("GUI", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(ui, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1000, 700);
            rt.localScale = Vector3.one * 0.002f; // 1000 px = 2 m, 700 px = 1.4 m
            go.transform.position = new Vector3(0f, 1.2f, 2f); // editor preview; GuiPanelPlacer moves it at runtime

            // --- stage 1: menu ---
            var stage = Panel(go.transform, "Stage 1 - Menu", new Color(0.09f, 0.1f, 0.14f, 1f));
            Label(stage, "Title", "Participant ID", 48, new Vector2(0, 240), new Vector2(800, 70));
            var idButton = MakeButton(stage, "ID Field", "(tap to enter)", 44, new Vector2(0, 150), new Vector2(620, 100),
                                      new Color(0.16f, 0.18f, 0.24f, 1f), out var idLabel);
            var idField = idButton.gameObject.AddComponent<ParticipantIdField>();
            idField.label = idLabel;
            idField.hint = Label(stage, "ID Hint", "", 24, new Vector2(0, 80), new Vector2(920, 50));
            var start = MakeButton(stage, "Start Experiment", "Start Experiment", 38, new Vector2(-220, -50), new Vector2(400, 120),
                                   new Color(0.13f, 0.42f, 0.24f, 1f), out _);
            var debug = MakeButton(stage, "Debug", "Debug", 38, new Vector2(220, -50), new Vector2(400, 120),
                                   new Color(0.42f, 0.32f, 0.12f, 1f), out _);
            var status = Label(stage, "Status", "", 30, new Vector2(0, -230), new Vector2(900, 80));
            var menu = stage.gameObject.AddComponent<MenuStage>();
            menu.idField = idField;
            menu.startButton = start;
            menu.debugButton = debug;
            menu.statusText = status;

            go.AddComponent<OVROverlayCanvas>();
            go.AddComponent<GuiPanelPlacer>();
            // The source canvas lives on a layer that the eye cameras do not render; only the overlay shows it.
            go.GetComponent<OVROverlayCanvas>().SetCanvasLayer(GuiPanelPlacer.HiddenCanvasLayer, true);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                cam.cullingMask &= ~((1 << GuiPanelPlacer.HiddenCanvasLayer) | (1 << GuiPanelPlacer.OverlayRenderLayer));

            var pointer = new GameObject("UI Pointer");
            pointer.transform.SetParent(ui, false);
            Directory.CreateDirectory(MaterialsDir);
            pointer.AddComponent<UiPointer>().laserMaterial = Mat("Laser", Color.white, unlit: true); // URP Unlit: renders in both eyes (single-pass stereo)

            MarkSceneDirty();
            Debug.Log("[HumPer] GUI stage 1 (menu) added under '" + GroupUI + "'.");
        }

        static void EnableSystemKeyboard()
        {
            var config = OVRProjectConfig.CachedProjectConfig;
            if (config.requiresSystemKeyboard) return;
            config.requiresSystemKeyboard = true; // lets Unity's TouchScreenKeyboard open the Quest system keyboard
            OVRProjectConfig.CommitProjectConfig(config);
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

        // ---- Temp step: tidy the scene hierarchy, place the spatial audio, add the visual stimulus box --------

        const float EyeLevelM = 1.2f;       // seated eye / ear height used for audio sources (runtime will use the real head height)
        const float StimulusRadiusM = 1f;

        [MenuItem("HumPer/Temp - Organize Scene Hierarchy, Place Audio, Add Visual Box", priority = 100)]
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
                else if (n == "GUI" || n == "UI Pointer" || n == "GUI Test Panel") target = GroupUI;
                else if (n.StartsWith("Visual Stimulus")) target = GroupStimuli;
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
            foreach (var g in new[] { GroupXR, GroupAudio, GroupStimuli, GroupEnvironment, GroupUI })
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

        /// <summary>Placeholder for the cardboard figure: 0.5 x 1.75 x 0.03 m box standing on the floor, 1 m straight ahead, facing the participant.</summary>
        static void AddVisualStimulusBox()
        {
            DestroyEverywhere(VisualBoxName);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = VisualBoxName;
            box.transform.SetParent(Group(GroupStimuli), false);
            box.transform.position = new Vector3(0f, 1.75f / 2f, StimulusRadiusM);
            box.transform.localScale = new Vector3(0.5f, 1.75f, 0.03f);
            box.GetComponent<Renderer>().sharedMaterial = Mat("Visual_Cardboard", new Color(0.78f, 0.65f, 0.45f));
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.SetActive(false); // hidden at the beginning; the experiment shows it per trial
        }

        // ---- helpers ------------------------------------------------------------------------------------------

        /// <summary>Mean wall absorption coefficient needed for a target RT60 in a shoebox (Sabine).</summary>
        public static float AbsorptionForRt60(float w, float d, float h, float rt60) =>
            0.161f * (w * d * h) / (2f * (w * d + w * h + d * h) * rt60);

        static bool IsGroup(string name) => name == GroupXR || name == GroupAudio || name == GroupStimuli || name == GroupEnvironment || name == GroupUI;

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
            BuildRoom(new Vector3(8f, 3.5f, 10f), MetaXRAudioRoomAcousticProperties.MaterialPreset.Concrete, bake: true);
            OrganizeHierarchy();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
