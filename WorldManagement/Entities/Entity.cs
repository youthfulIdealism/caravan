using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caravan.WorldManagement.Entities
{
    public abstract class Entity
    {
        public Vector2 location { get; set; }
        public WorldBase world { get; set; }

        public abstract void update(float tpf);
        public abstract void draw(SpriteBatch batch, Point offset);
    }
}
