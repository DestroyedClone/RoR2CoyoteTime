using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions;
using RoR2;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace ror2coyotetime
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.DestroyedClone.CoyoteTime", "Coyote Time", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public static ConfigEntry<float> cfgWindowOfTimeForActivation;

        public void Start()
        {
            cfgWindowOfTimeForActivation = Config.Bind(string.Empty, "Time Window", 0.3f, "The amount of time in seconds that the character can walk off a platform before they can no longer jump.");

            On.RoR2.CharacterMotor.OnLeaveStableGround += OnLeaveStableGround;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune560.riskofoptions"))
            {
                Compat_RiskOfOptions();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Compat_RiskOfOptions()
        {
            ModSettingsManager.AddOption(new RiskOfOptions.Options.SliderOption(cfgWindowOfTimeForActivation));
            ModSettingsManager.SetModDescription("Adds Coyote Time to RoR2", "com.DestroyedClone.CoyoteTime", "Coyote Time");
        }

        private void OnLeaveStableGround(On.RoR2.CharacterMotor.orig_OnLeaveStableGround orig, CharacterMotor self)
        {
            int initJumpCount = self.jumpCount;
            orig(self);
            if (self.jumpCount != initJumpCount)
            {
                self.jumpCount = initJumpCount;
                if (!self.gameObject.TryGetComponent(out RiskOfBulletstorm_CoyoteTimeController _))
                {
                    EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(self.gameObject, "Body");
                    if (entityStateMachine && entityStateMachine.IsInMainState())
                    {
                        RiskOfBulletstorm_CoyoteTimeController comp = self.gameObject.AddComponent<RiskOfBulletstorm_CoyoteTimeController>();
                        comp.entityStateMachine = entityStateMachine;
                        comp.characterMotor = self;
                        comp.jumpCountOnStart = initJumpCount;
                    }
                }
            }
        }

        private class RiskOfBulletstorm_CoyoteTimeController : MonoBehaviour
        {
            private float Duration => cfgWindowOfTimeForActivation.Value;
            public CharacterMotor characterMotor;
            public int jumpCountOnStart = 0;
            private float age = 0;
            public EntityStateMachine entityStateMachine;

            public void Awake()
            {
                if (!characterMotor)
                {
                    characterMotor = gameObject.GetComponent<CharacterMotor>();
                }
                characterMotor.useGravity = false;
            }

            public void FixedUpdate()
            {
                age += Time.fixedDeltaTime;

                if (characterMotor.isGrounded || age >= Duration)// || !entityStateMachine.IsInMainState())
                {
                    ConsumeJump();
                    Destroy(this);
                }
            }

            public void OnDestroy()
            {
                characterMotor.useGravity = characterMotor.gravityParameters.CheckShouldUseGravity();
            }

            public void ConsumeJump()
            {
                if (!characterMotor.isGrounded)
                {
                    if (characterMotor.jumpCount == jumpCountOnStart)
                    {
                        characterMotor.jumpCount += 1;
                    }
                }
            }
        }
    }
}