using KinematicCharacterController;
using RoR2;
using UnityEngine;

namespace PoppyMenu
{
    internal class MovementModule : PoppyModule
    {
        internal override string Name => "Move";

        internal static bool Flight;
        internal static bool NoClip;
        internal static bool AlwaysSprint;
        internal static bool JumpPack;
        internal static bool Bhop;
        internal static bool TeleportToPing;

        internal static void ToggleFlight() => Flight = !Flight;
        internal static void ToggleSprint() => AlwaysSprint = !AlwaysSprint;
        internal static void ToggleNoClip() => NoClip = !NoClip;
        internal static void ToggleTeleportToPing() => TeleportToPing = !TeleportToPing;

        private bool _gravityOff;
        private CharacterMotor _gravityMotor;
        private bool _collisionsDisabled;
        private KinematicCharacterMotor _kcm;
        private CharacterBody _lastBody;

        // Jump held while flying. The jump input is cleared so flying up does not spend jumps.
        private static bool _ascend;
        private static bool Flying => Flight || NoClip;

        internal override void Tick()
        {
            if (!PlayerContext.InGame || !PlayerContext.HasBody) return;

            CharacterMotor motor = PlayerContext.Motor;
            CharacterBody body = PlayerContext.Body;

            if (body != _lastBody)
            {
                _lastBody = body;
                _kcm = body.GetComponent<KinematicCharacterMotor>();
                _collisionsDisabled = false;
                _gravityOff = false;
            }

            if (_kcm != null && NoClip != _collisionsDisabled)
            {
                SetCollisions(!NoClip);
                _collisionsDisabled = NoClip;
            }

            bool flying = Flying;
            if (flying && motor != null)
            {
                _gravityMotor = motor;
                motor.useGravity = false;
                motor.disableAirControlUntilCollision = false;
                _gravityOff = true;

                Vector3 move = Vector3.zero;
                if (!InputCapture.Active && body.inputBank != null)
                {
                    // moveVector is the game's camera-relative movement input: follows rebinds and gamepads.
                    move = body.inputBank.moveVector;
                    move.y = 0f;
                    if (_ascend) move.y += 1f;
                    if (Input.GetKey(KeyCode.LeftControl)) move.y -= 1f;
                }

                if (move.sqrMagnitude > 0.01f)
                {
                    if (move.sqrMagnitude > 1f) move.Normalize();
                    motor.velocity = move * ModConfig.FlightSpeed.Value;
                }
                else
                {
                    motor.velocity = Vector3.Lerp(motor.velocity, Vector3.zero, 12f * Time.deltaTime);
                }
            }
            else if (_gravityOff)
            {
                RestoreGravity();
                _gravityMotor = null;
                _gravityOff = false;
            }

            if (AlwaysSprint && body != null)
            {
                bool isMoving = (body.inputBank != null && body.inputBank.moveVector.sqrMagnitude > 0.01f) || (motor != null && motor.velocity.sqrMagnitude > 0.1f);
                bool attacking = body.inputBank != null && (body.inputBank.skill1.down || body.inputBank.skill2.down || body.inputBank.skill3.down || body.inputBank.skill4.down);
                if (isMoving && !attacking)
                    body.isSprinting = true;
            }

            if (JumpPack && motor != null && !InputCapture.Active && Input.GetKeyDown(KeyCode.Space) && !flying)
            {
                Vector3 v = motor.velocity; v.y = 40f; motor.velocity = v;
            }

        } // End of Tick()

        internal static void Init()
        {
            On.RoR2.PlayerCharacterMasterController.FixedUpdate += PlayerCharacterMasterController_FixedUpdate;
            On.RoR2.PingerController.SetCurrentPing += PingerController_SetCurrentPing;
        }

        private static void PingerController_SetCurrentPing(On.RoR2.PingerController.orig_SetCurrentPing orig, PingerController self, PingerController.PingInfo newPingInfo)
        {
            orig(self, newPingInfo);

            if (TeleportToPing && self != null && self.hasAuthority && newPingInfo.active)
            {
                TeleportToPingInfo(newPingInfo);
            }
        }

        internal static void TeleportToCurrentPing()
        {
            if (!PlayerContext.InGame || !PlayerContext.HasBody)
            {
                Notify.Push("Not in game");
                return;
            }

            CharacterMaster master = PlayerContext.Master;
            if (master == null)
            {
                Notify.Push("No master");
                return;
            }

            PingerController pinger = master.GetComponent<PingerController>();
            if (pinger == null || !pinger.currentPing.active)
            {
                Notify.Push("No active ping");
                return;
            }

            TeleportToPingInfo(pinger.currentPing);
        }

