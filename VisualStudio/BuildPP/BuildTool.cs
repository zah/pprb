using System.IO;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

using System.Text.RegularExpressions;

namespace BuildPPRB
{
    public class BuildFailedException : Exception
    {
        private static Regex ErrorLineRegex = new Regex(@"^(.+?):(\d+): error: (.+)");

        private static Regex RubyBacktraceRegex = new Regex("Ruby backtrace:(.+)", RegexOptions.Singleline);

        public BuildFailedException(string pprbErrorOutput)
        {
            OriginalOutput = pprbErrorOutput;

            Match errorMatch = ErrorLineRegex.Match(pprbErrorOutput);
            if(errorMatch.Groups.Count == 4)
            {
                SourceFile = errorMatch.Groups[1].ToString();
                Line = int.Parse(errorMatch.Groups[2].ToString());
                ErrorMessage = errorMatch.Groups[3].ToString();
            }           

            Match backtraceMatch = RubyBacktraceRegex.Match(pprbErrorOutput);
            if(backtraceMatch.Groups.Count == 2)
            {
                RubyBacktrace = backtraceMatch.Groups[1].ToString();
            }
        }

        public string SourceFile;
        public int Line;

        public string ErrorMessage;
        public string RubyBacktrace;

        public string OriginalOutput;
    }

    public static class BuildTool
    {
        /// <summary>
        /// Executes a shell command synchronously.
        /// </summary>
        /// <param name="command">string command</param>
        /// <returns>string, as output of the command.</returns>
        public static string ExecuteCommandSync(string command, string workingDirectory)
        {
            // create the ProcessStartInfo using "cmd" as the program to be run,
            // and "/c " as the parameters.
            // Incidentally, /c tells cmd that we want it to execute the command that follows,
            // and then exit.
            System.Diagnostics.ProcessStartInfo procStartInfo =
                new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

            // The following commands are needed to redirect the standard output and error streams.
            // This means that they will be redirected to Process.StandardOutput and Process.StandardError respectfully.
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.RedirectStandardError = true;

            procStartInfo.UseShellExecute = false;
            
            // Do not create the black window.
            procStartInfo.CreateNoWindow = true;

            procStartInfo.WorkingDirectory = workingDirectory;
            
            // Now we create a process, assign its ProcessStartInfo and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            // Get the output into a string
            string result = proc.StandardOutput.ReadToEnd();

            // Get the error output into a string
            string errors = proc.StandardError.ReadToEnd();

            if (errors.Length != 0)
                throw new BuildFailedException(errors);

            return result + errors;
        }

        public static byte[] Build(string inputFileName)
        {
            var inputDir = Path.GetDirectoryName(inputFileName) ?? String.Empty;
            var code = ExecuteCommandSync(String.Format("pprb \"{0}\"", inputFileName), inputDir);
            return Encoding.UTF8.GetBytes(code);
        }
    }

    [Guid("384BDF01-6CE6-4775-95C9-F5E3BE27708B")]
    [ComVisible(true)]
    public class PpCodeGenerator : BaseCodeGeneratorWithSite
    {
        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            try
            {                
                return BuildTool.Build(inputFileName);
            }
            catch (BuildFailedException ex)
            {
                GeneratorErrorCallback(false, 0, ex.ErrorMessage, ex.Line, 0);
                return Encoding.UTF8.GetBytes("/* PPRB Output:\n" + ex.OriginalOutput + "\n*/");
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.AppendLine(ex.ToString());

                sb.AppendLine(String.Join("\n", AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.FullName)));

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
        }

        public override string GetDefaultExtension()
        {
            return ".pprb.cs";
        }

        #region Registration

        // You have to make sure that the value of this field (CustomToolGuid) is exactly 
        // the same as the value of the Guid attribure (at the top of the class)
        private static Guid CustomToolGuid =
            new Guid("{384BDF01-6CE6-4775-95C9-F5E3BE27708B}");

        private static Guid CSharpCategory =
            new Guid("{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}");

        private static Guid VBCategory =
            new Guid("{164B10B9-B200-11D0-8C61-00A0C91E29D5}");


        private const string CustomToolName = "BuildPPRB";

        private const string CustomToolDescription = "Processes files with pprb";

        private const string KeyFormat
            = @"SOFTWARE\Microsoft\VisualStudio\{0}\Generators\{1}\{2}";

        protected static void Register(Version vsVersion, Guid categoryGuid)
        {
            string subKey = String.Format(KeyFormat,
                vsVersion, categoryGuid.ToString("B"), CustomToolName);

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(subKey))
            {
                if (key == null)
                    return;
                key.SetValue("", CustomToolDescription);
                key.SetValue("CLSID", CustomToolGuid.ToString("B"));
                key.SetValue("GeneratesDesignTimeSource", 1);
            }
        }

        protected static void Unregister(Version vsVersion, Guid categoryGuid)
        {
            string subKey = String.Format(KeyFormat,
                vsVersion, categoryGuid.ToString("B"), CustomToolName);

            Registry.LocalMachine.DeleteSubKey(subKey, false);
        }

        public static int[] StudioVersions = { 8, 9, 10 };

        [ComRegisterFunction]
        public static void RegisterClass(Type t)
        {
            foreach (var version in StudioVersions)
            {
                Register(new Version(version, 0), CSharpCategory);
                Register(new Version(version, 0), VBCategory);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterClass(Type t)
        {
            foreach (var version in StudioVersions)
            {
                Unregister(new Version(version, 0), CSharpCategory);
                Unregister(new Version(version, 0), VBCategory);
            }
        }

        #endregion
    }
}
