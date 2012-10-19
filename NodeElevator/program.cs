using System;
using System.Text;
using System.IO;
using System.Reflection;

namespace PrivilegedExe
{
    class Program
    {
        static int Main(string[] args)
        {
            int retVal = 1;

            try
            {

                var programfiles = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                var path = programfiles + @"\iisnode-dev\release\x86\iisnode-inspector.dll";
                if (!File.Exists(path))
                {
                    var originPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "iisnode-inspector.dll");
                    if (Directory.Exists(programfiles + @"\iisnode-dev\release\x86\"))
                    {
                        File.Copy(originPath, programfiles + @"\iisnode-dev\release\x86\iisnode-inspector.dll");
                    }
                }

                retVal = 0;
            }
            catch (System.Security.SecurityException secEx)
            {
                retVal = 1;
            }
            catch (UnauthorizedAccessException authEx)
            {
                retVal = 2;
            }
            catch (Exception ex)
            {
                retVal = 3;
                throw;
            }
            Console.WriteLine("NodeElevator completed with return code: {0}", retVal.ToString());

            return (retVal);
        }
    }
}
