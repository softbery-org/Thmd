using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using FFmpeg.AutoGen;

namespace Thmd.Ffmpeg
{
    public unsafe class FfmpegVideoPlayer
    {
        public event Action<WriteableBitmap> FrameReady;

        private Thread _decodeThread;
        private bool _running;

        public void Play(string file)
        {
            ffmpeg.RootPath = @"ffmpeg"; // ustaw ścieżkę do FFmpeg
            ffmpeg.avformat_network_init();

            _running = true;
            _decodeThread = new Thread(() => DecodeLoop(file));
            _decodeThread.IsBackground = true;
            _decodeThread.Start();
        }

        public void Stop() => _running = false;

        private void DecodeLoop(string path)
        {
            AVFormatContext* fmt = ffmpeg.avformat_alloc_context();
            if (ffmpeg.avformat_open_input(&fmt, path, null, null) < 0) return;
            if (ffmpeg.avformat_find_stream_info(fmt, null) < 0) return;

            int videoStream = -1;
            for (int i = 0; i < fmt->nb_streams; i++)
            {
                if (fmt->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    videoStream = i;
                    break;
                }
            }
            if (videoStream == -1) return;

            AVCodecParameters* par = fmt->streams[videoStream]->codecpar;
            AVCodec* codec = ffmpeg.avcodec_find_decoder(par->codec_id);
            AVCodecContext* ctx = ffmpeg.avcodec_alloc_context3(codec);
            ffmpeg.avcodec_parameters_to_context(ctx, par);
            if (ffmpeg.avcodec_open2(ctx, codec, null) < 0) return;

            int w = ctx->width;
            int h = ctx->height;

            SwsContext* sws = ffmpeg.sws_getContext(
                w, h, ctx->pix_fmt,
                w, h, AVPixelFormat.AV_PIX_FMT_BGR24,
                (int)SwsFlags.SWS_FAST_BILINEAR,
                null, null, null);

            AVPacket* packet = ffmpeg.av_packet_alloc();
            AVFrame* frame = ffmpeg.av_frame_alloc();
            AVFrame* rgbFrame = ffmpeg.av_frame_alloc();

            int bufferSize = w * h * 3;
            byte* bufferPtr = (byte*)ffmpeg.av_malloc((ulong)bufferSize);

            byte_ptrArray4 dstData = new byte_ptrArray4();
            int_array4 dstLinesize = new int_array4();
            dstData[0] = bufferPtr;
            dstLinesize[0] = w * 3;

            // Tworzymy WriteableBitmap w wątku UI
            WriteableBitmap wb = null;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgr24, null);
                FrameReady?.Invoke(wb);
            });

            while (_running && ffmpeg.av_read_frame(fmt, packet) >= 0)
            {
                if (packet->stream_index != videoStream)
                {
                    ffmpeg.av_packet_unref(packet);
                    continue;
                }

                ffmpeg.avcodec_send_packet(ctx, packet);
                ffmpeg.av_packet_unref(packet);

                while (ffmpeg.avcodec_receive_frame(ctx, frame) == 0)
                {
                    ffmpeg.sws_scale(
                        sws,
                        frame->data,
                        frame->linesize,
                        0,
                        h,
                        dstData,
                        dstLinesize);

                    // Aktualizacja WriteableBitmap w wątku UI
                    if (wb != null)
                    {
                        wb.Dispatcher.InvokeAsync(() =>
                        {
                            wb.Lock();
                            IntPtr ptr = wb.BackBuffer;
                            Buffer.MemoryCopy(bufferPtr, (void*)ptr, bufferSize, bufferSize);
                            wb.AddDirtyRect(new Int32Rect(0, 0, w, h));
                            wb.Unlock();
                        });
                    }

                    ffmpeg.av_frame_unref(frame);
                    Thread.Sleep(15); // ~60 FPS = 15, ~30FPS = 30
                }
            }

            // Flush dekodera
            ffmpeg.avcodec_send_packet(ctx, null);
            while (ffmpeg.avcodec_receive_frame(ctx, frame) == 0)
            {
                ffmpeg.sws_scale(
                    sws,
                    frame->data,
                    frame->linesize,
                    0,
                    h,
                    dstData,
                    dstLinesize);

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    wb.Lock();
                    IntPtr ptr = wb.BackBuffer;
                    Buffer.MemoryCopy(bufferPtr, (void*)ptr, bufferSize, bufferSize);
                    wb.AddDirtyRect(new Int32Rect(0, 0, w, h));
                    wb.Unlock();
                });

                ffmpeg.av_frame_unref(frame);
            }

            ffmpeg.av_free(bufferPtr);
            ffmpeg.sws_freeContext(sws);
            ffmpeg.av_frame_free(&rgbFrame);
            ffmpeg.av_frame_free(&frame);
            ffmpeg.av_packet_free(&packet);
            ffmpeg.avcodec_free_context(&ctx);
            ffmpeg.avformat_close_input(&fmt);
        }
    }
}

/*use
Thmd.Ffmpeg.FfmpegVideoPlayer player = new FfmpegVideoPlayer();

player.FrameReady += (bitmap) =>
{
    VideoImage.Dispatcher.InvokeAsync(() =>
    {
        VideoImage.Source = bitmap;
    });
};

player.Play(@"F:\Filmy\Calineczka-WarnerBros-PL.mp4");*/
