using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HaishanTweaks
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jerry.haishantweaks";
        public const string PluginName = "HaishanTweaks";
        public const string PluginVersion = "0.11.1";

        internal static ConfigEntry<bool> InfiniteHealth;
        internal static ConfigEntry<bool> InfiniteMP;
        internal static ConfigEntry<bool> NoSkillCooldowns;
        internal static ConfigEntry<float> DamageMultiplier;
        internal static ConfigEntry<float> MovementSpeedMultiplier;
        internal static ConfigEntry<float> AbilitySizeMultiplier;
        internal static ConfigEntry<float> AbilityRangeMultiplier;
        internal static ConfigEntry<bool> ScaleAbilityVisuals;
        internal static ConfigEntry<bool> ScaleAbilityGameplayAreas;
        internal static ConfigEntry<bool> AbilityScalingDiagnostics;
        internal static ConfigEntry<float> AbilityVerticalCompensation;
        internal static ConfigEntry<int> ProjectileCountMultiplier;
        internal static ConfigEntry<float> MultishotDelaySeconds;
        internal static ConfigEntry<int> EnemyDensityMultiplier;
        internal static ConfigEntry<bool> EnemyDiagnostics;
        internal static ConfigEntry<float> RegularEnemyHealthMultiplier;
        internal static ConfigEntry<float> RegularEnemyDamageMultiplier;
        internal static ConfigEntry<float> RegularEnemyMovementSpeedMultiplier;
        internal static ConfigEntry<float> EliteHealthMultiplier;
        internal static ConfigEntry<float> EliteDamageMultiplier;
        internal static ConfigEntry<float> BossHealthMultiplier;
        internal static ConfigEntry<float> BossDamageMultiplier;
        internal static ConfigEntry<float> PlayerCharacterSizeMultiplier;
        internal static ConfigEntry<float> RegularEnemySizeMultiplier;
        internal static ConfigEntry<float> EliteSizeMultiplier;
        internal static ConfigEntry<float> BossSizeMultiplier;
        internal static ConfigEntry<bool> EnemyDifficultyDiagnostics;
        internal static ConfigEntry<bool> CharacterSizeDiagnostics;
        internal static ConfigEntry<bool> CameraGeometryDiagnostics;
        internal static ConfigEntry<float> CameraDistanceMultiplier;
        internal static ConfigEntry<bool> ReduceFogWhenZoomedOut;
        internal static ConfigEntry<bool> ReduceBlurWhenZoomedOut;
        internal static ConfigEntry<bool> HideZoomOccluders;
        internal static ConfigEntry<bool> AllowHighRankStartingSkills;
        internal static ConfigEntry<bool> AllowHighRankStartingEntries;
        internal static float RuntimeDamageMultiplier;
        internal static float RuntimeMovementMultiplier;
        internal static float RuntimeAbilitySizeMultiplier;
        internal static float RuntimeAbilityRangeMultiplier;
        internal static float RuntimeCameraDistanceMultiplier;

        private ConfigEntry<KeyboardShortcut> toggleKey;
        private Harmony harmony;
        private Rect windowRect = new Rect(30f, 30f, 660f, 720f);
        private Vector2 runStartScroll;
        private Vector2 currencyScroll;
        private Vector2 artifactScrollPosition;
        private Vector2 combatScrollPosition;
        private bool menuVisible;
        private bool sliderDirty;
        private bool confirmSkills;
        private bool confirmEntries;
        private bool confirmRemoveSkills;
        private bool confirmRemoveEntries;
        private bool confirmAchievements;
        private bool confirmCultivation;
        private string progressionMessage = string.Empty;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLockState;
        private string damageSearch = string.Empty;
        private string entrySearch = string.Empty;
        private int tab;
        private List<SkillData> skillCatalog = new List<SkillData>();
        private List<EntryData> entryCatalog = new List<EntryData>();
        private bool catalogReady;
        internal static ManualLogSource ModLogger;

        private void Awake()
        {
            ModLogger = Logger;
            InfiniteHealth = Config.Bind("Player", "InfiniteHealth", false, "Prevent HP loss for the controlled player NPC.");
            InfiniteMP = Config.Bind("Player", "InfiniteMP", false, "Prevent MP loss for the controlled player NPC.");
            NoSkillCooldowns = Config.Bind("Combat", "NoSkillCooldowns", false, "Remove skill cooldowns for the controlled player NPC.");
            DamageMultiplier = Config.Bind("Combat", "DamageMultiplier", 1f, "Outgoing damage multiplier for the controlled player NPC.");
            MovementSpeedMultiplier = Config.Bind("Movement", "MovementSpeedMultiplier", 1f, "Movement speed multiplier for the controlled player NPC.");
            AbilitySizeMultiplier = Config.Bind("Combat", "AbilitySizeMultiplier", 1f, "Player attack area size multiplier.");
            AbilityRangeMultiplier = Config.Bind("Combat", "AbilityRangeMultiplier", 1f, "Player attack object range multiplier.");
            ScaleAbilityVisuals = Config.Bind("Combat", "ScaleAbilityVisuals", true, "Scale visual effects emitted by the controlled player's active skills.");
            ScaleAbilityGameplayAreas = Config.Bind("Combat", "ScaleAbilityGameplayAreas", true, "Scale the controlled player's attack areas.");
            AbilityScalingDiagnostics = Config.Bind("Combat", "AbilityScalingDiagnostics", false, "Log one anchor and dimension record per player ability activation.");
            AbilityVerticalCompensation = Config.Bind("Combat", "AbilityVerticalCompensation", 0.25f, "Fraction of downward visual growth lifted to reduce floor clipping (0-0.5).");
            AbilityVerticalCompensation.Value = Mathf.Clamp(AbilityVerticalCompensation.Value, 0f, 0.5f);
            ProjectileCountMultiplier = Config.Bind("Combat", "ProjectileCountMultiplier", 1, "Additional player projectile count multiplier (1-10). Applies per native projectile emission.");
            ProjectileCountMultiplier.Value = Mathf.Clamp(ProjectileCountMultiplier.Value, 1, 10);
            MultishotDelaySeconds = Config.Bind("Combat", "MultishotDelaySeconds", 0.025f, "Delay between additional player projectiles in seconds (0-0.1).");
            MultishotDelaySeconds.Value = Mathf.Clamp(MultishotDelaySeconds.Value, 0f, 0.1f);
            EnemyDensityMultiplier = Config.Bind("Enemies", "EnemyDensityMultiplier", 1, "Ordinary combat enemy density multiplier (1-15). Bosses, elites, and scripted enemies remain native.");
            EnemyDensityMultiplier.Value = Mathf.Clamp(EnemyDensityMultiplier.Value, 1, 15);
            EnemyDiagnostics = Config.Bind("Enemies", "EnemyDiagnostics", false, "Log ordinary enemy density decisions.");
            RegularEnemyHealthMultiplier = Config.Bind("Enemies", "RegularEnemyHealthMultiplier", 1f, "Regular enemy health multiplier (0.25-20.00)." );
            RegularEnemyDamageMultiplier = Config.Bind("Enemies", "RegularEnemyDamageMultiplier", 1f, "Regular enemy outgoing damage multiplier (0.25-10.00)." );
            RegularEnemyMovementSpeedMultiplier = Config.Bind("Enemies", "RegularEnemyMovementSpeedMultiplier", 1f, "Regular enemy locomotion multiplier (0.50-3.00)." );
            EliteHealthMultiplier = Config.Bind("Enemies", "EliteHealthMultiplier", 1f, "Elite health multiplier (0.25-20.00)." );
            EliteDamageMultiplier = Config.Bind("Enemies", "EliteDamageMultiplier", 1f, "Elite outgoing damage multiplier (0.25-10.00)." );
            BossHealthMultiplier = Config.Bind("Enemies", "BossHealthMultiplier", 1f, "Boss health multiplier (0.25-20.00)." );
            BossDamageMultiplier = Config.Bind("Enemies", "BossDamageMultiplier", 1f, "Boss outgoing damage multiplier (0.25-10.00)." );
            PlayerCharacterSizeMultiplier = Config.Bind("CharacterSize", "PlayerCharacterSizeMultiplier", 1f, "Player visual model size multiplier (0.50-3.00)." );
            RegularEnemySizeMultiplier = Config.Bind("CharacterSize", "RegularEnemySizeMultiplier", 1f, "Regular enemy visual model size multiplier (0.50-3.00)." );
            EliteSizeMultiplier = Config.Bind("CharacterSize", "EliteSizeMultiplier", 1f, "Elite visual model size multiplier (0.50-3.00)." );
            BossSizeMultiplier = Config.Bind("CharacterSize", "BossSizeMultiplier", 1f, "Boss visual model size multiplier (0.50-3.00)." );
            EnemyDifficultyDiagnostics = Config.Bind("Enemies", "EnemyDifficultyDiagnostics", false, "Log one enemy difficulty record per initialization and damage event." );
            CharacterSizeDiagnostics = Config.Bind("CharacterSize", "CharacterSizeDiagnostics", false, "Log character visual-root scaling records." );
            ClampEnemyConfigs();
            CameraGeometryDiagnostics = Config.Bind("Camera", "CameraGeometryDiagnostics", false, "Log scene renderers encountered by extended zoom obstruction checks.");
            CameraDistanceMultiplier = Config.Bind("Camera", "CameraDistanceMultiplier", 1f, "Normal player-follow camera distance multiplier.");
            ReduceFogWhenZoomedOut = Config.Bind("Camera", "ReduceFogWhenZoomedOut", true, "Reduce Unity RenderSettings fog when the camera is zoomed out.");
            ReduceBlurWhenZoomedOut = Config.Bind("Camera", "ReduceBlurWhenZoomedOut", true, "Reduce the camera DepthOfField effect when zoomed out.");
            HideZoomOccluders = Config.Bind("Camera", "HideZoomOccluders", true, "Hide environment meshes blocking the camera when zoomed beyond 1.25x.");
            AllowHighRankStartingSkills = Config.Bind("RunStart", "AllowHighRankStartingSkills", false, "Allow unlocked, discovered skills of any rank as starting skills.");
            AllowHighRankStartingEntries = Config.Bind("RunStart", "AllowHighRankStartingEntries", false, "Allow unlocked, discovered entries of any rank as starting entries.");
            toggleKey = Config.Bind("GUI", "ToggleKey", new KeyboardShortcut(KeyCode.F10), "Show or hide the HaishanTweaks window.");

            DamageMultiplier.Value = ClampDamage(DamageMultiplier.Value);
            MovementSpeedMultiplier.Value = ClampMovement(MovementSpeedMultiplier.Value);
            AbilitySizeMultiplier.Value = ClampAbility(AbilitySizeMultiplier.Value);
            AbilityRangeMultiplier.Value = ClampAbility(AbilityRangeMultiplier.Value);
            CameraDistanceMultiplier.Value = ClampCamera(CameraDistanceMultiplier.Value);
            RuntimeDamageMultiplier = DamageMultiplier.Value;
            RuntimeMovementMultiplier = MovementSpeedMultiplier.Value;
            RuntimeAbilitySizeMultiplier = AbilitySizeMultiplier.Value;
            RuntimeAbilityRangeMultiplier = AbilityRangeMultiplier.Value;
            RuntimeCameraDistanceMultiplier = CameraDistanceMultiplier.Value;

            Logger.LogInfo("HaishanTweaks 0.11.1 loaded");
            Logger.LogInfo("Infinite Health: " + OnOff(InfiniteHealth.Value));
            Logger.LogInfo("Infinite MP: " + OnOff(InfiniteMP.Value));
            Logger.LogInfo("No Skill Cooldowns: " + OnOff(NoSkillCooldowns.Value));
            Logger.LogInfo("Damage Multiplier: " + DamageMultiplier.Value.ToString("F2") + "x");
            Logger.LogInfo("Movement Speed Multiplier: " + MovementSpeedMultiplier.Value.ToString("F2") + "x");
            Logger.LogInfo("Ability Size Multiplier: " + AbilitySizeMultiplier.Value.ToString("F2") + "x");
            Logger.LogInfo("Ability Range Multiplier: " + AbilityRangeMultiplier.Value.ToString("F2") + "x");
            Logger.LogInfo("Camera Distance Multiplier: " + CameraDistanceMultiplier.Value.ToString("F2") + "x");

            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Harmony patches applied");
        }

        private void Update()
        {
            PlayerMultishot.Update();
            EnemyDifficultySystem.Update();
            CharacterSizeSystem.Update();
            if (toggleKey.Value.IsDown())
            {
                SetMenuVisible(!menuVisible);
            }
        }

        private void OnGUI()
        {
            if (menuVisible)
            {
                windowRect = GUI.Window(84321, windowRect, DrawWindow, "HaishanTweaks v0.11.1");
                if (sliderDirty && Event.current.type == EventType.MouseUp)
                {
                    CommitSliders();
                }
            }
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            DrawTab("Player", 0);
            DrawTab("Combat", 1);
            DrawTab("Run Start", 2);
            DrawTab("Currencies", 3);
            DrawTab("Progression", 4);
            DrawTab("Artifacts", 5);
            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            if (tab == 0) DrawPlayerTab();
            else if (tab == 1) DrawCombatTab();
            else if (tab == 2) DrawRunStartTab();
            else if (tab == 3) DrawCurrenciesTab();
            else if (tab == 4) DrawProgressionTab();
            else if (tab == 5) DrawArtifactsTab();
            else DrawArtifactsTab();

            GUILayout.Space(8f);
            if (GUILayout.Button("Close")) SetMenuVisible(false);
            GUILayout.Label("F10 - Show/Hide");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawTab(string label, int value)
        {
            if (GUILayout.Toggle(tab == value, label, GUI.skin.button))
            {
                if (tab != value)
                {
                    tab = value;
                    if (tab == 2) RefreshCatalogs();
                }
            }
        }

        private void DrawPlayerTab()
        {
            GUILayout.Label("PLAYER");
            DrawToggle("Infinite Health", InfiniteHealth);
            DrawToggle("Infinite MP", InfiniteMP);
            GUILayout.Space(8f);
            GUILayout.Label("Movement Speed Multiplier");
            DrawSlider(true);
            GUILayout.Space(8f);
            GUILayout.Label("CAMERA");
            GUILayout.Label("Camera Distance");
            DrawCameraSlider();
            DrawToggle("Reduce Fog When Zoomed Out", ReduceFogWhenZoomedOut);
            DrawToggle("Reduce Blur When Zoomed Out", ReduceBlurWhenZoomedOut);
            DrawToggle("Hide Zoom Occluders", HideZoomOccluders);
        }

        private void DrawCombatTab()
        {
            combatScrollPosition = GUILayout.BeginScrollView(combatScrollPosition, GUILayout.Height(570f));
            GUILayout.Label("COMBAT");
            DrawToggle("No Skill Cooldowns", NoSkillCooldowns);
            GUILayout.Space(8f);
            GUILayout.Label("Damage Multiplier");
            DrawSlider(false);
            GUILayout.Label("Ability Size Multiplier");
            DrawAbilitySlider(true);
            GUILayout.Label("Ability Range Multiplier");
            DrawAbilitySlider(false);
            GUILayout.Label("Projectile Count");
            DrawProjectileCount();
            DrawMultishotDelay();
            if (GUILayout.Button("Reset Ability Scaling"))
            {
                RuntimeAbilitySizeMultiplier = 1f;
                RuntimeAbilityRangeMultiplier = 1f;
                ProjectileCountMultiplier.Value = 1;
                MultishotDelaySeconds.Value = 0.025f;
                EnemyDensityMultiplier.Value = 1;
                sliderDirty = true;
                Config.Save();
            }
            DrawToggle("Scale Ability Visuals", ScaleAbilityVisuals);
            DrawToggle("Scale Ability Gameplay Areas", ScaleAbilityGameplayAreas);
            GUILayout.Label("Standard SkillBox / projectile effects are supported. Some custom passive/artifact effects may use independent range logic.");
            if (RuntimeAbilitySizeMultiplier > 5f || RuntimeAbilityRangeMultiplier > 5f)
                GUILayout.Label("Extreme scaling may affect performance and cover very large areas.");
            GUILayout.Space(8f);
            GUILayout.Label("ENEMIES");
            DrawEnemyDensity();
            DrawEnemyDifficultySlider("Regular Enemy Health", RegularEnemyHealthMultiplier, 0.25f, 20f);
            DrawEnemyDifficultySlider("Regular Enemy Damage", RegularEnemyDamageMultiplier, 0.25f, 10f);
            DrawEnemyDifficultySlider("Regular Enemy Movement Speed", RegularEnemyMovementSpeedMultiplier, 0.5f, 3f);
            DrawEnemyDifficultySlider("Elite Health", EliteHealthMultiplier, 0.25f, 20f);
            DrawEnemyDifficultySlider("Elite Damage", EliteDamageMultiplier, 0.25f, 10f);
            DrawEnemyDifficultySlider("Boss Health", BossHealthMultiplier, 0.25f, 20f);
            DrawEnemyDifficultySlider("Boss Damage", BossDamageMultiplier, 0.25f, 10f);
            DrawToggle("Enemy Difficulty Diagnostics", EnemyDifficultyDiagnostics);
            GUILayout.Space(8f);
            GUILayout.Label("CHARACTER SIZE");
            DrawCharacterSizeSlider("Player Character Size", PlayerCharacterSizeMultiplier);
            DrawCharacterSizeSlider("Regular Enemy Size", RegularEnemySizeMultiplier);
            DrawCharacterSizeSlider("Elite Size", EliteSizeMultiplier);
            DrawCharacterSizeSlider("Boss Size", BossSizeMultiplier);
            DrawToggle("Character Size Diagnostics", CharacterSizeDiagnostics);
            if (GUILayout.Button("Reset Enemy Difficulty and Character Size")) ResetEnemyDifficultyAndSize();
            GUILayout.EndScrollView();
        }

        private static void ClampEnemyConfigs()
        {
            RegularEnemyHealthMultiplier.Value = Mathf.Clamp(RegularEnemyHealthMultiplier.Value, 0.25f, 20f);
            RegularEnemyDamageMultiplier.Value = Mathf.Clamp(RegularEnemyDamageMultiplier.Value, 0.25f, 10f);
            RegularEnemyMovementSpeedMultiplier.Value = Mathf.Clamp(RegularEnemyMovementSpeedMultiplier.Value, 0.5f, 3f);
            EliteHealthMultiplier.Value = Mathf.Clamp(EliteHealthMultiplier.Value, 0.25f, 20f);
            EliteDamageMultiplier.Value = Mathf.Clamp(EliteDamageMultiplier.Value, 0.25f, 10f);
            BossHealthMultiplier.Value = Mathf.Clamp(BossHealthMultiplier.Value, 0.25f, 20f);
            BossDamageMultiplier.Value = Mathf.Clamp(BossDamageMultiplier.Value, 0.25f, 10f);
            PlayerCharacterSizeMultiplier.Value = Mathf.Clamp(PlayerCharacterSizeMultiplier.Value, 0.5f, 3f);
            RegularEnemySizeMultiplier.Value = Mathf.Clamp(RegularEnemySizeMultiplier.Value, 0.5f, 3f);
            EliteSizeMultiplier.Value = Mathf.Clamp(EliteSizeMultiplier.Value, 0.5f, 3f);
            BossSizeMultiplier.Value = Mathf.Clamp(BossSizeMultiplier.Value, 0.5f, 3f);
        }

        private void DrawEnemyDifficultySlider(string label, ConfigEntry<float> entry, float min, float max)
        {
            float value = Mathf.Clamp(entry.Value, min, max);
            float next = Mathf.Clamp(GUILayout.HorizontalSlider(value, min, max), min, max);
            next = Mathf.Round(next / 0.05f) * 0.05f;
            if (!Mathf.Approximately(next, value)) { entry.Value = next; sliderDirty = true; }
            GUILayout.Label(label + ": " + value.ToString("F2") + "x");
        }

        private void DrawCharacterSizeSlider(string label, ConfigEntry<float> entry)
        {
            float value = Mathf.Clamp(entry.Value, 0.5f, 3f);
            float next = Mathf.Clamp(GUILayout.HorizontalSlider(value, 0.5f, 3f), 0.5f, 3f);
            next = Mathf.Round(next / 0.05f) * 0.05f;
            if (!Mathf.Approximately(next, value)) { entry.Value = next; sliderDirty = true; }
            GUILayout.Label(label + ": " + value.ToString("F2") + "x");
        }

        private void ResetEnemyDifficultyAndSize()
        {
            RegularEnemyHealthMultiplier.Value = RegularEnemyDamageMultiplier.Value = RegularEnemyMovementSpeedMultiplier.Value = 1f;
            EliteHealthMultiplier.Value = EliteDamageMultiplier.Value = BossHealthMultiplier.Value = BossDamageMultiplier.Value = 1f;
            PlayerCharacterSizeMultiplier.Value = RegularEnemySizeMultiplier.Value = EliteSizeMultiplier.Value = BossSizeMultiplier.Value = 1f;
            Config.Save();
        }

        private void DrawEnemyDensity()
        {
            int value = Mathf.Clamp(EnemyDensityMultiplier.Value, 1, 15);
            int next = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(value, 1f, 15f)), 1, 15);
            if (next != value)
            {
                EnemyDensityMultiplier.Value = next;
                Config.Save();
                Logger.LogInfo("Enemy Density: " + next + "x");
            }
            GUILayout.Label("Enemy Density: " + value + "x");
            if (value > 3) GUILayout.Label("High enemy density may reduce performance.");
            if (value > 8) GUILayout.Label("Very high enemy density can heavily affect AI/pathfinding performance.");
            if (value > 12) GUILayout.Label("Extreme enemy density may cause severe performance loss.");
        }

        private void DrawProjectileCount()
        {
            int value = Mathf.Clamp(ProjectileCountMultiplier.Value, 1, 10);
            int next = Mathf.Clamp(Mathf.RoundToInt(GUILayout.HorizontalSlider(value, 1f, 10f)), 1, 10);
            if (next != value)
            {
                ProjectileCountMultiplier.Value = next;
                Config.Save();
                Logger.LogInfo("Projectile Count: " + next + "x");
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current: " + value + "x");
            if (GUILayout.Button("Reset", GUILayout.Width(70f)))
            {
                ProjectileCountMultiplier.Value = 1;
                Config.Save();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawMultishotDelay()
        {
            float value = Mathf.Clamp(MultishotDelaySeconds.Value, 0f, 0.1f);
            float next = Mathf.Clamp(GUILayout.HorizontalSlider(value, 0f, 0.1f), 0f, 0.1f);
            next = Mathf.Round(next * 200f) / 200f;
            if (!Mathf.Approximately(next, value))
            {
                MultishotDelaySeconds.Value = next;
                Config.Save();
            }
            GUILayout.Label("Multishot Delay: " + Mathf.RoundToInt(value * 1000f) + " ms");
        }

        private void DrawSlider(bool movement)
        {
            float value = movement ? RuntimeMovementMultiplier : RuntimeDamageMultiplier;
            float min = movement ? 0.5f : 0.1f;
            float max = movement ? 5f : 20f;
            float next = Mathf.Clamp(GUILayout.HorizontalSlider(value, min, max), min, max);
            if (!Mathf.Approximately(next, value))
            {
                if (movement) RuntimeMovementMultiplier = next;
                else RuntimeDamageMultiplier = next;
                sliderDirty = true;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current: " + value.ToString("F2") + "x");
            if (GUILayout.Button("Reset", GUILayout.Width(70f)))
            {
                if (movement) SetMovementMultiplier(1f);
                else SetDamageMultiplier(1f);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawAbilitySlider(bool size)
        {
            float value = size ? RuntimeAbilitySizeMultiplier : RuntimeAbilityRangeMultiplier;
            float next = Mathf.Clamp(GUILayout.HorizontalSlider(value, 0.5f, 20f), 0.5f, 20f);
            next = ApplyMagneticSnap(next, 0.35f, 1f, 5f, 10f, 15f, 20f);
            next = Mathf.Round(next / 0.05f) * 0.05f;
            if (!Mathf.Approximately(next, value))
            {
                if (size) RuntimeAbilitySizeMultiplier = next;
                else RuntimeAbilityRangeMultiplier = next;
                sliderDirty = true;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Draft: " + value.ToString("F2") + "x");
            if (GUILayout.Button("Reset", GUILayout.Width(70f)))
            {
                if (size) RuntimeAbilitySizeMultiplier = 1f;
                else RuntimeAbilityRangeMultiplier = 1f;
                sliderDirty = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("1x", GUILayout.Width(25f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("5x", GUILayout.Width(25f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("10x", GUILayout.Width(30f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("15x", GUILayout.Width(30f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("20x", GUILayout.Width(30f));
            GUILayout.EndHorizontal();
        }

        private void DrawCameraSlider()
        {
            float value = RuntimeCameraDistanceMultiplier;
            float next = Mathf.Clamp(GUILayout.HorizontalSlider(value, 0.5f, 2f), 0.5f, 2f);
            next = ApplyMagneticSnap(next, 0.2f, 0.5f, 1f, 1.5f, 2f);
            next = Mathf.Round(next / 0.05f) * 0.05f;
            if (!Mathf.Approximately(next, value))
            {
                RuntimeCameraDistanceMultiplier = next;
                sliderDirty = true;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Draft: " + value.ToString("F2") + "x");
            if (GUILayout.Button("Reset Camera", GUILayout.Width(105f))) SetCameraDistanceMultiplier(1f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("0.5x", GUILayout.Width(35f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("1x", GUILayout.Width(25f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("1.5x", GUILayout.Width(35f));
            GUILayout.FlexibleSpace();
            GUILayout.Label("2x", GUILayout.Width(25f));
            GUILayout.EndHorizontal();
        }

        private void DrawRunStartTab()
        {
            if (!catalogReady) RefreshCatalogs();
            StartLoadout.Normalize();
            GUILayout.Label("RUN START");
            GUILayout.Label("Native game UI only displays the first 2 starting slots. Extra selections are managed by HaishanTweaks.");
            if (GUILayout.Button("Refresh")) RefreshCatalogs();
            runStartScroll = GUILayout.BeginScrollView(runStartScroll);
            GUILayout.Label("Starting Skills - Selected: " + StartLoadout.SelectedSkills().Count);
            DrawToggle("Allow High-Rank Starting Skills", AllowHighRankStartingSkills);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add All Eligible Skills"))
                progressionMessage = "Added " + StartLoadout.AddAllSkills() + " starting skills.";
            if (!confirmRemoveSkills)
            {
                if (GUILayout.Button("Remove All Starting Skills")) BeginConfirmation(1);
            }
            else
            {
                if (GUILayout.Button("Confirm Remove Skills"))
                {
                    progressionMessage = "Removed " + StartLoadout.RemoveAllSkills() + " starting skills.";
                    confirmRemoveSkills = false;
                }
                if (GUILayout.Button("Cancel")) confirmRemoveSkills = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("SELECTED SKILLS");
            List<KeyValuePair<int, string>> selectedSkills = StartLoadout.SelectedSkills();
            for (int i = 0; i < selectedSkills.Count; i++) DrawSelectedSkill(selectedSkills, i);
            GUILayout.Label("AVAILABLE SKILLS");
            GUILayout.Label("Search");
            damageSearch = GUILayout.TextField(damageSearch ?? string.Empty);
            foreach (SkillData skill in skillCatalog.Where(x => MatchesSkill(x, damageSearch)))
            {
                DrawSkillCandidate(skill);
            }
            GUILayout.Space(12f);
            GUILayout.Label("Starting Artifacts - Selected: " + StartLoadout.SelectedEntries().Count);
            DrawToggle("Allow High-Rank Starting Entries", AllowHighRankStartingEntries);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add All Eligible Artifacts"))
                progressionMessage = "Added " + StartLoadout.AddAllEntries() + " starting artifacts.";
            if (!confirmRemoveEntries)
            {
                if (GUILayout.Button("Remove All Starting Artifacts")) BeginConfirmation(2);
            }
            else
            {
                if (GUILayout.Button("Confirm Remove Artifacts"))
                {
                    progressionMessage = "Removed " + StartLoadout.RemoveAllEntries() + " starting artifacts.";
                    confirmRemoveEntries = false;
                }
                if (GUILayout.Button("Cancel")) confirmRemoveEntries = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("SELECTED ARTIFACTS");
            List<KeyValuePair<int, string>> selectedEntries = StartLoadout.SelectedEntries();
            for (int i = 0; i < selectedEntries.Count; i++) DrawSelectedEntry(selectedEntries, i);
            GUILayout.Label("AVAILABLE ARTIFACTS");
            GUILayout.Label("Search");
            entrySearch = GUILayout.TextField(entrySearch ?? string.Empty);
            foreach (EntryData entry in entryCatalog.Where(x => MatchesEntry(x, entrySearch)))
            {
                DrawEntryCandidate(entry);
            }
            if (selectedEntries.Count >= 20 || selectedSkills.Count >= 20)
            {
                GUILayout.Label("Large numbers of artifacts or skills may create unusual interactions or reduce performance.");
            }
            GUILayout.EndScrollView();
        }

        private void DrawArtifactsTab()
        {
            GUILayout.Label("ARTIFACTS / CURRENT RUN");
            artifactScrollPosition = GUILayout.BeginScrollView(artifactScrollPosition, GUILayout.Height(570f));
            Npc player = CtrlManager.Instance == null ? null : CtrlManager.Instance.CtrlNpc;
            if (player == null || player.m_ThingAttribute == null)
            {
                GUILayout.Label("No active run.");
                GUILayout.EndScrollView();
                return;
            }
            if (player.m_ThingAttribute.m_DicTriggerEntry == null)
            {
                GUILayout.Label("No active artifacts.");
                GUILayout.EndScrollView();
                return;
            }
            foreach (KeyValuePair<string, TriggerEntry> item in player.m_ThingAttribute.m_DicTriggerEntry.ToList())
            {
                TriggerEntry trigger = item.Value;
                EntryData data = trigger == null || EntryManager.Instance == null ? null : EntryManager.Instance.GetEntryData(trigger.Name);
                if (trigger == null || data == null)
                {
                    GUILayout.Label("[Missing/Unavailable] " + item.Key);
                    continue;
                }
                GUILayout.BeginHorizontal();
                DrawRankedLabel(LocalizedEntry(data.Name), data.Rank);
                GUILayout.Label("Rank: " + data.Rank + "  Level: " + trigger.Level + "/" + StartLoadout.EffectiveEntryMax(data));
                GUILayout.Label("Status: " + trigger.Status);
                string reason;
                bool canLevel = StartLoadout.CanLevelEntry(data, trigger, out reason);
                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && canLevel;
                if (GUILayout.Button("Level Up Once", GUILayout.Width(105f))) StartLoadout.TryLevelUpEntryOnce(data.Name, out reason);
                GUI.enabled = oldEnabled;
                GUILayout.EndHorizontal();
                if (!canLevel && reason == "Native upgrade path unresolved") GUILayout.Label("  Native upgrade path unresolved");
            }
            GUILayout.EndScrollView();
        }

        private void DrawSelectedSkill(List<KeyValuePair<int, string>> selected, int index)
        {
            string name = selected[index].Value;
            SkillData data = SkillManager.Instance == null ? null : SkillManager.Instance.GetSkillData(name);
            GUILayout.BeginHorizontal();
            DrawRankedLabel(data == null ? "[Missing/Unavailable] " + name : LocalizedSkill(name), data == null ? 0 : data.Rate);
            GUILayout.Label("大道: " + StartLoadout.SectText(data));
            GUILayout.Label("Rank: " + (data == null ? "?" : data.Rate.ToString()));
            GUI.enabled = index > 0;
            if (GUILayout.Button("^", GUILayout.Width(28f))) StartLoadout.MoveSkill(index, -1);
            GUI.enabled = index + 1 < selected.Count;
            if (GUILayout.Button("v", GUILayout.Width(28f))) StartLoadout.MoveSkill(index, 1);
            GUI.enabled = true;
            if (GUILayout.Button("Remove", GUILayout.Width(70f))) StartLoadout.RemoveSkill(selected[index].Key);
            GUILayout.EndHorizontal();
        }

        private void DrawSelectedEntry(List<KeyValuePair<int, string>> selected, int index)
        {
            string name = selected[index].Value;
            EntryData data = EntryManager.Instance == null ? null : EntryManager.Instance.GetEntryData(name);
            GUILayout.BeginHorizontal();
            DrawRankedLabel(data == null ? "[Missing/Unavailable] " + name : LocalizedEntry(name), data == null ? 0 : data.Rank);
            GUILayout.Label("Rank: " + (data == null ? "?" : data.Rank.ToString()));
            if (GUILayout.Button("Remove", GUILayout.Width(70f))) StartLoadout.RemoveEntry(selected[index].Key);
            GUILayout.EndHorizontal();
        }

        private void DrawSkillCandidate(SkillData skill)
        {
            bool selected = StartLoadout.ContainsSkill(skill.Name);
            GUILayout.BeginHorizontal();
            DrawRankedLabel(LocalizedSkill(skill.Name), skill.Rate);
            GUILayout.Label("大道: " + StartLoadout.SectText(skill));
            GUILayout.Label("Rank: " + skill.Rate);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && !selected && StartLoadout.CanAddSkill(skill.Name);
            if (GUILayout.Button(selected ? "Selected" : "Add", GUILayout.Width(70f))) StartLoadout.TryAddSkill(skill.Name);
            GUI.enabled = oldEnabled;
            GUILayout.EndHorizontal();
        }

        private void DrawEntryCandidate(EntryData entry)
        {
            bool selected = StartLoadout.ContainsEntry(entry.Name);
            GUILayout.BeginHorizontal();
            DrawRankedLabel(LocalizedEntry(entry.Name), entry.Rank);
            GUILayout.Label("Rank: " + entry.Rank);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && !selected && StartLoadout.CanAddEntry(entry.Name);
            if (GUILayout.Button(selected ? "Selected" : "Add", GUILayout.Width(70f))) StartLoadout.TryAddEntry(entry.Name);
            GUI.enabled = oldEnabled;
            GUILayout.EndHorizontal();
        }

        private void DrawRankedLabel(string text, int rank)
        {
            Color old = GUI.color;
            GUI.color = StartLoadout.RankColor(rank);
            GUILayout.Label(text);
            GUI.color = old;
        }

        private void DrawProgressionTab()
        {
            GUILayout.Label("PROGRESSION");
            GUILayout.Label("Unlock actions permanently change progression on the current save. Back up your save first.");
            string reason;
            bool skillsReady = StartLoadout.SkillProgressionReady(out reason);
            if (!confirmSkills)
            {
                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && skillsReady;
                if (GUILayout.Button("Unlock All Skills")) BeginConfirmation(3);
                GUI.enabled = oldEnabled;
                if (!skillsReady) GUILayout.Label("[disabled] " + reason);
            }
            else
            {
                GUILayout.Label("Unlock all normal installed skills? This permanently changes this save.");
                if (GUILayout.Button("Confirm Unlock All Skills"))
                {
                    progressionMessage = StartLoadout.UnlockAllSkills();
                    confirmSkills = false;
                    RefreshCatalogs();
                }
                if (GUILayout.Button("Cancel")) confirmSkills = false;
            }
            bool entriesReady = StartLoadout.EntryProgressionReady(out reason);
            if (!confirmEntries)
            {
                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && entriesReady;
                if (GUILayout.Button("Unlock All Artifacts")) BeginConfirmation(4);
                GUI.enabled = oldEnabled;
                if (!entriesReady) GUILayout.Label("[disabled] " + reason);
            }
            else
            {
                GUILayout.Label("Unlock all normal installed artifacts? This permanently changes this save.");
                if (GUILayout.Button("Confirm Unlock All Artifacts"))
                {
                    progressionMessage = StartLoadout.UnlockAllEntries();
                    confirmEntries = false;
                    RefreshCatalogs();
                }
                if (GUILayout.Button("Cancel")) confirmEntries = false;
            }
            bool schoolsReady = StartLoadout.CultivationProgressionReady(out reason);
            if (!confirmCultivation)
            {
                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && schoolsReady;
                if (GUILayout.Button("Unlock All Cultivation Schools")) BeginConfirmation(5);
                GUI.enabled = oldEnabled;
                if (!schoolsReady) GUILayout.Label("[disabled] " + reason);
            }
            else
            {
                GUILayout.Label("Unlock all cultivation schools? This may also unlock linked realms.");
                if (GUILayout.Button("Confirm Unlock All Cultivation Schools"))
                {
                    progressionMessage = StartLoadout.UnlockAllCultivationSchools();
                    confirmCultivation = false;
                }
                if (GUILayout.Button("Cancel")) confirmCultivation = false;
            }
            if (!confirmAchievements)
            {
                if (GUILayout.Button("Complete All In-Game Achievements")) BeginConfirmation(6);
            }
            else
            {
                GUILayout.Label("Complete all game achievements?");
                GUILayout.Label("This permanently changes global progression. The native path may also unlock Steam achievements when Steam is active. This may not be reversible.");
                if (GUILayout.Button("Confirm Complete All"))
                {
                    string achievementResult;
                    StartLoadout.TryCompleteAllAchievements(out achievementResult);
                    progressionMessage = achievementResult;
                    confirmAchievements = false;
                }
                if (GUILayout.Button("Cancel")) confirmAchievements = false;
            }
            if (!string.IsNullOrEmpty(progressionMessage)) GUILayout.Label(progressionMessage);
        }

        private void DrawCurrenciesTab()
        {
            GUILayout.Label("CURRENCIES");
            currencyScroll = GUILayout.BeginScrollView(currencyScroll);
            DrawCurrency("Currency_CanPo", 100f, 1000f, 10000f);
            DrawCurrency("Currency_CanPoJingCui", 10f, 100f, 1000f);
            DrawCurrency("Currency_YuanChuZhiQi", 10f, 100f, 1000f);
            GUILayout.Label("Currency changes persist on the game's next normal save.");
            GUILayout.EndScrollView();
        }

        private void BeginConfirmation(int confirmation)
        {
            confirmRemoveSkills = confirmation == 1;
            confirmRemoveEntries = confirmation == 2;
            confirmSkills = confirmation == 3;
            confirmEntries = confirmation == 4;
            confirmCultivation = confirmation == 5;
            confirmAchievements = confirmation == 6;
        }

        private void DrawCurrency(string key, float a, float b, float c)
        {
            GUILayout.Label(CurrencyTools.DisplayName(key) + "  Current: " + CurrencyTools.Get(key).ToString("F0"));
            bool available = CurrencyTools.IsAvailable();
            GUI.enabled = available;
            GUILayout.BeginHorizontal();
            DrawCurrencyButton(key, a);
            DrawCurrencyButton(key, b);
            DrawCurrencyButton(key, c);
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            if (!available) GUILayout.Label("Player data unavailable");
        }

        private void DrawCurrencyButton(string key, float amount)
        {
            if (GUILayout.Button("+" + amount.ToString("F0"))) CurrencyTools.Add(key, amount);
        }

        private void DrawToggle(string label, ConfigEntry<bool> setting)
        {
            bool value = GUILayout.Toggle(setting.Value, label);
            if (value != setting.Value)
            {
                setting.Value = value;
                Logger.LogInfo(label + ": " + OnOff(value));
            }
        }

        private void CommitSliders()
        {
            sliderDirty = false;
            SetDamageMultiplier(RuntimeDamageMultiplier, false);
            SetMovementMultiplier(RuntimeMovementMultiplier, false);
            SetAbilitySizeMultiplier(RuntimeAbilitySizeMultiplier, false);
            SetAbilityRangeMultiplier(RuntimeAbilityRangeMultiplier, false);
            SetCameraDistanceMultiplier(RuntimeCameraDistanceMultiplier, false);
            Config.Save();
        }

        private void SetDamageMultiplier(float value, bool save = true)
        {
            float clamped = ClampDamage(value);
            bool changed = !Mathf.Approximately(DamageMultiplier.Value, clamped);
            RuntimeDamageMultiplier = clamped;
            DamageMultiplier.Value = clamped;
            if (changed) Logger.LogInfo("Damage Multiplier: " + clamped.ToString("F2") + "x");
            if (save) Config.Save();
        }

        private void SetMovementMultiplier(float value, bool save = true)
        {
            float clamped = ClampMovement(value);
            bool changed = !Mathf.Approximately(MovementSpeedMultiplier.Value, clamped);
            RuntimeMovementMultiplier = clamped;
            MovementSpeedMultiplier.Value = clamped;
            if (changed) Logger.LogInfo("Movement Speed Multiplier: " + clamped.ToString("F2") + "x");
            if (save) Config.Save();
        }

        private void SetAbilitySizeMultiplier(float value, bool save = true)
        {
            float clamped = ClampAbility(value);
            bool changed = !Mathf.Approximately(AbilitySizeMultiplier.Value, clamped);
            RuntimeAbilitySizeMultiplier = clamped;
            AbilitySizeMultiplier.Value = clamped;
            if (changed) Logger.LogInfo("Ability Size Multiplier: " + clamped.ToString("F2") + "x");
            if (save) Config.Save();
        }

        private void SetAbilityRangeMultiplier(float value, bool save = true)
        {
            float clamped = ClampAbility(value);
            bool changed = !Mathf.Approximately(AbilityRangeMultiplier.Value, clamped);
            RuntimeAbilityRangeMultiplier = clamped;
            AbilityRangeMultiplier.Value = clamped;
            if (changed) Logger.LogInfo("Ability Range Multiplier: " + clamped.ToString("F2") + "x");
            if (save) Config.Save();
        }

        private void SetCameraDistanceMultiplier(float value, bool save = true)
        {
            float clamped = ClampCamera(value);
            bool changed = !Mathf.Approximately(CameraDistanceMultiplier.Value, clamped);
            RuntimeCameraDistanceMultiplier = clamped;
            CameraDistanceMultiplier.Value = clamped;
            if (changed) Logger.LogInfo("Camera Distance Multiplier: " + clamped.ToString("F2") + "x");
            if (save) Config.Save();
        }

        private void RefreshCatalogs()
        {
            if (SkillManager.Instance == null || EntryManager.Instance == null || UnlockManager.Instance == null)
            {
                catalogReady = false;
                return;
            }
            skillCatalog = StartLoadout.GetSkills().ToList();
            entryCatalog = StartLoadout.GetEntries().ToList();
            catalogReady = true;
        }

        private bool MatchesSkill(SkillData data, string query)
        {
            return string.IsNullOrEmpty(query) || LocalizedSkill(data.Name).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || data.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool MatchesEntry(EntryData data, string query)
        {
            return string.IsNullOrEmpty(query) || LocalizedEntry(data.Name).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || data.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static string LocalizedSkill(string name)
        {
            SkillData data = SkillManager.Instance == null ? null : SkillManager.Instance.GetSkillData(name);
            return Localized(data == null ? null : data.DisplayName, name);
        }

        internal static string LocalizedEntry(string name)
        {
            EntryData data = EntryManager.Instance == null ? null : EntryManager.Instance.GetEntryData(name);
            return Localized(data == null ? null : data.DisplayName, name);
        }

        private static string Localized(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key) || TFManager.Instance == null) return fallback;
            string value = TFManager.Instance.Get(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private void SetMenuVisible(bool visible)
        {
            if (menuVisible == visible) return;
            if (visible)
            {
                previousCursorVisible = Cursor.visible;
                previousCursorLockState = Cursor.lockState;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                RefreshCatalogs();
            }
            else
            {
                Cursor.visible = previousCursorVisible;
                Cursor.lockState = previousCursorLockState;
            }
            menuVisible = visible;
        }

        private void OnDestroy()
        {
            if (menuVisible) SetMenuVisible(false);
            FogController.RestoreNative();
            BlurController.RestoreNative();
            ZoomGeometryCleanup.RestoreNative();
            if (harmony != null) harmony.UnpatchSelf();
        }

        internal static bool IsPlayer(Thing owner)
        {
            Npc npc = owner as Npc;
            return npc != null && npc.IsPlayerNpc;
        }

        private static float ClampDamage(float value)
        {
            return IsFinite(value) ? Mathf.Clamp(value, 0.1f, 20f) : 1f;
        }

        private static float ClampMovement(float value)
        {
            return IsFinite(value) ? Mathf.Clamp(value, 0.5f, 5f) : 1f;
        }

        private static float ClampAbility(float value)
        {
            return IsFinite(value) ? Mathf.Clamp(value, 0.5f, 20f) : 1f;
        }

        private static float ClampCamera(float value)
        {
            return IsFinite(value) ? Mathf.Clamp(value, 0.5f, 2f) : 1f;
        }

        private static float ApplyMagneticSnap(float value, float threshold, params float[] points)
        {
            if (points != null)
            {
                foreach (float point in points)
                {
                    if (Mathf.Abs(value - point) <= threshold) return point;
                }
            }
            return value;
        }

        private static bool IsFinite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static string OnOff(bool value) { return value ? "ON" : "OFF"; }
        internal static void LogInfo(string message) { if (ModLogger != null) ModLogger.LogInfo(message); }
        internal static void LogError(string message) { if (ModLogger != null) ModLogger.LogError(message); }

    }

    internal static class StartLoadout
    {
        internal static void Normalize()
        {
            UnlockManager manager = UnlockManager.Instance;
            if (manager == null) return;
            EnsureNativeSlots(manager.m_StratSkill);
            EnsureNativeSlots(manager.m_StratEntry);
        }

        private static void EnsureNativeSlots(Dictionary<int, string> values)
        {
            if (values == null) return;
            if (!values.ContainsKey(0)) values.Add(0, null);
            if (!values.ContainsKey(1)) values.Add(1, null);
        }

        private static string Get(Dictionary<int, string> values, int slot) { string value; return values != null && values.TryGetValue(slot, out value) ? value : null; }

        internal static bool ContainsSkill(string name) { return Contains(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratSkill, name); }
        internal static bool ContainsEntry(string name) { return Contains(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratEntry, name); }
        private static bool Contains(Dictionary<int, string> values, string name) { return values != null && values.Any(x => x.Value == name); }
        internal static List<KeyValuePair<int, string>> SelectedSkills() { return Selected(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratSkill); }
        internal static List<KeyValuePair<int, string>> SelectedEntries() { return Selected(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratEntry); }
        private static List<KeyValuePair<int, string>> Selected(Dictionary<int, string> values) { return values == null ? new List<KeyValuePair<int, string>>() : values.Where(x => x.Value != null).OrderBy(x => x.Key).ToList(); }

        internal static bool TryAddSkill(string name)
        {
            if (!CanUseSkill(name)) return false;
            return Add(UnlockManager.Instance.m_StratSkill, name, "Starting Skill");
        }

        internal static bool TryAddEntry(string name)
        {
            if (!CanUseEntry(name)) return false;
            return Add(UnlockManager.Instance.m_StratEntry, name, "Starting Entry");
        }

        private static bool Add(Dictionary<int, string> values, string name, string label)
        {
            Normalize();
            if (values == null || string.IsNullOrEmpty(name) || values.Any(x => x.Value == name)) return false;
            int slot = -1;
            foreach (KeyValuePair<int, string> item in values.OrderBy(x => x.Key))
            {
                if (item.Key >= 0 && item.Value == null)
                {
                    slot = item.Key;
                    break;
                }
            }
            if (slot < 0)
            {
                int max = values.Where(x => x.Key >= 0).Select(x => x.Key).DefaultIfEmpty(-1).Max();
                slot = max + 1;
            }
            values[slot] = name;
            if (!string.IsNullOrEmpty(label)) Plugin.LogInfo(label + " added: " + name);
            return true;
        }

        internal static void RemoveSkill(int slot) { Remove(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratSkill, slot, "Starting Skill"); }
        internal static void RemoveEntry(int slot) { Remove(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratEntry, slot, "Starting Artifact"); }
        private static void Remove(Dictionary<int, string> values, int slot, string label)
        {
            if (values != null && slot >= 0 && values.ContainsKey(slot) && values[slot] != null)
            {
                values[slot] = null;
                Plugin.LogInfo(label + " removed: " + slot);
            }
        }

        internal static void MoveSkill(int selectedIndex, int direction) { Move(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratSkill, selectedIndex, direction, "Starting Skill"); }
        private static void Move(Dictionary<int, string> values, int selectedIndex, int direction, string label)
        {
            List<KeyValuePair<int, string>> selected = Selected(values);
            int otherIndex = selectedIndex + direction;
            if (selectedIndex < 0 || selectedIndex >= selected.Count || otherIndex < 0 || otherIndex >= selected.Count) return;
            int first = selected[selectedIndex].Key;
            int second = selected[otherIndex].Key;
            string value = values[first];
            values[first] = values[second];
            values[second] = value;
            Plugin.LogInfo(label + " moved: " + value + " -> position " + (otherIndex + 1));
        }

        internal static int AddAllSkills()
        {
            Normalize();
            if (UnlockManager.Instance == null) return 0;
            List<SkillData> candidates = GetSkills().Where(x => CanUseSkill(x.Name)).OrderBy(x => x.Rate).ThenBy(x => x.Name, StringComparer.Ordinal).ToList();
            int added = 0;
            foreach (SkillData data in candidates) if (!ContainsSkill(data.Name) && Add(UnlockManager.Instance.m_StratSkill, data.Name, null)) added++;
            Plugin.LogInfo("Added " + added + " starting skills");
            return added;
        }

        internal static int AddAllEntries()
        {
            Normalize();
            if (UnlockManager.Instance == null) return 0;
            List<EntryData> candidates = GetEntries().Where(x => CanUseEntry(x.Name)).OrderBy(x => x.Rank).ThenBy(x => x.Name, StringComparer.Ordinal).ToList();
            int added = 0;
            foreach (EntryData data in candidates) if (!ContainsEntry(data.Name) && Add(UnlockManager.Instance.m_StratEntry, data.Name, null)) added++;
            Plugin.LogInfo("Added " + added + " starting artifacts");
            return added;
        }

        internal static int RemoveAllSkills() { return RemoveAll(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratSkill, "starting skills"); }
        internal static int RemoveAllEntries() { return RemoveAll(UnlockManager.Instance == null ? null : UnlockManager.Instance.m_StratEntry, "starting artifacts"); }
        private static int RemoveAll(Dictionary<int, string> values, string label)
        {
            if (values == null) return 0;
            List<int> keys = values.Where(x => x.Value != null).Select(x => x.Key).ToList();
            foreach (int key in keys) values[key] = null;
            Plugin.LogInfo("Removed " + keys.Count + " " + label);
            return keys.Count;
        }

        internal static int EffectiveEntryMax(EntryData data) { return data == null || data.Levels == null ? 0 : data.Levels.Count + 1; }

        internal static bool CanLevelEntry(EntryData data, TriggerEntry trigger, out string reason)
        {
            reason = null;
            if (data == null || trigger == null || trigger.Status != 1) { reason = "Entry unavailable"; return false; }
            if (data.Choose != 0) { reason = "Native upgrade path unresolved"; return false; }
            if (data.Levels == null || trigger.Level < 1 || trigger.Level > data.Levels.Count) { reason = "Already at maximum level"; return false; }
            LevelInfo next = data.Levels[trigger.Level - 1];
            if (next == null || string.IsNullOrEmpty(next.ModifyName) || string.IsNullOrEmpty(data.Function)) { reason = "Native upgrade path unresolved"; return false; }
            System.Reflection.MethodInfo method = EntryManager.Instance == null ? null : EntryManager.Instance.GetType().GetMethod(data.Function);
            if (method == null || method.ReturnType != typeof(bool)) { reason = "Native upgrade path unresolved"; return false; }
            return true;
        }

        internal static bool TryLevelUpEntryOnce(string entryName, out string reason)
        {
            reason = null;
            if (CtrlManager.Instance == null || CtrlManager.Instance.CtrlNpc == null || CtrlManager.Instance.CtrlNpc.m_ThingAttribute == null) { reason = "No active run"; return false; }
            ThingAttribute attributes = CtrlManager.Instance.CtrlNpc.m_ThingAttribute;
            TriggerEntry trigger;
            if (attributes.m_DicTriggerEntry == null || !attributes.m_DicTriggerEntry.TryGetValue(entryName, out trigger)) { reason = "Entry unavailable"; return false; }
            EntryData data = EntryManager.Instance == null ? null : EntryManager.Instance.GetEntryData(entryName);
            if (!CanLevelEntry(data, trigger, out reason)) return false;
            try
            {
                if (!attributes.EntryDirectup(data, trigger)) { reason = "Native upgrade failed"; return false; }
                Plugin.LogInfo("Artifact leveled: " + entryName + " -> Level " + trigger.Level);
                return true;
            }
            catch (Exception ex)
            {
                reason = "Native upgrade failed";
                Plugin.LogInfo("Artifact upgrade failed: " + entryName + " (" + ex.GetType().Name + ")");
                return false;
            }
        }

        internal static bool SkillProgressionReady(out string reason)
        {
            if (UnlockManager.Instance == null) { reason = "UnlockManager unavailable"; return false; }
            if (SkillManager.Instance == null || SkillManager.Instance.m_DicSkilldata == null) { reason = "SkillManager unavailable"; return false; }
            if (IllustratedHandbookManager.Instance == null) { reason = "HandbookManager unavailable"; return false; }
            if (SectManager.Instance == null) { reason = "SectManager unavailable"; return false; }
            reason = null;
            return true;
        }

        internal static bool EntryProgressionReady(out string reason)
        {
            if (UnlockManager.Instance == null) { reason = "UnlockManager unavailable"; return false; }
            if (EntryManager.Instance == null || EntryManager.m_DicEntryData == null) { reason = "EntryManager unavailable"; return false; }
            if (IllustratedHandbookManager.Instance == null) { reason = "HandbookManager unavailable"; return false; }
            reason = null;
            return true;
        }

        internal static bool CultivationProgressionReady(out string reason)
        {
            if (UnlockManager.Instance == null) { reason = "UnlockManager unavailable"; return false; }
            if (UnlockManager.Instance.m_DicCultivationSchools == null || UnlockManager.Instance.m_CultivationSchoolUnLockList == null) { reason = "Cultivation school data unavailable"; return false; }
            reason = null;
            return true;
        }

        internal static string UnlockAllSkills()
        {
            string reason;
            if (!SkillProgressionReady(out reason)) return reason;
            int skills = 0;
            int sects = 0;
            HashSet<string> processed = new HashSet<string>();
            foreach (SkillData data in SkillManager.Instance.m_DicSkilldata.Values.Where(IsUnlockableSkill).ToList())
            {
                if (data.SectNames != null)
                {
                    foreach (string sect in data.SectNames.Where(x => !string.IsNullOrEmpty(x)).Distinct())
                    {
                        if (processed.Add(sect) && CanUnlockSect(sect) && !UnlockManager.Instance.GetSectUnlock(sect))
                        {
                            UnlockManager.Instance.SetSectUnlock(sect, false);
                            sects++;
                        }
                    }
                }
                if (!UnlockManager.Instance.GetSkillUnlock(data.Name))
                {
                    UnlockManager.Instance.SetSkillUnlock(data.Name, false);
                    skills++;
                }
                IllustratedHandbookManager.Instance.UnlockHandbook(E_HandbookType.Skill, data.Name);
            }
            Plugin.LogInfo("Unlocked " + skills + " skills and " + sects + " sects");
            return "Unlocked " + skills + " skills and " + sects + " sects.";
        }

        internal static string UnlockAllEntries()
        {
            string reason;
            if (!EntryProgressionReady(out reason)) return reason;
            int count = 0;
            foreach (EntryData data in EntryManager.m_DicEntryData.Values.Where(IsUnlockableEntry).ToList())
            {
                if (!UnlockManager.Instance.GetEntryUnlock(data.Name))
                {
                    UnlockManager.Instance.SetEntryUnlock(data.Name, false);
                    count++;
                }
                IllustratedHandbookManager.Instance.UnlockHandbook(E_HandbookType.Fabao, data.Name);
            }
            Plugin.LogInfo("Unlocked " + count + " artifacts");
            return "Unlocked " + count + " artifacts.";
        }

        private static bool IsUnlockableSkill(SkillData data)
        {
            return data != null && data.Hide == 0 && data.Skills != null && data.Skills.Count > 0 && SkillManager.Instance.GetSkill(data.Skills[0]) != null && DlcSkillAvailable(data) && UnlockManager.Instance.m_SkillUnLockList.ContainsKey(data.Name) && UnlockManager.Instance.m_DicSkillConfigs.ContainsKey(data.Name) && data.SectNames != null && data.SectNames.Any(CanUnlockSect);
        }

        private static bool IsUnlockableEntry(EntryData data)
        {
            return data != null && data.IsHide == 0 && DlcEntryAvailable(data) && IsValidEntry(data) && UnlockManager.Instance.m_EntryUnLockList.ContainsKey(data.Name) && UnlockManager.Instance.m_DicEntryConfigs.ContainsKey(data.Name);
        }

        private static bool CanUnlockSect(string name)
        {
            SectDef sect = SectManager.Instance == null ? null : SectManager.Instance.GetSect(name);
            if (sect == null || sect.IsHide != 0 || !UnlockManager.Instance.m_SectUnLockList.ContainsKey(name)) return false;
            UnLockData config;
            if (!UnlockManager.Instance.m_DicSectConfigs.TryGetValue(name, out config) || config == null) return false;
            return !config.isDLC || (DLCMamager.Instance != null && DLCMamager.Instance.GetDLCByType(config.DLC));
        }

        internal static bool TryCompleteAllAchievements(out string message)
        {
            message = null;
            GameWatch gameWatch = GameWatch.Instance;
            AchievementManager rawManager = GetRawAchievementManager(gameWatch);
            SaveManager saveManager = gameWatch == null ? null : gameWatch.m_SaveManager;
            UnlockManager unlockManager = UnlockManager.Instance;
            Wnd_GameMain gameMain = null;
            try { gameMain = Wnd_GameMain.Instance; } catch { }
            bool steamInitialized = false;
            try { steamInitialized = SteamManager.Initialized; } catch { }
            bool managerClrNull = object.ReferenceEquals(rawManager, null);
            bool managerUnityNull = !managerClrNull && rawManager == null;
            int definitionCount = -1;
            if (!managerClrNull)
            {
                try { definitionCount = rawManager.Achievements == null ? -1 : rawManager.Achievements.Count; }
                catch { definitionCount = -1; }
            }
            Plugin.LogInfo("AchievementManager state: CLR-null=" + managerClrNull + ", Unity-null=" + managerUnityNull + ", Definitions=" + (definitionCount < 0 ? "unavailable" : definitionCount.ToString()));
            Plugin.LogInfo("Achievement runtime state: GameWatch=" + (gameWatch != null) + ", AchievementManager=" + !managerClrNull + ", SaveManager=" + (saveManager != null) + ", AchievementDefinitions=" + (definitionCount < 0 ? "unavailable" : definitionCount.ToString()) + ", UnlockManager=" + (unlockManager != null) + ", Wnd_GameMain=" + (gameMain != null) + ", GameMode=" + GameWatch.m_GameMode + ", SteamInitialized=" + steamInitialized);

            if (gameWatch == null) { message = "GameWatch is unavailable."; return false; }
            if (saveManager == null) { message = "SaveManager is unavailable."; return false; }
            AchievementManager manager;
            if (!TryEnsureAchievementManager(out manager, out message)) return false;

            List<AchievementManager.AchievementData> pending = manager.Achievements.Values
                .Where(x => x != null && x.ID >= 0 && !string.IsNullOrEmpty(x.Name) && !manager.IsUnLockAchievement(x.ID))
                .ToList();
            int[] rewardIds = { 1042, 1047, 1048, 1049, 1050, 1051, 1075, 1076 };
            if (pending.Any(x => rewardIds.Contains(x.ID)) && unlockManager == null)
            {
                message = "UnlockManager is unavailable for achievement rewards.";
                return false;
            }
            if (gameMain == null) { message = "Achievement notification UI is unavailable in this scene. Enter a gameplay scene and try again."; return false; }
            if (manager.Achievements.Count == 0) { message = "Achievement definitions are unavailable."; return false; }

            int completed = 0;
            try
            {
                foreach (AchievementManager.AchievementData data in pending)
                {
                    manager.UnLockAchievement(data.ID);
                    completed++;
                }
            }
            catch (Exception ex)
            {
                message = "Achievement completion failed: " + ex.GetType().Name;
                Plugin.LogInfo(message);
                return false;
            }
            Plugin.LogInfo("Completed " + completed + " achievements");
            message = completed == 0 ? "All achievements are already completed." : "Completed " + completed + " achievements.";
            return true;
        }

        private static bool TryEnsureAchievementManager(out AchievementManager manager, out string reason)
        {
            manager = null;
            reason = null;
            GameWatch gameWatch = GameWatch.Instance;
            if (gameWatch == null) { reason = "GameWatch is unavailable."; return false; }
            AchievementManager rawManager = GetRawAchievementManager(gameWatch);
            if (!object.ReferenceEquals(rawManager, null))
            {
                try
                {
                    if (rawManager.Achievements != null && rawManager.Achievements.Count > 0)
                    {
                        manager = rawManager;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    reason = "Achievement definitions could not be read: " + ex.GetType().Name;
                    return false;
                }
                reason = "Achievement definitions are unavailable.";
                return false;
            }
            if (gameWatch.m_SaveManager == null) { reason = "SaveManager is unavailable."; return false; }
            try
            {
                AchievementManager candidate = new AchievementManager();
                if (object.ReferenceEquals(candidate, null))
                {
                    reason = "AchievementManager allocation returned a CLR null reference.";
                    return false;
                }
                candidate.Init();
                if (candidate.Achievements == null || candidate.Achievements.Count == 0)
                {
                    reason = "Achievement definitions are unavailable.";
                    return false;
                }
                System.Reflection.PropertyInfo property = typeof(GameWatch).GetProperty("m_AchievementManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                System.Reflection.MethodInfo setter = property == null ? null : property.GetSetMethod(true);
                if (setter == null) { reason = "GameWatch achievement manager setter is unavailable."; return false; }
                setter.Invoke(gameWatch, new object[] { candidate });
                AchievementManager assigned = GetRawAchievementManager(gameWatch);
                bool assignedClrNull = object.ReferenceEquals(assigned, null);
                Plugin.LogInfo("AchievementManager assignment: CLR-null=" + assignedClrNull + ", ReferenceMatch=" + object.ReferenceEquals(assigned, candidate));
                if (assignedClrNull)
                {
                    reason = "GameWatch assignment failed.";
                    return false;
                }
                if (!object.ReferenceEquals(assigned, candidate))
                {
                    reason = "GameWatch assignment returned a different manager.";
                    return false;
                }
                manager = assigned;
                Plugin.LogInfo("AchievementManager candidate: CLR-null=False, Unity-null=" + (manager == null) + ", Definitions=" + manager.Achievements.Count);
                return true;
            }
            catch (Exception ex)
            {
                reason = "AchievementManager initialization failed.";
                Plugin.LogError("AchievementManager lazy initialization failed: " + ex);
                return false;
            }
        }

        private static AchievementManager GetRawAchievementManager(GameWatch gameWatch)
        {
            if (gameWatch == null) return null;
            System.Reflection.PropertyInfo property = typeof(GameWatch).GetProperty("m_AchievementManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return property == null ? null : (AchievementManager)property.GetValue(gameWatch, null);
        }

        private static bool SafeGameMainAvailable()
        {
            try { return Wnd_GameMain.Instance != null; } catch { return false; }
        }

        private static bool SafeSteamInitialized()
        {
            try { return SteamManager.Initialized; } catch { return false; }
        }

        internal static string UnlockAllCultivationSchools()
        {
            UnlockManager manager = UnlockManager.Instance;
            if (!CultivationProgressionReady(out string reason) || manager.m_RealmUnLockList == null) return reason ?? "Realm data unavailable";
            int count = 0;
            foreach (KeyValuePair<CultivationSchoolType, CultivationSchool> item in manager.m_DicCultivationSchools.ToList())
            {
                if (item.Value == null || item.Value.Modifies == null || item.Value.Modifies.Any(x => !manager.m_RealmUnLockList.ContainsKey(x))) continue;
                if (!manager.m_CultivationSchoolUnLockList.ContainsKey(item.Key) || manager.m_CultivationSchoolUnLockList[item.Key]) continue;
                manager.SetCultivationSchoolsUnlock(item.Key, false);
                count++;
            }
            Plugin.LogInfo("Unlocked " + count + " cultivation schools");
            return "Unlocked " + count + " cultivation schools.";
        }

        internal static IEnumerable<SkillData> GetSkills()
        {
            if (SkillManager.Instance == null || UnlockManager.Instance == null) return Enumerable.Empty<SkillData>();
            return SkillManager.Instance.m_DicSkilldata.Values
                .Where(x => x != null && x.Hide == 0 && x.Skills != null && x.Skills.Count > 0 && SkillManager.Instance.GetSkill(x.Skills[0]) != null && DlcSkillAvailable(x) && HasUnlockedSect(x) && UnlockManager.Instance.GetSkillUnlock(x.Name) && IllustratedHandbookManager.Instance != null && IllustratedHandbookManager.Instance.CheckUnlock(E_HandbookType.Skill, x.Name))
                .OrderBy(x => x.Rate).ThenBy(x => x.Name);
        }

        internal static IEnumerable<EntryData> GetEntries()
        {
            if (EntryManager.Instance == null || UnlockManager.Instance == null) return Enumerable.Empty<EntryData>();
            return EntryManager.m_DicEntryData.Values
                .Where(x => x != null && x.IsHide == 0 && DlcEntryAvailable(x) && UnlockManager.Instance.GetEntryUnlock(x.Name) && IllustratedHandbookManager.Instance != null && IllustratedHandbookManager.Instance.CheckUnlock(E_HandbookType.Fabao, x.Name) && IsValidEntry(x))
                .OrderBy(x => x.Rank).ThenBy(x => x.Name);
        }

        private static bool HasUnlockedSect(SkillData data)
        {
            if (data.SectNames == null || data.SectNames.Count == 0) return false;
            return data.SectNames.Any(x => UnlockManager.Instance.GetSectUnlock(x) && SectManager.Instance != null && SectManager.Instance.GetSect(x) != null && SectManager.Instance.GetSect(x).IsHide == 0);
        }

        private static bool DlcSkillAvailable(SkillData data)
        {
            UnLockData config;
            if (!UnlockManager.Instance.m_DicSkillConfigs.TryGetValue(data.Name, out config) || config == null || !config.isDLC) return true;
            return DLCMamager.Instance != null && DLCMamager.Instance.GetDLCByType(config.DLC);
        }

        private static bool DlcEntryAvailable(EntryData data)
        {
            UnLockData config;
            if (!UnlockManager.Instance.m_DicEntryConfigs.TryGetValue(data.Name, out config) || config == null || !config.isDLC) return true;
            return DLCMamager.Instance != null && DLCMamager.Instance.GetDLCByType(config.DLC);
        }

        internal static bool CanAddSkill(string name) { return CanUseSkill(name); }
        internal static bool CanAddEntry(string name) { return CanUseEntry(name); }

        internal static string SectText(SkillData data)
        {
            if (data == null || data.SectNames == null || data.SectNames.Count == 0) return "Unknown";
            return string.Join(" / ", data.SectNames.Select(SectText).ToArray());
        }

        private static string SectText(string name)
        {
            SectDef sect = SectManager.Instance == null ? null : SectManager.Instance.GetSect(name);
            if (sect == null) return name;
            if (string.IsNullOrEmpty(sect.DisplayName) || TFManager.Instance == null) return name;
            string text = TFManager.Instance.Get(sect.DisplayName);
            return string.IsNullOrEmpty(text) ? name : text;
        }

        internal static Color RankColor(int rank)
        {
            switch (rank)
            {
                case 1: return new Color32(0, 153, 255, 255);
                case 2: return new Color32(204, 51, 255, 255);
                case 3: return new Color32(255, 153, 0, 255);
                case 4: return new Color32(255, 0, 0, 255);
                default: return Color.white;
            }
        }

        private static bool CanUseSkill(string name)
        {
            SkillData data = SkillManager.Instance == null ? null : SkillManager.Instance.GetSkillData(name);
            if (data == null || data.Hide != 0 || data.Skills == null || data.Skills.Count == 0 || SkillManager.Instance.GetSkill(data.Skills[0]) == null || !DlcSkillAvailable(data) || !UnlockManager.Instance.GetSkillUnlock(name) || !HasUnlockedSect(data) || IllustratedHandbookManager.Instance == null || !IllustratedHandbookManager.Instance.CheckUnlock(E_HandbookType.Skill, name)) return false;
            return Plugin.AllowHighRankStartingSkills.Value || data.Rate <= 1;
        }

        private static bool CanUseEntry(string name)
        {
            EntryData data = EntryManager.Instance == null ? null : EntryManager.Instance.GetEntryData(name);
            if (data == null || data.IsHide != 0 || !DlcEntryAvailable(data) || !UnlockManager.Instance.GetEntryUnlock(name) || IllustratedHandbookManager.Instance == null || !IllustratedHandbookManager.Instance.CheckUnlock(E_HandbookType.Fabao, name)) return false;
            return Plugin.AllowHighRankStartingEntries.Value || data.Rank <= 1;
        }

        private static bool IsValidEntry(EntryData data)
        {
            return !string.IsNullOrEmpty(data.Function) && EntryManager.Instance != null && EntryManager.Instance.GetType().GetMethod(data.Function) != null;
        }

    }

    internal static class CurrencyTools
    {
        private static readonly string[] Keys = { "Currency_CanPo", "Currency_CanPoJingCui", "Currency_YuanChuZhiQi" };
        internal static bool IsAvailable()
        {
            return WealthManager.Instance != null && CtrlManager.Instance != null && CtrlManager.Instance.CtrlNpc != null && CtrlManager.Instance.CtrlNpc.m_ThingAttribute != null;
        }
        internal static float Get(string key) { return WealthManager.Instance == null ? 0f : WealthManager.Instance.GetCurrency(key); }
        internal static string DisplayName(string key)
        {
            CurrencyDef def = ThingManager.Instance == null ? null : ThingManager.Instance.GetCurrencyDef(key);
            if (def != null && !string.IsNullOrEmpty(def.DisplayName) && TFManager.Instance != null)
            {
                string value = TFManager.Instance.Get(def.DisplayName);
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return key == "Currency_CanPo" ? "CanPo" : (key == "Currency_CanPoJingCui" ? "CanPoJingCui" : (key == "Currency_YuanChuZhiQi" ? "YuanChuZhiQi" : key));
        }
        internal static void Add(string key, float amount)
        {
            if (!IsAvailable() || !Keys.Contains(key) || amount <= 0f) return;
            WealthManager.Instance.AddCurrency(key, amount);
            Plugin.LogInfo("Added " + amount.ToString("F0") + " " + key);
        }
    }

    [HarmonyPatch(typeof(UnlockManager), nameof(UnlockManager.InitList), new Type[0])]
    internal static class StartLoadoutInitPatch
    {
        private static void Postfix() { StartLoadout.Normalize(); }
    }

    [HarmonyPatch(typeof(ThingAttribute), nameof(ThingAttribute.AddHP), new Type[]
    {
        typeof(float), typeof(int), typeof(E_DamageType), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
    })]
    internal static class InfiniteHealthPatch
    {
        private static void Prefix(ThingAttribute __instance, ref float addhp)
        {
            if (Plugin.InfiniteHealth.Value && addhp < 0f && __instance != null && Plugin.IsPlayer(__instance.m_Owner)) addhp = 0f;
        }
    }

    [HarmonyPatch(typeof(ThingAttribute), nameof(ThingAttribute.AddPropertyValue), new Type[]
    {
        typeof(AttributeName), typeof(float), typeof(AttributeKind), typeof(int), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(bool)
    })]
    internal static class InfiniteHealthFillCostPatch
    {
        private static void Prefix(ThingAttribute __instance, AttributeName name, AttributeKind kind, ref float value)
        {
            if (Plugin.InfiniteHealth.Value && name == AttributeName.HP && kind == AttributeKind.Fill && value < 0f && __instance != null && Plugin.IsPlayer(__instance.m_Owner))
            {
                if (Plugin.AbilityScalingDiagnostics.Value)
                    Plugin.ModLogger.LogInfo("Infinite HP blocked: Player=" + __instance.m_Owner.m_ID + " Source=SkillCost RequestedDelta=" + value.ToString("F2") + " Method=ThingAttribute.AddPropertyValue");
                value = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(ThingAttribute), nameof(ThingAttribute.AddPropertyFillValue), new Type[]
    {
        typeof(AttributeName), typeof(float), typeof(int), typeof(E_DamageType), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(bool)
    })]
    internal static class InfiniteHealthDirectFillCostPatch
    {
        private static void Prefix(ThingAttribute __instance, AttributeName name, ref float value)
        {
            if (Plugin.InfiniteHealth.Value && name == AttributeName.HP && value < 0f && __instance != null && Plugin.IsPlayer(__instance.m_Owner))
            {
                if (Plugin.AbilityScalingDiagnostics.Value)
                    Plugin.ModLogger.LogInfo("Infinite HP blocked: Player=" + __instance.m_Owner.m_ID + " Source=HPFill RequestedDelta=" + value.ToString("F2") + " Method=ThingAttribute.AddPropertyFillValue");
                value = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(ThingAttribute), nameof(ThingAttribute.AddPropertyFillValue), new Type[]
    {
        typeof(string), typeof(float), typeof(int), typeof(E_DamageType), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
    })]
    internal static class InfiniteHealthStringFillCostPatch
    {
        private static void Prefix(ThingAttribute __instance, string name, ref float value)
        {
            if (Plugin.InfiniteHealth.Value && string.Equals(name, "HP", StringComparison.Ordinal) && value < 0f && __instance != null && Plugin.IsPlayer(__instance.m_Owner)) value = 0f;
        }
    }

    [HarmonyPatch(typeof(PropertyData), "set__FillValue")]
    internal static class InfiniteHealthDirectCurrentHpPatch
    {
        private static void Prefix(PropertyData __instance, ref float value)
        {
            if (!Plugin.InfiniteHealth.Value || __instance == null || __instance.NameType != AttributeName.HP || __instance.TA == null || !Plugin.IsPlayer(__instance.TA.m_Owner)) return;
            if (value < __instance._FillValue)
            {
                if (Plugin.AbilityScalingDiagnostics.Value)
                    Plugin.ModLogger.LogInfo("Infinite HP blocked: Player=" + __instance.TA.m_Owner.m_ID + " Source=DirectCurrentHP RequestedValue=" + value.ToString("F2") + " BeforeHP=" + __instance._FillValue.ToString("F2") + " Method=PropertyData._FillValue");
                value = __instance._FillValue;
            }
        }
    }

    [HarmonyPatch(typeof(ThingAttribute), nameof(ThingAttribute.AddPropertyFillValue), new Type[]
    {
        typeof(string), typeof(float), typeof(int), typeof(E_DamageType), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool)
    })]
    internal static class InfiniteMPStringFillPatch
    {
        private static void Prefix(ThingAttribute __instance, string name, ref float value)
        {
            if (Plugin.InfiniteMP.Value && value < 0f && string.Equals(name, "MP", StringComparison.Ordinal) && __instance != null && Plugin.IsPlayer(__instance.m_Owner)) value = 0f;
        }
    }

    [HarmonyPatch(typeof(ThingAttribute), nameof(ThingAttribute.AddPropertyFillValue), new Type[]
    {
        typeof(AttributeName), typeof(float), typeof(int), typeof(E_DamageType), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(bool)
    })]
    internal static class InfiniteMPEnumFillPatch
    {
        private static void Prefix(ThingAttribute __instance, AttributeName name, ref float value)
        {
            if (Plugin.InfiniteMP.Value && value < 0f && name == AttributeName.MP && __instance != null && Plugin.IsPlayer(__instance.m_Owner)) value = 0f;
        }
    }

    [HarmonyPatch(typeof(ThingAttribute), nameof(ThingAttribute.AddPropertyMPFillValue), new Type[]
    {
        typeof(AttributeName), typeof(float), typeof(int), typeof(E_DamageType), typeof(string), typeof(string), typeof(bool), typeof(bool)
    })]
    internal static class InfiniteMPDirectFillPatch
    {
        private static void Prefix(ThingAttribute __instance, AttributeName name, ref float value)
        {
            if (Plugin.InfiniteMP.Value && value < 0f && name == AttributeName.MP && __instance != null && Plugin.IsPlayer(__instance.m_Owner)) value = 0f;
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.GetSkillCD), new Type[] { typeof(string) })]
    internal static class NoCooldownPatch
    {
        private static void Postfix(FightBody __instance, ref float __result)
        {
            if (Plugin.NoSkillCooldowns.Value && __instance != null && Plugin.IsPlayer(__instance.m_Owner)) __result = 0f;
        }
    }

    [HarmonyPatch(typeof(FightBody))]
    internal static class DamageMultiplierPatch
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(FightBody), nameof(FightBody.CalculationDamage), new Type[] { typeof(Thing), typeof(float), typeof(E_DamageType), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType() });
        }
        private static void Postfix(Thing fromthing, ref float __result)
        {
            if (!Plugin.IsPlayer(fromthing) || __result <= 0f) return;
            float result = __result * Plugin.RuntimeDamageMultiplier;
            if (!float.IsNaN(result) && !float.IsInfinity(result)) __result = result;
        }
    }

    [HarmonyPatch(typeof(ThingAttribute), "get_MoveSpeed")]
    internal static class MovementSpeedPatch
    {
        private static void Postfix(ThingAttribute __instance, ref float __result)
        {
            if (__instance != null && Plugin.IsPlayer(__instance.m_Owner))
            {
                float result = __result * Plugin.RuntimeMovementMultiplier;
                if (!float.IsNaN(result) && !float.IsInfinity(result)) __result = result;
            }
        }
    }

    [HarmonyPatch(typeof(CameraManager), "UpdateShake")]
    internal static class CameraConsistencyPatch
    {
        private static void Prefix(CameraManager __instance)
        {
            CameraConsistency.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(CameraManager), "OnLateUpdate")]
    internal static class CameraScalingDiagnosticPatch
    {
        private static void Postfix(CameraManager __instance)
        {
            FogController.Update();
            BlurController.Update(__instance);
            ZoomGeometryCleanup.Update(__instance);
        }
    }

    internal enum CameraLockKind
    {
        None,
        GameplayRoom,
        Scripted,
        Cinematic,
        Unknown
    }

    [HarmonyPatch(typeof(MapTriggerHelper), "OnTriggerEnter", new Type[] { typeof(Collider) })]
    internal static class CameraLockEnterPatch
    {
        private static void Postfix(MapTriggerHelper __instance)
        {
            CameraLockTracker.RecordTrigger(__instance);
        }
    }

    [HarmonyPatch(typeof(MapTriggerHelper), "OnTriggerExit", new Type[] { typeof(Collider) })]
    internal static class CameraLockExitPatch
    {
        private static void Postfix(MapTriggerHelper __instance)
        {
            CameraLockTracker.ClearIfUnlocked();
        }
    }

    internal static class CameraLockTracker
    {
        internal static CameraLockKind Kind { get; private set; }

        internal static void RecordTrigger(MapTriggerHelper trigger)
        {
            if (trigger == null || CameraManager.Instance == null || !CameraManager.Instance.Lock) return;
            if (trigger.m_Kind == TriggerKind.LockCameraPos)
            {
                Kind = MapMain.Instance != null && MapMain.Instance.GetIsPlotPlaying() ? CameraLockKind.Cinematic : CameraLockKind.GameplayRoom;
            }
            else if (trigger.m_Kind == TriggerKind.CameraMove)
            {
                Kind = CameraLockKind.Scripted;
            }
            else
            {
                Kind = CameraLockKind.Unknown;
            }
        }

        internal static void ClearIfUnlocked()
        {
            if (CameraManager.Instance == null || !CameraManager.Instance.Lock) Kind = CameraLockKind.None;
        }

        internal static CameraLockKind GetKind(CameraManager camera)
        {
            if (camera == null || !camera.Lock) return CameraLockKind.None;
            return Kind == CameraLockKind.None ? CameraLockKind.Unknown : Kind;
        }
    }

    internal static class CameraDistanceScope
    {
        private static readonly System.Reflection.FieldInfo FollowField = AccessTools.Field(typeof(CameraManager), "m_Npcplayer");
        private static readonly System.Reflection.FieldInfo PointField = AccessTools.Field(typeof(CameraManager), "m_GameObject");

        internal static bool IsNormalFollow(CameraManager camera)
        {
            if (camera == null || CtrlManager.Instance == null || CtrlManager.Instance.CtrlNpc == null) return false;
            if (MapMain.Instance != null && MapMain.Instance.GetIsPlotPlaying()) return false;
            Npc followed = FollowField == null ? null : FollowField.GetValue(camera) as Npc;
            if (followed == null || followed.m_ID != CtrlManager.Instance.CtrlNpc.m_ID || !followed.IsPlayerNpc) return false;
            GameObject point = PointField == null ? null : PointField.GetValue(camera) as GameObject;
            if (point == null || point.name != "CameraPoint" || GameWatch.Instance == null || point.transform.parent != GameWatch.Instance.transform) return false;
            return !camera.Lock || CameraLockTracker.GetKind(camera) == CameraLockKind.GameplayRoom;
        }
    }

    internal static class CameraConsistency
    {
        private static readonly System.Reflection.FieldInfo PointField = AccessTools.Field(typeof(CameraManager), "m_GameObject");
        private static CameraManager baselineManager;
        private static int baselineSceneHandle = int.MinValue;
        private static float baselineDistance;

        internal static float BaselineDistance { get { return baselineDistance; } }

        internal static float GetTargetDistance()
        {
            return baselineDistance > 0f ? baselineDistance * Plugin.RuntimeCameraDistanceMultiplier : 0f;
        }

        internal static float GetActualDistance(CameraManager camera, GameObject point)
        {
            return camera == null || camera.m_Camera == null || point == null ? 0f : Vector3.Distance(camera.m_Camera.transform.position, point.transform.position);
        }

        internal static void Apply(CameraManager camera)
        {
            if (!CameraDistanceScope.IsNormalFollow(camera) || camera.m_Camera == null || PointField == null) return;
            GameObject point = PointField.GetValue(camera) as GameObject;
            if (point == null) return;
            Vector3 nativeVector = camera.m_Camera.transform.position - point.transform.position;
            float nativeDistance = nativeVector.magnitude;
            if (nativeDistance <= 0.001f || float.IsNaN(nativeDistance) || float.IsInfinity(nativeDistance)) return;
            int sceneHandle = SceneManager.GetActiveScene().handle;
            if (baselineManager != camera || baselineSceneHandle != sceneHandle || baselineDistance <= 0f)
            {
                baselineManager = camera;
                baselineSceneHandle = sceneHandle;
                baselineDistance = nativeDistance;
            }
            float targetDistance = baselineDistance * Plugin.RuntimeCameraDistanceMultiplier;
            Vector3 desired = point.transform.position + nativeVector / nativeDistance * targetDistance;
            camera.m_Camera.transform.position = desired;
        }
    }

    internal static class ZoomGeometryCleanup
    {
        private static readonly System.Reflection.FieldInfo PointField = AccessTools.Field(typeof(CameraManager), "m_GameObject");
        private sealed class MaterialState
        {
            internal float Dither;
            internal int SceneHandle;
        }
        private sealed class RendererState
        {
            internal bool Enabled;
            internal UnityEngine.Rendering.ShadowCastingMode ShadowCastingMode;
        }

        private static readonly RaycastHit[] Hits = new RaycastHit[128];
        private static readonly Dictionary<Material, MaterialState> Modified = new Dictionary<Material, MaterialState>();
        private static readonly Dictionary<Renderer, RendererState> ModifiedRenderers = new Dictionary<Renderer, RendererState>();
        private static readonly HashSet<int> Reported = new HashSet<int>();
        private static readonly List<MeshRenderer> SceneCandidates = new List<MeshRenderer>();
        private static int sceneHandle = int.MinValue;
        private static float nextEvaluation;
        private static float nextCandidateRefresh;

        internal static void Update(CameraManager camera)
        {
            int currentScene = SceneManager.GetActiveScene().handle;
            if (currentScene != sceneHandle)
            {
                RestoreNative();
                sceneHandle = currentScene;
                SceneCandidates.Clear();
                nextCandidateRefresh = 0f;
                Reported.Clear();
            }
            if (camera == null || camera.m_Camera == null || PointField == null || Plugin.RuntimeCameraDistanceMultiplier <= 1.25f || !CameraDistanceScope.IsNormalFollow(camera))
            {
                RestoreNative();
                return;
            }
            if (!Plugin.HideZoomOccluders.Value) { RestoreNative(); return; }
            if (Time.unscaledTime < nextEvaluation) return;
            nextEvaluation = Time.unscaledTime + 0.15f;
            GameObject point = PointField.GetValue(camera) as GameObject;
            if (point == null) { RestoreNative(); return; }
            Vector3 origin = camera.m_Camera.transform.position;
            Vector3 target = point.transform.position + new Vector3(0f, 0.1f, 0f);
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.001f) { RestoreNative(); return; }
            if (Time.unscaledTime >= nextCandidateRefresh)
            {
                SceneCandidates.Clear();
                MeshRenderer[] meshes = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
                for (int i = 0; i < meshes.Length; i++) if (IsEnvironmentCandidate(meshes[i])) SceneCandidates.Add(meshes[i]);
                nextCandidateRefresh = Time.unscaledTime + 1f;
            }
            HashSet<Material> visible = new HashSet<Material>();
            HashSet<Renderer> visibleRenderers = new HashSet<Renderer>();
            HashSet<Renderer> ditherRenderers = new HashSet<Renderer>();
            int count = Physics.BoxCastNonAlloc(origin, new Vector3(0.1f, 0.1f, 0.1f), direction / distance, Hits, Quaternion.identity, distance);
            for (int i = 0; i < count; i++)
            {
                Collider collider = Hits[i].collider;
                if (collider == null || !collider.CompareTag("Scene")) continue;
                Renderer[] renderers = collider.gameObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    Material[] materials = renderer.materials;
                    bool supportsDither = materials.Length > 0;
                    foreach (Material material in materials)
                    {
                        if (material == null || material.shader == null || material.shader.name == "Standard" || !material.HasProperty("_Dither")) supportsDither = false;
                    }
                    if (supportsDither)
                    {
                        ditherRenderers.Add(renderer);
                        foreach (Material material in materials)
                        {
                            visible.Add(material);
                            MaterialState state;
                            if (!Modified.TryGetValue(material, out state))
                            {
                                state = new MaterialState { Dither = material.GetFloat("_Dither"), SceneHandle = currentScene };
                                Modified[material] = state;
                            }
                            material.SetFloat("_Dither", Mathf.Min(material.GetFloat("_Dither"), 0.3f));
                            LogCandidate(renderer, material, origin, point, "Dither", currentScene);
                        }
                    }
                    else
                    {
                        visibleRenderers.Add(renderer);
                        RendererState state;
                        if (!ModifiedRenderers.TryGetValue(renderer, out state))
                        {
                            state = new RendererState { Enabled = renderer.enabled, ShadowCastingMode = renderer.shadowCastingMode };
                            ModifiedRenderers[renderer] = state;
                        }
                        try { renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; LogCandidate(renderer, materials.Length > 0 ? materials[0] : null, origin, point, "ShadowsOnly", currentScene); }
                        catch (Exception) { renderer.enabled = false; LogCandidate(renderer, materials.Length > 0 ? materials[0] : null, origin, point, "Disabled", currentScene); }
                    }
                }
            }
            Ray ray = new Ray(origin, direction / distance);
            for (int i = 0; i < SceneCandidates.Count; i++)
            {
                MeshRenderer renderer = SceneCandidates[i];
                if (renderer == null || !renderer.enabled || ditherRenderers.Contains(renderer)) continue;
                float rayDistance;
                if (!renderer.bounds.IntersectRay(ray, out rayDistance) || rayDistance < 0f || rayDistance >= distance) continue;
                visibleRenderers.Add(renderer);
                RendererState state;
                if (!ModifiedRenderers.TryGetValue(renderer, out state))
                {
                    state = new RendererState { Enabled = renderer.enabled, ShadowCastingMode = renderer.shadowCastingMode };
                    ModifiedRenderers[renderer] = state;
                }
                try { renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; LogCandidate(renderer, renderer.sharedMaterial, origin, point, "ShadowsOnly", currentScene, rayDistance, distance, "RendererBounds"); }
                catch (Exception) { renderer.enabled = false; LogCandidate(renderer, renderer.sharedMaterial, origin, point, "Disabled", currentScene, rayDistance, distance, "RendererBounds"); }
            }
            foreach (Material material in Modified.Keys.ToList())
            {
                if (material == null || !visible.Contains(material))
                {
                    if (material != null) material.SetFloat("_Dither", Modified[material].Dither);
                    Modified.Remove(material);
                }
            }
            foreach (Renderer renderer in ModifiedRenderers.Keys.ToList())
            {
                if (renderer == null || !visibleRenderers.Contains(renderer))
                {
                    if (renderer != null)
                    {
                        renderer.enabled = ModifiedRenderers[renderer].Enabled;
                        renderer.shadowCastingMode = ModifiedRenderers[renderer].ShadowCastingMode;
                    }
                    ModifiedRenderers.Remove(renderer);
                }
            }
        }

        internal static void RestoreNative()
        {
            foreach (KeyValuePair<Material, MaterialState> item in Modified.ToList())
                if (item.Key != null) item.Key.SetFloat("_Dither", item.Value.Dither);
            Modified.Clear();
            foreach (KeyValuePair<Renderer, RendererState> item in ModifiedRenderers.ToList())
            {
                if (item.Key != null)
                {
                    if (Plugin.CameraGeometryDiagnostics.Value)
                        Plugin.ModLogger.LogInfo("Zoom occluder restored: Renderer=" + item.Key.GetType().Name + " Path=" + GetPath(item.Key.transform));
                    item.Key.enabled = item.Value.Enabled;
                    item.Key.shadowCastingMode = item.Value.ShadowCastingMode;
                }
            }
            ModifiedRenderers.Clear();
            Reported.Clear();
        }

        private static bool IsEnvironmentCandidate(MeshRenderer renderer)
        {
            if (renderer == null || renderer.GetComponentInParent<NpcView>() != null || HasAncestorComponent(renderer.transform, "UnityEngine.Canvas") || renderer.GetComponentInParent<BulletEffect>() != null || renderer.GetComponentInParent<ParticleSystem>() != null) return false;
            return renderer.gameObject.activeInHierarchy && renderer.gameObject.layer != LayerMask.NameToLayer("UI");
        }

        private static bool HasAncestorComponent(Transform current, string fullName)
        {
            while (current != null)
            {
                Component[] components = current.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) if (components[i] != null && components[i].GetType().FullName == fullName) return true;
                current = current.parent;
            }
            return false;
        }

        private static void LogCandidate(Renderer renderer, Material material, Vector3 origin, GameObject point, string action, int currentScene, float rayDistance = -1f, float targetDistance = -1f, string method = "Collider")
        {
            if (!Plugin.CameraGeometryDiagnostics.Value || renderer == null || !Reported.Add(renderer.GetInstanceID())) return;
            string shader = material == null || material.shader == null ? "(none)" : material.shader.name;
            bool supportsDither = material != null && material.shader != null && material.shader.name != "Standard" && material.HasProperty("_Dither");
            Plugin.ModLogger.LogInfo("Zoom occluder: Scene=" + SceneManager.GetActiveScene().name + " Path=" + GetPath(renderer.transform) + " Method=" + method + " Renderer=" + renderer.GetType().Name + " Material=" + (material == null ? "(none)" : material.name) + " Shader=" + shader + " Bounds=" + renderer.bounds + " RayDistance=" + rayDistance.ToString("F2") + " TargetDistance=" + targetDistance.ToString("F2") + " Action=" + action);
        }

        private static string GetPath(Transform current)
        {
            if (current == null) return "null";
            List<string> names = new List<string>();
            while (current != null) { names.Add(current.name); current = current.parent; }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }

    internal static class BlurController
    {
        private sealed class BlurState
        {
            internal object Effect;
            internal object EnabledParameter;
            internal object ApertureParameter;
            internal bool NativeEnabled;
            internal float NativeAperture;
            internal bool HasAperture;
            internal bool Applied;
            internal bool LastEnabled;
            internal float LastAperture;
            internal float AppliedMultiplier;
        }

        private static BlurState state;
        private static int sceneHandle = int.MinValue;

        internal static void Update(CameraManager camera)
        {
            if (camera == null || camera.m_Camera == null || Plugin.ReduceBlurWhenZoomedOut == null) return;
            int currentScene = SceneManager.GetActiveScene().handle;
            if (currentScene != sceneHandle)
            {
                RestoreNative();
                sceneHandle = currentScene;
            }
            if (!CameraDistanceScope.IsNormalFollow(camera) || !Plugin.ReduceBlurWhenZoomedOut.Value || Plugin.RuntimeCameraDistanceMultiplier <= 1f)
            {
                RestoreNative();
                return;
            }

            object effect = FindDepthOfField(camera.m_Camera, currentScene);
            if (effect == null)
            {
                RestoreNative();
                return;
            }
            if (state == null || !ReferenceEquals(state.Effect, effect))
            {
                RestoreNative();
                state = Capture(effect);
            }
            if (state == null || state.EnabledParameter == null) return;

            bool currentEnabled;
            float currentAperture;
            bool hasCurrentAperture = TryGetBool(state.EnabledParameter, out currentEnabled);
            if (!state.Applied || !Mathf.Approximately(Plugin.RuntimeCameraDistanceMultiplier, state.AppliedMultiplier))
            {
                if (hasCurrentAperture) state.NativeEnabled = currentEnabled;
                if (state.HasAperture && TryGetFloat(state.ApertureParameter, out currentAperture)) state.NativeAperture = currentAperture;
            }
            else
            {
                if (hasCurrentAperture && currentEnabled != state.LastEnabled) state.NativeEnabled = currentEnabled;
                if (state.HasAperture && TryGetFloat(state.ApertureParameter, out currentAperture) && !Mathf.Approximately(currentAperture, state.LastAperture)) state.NativeAperture = currentAperture;
            }

            float t = Mathf.Clamp01(Plugin.RuntimeCameraDistanceMultiplier - 1f);
            bool appliedEnabled = state.NativeEnabled;
            if (state.HasAperture)
            {
                float appliedAperture = Mathf.Lerp(state.NativeAperture, 0.05f, t);
                TrySetFloat(state.ApertureParameter, appliedAperture);
                state.LastAperture = appliedAperture;
            }
            if (t >= 0.999f) appliedEnabled = false;
            TrySetBool(state.EnabledParameter, appliedEnabled);
            state.LastEnabled = appliedEnabled;
            state.Applied = true;
            state.AppliedMultiplier = Plugin.RuntimeCameraDistanceMultiplier;
        }

        internal static void RestoreNative()
        {
            if (state != null)
            {
                TrySetBool(state.EnabledParameter, state.NativeEnabled);
                if (state.HasAperture) TrySetFloat(state.ApertureParameter, state.NativeAperture);
                state = null;
            }
        }

        private static object FindDepthOfField(Camera camera, int currentScene)
        {
            Component[] components = camera.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null) continue;
                string name = component.GetType().Name;
                if (name.IndexOf("PostProcessVolume", StringComparison.OrdinalIgnoreCase) < 0) continue;
                object profile = GetMember(component, "sharedProfile");
                object settings = GetMember(profile, "settings");
                System.Collections.IEnumerable enumerable = settings as System.Collections.IEnumerable;
                if (enumerable == null) continue;
                foreach (object setting in enumerable)
                {
                    if (setting == null) continue;
                    string settingName = setting.GetType().Name;
                    if (settingName.IndexOf("DepthOfField", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return setting;
                    }
                }
            }
            return null;
        }

        private static BlurState Capture(object effect)
        {
            object enabled = GetMember(effect, "enabled");
            bool nativeEnabled;
            if (enabled == null || !TryGetBool(enabled, out nativeEnabled)) return null;
            object aperture = GetMember(effect, "aperture");
            float nativeAperture = 0f;
            bool hasAperture = aperture != null && TryGetFloat(aperture, out nativeAperture);
            return new BlurState { Effect = effect, EnabledParameter = enabled, ApertureParameter = aperture, NativeEnabled = nativeEnabled, NativeAperture = hasAperture ? nativeAperture : 0f, HasAperture = hasAperture };
        }

        private static object GetMember(object target, string name)
        {
            if (target == null) return null;
            Type type = target.GetType();
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.FieldInfo field = type.GetField(name, flags);
            if (field != null) return field.GetValue(target);
            System.Reflection.PropertyInfo property = type.GetProperty(name, flags);
            return property == null ? null : property.GetValue(target, null);
        }

        private static bool TryGetBool(object parameter, out bool value)
        {
            object current = GetMember(parameter, "value");
            if (current is bool)
            {
                value = (bool)current;
                return true;
            }
            value = false;
            return false;
        }

        private static bool TryGetFloat(object parameter, out float value)
        {
            object current = GetMember(parameter, "value");
            if (current is float)
            {
                value = (float)current;
                return true;
            }
            value = 0f;
            return false;
        }

        private static bool TrySetBool(object parameter, bool value)
        {
            return SetMember(GetMemberTarget(parameter), "value", value);
        }

        private static bool TrySetFloat(object parameter, float value)
        {
            return SetMember(GetMemberTarget(parameter), "value", value);
        }

        private static object GetMemberTarget(object parameter)
        {
            return parameter;
        }

        private static bool SetMember(object target, string name, object value)
        {
            if (target == null) return false;
            Type type = target.GetType();
            System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            System.Reflection.FieldInfo field = type.GetField(name, flags);
            if (field != null) { field.SetValue(target, value); return true; }
            System.Reflection.PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.CanWrite) { property.SetValue(target, value, null); return true; }
            return false;
        }

        private static string GetPath(Transform current)
        {
            if (current == null) return "null";
            List<string> names = new List<string>();
            while (current != null) { names.Add(current.name); current = current.parent; }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }

    internal static class FogController
    {
        private sealed class FogState
        {
            internal bool Enabled;
            internal FogMode Mode;
            internal float Density;
            internal float Start;
            internal float End;
            internal Color Color;
        }

        private static FogState native;
        private static FogState applied;
        private static float appliedMultiplier;
        private static int sceneHandle = int.MinValue;

        internal static void Update()
        {
            if (Plugin.ReduceFogWhenZoomedOut == null) return;
            int currentScene = SceneManager.GetActiveScene().handle;
            if (currentScene != sceneHandle)
            {
                sceneHandle = currentScene;
                native = null;
                applied = null;
            }
            FogState current = Capture();
            if (native == null)
            {
                native = current;
            }
            float multiplier = Plugin.RuntimeCameraDistanceMultiplier;
            if (!Plugin.ReduceFogWhenZoomedOut.Value || multiplier <= 1f)
            {
                RestoreNative();
                return;
            }
            if (applied != null && Mathf.Approximately(multiplier, appliedMultiplier) && !Matches(current, applied))
            {
                native = current;
            }

            if (!native.Enabled)
            {
                RestoreNative();
                return;
            }

            FogState next = Copy(native);
            float t = Mathf.Clamp01(multiplier - 1f);
            if (next.Mode == FogMode.Exponential || next.Mode == FogMode.ExponentialSquared)
            {
                next.Density = native.Density * Mathf.Lerp(1f, 0.1f, t);
            }
            else if (next.Mode == FogMode.Linear)
            {
                next.End = native.End > 0f ? native.End * Mathf.Lerp(1f, 4f, t) : 1000f * Mathf.Lerp(1f, 4f, t);
            }
            Apply(next);
            applied = next;
            appliedMultiplier = multiplier;
        }

        internal static void RestoreNative()
        {
            if (native != null) Apply(native);
            applied = null;
            appliedMultiplier = 0f;
        }

        private static FogState Capture()
        {
            return new FogState { Enabled = RenderSettings.fog, Mode = RenderSettings.fogMode, Density = RenderSettings.fogDensity, Start = RenderSettings.fogStartDistance, End = RenderSettings.fogEndDistance, Color = RenderSettings.fogColor };
        }

        private static FogState Copy(FogState source)
        {
            return new FogState { Enabled = source.Enabled, Mode = source.Mode, Density = source.Density, Start = source.Start, End = source.End, Color = source.Color };
        }

        private static void Apply(FogState state)
        {
            RenderSettings.fog = state.Enabled;
            RenderSettings.fogMode = state.Mode;
            RenderSettings.fogDensity = state.Density;
            RenderSettings.fogStartDistance = state.Start;
            RenderSettings.fogEndDistance = state.End;
            RenderSettings.fogColor = state.Color;
        }

        private static bool Matches(FogState a, FogState b)
        {
            return a.Enabled == b.Enabled && a.Mode == b.Mode && Mathf.Approximately(a.Density, b.Density) && Mathf.Approximately(a.Start, b.Start) && Mathf.Approximately(a.End, b.End) && a.Color == b.Color;
        }
    }

    [HarmonyPatch(typeof(AttackHelperManager), nameof(AttackHelperManager.GetAttackHelperObj), new Type[] { typeof(Thing), typeof(SkillBox), typeof(int), typeof(float) })]
    internal static class PlayerAbilityBoxPatch
    {
        private static void Postfix(Thing thing, AttackBox __result)
        {
            AbilityScalingRegistry.Register(__result);
        }
    }

    [HarmonyPatch(typeof(AttackHelperManager), nameof(AttackHelperManager.TriggerAttackHelperForSkill), new Type[] { typeof(int), typeof(string), typeof(int), typeof(int), typeof(float) })]
    internal static class PlayerAbilitySizePatch
    {
        private static void Postfix(int thingid, string skillname, int level, int index)
        {
            if (!Plugin.ScaleAbilityGameplayAreas.Value || ThingManager.Instance == null) return;
            Thing thing = ThingManager.Instance.FindThing(thingid);
            if (!Plugin.IsPlayer(thing)) return;
            Npc npc = thing as Npc;
            Skill skill = SkillManager.Instance == null ? null : SkillManager.Instance.GetSkill(skillname);
            if (npc == null || skill == null) return;
            SkillBox box = skill.Boxs == null ? null : skill.Boxs.Find(x => x.Index == index);
            if (box == null) return;
            AttackBox attackBox = AttackHelperManager.Instance.GetCurrAttackHelper(npc, skill, level, box, index);
            AbilityScalingRegistry.Refresh(attackBox);
        }
    }

    [HarmonyPatch(typeof(AttackHelperManager), "Update")]
    internal static class PlayerAbilityDynamicBoxPatch
    {
        private static void Postfix(AttackHelperManager __instance)
        {
            AbilityScalingRegistry.ApplyDynamicScales(__instance);
        }
    }

    internal static class AbilityScalingRegistry
    {
        private sealed class ColliderBaseline
        {
            internal Vector3 Center;
            internal Vector3 Size;
            internal float Radius;
            internal float Height;
            internal Collider Collider;
        }

        private static readonly Dictionary<AttackBox, Vector3> NativeScales = new Dictionary<AttackBox, Vector3>();
        private static readonly Dictionary<AttackBox, List<ColliderBaseline>> NativeColliders = new Dictionary<AttackBox, List<ColliderBaseline>>();
        private static readonly System.Reflection.FieldInfo ShowListField = AccessTools.Field(typeof(AttackHelperManager), "m_ShowAttackList");

        internal static void Register(AttackBox attackBox)
        {
            if (attackBox == null || attackBox.transform == null) return;
            NativeScales[attackBox] = attackBox.transform.localScale;
            List<ColliderBaseline> baselines = new List<ColliderBaseline>();
            foreach (Collider collider in attackBox.GetComponentsInChildren<Collider>(true))
            {
                BoxCollider box = collider as BoxCollider;
                SphereCollider sphere = collider as SphereCollider;
                CapsuleCollider capsule = collider as CapsuleCollider;
                if (box != null) baselines.Add(new ColliderBaseline { Collider = collider, Center = box.center, Size = box.size });
                else if (sphere != null) baselines.Add(new ColliderBaseline { Collider = collider, Center = sphere.center, Radius = sphere.radius });
                else if (capsule != null) baselines.Add(new ColliderBaseline { Collider = collider, Center = capsule.center, Radius = capsule.radius, Height = capsule.height });
            }
            NativeColliders[attackBox] = baselines;
            Apply(attackBox);
        }

        internal static void Refresh(AttackBox attackBox)
        {
            if (attackBox == null) return;
            if (!NativeScales.ContainsKey(attackBox)) Register(attackBox);
            Apply(attackBox);
            if (Plugin.AbilityScalingDiagnostics.Value) LogActivation(attackBox);
        }

        internal static void ApplyDynamicScales(AttackHelperManager manager)
        {
            List<AttackBox> active = ShowListField == null ? null : ShowListField.GetValue(manager) as List<AttackBox>;
            if (active == null) return;
            foreach (AttackBox attackBox in active.ToList())
            {
                if (attackBox == null)
                {
                    NativeScales.Remove(attackBox);
                    continue;
                }
                if (!NativeScales.ContainsKey(attackBox)) Register(attackBox);
                Apply(attackBox);
            }
            foreach (AttackBox box in NativeScales.Keys.Where(x => x == null).ToList()) NativeScales.Remove(box);
        }

        private static void Apply(AttackBox attackBox)
        {
            Vector3 nativeScale;
            if (!NativeScales.TryGetValue(attackBox, out nativeScale)) return;
            bool player = Plugin.ScaleAbilityGameplayAreas.Value && Plugin.IsPlayer(attackBox.m_Ower);
            bool dynamic = attackBox.Size != null && attackBox.Size.BoxSizeDatas != null && attackBox.Size.BoxSizeDatas.Any(x => x != null && x.StartTimer <= Time.realtimeSinceStartup && x.EndTimer > Time.realtimeSinceStartup);
            if (!dynamic) attackBox.transform.localScale = nativeScale;
            List<ColliderBaseline> baselines;
            if (!player || !NativeColliders.TryGetValue(attackBox, out baselines)) return;
            float multiplier = Plugin.RuntimeAbilitySizeMultiplier;
            foreach (ColliderBaseline baseline in baselines)
            {
                BoxCollider box = baseline.Collider as BoxCollider;
                SphereCollider sphere = baseline.Collider as SphereCollider;
                CapsuleCollider capsule = baseline.Collider as CapsuleCollider;
                if (box != null) { box.center = baseline.Center; box.size = baseline.Size * multiplier; }
                else if (sphere != null) { sphere.center = baseline.Center; sphere.radius = baseline.Radius * multiplier; }
                else if (capsule != null) { capsule.center = baseline.Center; capsule.radius = baseline.Radius * multiplier; capsule.height = baseline.Height * multiplier; }
            }
        }

        private static void LogActivation(AttackBox attackBox)
        {
            List<ColliderBaseline> baselines = NativeColliders[attackBox];
            string dimensions = string.Empty;
            foreach (ColliderBaseline baseline in baselines)
            {
                BoxCollider box = baseline.Collider as BoxCollider;
                if (box != null) dimensions += " Box=" + baseline.Size + "->" + box.size;
                SphereCollider sphere = baseline.Collider as SphereCollider;
                if (sphere != null) dimensions += " Sphere=" + baseline.Radius.ToString("F2") + "->" + sphere.radius.ToString("F2");
                CapsuleCollider capsule = baseline.Collider as CapsuleCollider;
                if (capsule != null) dimensions += " Capsule=" + baseline.Height.ToString("F2") + "->" + capsule.height.ToString("F2");
            }
            Plugin.ModLogger.LogInfo("Ability scaling: Skill=" + attackBox.m_SkillName + " Owner=Player Mode=SkillBox SizeMultiplier=" + Plugin.RuntimeAbilitySizeMultiplier.ToString("F2") + " RangeMultiplier=" + Plugin.RuntimeAbilityRangeMultiplier.ToString("F2") + " NativeAnchor=" + attackBox.transform.position + " ScaledAnchor=" + attackBox.transform.position + " AnchorDelta=(0,0,0) NativeTarget=(n/a) ResolvedTarget=(n/a)" + dimensions + " VisualPath=SkillBoxCollider VisualScaled=n/a");
        }

        internal static void Forget(AttackBox attackBox)
        {
            if (attackBox != null) { NativeScales.Remove(attackBox); NativeColliders.Remove(attackBox); }
        }

        internal static void Clear()
        {
            NativeScales.Clear();
            NativeColliders.Clear();
        }
    }

    [HarmonyPatch(typeof(AttackHelperManager), nameof(AttackHelperManager.DestroyAttackHelperObj), new Type[] { typeof(AttackBox) })]
    internal static class PlayerAbilityDestroyedBoxPatch
    {
        private static void Prefix(AttackBox attobj) { AbilityScalingRegistry.Forget(attobj); }
    }

    [HarmonyPatch(typeof(AttackHelperManager), nameof(AttackHelperManager.RemoveAll), new Type[0])]
    internal static class PlayerAbilityRemoveAllPatch
    {
        private static void Postfix() { AbilityScalingRegistry.Clear(); }
    }

    [HarmonyPatch(typeof(BehaviorStateSkill), "SetFx")]
    internal static class PlayerSkillVisualPatch
    {
        private static readonly System.Reflection.FieldInfo OwnerField = AccessTools.Field(typeof(BehaviorState), "m_Owner");
        private static readonly System.Reflection.FieldInfo EffectsField = AccessTools.Field(typeof(BehaviorStateSkill), "m_SkillEffects");

        private static void Prefix(BehaviorStateSkill __instance, out int __state)
        {
            List<EffectObj> effects = EffectsField == null ? null : EffectsField.GetValue(__instance) as List<EffectObj>;
            __state = effects == null ? 0 : effects.Count;
        }

        private static void Postfix(BehaviorStateSkill __instance, int __state)
        {
            if (!Plugin.ScaleAbilityVisuals.Value || OwnerField == null || EffectsField == null) return;
            Thing owner = OwnerField.GetValue(__instance) as Thing;
            if (!Plugin.IsPlayer(owner)) return;
            List<EffectObj> effects = EffectsField.GetValue(__instance) as List<EffectObj>;
            if (effects == null) return;
            for (int i = Mathf.Clamp(__state, 0, effects.Count); i < effects.Count; i++)
            {
                EffectObj effect = effects[i];
                if (effect != null && effect.m_EffectObj != null)
                {
                    string mode = effect.m_FromType == 4 ? "CursorGround" : (effect.m_FromType == 10 ? "PlayerAttached" : (effect.m_FromType == 0 ? "PlayerAttached" : "Unsupported"));
                    AbilityVisualScaler.Apply(effect.m_EffectObj, Plugin.RuntimeAbilitySizeMultiplier, "BehaviorStateSkill.SetFx", effect.m_EffectObj.transform.position, owner, mode);
                }
            }
        }
    }

    internal static class AbilityVisualScaler
    {
        private sealed class Baseline
        {
            internal Vector3 Position;
            internal Vector3 Scale;
            internal TrailRenderer Trail;
            internal LineRenderer Line;
            internal float TrailStart;
            internal float TrailEnd;
            internal float LineStart;
            internal float LineEnd;
        }

        private static readonly Dictionary<Transform, Baseline> baselines = new Dictionary<Transform, Baseline>();

        internal static void Apply(GameObject root, float multiplier, string path, Vector3 anchor, Thing owner, string mode)
        {
            if (root == null || !Plugin.ScaleAbilityVisuals.Value || !Plugin.IsPlayer(owner)) return;
            List<Transform> candidates = new List<Transform>();
            AddTransforms(root.GetComponentsInChildren<ParticleSystem>(true), candidates);
            AddTransforms(root.GetComponentsInChildren<MeshRenderer>(true), candidates);
            AddTransforms(root.GetComponentsInChildren<SkinnedMeshRenderer>(true), candidates);
            AddTransforms(root.GetComponentsInChildren<SpriteRenderer>(true), candidates);
            AddTransforms(root.GetComponentsInChildren<TrailRenderer>(true), candidates);
            AddTransforms(root.GetComponentsInChildren<LineRenderer>(true), candidates);
            List<Transform> leaves = candidates.Distinct().Where(x => !candidates.Any(y => y != x && y.IsChildOf(x))).ToList();
            Dictionary<Transform, Vector3> nativePositions = new Dictionary<Transform, Vector3>();
            foreach (Transform transform in leaves)
            {
                Baseline baseline;
                if (!baselines.TryGetValue(transform, out baseline))
                {
                    baseline = new Baseline { Position = transform.localPosition, Scale = transform.localScale, Trail = transform.GetComponent<TrailRenderer>(), Line = transform.GetComponent<LineRenderer>() };
                    if (baseline.Trail != null) { baseline.TrailStart = baseline.Trail.startWidth; baseline.TrailEnd = baseline.Trail.endWidth; }
                    if (baseline.Line != null) { baseline.LineStart = baseline.Line.startWidth; baseline.LineEnd = baseline.Line.endWidth; }
                    baselines[transform] = baseline;
                }
                transform.localPosition = baseline.Position;
                nativePositions[transform] = transform.position;
                transform.localScale = baseline.Scale * multiplier;
                if (baseline.Trail != null) { baseline.Trail.startWidth = baseline.TrailStart * multiplier; baseline.Trail.endWidth = baseline.TrailEnd * multiplier; }
                if (baseline.Line != null) { baseline.Line.startWidth = baseline.LineStart * multiplier; baseline.Line.endWidth = baseline.LineEnd * multiplier; }
            }
            float downwardGrowth;
            float lift = 0f;
            if (mode == "PlayerAttached" || mode == "ForwardSkill")
            {
                Bounds nativeBounds;
                Bounds scaledBounds;
                foreach (Transform transform in leaves) transform.localScale = baselines[transform].Scale;
                if (TryGetReliableBounds(root, out nativeBounds))
                {
                    foreach (Transform transform in leaves) transform.localScale = baselines[transform].Scale * multiplier;
                    if (TryGetReliableBounds(root, out scaledBounds))
                    {
                        float originalBottomY = nativeBounds.min.y;
                        downwardGrowth = Mathf.Max(0f, originalBottomY - scaledBounds.min.y);
                        lift = Mathf.Min(downwardGrowth * Plugin.AbilityVerticalCompensation.Value, 1f);
                        foreach (Transform transform in leaves)
                        {
                            if (transform != root) transform.position = nativePositions[transform] + Vector3.up * lift;
                        }
                        if (Plugin.AbilityScalingDiagnostics.Value)
                            Plugin.ModLogger.LogInfo("VisualVertical: Skill=(visual) Mode=" + mode + " NativeBottomY=" + originalBottomY.ToString("F3") + " ScaledBottomY=" + scaledBounds.min.y.ToString("F3") + " DownwardGrowth=" + downwardGrowth.ToString("F3") + " CompensationFactor=" + Plugin.AbilityVerticalCompensation.Value.ToString("F2") + " AppliedLift=" + lift.ToString("F3") + " NativeVisualY=" + nativePositions[leaves[0]].y.ToString("F3") + " FinalVisualY=" + (nativePositions[leaves[0]].y + lift).ToString("F3"));
                    }
                }
            }
            if (Plugin.AbilityScalingDiagnostics.Value)
                Plugin.ModLogger.LogInfo("Ability scaling: Skill=(visual) Owner=Player Mode=Visual SizeMultiplier=" + multiplier.ToString("F2") + " NativeAnchor=" + anchor + " ScaledAnchor=" + root.transform.position + " AnchorDelta=" + (root.transform.position - anchor) + " NativeTarget=(n/a) ResolvedTarget=(n/a) VisualPath=" + path + " VisualScaled=" + (leaves.Count > 0));
        }

        private static bool TryGetReliableBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(x => !(x is TrailRenderer) && !(x is LineRenderer)).ToArray();
            if (renderers.Length == 0) { bounds = new Bounds(); return false; }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static void AddTransforms<T>(T[] components, List<Transform> transforms) where T : Component
        {
            foreach (T component in components) if (component != null && component.transform != null) transforms.Add(component.transform);
        }
    }

    [HarmonyPatch(typeof(BulletEffect), "Initialized")]
    internal static class PlayerProjectileVisualPatch
    {
        private static readonly System.Reflection.FieldInfo IndicatorField = AccessTools.Field(typeof(BulletEffect), "m_Indicator");

        private static void Postfix(BulletEffect __instance)
        {
            if (__instance == null || !Plugin.ScaleAbilityVisuals.Value || !Plugin.IsPlayer(__instance.m_Owner)) return;
            Vector3 anchor = __instance.transform.position;
            AbilityVisualScaler.Apply(__instance.gameObject, Plugin.RuntimeAbilitySizeMultiplier, "BulletEffect.Initialized", anchor, __instance.m_Owner, "Projectile");
            GameObject indicator = IndicatorField == null ? null : IndicatorField.GetValue(__instance) as GameObject;
            if (indicator != null) AbilityVisualScaler.Apply(indicator, Plugin.RuntimeAbilitySizeMultiplier, "BulletEffect.Indicator", indicator.transform.position, __instance.m_Owner, "Projectile");
        }
    }

    [HarmonyPatch(typeof(BehaviorStateSkill), "Search")]
    internal static class PlayerSkillSearchRangePatch
    {
        private static readonly System.Reflection.FieldInfo OwnerField = AccessTools.Field(typeof(BehaviorState), "m_Owner");
        private static readonly System.Reflection.FieldInfo SkillField = AccessTools.Field(typeof(BehaviorStateSkill), "m_Curskill");

        private static void Prefix(BehaviorStateSkill __instance, out SearchRangeState __state)
        {
            __state = null;
            if (Mathf.Approximately(Plugin.RuntimeAbilityRangeMultiplier, 1f) || OwnerField == null || SkillField == null) return;
            Thing owner = OwnerField.GetValue(__instance) as Thing;
            Skill skill = SkillField.GetValue(__instance) as Skill;
            if (!Plugin.IsPlayer(owner) || skill == null || skill.Search == null) return;
            __state = new SearchRangeState { Search = skill.Search, Min = skill.Search.MinDistance, Max = skill.Search.MaxDistance };
            skill.Search.MinDistance *= Plugin.RuntimeAbilityRangeMultiplier;
            skill.Search.MaxDistance *= Plugin.RuntimeAbilityRangeMultiplier;
        }

        private static Exception Finalizer(Exception __exception, SearchRangeState __state)
        {
            if (__state != null)
            {
                __state.Search.MinDistance = __state.Min;
                __state.Search.MaxDistance = __state.Max;
            }
            return __exception;
        }

        private sealed class SearchRangeState
        {
            internal SkillSearch Search;
            internal float Min;
            internal float Max;
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.SetSkillBullet), new Type[] { typeof(Skill), typeof(SkillBullet), typeof(int), typeof(Vector3), typeof(bool), typeof(List<SkillAudio>), typeof(Skill) })]
    internal static class PlayerProjectileRangePatch
    {
        private static void Prefix(FightBody __instance, SkillBullet skillbullet, out PlayerProjectileRangeState __state)
        {
            __state = null;
            if (Mathf.Approximately(Plugin.RuntimeAbilityRangeMultiplier, 1f) || __instance == null || !Plugin.IsPlayer(__instance.m_Owner) || skillbullet == null) return;
            __state = new PlayerProjectileRangeState { Bullet = skillbullet, Min = skillbullet.MinDistance, Max = skillbullet.MaxDistance };
            skillbullet.MinDistance *= Plugin.RuntimeAbilityRangeMultiplier;
            skillbullet.MaxDistance *= Plugin.RuntimeAbilityRangeMultiplier;
        }

        private static Exception Finalizer(Exception __exception, PlayerProjectileRangeState __state)
        {
            if (__state != null)
            {
                __state.Bullet.MinDistance = __state.Min;
                __state.Bullet.MaxDistance = __state.Max;
            }
            return __exception;
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.SetSkillBullet), new Type[] { typeof(Skill), typeof(SkillBullet), typeof(int), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(List<SkillAudio>), typeof(bool), typeof(Skill) })]
    internal static class PlayerProjectileRangeExplicitPatch
    {
        private static void Prefix(FightBody __instance, SkillBullet skillbullet, out PlayerProjectileRangeState __state)
        {
            __state = null;
            if (Mathf.Approximately(Plugin.RuntimeAbilityRangeMultiplier, 1f) || __instance == null || !Plugin.IsPlayer(__instance.m_Owner) || skillbullet == null) return;
            __state = new PlayerProjectileRangeState { Bullet = skillbullet, Min = skillbullet.MinDistance, Max = skillbullet.MaxDistance };
            skillbullet.MinDistance *= Plugin.RuntimeAbilityRangeMultiplier;
            skillbullet.MaxDistance *= Plugin.RuntimeAbilityRangeMultiplier;
        }

        private static Exception Finalizer(Exception __exception, PlayerProjectileRangeState __state)
        {
            if (__state != null)
            {
                __state.Bullet.MinDistance = __state.Min;
                __state.Bullet.MaxDistance = __state.Max;
            }
            return __exception;
        }

    }

    internal sealed class PlayerProjectileRangeState
    {
        internal SkillBullet Bullet;
        internal float Min;
        internal float Max;
    }

    internal static class PlayerMultishot
    {
        private sealed class PendingProjectile
        {
            internal Thing Owner;
            internal Skill Skill;
            internal SkillBullet Bullet;
            internal int Level;
            internal List<SkillAudio> Audios;
            internal Skill ZhenFaSkill;
            internal Vector3 Start;
            internal Vector3 End;
            internal Vector3 Rotation;
            internal float MoveTime;
            internal float AcceleratedSpeed;
            internal float FireAt;
            internal int SceneHandle;
        }

        private static readonly List<PendingProjectile> Pending = new List<PendingProjectile>();
        private static int pendingSceneHandle = int.MinValue;

        [ThreadStatic]
        private static int spawnDepth;

        internal static void Update()
        {
            int sceneHandle = SceneManager.GetActiveScene().handle;
            if (pendingSceneHandle != sceneHandle)
            {
                Pending.Clear();
                pendingSceneHandle = sceneHandle;
                return;
            }
            float now = Time.time;
            for (int i = Pending.Count - 1; i >= 0; i--)
            {
                PendingProjectile pending = Pending[i];
                if (pending == null || pending.SceneHandle != sceneHandle || pending.Owner == null || BulletManager.Instance == null || GameWatch.Instance == null)
                {
                    Pending.RemoveAt(i);
                    continue;
                }
                if (now < pending.FireAt) continue;
                Pending.RemoveAt(i);
                spawnDepth++;
                try
                {
                    AttackHelperManager.Instance.TriggerSkillBullet(pending.Owner.m_ID, pending.Skill, pending.Bullet, pending.Bullet.Mirror == 0 ? pending.Start : pending.End, pending.Bullet.Mirror == 0 ? pending.End : pending.Start, pending.Rotation, pending.MoveTime, pending.Level, pending.AcceleratedSpeed, pending.Audios, pending.ZhenFaSkill);
                }
                finally
                {
                    spawnDepth--;
                }
            }
        }

        internal static void Duplicate(Skill skill, SkillBullet bullet, int level, List<SkillAudio> audios, Skill zhenfaSkill, BulletEffect native)
        {
            if (native == null || bullet == null || skill == null || spawnDepth != 0) return;
            int count = Mathf.Clamp(Plugin.ProjectileCountMultiplier.Value, 1, 10);
            if (count <= 1)
            {
                if (Plugin.AbilityScalingDiagnostics.Value)
                    Plugin.ModLogger.LogInfo("Multishot: Skill=" + skill.Name + " Owner=Player Projectile=" + bullet.Name + " NativeCount=1 Multiplier=1 AdditionalSpawned=0 Spawn=" + native.m_StartPos + " Target=" + native.m_EndPos + " Direction=" + (native.m_EndPos - native.m_StartPos).normalized + " SpreadSupported=False SpreadAngles=[] Homing=" + (bullet.TrackType != BulletTrackType.None) + " Range=" + Vector3.Distance(native.m_StartPos, native.m_EndPos).ToString("F2") + " SizeMultiplier=" + Plugin.RuntimeAbilitySizeMultiplier.ToString("F2"));
                return;
            }
            if (bullet.TrackType != BulletTrackType.None || native.m_MoveTime <= 0f)
            {
                LogSkipped(skill, bullet, bullet.TrackType != BulletTrackType.None ? "HomingOrTracked" : "BeamLikeOrStationary");
                return;
            }

            Vector3 actualStart = native.m_StartPos;
            Vector3 actualEnd = native.m_EndPos;
            Vector3 travel = actualEnd - actualStart;
            Vector3 horizontal = new Vector3(travel.x, 0f, travel.z);
            bool spreadSupported = horizontal.sqrMagnitude > 0.0001f;
            List<float> angles = new List<float>();
            for (int i = 0; i < count; i++)
            {
                float angle = (i - (count - 1) * 0.5f) * 5f;
                if (Mathf.Abs(angle) > 0.001f) angles.Add(angle);
            }
            if (angles.Count < count - 1) angles.Add(5f);
            List<float> usedAngles = new List<float>();
            float delay = Mathf.Clamp(Plugin.MultishotDelaySeconds.Value, 0f, 0.1f);
            float nativeFireTime = Time.time;
            if (pendingSceneHandle == int.MinValue) pendingSceneHandle = SceneManager.GetActiveScene().handle;
            for (int i = 0; i < count - 1; i++)
            {
                float angle = spreadSupported ? angles[i] : 0f;
                Vector3 end = actualEnd;
                if (spreadSupported) end = actualStart + Quaternion.AngleAxis(angle, Vector3.up) * horizontal + Vector3.up * travel.y;
                usedAngles.Add(angle);
                if (delay <= 0f)
                {
                    Spawn(native.m_Owner, skill, bullet, level, audios, zhenfaSkill, actualStart, end, native.transform.eulerAngles, native.m_MoveTime, native.m_AcceleratedSpeed);
                }
                else
                {
                    Pending.Add(new PendingProjectile { Owner = native.m_Owner, Skill = skill, Bullet = bullet, Level = level, Audios = audios, ZhenFaSkill = zhenfaSkill, Start = actualStart, End = end, Rotation = native.transform.eulerAngles, MoveTime = native.m_MoveTime, AcceleratedSpeed = native.m_AcceleratedSpeed, FireAt = nativeFireTime + delay * (i + 1), SceneHandle = SceneManager.GetActiveScene().handle });
                }
            }
            if (Plugin.AbilityScalingDiagnostics.Value)
                Plugin.ModLogger.LogInfo("Multishot: Skill=" + skill.Name + " Owner=Player Projectile=" + bullet.Name + " NativeCount=1 Multiplier=" + count + " DelayMs=" + Mathf.RoundToInt(delay * 1000f) + " NativeFireTime=" + nativeFireTime.ToString("F3") + " QueuedExtras=" + (count - 1) + " ScheduledOffsetsMs=[" + string.Join(",", Enumerable.Range(1, count - 1).Select(x => Mathf.RoundToInt(delay * x * 1000f).ToString()).ToArray()) + "] Spawn=" + actualStart + " Target=" + actualEnd + " Direction=" + travel.normalized + " SpreadSupported=" + spreadSupported + " SpreadAngles=[" + string.Join(",", usedAngles.Select(x => x.ToString("F1")).ToArray()) + "] Homing=False Range=" + Vector3.Distance(actualStart, actualEnd).ToString("F2") + " SizeMultiplier=" + Plugin.RuntimeAbilitySizeMultiplier.ToString("F2"));
        }

        private static void Spawn(Thing owner, Skill skill, SkillBullet bullet, int level, List<SkillAudio> audios, Skill zhenfaSkill, Vector3 actualStart, Vector3 actualEnd, Vector3 rotation, float moveTime, float acceleratedSpeed)
        {
            spawnDepth++;
            try
            {
                AttackHelperManager.Instance.TriggerSkillBullet(owner.m_ID, skill, bullet, bullet.Mirror == 0 ? actualStart : actualEnd, bullet.Mirror == 0 ? actualEnd : actualStart, rotation, moveTime, level, acceleratedSpeed, audios, zhenfaSkill);
            }
            finally
            {
                spawnDepth--;
            }
        }

        private static void LogSkipped(Skill skill, SkillBullet bullet, string reason)
        {
            if (Plugin.AbilityScalingDiagnostics.Value)
                Plugin.ModLogger.LogInfo("Multishot skipped: Skill=" + skill.Name + " Projectile=" + bullet.Name + " Reason=" + reason);
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.SetSkillBullet), new Type[] { typeof(Skill), typeof(SkillBullet), typeof(int), typeof(Vector3), typeof(bool), typeof(List<SkillAudio>), typeof(Skill) })]
    internal static class PlayerMultishotPatch
    {
        private static void Postfix(Skill skill, SkillBullet skillbullet, int level, List<SkillAudio> audios, Skill zhenfaSkill, BulletEffect __result)
        {
            if (__result == null || __result.m_Owner == null || !Plugin.IsPlayer(__result.m_Owner)) return;
            PlayerMultishot.Duplicate(skill, skillbullet, level, audios, zhenfaSkill, __result);
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.SetSkillBullet), new Type[] { typeof(Skill), typeof(SkillBullet), typeof(int), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(List<SkillAudio>), typeof(bool), typeof(Skill) })]
    internal static class PlayerExplicitMultishotPatch
    {
        private static void Postfix(Skill skill, SkillBullet skillbullet, int level, List<SkillAudio> audios, Skill zhenfaSkill, BulletEffect __result)
        {
            if (__result == null || __result.m_Owner == null || !Plugin.IsPlayer(__result.m_Owner)) return;
            PlayerMultishot.Duplicate(skill, skillbullet, level, audios, zhenfaSkill, __result);
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.SetSkillBullet), new Type[] { typeof(Skill), typeof(SkillBullet), typeof(int), typeof(Vector3), typeof(bool), typeof(List<SkillAudio>), typeof(Skill) })]
    internal static class PlayerProjectileTargetDiagnostics
    {
        private static void Postfix(FightBody __instance, Vector3 targetpos, BulletEffect __result)
        {
            if (!Plugin.AbilityScalingDiagnostics.Value || __instance == null || !Plugin.IsPlayer(__instance.m_Owner) || __result == null) return;
            Plugin.ModLogger.LogInfo("Ability scaling: Skill=" + (__result.m_Skill == null ? "(unknown)" : __result.m_Skill.Name) + " Owner=Player Mode=CursorOrResolved SizeMultiplier=" + Plugin.RuntimeAbilitySizeMultiplier.ToString("F2") + " RangeMultiplier=" + Plugin.RuntimeAbilityRangeMultiplier.ToString("F2") + " NativeAnchor=" + __result.m_StartPos + " ScaledAnchor=" + __result.m_StartPos + " AnchorDelta=(0,0,0) NativeTarget=" + targetpos + " ResolvedTarget=" + __result.m_EndPos + " VisualPath=BulletEffect.Initialized VisualScaled=" + Plugin.ScaleAbilityVisuals.Value);
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.SetSkillBullet), new Type[] { typeof(Skill), typeof(SkillBullet), typeof(int), typeof(Vector3), typeof(Vector3), typeof(bool), typeof(List<SkillAudio>), typeof(bool), typeof(Skill) })]
    internal static class PlayerExplicitProjectileTargetDiagnostics
    {
        private static void Postfix(FightBody __instance, Vector3 targetpos, Vector3 startpos, BulletEffect __result)
        {
            if (!Plugin.AbilityScalingDiagnostics.Value || __instance == null || !Plugin.IsPlayer(__instance.m_Owner) || __result == null) return;
            Plugin.ModLogger.LogInfo("Ability scaling: Skill=" + (__result.m_Skill == null ? "(unknown)" : __result.m_Skill.Name) + " Owner=Player Mode=ExplicitTarget SizeMultiplier=" + Plugin.RuntimeAbilitySizeMultiplier.ToString("F2") + " RangeMultiplier=" + Plugin.RuntimeAbilityRangeMultiplier.ToString("F2") + " NativeAnchor=" + startpos + " ScaledAnchor=" + __result.m_StartPos + " AnchorDelta=" + (__result.m_StartPos - startpos) + " NativeTarget=" + targetpos + " ResolvedTarget=" + __result.m_EndPos + " VisualPath=BulletEffect.Initialized VisualScaled=" + Plugin.ScaleAbilityVisuals.Value);
        }
    }

    [HarmonyPatch(typeof(MapMain), nameof(MapMain.CreaMonster))]
    internal static class EnemyDensityPatch
    {
        private static readonly System.Reflection.FieldInfo RulesField = AccessTools.Field(typeof(MapMain), "m_MonsterBrushRules");
        private static readonly System.Reflection.FieldInfo CurrentRoomField = AccessTools.Field(typeof(MapMain), "_currRoom");
        private static readonly System.Reflection.FieldInfo WaveField = AccessTools.Field(typeof(MapMain), "creaMonsterIndex");

        private static void Postfix(MapMain __instance)
        {
            int multiplier = Mathf.Clamp(Plugin.EnemyDensityMultiplier.Value, 1, 15);
            if (multiplier <= 1 || __instance == null || RulesField == null) return;
            List<MonsterBrushRuleData> rules = RulesField.GetValue(__instance) as List<MonsterBrushRuleData>;
            RoomConfig room = CurrentRoomField == null ? null : CurrentRoomField.GetValue(__instance) as RoomConfig;
            if (rules == null || room == null || room.roomType == RoomType.Boss || room.roomType == RoomType.Elite || room.roomType == RoomType.Special || room.roomType == RoomType.Shop || __instance.GetIsPlotPlaying()) return;

            List<MonsterBrushRuleData> nativeRules = new List<MonsterBrushRuleData>(rules);
            int extrasRequested = 0;
            int extrasAdded = 0;
            foreach (MonsterBrushRuleData rule in nativeRules)
            {
                string reason;
                if (!IsRegularRule(rule, out reason))
                {
                    if (Plugin.EnemyDiagnostics.Value && (reason == "BossExcluded" || reason == "EliteExcluded"))
                        Plugin.ModLogger.LogInfo("Enemy density skipped: Enemy=" + RuleName(rule) + " Category=" + reason.Replace("Excluded", string.Empty) + " Reason=" + reason);
                    continue;
                }
                extrasRequested += multiplier - 1;
                for (int i = 1; i < multiplier; i++)
                {
                    rules.Add(rule);
                    extrasAdded++;
                }
            }
            if (Plugin.EnemyDiagnostics.Value && extrasRequested > 0)
            {
                string wave = WaveField == null ? "n/a" : WaveField.GetValue(__instance).ToString();
                Plugin.ModLogger.LogInfo("Enemy density: Enemy=(native rules) Category=Regular NativeSpawn=(native rule dispatch) Multiplier=" + multiplier + " ExtrasRequested=" + extrasRequested + " ExtrasSpawned=" + extrasAdded + " Encounter=" + __instance.m_RoomName + " Wave=" + wave + " RegisteredWithEncounter=True");
            }
        }

        private static bool IsRegularRule(MonsterBrushRuleData rule, out string reason)
        {
            reason = "UnknownPool";
            if (rule == null || rule.MonsterPools == null) return false;
            if (rule.MonsterPools.IsBoos == 1) { reason = "BossExcluded"; return false; }
            MonsterPools pools = rule.MonsterPools.GetMonsterPools(CtrlManager.Instance == null ? 0 : CtrlManager.Instance.M_leveDifficulty);
            if (pools == null || string.IsNullOrEmpty(pools.MonsterPool) || PoolManager.Instance == null || NpcManager.Instance == null) return false;
            PoolData data = PoolManager.Instance.GetPoolData(pools.MonsterPool);
            if (data == null || data.Randoms == null || data.Randoms.Items == null || data.Randoms.Items.Count == 0) return false;
            foreach (PoolItem item in data.Randoms.Items)
            {
                NpcUnit unit = item == null ? null : NpcManager.Instance.GetUnitData(item.Name);
                if (unit == null) return false;
                if (unit.Rank == UnitRank.Boss) { reason = "BossExcluded"; return false; }
                if (unit.Rank == UnitRank.Elite) { reason = "EliteExcluded"; return false; }
                if (unit.Rank != UnitRank.None) return false;
            }
            reason = string.Empty;
            return true;
        }

        private static string RuleName(MonsterBrushRuleData rule)
        {
            return rule == null || rule.MonsterPools == null ? "(unknown)" : rule.MonsterPools.MonsterPool;
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.SetSkillBox), new Type[] { typeof(string), typeof(int), typeof(int) })]
    internal static class PlayerProjectileCoverageDiagnostics
    {
        private static void Postfix(FightBody __instance, string skillname, int index)
        {
            if (!Plugin.AbilityScalingDiagnostics.Value || __instance == null || !Plugin.IsPlayer(__instance.m_Owner)) return;
            Plugin.ModLogger.LogInfo("Projectile coverage: Skill=" + skillname + " ExecutionPath=SetSkillBox Classification=PlayerAttachedHitbox Supported=False Reason=NotProjectile Index=" + index);
        }
    }

    internal enum EnemyDifficultyRank
    {
        Regular,
        Elite,
        Boss,
        Unsupported
    }

    internal static class EnemyDifficultySystem
    {
        private sealed class Baseline
        {
            public float MaxHP;
            public float FillHP;
            public float Attack;
            public float Speed;
        }

        private static readonly Dictionary<int, Baseline> Baselines = new Dictionary<int, Baseline>();

        internal static EnemyDifficultyRank GetRank(Thing thing)
        {
            Npc npc = thing as Npc;
            if (npc == null || npc.m_Unit == null || Plugin.IsPlayer(npc)) return EnemyDifficultyRank.Unsupported;
            if (npc.m_ThingAttribute == null || CtrlManager.Instance == null || CtrlManager.Instance.CtrlNpc == null) return EnemyDifficultyRank.Unsupported;
            if (npc.m_ThingAttribute.Team == CtrlManager.Instance.CtrlNpc.m_ThingAttribute.Team) return EnemyDifficultyRank.Unsupported;
            if (npc.m_Unit.Rank == UnitRank.Boss) return EnemyDifficultyRank.Boss;
            if (npc.m_Unit.Rank == UnitRank.Elite) return EnemyDifficultyRank.Elite;
            if (npc.m_Unit.Rank == UnitRank.None) return EnemyDifficultyRank.Regular;
            return EnemyDifficultyRank.Unsupported;
        }

        internal static float HealthMultiplier(EnemyDifficultyRank rank)
        {
            if (rank == EnemyDifficultyRank.Elite) return Plugin.EliteHealthMultiplier.Value;
            if (rank == EnemyDifficultyRank.Boss) return Plugin.BossHealthMultiplier.Value;
            return Plugin.RegularEnemyHealthMultiplier.Value;
        }

        internal static float DamageMultiplier(EnemyDifficultyRank rank)
        {
            if (rank == EnemyDifficultyRank.Elite) return Plugin.EliteDamageMultiplier.Value;
            if (rank == EnemyDifficultyRank.Boss) return Plugin.BossDamageMultiplier.Value;
            return Plugin.RegularEnemyDamageMultiplier.Value;
        }

        internal static void Initialize(Npc npc)
        {
            EnemyDifficultyRank rank = GetRank(npc);
            if (rank == EnemyDifficultyRank.Unsupported) return;
            PropertyData hp = npc.m_ThingAttribute.GetProperty(AttributeName.HP);
            Baseline baseline = new Baseline { MaxHP = hp.GetValue(), FillHP = hp._FillValue, Attack = npc.m_ThingAttribute.GetProperty(AttributeName.Attack).GetValue(), Speed = npc.m_ThingAttribute.GetProperty(AttributeName.Speed).GetValue() };
            Baselines[npc.m_ID] = baseline;
            ApplyHealth(npc, baseline, rank);
            if (Plugin.EnemyDifficultyDiagnostics.Value)
                Plugin.ModLogger.LogInfo("Enemy difficulty: Npc=" + npc.m_ID + " Rank=" + rank + " NativeMaxHP=" + baseline.MaxHP.ToString("F2") + " AppliedMaxHP=" + hp.GetValue().ToString("F2") + " HealthMultiplier=" + HealthMultiplier(rank).ToString("F2") + " DamageMultiplier=" + DamageMultiplier(rank).ToString("F2") + " NativeMoveSpeed=" + baseline.Speed.ToString("F2") + " AppliedMoveSpeed=" + (baseline.Speed * (rank == EnemyDifficultyRank.Regular ? Plugin.RegularEnemyMovementSpeedMultiplier.Value : 1f)).ToString("F2"));
        }

        private static void ApplyHealth(Npc npc, Baseline baseline, EnemyDifficultyRank rank)
        {
            PropertyData hp = npc.m_ThingAttribute.GetProperty(AttributeName.HP);
            float fraction = baseline.MaxHP <= 0f ? 1f : baseline.FillHP / baseline.MaxHP;
            hp._Value = baseline.MaxHP * HealthMultiplier(rank);
            hp._FillValue = hp.GetValue() * Mathf.Clamp01(fraction);
        }

        internal static void Update()
        {
            ClampLiveSettings();
        }

        private static void ClampLiveSettings()
        {
            if (Plugin.RegularEnemyHealthMultiplier == null) return;
            Plugin.RegularEnemyHealthMultiplier.Value = Mathf.Clamp(Plugin.RegularEnemyHealthMultiplier.Value, 0.25f, 20f);
            Plugin.RegularEnemyDamageMultiplier.Value = Mathf.Clamp(Plugin.RegularEnemyDamageMultiplier.Value, 0.25f, 10f);
            Plugin.RegularEnemyMovementSpeedMultiplier.Value = Mathf.Clamp(Plugin.RegularEnemyMovementSpeedMultiplier.Value, 0.5f, 3f);
            Plugin.EliteHealthMultiplier.Value = Mathf.Clamp(Plugin.EliteHealthMultiplier.Value, 0.25f, 20f);
            Plugin.EliteDamageMultiplier.Value = Mathf.Clamp(Plugin.EliteDamageMultiplier.Value, 0.25f, 10f);
            Plugin.BossHealthMultiplier.Value = Mathf.Clamp(Plugin.BossHealthMultiplier.Value, 0.25f, 20f);
            Plugin.BossDamageMultiplier.Value = Mathf.Clamp(Plugin.BossDamageMultiplier.Value, 0.25f, 10f);
        }

        internal static bool TryGetDamageMultiplier(Thing attacker, out float multiplier, out EnemyDifficultyRank rank)
        {
            rank = GetRank(attacker);
            multiplier = rank == EnemyDifficultyRank.Unsupported ? 1f : DamageMultiplier(rank);
            return rank != EnemyDifficultyRank.Unsupported;
        }
    }

    [HarmonyPatch(typeof(Npc), "OnInit")]
    internal static class EnemyDifficultyNpcPatch
    {
        private static void Postfix(Npc __instance) { EnemyDifficultySystem.Initialize(__instance); CharacterSizeSystem.Apply(__instance); }
    }

    [HarmonyPatch(typeof(Npc), nameof(Npc.OnDestroy))]
    internal static class CharacterSizeDestroyPatch
    {
        private static void Postfix(Npc __instance) { CharacterSizeSystem.Remove(__instance); }
    }

    [HarmonyPatch(typeof(MonsterBasicAI), nameof(MonsterBasicAI.Init))]
    internal static class EnemyDifficultyMovementPatch
    {
        private static void Postfix(MonsterBasicAI __instance, Npc npc)
        {
            if (__instance == null || npc == null || EnemyDifficultySystem.GetRank(npc) != EnemyDifficultyRank.Regular) return;
            object authoring = AccessTools.Field(typeof(MonsterBasicAI), "m_AgentAuthoring").GetValue(__instance);
            if (authoring == null) return;
            System.Reflection.PropertyInfo steeringProperty = AccessTools.Property(authoring.GetType(), "EntitySteering");
            if (steeringProperty == null) return;
            object steering = steeringProperty.GetValue(authoring, null);
            System.Reflection.FieldInfo speed = AccessTools.Field(steering.GetType(), "Speed");
            if (speed == null) return;
            speed.SetValue(steering, npc.m_Attribute.MoveSpeed * Plugin.RegularEnemyMovementSpeedMultiplier.Value);
            steeringProperty.SetValue(authoring, steering, null);
        }
    }

    [HarmonyPatch(typeof(FightBody), nameof(FightBody.CalculationDamage))]
    internal static class EnemyDifficultyDamagePatch
    {
        private static void Postfix(Thing fromthing, ref float __result)
        {
            float multiplier;
            EnemyDifficultyRank rank;
            if (!EnemyDifficultySystem.TryGetDamageMultiplier(fromthing, out multiplier, out rank) || __result <= 0f) return;
            float native = __result;
            __result *= multiplier;
            if (Plugin.EnemyDifficultyDiagnostics.Value)
                Plugin.ModLogger.LogInfo("Enemy damage: Attacker=" + fromthing.m_ID + " Rank=" + rank + " NativeDamage=" + native.ToString("F2") + " Multiplier=" + multiplier.ToString("F2") + " FinalDamage=" + __result.ToString("F2"));
        }
    }

    internal static class CharacterSizeSystem
    {
        private sealed class State { public Npc Npc; public Transform View; public Transform Root; public Vector3 Scale; public Vector3 Position; public float LastMultiplier = -1f; public int RendererCount; public float RetryUntil; public float NextRetry; public bool FailureReported; }
        private static readonly Dictionary<int, State> States = new Dictionary<int, State>();
        private static readonly HashSet<string> HierarchyReports = new HashSet<string>();
        private static readonly HashSet<string> ResolutionReports = new HashSet<string>();
        private static int sceneHandle = int.MinValue;

        internal static void Apply(Npc npc)
        {
            if (npc == null || npc.m_View == null || npc.m_View.ViewShow == null) return;
            Transform view = npc.m_View.ViewShow;
            State state;
            if (!States.TryGetValue(npc.m_ID, out state) || state.View != view) { state = new State { Npc = npc, View = view, RetryUntil = Time.unscaledTime + 3f, NextRetry = 0f }; States[npc.m_ID] = state; }
            Transform visual = state.Root;
            if (visual == null || visual == null || !visual.gameObject.activeInHierarchy)
            {
                if (state.FailureReported) return;
                if (Time.unscaledTime < state.NextRetry) return;
                visual = ResolveVisualRoot(view);
                state.NextRetry = Time.unscaledTime + 0.15f;
                if (visual == null)
                {
                    if (Time.unscaledTime < state.RetryUntil) return;
                    string reportKey = view.name + ":" + view.childCount;
                    if (!state.FailureReported && Plugin.CharacterSizeDiagnostics.Value && HierarchyReports.Add(reportKey)) LogHierarchy(npc, view);
                    state.FailureReported = true;
                    return;
                }
                state.Root = visual;
                state.Scale = visual.localScale;
                state.Position = visual.localPosition;
                state.RendererCount = view.GetComponentsInChildren<Renderer>(true).Length;
                state.LastMultiplier = -1f;
            }
            float multiplier = npc.IsPlayerNpc ? Plugin.PlayerCharacterSizeMultiplier.Value : SizeMultiplier(EnemyDifficultySystem.GetRank(npc));
            if (state.Root != null && Mathf.Approximately(state.LastMultiplier, multiplier)) return;
            int rendererCount = view.GetComponentsInChildren<Renderer>(true).Length;
            if (rendererCount != state.RendererCount) { state.Root = null; state.RendererCount = rendererCount; return; }
            visual.localScale = state.Scale;
            visual.localPosition = state.Position;
            Bounds nativeBounds;
            Bounds scaledBounds = default(Bounds);
            bool hasBounds = TryGetBounds(visual, out nativeBounds);
            visual.localScale = state.Scale * multiplier;
            hasBounds = hasBounds && TryGetBounds(visual, out scaledBounds);
            float lift = 0f;
            if (hasBounds)
            {
                lift = nativeBounds.min.y - scaledBounds.min.y;
                float parentScaleY = visual.parent == null ? 1f : Mathf.Abs(visual.parent.lossyScale.y);
                visual.localPosition = state.Position + Vector3.up * (lift / Mathf.Max(parentScaleY, 0.0001f));
            }
            else visual.localPosition = state.Position;
            string resolutionKey = visual.name + ":" + rendererCount;
            if (Plugin.CharacterSizeDiagnostics.Value && !Mathf.Approximately(state.LastMultiplier, multiplier) && ResolutionReports.Add(resolutionKey))
                Plugin.ModLogger.LogInfo("Character size: Npc=" + npc.m_ID + " Category=" + (npc.IsPlayerNpc ? "Player" : EnemyDifficultySystem.GetRank(npc).ToString()) + " VisualRoot=" + GetPath(visual) + " NativeScale=" + state.Scale + " Multiplier=" + multiplier.ToString("F2") + " AppliedScale=" + visual.localScale + " NativeBottomY=" + (hasBounds ? nativeBounds.min.y.ToString("F2") : "n/a") + " ScaledBottomY=" + (hasBounds ? scaledBounds.min.y.ToString("F2") : "n/a") + " VisualLift=" + lift.ToString("F2") + " GameplayRootScale=" + npc.m_View.transform.localScale + " ColliderChanged=False NavMeshChanged=False");
            state.LastMultiplier = multiplier;
        }

        private static float SizeMultiplier(EnemyDifficultyRank rank) { if (rank == EnemyDifficultyRank.Elite) return Plugin.EliteSizeMultiplier.Value; if (rank == EnemyDifficultyRank.Boss) return Plugin.BossSizeMultiplier.Value; if (rank == EnemyDifficultyRank.Regular) return Plugin.RegularEnemySizeMultiplier.Value; return 1f; }

        internal static void Update()
        {
            int currentScene = SceneManager.GetActiveScene().handle;
            if (currentScene != sceneHandle) { States.Clear(); sceneHandle = currentScene; }
            foreach (State state in States.Values) if (state.Npc != null) Apply(state.Npc);
        }

        internal static void Remove(Npc npc) { if (npc != null) States.Remove(npc.m_ID); }

        private static Transform ResolveVisualRoot(Transform view)
        {
            Renderer[] renderers = view.GetComponentsInChildren<Renderer>(true).Where(IsCharacterRenderer).ToArray();
            if (renderers.Length == 0) return null;
            Transform animator = FindComponentTransform(view, "UnityEngine.Animator");
            Transform common = renderers[0].transform;
            for (int i = 1; i < renderers.Length; i++) common = CommonAncestor(common, renderers[i].transform, view);
            if (IsSafe(common) && common != view) return common;
            if (animator != null && IsSafe(animator) && CountRenderers(animator) > 0) return animator;
            Transform[] all = view.GetComponentsInChildren<Transform>(true);
            Transform best = null;
            int bestCount = 0;
            for (int i = 0; i < all.Length; i++) { int count = CountRenderers(all[i]); if (all[i] != view && count > bestCount && IsSafe(all[i])) { best = all[i]; bestCount = count; } }
            return best;
        }

        private static bool IsCharacterRenderer(Renderer renderer) { return renderer != null && !(renderer is ParticleSystemRenderer) && !HasAncestorComponent(renderer.transform, "UnityEngine.Canvas"); }
        private static int CountRenderers(Transform root) { return root.GetComponentsInChildren<Renderer>(true).Count(IsCharacterRenderer); }
        private static Transform CommonAncestor(Transform a, Transform b, Transform stop) { Transform current = a; while (current != null) { Transform probe = b; while (probe != null) { if (probe == current) return current; if (probe == stop) break; probe = probe.parent; } if (current == stop) break; current = current.parent; } return stop; }

        private static bool IsSafe(Transform candidate)
        {
            if (candidate == null || candidate.GetComponentsInChildren<Collider>(true).Length > 0 || candidate.GetComponentsInChildren<CharacterController>(true).Length > 0 || HasAncestorComponent(candidate, "UnityEngine.AI.NavMeshAgent") || HasDescendantComponent(candidate, "ProjectDawn.Navigation.AgentAuthoring")) return false;
            return candidate.GetComponentsInChildren<Renderer>(true).Length > 0;
        }

        private static void LogHierarchy(Npc npc, Transform view)
        {
            Renderer[] renderers = view.GetComponentsInChildren<Renderer>(true);
            List<string> skinned = new List<string>();
            List<string> mesh = new List<string>();
            for (int i = 0; i < renderers.Length; i++) { string path = GetPath(renderers[i].transform); if (renderers[i] is SkinnedMeshRenderer) skinned.Add(path); else if (renderers[i] is MeshRenderer) mesh.Add(path); }
            Transform animator = FindComponentTransform(view, "UnityEngine.Animator");
            Plugin.ModLogger.LogInfo("Character hierarchy: Npc=" + npc.m_ID + " NpcRoot=" + GetPath(view) + " RootComponents=[" + string.Join(",", view.GetComponents<Component>().Select(x => x.GetType().Name).ToArray()) + "] ChildCount=" + view.childCount + " AnimatorPath=" + (animator == null ? "none" : GetPath(animator)) + " RendererCount=" + renderers.Length + " SkinnedRendererPaths=[" + string.Join(";", skinned.Take(12).ToArray()) + "] MeshRendererPaths=[" + string.Join(";", mesh.Take(12).ToArray()) + "] CandidateAncestors=runtime-common-ancestor");
        }

        private static bool TryGetBounds(Transform root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { bounds = default(Bounds); return false; }
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private static Transform FindComponentTransform(Transform root, string fullName)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++) if (components[i] != null && components[i].GetType().FullName == fullName) return components[i].transform;
            return null;
        }

        private static bool HasAncestorComponent(Transform current, string fullName)
        {
            while (current != null)
            {
                Component[] components = current.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++) if (components[i] != null && components[i].GetType().FullName == fullName) return true;
                current = current.parent;
            }
            return false;
        }

        private static bool HasDescendantComponent(Transform root, string fullName)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++) if (components[i] != null && components[i].GetType().FullName == fullName) return true;
            return false;
        }

        private static string GetPath(Transform transform) { string path = transform.name; while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; } return path; }
    }
}
