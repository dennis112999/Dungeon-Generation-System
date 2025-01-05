using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGeneration
{
    public class RoomManager : MonoBehaviour
    {
        [Header("Room Prefab")]
        [SerializeField] private GameObject _roomPrefab;

        [Header("Room Generation Settings")]
        [SerializeField, Range(15, 100)] private int _maxRooms = 15;
        [SerializeField, Range(7, 50)] private int _minRooms = 7;

        [Header("Grid Settings")]
        [SerializeField, Range(5, 50)] private int _gridSizeX = 10;
        [SerializeField, Range(5, 50)] private int _gridSizeY = 10;
        private int _roomWidth = 20;
        private int _roomHeight = 12;

        [Header("Generation Status")]
        private List<Room> _roomObjects = new List<Room>();              // List to store instantiated rooms
        private Queue<Vector2Int> _roomQueue = new Queue<Vector2Int>();  // Queue for room generation
        private int[,] _roomGrid;                                        // 2D grid to track room positions
        private int _roomCount;                                          // Counter for generated rooms
        private bool _generationComplete = false;                        // Flag to indicate if generation is complete

        #region MonoBehaviour

        void Start()
        {
            Initialize();
            StartCoroutine(GenerateRoomsCoroutine());
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color gizmosColor = Color.red;
            Gizmos.color = gizmosColor;

            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    Vector3 pos = GetPositionFromGridIndex(new Vector2Int(x, y));
                    Gizmos.DrawWireCube(new Vector3(pos.x, pos.y), new Vector3(_roomWidth, _roomHeight, 1));
                }
            }
        }

        private void OnGUI()
        {
            Rect buttonRect = new Rect(10, 10, 150, 50);

            if (GUI.Button(buttonRect, "Regenerate Rooms"))
            {
                StopAllCoroutines();
                RegenerateRooms();
                StartCoroutine(DelayedStartGeneration(0.1f));
            }
        }
