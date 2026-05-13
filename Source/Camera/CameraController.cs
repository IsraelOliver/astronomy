using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Astronomia;

public sealed class CameraController
{
    private const float FocusZoom = 3.15f;
    private const float TopDownInitialZoom = 0.13f;
    private const float InclinedInitialZoom = 1f;
    private const float TopDownMinimumZoom = 0.05f;
    private const float InclinedMinimumZoom = 0.35f;

    public float Zoom { get; private set; } = TopDownInitialZoom;
    public Vector2 Offset { get; private set; }

    public Vector2 GetSystemCenter(Viewport viewport)
    {
        return new Vector2(viewport.Width / 2f, viewport.Height / 2f) + Offset;
    }

    public void Reset(SystemViewMode viewMode)
    {
        Zoom = GetInitialZoom(viewMode);
        Offset = Vector2.Zero;
    }

    public void CenterOnSun(SolarSystemState solarSystem)
    {
        Offset = -BodyPositionService.GetSunOffset(solarSystem, Zoom);
    }

    public void UpdateFreeCamera(KeyboardState keyboard, MouseState mouse, MouseState previousMouse, int wheelDelta, float elapsedSeconds, SystemViewMode viewMode)
    {
        if (wheelDelta != 0)
            Zoom = MathHelper.Clamp(Zoom + wheelDelta / 1200f, GetMinimumZoom(viewMode), 3.6f);

        var panSpeed = 520f * elapsedSeconds;
        if (keyboard.IsKeyDown(Keys.Left))
            Offset += new Vector2(panSpeed, 0f);
        if (keyboard.IsKeyDown(Keys.Right))
            Offset -= new Vector2(panSpeed, 0f);
        if (keyboard.IsKeyDown(Keys.Up))
            Offset += new Vector2(0f, panSpeed);
        if (keyboard.IsKeyDown(Keys.Down))
            Offset -= new Vector2(0f, panSpeed);

        if (mouse.RightButton == ButtonState.Pressed && previousMouse.RightButton == ButtonState.Pressed)
            Offset += new Vector2(mouse.X - previousMouse.X, mouse.Y - previousMouse.Y);
    }

    public void UpdateFocus(SolarSystemState solarSystem, Viewport viewport, float elapsedSeconds)
    {
        if (!solarSystem.HasSelectedBody)
            return;

        var amount = MathHelper.Clamp(elapsedSeconds * 3.5f, 0f, 1f);
        Zoom = MathHelper.Lerp(Zoom, FocusZoom, amount);

        var viewportCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        var focusPoint = new Vector2(viewport.Width * 0.42f, viewport.Height * 0.56f);
        var selectedOffset = solarSystem.SelectedPlanet is not null
            ? BodyPositionService.GetPlanetOffset(solarSystem, solarSystem.SelectedPlanet, Zoom)
            : solarSystem.IsSunSelected
                ? BodyPositionService.GetSunOffset(solarSystem, Zoom)
            : Vector2.Zero;

        var targetOffset = focusPoint - viewportCenter - selectedOffset;
        Offset = Vector2.Lerp(Offset, targetOffset, amount);
    }

    private static float GetInitialZoom(SystemViewMode viewMode)
    {
        return viewMode == SystemViewMode.TopDown ? TopDownInitialZoom : InclinedInitialZoom;
    }

    private static float GetMinimumZoom(SystemViewMode viewMode)
    {
        return viewMode == SystemViewMode.TopDown ? TopDownMinimumZoom : InclinedMinimumZoom;
    }
}
