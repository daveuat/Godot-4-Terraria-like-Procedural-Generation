using Godot;
using System;

public partial class Player : CharacterBody2D
{
    public const float Speed = 300.0f;
    public const float JumpVelocity = -400.0f;
    public const float DashSpeed = 1000.0f;
    public const float DashDuration = 0.2f;
    public const float SlideDuration = 1.0f; // Duration of the slide
    public const float SlideDeceleration = 300.0f; // Rate of deceleration during slide

    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    private int jumps = 0;
    private const int MaxJumps = 2; // Maximum number of jumps

    private AnimationPlayer animationPlayer;
    private Node2D sprite; // Reference to the Sprite node

    private bool isJumping = false;
    private bool isDashing = false;
    private bool isSliding = false;
    private bool isWallSliding = false;
    private float dashTimer = 0;
    private float slideTimer = 0;

    public override void _Ready()
    {
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer"); // Adjust the path as necessary
        sprite = GetNode<Node2D>("Sprite"); // Adjust the path to your Sprite node
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        // Add the gravity.
        if (!isDashing && !isSliding)
        {
            velocity.Y += gravity * (float)delta;
        }

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

        // Handle Dash.
        if (Input.IsActionJustPressed("ui_dash") && !isDashing && !isSliding)
        {
            isDashing = true;
            dashTimer = DashDuration;
            velocity = new Vector2(sprite.Scale.X * DashSpeed, 0); // Dash in the direction the sprite is facing
            animationPlayer.Play("Dash"); // Play the dash animation
        }

        if (isDashing)
        {
            dashTimer -= (float)delta;
            if (dashTimer <= 0)
            {
                isDashing = false;
                animationPlayer.Stop(); // Stop the dash animation
            }
        }

        // Handle Slide - Only slide if moving.
        if (Input.IsActionJustPressed("ui_slide") && !isDashing && !isSliding && IsOnFloor() && Velocity.X != 0)
        {
            isSliding = true;
            slideTimer = SlideDuration;
            animationPlayer.Play("Slide"); // Play the slide animation
        }

        if (isSliding)
        {
            slideTimer -= (float)delta;
            velocity.X = Mathf.MoveToward(velocity.X, 0, SlideDeceleration * (float)delta); // Gradually slow down

            // Stop sliding if the timer is up or if the player is no longer on the floor.
            if (slideTimer <= 0 || !IsOnFloor())
            {
                isSliding = false;
                animationPlayer.Stop(); // Stop the slide animation
            }
        }

        // Handle Wall Sliding.
        if (IsOnWall() && !IsOnFloor() && 
            (Input.IsActionPressed("ui_left") || Input.IsActionPressed("ui_right")))
        {
            isWallSliding = true;
            animationPlayer.Seek(0.1, true); // Ensure the second frame of jump animation is played
            if (isDashing)
            {
                isDashing = false; // Stop dashing when wall sliding starts
                animationPlayer.Stop(); // Stop the dash animation
            }
        }
        else
        {
            isWallSliding = false;
        }

        if (isWallSliding)
        {
            float wallSlideGravity = 100.0f; 
            velocity.Y += wallSlideGravity * (float)delta; // Cast delta to float
            velocity.Y = Mathf.Min(velocity.Y, wallSlideGravity);
        }

        // Handle movement and deceleration.
        if (!isDashing && !isSliding)
        {
            Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
            velocity.X = direction != Vector2.Zero ? direction.X * Speed : Mathf.MoveToward(Velocity.X, 0, Speed);

            // Flip sprite based on direction.
            if (velocity.X != 0)
            {
                sprite.Scale = new Vector2(Mathf.Sign(velocity.X), 1);
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public override void _Process(double delta)
    {

        // Update animations based on state.
        if (isJumping)
        {
            // Update the frame of the jump animation based on the velocity.
            if (Velocity.Y < 0)
            {
                animationPlayer.Seek(0.0, true); // First frame for ascending.
            }
            else if (Velocity.Y >= 0)
            {
                animationPlayer.Seek(0.1, true); // Second frame for descending.
            }
        }
        else if (isWallSliding)
        {
            // Play the second frame of the jump animation when sliding on a wall.
            animationPlayer.Seek(0.1, true); // Second frame for descending.
        }
        else if (IsOnFloor() && !isDashing && !isSliding)
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
