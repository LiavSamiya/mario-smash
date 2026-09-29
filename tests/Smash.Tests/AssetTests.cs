using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Xunit;

namespace Smash.Tests
{
    public class AssetTests
    {
        [Fact]
        public void Markers_split_a_sheet_into_frames_and_origins()
        {
            //10x3 image: markers at 0 (start), 3 (origin), 5 (next start), 7 (origin), 9 (end)
            var pixels = Enumerable.Repeat(Color.Blue, 30).ToArray();
            var img = new ImageData(10, 3, pixels);
            foreach (int x in new[] { 0, 3, 5, 7, 9 })
                img[x, 2] = Color.Black;

            var sheet = new SpriteSheet("test", img);

            Assert.Equal(2, sheet.FrameCount);
            Assert.Equal(new Rectangle(0, 0, 5, 2), sheet.Frames[0]);
            Assert.Equal(new Rectangle(5, 0, 4, 2), sheet.Frames[1]);
            Assert.Equal(new Vector2(3, 2), sheet.Origins[0]);
            Assert.Equal(new Vector2(2, 2), sheet.Origins[1]);
            Assert.Equal(Color.Transparent, sheet.Image[1, 1]);
        }

        [Fact]
        public void Frames_are_cut_from_a_full_sheet_with_the_background_removed()
        {
            //a 6x4 green sheet with a 2x2 red square at (3, 1)
            var img = new ImageData(6, 4, Enumerable.Repeat(new Color(0, 128, 0), 24).ToArray());
            img[3, 1] = img[4, 1] = img[3, 2] = img[4, 2] = Color.Red;

            SpriteSheet sheet = SheetBuilder.Build("test", img, new Color(0, 128, 0), flip: false, new[] { new[] { 2, 0, 4, 4, 2, 4 } });

            Assert.Equal(1, sheet.FrameCount);
            Assert.Equal(new Vector2(2, 4), sheet.Origins[0]);
            Assert.Equal(Color.Transparent, sheet.Image[0, 0]);
            Assert.Equal(Color.Red, sheet.Image[1, 1]);
        }

        [Fact]
        public void Flipped_frames_mirror_their_pixels_and_handle()
        {
            var img = new ImageData(4, 2, Enumerable.Repeat(Color.White, 8).ToArray());
            img[0, 0] = Color.Red;

            SpriteSheet sheet = SheetBuilder.Build("test", img, Color.Black, flip: true, new[] { new[] { 0, 0, 4, 2, 1, 2 } });

            Assert.Equal(Color.Red, sheet.Image[3, 0]);
            Assert.Equal(new Vector2(3, 2), sheet.Origins[0]);
        }

        [Fact]
        public void All_four_characters_load()
        {
            string[] names = TestWorld.Characters.Select(c => c.Name).ToArray();
            Assert.Equal(new[] { "Mario", "Bowser", "Kratos", "Sasuke" }, names);
        }

        public static TheoryData<int> CharacterIndexes => new TheoryData<int> { 0, 1, 2, 3 };

        [Theory]
        [MemberData(nameof(CharacterIndexes))]
        public void Every_animation_frame_has_hurtboxes_and_every_hit_has_circles(int index)
        {
            Character c = TestWorld.Characters[index];
            foreach (Animation anim in c.Animations.Values)
            {
                Assert.True(anim.Count > 0, $"{c.Name} {anim.Id} has no frames");
                for (int f = 0; f < anim.Count; f++)
                    Assert.NotEmpty(anim.Data.Hurt[anim.SheetFrame(f)]);
            }
            foreach (Move move in c.Moves.Values)
            {
                Assert.NotEmpty(move.Hits);
                foreach (HitData hit in move.Hits)
                    for (int f = hit.FirstFrame; f <= hit.LastFrame; f++)
                        Assert.True(hit.Circles[f].Count > 0, $"{c.Name} {move.Id} has no hitbox on frame {f}");
            }
        }

        [Theory]
        [MemberData(nameof(CharacterIndexes))]
        public void Characters_are_drawn_at_a_sensible_size(int index)
        {
            Character c = TestWorld.Characters[index];
            Assert.InRange(c.BodyHeight, 70, 140);
            Assert.InRange(c.BodyHalfWidth, 6, 60);
        }

        [Theory]
        [MemberData(nameof(CharacterIndexes))]
        public void Frames_have_no_background_left_at_their_corners(int index)
        {
            //the corners of a cut frame are (almost always) background, so they must be transparent
            Character c = TestWorld.Characters[index];
            SpriteSheet sheet = c.Animations[AnimationId.Stand].Data.Sheet;
            Rectangle r = sheet.Frames[0];
            Assert.Equal(0, sheet.Image[r.Left, r.Top].A);
            Assert.Equal(0, sheet.Image[r.Right - 1, r.Top].A);
        }

        [Fact]
        public void Palettes_change_colours_but_keep_transparency()
        {
            var img = new ImageData(2, 1, new[] { new Color(40, 60, 220), Color.Transparent });
            ImageData shifted = Palette.Apply(img, 1);
            Assert.NotEqual(img[0, 0], shifted[0, 0]);
            Assert.Equal(0, shifted[1, 0].A);
        }

        [Fact]
        public void Stage_deck_is_found_from_the_image_and_placed_at_the_configured_height()
        {
            Stage stage = TestWorld.NewStage();

            Assert.Equal(stage.Def.DeckTopY, stage.Deck.Top, 1);
            Assert.Equal(stage.Def.CenterX, stage.Deck.Center.X, 1);
            Assert.InRange(stage.Deck.Width, 600, 900);
            Assert.InRange(stage.Deck.Height, 8, 120);
            Assert.True(stage.BlastZone.Width > stage.Bounds.Width);
        }
    }
}
