import { useUploadStore } from '../store/uploadStore';
import { useUploadMutation } from '../hooks/useUploadMutation';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Card, CardContent } from '@/components/ui/card';
import { X, Minimize2, Play, Pause, CheckCircle } from 'lucide-react';

export const GlobalUploader = () => {
  // Read from Zustand
  const { file, progress, status, reset } = useUploadStore();
  // Get Actions from React Query Hook
  const { pause, resume } = useUploadMutation();

  if (!file && status === 'idle') return null;

  return (
    <Card className="fixed bottom-4 right-4 w-80 shadow-2xl border-t-4 border-blue-600 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
      <CardContent className="p-4">
        
        <div className="flex justify-between items-center mb-3">
          <h4 className="font-semibold text-sm truncate max-w-[150px]">{file?.name}</h4>
          <div className="flex gap-2">
            <Minimize2 className="h-4 w-4 text-gray-400 cursor-pointer" />
            <X className="h-4 w-4 text-gray-400 cursor-pointer hover:text-red-500" onClick={reset} /> 
          </div>
        </div>

        <div className="space-y-1 mb-4">
          <div className="flex justify-between text-xs text-gray-500">
            <span className="uppercase font-bold">{status}</span>
            <span>{progress}%</span>
          </div>
          <Progress value={progress} className="h-2" />
        </div>

        <div className="flex gap-2">
           {status === 'uploading' && (
             <Button size="sm" variant="outline" className="w-full" onClick={pause}>
               <Pause className="h-3 w-3 mr-2" /> Pause
             </Button>
           )}
           {(status === 'paused' || status === 'error') && (
             <Button size="sm" className="w-full bg-blue-600" onClick={resume}>
               <Play className="h-3 w-3 mr-2" /> Resume
             </Button>
           )}
           {status === 'completed' && (
             <Button size="sm" variant="secondary" className="w-full text-green-700 bg-green-50" onClick={reset}>
               <CheckCircle className="h-3 w-3 mr-2" /> Done
             </Button>
           )}
        </div>
      </CardContent>
    </Card>
  );
};