using Godot;
using System;

public partial class Player : CharacterBody2D
{
    public const float Speed = 300.0f;
    public const float JumpVelocity = -400.0f;

    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    private int jumps = 0;
    private const int MaxJumps = 2; // Maximum number of jumps

    private AnimationPlayer animationPlayer;
    private Node2D sprite; // Reference to the Sprite node

    private bool isJumping = false;

    public override void _Ready()
    {
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer"); // Adjust the path as necessary
        sprite = GetNode<Node2D>("Sprite"); // Adjust the path to your Sprite node
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        // Add the gravity.
        velocity.Y += gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && jumps < MaxJumps)
        {
            velocity.Y = JumpVelocity;
            jumps++;
            isJumping = true;
            animationPlayer.Play("Jump"); // Play the jump animation immediately
        }

        // Reset jump count when on the floor.
        if (IsOnFloor())
        {
            if (isJumping)
            {
                isJumping = false;
                jumps = 0;
            }
        }

        // Handle movement and deceleration.
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        velocity.X = direction != Vector2.Zero ? direction.X * Speed : Mathf.MoveToward(Velocity.X, 0, Speed);

        // Flip sprite based on direction
        if (velocity.X != 0)
        {
            sprite.Scale = new Vector2(Mathf.Sign(velocity.X), 1);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public override void _Process(double delta)
    {
        // Update animations based on state
        if (isJumping)
        {
            // Update the frame of the jump animation based on the velocity
            if (Velocity.Y < 0)
            {
                animationPlayer.Seek(0.0, true); // First frame for ascending
            }
            else if (Velocity.Y >= 0)
            {
                animationPlayer.Seek(0.1, true); // Second frame for descending
            }
        }
        else if (IsOnFloor())
        {
            PlayAnimation(Velocity.X != 0 ? "Run" : "Idle");
        }
    }

    private void PlayAnimation(string animName)
    {
        if (animationPlayer.CurrentAnimation != animName)
        {
            animationPlayer.Play(animName);
        }
    }
}
