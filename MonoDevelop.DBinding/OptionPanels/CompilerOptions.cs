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
	public partial class CompilerOptions : Gtk.Bin
	{
		private Gtk.ListStore compilerStore = new Gtk.ListStore(typeof(string), typeof(DCompilerVendor));		
		private DCompiler configuration;

		public CompilerOptions () 
		{
			this.Build ();			
			
        	cmbCompiler.Clear();			
			Gtk.CellRendererText cellRenderer = new Gtk.CellRendererText();
			cmbCompiler.PackStart(cellRenderer, false);
			cmbCompiler.AddAttribute(cellRenderer, "text", 0);

			cmbCompiler.Model = compilerStore;
            compilerStore.AppendValues("DMD", DCompilerVendor.DMD);			
            compilerStore.AppendValues("GDC", DCompilerVendor.GDC);			
            compilerStore.AppendValues("LDC", DCompilerVendor.LDC);						
			
		}
		
		public void Load (DCompiler config)
		{
			configuration = config;
			
			//cmbCompiler.Active = (int)config.DefaultCompiler;
			Gtk.TreeIter iter;
			cmbCompiler.Model.GetIterFirst (out iter);
			if (cmbCompiler.Model.GetIterFirst (out iter)) {
				do {
					if (config.DefaultCompiler == (DCompilerVendor)cmbCompiler.Model.GetValue(iter, 1)) {
						cmbCompiler.SetActiveIter(iter);
						break;
					}
				} while (cmbCompiler.Model.IterNext (ref iter));
			}			
		}


		public bool Validate()
		{
			return true;
		}
		
		public bool Store ()
		{
			if (configuration == null)
				return false;
			
			//configuration.DefaultCompiler = (DCompilerVendor)cmbCompiler.Active;			
			Gtk.TreeIter iter;
			if (cmbCompiler.GetActiveIter(out iter))
				configuration.DefaultCompiler = (DCompilerVendor)cmbCompiler.Model.GetValue (iter,1);
			
			return true;
		}

	}
	
	public class CompilerOptionsBinding : OptionsPanel
	{
		private CompilerOptions panel;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			panel = new CompilerOptions ();
			panel.Load(DCompiler.Instance);
			return panel;
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
