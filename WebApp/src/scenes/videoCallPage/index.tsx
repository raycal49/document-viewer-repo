import Box from '@mui/material/Box';
import SideBar from '../../components/layout/SideBar.tsx';
import StreamToolbar from './StreamToolBar.tsx';
import { TopBar } from './TopBar.tsx';
import { VideoDisplay } from './VideoDisplay.tsx';
import { IncomingCallsPanel } from './IncomingCallsPanel.tsx';
import { ConnectionLog } from './ConnectionLog.tsx';
import { useVideoCallState } from './UseVideoCallState.ts';

function MainPage() {
  const {status, incomingCalls, logs,videoRef,
        connect, acceptCall, declineCall, disconnect,
        leaveCall, drawingEnabled, videoVisible, strokeType,
        strokeColor, handleToggleDrawing, handleToggleStrokeType,
        handleClearAnnotations, handleStroke, handleColorChange,
        } = useVideoCallState();

  const isStreaming = status === 'streaming';

  return (
    <Box sx={{ display: 'flex', flexDirection: 'row', height: '100vh', width: '100vw' }}>
      <SideBar />

      <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}>
        <TopBar status={status} onConnect={connect} onDisconnect={disconnect} />

        <Box sx={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
          <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, overflow: 'hidden' }}>

            {/* Video + toolbar */}
            <Box sx={{
              flex: videoVisible ? 1 : 0,
              overflow: 'hidden',
              display: videoVisible ? 'flex' : 'none',
              minHeight: 0,
            }}>
              <VideoDisplay
                videoRef={videoRef}
                drawingEnabled={drawingEnabled}
                strokeColor={strokeColor}
                onStroke={handleStroke}
              />
              <StreamToolbar
                visible={isStreaming}
                drawingEnabled={drawingEnabled}
                strokeType={strokeType}
                strokeColor={strokeColor}
                onColorChange={handleColorChange}
                onToggleDrawing={handleToggleDrawing}
                onToggleStrokeType={handleToggleStrokeType}
                onClearAnnotations={handleClearAnnotations}
                onEndCall={leaveCall}
                videoRef={videoRef}
              />
            </Box>

            <IncomingCallsPanel
              calls={incomingCalls}
              onAccept={acceptCall}
              onDecline={declineCall}
            />

            <Box sx={{ height: 280, flexShrink: 0, borderTop: 1, borderColor: 'divider' }}>
              <ConnectionLog logs={logs} />
            </Box>

          </Box>
        </Box>
      </Box>
    </Box>
  );
}

export default function VideoCallPage() {
  return <MainPage />;
}
