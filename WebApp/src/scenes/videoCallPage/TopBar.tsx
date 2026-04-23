import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import LinkIcon from '@mui/icons-material/Link';
import LinkOffIcon from '@mui/icons-material/LinkOff';
import type { ConnectionStatus } from '../../webrtc/BaseInterfaces.ts';

const STATUS_CONFIG: Record<ConnectionStatus, { color: 'default' | 'success' | 'warning' | 'error'; label: string }> = {
  disconnected: { color: 'default',  label: 'Disconnected' },
  waiting:      { color: 'warning',  label: 'Waiting for calls' },
  connecting:   { color: 'warning',  label: 'Connecting...' },
  streaming:    { color: 'success',  label: 'Streaming' },
  error:        { color: 'error',    label: 'Error' },
  'call-ended': { color: 'default',  label: 'Call ended' },
};

function StatusChip({ status }: { status: ConnectionStatus }) {
  const { color, label } = STATUS_CONFIG[status];
  return <Chip label={label} color={color} size="small" variant="outlined" />;
}

function getTopBarLabel(isConnecting: boolean, canDisconnect: boolean): string {
  if (isConnecting)   return 'Connecting...';
  if (canDisconnect)  return 'Disconnect From Server';
  return 'Connect To Server';
}

function getTopBarHandler(
  canDisconnect: boolean,
  canConnect: boolean,
  onConnect: () => void,
  onDisconnect: () => void,
): (() => void) | undefined {
  if (canDisconnect) return onDisconnect;
  if (canConnect)    return onConnect;
  return undefined;
}

function useTopBarButton(
  status: ConnectionStatus,
  onConnect: () => void,
  onDisconnect: () => void,
) {
  const isConnecting  = status === 'connecting';
  const canDisconnect = status === 'waiting' || status === 'streaming';
  const canConnect    = status === 'disconnected' || status === 'call-ended' || status === 'error';
  const color: 'error' | 'primary' = canDisconnect ? 'error' : 'primary';
  const icon    = canDisconnect ? <LinkOffIcon /> : <LinkIcon />;
  const label   = getTopBarLabel(isConnecting, canDisconnect);
  const handleClick = getTopBarHandler(canDisconnect, canConnect, onConnect, onDisconnect);
  return { isConnecting, label, color, icon, handleClick };
}

export function TopBar({ status, onConnect, onDisconnect }: {
  status: ConnectionStatus;
  onConnect: () => void;
  onDisconnect: () => void;
}) {
  const { isConnecting, label, color, icon, handleClick } = useTopBarButton(status, onConnect, onDisconnect);
  return (
    <Box sx={{
      display: 'flex', alignItems: 'center', gap: 1,
      px: 2, py: 0.5,
      bgcolor: 'background.paper', borderBottom: 1, borderColor: 'divider',
    }}>
      <StatusChip status={status} />
      <Button
        variant="contained" size="small"
        startIcon={icon}
        color={color}
        disabled={isConnecting}
        onClick={handleClick}
        sx={{
          ml: 1,
          ...(isConnecting && { transform: 'translateY(1px)', boxShadow: 'none', opacity: 0.7 }),
        }}
      >
        {label}
      </Button>
      <Box sx={{ flex: 1 }} />
    </Box>
  );
}
