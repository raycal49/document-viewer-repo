import { useState, useCallback } from 'react';

export function useVolumeControl(videoRef: React.RefObject<HTMLVideoElement | null>) {
  const [volume, setVolume] = useState(1);
  const [muted, setMuted]   = useState(false);

  const handleVolumeChange = useCallback((_: Event, value: number | number[]) => {
    const v = value as number;
    setVolume(v);
    setMuted(v === 0);
    if (videoRef.current) videoRef.current.volume = v;
  }, [videoRef]);

  const handleToggleMute = useCallback(() => {
    setMuted((prev) => {
      const next = !prev;
      if (videoRef.current) videoRef.current.muted = next;
      return next;
    });
  }, [videoRef]);

  return { volume, muted, handleVolumeChange, handleToggleMute };
}