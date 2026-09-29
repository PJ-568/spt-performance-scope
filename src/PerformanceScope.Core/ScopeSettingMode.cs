namespace PerformanceScope.Core
{
    /// <summary>
    /// 配置项的取值方式：沿用游戏默认，或使用插件自定义值。
    /// How a setting is resolved: keep the game default, or use the plugin's custom value.
    /// </summary>
    public enum ScopeSettingMode
    {
        /// <summary>
        /// 保持游戏默认，不做覆盖。
        /// Keep the game default and do not override it.
        /// </summary>
        GameDefault = 0,

        /// <summary>
        /// 使用插件自定义值。
        /// Use the plugin's custom value.
        /// </summary>
        Custom = 1
    }
}