        internal static void TeleportToPingInfo(PingerController.PingInfo pingInfo)
        {
            if (!PlayerContext.HasBody) return;
            CharacterBody body = PlayerContext.Body;

            Vector3 targetPos;
            if (pingInfo.targetGameObject != null)
            {
                targetPos = pingInfo.targetGameObject.transform.position + Vector3.up * 1f;
            }
            else
            {
                targetPos = pingInfo.origin;
                if (pingInfo.normal.sqrMagnitude > 0.001f)
                    targetPos += pingInfo.normal * 0.5f;
                else
                    targetPos += Vector3.up * 0.5f;
            }

            ExecuteTeleport(body, targetPos, "Teleported to ping");
        }

        internal static void TeleportToCursor()
        {
            if (!PlayerContext.InGame || !PlayerContext.HasBody)
            {
                Notify.Push("Not in game");
                return;
            }

            CharacterBody body = PlayerContext.Body;
            Ray aim = PlayerContext.AimRay();

            LayerMask mask = (LayerMask)((int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask);
            if (Physics.Raycast(aim, out RaycastHit hit, 1000f, mask, QueryTriggerInteraction.Ignore))
            {
                Vector3 targetPos = hit.point + (hit.normal.sqrMagnitude > 0.001f ? hit.normal * 0.5f : Vector3.up * 0.5f);
                ExecuteTeleport(body, targetPos, "Teleported to cursor");
            }
            else
            {
                Notify.Push("No surface in view");
            }
        }

        private static void ExecuteTeleport(CharacterBody body, Vector3 targetPos, string message)
        {
            if (body == null) return;

            TeleportHelper.TeleportBody(body, targetPos);

            if (body.characterMotor != null)
            {
                body.characterMotor.velocity = Vector3.zero;
            }

            try
            {
                GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TeleportOutBoom");
                if (effectPrefab != null)
                {
                    EffectManager.SimpleEffect(effectPrefab, targetPos, Quaternion.identity, false);
                }
            }
            catch { }

            Notify.Push(message);
        }

        private static void PlayerCharacterMasterController_FixedUpdate(On.RoR2.PlayerCharacterMasterController.orig_FixedUpdate orig, PlayerCharacterMasterController self)
        {
            orig(self);

            if (Flying && self.hasEffectiveAuthority && self.body != null && self.body == PlayerContext.Body && self.bodyInputs != null)
            {
                _ascend = self.bodyInputs.jump.down;
                self.bodyInputs.jump.PushState(false);
                return;
            }
            _ascend = false;

            if (Bhop && self.hasEffectiveAuthority && self.body != null && !InputCapture.Active)
            {
                CharacterMotor motor = self.body.characterMotor;
                InputBankTest input = self.bodyInputs;

                // If player is holding jump and lands on the ground, trick the game into thinking jump was JUST pressed!
                // This triggers the 100% native RoR2 jump (with proper Wax Quail math, animations, sounds, and state machine transitions).
                if (motor != null && input != null && motor.isGrounded && input.jump.down)
                {
                    input.jump.PushState(false);
                    input.jump.PushState(true);
                }
            }
        }

        // The game derives gravity from gravityParameters (skills like hovering change them).
        // Ask it for the current value instead of forcing gravity on.
        private void RestoreGravity()
        {
            if (_gravityMotor != null) _gravityMotor.useGravity = _gravityMotor.gravityParameters.CheckShouldUseGravity();
        }

        private void SetCollisions(bool on)
        {
            if (_kcm == null) return;
            _kcm.SetCapsuleCollisionsActivation(on);
            _kcm.SetMovementCollisionsSolvingActivation(on);
            _kcm.SetGroundSolvingActivation(on);
        }

        internal override void OnUnload()
        {
            if (_kcm != null && _collisionsDisabled) SetCollisions(true);
            if (_gravityOff) RestoreGravity();
        }

        internal override void DrawMenu()
        {
            Widgets.SectionBegin("Movement");
            Flight = Widgets.Toggle("Flight", Flight, "Move keys to fly, Jump up, Left Ctrl down. You can still aim and fire while flying.");
            ModConfig.FlightSpeed.Value = Widgets.Slider("Fly Speed", ModConfig.FlightSpeed.Value, 5f, 150f, format: "0");
            NoClip = Widgets.Toggle("No-Clip (fly through walls)", NoClip);
            AlwaysSprint = Widgets.Toggle("Always Sprint", AlwaysSprint);
            JumpPack = Widgets.Toggle("Jump Pack", JumpPack);
            Bhop = Widgets.Toggle("BunnyHop (Auto-jump on land)", Bhop);
            Widgets.SectionEnd();

            Widgets.SectionBegin("Teleportation");
            TeleportToPing = Widgets.Toggle("Auto Teleport on Ping", TeleportToPing,
                "Instantly teleports your survivor to wherever you place a ping in-game.");
            Widgets.Buttons("Teleport now", null, ("To ping", TeleportToCurrentPing), ("To cursor", TeleportToCursor));
            Widgets.SectionEnd();
        }
    }
}
