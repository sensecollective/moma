// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:c
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2006-2008 Jonathan Pobst (monkey@jpobst.com)
//
// Author:
//	Jonathan Pobst	monkey@jpobst.com
//

using System;
using System.IO;
using System.Windows.Forms;

namespace MoMA
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main (string[] args)
		{
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			MainForm form = new MainForm ();

			bool nogui = false;

			for (int i = 0; i < args.Length; i++) {
				string arg = args[i];
				string nextArg = (i + 1 < args.Length) ? args[i + 1] : null;

				switch (arg.ToLower ()) {
					case "--help":
					case "-help":
						ShowHelp ();
						return 0;
					case "--nogui":
					case "-nogui":
						nogui = true;
						break;
					case "--ignore":
					case "-ignore":
						form.AssembliesToIgnore = nextArg;
						i++;
						break;
					case "--out":
					case "-out":
						// if !full path, use report dir
						if (CheckPath (nextArg)) {
							form.ReportFileName = nextArg;
						}
						else {
							Console.WriteLine ("Need path definition for '-out', try '-out .\\" + nextArg + "' to use current directory.");
						}
						i++;
						break;
					case "--xml":
					case "-xml":
						if (CheckPath (nextArg)) {
							form.SubmitFileName = nextArg;
						}
						else {
							Console.WriteLine ("Need path definition for '-xml', try '-xml .\\" + nextArg + "' to use current directory.");
						}						
						i++;
						break;
					default:
						if (arg.StartsWith ("-")) {
							Console.WriteLine ("Unknown argument: {0}", arg);
							return 1;
						}
						AddAssemblies (form, arg);						
						break;
				}
			}

			if (!nogui){
				Application.Run (form);
				return 0;
			}
			else{
				return form.AnalyzeNoGui ();				
			}			
		}
		
		private static void AddAssemblies (MainForm form, string arg)
		{
			try {				
				FileAttributes att = File.GetAttributes (arg);
				if ((att & FileAttributes.Directory) == FileAttributes.Directory) {
					Console.WriteLine ("Searching for assemblies in: " + arg);
					form.ScanForAssemblies (arg);
				}
				else {
					form.AddAssembly (arg);
				}
			}
			catch (Exception ex) {
				Console.WriteLine (ex.ToString ());
			}
		}
		private static bool CheckPath (string nextArg)
		{
			string path = Path.GetDirectoryName (nextArg);
			return (path != "");
		}
		private static void ShowHelp ()
		{
			Console.WriteLine (
				"MoMA.exe [options] [inputfiles|inputFilePath]" + Environment.NewLine + Environment.NewLine +
				"Options:" + Environment.NewLine + Environment.NewLine +
				"  --help:" + Environment.NewLine +
				"   -help: Show this help message." + Environment.NewLine + Environment.NewLine +
				" --nogui:" + Environment.NewLine +
				"  -nogui: Run application without GUI." + Environment.NewLine + Environment.NewLine +
				"--ignore:" + Environment.NewLine +
				" -ignore: Comma separated list of Assemblies to ignore" + Environment.NewLine + Environment.NewLine +
				"   --out:" + Environment.NewLine +
				"    -out: HTML report filename" + Environment.NewLine + Environment.NewLine +
				"   --xml:" + Environment.NewLine +
				"    -xml: XML report filename" + Environment.NewLine + Environment.NewLine +
				"Sample:" + Environment.NewLine +
				"\tMoMA.exe " + @"-nogui -xml ./foobar.xml -out ./boing/out.html C:\MyApplication\bin\Debug -ignore WPF-tainted.dll"
				);
		}
	}
}
