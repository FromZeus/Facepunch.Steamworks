﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;

namespace Steamworks
{
	public static class SteamNetworking
	{
		static ISteamNetworking _internal;
		internal static ISteamNetworking Internal
		{
			get
			{
				if ( _internal == null )
				{
					_internal = new ISteamNetworking();
					_internal.Init();
				}

				return _internal;
			}
		}

		internal static void Shutdown()
		{
			_internal = null;
		}

		internal static void InstallEvents()
		{
			P2PSessionRequest_t.Install( x => OnP2PSessionRequest?.Invoke( x.SteamIDRemote ) );
			P2PSessionConnectFail_t.Install( x => OnP2PConnectionFailed?.Invoke( x.SteamIDRemote ) );
		}

		/// <summary>
		/// This SteamId wants to send you a message. You should respond by calling AcceptP2PSessionWithUser
		/// if you want to recieve their messages
		/// </summary>
		public static Action<SteamId> OnP2PSessionRequest;

		/// <summary>
		/// Called when packets can't get through to the specified user.
		/// All queued packets unsent at this point will be dropped, further attempts
		/// to send will retry making the connection (but will be dropped if we fail again).
		/// </summary>
		public static Action<SteamId> OnP2PConnectionFailed;

		/// <summary>
		/// This should be called in response to a OnP2PSessionRequest
		/// </summary>
		public static bool AcceptP2PSessionWithUser( SteamId user ) => Internal.AcceptP2PSessionWithUser( user );

		/// <summary>
		/// This should be called when you're done communicating with a user, as this will
		/// free up all of the resources allocated for the connection under-the-hood.
		/// If the remote user tries to send data to you again, a new OnP2PSessionRequest 
		/// callback will be posted
		/// </summary>
		public static bool CloseP2PSessionWithUser( SteamId user ) => Internal.CloseP2PSessionWithUser( user );

		/// <summary>
		/// Checks if a P2P packet is available to read, and gets the size of the message if there is one.
		/// </summary>
		public static bool IsP2PPacketAvailable( int channel = 0 )
		{
			uint _ = 0;
			return Internal.IsP2PPacketAvailable( ref _, channel );
		}

		/// <summary>
		/// Reads in a packet that has been sent from another user via SendP2PPacket..
		/// </summary>
		public unsafe static P2Packet? ReadP2PPacket( int channel = 0 )
		{
			uint size = 0;

			if ( !Internal.IsP2PPacketAvailable( ref size, channel ) )
				return null;

			var buffer = Helpers.TakeBuffer( (int) size );

			fixed ( byte* p = buffer )
            {
                SteamId steamid = 1;
                if ( !Internal.ReadP2PPacket( (IntPtr)p, (uint) buffer.Length, ref size, ref steamid, channel ) || size == 0 )
                    return null;

				var data = new byte[size];
				Array.Copy( buffer, 0, data, 0, size );

				return new P2Packet
				{
					SteamId = steamid,
					Data = data
				};
            }
		}

		/// <summary>
		/// Sends a P2P packet to the specified user.
		/// This is a session-less API which automatically establishes NAT-traversing or Steam relay server connections.
		/// NOTE: The first packet send may be delayed as the NAT-traversal code runs.
		/// </summary>
		public static unsafe bool SendP2PPacket( SteamId steamid, byte[] data, int length = -1, int nChannel = 0, P2PSend sendType = P2PSend.Reliable )
		{
			if ( length <= 0 )
				length = data.Length;

			fixed ( byte* p = data )
			{
				return Internal.SendP2PPacket( steamid, (IntPtr)p, (uint)length, (P2PSend)sendType, nChannel );
			}
		}


	}
}