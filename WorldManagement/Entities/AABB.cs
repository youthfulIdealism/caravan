using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caravan.WorldManagement.Entities
{
    public struct AABB
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public AABB(float X, float Y, float Width, float Height)
        {
            this.X = X;
            this.Y = Y;
            this.Width = Width;
            this.Height = Height;
        }

        public AABB(Vector2 loc, float Width, float Height) : this(loc.X, loc.Y, Width, Height)
        {
        }

        public AABB(Vector2 loc, Vector2 dimensions) : this(loc.X, loc.Y, dimensions.X, dimensions.Y)
        {
        }

        public Vector2 Center
        {
            get { return new Vector2(X + Width / 2, Y + Height / 2); }
        }

        public float Top
        {
            get { return Y; }
        }

        public float Bottom
        {
            get { return Y + Height; }
        }

        public float Left
        {
            get { return X; }
        }

        public float Right
        {
            get { return X + Width; }
        }

        public Rectangle ToRect()
        {
            return new Rectangle((int)X, (int)Y, (int)Width, (int)Height);
        }

        /*public bool Intersects(AABB other)
        {
            return ((other.Top < this.Top && other.Top > this.Bottom) || (other.Bottom > this.Bottom && other.Bottom < this.Top)) && ((other.Left > this.Left && other.Left < this.Right) || (other.Right > this.Left && other.Right < this.Right));
        }*/

        public bool Contains(float x, float y)
        {
            return ((((this.X <= x) && (x < (this.X + this.Width))) && (this.Y <= y)) && (y < (this.Y + this.Height)));
        }

        public bool Contains(Point value)
        {
            return ((((this.X <= value.X) && (value.X < (this.X + this.Width))) && (this.Y <= value.Y)) && (value.Y < (this.Y + this.Height)));
        }

        public bool Contains(Vector2 value)
        {
            return ((((this.X <= value.X) && (value.X < (this.X + this.Width))) && (this.Y <= value.Y)) && (value.Y < (this.Y + this.Height)));
        }

        public bool Contains(Rectangle value)
        {
            return ((((this.X <= value.X) && ((value.X + value.Width) <= (this.X + this.Width))) && (this.Y <= value.Y)) && ((value.Y + value.Height) <= (this.Y + this.Height)));
        }

        public bool Contains(AABB value)
        {
            return ((((this.X <= value.X) && ((value.X + value.Width) <= (this.X + this.Width))) && (this.Y <= value.Y)) && ((value.Y + value.Height) <= (this.Y + this.Height)));
        }


        public bool Intersects(Rectangle value)
        {
            return value.Left < Right &&
                   Left < value.Right &&
                   value.Top < Bottom &&
                   Top < value.Bottom;
        }

        public bool Intersects(AABB value)
        {
            return value.Left < Right &&
                   Left < value.Right &&
                   value.Top < Bottom &&
                   Top < value.Bottom;
        }

        public void Intersects(ref Rectangle value, out bool result)
        {
            result = value.Left < Right &&
                     Left < value.Right &&
                     value.Top < Bottom &&
                     Top < value.Bottom;
        }


        public static AABB Intersect(AABB value1, Rectangle value2)
        {
            AABB rectangle;
            Intersect(ref value1, ref value2, out rectangle);
            return rectangle;
        }

        public static void Intersect(ref AABB value1, ref Rectangle value2, out AABB result)
        {
            if (value1.Intersects(value2))
            {
                float right_side = Math.Min(value1.X + value1.Width, value2.X + value2.Width);
                float left_side = Math.Max(value1.X, value2.X);
                float top_side = Math.Max(value1.Y, value2.Y);
                float bottom_side = Math.Min(value1.Y + value1.Height, value2.Y + value2.Height);
                result = new AABB(left_side, top_side, right_side - left_side, bottom_side - top_side);
            }
            else
            {
                result = new AABB(0, 0, 0, 0);
            }
        }


        public static AABB Intersect(AABB value1, AABB value2)
        {
            AABB rectangle;
            Intersect(ref value1, ref value2, out rectangle);
            return rectangle;
        }

        public static void Intersect(ref AABB value1, ref AABB value2, out AABB result)
        {
            if (value1.Intersects(value2))
            {
                float right_side = Math.Min(value1.X + value1.Width, value2.X + value2.Width);
                float left_side = Math.Max(value1.X, value2.X);
                float top_side = Math.Max(value1.Y, value2.Y);
                float bottom_side = Math.Min(value1.Y + value1.Height, value2.Y + value2.Height);
                result = new AABB(left_side, top_side, right_side - left_side, bottom_side - top_side);
            }
            else
            {
                result = new AABB(0, 0, 0, 0);
            }
        }

        public static AABB Union(AABB value1, AABB value2)
        {
            float x = Math.Min(value1.X, value2.X);
            float y = Math.Min(value1.Y, value2.Y);
            return new AABB(x, y,
                                 Math.Max(value1.Right, value2.Right) - x,
                                     Math.Max(value1.Bottom, value2.Bottom) - y);
        }
    }
}
