import { createContext, useContext } from "react";

interface ColorModeContextType {
    mode: "light" | "dark";
    toggleColorMode: (mode: "light" | "dark") => void;
}

export const ColorModeContext = createContext<ColorModeContextType>({
    mode: "dark",
    toggleColorMode: () => {},
});

export const useColorMode = () => useContext(ColorModeContext);
