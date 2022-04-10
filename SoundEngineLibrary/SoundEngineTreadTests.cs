using System;
using NUnit.Framework;

namespace SoundEngineLibrary
{
    [TestFixture]
    class SoundEngineTreadTests
    {
        [Test]
        public void TestTemporalTreadTryChangeTrack()
        {
            var engine = new SoundEngine();
            var tread = engine.CreateTread(ThreadOptions.TemporalThread, "TestSamples\\music.mp3",
                FFTExistance.DoesntExist);
            Assert.Catch<InvalidOperationException>(() => engine.TreadList[tread]
                .ChangeTrack(""));
        }

        [Test]
        public void TestTreadInvalidChangeVolume()
        {
            var engine = new SoundEngine();
            var tread = engine.CreateTread(ThreadOptions.TemporalThread, "TestSamples\\music.mp3",
                FFTExistance.DoesntExist);
            Assert.Catch<ArgumentException>(() => engine.TreadList[tread]
                .ChangeVolume(-1, 10));
        }
    }
}
