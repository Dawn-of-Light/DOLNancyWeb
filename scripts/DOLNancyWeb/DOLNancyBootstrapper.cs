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

using Nancy;
using Nancy.Conventions;
using Nancy.Authentication.Forms;
using Nancy.Cryptography;
using Nancy.Session;

namespace DOLNancyWeb
{
	/// <summary>
	/// Custom Nancy Bootstrapper for DOL Module
	/// </summary>
	public class DOLNancyBootstrapper : DefaultNancyBootstrapper
	{
		/// <summary>
		/// Override Root Path with DOL Script Path.
		/// </summary>
		protected override IRootPathProvider RootPathProvider
		{
			get { return new DOLRootPathProvider(); }
		}
		
		/// <summary>
		/// Set Static File Path Convention
		/// </summary>
		/// <param name="nancyConventions"></param>
    	protected override void ConfigureConventions(NancyConventions nancyConventions)
		{
			base.ConfigureConventions(nancyConventions);
			
			nancyConventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("static", @"static"));
		}
    	
    	/// <summary>
    	/// Add Form Authentication to Bootstrap
    	/// </summary>
    	/// <param name="container"></param>
    	/// <param name="pipelines"></param>
		protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
		{
			base.ApplicationStartup(container, pipelines);
			
			// Enable Cookie Based Session
			CookieBasedSessions.Enable(pipelines);
			
			// Set Cryptography Configuration
			var cryptographyConfiguration = new CryptographyConfiguration(
				new RijndaelEncryptionProvider(new PassphraseKeyGenerator(DOLNancyWebInit.WEB_SERVER_COOKIE_CRYPT_SECRET, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
				new DefaultHmacProvider(new PassphraseKeyGenerator(DOLNancyWebInit.WEB_SERVER_COOKIE_CRYPT_HASH_SECRET, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));

			// Set Form Authentication Configuration
			var formsAuthConfiguration = new FormsAuthenticationConfiguration
			{
				CryptographyConfiguration = cryptographyConfiguration,
				RedirectUrl = "~/login",
				UserMapper = container.Resolve<IUserMapper>(),
			};
			
			FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
		}
		
		/// <summary>
		/// Register DOL User Mapper for Authentication Resolve.
		/// </summary>
		/// <param name="container"></param>
		protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
		{
			base.ConfigureApplicationContainer(container);
			container.Register<IUserMapper, DOLUserMapper>();
		}
	}
}
