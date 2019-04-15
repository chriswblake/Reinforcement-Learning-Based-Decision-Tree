using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Xunit;

namespace Xunit
{
    /*
     * Description:
     * Creates an "experiments" folder in the same directory as the project.
     * Creates a separate folder for each experiment class.
     * Creates a separate folder for each method in the experiment class.
     * 
     * When "ResultsDir" is used within a method, it dynamically creates the appropriate folder.
     * This allows automatic saving of the experiment results in the appropriate folder. 
     * 
     * Example folder structure:
     * (Project folder)
     *     "Experiments"
     *         (ClassName)
     *             (MethodName)
     *                 experiment result files
     *                 ...
    */

    public class Experiment
    {
        //Properties
        protected string ResultsDir
        {
            get
            {
                //Moves up from bin directory
                string basePath = Path.GetFullPath(Path.Combine("..", "..", "..", "Experiments"));

                //Retrieves name of class file
                string className = GetClassName();

                //Find the stack position for this "get"
                int stackPos = -1;
                do
                {
                    stackPos++;
                } while (GetCurrentMethodName(stackPos) != "get_ResultsDir");

                //Get called method name
                string methodName = GetCurrentMethodName(stackPos+1); //gets name of executing method

                //Combine
                string folderPath = Path.Combine(basePath, className, methodName);

                //Create folder if missing
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                return folderPath;
            }
        }

        //Methods
        private string GetClassName()
        {
            string className = this.GetType().Name;
            className = className.Replace("Experiments", "");
            className = className.Replace("Experiment", "");
            return className;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetCurrentMethodName(int stepsBack)
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1 + stepsBack);

            return sf.GetMethod().Name;
        }
    }
}