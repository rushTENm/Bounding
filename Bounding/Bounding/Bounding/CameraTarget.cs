using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Bounding
{
    class CameraTarget
    {
        private const float MinimumAltitude = 350.0f;

        /// <summary>
        /// A reference to the graphics device used to access the viewport for touch input.
        /// </summary>
        private GraphicsDevice graphicsDevice;

        public Model Model;

        float Scale;

        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 Up;
        private Vector3 right;
        public Vector3 Right
        {
            get { return right; }
        }

        /// <summary>
        /// Full speed at which ship can rotate; measured in radians per second.
        /// </summary>
        private const float RotationRate = 1.8f;
        private const float Mass = 1.0f;
        private const float ThrustForce = 10000.0f;
        private const float DragFactor = 0.9f;

        public Vector3 Velocity;

        public Matrix World
        {
            get { return world; }
        }
        private Matrix world;

        public CameraTarget(GraphicsDevice device)
        {
            graphicsDevice = device;
            Reset();
        }

        public void Reset()
        {
            Position = new Vector3(0, MinimumAltitude, 0);
            Direction = Vector3.Forward;
            Up = Vector3.Up;
            right = Vector3.Right;
            Velocity = Vector3.Zero;
        }

        public void Init(ContentManager content, float scale, string FileLoaction)
        {
            Model = content.Load<Model>(FileLoaction);

            Scale = scale;
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Determine rotation amount from input
            Vector2 rotationAmount = Vector2.Zero;
            if (keyboardState.IsKeyDown(Keys.Left))
                rotationAmount.X = 1.0f;
            if (keyboardState.IsKeyDown(Keys.Right))
                rotationAmount.X = -1.0f;
            if (keyboardState.IsKeyDown(Keys.Up))
                rotationAmount.Y = -1.0f;
            if (keyboardState.IsKeyDown(Keys.Down))
                rotationAmount.Y = 1.0f;

            // Scale rotation amount to radians per second
            rotationAmount = rotationAmount * RotationRate * elapsed;

            // Correct the X axis steering when the ship is upside down
            if (Up.Y < 0)
                rotationAmount.X = -rotationAmount.X;

            // Create rotation matrix from rotation amount
            Matrix rotationMatrix =
                Matrix.CreateFromAxisAngle(Right, rotationAmount.Y) *
                Matrix.CreateRotationY(rotationAmount.X);

            // Rotate orientation vectors
            Direction = Vector3.TransformNormal(Direction, rotationMatrix);
            Up = Vector3.TransformNormal(Up, rotationMatrix);

            // Re-normalize orientation vectors
            // Without this, the matrix transformations may introduce small rounding
            // errors which add up over time and could destabilize the ship.
            Direction.Normalize();
            Up.Normalize();

            // Re-calculate Right
            right = Vector3.Cross(Direction, Up);

            // The same instability may cause the 3 orientation vectors may
            // also diverge. Either the Up or Direction vector needs to be
            // re-computed with a cross product to ensure orthagonality
            Up = Vector3.Cross(Right, Direction);

            // Determine thrust amount from input
            float thrustAmount = 0;
            if (keyboardState.IsKeyDown(Keys.Space))
                thrustAmount = 1.0f;

            // Calculate force from thrust amount
            Vector3 force = Direction * thrustAmount * ThrustForce;

            // Apply acceleration
            Vector3 acceleration = force / Mass;
            Velocity += acceleration * elapsed;

            // Apply psuedo drag
            Velocity *= DragFactor;

            // Apply velocity
            Position += Velocity * elapsed;

            // Prevent ship from flying under the ground
            Position.Y = Math.Max(Position.Y, MinimumAltitude);

            // Reconstruct the ship's world matrix
            world = Matrix.Identity;
            world.Forward = Direction;
            world.Up = Up;
            world.Right = right;
            world.Translation = Position;
        }

        public void DrawMeshes(ChaseCamera camera)
        {
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                    effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateScale(Scale) * world;

                    // Use the matrices provided by the chase camera
                    effect.View = camera.View;
                    effect.Projection = camera.Projection;
                }
                mesh.Draw();
            }
        }
    }
}
