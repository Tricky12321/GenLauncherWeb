using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GenLauncherWeb.Services;

public class SymLinkService
{
    public static bool IsSymlinksSupported()
    {
        // Check the platform
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, symbolic link creation requires either:
            // - Administrator privileges, or
            // - Developer mode enabled on Windows 10 and above
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine("Checking if symlinks are supported: " + tempFile);
            string symlink = tempFile + "_symlink";
            Console.WriteLine("Symlink file: " + symlink);
            try
            {
                // Attempt to create a symlink
                File.Create(tempFile).Dispose();
                
                File.CreateSymbolicLink(symlink, tempFile);
                if (File.Exists(symlink))
                {
                    return true;
                }
                
                bool success = File.Exists(symlink);
                File.Delete(tempFile);
                if (success)
                {
                    File.Delete(symlink);
                }
                Console.WriteLine("Symlinks are supported: " + success);
                return success;
            }
            catch (Exception e)
            {
                Console.WriteLine("Symlinks are not supported");
                Console.WriteLine(e.ToString());
                return false;
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On Linux and macOS, check if we can create a symlink in a temporary directory
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string symlink = tempFile + "_symlink";
            try
            {
                if (File.Exists(symlink))
                {
                    File.Delete(symlink);
                }

                // Attempt to create a symlink
                if (!File.Exists(tempFile))
                {
                    File.WriteAllText(tempFile, "test", System.Text.Encoding.UTF8);
                }

                File.CreateSymbolicLink(symlink, tempFile);
                bool success = File.Exists(symlink);
                if (success)
                {
                    File.Delete(symlink);
                }

                File.Delete(tempFile);
                return success;
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                return false;
            }
        }

        return false;
    }

    public static bool CreateSymbolicLink(string linkFile, string sourceFile)
    {
        if (IsSymlinksSupported())
        {
            Console.WriteLine("Creating symlink: " + linkFile + " -> " + sourceFile);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                File.CreateSymbolicLink(linkFile, sourceFile);
                if (File.Exists(linkFile))
                {
                    return true;
                }

                return CreateSymbolicLink(linkFile, sourceFile, 0);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return UnixCreateSymbolicLink(linkFile, sourceFile);
            }
        }

        return false;
    }


    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

    private static bool UnixCreateSymbolicLink(string linkFile, string sourceFile)
    {
        if (File.Exists(linkFile))
        {
            if (Extensions.IsSymbolicLink(linkFile))
            {
                File.Delete(linkFile);
            }
            else
            {
                return false;
            }
        }

        Path.GetDirectoryName(linkFile).CreateFolderIfItDoesNotExist();
        File.CreateSymbolicLink(linkFile, sourceFile);
        if (File.Exists(sourceFile))
        {
            return true;
        }

        return false;
    }
}