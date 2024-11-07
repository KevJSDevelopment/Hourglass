using NAudio.Wave;

namespace LimiterMessaging.WPF.Services
{
    public class AudioService : IDisposable
    {
        private WaveOutEvent _outputDevice;
        private AudioFileReader _audioFile;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

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
                throw new Exception($"Error playing audio: {ex.Message}");
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

        public void Dispose()
        {
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _outputDevice = null;
            _audioFile?.Dispose();
            _audioFile = null;
        }
    }
}
