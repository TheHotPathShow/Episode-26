using UnityEngine;

namespace TMG.RacingRoyale
{
    public class CameraTarget : MonoBehaviour
    {
        public static CameraTarget Instance;

        private void Awake()
        {
            Instance = this;
        }
    }
}