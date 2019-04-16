﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Steamworks
{
	/// <summary>
	/// Interface which provides access to a range of miscellaneous utility functions
	/// </summary>
	public static class SteamUtils
	{
		static Internal.ISteamUtils _internal;
		internal static Internal.ISteamUtils Internal
		{
			get
			{
				if ( _internal == null )
					_internal = new Internal.ISteamUtils();

				return _internal;
			}
		}

		internal static void InstallEvents()
		{
			new Event<IPCountry_t>( x => OnIpCountryChanged?.Invoke() );
			new Event<LowBatteryPower_t>( x => OnLowBatteryPower?.Invoke( x.MinutesBatteryLeft ) );
			new Event<SteamShutdown_t>( x => OnSteamShutdown?.Invoke() );
			new Event<GamepadTextInputDismissed_t>( x => OnGamepadTextInputDismissed?.Invoke( x.Submitted ) );
		}

		/// <summary>
		/// The country of the user changed
		/// </summary>
		public static event Action OnIpCountryChanged;

		/// <summary>
		/// Fired when running on a laptop and less than 10 minutes of battery is left, fires then every minute
		/// The parameter is the number of minutes left
		/// </summary>
		public static event Action<int> OnLowBatteryPower;

		/// <summary>
		/// Called when Steam wants to shutdown
		/// </summary>
		public static event Action OnSteamShutdown;

		/// <summary>
		/// Big Picture gamepad text input has been closed. Parameter is true if text was submitted, false if cancelled etc.
		/// </summary>
		public static event Action<bool> OnGamepadTextInputDismissed;

		/// <summary>
		/// Returns the number of seconds since the application was active
		/// </summary>
		public static uint SecondsSinceAppActive => Internal.GetSecondsSinceAppActive();

		/// <summary>
		/// Returns the number of seconds since the user last moved the mouse etc
		/// </summary>
		public static uint SecondsSinceComputerActive => Internal.GetSecondsSinceComputerActive();

		// the universe this client is connecting to
		public static Universe ConnectedUniverse => Internal.GetConnectedUniverse();

		/// <summary>
		/// Steam server time.  Number of seconds since January 1, 1970, GMT (i.e unix time)
		/// </summary>
		public static DateTime SteamServerTime => Utility.Epoch.ToDateTime( Internal.GetServerRealTime() );

		/// <summary>
		/// returns the 2 digit ISO 3166-1-alpha-2 format country code this client is running in (as looked up via an IP-to-location database)
		/// e.g "US" or "UK".
		/// </summary>
		public static string IpCountry => Internal.GetIPCountry();

		/// <summary>
		/// returns true if the image exists, and the buffer was successfully filled out
		/// results are returned in RGBA format
		/// the destination buffer size should be 4 * height * width * sizeof(char)
		/// </summary>
		public static bool GetImageSize( int image, out uint width, out uint height )
		{
			width = 0;
			height = 0;
			return Internal.GetImageSize( image, ref width, ref height );
		}

		/// <summary>
		/// returns the image in RGBA format
		/// </summary>
		public static Image? GetImage( int image )
		{
			var i = new Image();

			if ( !GetImageSize( image, out i.Width, out i.Height ) )
				return null;

			var size = i.Width * i.Height * 4;

			var buf = Helpers.TakeBuffer( (int) size );

			if ( !Internal.GetImageRGBA( image, buf, (int)size ) )
				return null;

			i.Data = new byte[size];
			Array.Copy( buf, 0, i.Data, 0, size );
			return i;
		}

		/// <summary>
		/// Returns true if we're using a battery (ie, a laptop not plugged in)
		/// </summary>
		public static bool UsingBatteryPower => Internal.GetCurrentBatteryPower() != 255;

		/// <summary>
		/// Returns battery power [0-1]
		/// </summary>
		public static float CurrentBatteryPower => Math.Min( Internal.GetCurrentBatteryPower() / 100, 1.0f );

		/// <summary>
		/// returns the appID of the current process
		/// </summary>
		public static AppId AppId => Internal.GetAppID();

		static NotificationPosition overlayNotificationPosition = NotificationPosition.BottomRight;

		/// <summary>
		/// Sets the position where the overlay instance for the currently calling game should show notifications.
		/// This position is per-game and if this function is called from outside of a game context it will do nothing.
		/// </summary>
		public static NotificationPosition OverlayNotificationPosition
		{
			get => overlayNotificationPosition;

			set
			{
				overlayNotificationPosition = value;
				Internal.SetOverlayNotificationPosition( value );
			}
		}

		/// <summary>
		/// Returns true if the overlay is running and the user can access it. The overlay process could take a few seconds to
		/// start and hook the game process, so this function will initially return false while the overlay is loading.
		/// </summary>
		public static bool IsOverlayEnabled => Internal.IsOverlayEnabled();

		/// <summary>
		/// Normally this call is unneeded if your game has a constantly running frame loop that calls the 
		/// D3D Present API, or OGL SwapBuffers API every frame.
		///
		/// However, if you have a game that only refreshes the screen on an event driven basis then that can break 
		/// the overlay, as it uses your Present/SwapBuffers calls to drive it's internal frame loop and it may also
		/// need to Present() to the screen any time an even needing a notification happens or when the overlay is
		/// brought up over the game by a user.  You can use this API to ask the overlay if it currently need a present
		/// in that case, and then you can check for this periodically (roughly 33hz is desirable) and make sure you
		/// refresh the screen with Present or SwapBuffers to allow the overlay to do it's work.
		/// </summary>
		public static bool DoesOverlayNeedPresent => Internal.BOverlayNeedsPresent();

		/// <summary>
		/// Asynchronous call to check if an executable file has been signed using the public key set on the signing tab
		/// of the partner site, for example to refuse to load modified executable files.  
		/// </summary>
		public static async Task<CheckFileSignature> CheckFileSignature( string filename )
		{
			var r = await Internal.CheckFileSignature( filename );

			if ( !r.HasValue )
			{
				throw new System.Exception( "Something went wrong" );
			}

			return r.Value.CheckFileSignature;
		}

		/// <summary>
		/// Activates the Big Picture text input dialog which only supports gamepad input
		/// </summary>
		public static bool ShowGamepadTextInput( GamepadTextInputMode inputMode, GamepadTextInputLineMode lineInputMode, string description, int maxChars, string existingText = "" )
		{
			return Internal.ShowGamepadTextInput( inputMode, lineInputMode, description, (uint)maxChars, existingText );
		}

		/// <summary>
		/// Returns previously entered text
		/// </summary>
		public static string GetEnteredGamepadText()
		{
			var len = Internal.GetEnteredGamepadTextLength();
			if ( len == 0 ) return string.Empty;

			var sb = Helpers.TakeStringBuilder();
			if ( !Internal.GetEnteredGamepadTextInput( sb, len ) )
				return string.Empty;

			return sb.ToString();
		}

		/// <summary>
		/// returns the language the steam client is running in, you probably want 
		/// Apps.CurrentGameLanguage instead, this is for very special usage cases
		/// </summary>
		public static string SteamUILanguage => Internal.GetSteamUILanguage();

		/// <summary>
		/// returns true if Steam itself is running in VR mode
		/// </summary>
		public static bool IsSteamRunningInVR => Internal.IsSteamRunningInVR();

		/// <summary>
		/// Sets the inset of the overlay notification from the corner specified by SetOverlayNotificationPosition
		/// </summary>
		public static void SetOverlayNotificationInset( int x, int y )
		{
			Internal.SetOverlayNotificationInset( x, y );
		}

		/// <summary>
		/// returns true if Steam and the Steam Overlay are running in Big Picture mode
		/// Games much be launched through the Steam client to enable the Big Picture overlay. During development,
		/// a game can be added as a non-steam game to the developers library to test this feature
		/// </summary>
		public static bool IsSteamInBigPictureMode => Internal.IsSteamInBigPictureMode();


		/// <summary>
		/// ask SteamUI to create and render its OpenVR dashboard
		/// </summary>
		public static void StartVRDashboard() => Internal.StartVRDashboard();

		/// <summary>
		/// Set whether the HMD content will be streamed via Steam In-Home Streaming
		/// If this is set to true, then the scene in the HMD headset will be streamed, and remote input will not be allowed.
		/// If this is set to false, then the application window will be streamed instead, and remote input will be allowed.
		/// The default is true unless "VRHeadsetStreaming" "0" is in the extended appinfo for a game.
		/// (this is useful for games that have asymmetric multiplayer gameplay)
		/// </summary>
		public static bool VrHeadsetStreaming
		{
			get => Internal.IsVRHeadsetStreamingEnabled();
			
			set
			{
				Internal.SetVRHeadsetStreamingEnabled( value );
			}
		}


		internal static bool IsCallComplete( SteamAPICall_t call, out bool failed )
		{
			failed = false;
			return Internal.IsAPICallCompleted( call, ref failed );
		}

		internal static T? GetResult<T>( SteamAPICall_t call ) where T : struct, ISteamCallback
		{
			var t = new T();

			var size = t.GetStructSize();
			var ptr = Marshal.AllocHGlobal( size );

			try
			{
				bool failed = false;

				if ( !Internal.GetAPICallResult( call, ptr, size, t.GetCallbackId(), ref failed ) )
					return null;

				if ( failed )
					return null;

				t = (T)t.Fill( ptr );

				return t;
			}
			finally
			{
				Marshal.FreeHGlobal( ptr );
			}
		}
	}
}