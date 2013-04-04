using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Bounding
{
    class GameModel
    {
        public Model Model;
        public Vector3 Position = Vector3.Zero;

        float Scale;

        public void Init(ContentManager content, float scale, string FileLoaction)
        {
            Model = content.Load<Model>(FileLoaction);
            
            Scale = scale;
        }

        public void DrawMeshes(ChaseCamera Camera)
        {
            Matrix[] boneTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;

                    effect.World = boneTransforms[mesh.ParentBone.Index] * Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Position);
                    effect.Projection = Camera.Projection;
                    effect.View = Camera.View;
                }
                mesh.Draw();
            }
        }
    }
}
