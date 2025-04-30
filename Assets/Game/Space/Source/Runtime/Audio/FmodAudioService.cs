using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Nonatomic.ServiceLocator;
using UnityEngine;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Game.Space.Audio
{
	public interface IFmodAudioService
	{
		/// <summary>
		///     Check if banks are loaded and ready to play audio
		/// </summary>
		bool IsReady { get; }

		/// <summary>
		///     Play an FMOD event and await its completion
		/// </summary>
		/// <param name="eventPath">Path to the FMOD event</param>
		/// <param name="volume">Playback volume (0.0 to 1.0)</param>
		/// <param name="loop">Whether to loop the audio</param>
		/// <returns>Task that completes when audio finishes playing</returns>
		Task<bool> PlayAsync(string eventPath, float volume = 1.0f, bool loop = false);

		/// <summary>
		///     Play a 3D FMOD event at a specific position
		/// </summary>
		/// <param name="eventPath">Path to the FMOD event</param>
		/// <param name="position">World position for the sound</param>
		/// <param name="volume">Playback volume (0.0 to 1.0)</param>
		/// <param name="loop">Whether to loop the audio</param>
		/// <returns>Task that completes when audio finishes playing</returns>
		Task<bool> PlayAt3DPositionAsync(string eventPath, Vector3 position, float volume = 1.0f, bool loop = false);

		/// <summary>
		///     Stop an FMOD event by path
		/// </summary>
		/// <param name="eventPath">Path to the FMOD event to stop</param>
		/// <returns>Task that completes when audio is stopped</returns>
		Task<bool> StopAsync(string eventPath);

		/// <summary>
		///     Stop all currently playing FMOD events
		/// </summary>
		/// <returns>Task that completes when all audio is stopped</returns>
		Task<bool> StopAllAsync();
	}

	public class FmodAudioService : MonoService<IFmodAudioService>, IFmodAudioService
	{
		private readonly Dictionary<string, EventInstance> _activeEvents = new();
		private readonly Dictionary<EventInstance, FmodAudioRequest> _eventRequests = new();
		private readonly Queue<FmodAudioRequest> _requestQueue = new();
		private FmodAudioBankLoader _fmodAudioBankLoader;
		private bool _processingQueue;

		protected override void Awake()
		{
			base.Awake();

			if (_fmodAudioBankLoader == null)
			{
				_fmodAudioBankLoader = GetComponent<FmodAudioBankLoader>();
			}

			if (_fmodAudioBankLoader != null)
			{
				_fmodAudioBankLoader.OnBankLoaded += OnBanksLoaded;

				// If banks are already loaded, process the queue immediately
				if (_fmodAudioBankLoader.BankLoaded)
				{
					OnBanksLoaded();
				}
			}
			else
			{
				Debug.LogError("FmodAudioService: No FmodAudioBankLoader assigned!");
			}

			ServiceReady();
		}

		private void Update()
		{
			// Check all non-looping playing events for completion
			var eventsToRemove = new List<EventInstance>();

			foreach (var kvp in _eventRequests)
			{
				var eventInstance = kvp.Key;
				var request = kvp.Value;

				// Skip looping events
				if (request.Loop)
				{
					continue;
				}

				// Check playback state
				eventInstance.getPlaybackState(out var state);

				// If the event has stopped or failed, mark it as completed
				if (state is not (PLAYBACK_STATE.STOPPED or PLAYBACK_STATE.STOPPING))
				{
					continue;
				}

				eventsToRemove.Add(eventInstance);
				eventInstance.release();
				request.CompletionSource.TrySetResult(true);
			}

			// Clean up completed events
			foreach (var eventInstance in eventsToRemove)
			{
				var request = _eventRequests[eventInstance];
				_eventRequests.Remove(eventInstance);

				// Also remove from active events
				string eventPath = null;
				foreach (var kvp in _activeEvents)
				{
					if (kvp.Value.handle != eventInstance.handle)
					{
						continue;
					}

					eventPath = kvp.Key;
					break;
				}

				if (eventPath != null)
				{
					_activeEvents.Remove(eventPath);
				}
			}
		}

		private void OnDestroy()
		{
			// Stop and release all active events
			foreach (var eventInstance in _activeEvents.Values)
			{
				eventInstance.stop(STOP_MODE.IMMEDIATE);
				eventInstance.release();
			}

			_activeEvents.Clear();
			_eventRequests.Clear();

			// Clear the queue
			_requestQueue.Clear();

			// Remove event handler
			if (_fmodAudioBankLoader != null)
			{
				_fmodAudioBankLoader.OnBankLoaded -= OnBanksLoaded;
			}
		}

		public bool IsReady => _fmodAudioBankLoader != null && _fmodAudioBankLoader.BankLoaded;

		public Task<bool> PlayAsync(string eventPath, float volume = 1.0f, bool loop = false)
		{
			// Create a request for playing audio
			var request = new FmodAudioRequest(FmodAudioRequest.RequestType.Play, eventPath)
			{
				Volume = volume,
				Loop = loop,
				Is3D = false // This is a 2D sound
			};

			// Queue the request
			_requestQueue.Enqueue(request);

			// Start processing the queue if not already processing
			if (!_processingQueue)
			{
				ProcessQueue();
			}

			return request.CompletionSource.Task;
		}

		public Task<bool> PlayAt3DPositionAsync(string eventPath, Vector3 position, float volume = 1.0f,
			bool loop = false)
		{
			// Create a request for playing 3D audio
			var request = new FmodAudioRequest(FmodAudioRequest.RequestType.Play, eventPath)
			{
				Volume = volume,
				Loop = loop,
				Is3D = true,
				Position = position
			};

			// Queue the request
			_requestQueue.Enqueue(request);

			// Start processing the queue if not already processing
			if (!_processingQueue)
			{
				ProcessQueue();
			}

			return request.CompletionSource.Task;
		}

		public Task<bool> StopAsync(string eventPath)
		{
			// Create a request for stopping audio
			var request = new FmodAudioRequest(FmodAudioRequest.RequestType.Stop, eventPath);

			// Queue the request
			_requestQueue.Enqueue(request);

			// Start processing the queue if not already processing
			if (!_processingQueue)
			{
				ProcessQueue();
			}

			return request.CompletionSource.Task;
		}

		public async Task<bool> StopAllAsync()
		{
			var tasks = new List<Task<bool>>();

			// Create stop requests for all active events
			foreach (var eventPath in new List<string>(_activeEvents.Keys))
			{
				tasks.Add(StopAsync(eventPath));
			}

			// Wait for all stops to complete
			if (tasks.Count > 0)
			{
				await Task.WhenAll(tasks);
			}

			return true;
		}

		private void OnBanksLoaded()
		{
			Debug.Log("FmodAudioService: Banks loaded, processing queue...");

			// Log all loaded banks and events
			FmodDebugUtils.ListAllLoadedBanksAndEvents();

			ProcessQueue();
		}

		private async void ProcessQueue()
		{
			if (_processingQueue)
			{
				return;
			}

			_processingQueue = true;

			while (_requestQueue.Count > 0)
			{
				if (!IsReady)
				{
					// Wait for banks to load
					await Task.Yield();
					continue;
				}

				var request = _requestQueue.Dequeue();

				try
				{
					switch (request.Type)
					{
						case FmodAudioRequest.RequestType.Play:
							HandlePlayRequest(request);
							break;

						case FmodAudioRequest.RequestType.Stop:
							HandleStopRequest(request);
							break;
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"FmodAudioService: Error processing audio request: {ex.Message}");
					request.CompletionSource.TrySetException(ex);
				}
			}

			_processingQueue = false;
		}

		private void HandlePlayRequest(FmodAudioRequest request)
		{
			// Check if the event exists first
			if (!FmodDebugUtils.DoesEventExist(request.EventPath))
			{
				Debug.LogError($"FmodAudioService: Event not found: '{request.EventPath}'");
				request.CompletionSource.TrySetException(new Exception($"Event not found: {request.EventPath}"));
				return;
			}

			// Create and configure the event instance
			var eventInstance = RuntimeManager.CreateInstance(request.EventPath);

			// Set volume
			eventInstance.setVolume(request.Volume);

			// Handle 3D positioning if needed
			if (request.Is3D)
			{
				// Convert Unity position to FMOD 3D attributes
				var attributes = RuntimeUtils.To3DAttributes(request.Position);
				eventInstance.set3DAttributes(attributes);
			}

			// Set looping if needed
			if (request.Loop)
			{
				try
				{
					// Try to set loop parameter if it exists
					eventInstance.setParameterByName("Loop", 1.0f);
				}
				catch
				{
					// Otherwise the event might be configured to loop in FMOD Studio
					Debug.Log($"No 'Loop' parameter for {request.EventPath}, assuming it's configured in FMOD Studio");
				}
			}

			// Start the event
			eventInstance.start();

			// Store the event instance for monitoring and future stopping
			_activeEvents[request.EventPath] = eventInstance;
			request.EventInstance = eventInstance;
			_eventRequests[eventInstance] = request;

			// For all sounds, consider it "complete" when it starts successfully
			// For non-looping sounds, the completion will be set in Update when playback finishes
			request.CompletionSource.TrySetResult(true);
		}

		private void HandleStopRequest(FmodAudioRequest request)
		{
			if (_activeEvents.TryGetValue(request.EventPath, out var eventInstance))
			{
				// Get the original request to complete it
				if (_eventRequests.TryGetValue(eventInstance, out var originalRequest))
				{
					// Stop the event (with fade if needed)
					eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
					eventInstance.release();

					// Remove from tracking collections
					_activeEvents.Remove(request.EventPath);
					_eventRequests.Remove(eventInstance);

					// Complete the original play request if it's still pending
					originalRequest.CompletionSource.TrySetResult(true);
				}

				// Complete the stop request
				request.CompletionSource.TrySetResult(true);
			}
			else
			{
				// Event not found, still consider the stop request successful
				request.CompletionSource.TrySetResult(true);
			}
		}
	}
}