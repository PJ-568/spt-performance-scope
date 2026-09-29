using BepInEx.Configuration;
using PerformanceScope.Core;

namespace PerformanceScope
{
    /// <summary>
    /// 插件配置项定义。所有绑定项以静态属性暴露，供服务层直接读取。
    /// 配置界面由 BepInEx ConfigurationManager 提供（SPT 中默认 F12，可在其配置中改键）。
    /// Plugin configuration definitions. All bindings are exposed as static properties
    /// for the service layer to read directly.
    /// The config UI is provided by BepInEx ConfigurationManager (F12 by default in SPT; rebindable in its config).
    /// </summary>
    internal static class PluginConfig
    {
        private const string SectionGeneral = "1. 通用 · General";
        private const string SectionResolution = "2. 镜内分辨率 · Scope Resolution";

        private const string SectionDetails = "3. 镜内贴图与细节 · Scope Textures & Details";

        /// <summary>
        /// 底层配置文件，供插件订阅 <c>SettingChanged</c> 事件。
        /// The underlying config file, used by the plugin to subscribe to <c>SettingChanged</c>.
        /// </summary>
        public static ConfigFile File { get; private set; }

        public static ConfigEntry<bool> EnableMod { get; private set; }

        public static ConfigEntry<bool> EnableLogging { get; private set; }

        public static ConfigEntry<bool> VerboseLogging { get; private set; }

        public static ConfigEntry<OpticResolutionMode> Mode { get; private set; }

        public static ConfigEntry<float> ScreenHeightRatio { get; private set; }

        public static ConfigEntry<int> AbsolutePixels { get; private set; }

        public static ConfigEntry<bool> ApplyWhileScoped { get; private set; }

        public static ConfigEntry<ScopeSettingMode> MipMode { get; private set; }

        public static ConfigEntry<float> MipBias { get; private set; }

        public static ConfigEntry<bool> DisableBloom { get; private set; }

        public static ConfigEntry<bool> DisableUltimateBloom { get; private set; }

        public static ConfigEntry<bool> DisableChromaticAberration { get; private set; }

        public static ConfigEntry<bool> DisableFisheye { get; private set; }

        /// <summary>
        /// 绑定全部配置项并立即落盘，生成带注释的初始配置文件。
        /// Binds all config entries and saves immediately, generating the initial annotated config file.
        /// </summary>
        public static void Init(ConfigFile config)
        {
            File = config;

            EnableMod = config.Bind(
                SectionGeneral,
                "启用模组 | Enable Mod",
                true,
                new ConfigDescription(
                    "总开关。关闭后恢复游戏默认镜内分辨率。\nMaster switch. When off, restores the game's default scope resolution.",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }));

            EnableLogging = config.Bind(
                SectionGeneral,
                "启用日志 | Enable Logging",
                false,
                new ConfigDescription(
                    "输出本模组的常规/诊断/警告日志（错误日志始终输出）。\nWrites info/verbose/warning logs (errors are always logged).",
                    null,
                    new ConfigurationManagerAttributes { Order = 2 }));

