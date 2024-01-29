using Godot;
using System;

public partial class CustomCamera2D : Camera2D
{
    public Vector2 ZoomLevel = new Vector2(0.5f, 0.5f); // Smaller values zoom in
    public Rect2 LevelBounds; // Set this to the bounds of your level

    public override void _Ready()
    {
        Zoom = ZoomLevel;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 cameraPos = GlobalPosition;
        cameraPos.X = Mathf.Clamp(cameraPos.X, LevelBounds.Position.X, LevelBounds.End.X);
        cameraPos.Y = Mathf.Clamp(cameraPos.Y, LevelBounds.Position.Y, LevelBounds.End.Y);
        GlobalPosition = cameraPos;
    }
}
