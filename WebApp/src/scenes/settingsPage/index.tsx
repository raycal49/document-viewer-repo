import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Select from "@mui/material/Select";
import MenuItem from "@mui/material/MenuItem";
import FormControl from "@mui/material/FormControl";
import InputLabel from "@mui/material/InputLabel";
import { useColorMode } from "../../context/ColorModeContext.tsx";
import type { SelectChangeEvent } from "@mui/material/Select";

const SettingsPage = () => {
    const { mode, toggleColorMode } = useColorMode();

    const handleChange = (event: SelectChangeEvent) => {
        toggleColorMode(event.target.value as "light" | "dark");
    };

    return (
        <Box sx={{ p: 4 }}>
            <Typography variant="h6" gutterBottom color="text.primary">
                Theme
            </Typography>
            <FormControl size="small" sx={{ minWidth: 240 }}>
                <InputLabel id="theme-select-label">Select Theme: (Light/Dark)</InputLabel>
                <Select
                    labelId="theme-select-label"
                    value={mode}
                    label="Theme"
                    onChange={handleChange}
                >
                    <MenuItem value="dark">Dark</MenuItem>
                    <MenuItem value="light">Light</MenuItem>
                </Select>
            </FormControl>
        </Box>
    );
};

export default SettingsPage;
