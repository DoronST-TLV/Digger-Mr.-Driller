using UnityEngine;

namespace Strata
{
    /// <summary>
    /// Follows the player downward only, smoothly, and sizes the camera so the grid always fills the screen width.
    /// Sits on the CameraRig; the camera itself is a child so ScreenShake can offset it independently.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform target;
        [SerializeField] private Camera cam;

        private float currentY;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            ApplyOrthoSize();
        }

        private void Start()
        {
            currentY = target.position.y - config.cameraOffsetBelowPlayer;
            transform.position = new Vector3(0f, currentY, transform.position.z);
        }

        private void LateUpdate()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight) ApplyOrthoSize();

            float desiredY = target.position.y - config.cameraOffsetBelowPlayer;
            if (desiredY < currentY)   // never up
            {
                float k = 1f - Mathf.Exp(-Time.deltaTime / config.cameraSmoothTime);
                currentY = Mathf.Lerp(currentY, desiredY, k);
            }
            transform.position = new Vector3(0f, currentY, transform.position.z);
        }

        private void ApplyOrthoSize()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            float halfWidth = config.gridWidth * 0.5f;
            cam.orthographicSize = Mathf.Max(halfWidth / cam.aspect, config.minOrthoSize);
        }
    }
}
