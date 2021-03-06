﻿/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Reflection;

using Nancy;
using Nancy.Hosting.Self;
using log4net;

using DOL.GS;
using DOL.Events;
using DOL.GS.ServerProperties;
using DOL.Database;

namespace DOLNancyWeb
{
	/// <summary>
	/// Embedded Web Server Init Class.
	/// </summary>
	public static class DOLNancyWebInit
	{
		#region ServerProperties
		/// <summary>
		/// Enable Nancy Web Server, if this is set to false embedded server won't be started.
		/// </summary>
		[ServerProperty("dolnancyweb", "enable_web_server", "Enable/Disable Embedded Nancy Web Server", true)]
		public static bool ENABLE_WEB_SERVER;
		
		/// <summary>
		/// Embedded Nancy Web Server Listen URI
		/// http://{Host|IP}:{Port}
		/// </summary>
		[ServerProperty("dolnancyweb", "web_server_listen_uri", "Embedded Nancy Web Server Listen URI", "http://localhost:10200")]
		public static string WEB_SERVER_LISTEN_URI;
		
		/// <summary>
		/// Embedded Nancy Web cookie Encryption Secret
		/// </summary>
		[ServerProperty("dolnancyweb", "web_server_cookie_crypt_secret", "Embedded Web Server Cookie Encryption Secret (If Empty will be auto set on first start)", "")]
		public static string WEB_SERVER_COOKIE_CRYPT_SECRET;
		
		/// <summary>
		/// Embedded Nancy Web cookie Encryption Hash Secret
		/// </summary>
		[ServerProperty("dolnancyweb", "web_server_cookie_crypt_hash_secret", "Embedded Web Server Cookie Encryption Hash Secret (If Empty will be auto set on first start)", "")]
		public static string WEB_SERVER_COOKIE_CRYPT_HASH_SECRET;
		#endregion

		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		#region Members
		/// <summary>
		/// Define Self Hosted Web Server
		/// </summary>
		private static NancyHost m_embeddedWebServer = null;
		#endregion
		
		#region Startup/Shutdown
		/// <summary>
		/// Startup Event For Embedded Web Server Start.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		[ScriptLoadedEvent]
		public static void OnServerStart(DOLEvent e, object sender, EventArgs arguments)
		{
			var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_+.!?*$#{}[]|@";
			var secret = new char[64];
			
			// Init Crypto Secret
			if (string.IsNullOrEmpty(DOLNancyWebInit.WEB_SERVER_COOKIE_CRYPT_SECRET))
			{
				for (int i = 0; i < secret.Length; i++)
					secret[i] = chars[Util.CryptoNextInt(chars.Length)];
				
				WEB_SERVER_COOKIE_CRYPT_SECRET = new string(secret);
				ServerProperty property = GameServer.Database.SelectObject<ServerProperty>("`Key` = 'web_server_cookie_crypt_secret'");
				if (property != null)
				{
					property.Value = WEB_SERVER_COOKIE_CRYPT_SECRET;
					GameServer.Database.SaveObject(property);
				}
			}
			
			// Init Crypto Hash Secret
			if (string.IsNullOrEmpty(DOLNancyWebInit.WEB_SERVER_COOKIE_CRYPT_HASH_SECRET))
			{
				for (int i = 0; i < secret.Length; i++)
					secret[i] = chars[Util.CryptoNextInt(chars.Length)];
				
				WEB_SERVER_COOKIE_CRYPT_HASH_SECRET = new string(secret);
				ServerProperty property = GameServer.Database.SelectObject<ServerProperty>("`Key` = 'web_server_cookie_crypt_hash_secret'");
				if (property != null)
				{
					property.Value = WEB_SERVER_COOKIE_CRYPT_HASH_SECRET;
					GameServer.Database.SaveObject(property);
				}
			}
			
			if (DOLNancyWebInit.ENABLE_WEB_SERVER && m_embeddedWebServer == null)
			{
				try
				{
					HostConfiguration hostConfigs = new HostConfiguration();
					hostConfigs.UnhandledExceptionCallback = UnHandledException;
					hostConfigs.UrlReservations.CreateAutomatically = true;
					m_embeddedWebServer = new NancyHost(new Uri(DOLNancyWebInit.WEB_SERVER_LISTEN_URI), new DOLNancyBootstrapper(), hostConfigs);
					m_embeddedWebServer.Start();
					
				}
				catch (Exception ex)
				{
					if (log.IsErrorEnabled)
						log.ErrorFormat("Error While Starting Embedded DOL Nancy Web Server : {0}", ex);
				}
			}
			else
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Configuration Will not start Embedded Web Server : Enable - {0}, Host - {1}", DOLNancyWebInit.ENABLE_WEB_SERVER, m_embeddedWebServer);
			}
		}
		
		/// <summary>
		/// Shutdown Event For Embedded Web Server Stop
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		[ScriptUnloadedEvent]
		public static void OnServerStop(DOLEvent e, object sender, EventArgs arguments)
		{
			if (m_embeddedWebServer != null)
			{
				try
				{
					m_embeddedWebServer.Stop();
				}
				catch (Exception ex)
				{
					if (log.IsErrorEnabled)
						log.ErrorFormat("Error While Stopping Embedded Nancy Web Server : {0}", ex);
				}
			}
		}
		
		/// <summary>
		/// Method To Display UnHandled Exception From Nancy Process
		/// </summary>
		/// <param name="e"></param>
		public static void UnHandledException(Exception e)
		{
			// ignore network exception...
			if (e is System.Net.HttpListenerException && e.TargetSite.Name.Equals("Write"))
				return;

			if (log.IsWarnEnabled)
				log.WarnFormat("Exception in DOL Nancy Embedded WebServer : {0}", e);
		}
		#endregion
		
	}
}
