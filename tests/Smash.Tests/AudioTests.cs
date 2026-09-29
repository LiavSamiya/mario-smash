using System;
using System.Linq;
using Xunit;

namespace Smash.Tests
{
    public class AudioTests
    {
        public static TheoryData<string> Tracks => new TheoryData<string> { "title", "battle", "select" };

        [Theory]
        [MemberData(nameof(Tracks))]
        public void Music_tracks_are_loud_enough_and_never_clip(string track)
        {
            float[] s = track switch
            {
                "title" => SoundBank.TitleMusic(),
                "battle" => SoundBank.BattleMusic(),
                _ => SoundBank.SelectMusic(),
            };
            double rms = Math.Sqrt(s.Average(v => (double)v * v));

            Assert.True(s.Length > 22050 * 10, "a loop should be longer than 10 seconds");
            Assert.DoesNotContain(s, float.IsNaN);
            Assert.True(s.Max(Math.Abs) <= 0.91f);
            Assert.InRange(rms, 0.05, 0.4);
        }
    }
}
