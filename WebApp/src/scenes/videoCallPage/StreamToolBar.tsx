import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import BrushIcon from '@mui/icons-material/Brush';
import PhoneDisabledIcon from '@mui/icons-material/PhoneDisabled';
import TimerOffIcon from '@mui/icons-material/TimerOff';
import AllInclusiveIcon from '@mui/icons-material/AllInclusive';
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep';
import type { StrokeType } from './Annotation/AnnotationLogic.ts';
import { useState, useRef, useEffect } from 'react';
import { VolumeControl } from './VolumeControl/VolumeControl.tsx';
import { useVolumeControl } from './VolumeControl/UseVolumeControl.ts';

const STROKE_COLORS = [
  { hex: '#FF0000', label: 'Red' },
  { hex: '#FF8800', label: 'Orange' },
  { hex: '#FFEE00', label: 'Yellow' },
  { hex: '#22C55E', label: 'Green' },
  { hex: '#3B82F6', label: 'Blue' },
  { hex: '#8B5CF6', label: 'Purple' },
  { hex: '#EC4899', label: 'Pink' },
  { hex: '#FFFFFF', label: 'White' },
];

function ColorPickerButton({ drawingEnabled, strokeColor, onColorChange }: {
  drawingEnabled: boolean;
  strokeColor: string;
  onColorChange: (color: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  return (
    <Box ref={ref} sx={{ position: 'relative' }}>
      <Tooltip title="Stroke color" placement="left">
        <span>
          <IconButton
            onClick={() => setOpen(o => !o)}
            disabled={!drawingEnabled}
            sx={{ p: 0.5 }}
          >
            <Box sx={{
              width: 20, height: 20, borderRadius: '50%',
              bgcolor: strokeColor,
              border: '2px solid',
              borderColor: 'divider',
              opacity: drawingEnabled ? 1 : 0.4,
            }} />
          </IconButton>
        </span>
      </Tooltip>

      {open && (
        <Box sx={{
          position: 'absolute', right: 48, top: 0,
          bgcolor: 'background.paper', border: 1, borderColor: 'divider',
          borderRadius: 2, p: 1, display: 'grid',
          gridTemplateColumns: 'repeat(4, 1fr)', gap: 0.75, zIndex: 20,
          width: 112,
        }}>
          {STROKE_COLORS.map(c => (
            <Box
              key={c.hex}
              title={c.label}
              onClick={() => { onColorChange(c.hex); setOpen(false); }}
              sx={{
                width: 20, height: 20, borderRadius: '50%',
                bgcolor: c.hex,
                cursor: 'pointer',
                border: '2px solid',
                borderColor: c.hex === strokeColor ? 'text.primary' : 'transparent',
                '&:hover': { borderColor: 'text.secondary' },
              }}
            />
          ))}
        </Box>
      )}
    </Box>
  );
}

interface StreamToolbarProps {
  visible: boolean;
  drawingEnabled: boolean;
  strokeType: StrokeType;
  strokeColor: string; 
  onToggleDrawing: () => void;
  onToggleStrokeType: () => void;
  onColorChange: (color: string) => void;
  onClearAnnotations: () => void;
  onEndCall: () => void;
  videoRef: React.RefObject<HTMLVideoElement | null>;
}

function DrawToggleButton({ drawingEnabled, onToggleDrawing }: {
  drawingEnabled: boolean;
  onToggleDrawing: () => void;
}) {
  const title   = drawingEnabled ? 'Stop Drawing' : 'Draw on stream';
  const bgcolor = drawingEnabled ? 'warning.main' : 'transparent';
  const color   = drawingEnabled ? 'warning.contrastText' : 'text.secondary';
  const hoverBg = drawingEnabled ? 'warning.dark' : 'action.hover';
  return (
    <Tooltip title={title} placement="left">
      <IconButton onClick={onToggleDrawing} sx={{ bgcolor, color, '&:hover': { bgcolor: hoverBg } }}>
        <BrushIcon fontSize="small" />
      </IconButton>
    </Tooltip>
  );
}
 
function StrokeTypeButton({ drawingEnabled, strokeType, onToggleStrokeType }: {
  drawingEnabled: boolean;
  strokeType: StrokeType;
  onToggleStrokeType: () => void;
}) {
  const isFading = strokeType === 'fading';
  const title    = isFading ? 'Switch to Permanent strokes' : 'Switch to Fading strokes';
  const color    = drawingEnabled ? (isFading ? 'info.main' : 'success.main') : 'text.disabled';
  const icon     = isFading ? <TimerOffIcon fontSize="small" /> : <AllInclusiveIcon fontSize="small" />;
  return (
    <Tooltip title={title} placement="left">
      <span>
        <IconButton
          onClick={onToggleStrokeType}
          disabled={!drawingEnabled}
          sx={{ color, '&:hover': { bgcolor: 'action.hover' } }}
        >
          {icon}
        </IconButton>
      </span>
    </Tooltip>
  );
}
 
export default function StreamToolbar({
  visible, drawingEnabled, strokeType, strokeColor, onColorChange,
  onToggleDrawing, onToggleStrokeType, onClearAnnotations, onEndCall, videoRef,
}: StreamToolbarProps) {
  const { volume, muted, handleVolumeChange, handleToggleMute } = useVolumeControl(videoRef);
  if (!visible) return null;
 
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1,
               py: 2, px: 0.5, bgcolor: 'background.paper', borderLeft: 1, borderColor: 'divider', width: 48 }}>
 
      <DrawToggleButton drawingEnabled={drawingEnabled} onToggleDrawing={onToggleDrawing} />
 
      <StrokeTypeButton drawingEnabled={drawingEnabled} strokeType={strokeType} onToggleStrokeType={onToggleStrokeType} />
      <ColorPickerButton drawingEnabled={drawingEnabled} strokeColor={strokeColor} onColorChange={onColorChange}/>
 
      <Divider flexItem sx={{ borderColor: 'divider' }} />
 
      <Tooltip title="Clear all annotations" placement="left">
        <IconButton onClick={onClearAnnotations}
          sx={{ color: 'text.secondary', '&:hover': { bgcolor: 'action.hover', color: 'error.main' } }}>
          <DeleteSweepIcon fontSize="small" />
        </IconButton>
      </Tooltip>
 
      <Divider flexItem sx={{ borderColor: 'divider' }} />

      <VolumeControl
        volume={volume}
        muted={muted}
        onVolumeChange={handleVolumeChange}
        onToggleMute={handleToggleMute}
      />

      <Divider flexItem sx={{ borderColor: 'divider' }} />
 
      <Tooltip title="End Call" placement="left">
        <IconButton onClick={onEndCall}
          sx={{ bgcolor: 'error.main', color: 'error.contrastText', '&:hover': { bgcolor: 'error.dark' } }}>
          <PhoneDisabledIcon fontSize="small" />
        </IconButton>
      </Tooltip>
    </Box>
  );
}
 