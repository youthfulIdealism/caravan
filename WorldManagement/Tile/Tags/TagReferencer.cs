using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caravan.WorldManagement.Tile.Tags
{
    public static class TagReferencer
    {
        public static TileTag SOLID = new SOLID();
        public static TileTag AIR = new AIR();
        public static TileTag DRAWOUTSIDEOFBOUNDS = new DRAWOUTSIDEOFBOUNDS();
        public static TileTag FLAMMABLE = new FLAMMABLE();
        //private static bool hasRegisteredTags = false;
        /*public static void setUp()
        {
            if(!hasRegisteredTags)
            {
                hasRegisteredTags = true;
            }
        }*/
    }
}
