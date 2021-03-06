﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.D.Building;

namespace MonoDevelop.D.OptionPanels
{
	/// <summary>
	/// This panel provides UI access to project independent D settings such as generic compiler configurations, library and import paths etc.
	/// </summary>
	public partial class DCompilerOptions : Gtk.Bin
	{
		private DCompilerConfiguration configuration;
		
		private Gtk.ListStore defaultLibStore = new Gtk.ListStore (typeof(string));
		private Gtk.ListStore includePathStore = new Gtk.ListStore (typeof(string));		

		private BuildArgumentOptions releaseArgumentsDialog = null;
		private BuildArgumentOptions debugArgumentsDialog = null;	
		
		public DCompilerOptions () 
		{
			this.Build ();	
			
			
			Gtk.CellRendererText textRenderer = new Gtk.CellRendererText ();
			
			tvDefaultLibs.Model = defaultLibStore;
			tvDefaultLibs.HeadersVisible = false;
			tvDefaultLibs.AppendColumn ("Library", textRenderer, "text", 0);
			
			tvIncludePaths.Model = includePathStore;
			tvIncludePaths.HeadersVisible = false;
			tvIncludePaths.AppendColumn ("Include", textRenderer, "text", 0);	
			
			releaseArgumentsDialog = new BuildArgumentOptions();
			debugArgumentsDialog = new BuildArgumentOptions();
		}
	
		public void Load (DCompilerConfiguration config)
		{
			configuration = config;
			//for now, using Executable target compiler command for all targets source compiling
			LinkTargetConfiguration targetConfig;
 			targetConfig = config.GetTargetConfiguration(DCompileTarget.Executable); 			
			txtCompiler.Text = targetConfig.Compiler;
			
			//linker targets 			
 			targetConfig = config.GetTargetConfiguration(DCompileTarget.Executable); 						
			txtConsoleAppLinker.Text = targetConfig.Linker;			
			
 			targetConfig = config.GetTargetConfiguration(DCompileTarget.ConsolelessExecutable); 						
			txtGUIAppLinker.Text = targetConfig.Linker;			
			
 			targetConfig = config.GetTargetConfiguration(DCompileTarget.SharedLibrary); 						
			txtSharedLibLinker.Text = targetConfig.Linker;
			
 			targetConfig = config.GetTargetConfiguration(DCompileTarget.StaticLibrary); 						
			txtStaticLibLinker.Text = targetConfig.Linker;
			
			releaseArgumentsDialog.Load(config, false);		
			debugArgumentsDialog.Load(config, true);				

			defaultLibStore.Clear();
			foreach (string lib in config.DefaultLibraries)
				defaultLibStore.AppendValues (lib);

			includePathStore.Clear();
			foreach(var p in config.GlobalParseCache.DirectoryPaths)
				includePathStore.AppendValues(p);
										
		}


		public bool Validate()
		{
			return true;
		}
		
