import { useMutation } from '@tanstack/react-query';
import { uploadApi } from '../services/uploadService';
import { useUploadStore } from '../store/uploadStore';
import { useRef } from 'react';
import type { UploadPart, StoredUpload, PreSignedUrl } from '@/types/upload';

const CHUNK_SIZE = 5 * 1024 * 1024; // 5MB
const CONCURRENCY = 3;

export const useUploadMutation = () => {
  // Access store actions (non-reactive for inside async functions)
  const store = useUploadStore; 
  const abortRef = useRef<AbortController | null>(null);

  const mutation = useMutation({
    mutationFn: async (file: File) => {
      // 1. Setup UI
      store.getState().setStatus('uploading');
      store.getState().setFile(file); // Ensure file is in store
      abortRef.current = new AbortController();

      const fileId = `ent_upload_${file.name}_${file.lastModified}`;
      const storedLocal = localStorage.getItem(fileId);
      
      let uploadId = "", key = "";
      let batchUrls: PreSignedUrl[] = [];
      let completedParts: UploadPart[] = [];

      // 2. Resume Logic
      if (storedLocal) {
        const parsed: StoredUpload = JSON.parse(storedLocal);
        uploadId = parsed.uploadId;
        key = parsed.key;
        try {
          completedParts = await uploadApi.listParts(key, uploadId);
          store.getState().setUploadInfo(uploadId, key);
        } catch {
          // If expired, restart
          localStorage.removeItem(fileId);
        }
      }

      // 3. Fresh Start Logic
      if (!uploadId || !storedLocal) {
        const init = await uploadApi.start({ fileName: file.name, contentType: file.type, fileSize: file.size });
        uploadId = init.uploadId;
        key = init.key;
        batchUrls = init.urls;
        
        store.getState().setUploadInfo(uploadId, key);
        localStorage.setItem(fileId, JSON.stringify({ uploadId, key, fileName: file.name }));
      }

      // 4. Calculate Queue
      const totalParts = Math.ceil(file.size / CHUNK_SIZE);
      const queue: number[] = [];
      for (let i = 1; i <= totalParts; i++) {
        if (!completedParts.find(p => p.PartNumber === i)) queue.push(i);
      }

      // 5. Parallel Upload Loop
      if (queue.length > 0) {
        let active = 0;
        let queueIndex = 0;
        let partsDone = completedParts.length;

        await new Promise<void>((resolve, reject) => {
          const next = () => {
            if (abortRef.current?.signal.aborted) return reject(new Error("Paused"));
            if (queueIndex >= queue.length) {
              if (active === 0) resolve();
              return;
            }

            const partNum = queue[queueIndex++];
            active++;

            const process = async () => {
              const start = (partNum - 1) * CHUNK_SIZE;
              const end = Math.min(start + CHUNK_SIZE, file.size);
              const chunk = file.slice(start, end);

              // URL Strategy: Batch or Single
              let url = batchUrls.find(u => u.partNumber === partNum)?.url;
              if (!url) url = await uploadApi.getPartUrl(key, uploadId, partNum);

              await uploadApi.uploadBytes(url, chunk);
            };

            process().then(() => {
              active--;
              partsDone++;
              // UPDATE ZUSTAND PROGRESS
              store.getState().setProgress(Math.round((partsDone / totalParts) * 100));
              next();
            }).catch(reject);
          };

          for (let i = 0; i < CONCURRENCY; i++) next();
        });
      }

      // 6. Finalize
      const finalParts = await uploadApi.listParts(key, uploadId);
      await uploadApi.complete({ key, uploadId, parts: finalParts });
      
      localStorage.removeItem(fileId);
      return "Success";
    },
    onSuccess: () => {
      store.getState().setStatus('completed');
      store.getState().setProgress(100);
    },
    onError: (err: unknown) => {
      if (err instanceof Error && err.message === "Paused") {
        store.getState().setStatus('paused');
      } else {
        store.getState().setStatus('error');
      }
    }
  });

  // Helper to resume current file
  const resume = () => {
    const currentFile = store.getState().file;
    if (currentFile) mutation.mutate(currentFile);
  };

  return { ...mutation, pause: () => abortRef.current?.abort(), resume };
};