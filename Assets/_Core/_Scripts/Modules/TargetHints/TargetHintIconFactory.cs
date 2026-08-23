using UnityEngine;

namespace Modules.TargetHints
{
    /// <summary>
    /// Builds small unique sprites for vehicle, passenger and exit hints.
    /// </summary>
    public static class TargetHintIconFactory
    {
        public static Sprite Create(TargetHintKind kind)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"HintIcon_{kind}"
            };

            Clear(texture);
            switch (kind)
            {
                case TargetHintKind.Vehicle:
                    DrawCar(texture);
                    break;
                case TargetHintKind.Exit:
                    DrawExit(texture);
                    break;
                default:
                    DrawPerson(texture);
                    break;
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 32f, 32f),
                new Vector2(0.5f, 0.5f),
                32f);
            sprite.name = texture.name;
            return sprite;
        }

        private static void Clear(Texture2D texture)
        {
            var empty = new Color32(0, 0, 0, 0);
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                    texture.SetPixel(x, y, empty);
            }
        }

        private static void DrawCar(Texture2D texture)
        {
            var body = new Color32(255, 230, 80, 255);
            var cabin = new Color32(40, 50, 70, 255);
            var wheel = new Color32(20, 20, 20, 255);

            FillRect(texture, 4, 10, 24, 10, body);
            FillRect(texture, 9, 16, 14, 7, cabin);
            FillRect(texture, 6, 6, 6, 6, wheel);
            FillRect(texture, 20, 6, 6, 6, wheel);
        }

        private static void DrawPerson(Texture2D texture)
        {
            var color = new Color32(255, 255, 255, 255);
            FillCircle(texture, 16, 25, 4, color);
            FillRect(texture, 14, 10, 4, 12, color);
            FillRect(texture, 8, 16, 16, 3, color);
            FillRect(texture, 11, 4, 4, 8, color);
            FillRect(texture, 17, 4, 4, 8, color);
        }

        private static void DrawExit(Texture2D texture)
        {
            var frame = new Color32(255, 255, 255, 255);
            var door = new Color32(40, 180, 90, 255);
            var knob = new Color32(255, 220, 80, 255);

            FillRect(texture, 8, 4, 16, 24, frame);
            FillRect(texture, 10, 6, 12, 20, door);
            FillRect(texture, 18, 14, 2, 4, knob);
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            for (var py = y; py < y + height; py++)
            {
                for (var px = x; px < x + width; px++)
                {
                    if (px >= 0 && px < 32 && py >= 0 && py < 32)
                        texture.SetPixel(px, py, color);
                }
            }
        }

        private static void FillCircle(Texture2D texture, int cx, int cy, int radius, Color32 color)
        {
            var r2 = radius * radius;
            for (var y = cy - radius; y <= cy + radius; y++)
            {
                for (var x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= 32 || y < 0 || y >= 32)
                        continue;

                    var dx = x - cx;
                    var dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                        texture.SetPixel(x, y, color);
                }
            }
        }
    }
}
