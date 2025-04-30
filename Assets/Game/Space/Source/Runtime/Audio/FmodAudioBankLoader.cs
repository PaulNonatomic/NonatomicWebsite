using System;
using System.Threading.Tasks;
using FMODUnity;
using Game.Common;
using Nonatomic.ServiceLocator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Space.Audio
{
	public class FmodAudioBankLoader : MonoBehaviour
	{
		[SerializeField] private AssetReference _fmodBankRef;
		[SerializeField] private AssetReference _fmodStringsBankRef; // Added reference for strings bank

		private AsyncOperationHandle<TextAsset> _fmodBankHandle;
		private AsyncOperationHandle<TextAsset> _fmodStringsBankHandle;
		public bool BankLoaded { get; private set; }

		protected virtual async void Awake()
		{
			await LoadBankAsync();
			BankLoaded = true;
			OnBankLoaded?.Invoke(); // Added null conditional to prevent NullReferenceException
		}

		private void OnDestroy()
		{
			// Unload and release the main bank (your original code)
			RuntimeManager.UnloadBank(_fmodBankHandle.Result);

			if (_fmodBankHandle.IsValid())
			{
				_fmodBankHandle.Release();
			}

			// Unload and release the strings bank if it was loaded
			if (_fmodStringsBankHandle.IsValid())
			{
				RuntimeManager.UnloadBank(_fmodStringsBankHandle.Result);
				_fmodStringsBankHandle.Release();
			}

			BankLoaded = false;
		}

		public event Action OnBankLoaded;

		private async Task LoadBankAsync()
		{
			// First load the strings bank if assigned
			if (_fmodStringsBankRef != null && _fmodStringsBankRef.RuntimeKey != null)
			{
				_fmodStringsBankHandle = await AddressableLoader
					.LoadAssetAsync<TextAsset>(_fmodStringsBankRef, this)
					.WithErrorHandling();

				// Load the strings bank first
				RuntimeManager.LoadBank(_fmodStringsBankHandle.Result, true);
			}

			// Then load the main bank (your original code)
			_fmodBankHandle = await AddressableLoader
				.LoadAssetAsync<TextAsset>(_fmodBankRef, this)
				.WithErrorHandling();

			// Pass the loaded TextAsset to FMOD
			// The 'true' parameter tells FMOD to also load sample data
			RuntimeManager.LoadBank(_fmodBankHandle.Result, true);

			// Wait until all banks are loaded (as per FMOD's example)
			while (!RuntimeManager.HaveAllBanksLoaded)
			{
				await Task.Yield();
			}

			// Wait until all sample data is loaded (as per FMOD's example)
			while (RuntimeManager.AnySampleDataLoading())
			{
				await Task.Yield();
			}
		}
	}
}