import { useState } from "react";
import Box from "@mui/material/Box";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemText from "@mui/material/ListItemText";
import Popover from "@mui/material/Popover";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import NotificationsIcon from '@mui/icons-material/Notifications';
import MoreVertIcon from "@mui/icons-material/MoreVert";
import { useLocation } from "react-router";

interface InfoPopoverProps {
    title: string;
    message: string;
    anchorEl: HTMLButtonElement | null;
    onClose: () => void;
}

const InfoPopover = ({ title, message, anchorEl, onClose }: InfoPopoverProps) => (
    <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={onClose}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
    >
        <Box sx={{ width: 280 }}>
            <Typography variant="subtitle2" fontWeight={600} sx={{ px: 2, py: 1.5 }}>
                {title}
            </Typography>
            <Divider />
            <List disablePadding>
                <ListItem>
                    <ListItemText
                        secondary={message}
                        slotProps={{ secondary: { align: "center" } }}
                        sx={{ py: 1 }}
                    />
                </ListItem>
            </List>
        </Box>
    </Popover>
);

const pageTitles: Record<string, string> = {
    "/": "Home",
    "/call": "Video Call",
    "/settings": "Settings",
};

const TopBar = () => {
    const location = useLocation();
    const title = pageTitles[location.pathname] ?? "Engie";
    const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null);
    const [moreAnchorEl, setMoreAnchorEl] = useState<HTMLButtonElement | null>(null);

    return (
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", height: 48, px: 2, bgcolor: "background.paper", borderBottom: 1, borderColor: "divider", flexShrink: 0 }}>
            <Typography variant="subtitle1" fontWeight={600} color="text.primary">
                {title}
            </Typography>
            <Box sx={{ display: "flex", gap: 0.5 }}>
                <Tooltip title="Notifications">
                    <IconButton color="primary" size="small" onClick={(e) => setAnchorEl(e.currentTarget)}>
                        <NotificationsIcon />
                    </IconButton>
                </Tooltip>
                <InfoPopover title="Notifications" message="No notifications" anchorEl={anchorEl} onClose={() => setAnchorEl(null)} />
                <Tooltip title="More">
                    <IconButton color="primary" size="small" onClick={(e) => setMoreAnchorEl(e.currentTarget)}>
                        <MoreVertIcon />
                    </IconButton>
                </Tooltip>
                <InfoPopover title="More" message="Nothing here for now" anchorEl={moreAnchorEl} onClose={() => setMoreAnchorEl(null)} />
            </Box>
        </Box>
    );
};

export default TopBar;
