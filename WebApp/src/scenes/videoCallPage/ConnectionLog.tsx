import Box from '@mui/material/Box';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import Typography from '@mui/material/Typography';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import DownloadIcon from '@mui/icons-material/Download';
import { useState } from 'react';
import { useAnnotationLogger } from './UseAnnotationLogger';

type LogFilter = 'all' | 'annotation' | 'webrtc' | 'connection' | 'system';

function getLogColor(logLine: string): string {
  if (logLine.includes('[ANNOTATION]'))  return '#4CAF50';
  if (logLine.includes('[WEBRTC]'))      return '#2196F3';
  if (logLine.includes('[CONNECTION]'))  return '#FF9800';
  if (logLine.includes('[SYSTEM]'))      return '#9C27B0';
  return 'text.secondary';
}

function ConnectionLogHeader({
  filteredCount,
  expanded,
  onToggleExpand,
  filter,
  onFilterChange,
  onExport,
}: {
  filteredCount: number;
  expanded: boolean;
  onToggleExpand: () => void;
  filter: LogFilter;
  onFilterChange: (f: LogFilter) => void;
  onExport: () => void;
}) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
      <Box
        sx={{ display: 'flex', alignItems: 'center', cursor: 'pointer', flex: 1 }}
        onClick={onToggleExpand}
      >
        <Typography variant="overline" color="text.secondary">
          Log ({filteredCount})
        </Typography>
        {expanded ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
      </Box>

      <Tooltip title={`Export ${filter === 'all' ? 'all' : filter} logs`}>
        <IconButton size="small" onClick={onExport} sx={{ color: 'text.secondary' }}>
          <DownloadIcon fontSize="small" />
        </IconButton>
      </Tooltip>

      <Box sx={{ display: 'flex', gap: 0.5 }}>
        {(['all', 'annotation', 'webrtc', 'connection', 'system'] as const).map((f) => (
          <Chip
            key={f}
            label={f}
            size="small"
            onClick={() => onFilterChange(f)}
            color={filter === f ? 'primary' : 'default'}
            variant={filter === f ? 'filled' : 'outlined'}
            sx={{ textTransform: 'capitalize', fontSize: '0.7rem', height: '20px' }}
          />
        ))}
      </Box>
    </Box>
  );
}

export function ConnectionLog({ logs }: { logs: string[] }) {
  const [expanded, setExpanded] = useState(false);
  const [filter, setFilter] = useState<LogFilter>('all');

  const { exportLogs, exportCategoryLogs, exportAnnotations } = useAnnotationLogger();

  const filteredLogs =
    filter === 'all'
      ? logs
      : logs.filter((log) => log.includes(`[${filter.toUpperCase()}]`));

  const handleExport = () => {
    if (filter === 'annotation') {
      exportAnnotations();
    } else if (filter === 'all') {
      exportLogs();
    } else {
      exportCategoryLogs(filter);
    }
  };

  return (
    <Box sx={{
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
      px: 2, py: 1,
      bgcolor: 'background.paper',
    }}>
      <ConnectionLogHeader
        filteredCount={filteredLogs.length}
        expanded={expanded}
        onToggleExpand={() => setExpanded(!expanded)}
        filter={filter}
        onFilterChange={setFilter}
        onExport={handleExport}
      />

      <Box sx={{
        flex: 1,
        overflowY: 'auto',
        fontFamily: 'monospace',
        fontSize: '0.75rem',
        lineHeight: 1.8,
      }}>
        {filteredLogs.map((line, i) => (
          <Box
            key={i}
            sx={{
              borderBottom: 1,
              borderColor: 'divider',
              py: 0.25,
              color: getLogColor(line),
            }}
          >
            {line}
          </Box>
        ))}
        {filteredLogs.length === 0 && (
          <Typography variant="caption" color="text.secondary">
            No {filter !== 'all' ? filter : ''} logs yet
          </Typography>
        )}
      </Box>
    </Box>
  );
}