		public bool Store ()
		{
			if (configuration == null)
				return false;
			
			Gtk.TreeIter iter;
			string line;
			
			//for now, using Executable target compiler command for all targets source compiling
			LinkTargetConfiguration targetConfig;
 			targetConfig = configuration.GetTargetConfiguration(DCompileTarget.Executable); 			
			targetConfig.Compiler = txtCompiler.Text;
			
			//linker targets 			
 			targetConfig = configuration.GetTargetConfiguration(DCompileTarget.Executable); 						
			targetConfig.Linker = txtConsoleAppLinker.Text;			
			
 			targetConfig = configuration.GetTargetConfiguration(DCompileTarget.ConsolelessExecutable); 						
			targetConfig.Linker = txtGUIAppLinker.Text;			
			
 			targetConfig = configuration.GetTargetConfiguration(DCompileTarget.SharedLibrary); 						
			targetConfig.Linker = txtSharedLibLinker.Text;
			
 			targetConfig = configuration.GetTargetConfiguration(DCompileTarget.StaticLibrary); 						
			targetConfig.Linker = txtStaticLibLinker.Text;
			
			releaseArgumentsDialog.Store();			
			debugArgumentsDialog.Store();					
			
			defaultLibStore.GetIterFirst (out iter);
			configuration.DefaultLibraries.Clear();
			while (defaultLibStore.IterIsValid (iter)) {
				line = (string)defaultLibStore.GetValue (iter, 0);
				configuration.DefaultLibraries.Add (line);
				defaultLibStore.IterNext (ref iter);
			}
					
			// Store new include paths
			includePathStore.GetIterFirst (out iter);
			configuration.GlobalParseCache.ParsedGlobalDictionaries.Clear();
			while (includePathStore.IterIsValid (iter)) {
				line = (string)includePathStore.GetValue (iter, 0);
				// Add it to the compiler's global parse cache
				configuration.GlobalParseCache.Add(line);
				includePathStore.IterNext (ref iter);
			}

			try
			{
				// Update parse cache immediately
				configuration.GlobalParseCache.UpdateCache();
			}
			catch (Exception ex)
			{
				LoggingService.LogError("Include path analysis error",ex);
			}
					
			return true;
		}
		
		private void ShowArgumentsDialog(bool isDebug)
		{
			BuildArgumentOptions dialog = null;
			if (isDebug)
				dialog = debugArgumentsDialog;								
			else
				dialog = releaseArgumentsDialog;								
			
			Gtk.ResponseType response;	
			response = (Gtk.ResponseType) dialog.Run ();			
			dialog.Hide();
		}
		
		protected void btnReleaseArguments_Clicked (object sender, System.EventArgs e)
		{			
			ShowArgumentsDialog(false);						
		}
		protected void btnDebugArguments_Clicked (object sender, System.EventArgs e)
		{
			ShowArgumentsDialog(true);			
		}

		protected void btnBrowseDefaultLib_Clicked (object sender, System.EventArgs e)
		{
			Gtk.ResponseType response;
			AddLibraryDialog dialog = new AddLibraryDialog(AddLibraryDialog.FileFilterType.LibraryFiles);
			response = (Gtk.ResponseType) dialog.Run ();
			if (response == Gtk.ResponseType.Ok)
				txtDefaultLib.Text = dialog.Library;
		}
		
		private void OnDefaultLibAdded(object sender, System.EventArgs e)
		{
			if (txtDefaultLib.Text.Length > 0) {				
				defaultLibStore.AppendValues (txtDefaultLib.Text);
				txtDefaultLib.Text = string.Empty;
			}			
		}		
		
		protected void btnAddDefaultLib_Click (object sender, System.EventArgs e)
		{
			OnDefaultLibAdded(sender, e);
		}		
		
		protected void btnRemoveDefaultLib_Clicked (object sender, System.EventArgs e)
		{
			Gtk.TreeIter iter;
			tvDefaultLibs.Selection.GetSelected (out iter);
			defaultLibStore.Remove (ref iter);
		}				

		protected void tvDefaultLibs_CursorChanged (object sender, System.EventArgs e)
		{
			btnRemoveDefaultLib.Sensitive = true;
		}
		
		protected void txtDefaultLib_Changed (object sender, System.EventArgs e)
		{
			if (string.IsNullOrEmpty (txtDefaultLib.Text))
				btnAddDefaultLib.Sensitive = false;
			else
				btnAddDefaultLib.Sensitive = true;
		}

		protected void txtDefaultLib_Activated (object sender, System.EventArgs e)
		{
			OnDefaultLibAdded(sender, e);
		}		
		
