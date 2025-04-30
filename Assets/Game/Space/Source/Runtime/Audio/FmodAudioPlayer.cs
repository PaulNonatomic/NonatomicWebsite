using FMODUnity;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Game.Space.Audio
{
	public class FmodAudioPlayer : MonoBehaviour
	{
		[SerializeField] private ServiceLocator _serviceLocator;
		[SerializeField] private string _soundPath;
		[SerializeField] private bool _is3DSound;
		[SerializeField] private float _volume = 1.0f;
		[SerializeField] private bool _loop;

		private IFmodAudioService _audioPlayer;

		protected virtual async void Awake()
		{
			_audioPlayer = await _serviceLocator
				.GetAsync<IFmodAudioService>()
				.WithCancellation(destroyCancellationToken)
				.WithErrorHandling();

			_soundPath = $"event:/{_soundPath}";
			
			// Play the sound based on whether it's 2D or 3D
			bool result;
			if (_is3DSound)
			{
				// Play as 3D sound at the GameObject's position
				result = await _audioPlayer.PlayAt3DPositionAsync(_soundPath, transform.position, _volume, _loop);
			}
			else
			{
				// Play as normal 2D sound
				result = await _audioPlayer.PlayAsync(_soundPath, _volume, _loop);
			}

			Debug.Log($"Sound Complete: {result}");
		}

		private void OnDestroy()
		{
			// Stop the sound when the GameObject is destroyed
			if (_audioPlayer == null || string.IsNullOrEmpty(_soundPath)) return;
			
			_audioPlayer.StopAsync(_soundPath);
		}
	}
}