            VerboseLogging = config.Bind(
                SectionGeneral,
                "详细日志 | Verbose Logging",
                false,
                new ConfigDescription(
                    "在启用日志的基础上输出更详细的诊断信息。\nAdds more detailed diagnostics on top of Enable Logging.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));

            Mode = config.Bind(
                SectionResolution,
                "取值方式 | Resolution Mode",
                // 默认不改变游戏行为；玩家显式选择后再生效（4K 下 0.7 反而会高于默认 1024）。
                // Default leaves game behaviour unchanged; it applies only after the player picks explicitly
                // (at 4K, 0.7 is actually higher than the default 1024).
                OpticResolutionMode.GameDefault,
                new ConfigDescription(
                    "游戏默认 / 屏幕高度比例 / 绝对像素。\nGame default / screen-height ratio / absolute pixels.",
                    // 枚举无需 AcceptableValueList（ConfigurationManager 会自动渲染为下拉框）
                    // No AcceptableValueList needed for enums (ConfigurationManager renders a dropdown automatically)
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }));

            ScreenHeightRatio = config.Bind(
                SectionResolution,
                "屏幕高度比例 | Screen Height Ratio",
                0.5f,
                new ConfigDescription(
                    "镜内边长 = round(屏幕高度 × 该值)。\nScope edge length = round(screen height × this value).",
                    new AcceptableValueRange<float>(ResolutionMath.MinRatio, ResolutionMath.MaxRatio),
                    new ConfigurationManagerAttributes { ShowRangeAsPercent = true, Order = 2 }));

            AbsolutePixels = config.Bind(
                SectionResolution,
                "绝对像素 | Absolute Pixels",
                ResolutionMath.GameDefaultResolution,
                new ConfigDescription(
                    "镜内方形 RenderTexture 的边长（像素）。\nEdge length of the square scope RenderTexture, in pixels.",
                    new AcceptableValueRange<int>(ResolutionMath.MinResolution, ResolutionMath.MaxResolution),
                    new ConfigurationManagerAttributes { Order = 3 }));

            ApplyWhileScoped = config.Bind(
                SectionResolution,
                "瞄准中立即应用 | Apply While Scoped",
                true,
                new ConfigDescription(
                    "关闭时，瞄准镜内期间的改动会推迟到退出镜内后应用，以避免画面闪烁。\nWhen off, changes made while scoped are deferred until you leave the scope, to avoid a one-frame flicker.",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }));

            MipMode = config.Bind(
                SectionDetails,
                "镜内贴图 mip 模式 | Scope Mip Mode",
                ScopeSettingMode.GameDefault,
                new ConfigDescription(
                    "游戏默认 / 自定义。自定义时覆盖镜内相机的贴图 mip 偏差。\nGame default / custom. When custom, overrides the optic camera's texture mip bias.",
                    null,
                    new ConfigurationManagerAttributes { Order = 1 }));

            MipBias = config.Bind(
                SectionDetails,
                "Mip 偏差 | Mip Bias",
                ScopeMipMath.DefaultCustomBias,
                new ConfigDescription(
                    "绝对值覆盖镜内相机的 streamingMipmapBias：越大纹理越糊、越省带宽；游戏默认值取决于贴图品质，范围为 0 到 2。\nAbsolute override for the optic camera's streamingMipmapBias: higher is blurrier and cheaper. The game default depends on texture quality and ranges from 0 to 2.",
                    new AcceptableValueRange<float>(ScopeMipMath.MinBias, ScopeMipMath.MaxBias),
                    new ConfigurationManagerAttributes { Order = 2 }));

            DisableBloom = config.Bind(
                SectionDetails,
                "关闭镜内泛光 | Disable Scope Bloom",
                false,
                new ConfigDescription(
                    "勾选后关闭镜内相机的泛光（bloomOptimized）。不勾选则保持游戏行为。\nCheck to disable the optic camera's bloom (bloomOptimized). Unchecked keeps the game default.",
                    null,
                    new ConfigurationManagerAttributes { Order = 3 }));

            DisableUltimateBloom = config.Bind(
                SectionDetails,
                "关闭镜内终极泛光 | Disable Scope Ultimate Bloom",
                false,
                new ConfigDescription(
                    "勾选后关闭镜内相机的终极泛光（ultimateBloom）。\nCheck to disable the optic camera's ultimate bloom (ultimateBloom).",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }));

            DisableChromaticAberration = config.Bind(
                SectionDetails,
                "关闭镜内色散 | Disable Scope Chromatic Aberration",
                false,
                new ConfigDescription(
                    "勾选后关闭镜内相机的色散（chromaticAberration）。\nCheck to disable the optic camera's chromatic aberration (chromaticAberration).",
                    null,
                    new ConfigurationManagerAttributes { Order = 5 }));

            DisableFisheye = config.Bind(
                SectionDetails,
                "关闭镜内鱼眼 | Disable Scope Fisheye",
                false,
                new ConfigDescription(
                    "勾选后关闭镜内相机的鱼眼（fisheye）。\nCheck to disable the optic camera's fisheye (fisheye).",
                    null,
                    new ConfigurationManagerAttributes { Order = 6 }));

            config.Save();
        }
    }
}
