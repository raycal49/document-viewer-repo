import { Outlet } from "react-router";
import Box from "@mui/material/Box";
import SideBar from "./SideBar.tsx";
import ContextPanel from "./ContextPanel.tsx";
import TopBar from "./TopBar.tsx";

const AppLayout = () => (
    <Box sx={{ display: "flex", flexDirection: "row", height: "100vh", width: "100vw", overflow: "hidden" }}>
        <SideBar />
        <ContextPanel />
        <Box sx={{ display: "flex", flexDirection: "column", flex: 1, overflow: "hidden" }}>
            <TopBar />
            <Box sx={{ flex: 1, overflow: "auto", bgcolor: "background.default" }}>
                <Outlet />
            </Box>
        </Box>
    </Box>
);

export default AppLayout;
