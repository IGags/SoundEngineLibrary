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
        internal Dictionary<string, SoundEngineTread> TreadList { get; } =
            new Dictionary<string, SoundEngineTread>();

        /// <summary>
        /// Создаёт новый поток
        /// </summary>
        /// <param name="treadType">Тип потока</param>
        /// <param name="fullPath">Путь до файла</param>
        /// <param name="analyzeExistence">Параметры существования анализаторов</param>
        /// <returns></returns>
        public string CreateTread(ThreadOptions treadType, string fullPath, FFTExistance analyzeExistence)
        {
            ClearDiedTreads();
            var treadName = treadType == ThreadOptions.StaticThread
                ? $@"Static:{lastStaticNumber++}"
                : $@"Temporal:{lastTemporalNumber++}";
            TreadList[treadName] = new SoundEngineTread(fullPath, treadType, analyzeExistence);
            return treadName;
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