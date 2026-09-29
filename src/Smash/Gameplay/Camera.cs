using System;
using Microsoft.Xna.Framework;

namespace Smash
{
    //anything the camera can follow
    interface IFocus
    {
        Vector2 Position { get; }
        //the zoom level that would keep everything important on screen
        float DesiredZoom { get; }
    }

    //follows the middle of all fighters and zooms out as they move apart
    class MainFocus : IFocus
    {
        const float PaddingX = 260;
        const float PaddingY = 220;
        const float MaxZoom = 1.3f;

        readonly Match match;

        public Vector2 Position { get; private set; }
        public float DesiredZoom { get; private set; } = 1;

        public MainFocus(Match match)
        {
            this.match = match;
            Position = match.Stage.Deck.Center;
            match.event_update += Update;
        }

        public void Update()
        {
            float left = float.MaxValue, right = float.MinValue, top = float.MaxValue, bottom = float.MinValue;
            Box visible = match.Stage.Bounds;
            foreach (Fighter f in match.Fighters)
            {
                if (f.State == FighterState.Dead)
                    continue;
                //fighters flying towards the blast zone should not drag the camera outside the stage
                Vector2 p = Vector2.Clamp(f.Position, new Vector2(visible.Left, visible.Top), new Vector2(visible.Right, visible.Bottom));
                left = Math.Min(left, p.X);
                right = Math.Max(right, p.X);
                top = Math.Min(top, p.Y - f.Character.BodyHeight);
                bottom = Math.Max(bottom, p.Y);
            }
            if (left == float.MaxValue)
            {
                Position = match.Stage.Deck.Center;
                DesiredZoom = 1;
                return;
            }
            Position = new Vector2((left + right) / 2, (top + bottom) / 2);
            Vector2 vp = match.LayoutViewport;
            float zoomX = vp.X / (right - left + PaddingX * 2);
            float zoomY = vp.Y / (bottom - top + PaddingY * 2);
            DesiredZoom = Math.Min(MaxZoom, Math.Min(zoomX, zoomY));
        }
    }

    //a matrix camera that eases towards its focus and never shows anything outside the stage bounds
    class Camera
    {
        const float MoveEase = 0.08f;
        const float ZoomEase = 0.05f;

        readonly Match match;
        readonly IFocus focus;
        Vector2 position;
        float zoom = 1;
        float shake;
        readonly Random rng = new Random();

        public Matrix Transform { get; private set; } = Matrix.Identity;
        public float Zoom => zoom;

        public Camera(IFocus focus, Match match)
        {
            this.focus = focus;
            this.match = match;
            position = focus.Position;
            match.event_update += Update;
            //compute the matrix now, so a frame drawn before the first tick is already framed correctly
            Update();
        }

        public void Shake(float amount) => shake = Math.Max(shake, amount);

        public void Update()
        {
            Box bounds = match.Stage.Bounds;
            Vector2 vp = match.LayoutViewport;
            float minZoom = Math.Max(vp.X / bounds.Width, vp.Y / bounds.Height);
            zoom += (focus.DesiredZoom - zoom) * ZoomEase;
            zoom = Math.Max(minZoom, zoom);
            position += (focus.Position - position) * MoveEase;

            float halfW = vp.X / 2f / zoom;
            float halfH = vp.Y / 2f / zoom;
            position.X = MathHelper.Clamp(position.X, bounds.Left + halfW, bounds.Right - halfW);
            position.Y = MathHelper.Clamp(position.Y, bounds.Top + halfH, bounds.Bottom - halfH);

            Vector2 offset = Vector2.Zero;
            if (shake > 0.1f)
            {
                offset = new Vector2((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)) * shake;
                shake *= 0.85f;
            }

            Transform = Matrix.CreateTranslation(-position.X + offset.X, -position.Y + offset.Y, 0)
                      * Matrix.CreateScale(zoom * match.ScreenScale, zoom * match.ScreenScale, 1)
                      * Matrix.CreateTranslation(match.ViewportSize.X / 2f, match.ViewportSize.Y / 2f, 0);
        }

        public Vector2 WorldToScreen(Vector2 world) => Vector2.Transform(world, Transform);
    }
}
