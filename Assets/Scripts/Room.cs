using UnityEngine;

namespace DungeonGeneration
{
    public class Room : MonoBehaviour
    {
        [Header("Doors")]
        [SerializeField] private GameObject _topDoor;
        [SerializeField] private GameObject _bottomDoor;
        [SerializeField] private GameObject _leftDoor;
        [SerializeField] private GameObject _rightDoor;

        public Vector2Int RoomIndex { get; set; }

        /// <summary>
        /// Opens the door in the specified direction.
        /// </summary>
        /// <param name="direction">The direction of the door to open.</param>
        public void OpenDoor(Vector2Int direction)
        {
            switch (direction)
            {
                case Vector2Int v when v == Vector2Int.up:
                    _topDoor?.SetActive(true);
                    break;

                case Vector2Int v when v == Vector2Int.down:
                    _bottomDoor?.SetActive(true);
                    break;

                case Vector2Int v when v == Vector2Int.left:
                    _leftDoor?.SetActive(true);
                    break;

                case Vector2Int v when v == Vector2Int.right:
                    _rightDoor?.SetActive(true);
                    break;

                default:
#if UNITY_EDITOR
                    Debug.LogWarning($"Invalid direction {direction} passed to OpenDoor.");
#endif
                    break;
            }
        }
    }
}
