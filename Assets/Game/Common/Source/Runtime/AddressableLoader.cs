using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Common
{
	public static class AddressableLoader
	{
		/// <summary>
		///     Asynchronously loads an Addressable asset of type TObject using an AssetReference.
		///     It awaits the loading operation internally and returns the completed handle.
		///     IMPORTANT: The caller is responsible for checking the returned handle's Status and Result,
		///     and *must* release the handle using Addressables.Release(handle) when done.
		/// </summary>
		/// <typeparam name="TObject">The type of asset to load (must inherit from UnityEngine.Object).</typeparam>
		/// <param name="assetReference">The AssetReference pointing to the asset.</param>
		/// <param name="context">Optional context object for logging.</param>
		/// <returns>
		///     A Task resulting in the AsyncOperationHandle
		///     <TObject>
		///         after the load attempt completes.
		///         Check handle.IsValid() and handle.Status before use. Release the handle when finished.
		///         Returns a Task resulting in a default/invalid handle if the AssetReference is invalid.
		/// </returns>
		public static async Task<AsyncOperationHandle<TObject>> LoadAssetAsync<TObject>(
			AssetReference assetReference,
			Object context = null) where TObject : Object
		{
			if (assetReference == null || !assetReference.RuntimeKeyIsValid())
			{
				Debug.LogError("Asset reference provided is null or not valid.", context);
				return default;
			}

			var loadHandle = assetReference.LoadAssetAsync<TObject>();
			await loadHandle.Task.WithErrorHandling(
				errorHandler: ex => {
					var assetKey = assetReference.RuntimeKeyIsValid() 
						? assetReference.AssetGUID 
						: "invalid";
					
					Debug.LogError($"[AddressableLoader] Failed awaiting load for asset '{assetKey}'. Context: '{context?.name ?? "null"}'. Exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", context);
				},
				rethrowException: false
			);
			
			if (!loadHandle.IsValid() || loadHandle.Status != AsyncOperationStatus.Succeeded)
			{
				Debug.LogError($"[AddressableLoader] Failed. Handle Status: {loadHandle.Status}. OpException: {loadHandle.OperationException}", context);
				return loadHandle;
			}

			if (loadHandle.Result == null)
			{
				Debug.LogWarning($"[AddressableLoader] Succeeded according to status, but Result is null. AssetKey: {assetReference.RuntimeKey}", context);
			}

			// The caller MUST release this handle later.
			return loadHandle;
		}
	}
}