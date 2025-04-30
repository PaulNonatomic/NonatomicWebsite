using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Intro.Player.Game.Intro.Player;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Intro.Viseme
{
	[Serializable]
	public class VisemeData
	{
		// Fields from our extracted JSON
		public float start;
		public float end;
		public string phoneme;
		public string viseme;

		// For compatibility with the existing system
		public float StartTime => start;
		public float EndTime => end;
	}

	[Serializable]
	public class VisemeMapping
	{
		public string jsonViseme;
		public string targetViseme;
	}

	public class VisemeAnimationController : MonoBehaviour
	{
		[SerializeField] private TextAsset _visemeDataJson;
		[SerializeField] private AudioSource _audioSource;
		[SerializeField] private string _defaultViseme = "neutral";
		[SerializeField] private List<VisemeMapping> _visemeMappings = new();
		[SerializeField] private VisemeTextureUpdater _visemeUpdater;

		[Tooltip(
			"Offset in seconds to apply to viseme timing (positive values delay visemes, negative values speed them up)")]
		[SerializeField]
		private float _visemeTimingOffset;

		[Tooltip("Debug mode to show viseme changes in the console")] [SerializeField]
		private bool _debugVisemes;

		private readonly Dictionary<string, string> _visemeMap = new();
		private Coroutine _animationCoroutine;
		private bool _isPlaying;
		private float _startTime;
		private List<VisemeData> _visemeSequence;

		private void Awake()
		{
			InitializeVisemeMappings();
			LoadVisemeDataIfAvailable();
			SetDefaultViseme();
		}

		private void InitializeVisemeMappings()
		{
			// Initialize the viseme mapping dictionary
			_visemeMap.Clear();
			foreach (var mapping in _visemeMappings)
			{
				if (!string.IsNullOrEmpty(mapping.jsonViseme) && !string.IsNullOrEmpty(mapping.targetViseme))
				{
					_visemeMap[mapping.jsonViseme] = mapping.targetViseme;
				}
			}

			Debug.Log($"Initialized {_visemeMap.Count} viseme mappings");
		}

		private void LoadVisemeDataIfAvailable()
		{
			// Load the viseme data if provided
			if (_visemeDataJson != null)
			{
				LoadVisemeData(_visemeDataJson.text);
			}
		}

		private void SetDefaultViseme()
		{
			// Set the default viseme
			if (_visemeUpdater != null && !string.IsNullOrEmpty(_defaultViseme))
			{
				_visemeUpdater.SetViseme(_defaultViseme);
			}
		}

		/// <summary>
		///     Loads viseme data from JSON string
		/// </summary>
		public void LoadVisemeData(string jsonData)
		{
			try
			{
				_visemeSequence = JsonConvert.DeserializeObject<List<VisemeData>>(jsonData);
				Debug.Log($"Loaded {_visemeSequence.Count} viseme entries");
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to parse viseme JSON data: {e.Message}");
				_visemeSequence = new();
			}
		}

		/// <summary>
		///     Loads viseme data from a JSON file at the specified path
		/// </summary>
		public void LoadVisemeDataFromFile(string filePath)
		{
			try
			{
				var jsonData = File.ReadAllText(filePath);
				LoadVisemeData(jsonData);
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to load viseme data from file {filePath}: {e.Message}");
			}
		}

		/// <summary>
		///     Starts the viseme animation synced with audio playback
		/// </summary>
		[Button]
		public void PlayVisemeAnimation()
		{
			if (_visemeSequence == null || _visemeSequence.Count == 0)
			{
				Debug.LogWarning("No viseme data available to play");
				return;
			}

			if (_isPlaying)
			{
				StopVisemeAnimation();
			}

			_isPlaying = true;
			_startTime = Time.time;

			// Start the animation coroutine
			_animationCoroutine = StartCoroutine(AnimateVisemes());

			// Play the audio if an AudioSource is assigned
			if (_audioSource != null && _audioSource.clip != null)
			{
				_audioSource.Play();
			}
		}

		/// <summary>
		///     Stops the currently playing viseme animation
		/// </summary>
		public void StopVisemeAnimation()
		{
			if (!_isPlaying)
			{
				return;
			}

			if (_animationCoroutine != null)
			{
				StopCoroutine(_animationCoroutine);
				_animationCoroutine = null;
			}

			_isPlaying = false;

			// Stop the audio if it's playing
			if (_audioSource != null && _audioSource.isPlaying)
			{
				_audioSource.Stop();
			}

			// Reset to default viseme
			if (_visemeUpdater != null)
			{
				_visemeUpdater.SetViseme(_defaultViseme);
			}
		}

		/// <summary>
		///     Coroutine that handles the viseme animation timing
		/// </summary>
		private IEnumerator AnimateVisemes()
		{
			if (!_visemeUpdater)
			{
				Debug.LogError("VisemeTextureUpdater not assigned");
				yield break;
			}

			// Set to default viseme initially
			_visemeUpdater.SetViseme(_defaultViseme);

			// Wait for each viseme's timing
			foreach (var visemeData in _visemeSequence)
			{
				// Use the direct start and end times from our JSON, adjusted by the offset
				var startTime = visemeData.StartTime + _visemeTimingOffset;
				var endTime = visemeData.EndTime + _visemeTimingOffset;

				// Skip visemes that would start before the animation begins (due to negative offset)
				if (startTime < 0)
				{
					continue;
				}

				// Wait until it's time to show this viseme
				var timeUntilStart = startTime - (Time.time - _startTime);
				if (timeUntilStart > 0)
				{
					yield return new WaitForSeconds(timeUntilStart);
				}

				// Map the JSON viseme name to your system's viseme name
				var targetViseme = _defaultViseme;
				if (_visemeMap.TryGetValue(visemeData.viseme, out var mappedViseme))
				{
					targetViseme = mappedViseme;
				}
				else if (_visemeUpdater.SetViseme(visemeData.viseme))
				{
					// If no mapping but the viseme exists in the updater, use it directly
					targetViseme = visemeData.viseme;
				}

				// Set the viseme
				_visemeUpdater.SetViseme(targetViseme);

				if (_debugVisemes)
				{
					Debug.Log(
						$"Setting viseme: {visemeData.viseme} -> {targetViseme} (Time: {Time.time - _startTime:F2}s, Phoneme: {visemeData.phoneme})");
				}

				// Calculate how long to display this viseme
				var displayDuration = endTime - startTime;
				if (displayDuration > 0)
				{
					yield return new WaitForSeconds(displayDuration);
				}
			}

			// Animation complete, reset to default viseme
			_visemeUpdater.SetViseme(_defaultViseme);
			_isPlaying = false;
		}

		/// <summary>
		///     Adjusts the viseme timing offset
		/// </summary>
		[Button("Test Different Offset")]
		public void SetVisemeOffset(float newOffset)
		{
			_visemeTimingOffset = newOffset;
			Debug.Log($"Set viseme timing offset to {_visemeTimingOffset} seconds");
		}

		/// <summary>
		///     Utility method to adjust offset and replay animation
		/// </summary>
		[Button("Adjust Offset & Replay")]
		public void AdjustOffsetAndReplay(float newOffset)
		{
			_visemeTimingOffset = newOffset;
			Debug.Log($"Adjusted viseme timing offset to {_visemeTimingOffset} seconds");

			if (_isPlaying)
			{
				StopVisemeAnimation();
			}

			PlayVisemeAnimation();
		}
	}
}