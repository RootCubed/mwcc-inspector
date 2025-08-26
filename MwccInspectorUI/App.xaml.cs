using MwccInspectorUI.ViewModel;
using System.CommandLine;
using System.IO;
using System.Windows;

namespace MwccInspectorUI {
    public partial class App : Application {
        public List<string> FunctionNames = [];

        private void Application_Startup(object sender, StartupEventArgs e) {
            Argument<string> mwccPath = new("mwcc_path") {
                Description = "Path to MWCC executable",
            };

            Argument<string[]> mwccArgs = new("args") {
                Description = "Arguments passed to MWCC",
                Arity = ArgumentArity.ZeroOrMore
            };

            Option<string> cwd = new("--cwd") {
                Description = "Set working directory for MWCC process"
            };

            RootCommand rootCommand = new("MWCC Inspector");
            rootCommand.Arguments.Add(mwccPath);
            rootCommand.Arguments.Add(mwccArgs);
            rootCommand.Options.Add(cwd);

            rootCommand.SetAction(parseResult => {
                List<string> argsList = [];
                string mwccPathStr = parseResult.GetRequiredValue(mwccPath);
                argsList.Add(parseResult.GetRequiredValue(mwccPath));
                argsList.AddRange(parseResult.GetRequiredValue(mwccArgs));

                if (parseResult.GetValue(cwd) is string userCwd) {
                    if (!Directory.Exists(userCwd)) {
                        Console.WriteLine($"Error: Specified working directory does not exist: {userCwd}");
                        return;
                    }
                    Directory.SetCurrentDirectory(userCwd);
                }

                string mwccFullPath = Path.Join(Directory.GetCurrentDirectory(), mwccPathStr);
                if (!File.Exists(mwccFullPath)) {
                    Console.WriteLine($"Could not find MWCC execute at {mwccFullPath}");
                    return;
                }

                var vm = new MainViewModel(string.Join(" ", argsList));
                var mainWindow = new MainWindow { DataContext = vm };
                mainWindow.Show();
            });

            var args = e.Args;
            Console.WriteLine(string.Join(" ", args));
            ParseResult parseResult = rootCommand.Parse(args);
            parseResult.Invoke();
        }
    }

}
