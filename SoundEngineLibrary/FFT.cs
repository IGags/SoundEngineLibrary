using System;
using System.Collections.Generic;
using System.Linq;
using AForge.Math;
using NAudio.Wave;

namespace SoundEngineLibrary
{
    internal class FFT //TODO: сделать мод записи и чтения детекшена, написать тесты
    {
        public int HighEdge { get; set; } = 2;
        public int LowEdge { get; set; } = 1;
        public double ThresholdValue { get; set; } = 0;
        public TimeSpan LastQueryTime { get; private set; } = TimeSpan.MaxValue;
        private long CurrentSamplePosition { get; set; }

        private const int FFTSize = 1024;

        private readonly AcmMp3FrameDecompressor decompressor;
        private readonly int samplingFrequency;
        private readonly bool isTwoChannels;
        private readonly string filePath;

        private Mp3FileReader file;
        private List<double[]> FFTBuffer = new List<double[]>();
        private List<bool> beatBuffer = new List<bool>();
        private List<short> unusedSamples = new List<short>();
        private List<short> PCMSecondData;

        public FFT(string fullFilePath)
        {
            file = new Mp3FileReader(fullFilePath);
            var waveFormat = file.Mp3WaveFormat;
            decompressor = new AcmMp3FrameDecompressor(waveFormat);
            isTwoChannels = waveFormat.Channels == 2;
            samplingFrequency = waveFormat.SampleRate;
            unusedSamples = new List<short>();
            filePath = fullFilePath;
        }

        public double[] GetFFTData(TimeSpan trackPosition)
        {
            if ((int)LastQueryTime.TotalSeconds != (int)trackPosition.TotalSeconds)
            {
                FFTBuffer.Clear();
                GetPcmDataSecondFromPosition(trackPosition);
            }

            if (FFTBuffer.Count != 0) return FFTBuffer[ChooseElement(trackPosition)];
            
            var transformCount = PCMSecondData.Count / FFTSize;
            for (int i = 0; i < transformCount; i++)
            {
                var fftBuffer = PCMSecondData.Skip(i * FFTSize)
                    .Take(FFTSize)
                    .Select(value => new Complex(value, 0))
                    .ToArray();
                FFTBuffer.Add(ComputeFFT(fftBuffer));
            }

            return FFTBuffer[ChooseElement(trackPosition)];
        }

        public bool GetBeatData(TimeSpan trackPosition)
        {
            if ((int)LastQueryTime.TotalSeconds != (int)trackPosition.TotalSeconds)
            {
                beatBuffer.Clear();
                GetFFTData(trackPosition);
            }

            if (beatBuffer.Count != 0) return beatBuffer[ChooseElement(trackPosition)];
            ComputeBeat();
            return beatBuffer[ChooseElement(trackPosition)];
        }

        public int ChooseElement(TimeSpan trackPosition)
        {
            var currentMillisecond = trackPosition.TotalSeconds % 1;
            var maxElementPosition = samplingFrequency / FFTSize - 1;
            var millisecondPerElement = 1.0 / maxElementPosition;
            var elementPosition = (int)Math.Round(currentMillisecond / millisecondPerElement);
            return elementPosition > FFTBuffer.Count - 1 ? FFTBuffer.Count - 1 : elementPosition;
        }

        public double[] ComputeFFT(Complex[] pcmData)
        {
            FourierTransform.FFT(pcmData, FourierTransform.Direction.Forward);
            return pcmData.Take(pcmData.Length / 2)
                .Select(value => Math.Sqrt(value.Re * value.Re + value.Im * value.Im))
                .ToArray();
        }

        public void ComputeBeat()
        {
            var frequenciesPerCell = samplingFrequency / FFTSize;
            var lowEdge = LowEdge / frequenciesPerCell;
            var highEdge = HighEdge / frequenciesPerCell;
            var energies = FFTBuffer.Select(value =>
            {
                var energy = 0.0;
                for (int i = lowEdge; i < highEdge + 1; i++)
                {
                    energy += value[i];
                }

                return energy;
            }).ToList();

            var averageEnergy = 0.0;
            foreach (var energy in energies)
            {
                averageEnergy += energy;
            }

            averageEnergy /= energies.Count;
            var averageQuadratic = 0.0;
            foreach (var energy in energies)
            {
                averageQuadratic += (energy / averageEnergy - 1) * (energy / averageEnergy - 1);
            }

            averageQuadratic /= energies.Count;
            var thresholdConstant = (-ThresholdValue * averageQuadratic) + 1.5142857;

            foreach (var energy in energies)
            {
                beatBuffer.Add(energy > thresholdConstant * averageEnergy);
            }
        }

        public List<short> GetPcmDataSecondFromPosition(TimeSpan trackPosition)
        {
            if (file.TotalTime.TotalSeconds + 1 < trackPosition.TotalSeconds || trackPosition < TimeSpan.Zero) throw new ArgumentException("IncorrectFilePosition");

            if ((int)LastQueryTime.TotalSeconds > (int)trackPosition.TotalSeconds)
            {
                file = new Mp3FileReader(filePath);
                CurrentSamplePosition = 0;
                unusedSamples.Clear();
            }

            long targetSamplePosition = (int)trackPosition.TotalSeconds * samplingFrequency;
            var outData = new List<short>();
            CurrentSamplePosition += unusedSamples.Count;

            if ((int)trackPosition.TotalSeconds - (int)LastQueryTime.TotalSeconds == 1) outData.AddRange(unusedSamples);
            else
            {
                unusedSamples.Clear();
                var isTargetPosition = false;
                while (true)
                {
                    var frameData = DecompressFrame();
                    CurrentSamplePosition += frameData.Count;
                    var additionalSamples = CurrentSamplePosition - targetSamplePosition;
                    if (additionalSamples > 0)
                    {
                        outData = frameData.Skip((int)(frameData.Count - additionalSamples)).ToList();
                        isTargetPosition = true;
                    }
                    if(isTargetPosition) break;
                }
            }

            while (outData.Count < samplingFrequency)
            {
                var frameData = DecompressFrame();
                if (frameData.Count == 0)
                {
                    LastQueryTime = trackPosition;
                    PCMSecondData = outData.ToList();
                    return outData;
                }
                CurrentSamplePosition += outData.Count;
                outData.AddRange(frameData);
            }

            LastQueryTime = trackPosition;
            unusedSamples = outData.Skip(samplingFrequency).ToList();
            PCMSecondData = outData.Take(samplingFrequency).ToList();
            return PCMSecondData;
        }

        private List<short> DecompressFrame()
        {
            var frame = file.ReadNextFrame();
            var isFrameDecompressed = frame != null;
            var decompressedBytes = new byte[60000];
            var pcmData = new List<short>();
            if (isFrameDecompressed)
            {
                var decompressionSize = decompressor.DecompressFrame(frame, decompressedBytes, 0);
                for (var i = 0; i < decompressionSize; i += 2)
                {
                    var pcmValue = BitConverter.ToInt16(decompressedBytes, i);
                    if (isTwoChannels)
                    {
                        i += 2;
                        pcmValue = (short)(pcmValue / 2 + BitConverter.ToInt16(decompressedBytes, i) / 2);
                    }

                    pcmData.Add(pcmValue);
                }

                return pcmData;
            }

            return new List<short>();
        }
    }
}