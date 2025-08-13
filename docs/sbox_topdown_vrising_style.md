# Top-Down Perspective Camera & Mouse Click Targeting (S&box ECS, C#)

This document outlines how to implement a **V Rising–style** top-down camera using a perspective projection with a fixed angle, along with a mouse-click targeting system and character rotation toward the cursor, in **S&box**.

---

## 1. Camera Setup – Perspective with Fixed Angle

We won't use orthographic projection. Instead, we’ll create a **perspective camera** pitched downward at a fixed angle, following the player.

### Steps:
1. Add a `CameraComponent` to a dedicated GameObject (e.g., `TopDownCamera`).
2. Set the projection mode to perspective (default in S&box).
3. Position the camera **behind and above** the player, angled down.
4. Update its position in `OnUpdate()` to follow the player smoothly.

```csharp
public class TopDownCameraComponent : Component
{
    [Property] public GameObject Target { get; set; }
    [Property] public Vector3 Offset { get; set; } = new Vector3(0, -10, 10);
    [Property] public Rotation CameraRotation { get; set; } = Rotation.FromPitch(45f);

    private CameraComponent _camera;

    public override void OnStart()
    {
        _camera = GameObject.GetOrAddComponent<CameraComponent>();
        _camera.FieldOfView = 60f; // perspective
    }

    public override void OnUpdate()
    {
        if (Target == null) return;

        var targetPos = Target.Transform.Position;
        Transform.Position = targetPos + Offset;
        Transform.Rotation = CameraRotation;
    }
}
```

---

## 2. Mouse Click Targeting

We’ll use the mouse position to cast a ray from the camera into the world, detecting clicked objects.

```csharp
public class ClickTargeterComponent : Component
{
    [Property] public CameraComponent Camera { get; set; }

    public override void OnUpdate()
    {
        if (Input.Pressed("attack1")) // left click
        {
            var mousePos = Mouse.Position;
            var ray = Camera.ScreenPixelToRay(mousePos);
            var tr = Scene.Trace.Raycast(ray, 1000f, Mask.All);

            if (tr.Hit)
            {
                var clickedEntity = tr.Entity;
                Log.Info($"Clicked on {clickedEntity}");
            }
        }
    }
}
```

---

## 3. Character Rotation to Face Mouse Cursor

We’ll rotate the character so they face the world position under the mouse cursor.

```csharp
public class CharacterFacingMouseComponent : Component
{
    [Property] public CameraComponent Camera { get; set; }

    public override void OnUpdate()
    {
        var mousePos = Mouse.Position;
        var ray = Camera.ScreenPixelToRay(mousePos);
        var tr = Scene.Trace.Raycast(ray, 1000f, Mask.All);

        if (tr.Hit)
        {
            var lookTarget = tr.EndPosition;
            var direction = (lookTarget.WithZ(Transform.Position.z) - Transform.Position).Normal;
            Transform.Rotation = Rotation.LookAt(direction, Vector3.Up);
        }
    }
}
```

---

## 4. Suggested Component Architecture

- **TopDownCameraComponent** – Handles camera position and rotation.
- **ClickTargeterComponent** – Detects what the player clicks in the game world.
- **CharacterFacingMouseComponent** – Rotates the player to face the mouse target position.

---

## 5. Notes

- You can integrate `ClickTargeterComponent` and `CharacterFacingMouseComponent` into the player controller directly if desired.
- Consider smoothing camera movement with `Vector3.Lerp`.
- For better targeting, restrict raycasts to a ground plane layer mask.
