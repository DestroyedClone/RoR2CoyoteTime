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
    [BepInPlugin(modGUID, "Coyote Time", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        public const string modGUID = "com.DestroyedClone.CoyoteTime";
        public static ConfigEntry<float> cfgWindowOfTimeForActivation;
        public static ConfigEntry<bool> cfgPlayerOnly;

        public const float maxSeconds = 1f;
        public const float minSeconds = 0f;

        public void Start()
        {
            cfgWindowOfTimeForActivation = Config.Bind(string.Empty, "Time Window", 0.3f, "The amount of time in seconds that the character can walk off a platform before they can no longer jump. Range: 0.00 to 1.00 seconds.");
            cfgPlayerOnly = Config.Bind("Server", "Player Only", true, "If true, then only players can activate coyote time. AI does not have the logic to use this correctly, though you can turn it on if you want to for some reason.");

            On.RoR2.CharacterMotor.OnLeaveStableGround += OnLeaveStableGround;
            cfgWindowOfTimeForActivation.SettingChanged += CfgWindowOfTimeForActivation_SettingChanged;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                Compat_RiskOfOptions();
            }
        }

        private void CfgWindowOfTimeForActivation_SettingChanged(object sender, System.EventArgs e)
        {
            if (cfgWindowOfTimeForActivation.Value < minSeconds)
                cfgWindowOfTimeForActivation.Value = minSeconds;
            else if (cfgWindowOfTimeForActivation.Value > maxSeconds)
                cfgWindowOfTimeForActivation.Value = maxSeconds;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void Compat_RiskOfOptions()
        {
            ModSettingsManager.SetModDescription("Adds Coyote Time to RoR2", modGUID, "Coyote Time");
            ModSettingsManager.AddOption(new RiskOfOptions.Options.StepSliderOption(cfgWindowOfTimeForActivation, new RiskOfOptions.OptionConfigs.StepSliderConfig()
            {
                min = minSeconds,
                max = maxSeconds,
                //increment = 0.01f
            }));
        }

        private void OnLeaveStableGround(On.RoR2.CharacterMotor.orig_OnLeaveStableGround orig, CharacterMotor self)
        {
            int initJumpCount = self.jumpCount;
            orig(self);
            if (!cfgPlayerOnly.Value || self.body.isPlayerControlled)
                if (self.jumpCount != initJumpCount)
                {
                    self.jumpCount = initJumpCount;
                    if (!self.gameObject.TryGetComponent(out CoyoteTimeBehaviour _))
                    {
                        EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(self.gameObject, "Body");
                        if (entityStateMachine && entityStateMachine.IsInMainState())
                        {
                            CoyoteTimeBehaviour comp = self.gameObject.AddComponent<CoyoteTimeBehaviour>();
                            comp.entityStateMachine = entityStateMachine;
                            comp.characterMotor = self;
                            comp.jumpCountOnStart = initJumpCount;
                        }
                    }
                }
        }

        private class CoyoteTimeBehaviour : MonoBehaviour
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

                if (characterMotor.jumpCount > jumpCountOnStart || characterMotor.isGrounded || age >= Duration)// || !entityStateMachine.IsInMainState())
                {
                    ConsumeJumpIfUnspent();
                    Destroy(this);
                }
            }

            public void OnDestroy()
            {
                characterMotor.useGravity = characterMotor.gravityParameters.CheckShouldUseGravity();
            }

            public void ConsumeJumpIfUnspent()
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