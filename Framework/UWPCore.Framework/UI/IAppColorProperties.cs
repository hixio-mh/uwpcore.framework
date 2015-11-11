﻿using Windows.UI;

namespace UWPCore.Framework.UI
{
    public interface IAppColorProperties
    {
        /// <summary>
        /// Gets whether this is a auto configured or an custom color setting.
        /// </summary>
        bool IsAutoConfigured { get; }

        /// <summary>
        /// Gets the main theme color.
        /// </summary>
        Color? Theme { get; }

        /// <summary>
        /// Gets the title bar foreground color.
        /// </summary>
        Color? TitleBarForeground { get; }

        /// <summary>
        /// Gets the title bar background color.
        /// </summary>
        Color? TitleBarBackground { get; }
    }
}
