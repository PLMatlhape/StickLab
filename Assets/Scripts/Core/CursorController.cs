using UnityEngine;

namespace StickLab.Core
{
    public class CursorController : MonoBehaviour
    {
        [Header("Cursor")]
        [SerializeField] private Transform cursorVisual;
        [SerializeField] private Rect playArea = new Rect(-4f, -2.5f, 8f, 5f);
        [SerializeField, Min(0f)] private float sensitivity = 8f;
        [SerializeField] private float fixedZ = 0f;

        public Vector2 Position2D { get; private set; }

        private Transform ActiveTransform => cursorVisual != null ? cursorVisual : transform;

        private void Awake()
        {
            Vector3 startPosition = ActiveTransform.position;
            Position2D = new Vector2(startPosition.x, startPosition.y);
            fixedZ = startPosition.z;
        }

        private void Start()
        {
            SetPosition(Position2D);
        }

        public void Tick(Vector2 stickInput, float deltaTime, float sensitivityMultiplier = 1f)
        {
            float effectiveSensitivity = sensitivity * Mathf.Max(0f, sensitivityMultiplier);
            Vector2 nextPosition = Position2D + (stickInput * effectiveSensitivity * deltaTime);
            Position2D = ClampToPlayArea(nextPosition);
            ApplyToTransform();
        }

        public void SetPosition(Vector2 worldPosition)
        {
            Position2D = ClampToPlayArea(worldPosition);
            ApplyToTransform();
        }

        public void SetPlayArea(Rect newPlayArea)
        {
            playArea = newPlayArea;
            Position2D = ClampToPlayArea(Position2D);
            ApplyToTransform();
        }

        public void SetSensitivity(float newSensitivity)
        {
            sensitivity = Mathf.Max(0f, newSensitivity);
        }

        private Vector2 ClampToPlayArea(Vector2 position)
        {
            float x = Mathf.Clamp(position.x, playArea.xMin, playArea.xMax);
            float y = Mathf.Clamp(position.y, playArea.yMin, playArea.yMax);
            return new Vector2(x, y);
        }

        private void ApplyToTransform()
        {
            ActiveTransform.position = new Vector3(Position2D.x, Position2D.y, fixedZ);
        }
    }
}
