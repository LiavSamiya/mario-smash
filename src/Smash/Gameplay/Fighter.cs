using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Smash
{
    enum FighterState
    {
        Idle,
        Run,
        Turn,
        JumpSquat,
        Airborne,
        Landing,
        Attack,
        Shield,
        Roll,
        SpotDodge,
        AirDodge,
        Hitstun,
        ShieldBreak,
        Dead,
        Respawn,
        LedgeHang,
    }

    class FighterStats
    {
        public int KOs;
        public int Falls;
        public int SelfDestructs;
        public float DamageDealt;
        public float DamageTaken;
    }

    //a fighter in a match. instead of inheriting behaviour (Player -> Collisions -> Mario) it is composed of
    //a Character (stats, animations, moves), an input source (BaseKeys) and an AnimationPlayer
    class Fighter : IDisposable
    {
        #region tuning
        const int TurnTicks = 4;
        public const float MaxShield = 50;
        const float ShieldDrain = 0.14f;
        const float ShieldRegen = 0.07f;
        const int RollTicks = 24, RollInvStart = 3, RollInvEnd = 18;
        const float RollSpeed = 6.5f;
        const int SpotDodgeTicks = 22, SpotInvStart = 2, SpotInvEnd = 17;
        const int AirDodgeTicks = 30, AirInvStart = 2, AirInvEnd = 22;
        const int AirDodgeLandingLag = 10;
        //a directional air dodge moves the fighter, which also helps to get back to the stage
        const float AirDodgeSpeed = 8;
        const int AirDodgeMoveTicks = 12;
        //the small area next to a tip of the platform where a falling fighter grabs the ledge:
        //the body may be up to LedgeReachX away from the tip (or overlap it by LedgeOverlap),
        //and the hands must be between LedgeReachAbove above and LedgeReachY below the tip
        const float LedgeReachX = 14;
        const float LedgeOverlap = 6;
        const float LedgeReachAbove = 14;
        const float LedgeReachY = 24;
        const int LedgeClimbLag = 8;
        const int LedgeInvincibility = 24;
        //about one second of hanging, then the fighter lets go
        const int LedgeHangTicks = 60;
        //after letting go the same fighter cannot grab again right away
        const int LedgeRegrabTicks = 45;
        const int ShieldBreakTicks = 180;
        const int RespawnDelay = 60;
        const int RespawnInvincibility = 120;
        const int RespawnMaxWait = 180;
        const int ReboundLag = 12;
        //how long the last attacker gets the credit for a KO
        const int AttackerMemoryTicks = 600;
        #endregion

        #region data
        public int Index { get; }
        public Character Character { get; }
        public int PaletteIndex { get; }
        public BaseKeys Keys { get; }
        public Color TagColor { get; }
        public string Tag { get; }

        //position of the feet
        public Vector2 Position;
        //movement controlled by the fighter
        public Vector2 Velocity;
        //movement caused by being hit, decays over time
        public Vector2 LaunchVelocity;
        public bool FacingRight;
        public bool Grounded;
        public float Damage;
        public int Stocks;
        public float ShieldHealth = MaxShield;
        //freeze frames after a hit connects
        public int Hitlag;
        public FighterStats Stats { get; } = new FighterStats();
        public AnimationPlayer Anim { get; } = new AnimationPlayer();

        public FighterState State { get; private set; }
        public Move CurrentMove { get; private set; }
        //increases for every attack, so one attack can only hit a target once
        public int AttackInstance { get; private set; }
        public Fighter LastAttacker { get; private set; }
        public bool UsedDoubleJump { get; private set; }
        public bool UsedAirDodge => usedAirDodge;

        int stateTicks;
        int hitstun;
        int invincible;
        int landingLag;
        int rollDirection;
        int flashTicks;
        int lastAttackerTicks;
        bool usedAirDodge;
        Vector2 airDodgeDirection;
        //-1 = hanging on the left corner, 1 = hanging on the right corner
        int ledgeSide;
        int regrabCooldown;
        bool fastFalling;
        bool followUpQueued;
        //the last attack of each opponent that hit this fighter: attacker index -> (attack instance, hit index)
        readonly Dictionary<int, (int instance, int hit)> hitsTaken = new Dictionary<int, (int, int)>();
        readonly Match match;
        #endregion

        public Fighter(Match match, int index, Character character, int palette, BaseKeys keys, Color tagColor, string tag, Vector2 spawn, int stocks)
        {
            this.match = match;
            Index = index;
            Character = character;
            PaletteIndex = palette;
            Keys = keys;
            TagColor = tagColor;
            Tag = tag;
            Position = spawn;
            Stocks = stocks;
            FacingRight = spawn.X < match.Stage.Deck.Center.X;
            Grounded = true;
            SetState(FighterState.Idle);
            match.event_update += Update;
            match.event_draw += Draw;
        }

        //removes this fighter from the match's events, so nothing keeps updating it
        public void Dispose()
        {
            match.event_update -= Update;
            match.event_draw -= Draw;
        }

        #region properties
        CharacterDefinition Def => Character.Def;
        public float Scale => Character.Scale;
        public bool Eliminated => Stocks <= 0 && State == FighterState.Dead;
        public bool IsShielding => State == FighterState.Shield;
        public int StateTicks => stateTicks;

        //true while attacks pass through this fighter
        public bool IsIntangible =>
            invincible > 0
            || State == FighterState.Dead
            || State == FighterState.Respawn
            || (State == FighterState.Roll && stateTicks >= RollInvStart && stateTicks <= RollInvEnd)
            || (State == FighterState.SpotDodge && stateTicks >= SpotInvStart && stateTicks <= SpotInvEnd)
            || (State == FighterState.AirDodge && stateTicks >= AirInvStart && stateTicks <= AirInvEnd);

        public Box Body => new Box(Position.X - Character.BodyHalfWidth, Position.Y - Character.BodyHeight, Position.X + Character.BodyHalfWidth, Position.Y);

        public HitData ActiveHit => State == FighterState.Attack ? CurrentMove.ActiveHit(Anim.Frame) : null;

        public IEnumerable<Circle> HitCircles(HitData hit) =>
            hit.Circles[Anim.Frame].Select(c => c.ToWorld(Position, Scale, FacingRight));

        public IEnumerable<Circle> HurtCircles =>
            Anim.Current.Data.Hurt[Anim.SheetFrame].Select(c => c.ToWorld(Position, Scale, FacingRight));

        public Circle ShieldCircle =>
            new Circle(Position - new Vector2(0, Character.BodyHeight / 2), Character.BodyHeight * 0.62f * (0.45f + 0.55f * ShieldHealth / MaxShield));
        #endregion

        #region update
        public void Update()
        {
            Keys.Update();
            if (!match.Started || match.Over)
            {
                Anim.Update();
                return;
            }
            if (State == FighterState.Dead)
            {
                if (++stateTicks >= RespawnDelay && Stocks > 0)
                    BeginRespawn();
                return;
            }
            if (Hitlag > 0)
            {
                Hitlag--;
                return;
            }
            if (invincible > 0) invincible--;
            if (flashTicks > 0) flashTicks--;
            if (regrabCooldown > 0) regrabCooldown--;
            if (lastAttackerTicks > 0 && --lastAttackerTicks == 0) LastAttacker = null;
            if (State != FighterState.Shield)
                ShieldHealth = Math.Min(MaxShield, ShieldHealth + ShieldRegen);
            stateTicks++;

            //walked or was pushed off the edge
            if (!Grounded && IsGroundState(State))
                SetState(FighterState.Airborne);

            switch (State)
            {
                case FighterState.Idle: UpdateIdle(); break;
                case FighterState.Run: UpdateRun(); break;
                case FighterState.Turn: UpdateTurn(); break;
                case FighterState.JumpSquat: UpdateJumpSquat(); break;
                case FighterState.Airborne: UpdateAirborne(); break;
                case FighterState.Landing: UpdateLanding(); break;
                case FighterState.Attack: UpdateAttack(); break;
                case FighterState.Shield: UpdateShield(); break;
                case FighterState.Roll: UpdateRoll(); break;
                case FighterState.SpotDodge: UpdateSpotDodge(); break;
                case FighterState.AirDodge: UpdateAirDodge(); break;
                case FighterState.Hitstun: UpdateHitstun(); break;
                case FighterState.ShieldBreak: UpdateShieldBreak(); break;
                case FighterState.Respawn: UpdateRespawn(); break;
                case FighterState.LedgeHang: UpdateLedgeHang(); break;
            }

            ApplyPhysics();
            Anim.Update();
        }

        static bool IsGroundState(FighterState s) =>
            s == FighterState.Idle || s == FighterState.Run || s == FighterState.Turn || s == FighterState.JumpSquat
            || s == FighterState.Landing || s == FighterState.Shield || s == FighterState.Roll || s == FighterState.SpotDodge;

        //actions that can start from standing, running or turning. returns true if the state changed
        bool TryGroundActions()
        {
            if (Keys.Shield() && ShieldHealth > 0)
            {
                SetState(FighterState.Shield);
                return true;
            }
            if (Keys.Pressed(InputButtons.Up))
            {
                SetState(FighterState.JumpSquat);
                return true;
            }
            if (Keys.Pressed(InputButtons.Attack))
            {
                if (Keys.Down())
                    StartMove(MoveId.DownSmash);
                else if (State == FighterState.Run || Keys.Up())
                    StartMove(MoveId.UpTilt);
                else
                    StartMove(MoveId.Jab);
                return true;
            }
            return false;
        }

        void UpdateIdle()
        {
            Velocity.X = Approach(Velocity.X, 0, Def.GroundFriction);
            if (TryGroundActions())
                return;
            int dir = Keys.Horizontal();
            if (dir != 0)
                StartRunOrTurn(dir);
        }

        void UpdateRun()
        {
            if (TryGroundActions())
                return;
            int dir = Keys.Horizontal();
            if (dir == 0)
            {
                SetState(FighterState.Idle);
                return;
            }
            if (dir > 0 != FacingRight)
            {
                StartRunOrTurn(dir);
                return;
            }
            Velocity.X = Approach(Velocity.X, dir * Def.RunSpeed, Def.GroundAcceleration);
        }

        void StartRunOrTurn(int dir)
        {
            if (dir > 0 != FacingRight)
            {
                FacingRight = dir > 0;
                SetState(FighterState.Turn);
            }
            else
            {
                SetState(FighterState.Run);
            }
        }

        void UpdateTurn()
        {
            Velocity.X = Approach(Velocity.X, 0, Def.GroundFriction * 2);
            if (TryGroundActions())
                return;
            if (stateTicks < TurnTicks)
                return;
            int dir = Keys.Horizontal();
            if (dir == 0)
                SetState(FighterState.Idle);
            else
                StartRunOrTurn(dir);
        }

        void UpdateJumpSquat()
        {
            Velocity.X *= 0.9f;
            //attacking during the jump squat performs an up smash (like "up smash out of jump squat")
            if (Keys.Pressed(InputButtons.Attack))
            {
                StartMove(MoveId.UpSmash);
                return;
            }
            if (stateTicks < Def.JumpSquatTicks)
                return;
            //releasing Up before take-off gives a short hop
            Velocity.Y = -(Keys.Up() ? Def.JumpVelocity : Def.ShortHopVelocity);
            Grounded = false;
            SetState(FighterState.Airborne);
            Anim.Play(Character.Animations[AnimationId.Jump], restart: true);
            match.Audio.Play(Sfx.Jump);
        }

        void UpdateAirborne()
        {
            AirDrift(1);
            if (Keys.Pressed(InputButtons.Up) && !UsedDoubleJump)
            {
                UsedDoubleJump = true;
                fastFalling = false;
                Velocity.Y = -Def.DoubleJumpVelocity;
                Anim.Play(Character.Animations[AnimationId.Jump], restart: true);
                match.Audio.Play(Sfx.DoubleJump);
            }
            CheckFastFall();
            if (Keys.Pressed(InputButtons.Attack))
            {
                StartMove(Keys.Up() ? MoveId.UpAir : MoveId.NeutralAir);
                return;
            }
            if (Keys.Pressed(InputButtons.Shield) && !usedAirDodge)
            {
                SetState(FighterState.AirDodge);
                return;
            }
            if (Anim.Current.Id != AnimationId.Jump || (Anim.Finished && Velocity.Y > 0))
                Anim.Play(Character.Animations[AnimationId.Fall]);
        }

        void UpdateLanding()
        {
            Velocity.X = Approach(Velocity.X, 0, Def.GroundFriction);
            if (stateTicks >= landingLag)
                SetState(FighterState.Idle);
        }

        void UpdateAttack()
        {
            if (CurrentMove.Def.Aerial)
            {
                AirDrift(1);
                CheckFastFall();
            }
            else
            {
                Velocity.X = Approach(Velocity.X, 0, Def.GroundFriction * 0.5f);
            }
            if (CurrentMove.Def.FollowUp != null && Anim.Frame >= 1 && Keys.Pressed(InputButtons.Attack))
                followUpQueued = true;
            if (!Anim.Finished)
                return;
            if (followUpQueued && Grounded)
                StartMove(CurrentMove.Def.FollowUp.Value);
            else
                SetState(Grounded ? FighterState.Idle : FighterState.Airborne);
        }

        void UpdateShield()
        {
            Velocity.X = Approach(Velocity.X, 0, Def.GroundFriction);
            ShieldHealth -= ShieldDrain;
            if (ShieldHealth <= 0)
            {
                BreakShield();
                return;
            }
            if (!Keys.Shield())
            {
                SetState(FighterState.Idle);
                return;
            }
            //jump out of shield
            if (Keys.Pressed(InputButtons.Up))
            {
                SetState(FighterState.JumpSquat);
                return;
            }
            if (Keys.Pressed(InputButtons.Left) || Keys.Pressed(InputButtons.Right))
            {
                rollDirection = Keys.Pressed(InputButtons.Right) ? 1 : -1;
                SetState(FighterState.Roll);
                return;
            }
            if (Keys.Pressed(InputButtons.Down))
                SetState(FighterState.SpotDodge);
        }

        void UpdateRoll()
        {
            Velocity.X = rollDirection * RollSpeed * (stateTicks < RollTicks - 6 ? 1 : 0.3f);
            if (stateTicks < RollTicks)
                return;
            //like in Smash, a roll ends facing away from the direction of movement
            FacingRight = rollDirection < 0;
            Velocity.X = 0;
            SetState(Keys.Shield() ? FighterState.Shield : FighterState.Idle);
        }

        void UpdateSpotDodge()
        {
            Velocity.X = 0;
            if (stateTicks >= SpotDodgeTicks)
                SetState(Keys.Shield() ? FighterState.Shield : FighterState.Idle);
        }

        void UpdateAirDodge()
        {
            if (AirDodgeMoving)
                Velocity = airDodgeDirection * AirDodgeSpeed * (1 - 0.5f * stateTicks / AirDodgeMoveTicks);
            else
                AirDrift(0.4f);
            if (stateTicks >= AirDodgeTicks)
                SetState(FighterState.Airborne);
        }

        void UpdateHitstun()
        {
            if (--hitstun > 0)
                return;
            SetState(Grounded ? FighterState.Idle : FighterState.Airborne);
        }

        void UpdateShieldBreak()
        {
            Velocity.X = Approach(Velocity.X, 0, Def.GroundFriction);
            Anim.Play(Character.Animations[Grounded ? AnimationId.Stand : AnimationId.Fall]);
            if (stateTicks >= ShieldBreakTicks)
            {
                ShieldHealth = MaxShield * 0.6f;
                SetState(Grounded ? FighterState.Idle : FighterState.Airborne);
            }
        }

        void UpdateRespawn()
        {
            Velocity = Vector2.Zero;
            LaunchVelocity = Vector2.Zero;
            bool moved = Keys.Pressed(InputButtons.Left) || Keys.Pressed(InputButtons.Right)
                || Keys.Pressed(InputButtons.Down) || Keys.Pressed(InputButtons.Up) || Keys.Pressed(InputButtons.Attack);
            if (moved || stateTicks >= RespawnMaxWait)
                SetState(FighterState.Airborne);
        }

        void AirDrift(float control)
        {
            int dir = Keys.Horizontal();
            if (dir != 0)
                Velocity.X = Approach(Velocity.X, dir * Def.AirSpeed, Def.AirAcceleration * control);
            else
                Velocity.X = Approach(Velocity.X, 0, Def.AirFriction);
        }

        //pressing down after the top of a jump falls faster
        void CheckFastFall()
        {
            if (!fastFalling && Keys.Pressed(InputButtons.Down) && Velocity.Y > -2)
                fastFalling = true;
        }
        #endregion

        #region state changes
        void SetState(FighterState state)
        {
            State = state;
            stateTicks = 0;
            if (state != FighterState.Attack)
                CurrentMove = null;
            switch (state)
            {
                case FighterState.Idle:
                case FighterState.SpotDodge:
                case FighterState.ShieldBreak:
                case FighterState.Respawn:
                    Anim.Play(Character.Animations[AnimationId.Stand]);
                    break;
                case FighterState.Shield:
                    Anim.Play(Character.Animations[AnimationId.Shield]);
                    break;
                case FighterState.LedgeHang:
                    Anim.Play(Character.Animations[AnimationId.Hang], restart: true);
                    break;
                case FighterState.Run:
                case FighterState.Roll:
                    Anim.Play(Character.Animations[AnimationId.Run]);
                    break;
                case FighterState.Turn:
                    Anim.Play(Character.Animations[AnimationId.Turn], restart: true);
                    break;
                case FighterState.JumpSquat:
                    Anim.Play(Character.Animations[AnimationId.JumpSquat], restart: true);
                    break;
                case FighterState.Landing:
                    Anim.Play(Character.Animations[AnimationId.Landing], restart: true);
                    break;
                case FighterState.Airborne:
                    if (Anim.Current?.Id != AnimationId.Jump)
                        Anim.Play(Character.Animations[AnimationId.Fall]);
                    break;
                case FighterState.AirDodge:
                    usedAirDodge = true;
                    fastFalling = false;
                    airDodgeDirection = new Vector2(Keys.Horizontal(), Keys.Up() ? -1 : Keys.Down() ? 1 : 0);
                    if (airDodgeDirection != Vector2.Zero)
                    {
                        airDodgeDirection.Normalize();
                        Velocity = airDodgeDirection * AirDodgeSpeed;
                    }
                    Anim.Play(Character.Animations[AnimationId.Fall]);
                    break;
                case FighterState.Hitstun:
                    Anim.Play(Character.Animations[AnimationId.Fall]);
                    break;
            }
        }

        void StartMove(MoveId id)
        {
            SetState(FighterState.Attack);
            CurrentMove = Character.Moves[id];
            AttackInstance++;
            followUpQueued = false;
            Anim.Play(CurrentMove.Animation, restart: true);
        }

        void OnLand()
        {
            UsedDoubleJump = false;
            usedAirDodge = false;
            fastFalling = false;
            switch (State)
            {
                case FighterState.Attack when CurrentMove.Def.Aerial:
                    landingLag = CurrentMove.Def.LandingLag;
                    SetState(FighterState.Landing);
                    break;
                case FighterState.Airborne:
                    landingLag = Def.LandingTicks;
                    SetState(FighterState.Landing);
                    break;
                case FighterState.AirDodge:
                    landingLag = AirDodgeLandingLag;
                    SetState(FighterState.Landing);
                    break;
            }
        }

        void BreakShield()
        {
            ShieldHealth = 0;
            SetState(FighterState.ShieldBreak);
            Grounded = false;
            Velocity.Y = -9;
            match.Audio.Play(Sfx.ShieldBreak);
        }

        void BeginRespawn()
        {
            Position = match.Stage.RespawnPoint(Index);
            Velocity = Vector2.Zero;
            LaunchVelocity = Vector2.Zero;
            Damage = 0;
            ShieldHealth = MaxShield;
            UsedDoubleJump = false;
            usedAirDodge = false;
            fastFalling = false;
            Grounded = false;
            hitsTaken.Clear();
            FacingRight = Position.X < match.Stage.Deck.Center.X;
            invincible = RespawnInvincibility;
            SetState(FighterState.Respawn);
        }

        //called when the fighter leaves the blast zone
        public void KnockOut()
        {
            Stocks--;
            Stats.Falls++;
            if (LastAttacker != null)
                LastAttacker.Stats.KOs++;
            else
                Stats.SelfDestructs++;
            LastAttacker = null;
            Velocity = Vector2.Zero;
            LaunchVelocity = Vector2.Zero;
            Hitlag = 0;
            SetState(FighterState.Dead);
        }

        //the attack of an opponent connected
        public void TakeHit(Fighter attacker, float damage, Vector2 launch, int hitstunTicks)
        {
            Damage = Math.Min(999, Damage + damage);
            Stats.DamageTaken += damage;
            attacker.Stats.DamageDealt += damage;
            LastAttacker = attacker;
            lastAttackerTicks = AttackerMemoryTicks;
            SetState(FighterState.Hitstun);
            hitstun = Math.Max(6, hitstunTicks);
            Velocity = Vector2.Zero;
            LaunchVelocity = launch;
            if (launch.Y < -0.5f)
                Grounded = false;
            fastFalling = false;
            flashTicks = 10;
        }

        //an attack hit this fighter's shield
        public void ShieldHit(float damage, int direction)
        {
            ShieldHealth -= damage * 1.2f + 1;
            LaunchVelocity = new Vector2(direction * (1.5f + damage * 0.12f), 0);
            if (ShieldHealth <= 0)
                BreakShield();
        }

        //both attacks clashed: cancel the attack and bounce back a little
        public void Rebound()
        {
            LaunchVelocity = new Vector2((FacingRight ? -1 : 1) * 3, 0);
            landingLag = ReboundLag;
            SetState(Grounded ? FighterState.Landing : FighterState.Airborne);
        }

        public bool AlreadyHitBy(Fighter attacker, HitData hit) =>
            hitsTaken.TryGetValue(attacker.Index, out var last) && last.instance == attacker.AttackInstance && last.hit == hit.Index;

        public void RememberHit(Fighter attacker, HitData hit) =>
            hitsTaken[attacker.Index] = (attacker.AttackInstance, hit.Index);
        #endregion

        #region physics
        void ApplyPhysics()
        {
            if (State == FighterState.Respawn || State == FighterState.Dead || State == FighterState.LedgeHang)
                return;

            if (Grounded)
            {
                Velocity.Y = Math.Min(0, Velocity.Y);
            }
            else if (AirDodgeMoving)
            {
                //no gravity during the first part of a directional air dodge
            }
            else if (fastFalling)
            {
                Velocity.Y = Def.FastFallSpeed;
            }
            else
            {
                Velocity.Y = Math.Min(Velocity.Y + Def.Gravity, Def.MaxFallSpeed);
            }

            float speed = LaunchVelocity.Length();
            if (speed > 0)
            {
                float decayed = Math.Max(0, speed - Knockback.LaunchDecay);
                LaunchVelocity = decayed == 0 ? Vector2.Zero : LaunchVelocity * (decayed / speed);
            }
            if (Grounded)
                LaunchVelocity.X = Approach(LaunchVelocity.X, 0, Def.GroundFriction * 0.5f);

            Move(Velocity + LaunchVelocity);
            TryCatchLedge();
        }

        bool AirDodgeMoving => State == FighterState.AirDodge && airDodgeDirection != Vector2.Zero && stateTicks <= AirDodgeMoveTicks;

        //a fighter falling past a corner of the stage grabs it and hangs there
        void TryCatchLedge()
        {
            bool canGrab = State == FighterState.Airborne || State == FighterState.AirDodge
                || (State == FighterState.Attack && CurrentMove.Def.Aerial);
            if (Grounded || !canGrab || regrabCooldown > 0 || Velocity.Y + LaunchVelocity.Y < -2)
                return;
            Stage stage = match.Stage;
            float hw = Character.BodyHalfWidth;
            //only a small area right next to the tip of the platform counts: the side of the body
            //nearest the stage must be close to the tip, and the hands (top of the body) near its height
            float head = Position.Y - Character.BodyHeight;
            bool Near(Vector2 ledge, int side)
            {
                float gap = side < 0 ? ledge.X - (Position.X + hw) : (Position.X - hw) - ledge.X;
                return gap >= -LedgeOverlap && gap <= LedgeReachX && head >= ledge.Y - LedgeReachAbove && head <= ledge.Y + LedgeReachY;
            }
            bool left = Near(stage.LedgeLeft, -1);
            bool right = !left && Near(stage.LedgeRight, 1);
            if (!left && !right)
                return;

            ledgeSide = left ? -1 : 1;
            //while hanging the position is the tip the hands hold
            Position = left ? stage.LedgeLeft : stage.LedgeRight;
            Velocity = Vector2.Zero;
            LaunchVelocity = Vector2.Zero;
            FacingRight = left;
            UsedDoubleJump = false;
            usedAirDodge = false;
            fastFalling = false;
            invincible = Math.Max(invincible, LedgeInvincibility);
            SetState(FighterState.LedgeHang);
            match.Audio.Play(Sfx.LedgeGrab);
        }

        //Up, Attack or towards the stage climbs up; Down or away lets go; after a second the fighter falls
        void UpdateLedgeHang()
        {
            Velocity = Vector2.Zero;
            LaunchVelocity = Vector2.Zero;
            InputButtons toward = ledgeSide < 0 ? InputButtons.Right : InputButtons.Left;
            InputButtons away = ledgeSide < 0 ? InputButtons.Left : InputButtons.Right;
            if (stateTicks > 4 && (Keys.Pressed(InputButtons.Up) || Keys.Pressed(InputButtons.Attack) || Keys.Pressed(toward)))
            {
                Stage stage = match.Stage;
                float hw = Character.BodyHalfWidth;
                float x = ledgeSide < 0 ? stage.Deck.Left + hw + 2 : stage.Deck.Right - hw - 2;
                Position = new Vector2(x, stage.SurfaceY(x));
                Grounded = true;
                landingLag = LedgeClimbLag;
                SetState(FighterState.Landing);
                match.Audio.Play(Sfx.Jump);
            }
            else if (stateTicks >= LedgeHangTicks || (stateTicks > 4 && (Keys.Pressed(InputButtons.Down) || Keys.Pressed(away))))
            {
                LetGoOfLedge();
            }
        }

        void LetGoOfLedge()
        {
            Vector2 ledge = ledgeSide < 0 ? match.Stage.LedgeLeft : match.Stage.LedgeRight;
            //the body hangs below the tip, so it falls from there, just outside the stage
            Position = new Vector2(ledge.X + ledgeSide * (Character.BodyHalfWidth + 2), ledge.Y + Character.BodyHeight);
            regrabCooldown = LedgeRegrabTicks;
            SetState(FighterState.Airborne);
        }

        //moves and resolves collisions with the stage, which is solid from every side
        void Move(Vector2 delta)
        {
            Stage stage = match.Stage;
            Box deck = stage.Deck;
            float prevX = Position.X;
            float prevY = Position.Y;
            bool wasGrounded = Grounded;
            Position += delta;

            if (Grounded)
            {
                if (Position.X < deck.Left || Position.X > deck.Right)
                    Grounded = false;
                else
                    Position.Y = stage.SurfaceY(Position.X);
                return;
            }

            if (!Body.Overlaps(deck))
                return;

            //the surface is not flat everywhere (the tips slope down): above it there is nothing to hit
            bool overDeck = Position.X >= deck.Left && Position.X <= deck.Right;
            float surfaceY = overDeck ? stage.SurfaceY(Position.X) : deck.Top;
            bool wasAbove = prevY <= stage.SurfaceY(MathHelper.Clamp(prevX, deck.Left, deck.Right)) + 0.5f;
            if (overDeck && Position.Y < surfaceY && wasAbove)
                return;

            bool fromAbove = wasAbove && delta.Y >= 0;
            if (fromAbove && overDeck)
            {
                //a tumbling fighter hitting the ground hard bounces
                if (State == FighterState.Hitstun && LaunchVelocity.Y > 4)
                {
                    Position.Y = surfaceY;
                    LaunchVelocity.Y = -LaunchVelocity.Y * 0.6f;
                    Velocity.Y = 0;
                    return;
                }
                Position.Y = surfaceY;
                Velocity.Y = 0;
                LaunchVelocity.Y = 0;
                Grounded = true;
                if (!wasGrounded)
                    OnLand();
                return;
            }

            //push out through the side with the smallest overlap
            Box body = Body;
            float pushLeft = body.Right - deck.Left;
            float pushRight = deck.Right - body.Left;
            float pushDown = deck.Bottom - body.Top;
            float min = Math.Min(pushLeft, Math.Min(pushRight, pushDown));
            if (min == pushDown && !fromAbove)
            {
                Position.Y += pushDown;
                if (Velocity.Y < 0) Velocity.Y = 0;
                if (LaunchVelocity.Y < 0) LaunchVelocity.Y = -LaunchVelocity.Y * 0.5f;
            }
            else if (pushLeft <= pushRight)
            {
                Position.X -= pushLeft;
                if (Velocity.X > 0) Velocity.X = 0;
                if (LaunchVelocity.X > 0) LaunchVelocity.X = -LaunchVelocity.X * 0.5f;
            }
            else
            {
                Position.X += pushRight;
                if (Velocity.X < 0) Velocity.X = 0;
                if (LaunchVelocity.X < 0) LaunchVelocity.X = -LaunchVelocity.X * 0.5f;
            }
        }

        static float Approach(float value, float target, float step)
        {
            if (value < target) return Math.Min(value + step, target);
            if (value > target) return Math.Max(value - step, target);
            return value;
        }
        #endregion

        #region draw
        public void Draw(SpriteBatch sb)
        {
            if (State == FighterState.Dead)
                return;
            Primitives prims = match.Prims;

            if (State == FighterState.Respawn)
                prims.Rect(sb, new Rectangle((int)Position.X - 40, (int)Position.Y, 80, 6), TagColor);

            SpriteSheet sheet = Anim.Current.Data.Sheet;
            int frame = Anim.SheetFrame;
            Rectangle src = sheet.Frames[frame];
            Vector2 origin = sheet.Origins[frame];
            if (!FacingRight)
                origin.X = src.Width - origin.X;

            float alpha = 1;
            if (State == FighterState.Roll || State == FighterState.SpotDodge || State == FighterState.AirDodge)
                alpha = IsIntangible ? 0.45f : 0.8f;
            else if (invincible > 0 && invincible / 4 % 2 == 0)
                alpha = 0.5f;
            Color color = flashTicks > 0 ? new Color(255, 140, 140) : Color.White;

            Texture2D tex = Anim.Current.Data.Texture(match.Graphics, PaletteIndex);
            sb.Draw(tex, new Vector2((int)Position.X, (int)Position.Y), src, color * alpha, 0, origin, Scale,
                FacingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);

            if (State == FighterState.Shield)
            {
                //a slowly turning, pulsing energy bubble that turns red and flickers as it weakens
                Circle shield = ShieldCircle;
                float health = ShieldHealth / MaxShield;
                float pulse = 0.85f + 0.15f * (float)Math.Sin(match.ElapsedTicks * 0.25f);
                if (health < 0.3f && match.ElapsedTicks / 3 % 2 == 0)
                    pulse *= 0.5f;
                Color tint = Color.Lerp(new Color(255, 60, 40), TagColor, health);
                Texture2D bubble = prims.ShieldBubble;
                sb.Draw(bubble, shield.Center, null, tint * (0.85f * pulse), match.ElapsedTicks * 0.01f,
                    new Vector2(bubble.Width / 2f), shield.Radius * 2 / bubble.Width, SpriteEffects.None, 0);
                prims.Circle(sb, shield.Center + new Vector2(-0.35f, -0.4f) * shield.Radius, shield.Radius * 0.16f, Color.White * (0.35f * pulse));
            }
            if (State == FighterState.ShieldBreak)
            {
                //dizzy stars around the head
                for (int i = 0; i < 3; i++)
                {
                    float a = match.ElapsedTicks * 0.12f + i * MathHelper.TwoPi / 3;
                    Vector2 p = Position + new Vector2((float)Math.Cos(a) * 22, -Character.BodyHeight - 8 + (float)Math.Sin(a) * 6);
                    prims.Circle(sb, p, 4, Color.Yellow);
                }
            }

            //player tag above the head
            float top = Position.Y - Character.BodyHeight - 14;
            match.Font.DrawCentered(sb, Tag, new Vector2(Position.X, top - 8), TagColor, 2);
            prims.Rect(sb, new Rectangle((int)Position.X - 3, (int)top, 6, 4), TagColor);
        }

        //shows the hurtboxes (blue), hitboxes (red) and the body box used for stage collision
        public void DrawDebug(SpriteBatch sb)
        {
            if (State == FighterState.Dead)
                return;
            Primitives prims = match.Prims;
            Color hurt = IsIntangible ? Color.White * 0.35f : new Color(60, 90, 255) * 0.45f;
            foreach (Circle c in HurtCircles)
                prims.Circle(sb, c.Center, c.Radius, hurt);
            HitData hit = ActiveHit;
            if (hit != null)
                foreach (Circle c in HitCircles(hit))
                    prims.Circle(sb, c.Center, c.Radius, new Color(255, 30, 30) * 0.6f);
            prims.RectOutline(sb, Body.ToRectangle(), Color.Yellow * 0.8f);
        }
        #endregion
    }
}
