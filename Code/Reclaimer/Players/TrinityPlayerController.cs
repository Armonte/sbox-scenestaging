using Sandbox;
using Sandbox.Citizen;

namespace Reclaimer
{
	public class TrinityPlayerController : Component
	{
		[Property] public Vector3 Gravity { get; set; } = new Vector3(0, 0, 800);
		[Property] public float Acceleration { get; set; } = 10.0f;
		[Property] public float Friction { get; set; } = 4.0f;
		[Property] public float JumpForce { get; set; } = 400.0f;
		[Property] public GameObject Body { get; set; }
		[Property] public GameObject Eye { get; set; }
		[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
		[Property] public bool FirstPerson { get; set; }

		// Camera control properties
		[Property] public bool UseMouseLook { get; set; } = true;
		[Property] public GameObject CameraObject { get; set; }
		[Property] public bool UseTopDownCamera { get; set; } = false;
		[Property] public Vector3 TopDownOffset { get; set; } = new Vector3(0, -400, 200);
		[Property] public float TopDownPitch { get; set; } = -45f;
		[Property] public bool ShowCursor { get; set; } = false;
		[Property] public float RotationSpeed { get; set; } = 2.0f;
		[Property] public float CameraRotationSpeed { get; set; } = 1.0f;
		
		// Zoom control properties
		[Property] public float ZoomSpeed { get; set; } = 0.05f;
		[Property] public float MinZoomMultiplier { get; set; } = 0.3f;
		[Property] public float MaxZoomMultiplier { get; set; } = 3.0f;
		[Property] public float ZoomSmoothness { get; set; } = 8.0f;

		private float targetZoomDistance = 1.0f;
		private float currentZoomDistance = 1.0f;
		private float cameraYaw = 0f;
		private Vector2 lastMousePosition;
		
		[Sync] public Vector3 Velocity { get; set; }
		[Sync] public bool IsGrounded { get; set; }

		public Vector3 WishVelocity { get; private set; }

		[Sync]
		public Angles EyeAngles { get; set; }

		[Sync]
		public bool IsRunning { get; set; }

		protected override void OnEnabled()
		{
			base.OnEnabled();

			if (IsProxy)
				return;

			// Initialize zoom distance as a multiplier (1.0 = normal distance)
			targetZoomDistance = 1.0f;
			currentZoomDistance = 1.0f;
			cameraYaw = 0f;
			lastMousePosition = Mouse.Position;

			var cam = GetCamera();
			if (cam.IsValid())
			{
				var ee = cam.WorldRotation.Angles();
				ee.roll = 0;
				EyeAngles = ee;
			}
		}

		protected override void OnUpdate()
		{
			// Eye input - only apply mouse look if enabled
			if (!IsProxy)
			{
				if (UseMouseLook && !UseTopDownCamera)
				{
					var ee = EyeAngles;
					ee += Input.AnalogLook * 0.5f;
					ee.roll = 0;
					EyeAngles = ee;
				}

				// Handle RotCamera action for top-down camera rotation
				if (UseTopDownCamera && Input.Down("RotCamera"))
				{
					var currentMousePos = Mouse.Position;
					var mouseDelta = currentMousePos - lastMousePosition;
					cameraYaw += mouseDelta.x * CameraRotationSpeed * 0.01f;
					lastMousePosition = currentMousePos;
				}
				else if (UseTopDownCamera)
				{
					// Update mouse position when not dragging so next drag starts fresh
					lastMousePosition = Mouse.Position;
				}

				// Handle scroll wheel zoom for top-down camera
				if (UseTopDownCamera && Input.MouseWheel != Vector2.Zero)
				{
					var previousZoom = targetZoomDistance;
					targetZoomDistance -= Input.MouseWheel.y * ZoomSpeed;
					targetZoomDistance = targetZoomDistance.Clamp(MinZoomMultiplier, MaxZoomMultiplier);
					
					// Debug output to see zoom changes
					if (Math.Abs(targetZoomDistance - previousZoom) > 0.001f)
					{
						Log.Info($"Zoom: {previousZoom:F3} -> {targetZoomDistance:F3} (wheel: {Input.MouseWheel.y:F3})");
					}
				}

				// Smooth interpolation toward target zoom with beautiful easing
				if (UseTopDownCamera)
				{
					// Use exponential easing for super smooth zoom
					float easingFactor = 1.0f - MathF.Exp(-ZoomSmoothness * Time.Delta);
					currentZoomDistance = MathX.Lerp(currentZoomDistance, targetZoomDistance, easingFactor);
				}

				UpdateCamera();

				IsRunning = Input.Down("Run");
			}

			// Ground check
			CheckGrounded();

			float moveRotationSpeed = 0;

			// rotate body to look angles
			if (Body.IsValid())
			{
				Rotation targetAngle;

				if (UseTopDownCamera)
				{
					// Top-down mode: rotate toward mouse cursor (unless using RotCamera for camera control)
					bool isRotatingCamera = Input.Down("RotCamera");
					
					if (!isRotatingCamera)
					{
						var mouseWorldPos = GetMouseWorldPosition();
						if (mouseWorldPos.HasValue)
						{
							var directionToMouse = (mouseWorldPos.Value - WorldPosition).Normal;
							targetAngle = Rotation.LookAt(directionToMouse, Vector3.Up);
						}
						else
						{
							// Fallback to current rotation if raycast fails
							targetAngle = Body.WorldRotation;
						}
					}
					else
					{
						// Keep current rotation while camera is being rotated
						targetAngle = Body.WorldRotation;
					}
				}
				else
				{
					// Standard mode: rotate toward movement or camera direction
					targetAngle = new Angles(0, EyeAngles.yaw, 0).ToRotation();

					var v = Velocity.WithZ(0);

					if (v.Length > 10.0f)
					{
						targetAngle = Rotation.LookAt(v, Vector3.Up);
					}
				}

				float rotateDifference = Body.WorldRotation.Distance(targetAngle);

				if (rotateDifference > 50.0f || Velocity.Length > 10.0f || UseTopDownCamera)
				{
					// Use faster rotation for top-down camera mode
					float rotSpeed = UseTopDownCamera ? RotationSpeed * 3.0f : RotationSpeed;
					var newRotation = Rotation.Lerp(Body.WorldRotation, targetAngle, Time.Delta * rotSpeed);

					// We won't end up actually moving to the targetAngle, so calculate how much we're actually moving
					var angleDiff = Body.WorldRotation.Angles() - newRotation.Angles(); // Rotation.Distance is unsigned
					moveRotationSpeed = angleDiff.yaw / Time.Delta;

					Body.WorldRotation = newRotation;
				}
			}

			if (AnimationHelper.IsValid())
			{
				AnimationHelper.WithVelocity(Velocity);
				AnimationHelper.WithWishVelocity(WishVelocity);
				AnimationHelper.IsGrounded = IsGrounded;
				AnimationHelper.MoveRotationSpeed = moveRotationSpeed;
				AnimationHelper.WithLook(EyeAngles.Forward, 1, 1, 1.0f);
				AnimationHelper.MoveStyle = IsRunning ? CitizenAnimationHelper.MoveStyles.Run : CitizenAnimationHelper.MoveStyles.Walk;
			}
		}

		void UpdateCamera()
		{
			var cam = GetCamera();
			if (!cam.IsValid()) return;

			// Control cursor visibility with new API
			if (ShowCursor || UseTopDownCamera)
			{
				Mouse.Visibility = MouseVisibility.Visible;
			}
			else
			{
				Mouse.Visibility = MouseVisibility.Auto;
			}

			if (UseTopDownCamera)
			{
				// Top-down camera mode with dynamic zoom
				// Use TopDownOffset as base position, then apply zoom multiplier and yaw rotation
				var baseOffset = TopDownOffset * currentZoomDistance;
				
				// Apply yaw rotation around the player
				var rotatedOffset = Rotation.FromYaw(cameraYaw) * baseOffset;
				cam.WorldPosition = WorldPosition + rotatedOffset;
				cam.WorldRotation = Rotation.LookAt((WorldPosition + Vector3.Up * 50) - cam.WorldPosition, Vector3.Up);
			}
			else
			{
				// Standard third-person/first-person camera
				var lookDir = EyeAngles.ToRotation();

				if (FirstPerson)
				{
					cam.WorldPosition = Eye.WorldPosition;
					cam.WorldRotation = lookDir;
				}
				else
				{
					cam.WorldPosition = WorldPosition + lookDir.Backward * 300 + Vector3.Up * 75.0f;
					cam.WorldRotation = lookDir;
				}
			}
		}

		CameraComponent GetCamera()
		{
			// First try to use the assigned CameraObject
			if (CameraObject.IsValid())
			{
				var cam = CameraObject.Components.Get<CameraComponent>();
				if (cam.IsValid()) return cam;
			}

			// Fallback to scene camera
			return Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		}

		public Vector3? GetMouseWorldPosition()
		{
			var cam = GetCamera();
			if (!cam.IsValid()) return null;

			// Get mouse screen position
			var screenPos = Mouse.Position;

			// Create ray from camera through mouse position
			var ray = cam.ScreenPixelToRay(screenPos);

			// Raycast to find ground intersection
			var trace = Scene.Trace.Ray(ray.Position, ray.Position + ray.Forward * 5000f)
				.WithoutTags("player", "projectile")
				.Run();

			if (trace.Hit)
			{
				return trace.HitPosition;
			}

			// Fallback: project ray onto a ground plane at player's Z level
			var groundZ = WorldPosition.z;
			var rayDir = ray.Forward;

			if (Math.Abs(rayDir.z) > 0.001f) // Avoid division by zero
			{
				var t = (groundZ - ray.Position.z) / rayDir.z;
				if (t > 0) // Ray hits ground in front of camera
				{
					return ray.Position + rayDir * t;
				}
			}

			return null;
		}

		[Rpc.Broadcast]
		public void OnJump(float floatValue, string dataString, object[] objects, Vector3 position)
		{
			AnimationHelper?.TriggerJump();
		}

		float fJumps;

		protected override void OnFixedUpdate()
		{
			if (IsProxy)
				return;

			BuildWishVelocity();

			// Handle jumping
			if (IsGrounded && Input.Down("Jump"))
			{
				Velocity = Velocity.WithZ(JumpForce);
				OnJump(fJumps, "Hello", new object[] { Time.Now.ToString(), 43.0f }, Vector3.Random);
				fJumps += 1.0f;
			}

			// Apply movement physics
			if (IsGrounded)
			{
				// Only zero Z velocity if we're not jumping
				if (Velocity.z <= 0)
				{
					Velocity = Velocity.WithZ(0);
				}
				Velocity = MoveGround(Velocity, WishVelocity);
			}
			else
			{
				Velocity -= Gravity * Time.Delta * 0.5f;
				Velocity = MoveAir(Velocity, WishVelocity);
			}

			// Move with collision detection
			MoveWithCollision();

			// Apply gravity again (verlet integration)
			if (!IsGrounded)
			{
				Velocity -= Gravity * Time.Delta * 0.5f;
			}
		}

		public void BuildWishVelocity()
		{
			Vector3 inputMove = Input.AnalogMove;
			
			if (UseTopDownCamera)
			{
				// Apply camera rotation to movement input
				var cameraRotation = Rotation.FromYaw(cameraYaw);
				WishVelocity = cameraRotation * inputMove;
			}
			else
			{
				var rot = EyeAngles.ToRotation();
				WishVelocity = rot * inputMove;
			}

			WishVelocity = WishVelocity.WithZ(0);

			if (!WishVelocity.IsNearZeroLength) WishVelocity = WishVelocity.Normal;

			// Disable sprint - always use walk speed
			WishVelocity *= 110.0f;
		}

		void CheckGrounded()
		{
			var trace = Scene.Trace.Ray(WorldPosition, WorldPosition + Vector3.Down * 10f)
				.WithoutTags("player", "projectile")
				.Run();
				
			IsGrounded = trace.Hit && trace.Distance <= 5f;
		}

		Vector3 MoveGround(Vector3 velocity, Vector3 wishVelocity)
		{
			// Apply friction
			float speed = velocity.Length;
			if (speed > 0.1f)
			{
				float drop = speed * Friction * Time.Delta;
				velocity *= Math.Max(speed - drop, 0) / speed;
			}

			// Accelerate
			velocity += wishVelocity * Acceleration * Time.Delta;
			return velocity;
		}

		Vector3 MoveAir(Vector3 velocity, Vector3 wishVelocity)
		{
			// Limited air acceleration
			var wishDir = wishVelocity.Normal;
			var currentSpeed = Vector3.Dot(velocity, wishDir);
			var addSpeed = Math.Min(50f - currentSpeed, wishVelocity.Length);
			
			if (addSpeed > 0)
			{
				velocity += wishDir * addSpeed * Acceleration * 0.1f * Time.Delta;
			}
			
			return velocity;
		}

		void MoveWithCollision()
		{
			var movement = Velocity * Time.Delta;
			
			// Simple approach: try to move, then check if we need to fix position
			var newPos = WorldPosition + movement;
			
			// Check if new position is valid (not inside ground/walls)
			var groundTrace = Scene.Trace.Ray(newPos + Vector3.Up * 32f, newPos + Vector3.Down * 32f)
				.WithoutTags("player", "projectile")
				.Run();
			
			if (groundTrace.Hit)
			{
				// Make sure we're above ground
				if (newPos.z < groundTrace.HitPosition.z)
				{
					newPos = newPos.WithZ(groundTrace.HitPosition.z + 1f);
					// Stop downward velocity when hitting ground
					if (Velocity.z < 0)
					{
						Velocity = Velocity.WithZ(0);
					}
				}
			}
			
			WorldPosition = newPos;
		}
	}
}