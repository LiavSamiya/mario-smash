using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    //the frames of one animation with the hurtboxes of every frame
    class SheetData
    {
        public SpriteSheet Sheet { get; }
        public List<Circle>[] Hurt { get; }
        //one texture per colour palette, created on first use
        readonly Texture2D[] textures = new Texture2D[Palette.Count];

        public SheetData(SpriteSheet sheet)
        {
            Sheet = sheet;
            Hurt = new List<Circle>[sheet.FrameCount];
            for (int f = 0; f < sheet.FrameCount; f++)
                Hurt[f] = AutoHitboxes.Hurtboxes(sheet, f);
        }

        public Texture2D Texture(GraphicsDevice gd, int palette)
        {
            if (textures[palette] == null)
                textures[palette] = Palette.Apply(Sheet.Image, palette).ToTexture(gd);
            return textures[palette];
        }
    }

    //an animation: a range of frames inside a sheet
    class Animation
    {
        public AnimationId Id { get; }
        public SheetData Data { get; }
        public int First { get; }
        public int Count { get; }
        public int FrameTicks { get; }
        public bool Loop { get; }

        public Animation(AnimationId id, SheetData data, AnimationDefinition def)
        {
            Id = id;
            Data = data;
            First = 0;
            Count = data.Sheet.FrameCount;
            FrameTicks = Math.Max(1, def.FrameTicks);
            Loop = def.Loop;
        }

        public int SheetFrame(int frame) => First + frame;
    }

    //one hit of a move, with its circles for every animation frame
    class HitData
    {
        public int Index { get; }
        public HitDefinition Def { get; }
        public int FirstFrame { get; }
        public int LastFrame { get; }
        //circles per animation frame (empty outside the active frames)
        public List<Circle>[] Circles { get; }

        public HitData(int index, HitDefinition def, Animation anim, float scale)
        {
            Index = index;
            Def = def;
            if (def.Frames == null || def.Frames.Length != 2)
                throw new InvalidDataException($"A hit of {anim.Id} needs \"frames\": [first, last]");
            if (def.Hitbox == null)
                throw new InvalidDataException($"A hit of {anim.Id} needs a \"hitbox\"");
            FirstFrame = def.Frames[0];
            LastFrame = def.Frames[1];
            if (FirstFrame < 0 || LastFrame >= anim.Count || LastFrame < FirstFrame)
                throw new InvalidDataException($"Hit frames {FirstFrame}-{LastFrame} are outside animation {anim.Id} (0-{anim.Count - 1})");

            Circles = new List<Circle>[anim.Count];
            for (int f = 0; f < anim.Count; f++)
            {
                Circles[f] = new List<Circle>();
                if (f < FirstFrame || f > LastFrame)
                    continue;
                int sheetFrame = anim.SheetFrame(f);
                //the radius is given in world pixels, circles are stored in sprite pixels
                Circles[f].AddRange(AutoHitboxes.Hitboxes(anim.Data.Sheet, sheetFrame, def.Hitbox.Direction, def.Hitbox.Radius / scale));
            }
        }

        public bool IsActive(int frame) => frame >= FirstFrame && frame <= LastFrame && Circles[frame].Count > 0;
    }

    class Move
    {
        public MoveId Id { get; }
        public MoveDefinition Def { get; }
        public Animation Animation { get; }
        public List<HitData> Hits { get; } = new List<HitData>();

        public Move(MoveId id, MoveDefinition def, Animation animation, float scale)
        {
            Id = id;
            Def = def;
            Animation = animation;
            for (int i = 0; i < def.Hits.Count; i++)
                Hits.Add(new HitData(i, def.Hits[i], animation, scale));
        }

        //the hit that is active on this animation frame, if any
        public HitData ActiveHit(int frame) => Hits.FirstOrDefault(h => h.IsActive(frame));
    }

    //a playable character: its stats, animations and moves
    class Character
    {
        public string Folder { get; }
        public CharacterDefinition Def { get; }
        public Dictionary<AnimationId, Animation> Animations { get; } = new Dictionary<AnimationId, Animation>();
        public Dictionary<MoveId, Move> Moves { get; } = new Dictionary<MoveId, Move>();
        //size of the body box used for stage collision, in world pixels
        public float BodyHalfWidth { get; }
        public float BodyHeight { get; }

        public string Name => Def.Name;
        public float Scale => Def.Scale;
        //height of the standing sprite in sheet pixels
        public int StandHeight { get; }

        //the scale that draws the standing sprite this many screen pixels tall (for menus and the HUD)
        //maxScale keeps small, low resolution sprites from turning into big blocks
        public float ScaleForHeight(float pixels, float maxScale = float.MaxValue) => Math.Min(maxScale, pixels / StandHeight);

        public Character(string folder)
        {
            Folder = folder;
            Def = DataLoader.Load<CharacterDefinition>(Path.Combine(folder, "character.json"));

            //the frames are cut out of the full sprite sheet in Content/Spritesheets
            FramesDefinition frames = DataLoader.Load<FramesDefinition>(Path.Combine(folder, "frames.json"));
            ImageData full = SheetBuilder.LoadSheet(Path.Combine(G.ContentRoot, "Spritesheets", frames.Sheet));
            var bg = new Color(frames.Background[0], frames.Background[1], frames.Background[2]);
            foreach (AnimationId id in Enum.GetValues(typeof(AnimationId)))
            {
                if (!frames.Animations.TryGetValue(id, out AnimationDefinition def))
                    throw new InvalidDataException($"{Def.Name} has no animation '{id}'");
                SpriteSheet sheet = SheetBuilder.Build(Def.Name + " " + id, full, bg, frames.Flip, def.Frames);
                Animations[id] = new Animation(id, new SheetData(sheet), def);
            }

            foreach (MoveId id in Enum.GetValues(typeof(MoveId)))
            {
                if (!Def.Moves.TryGetValue(id, out MoveDefinition def))
                    throw new InvalidDataException($"{Def.Name} has no move '{id}'");
                Moves[id] = new Move(id, def, Animations[def.Animation], Scale);
            }

            Animation stand = Animations[AnimationId.Stand];
            Rectangle body = AutoHitboxes.Bounds(stand.Data.Sheet, stand.First);
            BodyHalfWidth = body.Width * Scale * 0.3f;
            BodyHeight = body.Height * Scale;
            StandHeight = body.Height;
        }

        //loads every folder in Content/Characters that has a character.json
        public static List<Character> LoadAll()
        {
            string root = Path.Combine(G.ContentRoot, "Characters");
            return Directory.GetDirectories(root)
                .Where(d => File.Exists(Path.Combine(d, "character.json")))
                .Select(d => new Character(d))
                .OrderBy(c => c.Def.Order)
                .ThenBy(c => c.Name)
                .ToList();
        }
    }
}
