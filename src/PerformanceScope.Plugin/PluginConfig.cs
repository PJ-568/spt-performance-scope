using BepInEx.Configuration;
using PerformanceScope.Core;

namespace PerformanceScope
{
    /// <summary>
    /// 插件配置项定义。所有绑定项以静态属性暴露，供服务层直接读取。
    /// 配置界面由 BepInEx ConfigurationManager 提供（默认 F1，可在其配置中改键）。
    /// Plugin configuration definitions. All bindings are exposed as static properties
    /// for the service layer to read directly.
    /// The config UI is provided by BepInEx ConfigurationManager (F1 by default; rebindable in its config).
    /// </summary>
    internal static class PluginConfig
    {
        private const string SectionGeneral = "1. 通用 · General";
        private const string SectionResolution = "2. 镜内分辨率 · Scope Resolution";

        public static ConfigEntry<bool> EnableMod { get; private set; }

        public static ConfigEntry<bool> EnableLogging { get; private set; }

        public static ConfigEntry<bool> VerboseLogging { get; private set; }

        public static ConfigEntry<OpticResolutionMode> Mode { get; private set; }

        public static ConfigEntry<float> ScreenHeightRatio { get; private set; }

        public static ConfigEntry<int> AbsolutePixels { get; private set; }

        public static ConfigEntry<bool> ApplyWhileScoped { get; private set; }

        /// <summary>
        /// 绑定全部配置项并立即落盘，生成带注释的初始配置文件。
        /// Binds all config entries and saves immediately, generating the initial annotated config file.
        /// </summary>
        public static void Init(ConfigFile config)
        {
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

            config.Save();
        }
    }
}
