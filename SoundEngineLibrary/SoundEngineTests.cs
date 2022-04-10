using NUnit.Framework;
using System.Threading;

namespace SoundEngineLibrary
{
    [TestFixture]
    class SoundEngineTests
    {
        [Test]
        public void TestTreadCreation()
        {
            var engine = new SoundEngine();
            var treadName = engine.CreateTread(ThreadOptions.StaticThread, "TestSamples\\music.mp3",
                FFTExistance.DoesntExist);
            Assert.IsTrue(engine.TreadList.ContainsKey(treadName));
        }

        [Test]
        public void TestTreadDeletion()
        {
            var engine = new SoundEngine();
            var treadName = engine.CreateTread(ThreadOptions.StaticThread, "TestSamples\\music.mp3",
                FFTExistance.DoesntExist);
            engine.TerminateTread(treadName);
            Assert.IsEmpty(engine.TreadList);
        }

        [Test]
        public void TestGarbageCollection()
        {
            var engine = new SoundEngine();
            var treadName = engine.CreateTread(ThreadOptions.TemporalThread, "TestSamples\\empty.mp3",
                FFTExistance.DoesntExist);
            Thread.Sleep(10);
            engine.ClearDiedTreads();
            Assert.IsEmpty(engine.TreadList);
        }
    }
}
