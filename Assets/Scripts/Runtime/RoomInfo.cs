using UnityEngine;

namespace HumPer
{
    /// <summary>
    /// Dimensions of the room the setup step built (meters). The visual room and the Meta XR Audio shoebox model
    /// are both generated from these values; the room is centered on the origin in x/z with the floor at y = 0.
    /// </summary>
    public class RoomInfo : MonoBehaviour
    {
        public float width = 8.72f;   // x
        public float depth = 15f;  // z
        public float height = 3.5f; // y
    }
}
