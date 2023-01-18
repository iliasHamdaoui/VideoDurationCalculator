using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace CalculateVideoDuration
{
    public class CalculateVideoDuration
    {
        [FunctionName("CalculateVideoDuration")]
        [return: ServiceBus("video-duration-calculator", Connection = "ServiceBusConnection")]
        public async Task<VideoDurationCalculated>  Run([BlobTrigger("resources/{name}", Connection = "ResourcesBlobConnectionString")]Stream myBlob, string name, 
        ILogger log, ExecutionContext context)
        {

            var applicationFolder = context.FunctionAppDirectory;
            var fileName = name.Split('/').Last();

            string fileLocalPath = Path.Combine(applicationFolder, fileName);
    
            var fs = new FileStream(fileLocalPath, FileMode.Create);
            try
            {
                await myBlob.CopyToAsync(fs);
            }
            finally
            {
                await fs.DisposeAsync();
            }

            FFmpeg.SetExecutablesPath(Path.Combine(applicationFolder, "ffmpeg"));

            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(fileLocalPath);
            var videoDuration = Convert.ToInt32(mediaInfo.VideoStreams.First().Duration.TotalSeconds);

            File.Delete(fileLocalPath);

            return new VideoDurationCalculated(name, videoDuration);
        }
    }
}
