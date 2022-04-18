using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace SoundEngineLibrary 
{
    public enum ThreadOptions
    {
        StaticThread,
        TemporalThread
    }
    public class SoundEngine
    {
        private int lastStaticNumber;
        private int lastTemporalNumber;
        private int maxPower { get; set; }
        private int soundPower { get; set; }

        internal Dictionary<string, SoundEngineTread> TreadList { get; } =
            new Dictionary<string, SoundEngineTread>();

        /// <summary>
        /// Создаёт новый поток
        /// </summary>
        /// <param name="treadType">Тип потока</param>
        /// <param name="fullPath">Путь до файла</param>
        /// <param name="analyzeExistence">Параметры существования анализаторов</param>
        /// <returns></returns>
        public SoundEngine(){}
        
        public SoundEngine(int maxPower, int soundPower)
        {
            this.maxPower = maxPower;
            this.soundPower = soundPower;
        }

        public string CreateTread(ThreadOptions treadType, string fullPath, FFTExistance analyzeExistence)
        {
            var treadName = CreateTreadBase(treadType);
            if (soundPower == 0 && maxPower == 0)
                TreadList[treadName] = new SoundEngineTread(fullPath, treadType, analyzeExistence);
            else TreadList[treadName] = new SoundEngineTread(fullPath, treadType, analyzeExistence, soundPower, maxPower);
            return treadName;
        }

        private string CreateTreadBase(ThreadOptions treadType)
        {
            ClearDiedTreads();
            return treadType == ThreadOptions.StaticThread
                ? $@"Static:{lastStaticNumber++}"
                : $@"Temporal:{lastTemporalNumber++}";
        }

        public void ChangeEngineVolume(int volume, int maxVolume)
        {
            if (maxPower == 0) return;
            maxPower = maxVolume;
            soundPower = volume;
            foreach (var tread in TreadList)
            {
                tread.Value.ChangeVolume(volume, maxVolume);
            }
        }

        public SoundEngineTread GetTread(string treadName)
        {
            return TreadList[treadName];
        }

        /// <summary>
        /// Убивает поток по имени
        /// </summary>
        /// <param name="treadName">Имя потока</param>
        public void TerminateTread(string treadName)
        {
            TreadList.Remove(treadName);
        }

        /// <summary>
        /// Чистит мёртвые потоки
        /// </summary>
        public void ClearDiedTreads()
        {
            var keys = TreadList.Keys.ToList();
            foreach (var key in keys)
            {
                if (TreadList[key].TreadType == ThreadOptions.TemporalThread
                    && TreadList[key].OutputDevice.PlaybackState == PlaybackState.Stopped) TreadList.Remove(key);
            }
        }
    }
}