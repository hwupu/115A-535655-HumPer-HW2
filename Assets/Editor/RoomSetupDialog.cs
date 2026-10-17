using UnityEditor;
using UnityEngine;

namespace HumPer.EditorTools
{
    /// <summary>HumPer > Step 2: room size and shoebox material. One size feeds both the visual room and the Meta XR Audio shoebox.</summary>
    public class RoomSetupDialog : EditorWindow
    {
        Vector3 _size;
        MetaXRAudioRoomAcousticProperties.MaterialPreset _material;
        bool _bake = true;

        public static void Open()
        {
            var w = GetWindow<RoomSetupDialog>(true, "Room Setup", true);
            w._size = HumPerSetup.CurrentRoomSize();     // x = width, y = height, z = depth
            w._material = HumPerSetup.CurrentRoomMaterial();
            w.minSize = w.maxSize = new Vector2(400, 270);
            w.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Builds (or rebuilds) Environment/Room: floor, ceiling, four walls, a soft baked ceiling light, " +
                                    "and Environment/Shoebox Acoustics (Meta XR Audio) with the same dimensions and no clutter.", MessageType.Info);
            _size.x = Mathf.Max(1f, EditorGUILayout.FloatField("Width (x, m)", _size.x));
            _size.z = Mathf.Max(1f, EditorGUILayout.FloatField("Depth (z, m)", _size.z));
            _size.y = Mathf.Max(1.5f, EditorGUILayout.FloatField("Height (y, m)", _size.y));
            _material = (MetaXRAudioRoomAcousticProperties.MaterialPreset)EditorGUILayout.EnumPopup("Shoebox material (all 6)", _material);
            _bake = EditorGUILayout.Toggle("Bake lighting after build", _bake);

            float need = HumPerSetup.AbsorptionForRt60(_size.x, _size.z, _size.y, 0.7f);
            EditorGUILayout.LabelField("Mean absorption needed for RT60 0.7 s (Sabine): " + need.ToString("0.000"), EditorStyles.miniLabel);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel")) Close();
                if (GUILayout.Button("Build Room"))
                {
                    var size = _size; var material = _material; bool bake = _bake;
                    Close();
                    EditorApplication.delayCall += () => HumPerSetup.BuildRoom(size, material, bake);
                }
            }
        }
    }
}
