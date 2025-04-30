using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Intro.Player
{
	namespace Game.Intro.Player
	{
		[Serializable]
		public class VisemeMouthTexture
		{
			public string VisemeKey;
			public Texture2D MouthTexture;
		}

		public class VisemeTextureUpdater : MonoBehaviour
		{
			[SerializeField] private string _initialVismeKey;
			[SerializeField] private Renderer _targetRenderer;
			[SerializeField] private int _materialIndex;
			[SerializeField] private string _texturePropertyName = "_BaseMap";
			[SerializeField] private Vector2Int _mouthPosition;
			[SerializeField] private List<VisemeMouthTexture> _visemeMouthTextures = new();

			private readonly Dictionary<string, Texture2D> _visemeTextureLookup = new();
			private string _currentViseme = string.Empty;
			private Texture2D _originalTexture;
			private Texture2D _workingTexture;

			private void Awake()
			{
				InitializeVisemeDictionary();
				InitializeTexture();
				SetViseme(_initialVismeKey);
			}

			private void OnDestroy()
			{
				ResetTexture();

				if (_workingTexture != null)
				{
					Destroy(_workingTexture);
				}
			}

			/// <summary>
			///     Initializes the viseme dictionary for faster lookup.
			/// </summary>
			private void InitializeVisemeDictionary()
			{
				foreach (var visemeTexture in _visemeMouthTextures)
				{
					if (!string.IsNullOrEmpty(visemeTexture.VisemeKey) && visemeTexture.MouthTexture != null)
					{
						_visemeTextureLookup[visemeTexture.VisemeKey] = visemeTexture.MouthTexture;
					}
				}
			}

			/// <summary>
			///     Initializes the texture and validates requirements.
			/// </summary>
			private void InitializeTexture()
			{
				if (_targetRenderer == null)
				{
					Debug.LogError("No target renderer assigned!");
					return;
				}

				// Make sure materialIndex is valid
				if (_materialIndex >= _targetRenderer.materials.Length)
				{
					Debug.LogError("Material index out of range!");
					return;
				}

				// Get the original texture
				_originalTexture =
					_targetRenderer.materials[_materialIndex].GetTexture(_texturePropertyName) as Texture2D;

				if (_originalTexture != null)
				{
					// Create a working copy of the texture
					InitializeWorkingTexture();
				}
				else
				{
					Debug.LogError($"No texture found at property {_texturePropertyName}!");
				}
			}

			/// <summary>
			///     Creates a working copy of the original texture.
			/// </summary>
			private void InitializeWorkingTexture()
			{
				// Create a new texture with the same dimensions as the original
				_workingTexture = new(
					_originalTexture.width,
					_originalTexture.height,
					_originalTexture.format,
					_originalTexture.mipmapCount > 1
				);

				// Copy the original pixels to our working texture
				Graphics.CopyTexture(_originalTexture, _workingTexture);

				// Apply the working texture to the material
				_targetRenderer.materials[_materialIndex].SetTexture(_texturePropertyName, _workingTexture);
			}

			/// <summary>
			///     Sets the current viseme and updates the texture.
			/// </summary>
			/// <param name="visemeKey">The key of the viseme to display.</param>
			/// <returns>True if the viseme was found and applied, false otherwise.</returns>
			[Button]
			public bool SetViseme(string visemeKey)
			{
				if (_workingTexture == null || _visemeTextureLookup.Count == 0)
				{
					Debug.LogError("Texture or viseme dictionary not initialized!");
					return false;
				}

				// If it's the same viseme, no need to update
				if (_currentViseme == visemeKey)
				{
					//return true;
				}

				// Check if the viseme exists
				if (!_visemeTextureLookup.TryGetValue(visemeKey, out var mouthTexture))
				{
					Debug.LogWarning($"Viseme '{visemeKey}' not found!");
					return false;
				}

				// First restore the original base texture (to clean previous mouth)
				RestoreOriginalMouthArea();

				// Draw the new mouth texture at the specified position
				DrawMouthTexture(mouthTexture);

				// Update the current viseme
				_currentViseme = visemeKey;

				return true;
			}

			/// <summary>
			///     Restores the original texture data in the mouth area.
			/// </summary>
			[Button]
			private void RestoreOriginalMouthArea()
			{
				if (string.IsNullOrEmpty(_currentViseme))
				{
					return;
				}

				if (!_visemeTextureLookup.TryGetValue(_currentViseme, out var currentMouthTexture))
				{
					return;
				}

				var mouthWidth = currentMouthTexture.width;
				var mouthHeight = currentMouthTexture.height;
				var originalMouthPixels =
					_originalTexture.GetPixels(_mouthPosition.x, _mouthPosition.y, mouthWidth, mouthHeight);

				_workingTexture.SetPixels(_mouthPosition.x, _mouthPosition.y, mouthWidth, mouthHeight,
					originalMouthPixels);
				_workingTexture.Apply();
			}

			/// <summary>
			///     Draws the mouth texture onto the character's texture.
			/// </summary>
			/// <param name="mouthTexture">The mouth texture to draw.</param>
			private void DrawMouthTexture(Texture2D mouthTexture)
			{
				var mouthPixels = mouthTexture.GetPixels();

				_workingTexture.SetPixels(
					_mouthPosition.x, _mouthPosition.y,
					mouthTexture.width, mouthTexture.height,
					mouthPixels);

				_workingTexture.Apply();
			}

			/// <summary>
			///     Resets to the original texture.
			/// </summary>
			public void ResetTexture()
			{
				if (_originalTexture == null || _targetRenderer == null)
				{
					return;
				}

				_targetRenderer.materials[_materialIndex].SetTexture(_texturePropertyName, _originalTexture);
				_currentViseme = string.Empty;
			}
		}
	}
}