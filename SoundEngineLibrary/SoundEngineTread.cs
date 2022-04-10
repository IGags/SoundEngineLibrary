using System;
using NAudio.Wave;

namespace SoundEngineLibrary
{
    public enum FFTExistance
    {
        Exist,
        DoesntExist
    }

    internal class SoundEngineTread
    {
        public Mp3FileReader CurrentTrack { get; private set; }
        public string FullFilePath { get; private set; }
        public WaveOutEvent OutputDevice { get; private set; }
        public TimeSpan MaxSongDuration { get; private set; }
        public FFT TrackFFT { get; private set; }
        public ThreadOptions TreadType { get; }

        /// <summary>
        /// Проигрывает файл по указаному пути
        /// </summary>
        /// <param name="fullPath">Путь до файла</param>
        /// <param name="treadType">Тип создаваемого потока</param>
        /// <param name="existence">Существование анализаторов бита и спектра</param>
        public SoundEngineTread(string fullPath, ThreadOptions treadType, FFTExistance existence)
        {
            FullFilePath = fullPath;
            TreadType = treadType;
            OutputDevice = new WaveOutEvent();
            CurrentTrack = new Mp3FileReader(fullPath);
            TrackFFT = existence == FFTExistance.Exist ? new FFT(fullPath) : null;
            GC.Collect();
            OutputDevice.Init(CurrentTrack);
            OutputDevice.Play();
            MaxSongDuration = CurrentTrack.TotalTime;
        }

        /// <summary>
        /// Меняет проигрываемый файл по указанному пути
        /// </summary>
        /// <param name="fullPath">Путь до файла</param>
        public void ChangeTrack(string fullPath)
        {
            if (TreadType == ThreadOptions.StaticThread)
            {
                FullFilePath = fullPath;
                if (OutputDevice != null) OutputDevice.Stop();
                else OutputDevice = new WaveOutEvent();
                CurrentTrack = new Mp3FileReader(fullPath);
                TrackFFT = TrackFFT != null ? new FFT(fullPath) : null;
                GC.Collect();
                OutputDevice.Init(CurrentTrack);
                OutputDevice.Play();
                MaxSongDuration = CurrentTrack.TotalTime;
            }
            else throw new InvalidOperationException("Cannot change track in temporal tread");
        }

        /// <summary>
        /// Меняет состояние потока
        /// </summary>
        public void ChangePlaybackState()
        {
            if (OutputDevice != null)
            {
                switch (OutputDevice.PlaybackState)
                {
                    case PlaybackState.Playing:
                        OutputDevice.Pause();
                        break;
                    case PlaybackState.Paused:
                        OutputDevice.Play();
                        break;
                    case PlaybackState.Stopped:
                        CurrentTrack.Position = 0;
                        OutputDevice.Play();
                        break;
                }
            }
        }

        /// <summary>
        /// Изменяет уровень громкости потока относительно максимума
        /// </summary>
        /// <param name="value">Требуемое значение</param>
        /// <param name="max">Максимально возможное значение</param>
        public void ChangeVolume(int value, int max)
        {
            if (value > max || value < 0) 
                throw new ArgumentException("Значение выше максимального или меньше нуля");
            OutputDevice.Volume = (float)value / max;
        }

        /// <summary>
        /// Измерят время В СЕКУНДАХ текущего аудио потока
        /// </summary>
        /// <returns>время В СЕКУНДАХ</returns>
        public int MeasureTime()
        {
            return OutputDevice != null ? (int)CurrentTrack.CurrentTime.TotalSeconds : 0;
        }

        /// <summary>
        /// Выбирает время воспроизведения В СЕКУНДАХ
        /// </summary>
        /// <param name="position">время воспроизведения</param>
        public void ChangePlayingPosition(int position)
        {
            if (OutputDevice != null)
            {
                CurrentTrack.CurrentTime = new TimeSpan(0, 0, position);
            }
        }
    }
}
