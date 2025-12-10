export interface PreSignedUrl {
  partNumber: number;
  url: string;
}

export interface StartUploadResponse {
  key: string;
  uploadId: string;
  urls: PreSignedUrl[];
}

export interface UploadPart {
  PartNumber: number;
  ETag: string;
}

export interface StoredUpload {
  uploadId: string;
  key: string;
  fileName: string;
  timestamp: number;
}