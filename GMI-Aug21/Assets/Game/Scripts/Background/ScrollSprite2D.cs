using UnityEngine;

namespace Oxtail.SpaceshipIncremental
{
    /// <summary>
    /// Infinite scroller for SpriteRenderer (100% 2D projects, no quads, no UI).
    /// Optimized for MOBILE / low-end devices:
    ///  - Moves ONE container transform per frame (tiles are children at rest).
    ///  - Caches the camera reference (no per-frame Camera.main lookups).
    ///  - Allocates nothing per frame (zero GC pressure).
    ///
    /// Tile renderers with the same sprite+material batch into 1 draw call
    /// (enable Dynamic Batching in the project). For ultra low-end, use a single
    /// layer with the baked backgrounds instead of the 3 parallax layers.
    ///
    /// USAGE:
    ///   1. Import the PNG as Sprite (2D and UI), Mode: Single, Pivot: Center.
    ///   2. Create an empty GameObject -> Add Component -> Sprite Renderer -> sprite.
    ///   3. Set Order in Layer (background: -30, -20, -10...) or a "Background" layer.
    ///   4. Add Component -> ScrollSprite2D -> set the speed.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ScrollSprite2D : MonoBehaviour
    {
        [Tooltip("Scroll speed in world units/second. Parallax: near layers move faster.")]
        [SerializeField] private Vector2 m_Speed = new Vector2(0.12f, 0.05f);

        [Tooltip("The background follows the camera (keep enabled if the camera moves).")]
        [SerializeField] private bool m_FollowCamera = true;

        private Transform m_Container;
        private Transform m_Camera;
        private Vector2 m_TileSize;
        private Vector2 m_Offset;
        private Vector2 m_Anchor;

        private void Start()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            m_TileSize = new Vector2(sr.bounds.size.x, sr.bounds.size.y);
            m_Anchor = transform.position;

            Camera cam = Camera.main;
            m_Camera = cam != null ? cam.transform : null;
            float viewH = cam != null ? cam.orthographicSize * 2f : m_TileSize.y;
            float viewW = cam != null ? viewH * cam.aspect : m_TileSize.x;

            // enough copies to cover the screen plus one tile of margin per side
            int tilesX = Mathf.Max(2, Mathf.CeilToInt(viewW / m_TileSize.x) + 2);
            int tilesY = Mathf.Max(2, Mathf.CeilToInt(viewH / m_TileSize.y) + 2);
            float centerX = (tilesX - 1) * 0.5f;
            float centerY = (tilesY - 1) * 0.5f;

            // one container holds every tile: moving it moves the whole grid
            GameObject container = new GameObject(name + "_container");
            m_Container = container.transform;
            m_Container.SetParent(transform.parent, false);
            m_Container.position = m_Anchor;

            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    Transform tile;
                    if (x == 0 && y == 0)
                    {
                        tile = transform; // reuse the original instance
                        tile.SetParent(m_Container, true);
                    }
                    else
                    {
                        GameObject go = new GameObject($"{name}_tile_{x}_{y}");
                        tile = go.transform;
                        SpriteRenderer r = go.AddComponent<SpriteRenderer>();
                        r.sprite = sr.sprite;
                        r.color = sr.color;
                        r.flipX = sr.flipX;
                        r.flipY = sr.flipY;
                        r.sortingLayerID = sr.sortingLayerID;
                        r.sortingOrder = sr.sortingOrder;
                        tile.SetParent(m_Container, false);
                    }
                    tile.localScale = transform.localScale;
                    tile.localRotation = transform.localRotation;
                    tile.position = new Vector3(
                        m_Anchor.x + (x - centerX) * m_TileSize.x,
                        m_Anchor.y + (y - centerY) * m_TileSize.y,
                        tile.position.z);
                }
            }
            Reposition(); // avoid a one-frame gap before the first Update
        }

        private void Update()
        {
            m_Offset += m_Speed * Time.deltaTime;
            Reposition();
        }

        private void Reposition()
        {
            float ox = Mathf.Repeat(m_Offset.x, m_TileSize.x);
            float oy = Mathf.Repeat(m_Offset.y, m_TileSize.y);

            Vector2 center = m_Anchor;
            if (m_FollowCamera && m_Camera != null)
                center = m_Camera.position;

            m_Container.position = new Vector3(center.x - ox, center.y - oy, m_Container.position.z);
        }

        private void OnDestroy()
        {
            if (m_Container != null)
                Destroy(m_Container.gameObject); // clones are children: they go with it
        }
    }
}