import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import Tooltip from "@mui/material/Tooltip";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import CallIcon from "@mui/icons-material/Call";
import CallEndIcon from "@mui/icons-material/CallEnd";
import BrushIcon from "@mui/icons-material/Brush";
import { useLocation } from "react-router";
import type { JSX } from "react";

// const HomePanel = () => (
//     <>
//         <Typography variant="overline" color="text.secondary" sx={{ px: 2, pt: 2, display: "block" }}>
//             Chats
//         </Typography>
//         <Divider />
//         <List dense>
//             {["Find some way to integrate this into the app", "Or find another tab because just having a video feed is weird"].map((item) => (
//                 <ListItem key={item}>
//                     <ListItemText primary={item} slotProps={{ primary: { variant: "body2" } }} />
//                 </ListItem>
//             ))}
//         </List>
//     </>
// );

const CallPanel = () => {
    return (
        <>
            <Typography variant="overline" color="text.secondary" sx={{ px: 2, pt: 2, display: "block" }}>
                Call Controls
            </Typography>
            <Divider />
            <Box sx={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 2, pt: 3 }}>
                {/* <Chip label={label} color={color} size="small" variant="outlined" /> */}
                {/* <Button
                    variant="contained" size="small"
                    startIcon={connected ? <LinkOffIcon /> : <LinkIcon />}
                    color={connected ? "error" : "primary"}
                    onClick={connected ? disconnect : connect}
                >
                    {connected ? "Disconnect" : "Connect"}
                </Button> */}
                <Tooltip title="Pick Up Call" placement="right">
                    <IconButton color="success">
                        <CallIcon />
                    </IconButton>
                </Tooltip>
                <Tooltip title="End Call" placement="right">
                    <IconButton color="error">
                        <CallEndIcon />
                    </IconButton>
                </Tooltip>
                {/* Self note: Develop this in the backend */}
                <Tooltip title="Draw" placement="right">
                    <IconButton color="primary">
                        <BrushIcon />
                    </IconButton>
                </Tooltip>
            </Box>
        </>
    );
};

const SettingsPanel = () => (
    <>
        <Typography variant="overline" color="text.secondary" sx={{ px: 2, pt: 2, display: "block" }}>
            Settings
        </Typography>
        <Divider />
        <List dense>
            <ListItem sx={{ cursor: "default" }}>
                <ListItemText primary="Appearance" slotProps={{ primary: { variant: "body2", color: "primary" } }} />
            </ListItem>
        </List>
    </>
);

const panelMap: Record<string, JSX.Element> = {
    // "/home": <HomePanel />,
    "/call": <CallPanel />,
    "/settings": <SettingsPanel />,
};

const ContextPanel = () => {
    const location = useLocation();
    const panel = panelMap[location.pathname] ?? null;

    return (
        <Box
            sx={{
                width: 240,
                flexShrink: 0,
                height: "100vh",
                bgcolor: "background.paper",
                borderRight: 1,
                borderColor: "divider",
                overflowY: "auto",
            }}
        >
            {panel}
        </Box>
    );
};

export default ContextPanel;
