using UnityEngine;
using UnityEditor;
using System.IO;

namespace VacuumVille.Editor
{
    public static class GenerateSprites
    {
        private const int S = 64; // default sprite size

        [MenuItem("VacuumVille/Regenerate Sprites")]
        public static void RegenerateAll()
        {
            DrawStar();
            DrawSock();
            DrawCushion();
            DrawToy();
            DrawCrumb();
            DrawDuck();
            DrawBlocks();
            DrawFlower();
            DrawBox();

            DrawTriangle();
            DrawSquare();
            DrawCircle();
            DrawRectangle();
            DrawPentagon();
            DrawHexagon();

            DrawStarFilled();
            DrawStarEmpty();
            DrawBadgeGold();
            DrawBadgeSilver();
            DrawBadgeBronze();

            DrawVacuumNeato();

            AssetDatabase.Refresh();
            Debug.Log("[GenerateSprites] All sprites regenerated.");
        }

        // ── helpers (size-aware) ─────────────────────────────────────────────────

        static Color32 C(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);
        static Color32 Transparent => new Color32(0, 0, 0, 0);

        static Texture2D Blank(int size = S)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Transparent;
            t.SetPixels32(px);
            return t;
        }

        static void Save(Texture2D tex, string name)
        {
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            string path = Path.Combine(Application.dataPath, "Resources/Sprites", name + ".png");
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }

        // ── drawing primitives (use texture.width for bounds) ────────────────────

