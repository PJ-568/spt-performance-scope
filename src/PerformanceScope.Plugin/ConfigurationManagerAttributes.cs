using System;
using BepInEx.Configuration;

namespace PerformanceScope
{
    /// <summary>
    /// 供 BepInEx.ConfigurationManager 通过反射读取的附加显示属性。
    /// 字段名与上游 ConfigurationManager 约定保持一致，勿随意改名。
    /// </summary>
    public sealed class ConfigurationManagerAttributes
    {
        /// <summary>自定义绘制回调；为 null 时使用默认绘制。</summary>
        public Action<ConfigEntryBase> CustomDrawer;

        /// <summary>是否把数值范围显示为百分比。</summary>
        public bool? ShowRangeAsPercent;

        /// <summary>是否在插件列表中可见。</summary>
        public bool? Browsable;

        /// <summary>是否属于高级选项（仅在展开高级设置时显示）。</summary>
        public bool? IsAdvanced;

        /// <summary>是否隐藏“恢复默认”按钮。</summary>
        public bool? HideDefaultButton;

        /// <summary>是否隐藏设置项名称。</summary>
        public bool? HideSettingName;

        /// <summary>同一节内的排序权重，越小越靠前。</summary>
        public int? Order;
    }
}
