using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using Ionic.Zip;
using ZipFile = Ionic.Zip.ZipFile;

namespace updatescript {

    static class Ext {
        public static string Cmdify(this string s) {
            return s.Trim().Replace(";", "").Replace("\"", "");
        }
    }

    class Program {

        static string ACCEPT_HEADER = "updatescript v1.0";

        static bool breakOperation = false;

        static int currentVersion = -1;

        static StringBuilder logger = new StringBuilder();

        static void Main(string[] args) {
            
            string cd = Environment.CurrentDirectory;
            // VALIDATE

            if (args.Length != 1) {
                PrintLine("Invalid arguments. Syntax: updatescript.exe path");
                return;
            }
            string scriptPath = args[0];
            string[] updateScript;
            try {
                if (File.Exists(scriptPath))
                    updateScript = File.ReadAllText(scriptPath).Replace("\r\n", "\n").Split('\n'); // splits the script into lines
                else {
                    PrintLine("File does not exist");
                    return;
                }
            }
            catch (Exception e) {
                PrintLine("Error: " + e.Message);
                return;
            } // try to read the script; crash when failed

            if (updateScript.Length <= 1) {
                PrintLine("The script does not contain any commands");
                return;
            } // the script has no commands

            string header = updateScript[0]; // the header should contain the us version. It has to match the specified one.
            if (header != ACCEPT_HEADER) {
                PrintLine("The file has an incorrect header; Got: " + header + "; Expected: " + ACCEPT_HEADER);
                return;
            } else PrintLine("Header OK.");


            // FIND VERSIONS

            Dictionary<int, int> versionDictionary = new Dictionary<int, int>(); // VERSION : LINE

            PrintLine("Searching for versions.");
            for (int i = 1; i < updateScript.Length; i++) { // map versions
                if (updateScript[i].StartsWith("@")) {
                    int cVer = -1;
                    if (int.TryParse(updateScript[i].Substring(1), out cVer)) { // TODO: replace with the failsafe method TryParse
                        versionDictionary.Add(cVer, i);
                        PrintLine("Version " + cVer + "@" + i);
                    } else {
                        PrintLine("A version id failed to parse. Halting install.");
                        return;
                    }
                }
            }
            PrintLine("File contains " + versionDictionary.Count + " versions, the newest one beeing " + versionDictionary.Last().Key + "@" + versionDictionary.Last().Value);

            ChangeDirectory(Path.GetDirectoryName(Path.GetFullPath(scriptPath)));

            // FIND LOCAL VERSION

            int version = -1;
            if (File.Exists(".usver")) {
                if (int.TryParse(File.ReadAllText(".usver"), out version)) {
                    PrintLine("Client version: " + version);
                } else {
                    PrintLine("Client version incorrect; Assuming version -1");
                    version = -1;
                }
            }


            // FIND THE LINE WITH A NEWER VERSION

            int startingLine = 0;
            int startingVersion = -1;
            foreach (var item in versionDictionary.Keys) {
                startingLine = versionDictionary[item];
                startingVersion = item;
                if (item > version) break;
            }

            if (startingVersion == version) {PrintLine("There is nothing to update."); return; }

            PrintLine("The script will start from version " + startingVersion + "@" + startingLine);


            // EXECUTE

            PrintLine("Executing updateScript " + scriptPath + " ===============================");
            

            
            for (int i = startingLine; i < updateScript.Length; i++) {
                try {
                    Interpret(updateScript[i].Trim(), i);
                } catch(Exception e) {
                    PrintLine("Misc. Exception while executing line " + i + " : " + e.Message + ";" + e.StackTrace);
                    breakOperation = true;
                }
                if (breakOperation) break;
            }

            if (!breakOperation) {       
                PrintLine("Finished executing updateScript. Current version: " + currentVersion);
                File.WriteAllText(".usver", currentVersion.ToString());
            }
            else {
                PrintLine("updateScript failed. Please try again or send the log to the software's support.");
                File.WriteAllText("log.log", logger.ToString());
                Process.Start("log.log");
            }

            Environment.CurrentDirectory = cd;
        }

        static void Interpret(string command, int line = 0) {

            if(command.Length <= 1) {
                return;
            }
            
            char startChar = command[0];
            
            switch (startChar) {
                case '#': //comment
                    break;
                case '+': //download
                    Print(line + ": ");
                    DownloadFile(command.Substring(1));
                    break;
                case '-': //remove
                    Print(line + ": ");
                    Remove(command.Substring(1));
                    break;
                case '/': //execute
                    Print(line + ": ");
                    Execute(command.Substring(1));
                    break;
                case '~': //wait
                    Print(line + ": ");
                    Timeout(command.Substring(1));
                    break;
                case '\'': //print
                    DebugPrint("`" + command.Substring(1));
                    break;
                case '>': //cd
                    Print(line + ": ");
                    ChangeDirectory(command.Substring(1));
                    break;
                case '@': //version header
                    Print(line + ": ");
                    VersionCheck(command.Substring(1));
                    break;
                case '^':
                    Print(line + ": ");
                    Unzip(command.Substring(1));
                    break;
                default:
                    PrintLine("Unknown command: " + startChar);
                    break;
            }
        } 

