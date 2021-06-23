using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

using SteganographyApp.Common.Injection;

namespace SteganographyApp.Common
{

    public enum HelpItemSet
    {
        Main,
        Calculator,
        Converter
    }

    /// <summary>
    /// Class containing the results of parsing the help file and a utiilty
    /// method to retrieve help messages for a given set of arguments.
    /// </summary>
    public sealed class HelpInfo
    {

        public static readonly ImmutableArray<string> MainHelpLabels = ImmutableArray.Create(
            new string[]
            {
                "AppDescription", "AppAction", "Input", "Output", "Images", "Password",
                "PrintStack", "EnableCompression", "ChunkSize", "RandomSeed", "EnableDummies",
                "EnableLogs"
            }
        );

        public static readonly ImmutableArray<string> CalculatorHelpLabels = ImmutableArray.Create(
            new string[]
            {
                "CalculatorDescription", "CalculatorAction", "Input", "Images", "Password",
                "EnableCompression", "ChunkSize", "EnableDummies", "RandomSeed", "EnableLogs"
            }
        );

        public static readonly ImmutableArray<string> ConverterHelpLabels = ImmutableArray.Create(
            new string[]
            {
                "ConverterDescription", "ConvertAction", "Images", "DeleteOriginals",
                "CompressionLevel", "EnableLogs"
            }
        );

        private static readonly ImmutableDictionary<HelpItemSet, ImmutableArray<string>> HelpLabelMappings = new Dictionary<HelpItemSet, ImmutableArray<string>>
        {
            { HelpItemSet.Main, MainHelpLabels },
            { HelpItemSet.Calculator, CalculatorHelpLabels },
            { HelpItemSet.Converter, ConverterHelpLabels }
        }
        .ToImmutableDictionary();

        /// <summary>
        /// A dictionary containing the results of the help file parsing.
        /// This will include a key value representing the name of the help
        /// message or the parameter that it corresponds to and a value 
        /// being the actual help message.
        /// </summary>
        private readonly ImmutableDictionary<string, string> helpItems;

        public HelpInfo(ImmutableDictionary<string, string> helpItems)
        {
            this.helpItems = helpItems;
        }

        /// <summary>
        /// Attempts to retrieve an of array of help messages that are associated with
        /// the provided array of names and return them in the order in which they
        /// were requested.
        /// <para>If a particular parameter name was not loaded from the help file then a
        /// standard message will be returned in place of the actual help message.</para>
        /// </summary>
        /// <param name="item">The HelpItemSet enum value indicating which help items should
        /// be loaded and displayed to the user.</param>
        /// <returns>An array of help messages whose order is based on the order of the names
        /// provided to lookup.</returns>
        public ImmutableArray<string> GetHelpMessagesFor(HelpItemSet itemSet)
        {
            ImmutableArray<string> helpLabels = HelpLabelMappings[itemSet];
            string[] messages = new string[helpLabels.Length];
            for (int i = 0; i < helpLabels.Length; i++)
            {
                if (!helpItems.ContainsKey(helpLabels[i]))
                {
                    messages[i] = $"No help information configured for {helpLabels[i]}.\n";
                    continue;
                }
                messages[i] = helpItems[helpLabels[i]];
            }
            return ImmutableArray.Create(messages);
        }
    }

    /// <summary>
    /// Utility class to parse help information from the help.prop file
    /// so it can be shared between the regular app and the calculator.
    /// </summary>
    public class HelpParser
    {

        /// <summary>
        /// When found at the beginning of a line it indicates a new help item
        /// entry.
        /// </summary>
        private readonly string HelpItemIndicator = "~";

        /// <summary>
        /// The last error that occured during the execution of TryParse.
        /// </summary>
        public string LastError { get; private set; }

        /// <summary>
        /// Attempts to parse the help file located at the fileName parameter value.
        /// <para> If successful it will have populated the <see cref="Results"/> dictionary with all the
        /// help messages and the parameters that they are associated with.</para>
        /// <para>If not successful it will exit early and set the value of the <see cref="LastError"/>
        /// property to a message identifying why the process failed.</para>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>True if the help file was successfully parsed, otherwise false.</returns>
        public bool TryParseHelpFile(out HelpInfo info, string fileName = "help.prop")
        {

            string assemblyPath = GetAssemblyPath();
            string helpFileLocation = System.IO.Path.Combine(assemblyPath, fileName);
            
            if (!Injector.Provide<IFileProvider>().IsExistingFile(helpFileLocation))
            {
                LastError = $"The help file named {helpFileLocation} could not be found.";
                info = new HelpInfo(null);
                return false;
            }

            try
            {
                info = new HelpInfo(ParseHelpItems(helpFileLocation));
            }
            catch (Exception e)
            {
                LastError = e.Message;
                info = new HelpInfo(null);
                return false;
            }

            return true;
        }

        private ImmutableDictionary<string, string> ParseHelpItems(string helpFileLocation)
        {
            Dictionary<string, string> helpItems = new Dictionary<string, string>();
            string[] lines = Injector.Provide<IFileProvider>().ReadAllLines(helpFileLocation);
            for(int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(HelpItemIndicator))
                {
                    helpItems[LineWithoutHelpItemIndicator(lines[i])] = ReadHelpItem(lines, i + 1);
                }
            }
            return helpItems.ToImmutableDictionary();
        }

        private string LineWithoutHelpItemIndicator(string line)
        {
            return line.Substring(1);
        }

        private string GetAssemblyPath()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            int index = assemblyPath.LastIndexOf("\\");
            if(index == -1)
            {
                index = assemblyPath.LastIndexOf("/");
            }
            return assemblyPath.Substring(0, index);
        }

        /// <summary>
        /// Attempts to read all the lines of text that make up the help message that is associated
        /// with a particular parameter.
        /// <para>This will read all the lines from the start index provided until it reaches the end of
        /// the file or finds a line starting with a tilde (~) representing the start of a new parameter
        /// help message.</para>
        /// </summary>
        /// <param name="lines">The array of lines loaded from the help file.</param>
        /// <param name="startLine">The index to start reading the help message lines from.</param>
        /// <returns>A single string containing all the lines of the help message delimeted by line breaks.</returns>
        private string ReadHelpItem(string[] lines, int startLine)
        {
            string message = "";
            for(int i = startLine; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(HelpItemIndicator))
                {
                    break;
                }
                else if (lines[i].Trim().Length == 0)
                {
                    continue;
                }
                if(i != startLine)
                {
                    message += "\n";
                }
                message += lines[i].Replace("\\t", "\t");
            }
            return message;
        }

        /// <summary>
        /// Utility to print out a common error message in the instance where there is an error parsing
        /// the help properties file.
        /// </summary>
        public void PrintCommonErrorMessage()
        {
            Console.WriteLine("An error occurred while parsing the help file: {0}", LastError);
            Console.WriteLine("Check that the help.prop file is in the same directory as the application and try again.");
            Console.WriteLine();
        }
    }
}
