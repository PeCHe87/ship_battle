using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace InControl
{
	#if UNITY_EDITOR
	using UnityEditor;
	#endif
	#if UNITY_6000_5_OR_NEWER
	using InstanceId = UnityEngine.EntityId;
	#else
	using InstanceId = Int32;
	#endif


	// ReSharper disable StaticMemberInGenericType
	public abstract class SingletonMonoBehavior<TComponent> : MonoBehaviour
		where TComponent : MonoBehaviour
	{
		static TComponent instance;
		static bool hasInstance;
		static InstanceId instanceId;
		static readonly object lockObject = new();


		public static TComponent Instance
		{
			get
			{
				lock (lockObject)
				{
					if (hasInstance)
					{
						return instance;
					}

					instance = FindFirstInstance();
					if (!instance)
					{
						throw new Exception( "The instance of singleton component " + typeof(TComponent) + " was requested, but it doesn't appear to exist in the scene." );
					}

					hasInstance = true;
					instanceId = IdOf( instance );
					return instance;
				}
			}
		}


		/// <summary>
		///     Returns true if the object is NOT the singleton instance and should exit early from doing any redundant work.
		///     It will also log a warning if called from another instance in the editor during play mode.
		/// </summary>
		protected bool EnforceSingleton
		{
			get
			{
				if (IdOf( this ) == IdOf( Instance ))
				{
					return false;
				}

				if (Application.isPlaying)
				{
					enabled = false;
				}

				return true;
			}
		}


		/// <summary>
		///     Returns true if the object is the singleton instance.
		/// </summary>
		protected bool IsTheSingleton
		{
			get
			{
				lock (lockObject)
				{
					// We compare against the last known instance ID because Unity destroys objects
					// in random order and this may get called during teardown when the instance is
					// already gone.
					return IdOf( this ) == instanceId;
				}
			}
		}


		/// <summary>
		///     Returns true if the object is not the singleton instance.
		/// </summary>
		protected bool IsNotTheSingleton
		{
			get
			{
				lock (lockObject)
				{
					// We compare against the last known instance ID because Unity destroys objects
					// in random order and this may get called during teardown when the instance is
					// already gone.
					return IdOf( this ) != instanceId;
				}
			}
		}


		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual void Awake()
		{
			if (Application.isPlaying && Instance)
			{
				if (IdOf( this ) != instanceId)
				{
					#if UNITY_EDITOR
					Debug.LogWarning( "A redundant instance (" + name + ") of singleton " + typeof(TComponent) + " is present in the scene.", this );
					EditorGUIUtility.PingObject( this );
					#endif
					enabled = false;
				}

				// This might be unnecessary, but just to be safe, we do it anyway.
				foreach (var redundantInstance in FindInstances().Where( o => IdOf( o ) != instanceId ))
				{
					redundantInstance.enabled = false;
				}
			}
		}


		// ReSharper disable once VirtualMemberNeverOverridden.Global
		protected virtual void OnDestroy()
		{
			lock (lockObject)
			{
				if (IdOf( this ) == instanceId)
				{
					hasInstance = false;
				}
			}
		}


		static TComponent[] FindInstances()
		{
			#if UNITY_6000_5_OR_NEWER
			var objects = FindObjectsByType<TComponent>();
			#elif UNITY_2023_1_OR_NEWER
			var objects = FindObjectsByType<TComponent>( FindObjectsSortMode.None );
			#else
			// FindObjectsByType exists in 2021.3.18+ and 2022.2.5+, but no define can
			// detect the patch version, so fall back to FindObjectsOfType before 2023.1.
			var objects = FindObjectsOfType<TComponent>();
			#endif
			Array.Sort( objects, ( a, b ) => a.transform.GetSiblingIndex().CompareTo( b.transform.GetSiblingIndex() ) );
			return objects;
		}


		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		static InstanceId IdOf( MonoBehaviour behaviour )
		{
			#if UNITY_6000_5_OR_NEWER
			return behaviour.GetEntityId();
			#else
			return behaviour.GetInstanceID();
			#endif
		}


		static TComponent FindFirstInstance()
		{
			var objects = FindInstances();
			return objects.Length > 0 ? objects[0] : null;
		}
	}
}