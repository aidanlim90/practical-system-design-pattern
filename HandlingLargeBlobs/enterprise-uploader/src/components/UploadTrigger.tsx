import React from 'react';
import { useUploadMutation } from '../hooks/useUploadMutation';
import { Button } from '@/components/ui/button';
import { CloudUpload } from 'lucide-react';

export const UploadTrigger = () => {
  const { mutate } = useUploadMutation();

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files?.[0]) {
      mutate(e.target.files[0]); // Start the magic
      e.target.value = ''; // Reset input
    }
  };

  return (
    <div className="flex flex-col items-center gap-4 p-8 border-2 border-dashed border-gray-300 rounded-xl hover:bg-gray-50 transition">
      <CloudUpload className="w-12 h-12 text-gray-400" />
      <div className="relative">
        <Button>Select Large File</Button>
        <input type="file" className="absolute inset-0 opacity-0 cursor-pointer" onChange={handleFileChange} />
      </div>
    </div>
  );
};