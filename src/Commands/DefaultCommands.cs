using SYMM_Backend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Flash_dl.Commands
{
    public static class DefaultCommands
    {

        public static string Exit()
        {
            Console.WriteLine("Goodbye!");
            Thread.Sleep(1000);
            Environment.Exit(0);
            return "";
        }

        
    }
}
