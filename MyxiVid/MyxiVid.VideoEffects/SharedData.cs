using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyxiVid.VideoEffects
{
    internal class SharedData
    {
        public static readonly float[] Frames = new float[512];
        public static int FrameIndex = 0;
        public static ConcurrentQueue<float[]> AudioFrames = new ConcurrentQueue<float[]>();
    }
}
