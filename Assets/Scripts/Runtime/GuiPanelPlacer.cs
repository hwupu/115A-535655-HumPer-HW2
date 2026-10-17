using System.Collections;
using UnityEngine;

namespace HumPer
{
    /// <summary>
    /// Puts a GUI canvas in front of the participant: <see cref="distanceM"/> ahead of the rig's forward direction
    /// at head height (+ <see cref="heightOffsetM"/>), facing the participant. Placement is done once at start,
    /// after head tracking is valid (the first frames report (0,0,0), which would put the panel on the floor).
    /// Also keeps the overlay-canvas source layers out of the eye cameras (see HumPerSetup).
    /// </summary>
    public class GuiPanelPlacer : MonoBehaviour
    {
        /// <summary>Layer holding the source uGUI of an OVROverlayCanvas; the eye cameras must not render it.</summary>
        public const int HiddenCanvasLayer = 30;
        /// <summary>OVROverlayCanvasSettings.CanvasRenderLayer, used internally by the canvas camera.</summary>
        public const int OverlayRenderLayer = 31;

        public float distanceM = 2f;
        public float heightOffsetM = 0f;
        public float maxWaitSec = 5f;

        IEnumerator Start()
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig == null)
            {
                Debug.LogError("[HumPer] GuiPanelPlacer: no OVRCameraRig in the scene (add the Camera Rig building block).");
                yield break;
            }

            foreach (var cam in rig.GetComponentsInChildren<Camera>(true))
                cam.cullingMask &= ~((1 << HiddenCanvasLayer) | (1 << OverlayRenderLayer));

            // Wait for valid head tracking (head above the rig origin).
            var head = rig.centerEyeAnchor;
            float waited = 0f;
            while (head.position.y - rig.transform.position.y < 0.3f && waited < maxWaitSec)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Place(rig);
        }

        /// <summary>Place (or re-place) the canvas in front of the rig, e.g. after a recenter.</summary>
        [ContextMenu("Place now")]
        public void Place()
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null) Place(rig);
        }

        void Place(OVRCameraRig rig)
        {
            var head = rig.centerEyeAnchor;
            var yaw = Quaternion.Euler(0f, rig.transform.eulerAngles.y, 0f);
            transform.position = head.position + yaw * Vector3.forward * distanceM + Vector3.up * heightOffsetM;
            transform.rotation = yaw; // same yaw as the rig: the canvas +Z axis points away from the participant, so its front faces them
            Debug.Log("[HumPer] GUI panel placed at " + transform.position.ToString("F2") +
                      " (head " + head.position.ToString("F2") + ", rig yaw " + yaw.eulerAngles.y.ToString("F0") + ")");
        }
    }
}
