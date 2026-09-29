namespace Smash
{
    //plays an animation one tick at a time
    class AnimationPlayer
    {
        public Animation Current { get; private set; }
        //frame inside the current animation (0 = first frame of the animation)
        public int Frame { get; private set; }
        //true when a non looping animation has shown its last frame for its full time
        public bool Finished { get; private set; }
        int ticks;

        public int SheetFrame => Current.SheetFrame(Frame);

        //starts an animation. playing the same animation again keeps its progress unless restart is set
        public void Play(Animation anim, bool restart = false)
        {
            if (anim == Current && !restart)
                return;
            Current = anim;
            Frame = 0;
            ticks = 0;
            Finished = false;
        }

        public void Update()
        {
            if (Finished)
                return;
            if (++ticks < Current.FrameTicks)
                return;
            ticks = 0;
            if (Frame + 1 < Current.Count)
                Frame++;
            else if (Current.Loop)
                Frame = 0;
            else
                Finished = true;
        }
    }
}
