using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smash
{
    //every animation a character can play
    enum AnimationId
    {
        Stand,
        Run,
        Turn,
        JumpSquat,
        Jump,
        Fall,
        Landing,
        Jab,
        MultiJab,
        UpTilt,
        UpSmash,
        DownSmash,
        NeutralAir,
        UpAir,
        Hang,
        Shield,
    }

    //every attack a character can perform
    enum MoveId
    {
        Jab,
        MultiJab,
        UpTilt,
        UpSmash,
        DownSmash,
        NeutralAir,
        UpAir,
    }

    //one animation: a list of frames cut out of the character's full sprite sheet
    class AnimationDefinition
    {
        //how many ticks every frame is shown
        public int FrameTicks { get; set; } = 5;
        public bool Loop { get; set; }
        //every frame is [x, y, width, height, handleX, handleY] in sheet pixels.
        //the handle is the point between the feet, relative to the frame rectangle
        public int[][] Frames { get; set; }
    }

    //Content/Characters/<name>/frames.json, written by tools/SpriteCutter
    class FramesDefinition
    {
        //file in Content/Spritesheets
        public string Sheet { get; set; }
        //true when the character faces left on the sheet
        public bool Flip { get; set; }
        //background colour of the sheet [r, g, b]
        public int[] Background { get; set; }
        public Dictionary<AnimationId, AnimationDefinition> Animations { get; set; } = new Dictionary<AnimationId, AnimationDefinition>();
    }

    //a hitbox placed on the silhouette of the frame
    class HitboxDefinition
    {
        public HitboxDirection Direction { get; set; }
        //radius in world pixels
        public float Radius { get; set; } = 18;
    }

    //one hit of an attack
    class HitDefinition
    {
        //first and last animation frame in which the hit is active
        public int[] Frames { get; set; }
        //percent added to the target
        public float Damage { get; set; }
        //launch angle in degrees (0 = forward, 90 = up). 361 = the classic "Sakurai angle"
        public float Angle { get; set; }
        public float BaseKnockback { get; set; }
        public float KnockbackGrowth { get; set; }
        public HitboxDefinition Hitbox { get; set; }
    }

    class MoveDefinition
    {
        public AnimationId Animation { get; set; }
        public bool Aerial { get; set; }
        //ticks of landing lag when landing during this aerial
        public int LandingLag { get; set; }
        //attack performed when Attack is pressed again during this one (e.g. jab -> multi jab)
        public MoveId? FollowUp { get; set; }
        public List<HitDefinition> Hits { get; set; } = new List<HitDefinition>();
    }

    //all stats of a character, loaded from Content/Characters/<name>/character.json
    class CharacterDefinition
    {
        public string Name { get; set; }
        //position on the character select screen
        public int Order { get; set; }
        public float Scale { get; set; } = 2;
        public float Weight { get; set; } = 100;
        public float RunSpeed { get; set; }
        public float GroundAcceleration { get; set; }
        public float GroundFriction { get; set; }
        public float AirSpeed { get; set; }
        public float AirAcceleration { get; set; }
        public float AirFriction { get; set; }
        public float Gravity { get; set; }
        public float MaxFallSpeed { get; set; }
        public float FastFallSpeed { get; set; }
        public float JumpVelocity { get; set; }
        public float ShortHopVelocity { get; set; }
        public float DoubleJumpVelocity { get; set; }
        public int JumpSquatTicks { get; set; } = 3;
        public int LandingTicks { get; set; } = 4;
        public Dictionary<MoveId, MoveDefinition> Moves { get; set; } = new Dictionary<MoveId, MoveDefinition>();
    }

    //loaded from Content/Stages/stage.json
    class StageDefinition
    {
        public string Name { get; set; }
        public string Sheet { get; set; }
        public string Background { get; set; }
        public float BackgroundScale { get; set; } = 2;
        public float Scale { get; set; } = 2;
        public int FrameTicks { get; set; } = 15;
        //world x of the stage centre and world y of the walkable surface
        public float CenterX { get; set; }
        public float DeckTopY { get; set; }
        //how many sprite pixels below the top edge of the platform the fighters stand (the top face is drawn in perspective)
        public int SurfaceDepth { get; set; }
        //how far outside the background a fighter must fly to be knocked out
        public float BlastMargin { get; set; } = 80;
    }

    static class DataLoader
    {
        static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() },
        };

        public static T Load<T>(string path)
        {
            T value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), options);
            if (value == null)
                throw new InvalidDataException($"'{path}' is empty");
            return value;
        }
    }
}
