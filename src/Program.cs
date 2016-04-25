using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Flash_dl
{
    class Program
    {
        const string _commandNamespace = "Flash_dl.Commands";
        static Dictionary<string, Dictionary<string, IEnumerable<ParameterInfo>>> _commandLibraries;

        const string _readPrompt = "Flash-dl> ";
        private const ConsoleColor _readPromtColor = ConsoleColor.DarkGreen;
        private const ConsoleColor _readCommandColor = ConsoleColor.White;
        private const ConsoleColor _promtCommandInvalidParameterColor = ConsoleColor.DarkYellow;
        private const ConsoleColor _promtCommandOutputColor = ConsoleColor.Gray;
        private const ConsoleColor _promtCommandOutputErrorColor = ConsoleColor.Red;

        private static int WriteBlockOffsetLeft = Console.WindowWidth / 5;
        private static int MaxLines = Console.WindowHeight / 2 +8;
        private static int MaxWidth = Console.BufferWidth / 2;

        private static int currentY = Console.WindowHeight / 2 +8;

        private static List<string> lines = new List<string>();
        public static SpectrumBeatDetector sbd = new SpectrumBeatDetector(3);

        static void Main(string[] args)
        {
            Backend.UpdateTitle();
            if (Properties.Settings.Default.SettingsNeedReset)
                Commands.Settings.Reset(true);

            Console.Clear();

            // Any static classes containing commands for use from the 
            // console are located in the Commands namespace. Load 
            // references to each type in that namespace via reflection:
            _commandLibraries = new Dictionary<string, Dictionary<string, IEnumerable<ParameterInfo>>>(StringComparer.OrdinalIgnoreCase);

            // Use reflection to load all of the classes in the Commands namespace:
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    where t.IsClass && t.Namespace == _commandNamespace
                    select t;
            var commandClasses = q.ToList();

            foreach (var commandClass in commandClasses)
            {
                // Load the method info from each class into a dictionary:
                var methods = commandClass.GetMethods(BindingFlags.Static | BindingFlags.Public);
                var methodDictionary = new Dictionary<string, IEnumerable<ParameterInfo>>(StringComparer.OrdinalIgnoreCase);
                foreach (var method in methods)
                {
                    string commandName = method.Name;
                    methodDictionary.Add(commandName, method.GetParameters());
                }
                // Add the dictionary of methods for the current class into a dictionary of command classes:
                _commandLibraries.Add(commandClass.Name, methodDictionary);
            }
            Run();
        }

        static void Run()
        {
            Console.ForegroundColor = _readCommandColor;
            WriteToConsole(Backend.SayHello());

            while (true)
            {  
                var consoleInput = ReadFromConsole();
                if (string.IsNullOrWhiteSpace(consoleInput)) continue;

                try
                {
                    // Create a ConsoleCommand instance:
                    var cmd = new ConsoleCommand(consoleInput);

                    // Execute the command:
                    string[] result = Execute(cmd);

                    // Write out the result:
                    WriteToConsole(result);
                }
                catch (Exception ex)
                {
                    // OOPS! Something went wrong - Write out the problem:
                    Console.ForegroundColor = _promtCommandOutputErrorColor;
                    WriteToConsole(ex.Message);
                }
            }
        }


        static string[] Execute(ConsoleCommand command)
        {
            // Validate the class name and command name:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            string[] badCommandMessage = new string[] { command.Name + " not found. Enter 'help' for more information." };

            // Validate the command name:
            if (!_commandLibraries.ContainsKey(command.LibraryClassName))
            {
                Console.ForegroundColor = _promtCommandOutputErrorColor;
                return badCommandMessage;
            }
            var methodDictionary = _commandLibraries[command.LibraryClassName];
            if (!methodDictionary.ContainsKey(command.Name))
            {
                Console.ForegroundColor = _promtCommandOutputErrorColor;
                return badCommandMessage;
            }

            // Make sure the corret number of required arguments are provided:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            var methodParameterValueList = new List<object>();
            IEnumerable<ParameterInfo> paramInfoList = methodDictionary[command.Name].ToList();

            // Validate proper # of required arguments provided. Some may be optional:
            var requiredParams = paramInfoList.Where(p => p.IsOptional == false);
            var optionalParams = paramInfoList.Where(p => p.IsOptional == true);
            int requiredCount = requiredParams.Count();
            int optionalCount = optionalParams.Count();
            int providedCount = command.Arguments.Count();

            if (requiredCount > providedCount)
            {
                Console.ForegroundColor = _promtCommandInvalidParameterColor;
                return new string[] { string.Format(
                    "Missing required argument. {0} required, {1} optional, {2} provided",
                    requiredCount, optionalCount, providedCount) };
            }

            // Make sure all arguments are coerced to the proper type, and that there is a 
            // value for every emthod parameter. The InvokeMember method fails if the number 
            // of arguments provided does not match the number of parameters in the 
            // method signature, even if some are optional:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            if (paramInfoList.Count() > 0)
            {
                // Populate the list with default values:
                foreach (var param in paramInfoList)
                {
                    // This will either add a null object reference if the param is required 
                    // by the method, or will set a default value for optional parameters. in 
                    // any case, there will be a value or null for each method argument 
                    // in the method signature:
                    methodParameterValueList.Add(param.DefaultValue);
                }

                // Now walk through all the arguments passed from the console and assign 
                // accordingly. Any optional arguments not provided have already been set to 
                // the default specified by the method signature:
                for (int i = 0; i < command.Arguments.Count(); i++)
                {
                    var methodParam = paramInfoList.ElementAt(i);
                    var typeRequired = methodParam.ParameterType;
                    object value = null;
                    try
                    {
                        // Coming from the Console, all of our arguments are passed in as 
                        // strings. Coerce to the type to match the method paramter:
                        value = CoerceArgument(typeRequired, command.Arguments.ElementAt(i));
                        methodParameterValueList.RemoveAt(i);
                        methodParameterValueList.Insert(i, value);
                    }
                    catch (ArgumentException)
                    {
                        string argumentName = methodParam.Name;
                        string argumentTypeName = typeRequired.Name;
                        string message =
                            string.Format(""
                            + "The value passed for argument '{0}' cannot be parsed to type '{1}'",
                            argumentName, argumentTypeName);
                        throw new ArgumentException(message);
                    }
                }
            }

            // Set up to invoke the method using reflection:
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            Assembly current = typeof(Program).Assembly;

            // Need the full Namespace for this:
            Type commandLibaryClass =
                current.GetType(_commandNamespace + "." + command.LibraryClassName, false, true);

            object[] inputArgs = null;
            if (methodParameterValueList.Count > 0)
            {
                inputArgs = methodParameterValueList.ToArray();
            }
            var typeInfo = commandLibaryClass;

            // This will throw if the number of arguments provided does not match the number 
            // required by the method signature, even if some are optional:
            try
            {
                var result = typeInfo.InvokeMember(
                    command.Name,
                    BindingFlags.IgnoreCase | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                    null, null, inputArgs);
                Console.ForegroundColor = _promtCommandOutputColor;
                return (string[])result;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }


        static object CoerceArgument(Type requiredType, string inputValue)
        {
            var requiredTypeCode = Type.GetTypeCode(requiredType);
            string exceptionMessage = 
                string.Format("Cannnot coerce the input argument {0} to required type {1}", 
                inputValue, requiredType.Name);

            object result = null;
            switch (requiredTypeCode)
            {
                case TypeCode.String:
                    result = inputValue;
                    break;

                case TypeCode.Int16:
                    short number16;
                    if (Int16.TryParse(inputValue, out number16))
                    {
                        result = number16;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;

                case TypeCode.Int32:
                    int number32;
                    if (Int32.TryParse(inputValue, out number32))
                    {
                        result = number32;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;

                case TypeCode.Int64:
                    long number64;
                    if (Int64.TryParse(inputValue, out number64))
                    {
                        result = number64;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;

                case TypeCode.Boolean:
                    bool trueFalse;
                    if (bool.TryParse(inputValue, out trueFalse))
                    {
                        result = trueFalse;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;

                case TypeCode.Byte:
                    byte byteValue;
                    if (byte.TryParse(inputValue, out byteValue))
                    {
                        result = byteValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;

                case TypeCode.Char:
                    char charValue;
                    if (char.TryParse(inputValue, out charValue))
                    {
                        result = charValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;

                case TypeCode.DateTime:
                    DateTime dateValue;
                    if (DateTime.TryParse(inputValue, out dateValue))
                    {
                        result = dateValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Decimal:
                    Decimal decimalValue;
                    if (Decimal.TryParse(inputValue, out decimalValue))
                    {
                        result = decimalValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Double:
                    Double doubleValue;
                    if (Double.TryParse(inputValue, out doubleValue))
                    {
                        result = doubleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Single:
                    Single singleValue;
                    if (Single.TryParse(inputValue, out singleValue))
                    {
                        result = singleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt16:
                    UInt16 uInt16Value;
                    if (UInt16.TryParse(inputValue, out uInt16Value))
                    {
                        result = uInt16Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt32:
                    UInt32 uInt32Value;
                    if (UInt32.TryParse(inputValue, out uInt32Value))
                    {
                        result = uInt32Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt64:
                    UInt64 uInt64Value;
                    if (UInt64.TryParse(inputValue, out uInt64Value))
                    {
                        result = uInt64Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                default:
                    throw new ArgumentException(exceptionMessage);
            }
            return result;
        }

        public static void WriteToConsole(string[] messages)
        {
            foreach (string message in messages)
                WriteToConsole(message);
        }

        public static void WriteToConsole(string message = "")
        {
            if(message.Length > MaxWidth)
            {
                lines.AddRange(Extensions.GetChunks(message, MaxWidth));
            }
            else
            {
                lines.Add(message);
            }
            
            if(lines.Count > MaxLines)
            {
                lines.RemoveRange(0, lines.Count - MaxLines);
            }

            WriteOutput();
        }

        private static void WriteOutput()
        {
            Console.CursorVisible = false;
            ClearOutput();

            for (int i = 0; i < MaxLines; i++)
            {
                try
                {
                    Console.SetCursorPosition(WriteBlockOffsetLeft, Console.WindowHeight / 2 + 5 - i);
                    string line = lines[lines.Count - i];

                    if (line.StartsWith("  "))
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                    else
                        Console.ForegroundColor = _readCommandColor;

                    Console.Write(line);
                }
                catch (ArgumentOutOfRangeException)
                {
                    continue;
                }
            }

            Console.CursorVisible = false;
        }

        private static void ClearOutput()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            for (int y = 0; y < Console.WindowHeight; y++)
            {
                Console.SetCursorPosition(WriteBlockOffsetLeft, y);
                Console.Write("".PadLeft(MaxWidth + 10));
            }
            Console.ForegroundColor = _readCommandColor;
        } 

        public static string ReadFromConsole(string promptMessage = "")
        {
            // Show a prompt, and get input:
            Console.ForegroundColor = _readPromtColor;
            Console.SetCursorPosition(WriteBlockOffsetLeft, Console.WindowHeight / 2 +8);
            Console.WriteLine();
            Console.SetCursorPosition(WriteBlockOffsetLeft, Console.WindowHeight /2 +8);
            Console.Write(_readPrompt);
            Console.ForegroundColor = _readCommandColor;
            Console.Write(promptMessage);

            return Console.ReadLine();
        }
    }
}

public static class Extensions
{
    public static IEnumerable<string> GetChunks(string str, int maxChunkSize)
    {
        for (int i = 0; i < str.Length; i += maxChunkSize)
            yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
    }
}