import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import PhoneIcon from '@mui/icons-material/Phone';
import PhoneDisabledIcon from '@mui/icons-material/PhoneDisabled';
import type { IncomingCall } from '../../webrtc/BaseInterfaces.ts';

function IncomingCallCard({ call, onAccept, onDecline }: {
  call: IncomingCall;
  onAccept: (room: string) => void;
  onDecline: (room: string) => void;
}) {
  const claimed = call.claimed;
  return (
    <Box sx={{
      display: 'flex', alignItems: 'center', gap: 1.5, px: 2, py: 1, borderRadius: 2,
      border: 1, borderColor: claimed ? 'divider' : 'primary.main',
      opacity: claimed ? 0.4 : 1, bgcolor: 'background.default',
    }}>
      <Box>
        <Typography variant="body2">{call.callerName || 'Quest User'}</Typography>
        <Typography variant="caption" color="text.secondary">{call.room}</Typography>
      </Box>
      {claimed ? (
        <Typography variant="caption" color="text.secondary">answered</Typography>
      ) : (
        <>
          <Tooltip title="Accept">
            <IconButton size="small" color="success" onClick={() => onAccept(call.room)}>
              <PhoneIcon fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Decline">
            <IconButton size="small" color="error" onClick={() => onDecline(call.room)}>
              <PhoneDisabledIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </>
      )}
    </Box>
  );
}

export function IncomingCallsPanel({ calls, onAccept, onDecline }: {
  calls: IncomingCall[];
  onAccept: (room: string) => void;
  onDecline: (room: string) => void;
}) {
  if (calls.length === 0) return null;
  return (
    <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider', bgcolor: 'background.paper' }}>
      <Typography variant="overline" color="text.secondary">Incoming Calls</Typography>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, mt: 1 }}>
        {calls.map((call) => (
          <IncomingCallCard
            key={call.room}
            call={call}
            onAccept={onAccept}
            onDecline={onDecline}
          />
        ))}
      </Box>
    </Box>
  );
}
