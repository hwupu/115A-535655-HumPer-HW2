using Meta.XR.Audio;
using UnityEngine;

namespace HumPer
{
    /// <summary>
    /// Room acoustics for the Reverb condition.
    /// 1. Turns Meta XR Audio's room modeling and late reverberation on. Both are OFF by default in the plugin; a Unity project normally
    ///    switches them on with the "Meta XR Audio Reflection" effect on an audio mixer group (this project has one, made by Step 4).
    /// 2. Sets the reverberation time. Meta's shoebox component only offers material presets, so the component is switched off and the
    ///    room is sent to the plugin from here, with one uniform reflection coefficient on all six surfaces. The coefficient for a target
    ///    RT60 comes from a MEASURED table (<see cref="Rt60Calibration"/>), not from a formula: Eyring's formula was off by 20-170 %.
    ///    <see cref="UsePreset"/> hands control back to the component (its material preset). Check the result on the headset with the Debug
    ///    bench "Measure RT60" (<see cref="Rt60Meter"/>).
    /// </summary>
    public class ReverbTuner : MonoBehaviour
    {
        public MetaXRAudioRoomAcousticProperties room;
        public RoomInfo roomInfo;

        public bool Custom { get; private set; }
        public float TargetRt60 { get; private set; }
        public float Reflection { get; private set; }

        /// <summary>Result of switching room modeling on (plugin result codes, 0 = success), for the log and the Debug bench.</summary>
        public string RoomModelingStatus { get; private set; } = "not set";

        readonly float[] _walls = new float[6 * 4];
        readonly float[] _clutter = new float[4];

        void Awake() => EnableRoomModeling();

        public void EnableRoomModeling()
        {
            var plugin = MetaXRAudioNativeInterface.Interface;
            int roomResult = plugin.SetEnabled((int)EnableFlag.SIMPLE_ROOM_MODELING, true);
            int lateResult = plugin.SetEnabled((int)EnableFlag.LATE_REVERBERATION, true);
            RoomModelingStatus = "room modeling " + (roomResult == 0 ? "on" : "FAILED " + roomResult) + ", late reverb " + (lateResult == 0 ? "on" : "FAILED " + lateResult);
            Debug.Log("[HumPer] Reverb: " + RoomModelingStatus);
        }

        public void SetRt60(float seconds)
        {
            if (this.room == null) return;
            ComputeRoom(out float v, out float s);
            TargetRt60 = Mathf.Max(0.05f, seconds);
            Reflection = Rt60Calibration.ReflectionForRt60(TargetRt60, v, s);
            for (int i = 0; i < _walls.Length; i++) _walls[i] = Reflection;
            Custom = true;
            this.room.enabled = false; // its Update would overwrite the room every frame
            Push();
            Debug.Log("[HumPer] Reverb: custom RT60 " + TargetRt60.ToString("0.00") + " s (calibrated), reflection " + Reflection.ToString("0.000"));
        }

        /// <summary>Set the uniform reflection coefficient directly (calibration experiments). RT60 target is unknown then.</summary>
        public void SetReflection(float reflection)
        {
            if (room == null) return;
            Reflection = Mathf.Clamp01(reflection);
            TargetRt60 = 0f;
            for (int i = 0; i < _walls.Length; i++) _walls[i] = Reflection;
            Custom = true;
            room.enabled = false;
            Push();
        }

        public void UsePreset()
        {
            Custom = false;
            if (room != null) room.enabled = true;
        }

        /// <summary>Calculated RT60 of the Meta material preset(s) in the scene (mean over the six surfaces and four bands).</summary>
        public float PresetRt60()
        {
            if (room == null) return 0f;
            var presets = new[] { room.leftMaterial, room.rightMaterial, room.ceilingMaterial, room.floorMaterial, room.frontMaterial, room.backMaterial };
            float sum = 0f;
            foreach (var preset in presets)
                foreach (float band in MetaXRAudioRoomAcousticProperties.GetMaterialPresetBands(preset)) sum += band;
            ComputeRoom(out float v, out float s);
            return Rt60Calibration.Rt60ForReflection(sum / (presets.Length * 4), v, s);
        }

        public string Describe() => Custom
            ? "RT60 " + TargetRt60.ToString("0.00") + " s (calibrated, R " + Reflection.ToString("0.000") + ")"
            : "Meta preset " + (room != null ? room.floorMaterial.ToString() : "?") + ", RT60 about " + PresetRt60().ToString("0.0") + " s (calibrated estimate)";

        void Update()
        {
            if (Custom) Push();
        }

        void OnDisable() => UsePreset();

        void ComputeRoom(out float v, out float s)
        {
            float w = roomInfo != null ? roomInfo.width : room.width;
            float d = roomInfo != null ? roomInfo.depth : room.depth;
            float h = roomInfo != null ? roomInfo.height : room.height;
            RoomAcousticsMath.BoxVolumeSurface(w, d, h, out v, out s);
        }

        void Push()
        {
            MetaXRAudioNativeInterface.Interface.SetAdvancedBoxRoomParameters(room.width, room.height, room.depth,
                room.lockPositionToListener, room.transform.position, _walls);
            float factor = room.clutterFactor; // same band spreading as Meta's component
            for (int band = _clutter.Length - 1; band >= 0; --band) { _clutter[band] = factor; factor *= 0.5f; }
            MetaXRAudioNativeInterface.Interface.SetRoomClutterFactor(_clutter);
        }
    }
}