		protected void btnBrowseIncludePath_Clicked (object sender, System.EventArgs e)
		{
			/*Gtk.ResponseType response;
			AddPathDialog dialog = new AddPathDialog (System.IO.Directory.GetCurrentDirectory().ToString());
			response = (Gtk.ResponseType) dialog.Run ();
			if (response == Gtk.ResponseType.Ok)
				txtIncludePath.Text = dialog.SelectedPath;*/
			
			Gtk.FileChooserDialog dialog = new Gtk.FileChooserDialog("Select D Source Folder", null , Gtk.FileChooserAction.SelectFolder, "Cancel", Gtk.ResponseType.Cancel, "Ok", Gtk.ResponseType.Ok);
			try{
				
				dialog.WindowPosition = Gtk.WindowPosition.Center;
				if (dialog.Run() == (int)Gtk.ResponseType.Ok)
					txtIncludePath.Text = dialog.Filename;
			}finally{
				dialog.Destroy();
			}			
			
			
		}
		
		private void OnIncludePathAdded(object sender, System.EventArgs e)
		{
			if (txtIncludePath.Text.Length > 0) {				
				includePathStore.AppendValues (txtIncludePath.Text);
				txtIncludePath.Text = string.Empty;
			}			
		}
		
		protected void btnAddIncludePath_Clicked (object sender, System.EventArgs e)
		{
			OnIncludePathAdded(sender, e);
		}

		protected void btnRemoveIncludePath_Clicked (object sender, System.EventArgs e)
		{
			Gtk.TreeIter iter;
			tvIncludePaths.Selection.GetSelected (out iter);
			includePathStore.Remove (ref iter);
		}

		protected void tvIncludePaths_CursorChanged (object sender, System.EventArgs e)
		{
			btnRemoveIncludePath.Sensitive = true;
		}

		protected void txtIncludePath_Changed (object sender, System.EventArgs e)
		{
			if (string.IsNullOrEmpty (txtIncludePath.Text))
				btnAddIncludePath.Sensitive = false;
			else
				btnAddIncludePath.Sensitive = true;
		}

		protected void txtIncludePath_Activated (object sender, System.EventArgs e)
		{
			OnIncludePathAdded(sender, e);
		}

		protected void btnDefaults_Clicked (object sender, System.EventArgs e)
		{			
			//need new object, because the user can still hit canel at the config screen
			//so we don't want to update the real object yet
			DCompilerConfiguration realConfig = configuration;			
			try
			{
				DCompilerConfiguration tempConfig = new DCompilerConfiguration{Vendor = configuration.Vendor};		
				DCompilerConfiguration.ResetToDefaults(tempConfig, configuration.Vendor);	
				Load (tempConfig);				
			}finally{
				configuration = realConfig;	
				releaseArgumentsDialog.Configuration = realConfig;
				debugArgumentsDialog.Configuration = realConfig;
			}				
		}
	}
	
	public class DMDCompilerOptionsBinding : OptionsPanel
	{
		private DCompilerOptions panel;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			panel = new DCompilerOptions ();
			LoadConfigData();
			return panel;
		}
		
		public void LoadConfigData ()
		{					
			panel.Load(DCompiler.Instance.Dmd);
		}		

		public override bool ValidateChanges()
		{
			return panel.Validate();
		}
			
		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}
	
	public class GDCCompilerOptionsBinding : OptionsPanel
	{
		private DCompilerOptions panel;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			panel = new DCompilerOptions ();
			LoadConfigData();
			return panel;
		}
		
		public void LoadConfigData ()
		{
			panel.Load(DCompiler.Instance.Gdc);
		}		

		public override bool ValidateChanges()
		{
			return panel.Validate();
		}
			
		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}
	
	public class LDCCompilerOptionsBinding : OptionsPanel
	{
		private DCompilerOptions panel;

		public override Gtk.Widget CreatePanelWidget ()
		{
			panel = new DCompilerOptions ();
			LoadConfigData();
			return panel;
		}
			
		public void LoadConfigData ()
		{		
			panel.Load(DCompiler.Instance.Ldc);
		}		

		public override bool ValidateChanges()
		{
			return panel.Validate();
		}
			
		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}	
	
}
