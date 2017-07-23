using System;
using System.Collections.Generic;
using System.IO;
using Octodiff.CommandLine.Support;
using Octodiff.Core;
using Octodiff.Diagnostics;

namespace Octodiff.CommandLine
{
    [Command("signature-dir", "sig-dir", Description = "Given a basis directory, creates signature files for all files inside - including subdirectories", Usage = "<basis-dir> [<signature-dir>]")]
    class SignatureDirCommand : ICommand
    {
        private readonly OptionSet options;
        private string basisDirPath;
        private string signatureDirPath;

        public SignatureDirCommand()
        {
            options = new OptionSet();
            options.Positional("basis-dir", "The directory to read and create signatures from.", v => basisDirPath = v);
            options.Positional("signature-dir", "The directory to write the signatures to.", v => signatureDirPath = v);
        }

        public void GetHelp(TextWriter writer)
        {
            options.WriteOptionDescriptions(writer);
        }

        public int Execute(string[] commandLineArguments)
        {
            options.Parse(commandLineArguments);

            if (string.IsNullOrWhiteSpace(basisDirPath))
                throw new OptionException("No basis directory was specified", "basis-dir");

            basisDirPath = Path.GetFullPath(basisDirPath);

            if (!Directory.Exists(basisDirPath))
            {
                throw new DirectoryNotFoundException("Directory not found: " + basisDirPath);
            }

            foreach (var basisFilePath in Directory.GetFiles(basisDirPath, "*.*", SearchOption.AllDirectories))
            {
                var args = new List<string> { basisFilePath };
                if (!string.IsNullOrWhiteSpace(signatureDirPath))
                    args.Add(GetRelativePath(basisFilePath, Path.GetFullPath(signatureDirPath)));

                var signatureCommand = new SignatureCommand();
                var result = signatureCommand.Execute(args.ToArray());
                if (result != 0)
                    throw new Exception($"Signature command returned: {result}. BasisPath: {basisFilePath}");
            }

            return 0;
        }

        private string GetRelativePath(string filePath, string directoryPath)
        {
            Uri pathUri = new Uri(filePath);

            // Folders must end in a slash
            if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                directoryPath += Path.DirectorySeparatorChar;

            Uri folderUri = new Uri(directoryPath);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