        static void FillCircle(Texture2D t, int cx, int cy, int r, Color32 col)
        {
            int W = t.width, H = t.height;
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= W || y < 0 || y >= H) continue;
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    t.SetPixel(x, y, col);
            }
        }

        static void FillRect(Texture2D t, int x0, int y0, int w, int h, Color32 col)
        {
            int W = t.width, H = t.height;
            for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
            {
                if (x < 0 || x >= W || y < 0 || y >= H) continue;
                t.SetPixel(x, y, col);
            }
        }

        static void StrokeCircle(Texture2D t, int cx, int cy, int r, int thick, Color32 col)
        {
            for (int i = 0; i < thick; i++)
                DrawRing(t, cx, cy, r - i, col);
        }

        static void DrawRing(Texture2D t, int cx, int cy, int r, Color32 col)
        {
            int W = t.width, H = t.height;
            // Draw only the border pixels of the circle
            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || x >= W || y < 0 || y >= H) continue;
                int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                int inner = (r - 1) * (r - 1);
                if (d2 >= inner && d2 <= r * r)
                    t.SetPixel(x, y, col);
            }
        }

        static void FillTriangle(Texture2D t, Vector2Int a, Vector2Int b, Vector2Int c, Color32 col)
        {
            int W = t.width, H = t.height;
            int minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            int maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            for (int y = minY; y <= maxY; y++)
            {
                float xa = EdgeX(a, b, y), xb = EdgeX(b, c, y), xc = EdgeX(c, a, y);
                int x0 = Mathf.RoundToInt(Mathf.Min(xa, Mathf.Min(xb, xc)));
                int x1 = Mathf.RoundToInt(Mathf.Max(xa, Mathf.Max(xb, xc)));
                for (int x = x0; x <= x1; x++)
                {
                    if (x < 0 || x >= W || y < 0 || y >= H) continue;
                    if (PointInTriangle(new Vector2(x, y),
                            new Vector2(a.x, a.y), new Vector2(b.x, b.y), new Vector2(c.x, c.y)))
                        t.SetPixel(x, y, col);
                }
            }
        }

        static float EdgeX(Vector2Int p1, Vector2Int p2, int y)
        {
            if (p1.y == p2.y) return p1.x;
            return p1.x + (float)(y - p1.y) * (p2.x - p1.x) / (p2.y - p1.y);
        }

        static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }

        static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        static void FillPolygon(Texture2D t, Vector2Int[] pts, Color32 col)
        {
            float cx = 0, cy = 0;
            foreach (var p in pts) { cx += p.x; cy += p.y; }
            var center = new Vector2Int(Mathf.RoundToInt(cx / pts.Length), Mathf.RoundToInt(cy / pts.Length));
            for (int i = 0; i < pts.Length; i++)
                FillTriangle(t, center, pts[i], pts[(i + 1) % pts.Length], col);
        }

        static Vector2Int Polar(int cx, int cy, float r, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2Int(cx + Mathf.RoundToInt(r * Mathf.Cos(rad)),
                                  cy + Mathf.RoundToInt(r * Mathf.Sin(rad)));
        }

        // ── counting objects ────────────────────────────────────────────────────

        static void DrawStar()
        {
            var t = Blank();
            var pts = new Vector2Int[10];
            for (int i = 0; i < 10; i++)
                pts[i] = Polar(32, 32, i % 2 == 0 ? 28f : 12f, -90 + i * 36);
            FillPolygon(t, pts, C(255, 220, 0));
            Save(t, "star");
        }

        static void DrawSock()
        {
            var t = Blank();

            // Vivid blue body so the sock is clearly visible on any background
            Color32 body   = C( 50, 120, 225); // bright blue
            Color32 light  = C( 90, 160, 255); // lighter blue highlight
            Color32 dark   = C( 30,  80, 170); // darker blue shadow / heel
            Color32 cuff1  = C(220,  45,  45); // red cuff band
            Color32 cuff2  = C(255, 180, 180); // pale pink stripe
            Color32 white  = C(255, 255, 255); // toe patch

            // ── cuff at top (wider) ──
            FillRect(t, 20, 50, 24, 12, cuff1);
            FillRect(t, 20, 54, 24,  3, cuff2);
            FillRect(t, 20, 57, 24,  3, cuff1);

            // ── leg (vertical part below cuff) ──
            FillRect(t, 22, 20, 20, 32, body);

            // ── ankle-to-foot bend (filled quarter-circle) ──
            FillCircle(t, 26, 22, 11, body);
            FillCircle(t, 25, 21,  5, dark);    // heel shadow
            FillCircle(t, 26, 22,  3, light);   // heel highlight

            // ── foot (horizontal) ──
            FillRect(t,  6, 10, 32, 16, body);

            // ── toe (rounded end) ──
            FillCircle(t,  9, 18,  8, body);
            FillCircle(t,  8, 19,  4, light);   // toe highlight
            FillCircle(t,  7, 17,  2, white);   // toe specular

            Save(t, "sock");
        }

        static void DrawCushion()
        {
            var t = Blank();
            FillRect(t, 8, 14, 48, 36, C(255, 150, 200));
            FillCircle(t, 14, 20, 5, C(200, 100, 160));
            FillCircle(t, 50, 20, 5, C(200, 100, 160));
            FillCircle(t, 14, 44, 5, C(200, 100, 160));
            FillCircle(t, 50, 44, 5, C(200, 100, 160));
            FillCircle(t, 32, 32, 4, C(200, 100, 160));
            Save(t, "cushion");
        }

        static void DrawToy()
        {
            var t = Blank();
            FillRect(t, 6, 24, 52, 18, C(220, 60, 60));
            FillRect(t, 16, 14, 28, 14, C(180, 40, 40));
            FillRect(t, 19, 16, 10, 10, C(180, 220, 255));
            FillRect(t, 33, 16, 10, 10, C(180, 220, 255));
            FillCircle(t, 16, 42, 8, C(50, 50, 50));
            FillCircle(t, 16, 42, 4, C(150, 150, 150));
            FillCircle(t, 48, 42, 8, C(50, 50, 50));
            FillCircle(t, 48, 42, 4, C(150, 150, 150));
            Save(t, "toy");
        }

        static void DrawCrumb()
        {
            var t = Blank();
            FillCircle(t, 26, 28, 10, C(160, 100, 40));
            FillCircle(t, 38, 32,  7, C(140,  85, 30));
            FillCircle(t, 20, 38,  6, C(170, 110, 50));
            FillCircle(t, 40, 20,  5, C(150,  95, 35));
            FillCircle(t, 30, 20,  4, C(155, 100, 38));
            Save(t, "crumb");
        }

        static void DrawDuck()
        {
            var t = Blank();
            FillCircle(t, 32, 38, 18, C(255, 220, 30));
            FillCircle(t, 38, 22, 12, C(255, 220, 30));
            FillCircle(t, 42, 20,  3, C(20, 20, 20));
            FillCircle(t, 43, 19,  1, C(255, 255, 255));
            var beak = new[] { new Vector2Int(50, 22), new Vector2Int(57, 20), new Vector2Int(50, 18) };
            FillPolygon(t, beak, C(255, 140, 0));
            FillCircle(t, 28, 36, 8, C(230, 190, 0));
            Save(t, "duck");
        }

        static void DrawBlocks()
        {
            var t = Blank();
            FillRect(t, 12,  8, 22, 16, C(255, 80,  80));
            FillRect(t, 30,  8, 22, 16, C(80, 160, 255));
            FillRect(t,  8, 26, 22, 16, C(80, 200,  80));
            FillRect(t, 34, 26, 22, 16, C(255, 200,  0));
            FillRect(t, 20, 44, 24, 14, C(180, 80, 200));
            Save(t, "blocks");
        }

        static void DrawFlower()
        {
            var t = Blank();
            Color32 petal = C(255, 100, 180);
            FillCircle(t, 32, 16, 9, petal);
            FillCircle(t, 32, 48, 9, petal);
            FillCircle(t, 16, 32, 9, petal);
            FillCircle(t, 48, 32, 9, petal);
            FillCircle(t, 20, 20, 8, petal);
            FillCircle(t, 44, 20, 8, petal);
            FillCircle(t, 20, 44, 8, petal);
            FillCircle(t, 44, 44, 8, petal);
            FillCircle(t, 32, 32, 11, C(255, 220, 0));
            FillCircle(t, 32, 32,  6, C(200, 160, 0));
            Save(t, "flower");
        }

        static void DrawBox()
        {
            var t = Blank();
            FillRect(t, 10, 18, 44, 38, C(200, 160, 90));
            FillRect(t,  8, 12, 48, 10, C(170, 130, 60));
            FillRect(t, 28, 10,  8, 48, C(230, 200, 120));
            FillRect(t,  8, 28, 48,  8, C(230, 200, 120));
            FillRect(t, 10, 18, 44,  2, C(140, 100, 40));
            Save(t, "box");
        }

        // ── shapes ──────────────────────────────────────────────────────────────

        static void DrawTriangle()
        {
            var t = Blank();
            FillTriangle(t, new Vector2Int(32, 6), new Vector2Int(58, 56), new Vector2Int(6, 56), C(255, 100, 100));
            Save(t, "triangle");
        }

        static void DrawSquare()
        {
            var t = Blank();
            FillRect(t, 8, 8, 48, 48, C(100, 160, 255));
            Save(t, "square");
        }

        static void DrawCircle()
        {
            var t = Blank();
            FillCircle(t, 32, 32, 26, C(100, 210, 130));
            Save(t, "circle");
        }

        static void DrawRectangle()
        {
            var t = Blank();
            FillRect(t, 4, 16, 56, 32, C(255, 180, 60));
            Save(t, "rectangle");
        }

        static void DrawPentagon()
        {
            var t = Blank();
            var pts = new Vector2Int[5];
            for (int i = 0; i < 5; i++) pts[i] = Polar(32, 32, 26, -90 + i * 72);
            FillPolygon(t, pts, C(200, 100, 255));
            Save(t, "pentagon");
        }

        static void DrawHexagon()
        {
            var t = Blank();
            var pts = new Vector2Int[6];
            for (int i = 0; i < 6; i++) pts[i] = Polar(32, 32, 28, i * 60);
            FillPolygon(t, pts, C(80, 200, 220));
            Save(t, "hexagon");
        }

        // ── UI stars & badges ────────────────────────────────────────────────────

        static void DrawStarFilled()
        {
            var t = Blank();
            var pts = new Vector2Int[10];
            for (int i = 0; i < 10; i++)
                pts[i] = Polar(32, 32, i % 2 == 0 ? 28f : 12f, -90 + i * 36);
            FillPolygon(t, pts, C(255, 200, 0));
            Save(t, "star_filled");
        }

        static void DrawStarEmpty()
        {
            var t = Blank();
            var pts = new Vector2Int[10];
            for (int i = 0; i < 10; i++)
                pts[i] = Polar(32, 32, i % 2 == 0 ? 28f : 12f, -90 + i * 36);
            FillPolygon(t, pts, C(180, 180, 180));
            var inner = new Vector2Int[10];
            for (int i = 0; i < 10; i++)
                inner[i] = Polar(32, 32, i % 2 == 0 ? 22f : 9f, -90 + i * 36);
            FillPolygon(t, inner, Transparent);
            Save(t, "star_empty");
        }

        static void DrawBadge(string name, Color32 main, Color32 shine)
        {
            var t = Blank();
            FillCircle(t, 32, 34, 26, main);
            FillCircle(t, 26, 28,  8, shine);
            FillTriangle(t, new Vector2Int(14, 56), new Vector2Int(22, 42), new Vector2Int(6, 42), main);
            FillTriangle(t, new Vector2Int(50, 56), new Vector2Int(42, 42), new Vector2Int(58, 42), main);
            var pts = new Vector2Int[10];
            for (int i = 0; i < 10; i++)
                pts[i] = Polar(32, 34, i % 2 == 0 ? 14f : 6f, -90 + i * 36);
            FillPolygon(t, pts, C(255, 255, 200));
            Save(t, name);
        }

        static void DrawBadgeGold()   => DrawBadge("badge_gold",   C(220, 170,   0), C(255, 230, 100));
        static void DrawBadgeSilver() => DrawBadge("badge_silver", C(160, 160, 170), C(220, 220, 230));
        static void DrawBadgeBronze() => DrawBadge("badge_bronze", C(180, 100,  40), C(220, 160,  80));

        // ── Neato robotic vacuum — top-down D-shaped view, 128×128 ───────────────
        // Neato vacuums are D-shaped: semicircular back, flat front with brush bar.

        static void DrawVacuumNeato()
        {
            const int N  = 128;
            const int cx = 72, cy = 64; // centre shifted right so flat front has room

            var t = Blank(N);

            Color32 bodyMid   = C(185, 190, 198); // silver-grey body
            Color32 bodyLight = C(220, 225, 232); // lighter dome
            Color32 bodyDark  = C(110, 115, 124); // shadow / detail
            Color32 bumper    = C( 55,  58,  65); // very dark bumper ring
            Color32 brushDark = C( 25,  25,  30); // brush bar
            Color32 brushMid  = C( 55,  55,  60); // brush bristles
            Color32 sensor    = C( 30, 130, 215); // blue sensor
            Color32 sensorLt  = C(140, 210, 255); // sensor highlight
            Color32 wheel     = C( 20,  20,  25); // tyre rubber
            Color32 wheelRim  = C( 90,  90,  95); // rim

            // ── 1. Solid circular body (clips left later for flat front) ─────────
            FillCircle(t, cx, cy, 56, bodyMid);

            // ── 2. Clip left of x=16 → transparent (creates flat front edge) ────
            const int flatX = 16;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < flatX; x++)
                t.SetPixel(x, y, Transparent);

            // ── 3. Dark bumper ring ───────────────────────────────────────────────
            StrokeCircle(t, cx, cy, 56, 5, bumper);
            // Flat-front bumper strip
            FillRect(t, flatX, cy - 54, 5, 108, bumper);
            // Clip overhang again
            for (int y = 0; y < N; y++)
            for (int x = 0; x < flatX; x++)
                t.SetPixel(x, y, Transparent);

            // ── 4. Lighter dome (offset slightly for 3-D illusion) ───────────────
            FillCircle(t, cx + 4, cy + 2, 42, bodyLight);
            FillCircle(t, cx + 6, cy + 2, 30, C(230, 234, 240));
            // Clip again (dome fills over flatX region)
            for (int y = 0; y < N; y++)
            for (int x = 0; x < flatX; x++)
                t.SetPixel(x, y, Transparent);

            // ── 5. Brush bar at flat front ────────────────────────────────────────
            int brushW = 14;
            FillRect(t, flatX + 5, cy - 46, brushW, 92, brushDark);
            // Bristle highlights
            for (int by = cy - 42; by < cy + 42; by += 7)
                FillRect(t, flatX + 7, by, brushW - 4, 4, brushMid);

            // ── 6. Corner bumper sensors ──────────────────────────────────────────
            FillCircle(t, flatX + 12, cy - 42, 6, sensor);
            FillCircle(t, flatX + 12, cy + 42, 6, sensor);
            FillCircle(t, flatX + 10, cy - 43, 2, sensorLt);
            FillCircle(t, flatX + 10, cy + 41, 2, sensorLt);

            // ── 7. Wheel cutouts (left-right flanks) ──────────────────────────────
            // Right wheel
            FillRect(t, cx + 22, cy + 46, 16, 14, bodyDark);
            FillCircle(t, cx + 30, cy + 53, 7, wheel);
            FillCircle(t, cx + 30, cy + 53, 4, wheelRim);
            // Left wheel
            FillRect(t, cx + 22, cy - 60, 16, 14, bodyDark);
            FillCircle(t, cx + 30, cy - 53, 7, wheel);
            FillCircle(t, cx + 30, cy - 53, 4, wheelRim);

            // ── 8. LIDAR / navigation tower (centre-top) ─────────────────────────
            FillCircle(t, cx + 2, cy, 15, bodyDark);
            FillCircle(t, cx + 2, cy, 11, C(45, 48, 56));
            FillCircle(t, cx + 2, cy,  7, sensor);
            FillCircle(t, cx + 2, cy,  4, sensorLt);
            FillCircle(t, cx,     cy - 2, 1, C(255, 255, 255)); // specular

            // ── 9. Shine arc on dome ──────────────────────────────────────────────
            for (int ang = 120; ang <= 165; ang += 3)
            {
                var p = Polar(cx, cy, 46, ang);
                if (p.x >= flatX && p.x < N && p.y >= 0 && p.y < N)
                    t.SetPixel(p.x, p.y, C(255, 255, 255, 160));
            }

            // ── 10. Final clip — remove anything beyond flat front ────────────────
            for (int y = 0; y < N; y++)
            for (int x = 0; x < flatX; x++)
                t.SetPixel(x, y, Transparent);

            Save(t, "vacuum_neato");
        }
    }
}
