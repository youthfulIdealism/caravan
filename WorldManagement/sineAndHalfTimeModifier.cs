using ArmadilloLib.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace caravan.WorldManagement
{
    public class SineAndHalfTimeModifier : BendModifier
    {
        float durationModifier;
        float amplitude;
        float amplitudeNegative;
        float progress;

        public SineAndHalfTimeModifier(float durationModifier, float amplitude, float amplitudeNegative)
        {
            this.durationModifier = durationModifier;
            this.amplitude = amplitude;
            this.amplitudeNegative = amplitudeNegative;
            progress = 0;
        }

        public override void update(float tpf)
        {
            progress += tpf;
            if(Math.Sin((progress / durationModifier) * 3.14 * 2) < 1)
            {
                progress += tpf;
            }
        }

        public override float getModifier()
        {
            float mod = (float)Math.Sin((progress / durationModifier) * 3.14 * 2);
            if(mod > 0) { mod *= amplitude; }
            else { mod *= amplitudeNegative; }
            return mod;
        }

        public override bool isFinished()
        {
            return progress > durationModifier;
        }

        
    }
}
