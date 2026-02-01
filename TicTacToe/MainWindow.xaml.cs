using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace ChatApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<string> _chatHistory = new();
        private readonly Dictionary<string, string> _boardState = new()
        {
            {"Cell00", ""}, {"Cell01", ""}, {"Cell02", ""},
            {"Cell10", ""}, {"Cell11", ""}, {"Cell12", ""},
            {"Cell20", ""}, {"Cell21", ""}, {"Cell22", ""}
        };
        string userMessage = "Start a new Tic-Tac-Toe game. You are O, I am X. I passed  a dict(_boardState) to you where key is cell position. Respond ONLY with the cell name (e.g., Cell00) where you want to move. Do not include any other text."; 
            
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Cell_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            var userMoveCell = btn.Name; // FIX: define userMoveCell from the clicked button's Name
            btn.Content = "X";
            _boardState[btn.Name] = "X";

            try
            {
                string hfToken = "";
                var client = new AiService(hfToken);

                // Serialize _boardState to a non-null string for the AI service
                var boardStateString = System.Text.Json.JsonSerializer.Serialize(_boardState);
                var prompt = new StringBuilder();
                prompt.AppendLine("You are O, I am X.");
                prompt.AppendLine("I will pass the current board state as JSON.");
                prompt.AppendLine("BoardState:");
                prompt.AppendLine(boardStateString);
                prompt.AppendLine($"I (X) just played: {userMoveCell}");
                prompt.AppendLine("You (O) should respond ONLY with the cell name where you want to move, using the exact format CellXY (e.g., Cell00). " +
                    "Do NOT output any other text only when X or O won you say cellname + X won or O won, explanations, or punctuation.");

                var reply = await client.GetResponseAsync(prompt.ToString());
                Console.WriteLine(reply);
                var aiCell = ExtractCellName(reply);
                var result = IsSomeoneWon(reply);
                if (!string.IsNullOrEmpty(aiCell)) //&& _boardState.ContainsKey(aiCell) && string.IsNullOrEmpty(_boardState[aiCell]))
                {
                    var aiButton = this.FindName(aiCell) as Button;
                    if (aiButton != null)
                    {
                        if (result != "")
                        {
                            if(result == "X won")
                            {
                                StatusTextBlock.Text = result;
                                return;
                            } else if (result == "O won")
                            {
                                aiButton.Content = "O";
                                _boardState[aiCell] = "O";
                                StatusTextBlock.Text = result;
                                return;
                            }
                            
                        }
                        aiButton.Content = "O";
                        _boardState[aiCell] = "O";
                        
                        
                    }
                }
            }
            catch (Exception ex)
            {
                _chatHistory.Add("Error: " + ex.Message);
            }
        }

        private async void StartGameClicked(object sender, RoutedEventArgs e)
        {
            string hfToken = "";

            var client = new AiService(hfToken);
            await client.GetResponseAsync(userMessage);
        }

        private string? ExtractCellName(string aiResponse)
        {
            var match = Regex.Match(aiResponse, @"Cell\d\d");
            return match.Success ? match.Value : null;
        }

        private string IsSomeoneWon(string aiResponse)
        {
                if(aiResponse.Contains("X won"))
                {
                    return "X won";
                }
                else if(aiResponse.Contains("O won"))
                {
                    return "O won";
                }
                return "";
        }

    }
}