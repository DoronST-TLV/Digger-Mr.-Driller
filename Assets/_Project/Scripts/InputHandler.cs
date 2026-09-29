using UnityEngine;
using UnityEngine.EventSystems;

namespace Strata
{
    /// <summary>
    /// Turns keyboard, mouse and touch into one intent per frame for the player.
    /// A tap is classified by where it is relative to the player: left / right / below. Above does nothing.
    /// Input.GetMouseButtonDown covers both the mouse and the first touch on Android.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Camera gameCamera;

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.State != GameState.Playing) return;
            if (game.StateChangedFrame == Time.frameCount) return;   // the tap that started the game is not a dig

            Vector2Int direction = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) direction = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) direction = Vector2Int.right;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) direction = GridManager.Down;
            else if (Input.GetMouseButtonDown(0) && !IsPointerOverUI()) direction = DirectionFromPointer(Input.mousePosition);

            if (direction != Vector2Int.zero) player.SetIntent(direction);
        }

        private Vector2Int DirectionFromPointer(Vector3 screenPosition)
        {
            Vector3 playerOnScreen = gameCamera.WorldToScreenPoint(player.transform.position);
            float dx = screenPosition.x - playerOnScreen.x;
            float dy = screenPosition.y - playerOnScreen.y;
            if (Mathf.Abs(dx) > Mathf.Abs(dy)) return dx < 0f ? Vector2Int.left : Vector2Int.right;
            return dy < 0f ? GridManager.Down : Vector2Int.zero;
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
