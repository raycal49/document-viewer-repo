import Box from '@mui/material/Box';
import AnnotationCanvas from './Annotation/AnnotationCanvas.tsx';
import type { Stroke } from './Annotation/AnnotationCanvas.tsx';

export function VideoDisplay({
  videoRef,
  drawingEnabled,
  strokeColor,
  onStroke,
}: {
  videoRef: React.RefObject<HTMLVideoElement | null>;
  drawingEnabled: boolean;
  strokeColor: string;
  onStroke: (stroke: Stroke) => void;
}) {
  return (
    <Box sx={{
      flex: 1,
      position: 'relative',
      bgcolor: '#000',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      overflow: 'hidden',
    }}>
      <video
        ref={videoRef}
        autoPlay
        playsInline
        style={{
          width: '100%',
          height: '100%',
          objectFit: 'contain',
          position: 'relative',
          zIndex: 1,
        }}
      />
      <AnnotationCanvas
        enabled={drawingEnabled}
        videoRef={videoRef}
        strokeColor={strokeColor}
        onStroke={onStroke}
      />
    </Box>
  );
}
