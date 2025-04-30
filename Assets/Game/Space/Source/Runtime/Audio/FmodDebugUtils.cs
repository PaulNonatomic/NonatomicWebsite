using System;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using Debug = UnityEngine.Debug;

namespace Game.Space.Audio
{
	public class FmodDebugUtils
	{
		/// <summary>
		///     Lists all loaded banks and their events to the Unity console
		/// </summary>
		public static void ListAllLoadedBanksAndEvents()
		{
			Bank[] banks;

			// Get all loaded banks
			RuntimeManager.StudioSystem.getBankList(out banks);

			Debug.Log($"==== FMOD: {banks.Length} banks loaded ====");

			for (var i = 0; i < banks.Length; i++)
			{
				// Get bank info
				banks[i].getPath(out var bankPath);
				banks[i].getID(out var bankID);
				Debug.Log($"Bank {i}: {bankPath} (ID: {bankID})");

				// Get events in this bank
				EventDescription[] events; // Reasonable size
				banks[i].getEventList(out events);

				Debug.Log($"  Contains {events.Length} events:");

				for (var j = 0; j < events.Length; j++)
				{
					events[j].getPath(out var eventPath);
					Debug.Log($"    - {eventPath}");
				}
			}

			Debug.Log("==== FMOD: End of bank listing ====");
		}

		/// <summary>
		///     Checks if a specific event path exists in any loaded bank
		/// </summary>
		public static bool DoesEventExist(string eventPath)
		{
			try
			{
				EventDescription eventDesc;
				var result = RuntimeManager.StudioSystem.getEvent(eventPath, out eventDesc);
				var exists = result == RESULT.OK && eventDesc.isValid();

				if (!exists)
				{
					Debug.LogWarning($"FMOD event '{eventPath}' not found in any loaded bank. Result: {result}");
				}

				return exists;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error checking event existence: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		///     Finds similar event names to help with debugging typos
		/// </summary>
		public static List<string> FindSimilarEvents(string partialEventName)
		{
			var similarEvents = new List<string>();
			partialEventName = partialEventName.ToLower();

			Bank[] banks;

			RuntimeManager.StudioSystem.getBankList(out banks);

			for (var i = 0; i < banks.Length; i++)
			{
				EventDescription[] events;
				banks[i].getEventList(out events);

				for (var j = 0; j < events.Length; j++)
				{
					events[j].getPath(out var eventPath);

					if (eventPath.ToLower().Contains(partialEventName))
					{
						similarEvents.Add(eventPath);
					}
				}
			}

			return similarEvents;
		}
	}
}