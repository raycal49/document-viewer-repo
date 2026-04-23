import { Sidebar, Menu, MenuItem } from "react-pro-sidebar";
import { useNavigate, useLocation } from "react-router";
// import HomeIcon from "@mui/icons-material/Home";
import VideoCallIcon from "@mui/icons-material/VideoCall";
import SettingsIcon from "@mui/icons-material/Settings";
import { useTheme } from "@mui/material";
import type { Theme } from "@mui/material/styles";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Divider from "@mui/material/Divider";

const getSidebarBg = (theme: Theme) =>
    theme.palette.mode === "dark" ? "#252525" : "#EEEEEE";

const getMenuItemStyles = (theme: Theme) => ({
    button: ({ active }: { active: boolean }) => ({
        display: "flex",
        justifyContent: "center",
        color: active ? theme.palette.primary.main : theme.palette.text.secondary,
        backgroundColor: "transparent",
        "&:hover": { color: theme.palette.primary.main, backgroundColor: "transparent" },
    }),
    icon: { margin: 0 },
});

const navItems = [
    // { icon: <HomeIcon />, label: "Home", path: "/home" },
    { icon: <VideoCallIcon />, label: "Video Call", path: "/call" },
    { icon: <SettingsIcon />, label: "Settings", path: "/settings" },
];

const SideBar = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const theme = useTheme();
    const bg = getSidebarBg(theme);

    return (
        <Sidebar
            collapsed
            width="64px"
            collapsedWidth="64px"
            style={{ height: "100vh", borderRight: "none", backgroundColor: bg }}
            rootStyles={{ ["& .ps-sidebar-container"]: { backgroundColor: bg } }}
        >
            <Box sx={{ height: 48, display: "flex", alignItems: "center", justifyContent: "center" }}>
                <Typography variant="h6" fontWeight={700} color="primary">E</Typography>
            </Box>
            <Divider />
            <Menu menuItemStyles={getMenuItemStyles(theme)}>
                {navItems.map(({ icon, label, path }) => (
                    <MenuItem
                        key={path}
                        icon={icon}
                        active={location.pathname === path}
                        onClick={() => navigate(path)}
                        title={label}
                    />
                ))}
            </Menu>
        </Sidebar>
    );
};

export default SideBar;
