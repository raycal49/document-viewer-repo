import { useState, useMemo } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { ColorModeContext } from "./context/ColorModeContext.tsx";
import { createAppTheme } from "./theme.ts";
import AppLayout from "./components/layout/AppLayout.tsx";
import HomePage from "./scenes/homePage/index.tsx";
import VideoCallPage from "./scenes/videoCallPage/index.tsx";
import SettingsPage from "./scenes/settingsPage/index.tsx";
import LoginPage from "./scenes/loginPage/index.tsx";

export default function App() {
    const [mode, setMode] = useState<"light" | "dark">("dark");
    const theme = useMemo(() => createAppTheme(mode), [mode]);

    return (
        <ColorModeContext.Provider value={{ mode, toggleColorMode: setMode }}>
            <ThemeProvider theme={theme}>
                <CssBaseline />
                <BrowserRouter>
                    <Routes>
                        <Route path="/" element={<Navigate to="/call" replace />} />
                        <Route path="/login" element={<LoginPage />} />
                        <Route path="/call" element={<VideoCallPage />} />
                        <Route element={<AppLayout />}>
                            <Route index path="/home" element={<HomePage />} />
                            <Route path="/settings" element={<SettingsPage />} />
                        </Route>
                    </Routes>
                </BrowserRouter>
            </ThemeProvider>
        </ColorModeContext.Provider>
    );
}
