import { UploadTrigger } from "@/components/UploadTrigger"
import { GlobalUploader } from "@/components/GlobalUploader"

function App() {
  return (
    <div className="min-h-screen bg-slate-50 flex flex-col items-center justify-center p-4">
      <h1 className="text-3xl font-bold mb-8 text-slate-800">Enterprise Uploader</h1>
      
      {/* 1. Trigger Area */}
      <UploadTrigger />

      {/* 2. Global Widget (Visible even if you add React Router and change pages) */}
      <GlobalUploader />
    </div>
  )
}

export default App