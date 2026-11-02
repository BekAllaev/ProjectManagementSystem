using MudBlazor;

namespace ProjectManagementSystem.Client.Theme;

/// <summary>
/// The MudBlazor theme of the client, from the Nocturne design system of the UI prototype: a dark blue-grey ground
/// (or its light counterpart), Inter, 8px radii and one blurple accent. The same tokens are CSS variables in
/// <c>wwwroot/css/app.css</c> for the parts MudBlazor has no parameter for; <see cref="ThemeService"/> switches both.
/// </summary>
public static class ProjectFlowTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#6f62c4",
            PrimaryContrastText = "#ffffff",
            Secondary = "#7972a9",
            Background = "#f4f4f8",
            BackgroundGray = "#ececf2",
            Surface = "#fbfbfd",
            AppbarBackground = "#fbfbfd",
            AppbarText = "#1b1c28",
            DrawerBackground = "#f4f4f8",
            DrawerText = "#3f424d",
            DrawerIcon = "#595d6c",
            TextPrimary = "#1b1c28",
            TextSecondary = "rgba(27,28,40,0.6)",
            TextDisabled = "rgba(27,28,40,0.38)",
            ActionDefault = "#595d6c",
            ActionDisabled = "rgba(27,28,40,0.3)",
            ActionDisabledBackground = "rgba(207,211,229,0.5)",
            LinesDefault = "#e1e2ea",
            LinesInputs = "#cfd3e5",
            TableLines = "#e4e5ec",
            Divider = "#e4e5ec",
            DividerLight = "#ececf2",
            Skeleton = "rgba(27,28,40,0.08)",
            OverlayDark = "rgba(27,28,40,0.35)",
            Info = "#3b6fd8",
            Success = "#2e9e72",
            Warning = "#c98a1c",
            Error = "#d0453b",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#9184d9",
            PrimaryContrastText = "#161826",
            Secondary = "#a7a1db",
            Background = "#161826",
            BackgroundGray = "#1c1e2b",
            Surface = "#232532",
            AppbarBackground = "#232532",
            AppbarText = "#e9e9ed",
            DrawerBackground = "#161826",
            DrawerText = "#e4e7f5",
            DrawerIcon = "#b2b6ca",
            TextPrimary = "#e9e9ed",
            TextSecondary = "rgba(233,233,237,0.55)",
            TextDisabled = "rgba(233,233,237,0.35)",
            ActionDefault = "#b2b6ca",
            ActionDisabled = "rgba(233,233,237,0.3)",
            ActionDisabledBackground = "rgba(63,66,77,0.45)",
            LinesDefault = "#3f424d",
            LinesInputs = "#595d6c",
            TableLines = "#3f424d",
            Divider = "#292b31",
            DividerLight = "#3f424d",
            Skeleton = "rgba(233,233,237,0.08)",
            OverlayDark = "rgba(22,24,38,0.6)",
            Info = "#4a86ff",
            Success = "#4fbf95",
            Warning = "#e5b05a",
            Error = "#e0665d",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "system-ui", "sans-serif"],
                FontSize = "0.875rem",
                LineHeight = "1.5",
            },
            H3 = new H3Typography { FontSize = "1.5625rem", FontWeight = "500", LineHeight = "1.12", LetterSpacing = "-0.015em" },
            H4 = new H4Typography { FontSize = "1.25rem", FontWeight = "500", LineHeight = "1.2", LetterSpacing = "-0.01em" },
            H5 = new H5Typography { FontSize = "1rem", FontWeight = "500", LineHeight = "1.3" },
            H6 = new H6Typography { FontSize = "0.9375rem", FontWeight = "500", LineHeight = "1.4" },
            Button = new ButtonTypography { FontWeight = "500", TextTransform = "none" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            AppbarHeight = "56px",
            DrawerWidthLeft = "232px",
        },
    };
}
