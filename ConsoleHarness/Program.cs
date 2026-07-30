using System;
using System.IO;
using FrankenDrift.Glue;
using Adrift = FrankenDrift.Adrift;

namespace FrankenDrift.Harness
{
    class NoMap : Glue.Map
    {
        public void RecalculateNode(object node) { }
        public void SelectNode(string key) { }
    }

    class NullBox : RichTextBox
    {
        public void Clear() { }
        public int TextLength => 0;
        public string Text { get; set; } = "";
        public string SelectedText { get; set; } = "";
        public int SelectionStart { get; set; }
        public int SelectionLength { get; set; }
        public bool IsDisposed => false;
    }

    class ConsoleSession : UIGlue, frmRunner
    {
        private readonly NullBox _box = new();
        public Glue.Infragistics.Win.UltraWinToolbars.UltraToolbarsManager UTMMain => null;
        public RichTextBox txtOutput => _box;
        public RichTextBox txtInput => _box;
        public bool Locked => false;

        public void Close() { Environment.Exit(0); }
        public void ReloadMacros() { }
        public void SaveLayout() { }
        public void SetBackgroundColour() { }
        public void UpdateStatusBar(string desc, string score, string user) { }
        public void SubmitCommand() { }

        public void ErrMsg(string message, Exception ex = null)
        {
            Console.Error.WriteLine($"** ADRIFT error: {message}" + (ex != null ? $" ({ex.Message})" : ""));
        }
        public void MakeNote(string msg) => Console.Out.WriteLine($"[note: {msg}]");
        public void EnableButtons() { }
        public void SetGameName(string name) { }
        public void ScrollToEnd() { }
        public bool AskYesNoQuestion(string question, string title = null)
        {
            Console.Out.WriteLine($"[Q: {question}] -> answering no");
            return false;
        }
        public void ShowInfo(string info, string title = null) => Console.Out.WriteLine($"[info: {info}]");
        public string QuerySavePath() => "";
        public string QueryRestorePath() => "";
        public QueryResult QuerySaveBeforeQuit() => QueryResult.NO;
        public void OutputHTML(string source) => Console.Out.Write(source);
        public void InitInput() { }
        public void ShowCoverArt(byte[] img) { }
        public void DoEvents() { }
        public string GetAppDataPath() => Path.GetTempPath();
        public string GetExecutableLocation() => AppContext.BaseDirectory;
        public string GetExecutablePath() => Environment.ProcessPath ?? "fdconsole";
        public string GetClaimedAdriftVersion() => "5.0000364";
    }

    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: fdconsole <game.taf> [commands.txt]");
                return 2;
            }
            var session = new ConsoleSession();
            Adrift.SharedModule.Glue = session;
            Adrift.SharedModule.fRunner = session;
            Glue.Application.SetFrontend(session);
            Adrift.SharedModule.UserSession = new Adrift.RunnerSession { Map = new NoMap(), bShowShortLocations = true };
            if (!Adrift.SharedModule.UserSession.OpenAdventure(args[0]))
            {
                Console.Error.WriteLine("Failed to open adventure.");
                return 1;
            }

            TextReader input = args.Length > 1 ? new StreamReader(args[1]) : Console.In;
            string line;
            while ((line = input.ReadLine()) != null)
            {
                // Exactly what ADRIFT's own frmRunner.SubmitCommand does: push a fresh empty slot,
                // write the command into the one before it (which is what AGAIN reads back), bump
                // the turn counter, and only then process.
                var hist = Adrift.SharedModule.UserSession.salCommands;
                hist.Add("");
                hist[hist.Count - 2] = line.Trim();
                Adrift.SharedModule.Adventure.Turns += 1;
                Adrift.SharedModule.UserSession.Process(line.Trim());
                if (Adrift.SharedModule.Adventure.eGameState != Adrift.clsAction.EndGameEnum.Running)
                    break;
            }
            Console.Out.Write("\n[eof]\n");
            return 0;
        }
    }
}
