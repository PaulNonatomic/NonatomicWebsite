using System.Threading.Tasks;
using FMOD.Studio;
using UnityEngine;

namespace Game.Space.Audio
{
	public class FmodAudioRequest
	{
		public enum RequestType
		{
			Play,
			Stop
		}

		public FmodAudioRequest(RequestType type, string eventPath, TaskCompletionSource<bool> completionSource = null)
		{
			Type = type;
			EventPath = eventPath;
			CompletionSource = completionSource ?? new TaskCompletionSource<bool>();
		}

		public RequestType Type { get; }
		public string EventPath { get; }
		public TaskCompletionSource<bool> CompletionSource { get; }
		public float Volume { get; set; } = 1.0f;
		public bool Loop { get; set; } = false;
		public EventInstance EventInstance { get; set; }
		public bool Is3D { get; set; } = false;
		public Vector3 Position { get; set; } = Vector3.zero;
	}
}