        static void DownloadFile(string command) {
            string[] cmd = command.Split(';');

            string from = cmd[0].Cmdify();
            string to = cmd.Length > 1 ? cmd[1].Cmdify() : GetFileNameFromUrl(from);

            if(File.Exists(to)) {
                File.Delete(to);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(to));
            Print($"Downloading {from} to {to}...");
            WebClient wc = new WebClient();
            try {
                wc.DownloadFile(from, to);
            }
            catch (WebException we) {
                PrintLine("Downloading failed. Thrown WebException: " + we.Message);
                breakOperation = true;
            }
            catch (Exception e) {
                PrintLine("An error occured while downloading files. Thrown exception: " + e.Message);
                breakOperation = true;
            }
            finally {
                PrintLine("Finished download");
            }
        }

        static void ChangeDirectory(string command) {
            if (Directory.Exists(command.Cmdify())) {
                Environment.CurrentDirectory = command.Cmdify();
                PrintLine("Changing directory to " + command.Cmdify()); 
            } else {
                PrintLine("Failed changing directory to " + command.Cmdify() + "; Directory does not exist.");
                breakOperation = true;
            }
        }

        static void Remove(string command) {
            
            if (File.Exists(command.Cmdify())) {
                PrintLine("Deleting " + command.Cmdify());
                File.Delete(command.Cmdify());
            } else if(Directory.Exists(command.Cmdify())) {
                PrintLine("Deleting " + command.Cmdify());
                Directory.Delete(command.Cmdify(),true);
            } else {
                PrintLine("The file  " + command.Cmdify() + " could not have been found, skipping deletion.");
            }
        }

        static void Execute(string command) {
            try {
                PrintLine("Running: " + command);

                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.Arguments = "/c " + command;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                PrintLine("STDOUT:");
                while (!p.StandardOutput.EndOfStream) {
                    string ln = ((char)p.StandardOutput.Read()).ToString();
                    Print(ln);
                }

            } catch (FileNotFoundException f){
                PrintLine("Error when running: " + command + "; It appears that this file does not exist.");
                breakOperation = true;
            } catch (Exception e) {
                PrintLine("Error when running: " + command + ";" +e.Message);
                breakOperation = true;
            }

            PrintLine("");
        }

        static void Timeout(string command) {
            int seconds = 0;
            
            if (int.TryParse(command, out seconds)) {
                for (int i= seconds; i >= 0; i--) {
                    Console.CursorLeft=0;
                    Print("Waiting..." + i + "s          ");
                    Thread.Sleep(1000);
                }
                PrintLine("Wait finished.");
            } else {
                PrintLine("Skipping wait command, format incorrect");
            }
        }

        static void DebugPrint(string command) {
            PrintLine(command);
        }

        static void VersionCheck(string command) {
            if (int.TryParse(command, out currentVersion))
                PrintLine("Updating to " + currentVersion);
            else {
                PrintLine("Version marker incorrect; " + command + " is not a valid version");
                breakOperation = true;
            }
        }

        static void Unzip(string command) {
            string[] cmd = command.Split(';');

            string from = cmd[0].Cmdify();
            string to = cmd[1].Cmdify();

            Print("Unzipping " + from + " to " + to + "...");
            if (!File.Exists(from)) {
                PrintLine("Failed. The source file does not exist.");
                breakOperation = true;
                return;
            }
            Directory.CreateDirectory(to);
            try {
                using (ZipFile zip1 = ZipFile.Read(from)) {
                    foreach (ZipEntry e in zip1) {
                        e.Extract(to, ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            } catch (IOException ie) {
                PrintLine("ionum " + ie.HResult);
                Print("An IO error occured.");
                breakOperation = true;
            }
            catch (Exception e) {
                PrintLine("An error occured: " + e.Message);
                breakOperation = true;
            }
            finally {
                PrintLine("Unzipped.");
            }
        }

        static string GetFileNameFromUrl(string url) {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(url);

            return Path.GetFileName(uri.LocalPath);

        }

        static void Print(string s) {
            Console.Write(s);
            logger.Append(s);
        }
        static void PrintLine(string s) {
            Console.WriteLine(s);
            logger.Append(s + "\n");
        }

       
    }
}
