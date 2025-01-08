using NAudio.Wave;
using System;

namespace HourglassLibrary.Services
{
    public class AudioService : IDisposable
    {
        private WaveOutEvent _outputDevice;
        private AudioFileReader _audioFile;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public AudioFileReader AudioFile => _audioFile;

        public TimeSpan CurrentTime => _audioFile?.CurrentTime ?? TimeSpan.Zero;
        public TimeSpan TotalTime => _audioFile?.TotalTime ?? TimeSpan.Zero;

        public void PlayAudio(string filePath)
        {
            try
            {
                if (!_isPlaying)
                {
                    if (_outputDevice == null)
                    {
                        _outputDevice = new WaveOutEvent();
                        _audioFile = new AudioFileReader(filePath);
                        _outputDevice.Init(_audioFile);
                    }

                    _outputDevice.Play();
                    _isPlaying = true;
                }
                else if (_outputDevice.PlaybackState == PlaybackState.Paused)
                {
                    _outputDevice.Play();
                    _isPlaying = true;
                }
            }
            catch (Exception ex)
            {
                CleanupResources();
                throw new Exception($"Error playing audio: {ex.Message}", ex);
            }
        }

        public void PauseAudio()
        {
            if (_outputDevice != null && _isPlaying)
            {
                _outputDevice.Pause();
                _isPlaying = false;
            }
        }

        public void StopAudio()
        {
            if (_outputDevice != null)
            {
                _outputDevice.Stop();
                _isPlaying = false;
                if (_audioFile != null)
                {
                    _audioFile.Position = 0; // Reset position to start
                }
            }
        }

        public void SeekToPosition(TimeSpan position)
        {
            if (_audioFile != null)
            {
                _audioFile.CurrentTime = position;
            }
        }

        public double GetCurrentPosition()
        {
            return _audioFile?.CurrentTime.TotalSeconds ?? 0;
        }

        public double GetTotalDuration()
        {
            return _audioFile?.TotalTime.TotalSeconds ?? 0;
        }

        private void CleanupResources()
        {
            if (_outputDevice != null)
            {
                if (_outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    _outputDevice.Stop();
                }
                _outputDevice.Dispose();
                _outputDevice = null;
            }

            if (_audioFile != null)
            {
                _audioFile.Dispose();
                _audioFile = null;
            }

            _isPlaying = false;
        }

        public void Dispose()
        {
            CleanupResources();
            GC.SuppressFinalize(this);
        }

        ~AudioService()
        {
            CleanupResources();
        }
    }
}
