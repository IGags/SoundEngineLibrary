using System;
using System.Linq;
using AForge.Math;
using NUnit.Framework;

namespace SoundEngineLibrary
{
    [TestFixture]
    class FFTTests
    {
        [Test]
        public void TestInvalidFilePath()
        {
            Assert.Catch<ArgumentException>(() => new FFT(""));
        }

        [Test]
        public void TestSamplesPerSecond()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            var pcmDataCount = fft.GetPcmDataSecondFromPosition(new TimeSpan(0, 0, 0)).Count;
            Assert.AreEqual(44100, pcmDataCount);
        }

        [Test]
        public void TestPcmNegativeTime()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            Assert.Catch<ArgumentException>(() => fft.GetPcmDataSecondFromPosition(new TimeSpan(-1)));
        }

        [Test]
        public void TestPcmOvertime()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            Assert.Catch<ArgumentException>(() => fft.GetPcmDataSecondFromPosition(new TimeSpan(1,1,1,1)));
        }

        [Test]
        public void TestFractionalPcmTime()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            var pcmDataCount = fft.GetPcmDataSecondFromPosition(new TimeSpan(0, 0, 1, 1, 222)).Count;
            Assert.AreEqual(44100, pcmDataCount);
            Assert.AreEqual(new TimeSpan(0, 0, 1, 1, 222), fft.LastQueryTime);
        }

        [Test]
        public void TestTwoSequentQueries()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            fft.GetPcmDataSecondFromPosition(new TimeSpan(0, 0, 1));
            var pcmDataCount = fft.GetPcmDataSecondFromPosition(new TimeSpan(0, 0, 2)).Count;
            Assert.AreEqual(44100, pcmDataCount);
            Assert.AreEqual(new TimeSpan(0, 0, 0, 2), fft.LastQueryTime);
        }

        [Test]
        public void TestTwoInconsistentQueries()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            var firstPcmData = fft.GetPcmDataSecondFromPosition(new TimeSpan(0, 1, 1));
            var secondPcmData = fft.GetPcmDataSecondFromPosition(new TimeSpan(0, 0, 2));
            var pcmDataCount = secondPcmData.Count;
            Assert.AreEqual(44100, pcmDataCount);
            Assert.AreEqual(new TimeSpan(0, 0, 0, 2), fft.LastQueryTime);
            Assert.AreNotEqual(firstPcmData, secondPcmData);
        }

        [Test]
        public void TestNotFullQuery()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            var pcmData = fft.GetPcmDataSecondFromPosition(new TimeSpan(0, 3, 16));
            Assert.AreNotEqual(44100, pcmData.Count);
        }

        [Test]
        public void TestChooseMethodTakesLastFrame()
        {
            var fft = new FFT("TestSamples\\music.mp3");
            var time = new TimeSpan(0, 3, 16);
            var pcmData = fft.GetPcmDataSecondFromPosition(time)
                .Skip(4096)
                .Take(1024)
                .Select(value => new Complex(value, 0))
                .ToArray();
            var expectedFFTFrame = fft.ComputeFFT(pcmData);
            var fftFrame = fft.GetFFTData(time + new TimeSpan(0, 0,0,0,700));
            Assert.AreEqual(expectedFFTFrame, fftFrame);
        }
    }
}