#endif

        #endregion MonoBehaviour

        private void Initialize()
        {
            _roomGrid = new int[_gridSizeX, _gridSizeY];
            _roomQueue = new Queue<Vector2Int>();

            Vector2Int initialRoomIndex = new Vector2Int(_gridSizeX / 2, _gridSizeY / 2);

            StartRoomGenerationFromRoom(initialRoomIndex);
        }

        private IEnumerator GenerateRoomsCoroutine()
        {
            while (_roomQueue.Count > 0 && _roomCount < _maxRooms && !_generationComplete)
            {
                Vector2Int roomIndex = _roomQueue.Dequeue();
                int gridX = roomIndex.x;
                int gridY = roomIndex.y;

                TryGenerationRoom(new Vector2Int(gridX - 1, gridY)); // Left
                TryGenerationRoom(new Vector2Int(gridX + 1, gridY)); // Right
                TryGenerationRoom(new Vector2Int(gridX, gridY - 1)); // Down
                TryGenerationRoom(new Vector2Int(gridX, gridY + 1)); // Up

                yield return new WaitForSeconds(0.1f);
            }

            _generationComplete = true;
        }

        private void StartRoomGenerationFromRoom(Vector2Int roomIndex)
        {
            _roomQueue.Enqueue(roomIndex);

            _roomGrid[roomIndex.x, roomIndex.y] = 1;
            _roomCount++;

            var newRoom = InstantiateRoom(roomIndex);
            _roomObjects.Add(newRoom);
        }

        private bool TryGenerationRoom(Vector2Int roomIndex)
        {
            // Validate room position
            if (!IsWithinGridBounds(roomIndex) || _roomGrid[roomIndex.x, roomIndex.y] != 0)
                return false;

            // Check room generation constraints
            if (_roomCount >= _maxRooms || ShouldSkipRoomGeneration(roomIndex) || CountAdjacentRooms(roomIndex) > 1)
                return false;

            // Add the room to the grid and queue
            _roomQueue.Enqueue(roomIndex);
            _roomGrid[roomIndex.x, roomIndex.y] = 1;
            _roomCount++;

            // Instantiate and register the room
            var newRoom = InstantiateRoom(roomIndex);
            _roomObjects.Add(newRoom);

            // Open doors to adjacent rooms
            OpenDoors(newRoom, roomIndex.x, roomIndex.y);

            return true;
        }

        /// <summary>
        /// Counts the number of adjacent rooms around the specified room index.
        /// </summary>
        /// <param name="roomIndex">The grid index of the room to check.</param>
        /// <returns>The number of adjacent rooms.</returns>
        private int CountAdjacentRooms(Vector2Int roomIndex)
        {
            int x = roomIndex.x;
            int y = roomIndex.y;
            int count = 0;

            if (x > 0 && _roomGrid[x - 1, y] != 0) count++;
            if (x < _gridSizeX - 1 && _roomGrid[x + 1, y] != 0) count++;
            if (y > 0 && _roomGrid[x, y - 1] != 0) count++;
            if (y < _gridSizeY - 1 && _roomGrid[x, y + 1] != 0) count++;

            return count;
        }

        /// <summary>
        /// Opens doors between the specified room and its adjacent rooms.
        /// </summary>
        /// <param name="room">The current room for which doors should be opened.</param>
        /// <param name="x">The X coordinate of the current room in the grid.</param>
        /// <param name="y">The Y coordinate of the current room in the grid.</param>
        private void OpenDoors(Room room, int x, int y)
        {
            Room leftRoom = GetRoomAt(new Vector2Int(x - 1, y));
            Room rightRoom = GetRoomAt(new Vector2Int(x + 1, y));
            Room bottomRoom = GetRoomAt(new Vector2Int(x, y - 1));
            Room topRoom = GetRoomAt(new Vector2Int(x, y + 1));

            if (x > 0 && _roomGrid[x - 1, y] != 0)
            {
                room.OpenDoor(Vector2Int.left);
                leftRoom?.OpenDoor(Vector2Int.right);
            }
            if (x < _gridSizeX - 1 && _roomGrid[x + 1, y] != 0)
            {
                room.OpenDoor(Vector2Int.right);
                rightRoom?.OpenDoor(Vector2Int.left);
            }
            if (y > 0 && _roomGrid[x, y - 1] != 0)
            {
                room.OpenDoor(Vector2Int.down);
                bottomRoom?.OpenDoor(Vector2Int.up);
            }
            if (y < _gridSizeY - 1 && _roomGrid[x, y + 1] != 0)
            {
                room.OpenDoor(Vector2Int.up);
                topRoom?.OpenDoor(Vector2Int.down);
            }
        }

        /// <summary>
        /// Clear all rooms and try again
        /// </summary>
        private void RegenerateRooms()
        {
            _roomObjects.ForEach(room => Destroy(room.gameObject));
            _roomObjects.Clear();

            _roomGrid = new int[_gridSizeX, _gridSizeY];
            _roomQueue.Clear();
            _roomCount = 0;

            _generationComplete = false;

            Vector2Int initialRoomIndex = new Vector2Int(_gridSizeX / 2, _gridSizeY / 2);
            StartRoomGenerationFromRoom(initialRoomIndex);
        }

        private Room GetRoomAt(Vector2Int roomIndex)
        {
            return _roomObjects.Find(r => r.RoomIndex == roomIndex);
        }

        /// <summary>
        /// Instantiate room
        /// </summary>
        /// <param name="roomIndex">The grid index where the room will be instantiated</param>
        /// <returns>instantiated Room object or null</returns>
        private Room InstantiateRoom(Vector2Int roomIndex)
        {
            var newRoom = Instantiate(_roomPrefab, GetPositionFromGridIndex(roomIndex), Quaternion.identity);
            newRoom.transform.SetParent(transform, false);
            newRoom.name = $"Room {_roomCount}";

            if (newRoom.TryGetComponent<Room>(out Room room))
            {
                room.RoomIndex = roomIndex;
                return room;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Room component is missing on the instantiated prefab: {newRoom.name}");
#endif
                return null;
            }
        }

        private Vector3 GetPositionFromGridIndex(Vector2Int gridIndex)
        {
            int gridX = gridIndex.x;
            int gridY = gridIndex.y;
            return new Vector3(_roomWidth * (gridX - _gridSizeX / 2),
                _roomHeight * (gridY - _gridSizeY / 2));
        }

        #region Room Validation and Utility Methods

        /// <summary>
        /// Checks if the given room index is within the bounds of the grid.
        /// </summary>
        /// <param name="roomIndex">The grid index of the room to check.</param>
        /// <returns>True if the index is within bounds; otherwise, false.</returns>
        private bool IsWithinGridBounds(Vector2Int roomIndex)
        {
            int x = roomIndex.x;
            int y = roomIndex.y;
            return x >= 0 && x < _gridSizeX && y >= 0 && y < _gridSizeY;
        }

        /// <summary>
        /// Determines if the room generation at the given index should be skipped.
        /// </summary>
        /// <param name="roomIndex">The grid index of the room to check.</param>
        /// <returns>True if the room generation should be skipped; otherwise, false.</returns>
        private bool ShouldSkipRoomGeneration(Vector2Int roomIndex)
        {
            return Random.value < 0.3f && roomIndex != Vector2Int.zero;
        }

        #endregion


#if UNITY_EDITOR
        /// <summary>
        /// Delayed Start Generation
        /// </summary>
        /// <param name="delay">The duration (in seconds)</param>
        /// <returns></returns>
        private IEnumerator DelayedStartGeneration(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartCoroutine(GenerateRoomsCoroutine());
        }
#endif
    }
}
