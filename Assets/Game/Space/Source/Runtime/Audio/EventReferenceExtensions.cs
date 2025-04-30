using System;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Debug = UnityEngine.Debug;

namespace Game.Space.Audio
{
	/// <summary>
	///     Helper class to work with EventReference consistently across editor and builds
	/// </summary>
	public static class EventReferenceExtensions
	{
		// Cache for GUID to path lookups to avoid repeated FMOD calls
		private static readonly Dictionary<Guid, string> _guidToPathCache = new();

		/// <summary>
		///     Gets the event path from an EventReference, ensuring it works in both editor and runtime
		/// </summary>
		public static string GetPath(this EventReference eventRef)
		{
			if (eventRef.Guid == Guid.Empty)
			{
				Debug.LogError("EventReference has an empty GUID");
				return string.Empty;
			}

			// Check if we've already cached this path
			if (_guidToPathCache.TryGetValue(eventRef.Guid, out var cachedPath))
			{
				return cachedPath;
			}

			// Use the runtime API to get the path from the GUID
			try
			{
				var path = string.Empty;

				// Get the event description from the GUID
				RuntimeManager.StudioSystem.getEventByID(eventRef.Guid, out var eventDesc);

				if (eventDesc.isValid())
				{
					eventDesc.getPath(out path);

					// Cache the result for future lookups
					_guidToPathCache[eventRef.Guid] = path;

					return path;
				}

				Debug.LogWarning($"Could not get event path for GUID {eventRef.Guid}");
				return string.Empty;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error getting path from EventReference: {ex.Message}");
				return string.Empty;
			}
		}

		/// <summary>
		///     Checks if the EventReference is valid and can be played
		/// </summary>
		public static bool IsValid(this EventReference eventRef)
		{
			if (eventRef.Guid == Guid.Empty)
			{
				return false;
			}

			EventDescription eventDesc;
			var result = RuntimeManager.StudioSystem.getEventByID(eventRef.Guid, out eventDesc);

			return result == RESULT.OK && eventDesc.isValid();
		}
	}
}