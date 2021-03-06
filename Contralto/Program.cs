﻿/*  
    This file is part of ContrAlto.

    ContrAlto is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    ContrAlto is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with ContrAlto.  If not, see <http://www.gnu.org/licenses/>.
*/

using Contralto.Scripting;
using Contralto.SdlUI;
using System;
using System.Windows.Forms;

namespace Contralto
{
    public static class StartupOptions
    {
        public static string ConfigurationFile;

        public static string ScriptFile;

        public static string RomPath;
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //
            // Check for command-line arguments.
            //            
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i++].ToLowerInvariant())
                    {
                        case "-config":
                            if (i < args.Length)
                            {
                                StartupOptions.ConfigurationFile = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        case "-script":
                            if (i < args.Length)
                            {
                                StartupOptions.ScriptFile = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        case "-rompath":
                            if (i < args.Length)
                            {
                                StartupOptions.RomPath = args[i];
                            }
                            else
                            {
                                PrintUsage();
                                return;
                            }
                            break;

                        default:
                            PrintUsage();
                            return;
                    }
                }
            }
            
            PrintHerald();

            _system = new AltoSystem();

            // Load disks specified by configuration
            if (!String.IsNullOrEmpty(Configuration.Drive0Image))
            {
                try
                {
                    _system.LoadDiabloDrive(0, Configuration.Drive0Image, false);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Could not load image '{0}' for Diablo drive 0.  Error '{1}'.", Configuration.Drive0Image, e.Message);
                    _system.UnloadDiabloDrive(0);
                }
            }

            if (!String.IsNullOrEmpty(Configuration.Drive1Image))
            {
                try
                {
                    _system.LoadDiabloDrive(1, Configuration.Drive1Image, false);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not load image '{0}' for Diablo drive 1.  Error '{1}'.", Configuration.Drive1Image, e.Message);
                    _system.UnloadDiabloDrive(1);
                }
            }


            if (Configuration.TridentImages != null)
            {                
                for (int i = 0; i < Math.Min(8, Configuration.TridentImages.Count); i++)
                {
                    try
                    {
                        if (!String.IsNullOrWhiteSpace(Configuration.TridentImages[i]))
                        {
                            _system.LoadTridentDrive(i, Configuration.TridentImages[i], false);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Could not load image '{0}' for Trident drive {1}.  Error '{2}'.", Configuration.TridentImages[i], i, e.Message);
                        _system.UnloadTridentDrive(i);
                    }
                }
            }

            //
            // Attach handlers so that we can properly flush state if we're terminated.
            //
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            //
            // Invoke the main UI window; this will run until the user closes it, at which
            // point we are done.
            //

            if (Configuration.Platform == PlatformType.Windows)
            {
                using (AltoWindow mainWindow = new AltoWindow())
                {
                    mainWindow.AttachSystem(_system);
                    Application.Run(mainWindow);
                }
            }
            else
            {
                using (SdlAltoWindow mainWindow = new SdlAltoWindow())
                {
                    // Invoke the command-line console
                    SdlConsole console = new SdlConsole(_system);
                    console.Run(mainWindow);

                    // Start the SDL display running.
                    mainWindow.AttachSystem(_system);
                    mainWindow.Run();
                }
            }
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Exiting...");
                        
            _system.Shutdown(false /* don't commit disks */);

            //
            // Commit current configuration to disk
            //
            Configuration.WriteConfiguration();
        }

        private static void PrintHerald()
        {
            Console.WriteLine("ContrAlto v{0} (c) 2015-2018 Living Computers: Museum+Labs.", typeof(Program).Assembly.GetName().Version);
            Console.WriteLine("Bug reports to joshd@livingcomputers.org");
            Console.WriteLine();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: ContrAlto [-config <configurationFile>] [-script <scriptFile>]");
        }

        private static AltoSystem _system;
    }
}
