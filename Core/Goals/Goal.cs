using System;

namespace AnimalFall.Core.Goals
{
    [Serializable]
    public class Goal
    {
        public int chickenCount;
        public int dogCount;
        public int cowCount;
        public int catCount;
        public int monkeyCount;
        public int balloonCount;

        public int TotalCount =>
            chickenCount + dogCount + cowCount + catCount + monkeyCount + balloonCount;
    }
}
