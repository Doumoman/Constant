#if LEGACY_DISABLED
using StarNight.Player.Presentation;
using UnityEngine;

namespace StarNight.Player.Lab
{
    [DisallowMultipleComponent]
    public sealed class Core03PrologueLab : MonoBehaviour
    {
        private static readonly Color Background = new Color(0.025f, 0.055f, 0.12f, 1f);
        private static readonly Color Terrain = new Color(0.08f, 0.34f, 0.38f, 1f);
        private static readonly Color Platform = new Color(0.17f, 0.52f, 0.52f, 1f);

        [SerializeField] private bool buildOnAwake = true;

        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;

        private void Awake()
        {
            if (buildOnAwake && transform.Find("Core03GeneratedRoom") == null)
            {
                BuildRoom();
            }
        }

        private void OnGUI()
        {
            if (headingStyle == null)
            {
                headingStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 21,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.78f, 0.35f, 1f) },
                };
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    normal = { textColor = new Color(0.78f, 0.92f, 0.93f, 1f) },
                };
            }

            GUI.Label(new Rect(24f, 20f, 520f, 34f), "CORE-03  이동·점프 검증 구역", headingStyle);
            GUI.Label(
                new Rect(26f, 52f, 780f, 50f),
                "← → 이동   Space 점프   X 행동   ↓+X 내려놓기   Z 폭탄   C 로프\nWASD는 기본 이동에 바인딩되지 않습니다.",
                bodyStyle);
        }

        private void BuildRoom()
        {
            GameObject room = new GameObject("Core03GeneratedRoom");
            room.transform.SetParent(transform, false);

            CreateVisual(room.transform, "Background", new Vector2(0f, 0.5f), new Vector2(24f, 12f), Background, -20, false);

            for (int index = 0; index < 5; index++)
            {
                CreateVisual(
                    room.transform,
                    $"TerrainSeam_{index}",
                    new Vector2(-8f + index * 4f, -3.5f),
                    new Vector2(4.02f, 1f),
                    Terrain,
                    0,
                    true);
            }

            CreateVisual(room.transform, "JumpPlatform", new Vector2(-1.5f, -0.5f), new Vector2(3f, 0.5f), Platform, 1, true);
            CreateVisual(room.transform, "OneCellTunnelRoof", new Vector2(5.5f, -1.5f), new Vector2(3f, 1f), Terrain, 0, true);
            CreateVisual(room.transform, "RightBoundary", new Vector2(10.5f, 0f), new Vector2(1f, 7f), Terrain, 0, true);
            CreateVisual(room.transform, "LeftBoundary", new Vector2(-10.5f, 0f), new Vector2(1f, 7f), Terrain, 0, true);
        }

        private static void CreateVisual(
            Transform parent,
            string objectName,
            Vector2 position,
            Vector2 size,
            Color color,
            int sortingOrder,
            bool collision)
        {
            GameObject item = new GameObject(objectName);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = new Vector3(position.x, position.y, 0f);
            item.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.GetWhitePixel();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (collision)
            {
                item.layer = LayerMask.NameToLayer("Ground");
                BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
            }
        }
    }
}

#endif
