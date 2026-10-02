using AsmResolver.Patching;
using Cpp2IL.Core.Extensions;
using NAudio.Wave;

namespace Plana.Core;

//code by slok7565
public static class AudioLoader
{
    public static async System.Threading.Tasks.Task<AudioClip> LoadAudioClipAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        switch (ext)
        {
            case ".wav":
                try
                {
                    return await LoadWavAsync(filePath);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                    return null;
                }
            case ".mp3":
            case ".ogg":
            case ".aif":
            case ".flac":
                try
                {
                    return await LoadWithNAudioAsync(filePath);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                    return null;
                }
            default:
                PDebug.Log($"[AudioLoader] Unsupported extension: {ext}");
                return null;
        }
    }

    #region IO Helper

    private static async System.Threading.Tasks.Task<byte[]> ReadAllBytesAsync(string path)
    {
        try
        {
            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var buffer = new byte[fs.Length];
            _ = await fs.ReadAsync(buffer.AsMemory(0, (int)fs.Length));
            return buffer;
        }
        finally
        {
            GC.Collect();
        }
    }

    #endregion

    #region WAV

    private static async System.Threading.Tasks.Task<AudioClip> LoadWavAsync(string path)
    {
        var bytes = await ReadAllBytesAsync(path);
        var name = Path.GetFileNameWithoutExtension(path);
        if (TryParseWavHeader(bytes, out var info))
            return await CreateClipFromPCM(info.data, info.sampleRate, info.channels, info.bitDepth, name);

        PDebug.Log("[AudioLoader] WAV header invalid, trying raw 16-bit fallback");
        return await CreateClipFromRaw(bytes);
    }
    private static bool TryParseWavHeader(byte[] data,
        out (byte[] data, int sampleRate, int channels, int bitDepth) info)
    {
        info = default;
        if (data.Length < 44 ||
            Encoding.ASCII.GetString(data, 0, 4) != "RIFF" ||
            Encoding.ASCII.GetString(data, 8, 4) != "WAVE")
            return false;

        try
        {
            var fmt = FindChunk(data, "fmt ");
            if (fmt < 0) return false;

            int format = BitConverter.ToInt16(data, fmt + 8);
            if (format != 1 && format != 3) return false;

            int channels = BitConverter.ToInt16(data, fmt + 10);
            var sampleRate = BitConverter.ToInt32(data, fmt + 12);
            int bitDepth = BitConverter.ToInt16(data, fmt + 22);

            var dataChunk = FindChunk(data, "data");
            if (dataChunk < 0) return false;

            var dataSize = BitConverter.ToInt32(data, dataChunk + 4);
            var dataStart = dataChunk + 8;

            var pcm = new byte[dataSize];
            Buffer.BlockCopy(data, dataStart, pcm, 0, dataSize);

            info = (pcm, sampleRate, channels, bitDepth);
            return true;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return false;
        }
        finally
        {
            GC.Collect();
        }
    }

    private static int FindChunk(byte[] data, string id)
    {
        var idx = 12;
        while (idx < data.Length - 8)
        {
            if (Encoding.ASCII.GetString(data, idx, 4) == id)
                return idx;
            idx += 8 + BitConverter.ToInt32(data, idx + 4);
        }

        return -1;
    }

    #endregion

    #region NAudio

    private static async System.Threading.Tasks.Task<AudioClip> LoadWithNAudioAsync(string path)
    {
        await using var reader = new AudioFileReader(path); // 自动识别 AIFF/FLAC/MP3/WAV，但WAV有更高效的方法
        var name = Path.GetFileNameWithoutExtension(path);
        return await BuildClipFromNAudio(reader, name);
    }

    private static async System.Threading.Tasks.Task<AudioClip> BuildClipFromNAudio(AudioFileReader reader, string name)
    {
        var fmt = reader.WaveFormat;
        var channels = fmt.Channels;
        var sampleRate = fmt.SampleRate;
        float[] samples;

        try
        {
            samples = await System.Threading.Tasks.Task.Run(() =>
            {
                var totalSamples = reader.Length / (fmt.BitsPerSample / 8);
                var data = new float[totalSamples];
                var read = reader.Read(data, 0, data.Length);
                return read == data.Length ? data : data.Take(read).ToArray();
            });
        }
        finally
        {
            await reader.DisposeAsync();
        }

        var frames = samples.Length / channels;
        var clip = AudioClip.Create(name, frames, channels, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    #endregion

    #region PCM

    private static readonly HashSet<int> supportedBits = [8, 16, 24, 32, 64];

    private static async System.Threading.Tasks.Task<AudioClip> CreateClipFromPCM(byte[] pcm, int sr, int ch, int bits, string name)
    {
        if (!supportedBits.Contains(bits))
        {
            PDebug.Log($"[AudioLoader] Unsupported bit depth: {bits}");
            return null;
        }

        var data = await System.Threading.Tasks.Task.Run(() => ConvertBytesToFloats(pcm, bits));
        var frames = data.Length / ch;
        var clip = AudioClip.Create(name, frames, ch, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static async System.Threading.Tasks.Task<AudioClip> CreateClipFromRaw(byte[] raw)
    {
        const int ch = 2, sr = 44100;
        var data = await System.Threading.Tasks.Task.Run(() => ConvertBytesToFloats(raw, 16));
        var frames = data.Length / ch;
        var clip = AudioClip.Create("LoadedAudioClip", frames, ch, sr, false);
        clip.SetData(data, 0);
        return clip;
    }

    #endregion

    #region Bit深度转换

    private static float[] ConvertBytesToFloats(byte[] src, int bits)
    {
        return bits switch
        {
            8 => Convert8Bit(src),
            16 => Convert16Bit(src),
            24 => Convert24Bit(src),
            32 => Convert32BitInt(src),
            64 => Convert32Float(src),
            _ => throw new NotSupportedException()
        };
    }

    private static float[] Convert8Bit(byte[] src)
    {
        var dst = new float[src.Length];
        for (var i = 0; i < src.Length; i++)
            dst[i] = (src[i] - 128) / 128f;
        return dst;
    }

    private static float[] Convert16Bit(byte[] src)
    {
        /*if (src.Length % 2 != 0) throw new ArgumentException("odd length");
        var dst = new float[src.Length / 2];
        unsafe
        {
            fixed (byte* pByte = src)
            fixed (float* pFloat = dst)
            {
                var pShort = (short*)pByte;
                for (var i = 0; i < dst.Length; i++)
                    pFloat[i] = pShort[i] / 32768f;
            }
        }

        return dst;*/
        if (src.Length % 2 != 0) throw new ArgumentException("odd length");
        var dst = new float[src.Length / 2];
        var shortArray = new short[dst.Length];
        Buffer.BlockCopy(src, 0, shortArray, 0, src.Length);
        const float divisor = 32768f;
        for (var i = 0; i < dst.Length; i++)
        {
            dst[i] = shortArray[i] / divisor;
        }
        return dst;
    }

    private static float[] Convert24Bit(byte[] src)
    {
        if (src.Length % 3 != 0) throw new ArgumentException("length not multiple of 3");
        var dst = new float[src.Length / 3];
        for (var i = 0; i < dst.Length; i++)
        {
            var o = i * 3;
            var s = src[o] | (src[o + 1] << 8) | (src[o + 2] << 16);
            if ((s & 0x800000) != 0) s |= unchecked((int)0xFF000000);
            dst[i] = s / 8388608f;
        }

        return dst;
    }

    private static float[] Convert32BitInt(byte[] src)
    {
        /*if (src.Length % 4 != 0) throw new ArgumentException("odd length");
        var dst = new float[src.Length / 4];
        unsafe
        {
            fixed (byte* pByte = src)
            fixed (float* pFloat = dst)
            {
                var pInt = (int*)pByte;
                for (var i = 0; i < dst.Length; i++)
                    pFloat[i] = pInt[i] / 2147483648f;
            }
        }

        return dst;*/
        if (src.Length % 4 != 0) throw new ArgumentException("odd length");
        var dst = new float[src.Length / 4];
        var intArray = new int[dst.Length];
        Buffer.BlockCopy(src, 0, intArray, 0, src.Length);
        const float divisor = 2147483648f;
        for (var i = 0; i < dst.Length; i++)
        {
            dst[i] = intArray[i] / divisor;
        }
        return dst;
    }

    private static float[] Convert32Float(byte[] src)
    {
        if (src.Length % 4 != 0) throw new ArgumentException("odd length");
        var dst = new float[src.Length / 4];
        Buffer.BlockCopy(src, 0, dst, 0, src.Length);
        return dst;
    }
}
