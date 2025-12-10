import type { StartUploadResponse, UploadPart } from '@/types/upload';
import axios from 'axios';

const API_BASE = "http://localhost:5117"; // Update this

export const uploadApi = {
  // 1. Start Fresh (Returns Batch URLs)
  start: async (payload: { fileName: string, contentType: string, fileSize: number }) => {
    const { data } = await axios.post<StartUploadResponse>(`${API_BASE}/files/start-parallel-multipart`, null, {
      params: payload
    });
    return data;
  },

  // 2. Get Single URL (Fallback for Resuming)
  getPartUrl: async (key: string, uploadId: string, partNumber: number) => {
    const { data } = await axios.post(`${API_BASE}/images/${key}/presigned-part`, null, {
      params: { uploadId, partNumber }
    });
    return data.url as string;
  },

  // 3. List Existing Parts (Resume Check)
  listParts: async (key: string, uploadId: string) => {
    const { data } = await axios.get<UploadPart[]>(`${API_BASE}/images/${key}/${uploadId}/parts`);
    return data;
  },

  // 4. Upload Bytes
  uploadBytes: async (url: string, chunk: Blob) => {
    await fetch(url, {
        method: 'PUT',
        body: chunk,
        // Crucial: We intentionally do NOT set any headers object.
        // Since chunk.slice() usually creates a Blob with empty type, 
        // the browser will send this request with NO Content-Type header.
    });
  },

  // 5. Complete
  complete: async (payload: { key: string, uploadId: string, parts: UploadPart[] }) => {
    await axios.post(`${API_BASE}/images/${payload.key}/complete-multipart`, {
      Key: payload.key,
      UploadId: payload.uploadId,
      Parts: payload.parts
    });
  }
};