import { useState, useRef, useCallback } from 'react';
import Box from '@mui/material/Box';
import IconButton from '@mui/material/IconButton';
import Slider from '@mui/material/Slider';
import Tooltip from '@mui/material/Tooltip';
import VolumeUpIcon from '@mui/icons-material/VolumeUp';
import VolumeDownIcon from '@mui/icons-material/VolumeDown';
import VolumeOffIcon from '@mui/icons-material/VolumeOff';
import VolumeMuteIcon from '@mui/icons-material/VolumeMute';

function getVolumeIcon(muted: boolean, volume: number) {
  if (muted || volume === 0) return <VolumeOffIcon fontSize="small" />;
  if (volume < 0.4)          return <VolumeMuteIcon fontSize="small" />;
  if (volume < 0.7)          return <VolumeDownIcon fontSize="small" />;
  return                            <VolumeUpIcon fontSize="small" />;
}

type SliderPopoverProps = {
  top: number;
  right: number;
  volume: number;
  muted: boolean;
  onVolumeChange: (e: Event, value: number | number[]) => void;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
  onDragStart: () => void;
  onDragEnd: () => void;
};

function SliderPopover({
  top, right, volume, muted,
  onVolumeChange, onMouseEnter, onMouseLeave, onDragStart, onDragEnd,
}: SliderPopoverProps) {
  return (
    <Box
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
      sx={{
        position: 'fixed',
        top,
        right,
        transform: 'translateY(-50%)',
        width: 120,
        display: 'flex',
        alignItems: 'center',
        bgcolor: 'background.paper',
        borderRadius: 2,
        px: 1.5,
        py: 0.75,
        boxShadow: 3,
        zIndex: 1300,
      }}
    >
      <Slider
        size="small"
        min={0}
        max={1}
        step={0.05}
        value={muted ? 0 : volume}
        onChange={onVolumeChange}
        onMouseDown={onDragStart}
        onChangeCommitted={onDragEnd}
        aria-label="Volume"
        sx={{ color: 'primary.main' }}
      />
    </Box>
  );
}

export function VolumeControl({ volume, muted, onVolumeChange, onToggleMute }: {
  volume: number;
  muted: boolean;
  onVolumeChange: (e: Event, value: number | number[]) => void;
  onToggleMute: () => void;
}) {
  const [hovered, setHovered]   = useState(false);
  const [dragging, setDragging] = useState(false);
  const [sliderPos, setSliderPos] = useState({ top: 0, right: 0 });
  const iconRef = useRef<HTMLDivElement>(null);

  const open = hovered || dragging;

  const handleMouseEnter = useCallback(() => {
    if (iconRef.current) {
      const rect = iconRef.current.getBoundingClientRect();
      setSliderPos({
        top:   rect.top + rect.height / 2,
        right: window.innerWidth - rect.left + 8,
      });
    }
    setHovered(true);
  }, []);

  return (
    <>
      <Box
        ref={iconRef}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={() => setHovered(false)}
      >
        <Tooltip title={muted ? 'Unmute' : 'Mute'} placement="left" open={open ? false : undefined}>
          <IconButton size="small" onClick={onToggleMute} color="inherit">
            {getVolumeIcon(muted, volume)}
          </IconButton>
        </Tooltip>
      </Box>

      {open && (
        <SliderPopover
          top={sliderPos.top}
          right={sliderPos.right}
          volume={volume}
          muted={muted}
          onVolumeChange={onVolumeChange}
          onMouseEnter={() => setHovered(true)}
          onMouseLeave={() => setHovered(false)}
          onDragStart={() => setDragging(true)}
          onDragEnd={() => setDragging(false)}
        />
      )}
    </>
  );
}