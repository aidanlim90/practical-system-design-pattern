import { create } from 'zustand';

interface UploadState {
  file: File | null;
  progress: number;
  status: 'idle' | 'uploading' | 'paused' | 'completed' | 'error';
  uploadId: string | null;
  key: string | null;
  
  // Actions
  setFile: (file: File | null) => void;
  setProgress: (progress: number) => void;
  setStatus: (status: UploadState['status']) => void;
  setUploadInfo: (id: string, key: string) => void;
  reset: () => void;
}

export const useUploadStore = create<UploadState>((set) => ({
  file: null,
  progress: 0,
  status: 'idle',
  uploadId: null,
  key: null,

  setFile: (file) => set({ file, status: 'idle', progress: 0 }),
  setProgress: (progress) => set({ progress }),
  setStatus: (status) => set({ status }),
  setUploadInfo: (uploadId, key) => set({ uploadId, key }),
  reset: () => set({ file: null, progress: 0, status: 'idle', uploadId: null, key: null }),
}));