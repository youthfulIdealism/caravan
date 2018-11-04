using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArmadilloLib.Animation;
using ArmadilloTree;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace caravan.WorldManagement.Entities
{
    public class PlantWrapper : Entity
    {
        public AnimatedTree tree { get; set; }
        public HashSet<BendModifier> bendModifiers { get; set; }
        public float stiffness { get; set; }
        public float bend { get; protected set; }
        public bool isBackground { get; set; }
        public int variantIndex = 0;


        public PlantWrapper(AnimatedTree tree, WorldBase world)
        {
            this.world = world;
            this.tree = tree;
            this.variantIndex = world.rand.Next(world.quadEffect.Length);
            location = tree.location;
            bendModifiers = tree.bendModifiers;
            stiffness = tree.stiffness;
            bend = tree.bend;
        }

        public override void update(float tpf)
        {
            tree.location = location;
            tree.stiffness = stiffness;
            tree.update(tpf);
        }

        

        public void DrawVertexRectangle(Rectangle r)
        {
            VertexPositionColorTexture[] quad = new VertexPositionColorTexture[6];

            float bendAmt = tree.bend * 100;
            // +tree.bend * 10
            quad[0].Position = new Vector3(r.Left + bendAmt, r.Top, 0f); quad[0].Color = Color.White; quad[0].TextureCoordinate = new Vector2(0f, 0f);  // p1
            quad[1].Position = new Vector3(r.Left, r.Bottom, 0f); quad[1].Color = Color.White; quad[1].TextureCoordinate = new Vector2(0f, 1f); // p0
            quad[2].Position = new Vector3(r.Right, r.Bottom, 0f); quad[2].Color = Color.White; quad[2].TextureCoordinate = new Vector2(1f, 1f);// p3
            //
            quad[3].Position = new Vector3(r.Right, r.Bottom, 0f); quad[3].Color = Color.White; quad[3].TextureCoordinate = new Vector2(1f, 1f);// p3
            quad[4].Position = new Vector3(r.Right + bendAmt, r.Top, 0f); quad[4].Color = Color.White; quad[4].TextureCoordinate = new Vector2(1f, 0f);// p2
            quad[5].Position = new Vector3(r.Left + bendAmt, r.Top, 0f); quad[5].Color = Color.White; quad[5].TextureCoordinate = new Vector2(0f, 0f);// p1

            //tree
            world.setGraphicForQuad();

            BasicEffect usedEffect = world.quadEffect[variantIndex];

            foreach (EffectPass pass in usedEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Game1.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, quad, 0, 2);
            }
        }

        public override void draw(SpriteBatch batch, Point offset)
        {
            // batch.Draw(tree, (location.ToPoint() + offset).ToVector2(), Color.White);
            //Rectangle r = new Rectangle(location.ToPoint() + offset, new Point(60, 60));
            int tw = Game1.grass_img.Width;
            int th = Game1.grass_img.Height;
            Rectangle r = new Rectangle(location.ToPoint() + offset + new Point(-tw / 2 + 30, -th + 60), new Point(Game1.grass_img.Width, Game1.grass_img.Height));
            DrawVertexRectangle(r);
        }
    }
}
