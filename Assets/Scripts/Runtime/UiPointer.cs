using UnityEngine;
using UnityEngine.UI;

namespace HumPer
{
    /// <summary>
    /// Controller laser for world-space uGUI. Meta's OVRInputModule relies on the legacy Input API (disabled in this project),
    /// so this does its own ray/rect hit test against every active, interactable <see cref="Selectable"/>.
    /// The hand that last pulled its index trigger owns the ray (right hand by default). The laser is only drawn while some
    /// interactable UI exists.
    /// </summary>
    public class UiPointer : MonoBehaviour
    {
        public float maxDistanceM = 6f;
        public float idleLaserLengthM = 2f;
        [Tooltip("Must support single-pass stereo (e.g. URP Unlit). Sprites/Default renders in the left eye only.")]
        public Material laserMaterial;

        LineRenderer _line;
        Selectable _hover;
        bool _useLeft;
        Transform _leftRay, _rightRay;
        float _nextSearchTime;
        OVRCameraRig _rig;

        void Awake()
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = _line.endWidth = 0.004f;
            _line.sharedMaterial = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _line.startColor = _line.endColor = Color.white;
            _line.enabled = false;
        }

        void Update()
        {
            var targets = Selectable.allSelectablesArray;
            bool any = false;
            for (int i = 0; i < targets.Length && !any; i++) any = IsUsable(targets[i]);
            if (!any)
            {
                _line.enabled = false;
                SetHover(null);
                return;
            }

            bool leftDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            bool rightDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            if (leftDown && !rightDown) _useLeft = true;
            else if (rightDown) _useLeft = false;

            Transform origin = _useLeft ? Resolve(OVRInput.Controller.LTouch, ref _leftRay) : Resolve(OVRInput.Controller.RTouch, ref _rightRay);
            if (origin == null) { _line.enabled = false; return; }

            var ray = new Ray(origin.position, origin.forward);
            Selectable hit = Raycast(ray, targets, out Vector3 hitPoint);
            SetHover(hit);

            _line.enabled = true;
            _line.SetPosition(0, ray.origin);
            _line.SetPosition(1, hit != null ? hitPoint : ray.origin + ray.direction * idleLaserLengthM);

            if ((leftDown || rightDown) && hit is Button button) button.onClick.Invoke();
        }

        static bool IsUsable(Selectable s) => s != null && s.isActiveAndEnabled && s.interactable;

        Selectable Raycast(Ray ray, Selectable[] targets, out Vector3 point)
        {
            Selectable best = null;
            float bestT = maxDistanceM;
            point = Vector3.zero;
            foreach (var s in targets)
            {
                if (!IsUsable(s)) continue;
                var rt = s.transform as RectTransform;
                if (rt == null) continue;
                Vector3 n = rt.forward;
                float denom = Vector3.Dot(ray.direction, n);
                if (Mathf.Abs(denom) < 1e-5f) continue;
                float t = Vector3.Dot(rt.position - ray.origin, n) / denom;
                if (t <= 0f || t >= bestT) continue;
                Vector3 p = ray.origin + ray.direction * t;
                if (rt.rect.Contains(rt.InverseTransformPoint(p))) { best = s; bestT = t; point = p; }
            }
            return best;
        }

        void SetHover(Selectable s)
        {
            if (_hover == s) return;
            if (_hover != null) _hover.OnPointerExit(null);
            _hover = s;
            if (_hover != null) _hover.OnPointerEnter(null);
        }

        /// <summary>
        /// Ray origin = the active controller model's "laser_begin" transform (from the Controller Tracking building block's
        /// OVRControllerHelper), else the helper itself, else the camera rig's controller anchor.
        /// </summary>
        Transform Resolve(OVRInput.Controller hand, ref Transform cache)
        {
            if (cache != null && cache.gameObject.activeInHierarchy) return cache;
            if (Time.unscaledTime < _nextSearchTime) return null;
            _nextSearchTime = Time.unscaledTime + 0.5f;

            cache = null;
            foreach (var helper in FindObjectsByType<OVRControllerHelper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (helper.m_controller != hand) continue;
                foreach (var t in helper.GetComponentsInChildren<Transform>(false))
                    if (t.name.EndsWith("laser_begin")) { cache = t; break; }
                if (cache == null) cache = helper.GetPointerRayTransform();
                break;
            }
            if (cache == null)
            {
                if (_rig == null) _rig = FindFirstObjectByType<OVRCameraRig>();
                if (_rig != null) cache = hand == OVRInput.Controller.LTouch ? _rig.leftControllerAnchor : _rig.rightControllerAnchor;
            }
            return cache;
        }
    }
}
