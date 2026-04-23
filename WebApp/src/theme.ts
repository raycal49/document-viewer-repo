import { createTheme } from "@mui/material/styles";

// ENGIE colors
const EngieColors = {
    blue: "#00AAFF",
    blueDark: "#0088CC",
    blueLight: "#33BBFF",
    white: "#FFFFFF",
    darkBg: "#121212",
    darkSurface: "#1E1E1E",
    darkSideNav: "#252525",
    lightBg: "#F5F5F5",
    lightSurface: "#FFFFFF",
    lightSideNav: "#EEEEEE",
    grey: "#9E9E9E",
    textPrimary: "#FFFFFF",
    textSecondary: "#B0BEC5",
    lightTextPrimary: "#212121",
    lightTextSecondary: "#616161",
    success: "#4CAF50",
    error: "#F44336",
};

export const createAppTheme = (mode: "light" | "dark") =>
    createTheme({
        palette: {
            mode,
            primary: {
                main: EngieColors.blue,
                dark: EngieColors.blueDark,
                light: EngieColors.blueLight,
            },
            secondary: {
                main: EngieColors.grey,
            },
            background: {
                default: mode === "dark" ? EngieColors.darkBg : EngieColors.lightBg,
                paper: mode === "dark" ? EngieColors.darkSurface : EngieColors.lightSurface,
            },
            success: {
                main: EngieColors.success,
            },
            error: {
                main: EngieColors.error,
            },
            text: {
                primary: mode === "dark" ? EngieColors.textPrimary : EngieColors.lightTextPrimary,
                secondary: mode === "dark" ? EngieColors.textSecondary : EngieColors.lightTextSecondary,
            },
        },
        typography: {
            fontFamily: "'Roboto', 'Helvetica', 'Arial', sans-serif",
        },
    });

export const EngieColorTokens = EngieColors;
