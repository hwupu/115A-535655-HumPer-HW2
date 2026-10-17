using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HumPer.EditorTools
{
    /// <summary>
    /// Step-by-step scene setup, one idempotent menu item per step. Each step edits the OPEN scene only and never
    /// touches the Meta Building Block objects (Camera Rig, Controller Tracking, Spatial Audio). Save the scene afterwards.
    /// </summary>
    public static class HumPerSetup
    {
        const string RootName = "HumPer";
        const string PanelName = "GUI Test Panel";
        const string LightingSettingsPath = "Assets/Settings/ExperimentLighting.lighting";

        // ---- Step: global illumination instead of a directional light -----------------------------------------

        [MenuItem("HumPer/Step 1 - Lighting: Global Illumination (remove directional light)")]
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
            Debug.Log("[HumPer] Lighting: removed " + removed + " directional light(s); flat ambient + baked GI settings applied (nothing to bake until the room exists).");
        }

        // ---- Step: empty GUI panel to test visibility / placement ---------------------------------------------

        [MenuItem("HumPer/Step 2 - GUI: Add empty test panel (OVROverlayCanvas)")]
        public static void AddTestPanel()
        {
            EnsureLayerName(GuiPanelPlacer.HiddenCanvasLayer, "HiddenOverlayCanvas");

            var root = GameObject.Find(RootName) ?? new GameObject(RootName);
            var existing = root.transform.Find(PanelName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject(PanelName, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(root.transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1000, 700);
            rt.localScale = Vector3.one * 0.002f; // 1000 px = 2 m, 700 px = 1.4 m
            go.transform.position = new Vector3(0f, 1.2f, 2f); // editor preview; GuiPanelPlacer moves it at runtime

            // Empty panel: a solid background only.
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 1f);

            go.AddComponent<OVROverlayCanvas>();
            go.AddComponent<GuiPanelPlacer>();

            // The source canvas lives on a layer that the eye cameras do not render; only the overlay shows it.
            go.GetComponent<OVROverlayCanvas>().SetCanvasLayer(GuiPanelPlacer.HiddenCanvasLayer, true);
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                cam.cullingMask &= ~((1 << GuiPanelPlacer.HiddenCanvasLayer) | (1 << GuiPanelPlacer.OverlayRenderLayer));

            MarkSceneDirty();
            Debug.Log("[HumPer] Added '" + PanelName + "' under '" + RootName + "'.");
        }

        // ---- helpers ------------------------------------------------------------------------------------------

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

        static void MarkSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        /// <summary>Headless entry for verification: runs both steps and saves the scene.</summary>
        public static void RunAllHeadless()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ExperimentScene.unity");
            SetupLighting();
            AddTestPanel();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
