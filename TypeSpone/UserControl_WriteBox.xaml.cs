using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TypeSpone
{
    /// <summary>
    /// Logique d'interaction pour UserControl_WriteBox.xaml
    /// </summary>
    public partial class UserControl_WriteBox : UserControl
    {
        public UserControl_WriteBox(string wordToWrite, Action<UserControl_WriteBox> wordCompleted, bool isDiacriticsSensitive = false)
        {
            InitializeComponent();

            // A|B|C
            uniformGrid_word.Columns = wordToWrite.Length;

            // ajoute les lettres
            for (int i = 0; i < wordToWrite.Length; i++)
            {
                Viewbox viewbox = new Viewbox();
                TextBlock textBlock = new TextBlock();
                textBlock.Text = Char.ToString(wordToWrite[i]);
                textBlock.Foreground = MainWindow.WordColorBeforeTyping;
                viewbox.Child = textBlock;
                viewbox.Margin = new Thickness(2, 0, 2, 0);
                uniformGrid_word.Children.Add(viewbox);
            }

            // enlève les accents si IsDiacriticsSensitive = false
            WordToWrite = (!IsDiacriticsSensitive ? Utilities.RemoveDiacritics(wordToWrite) : wordToWrite).ToLower();
            WordCompleted = wordCompleted;
            IsDiacriticsSensitive = isDiacriticsSensitive;
        }

        public string WordToWrite { get; }
        public Action<UserControl_WriteBox> WordCompleted { get; }
        public bool IsDiacriticsSensitive { get; }
        public bool IsDestroying { get; set; }
        public bool IsWordComplet { get; private set; } = false;
        public bool IsInDeadLine { get; internal set; } = false;

        private string CompletedWord = String.Empty;

        /// <summary>
        /// Une lettre est appuyé, on regarde si on peut compléter le mot
        /// </summary>
        /// <param name="key"></param>
        internal bool KeyDown(Key key, bool clearWordIfFinished)
        {
            if (!IsInDeadLine)
            {
                char pressedLetter = Utilities.GetCharFromKey(key);

                // si on garde l'accent ou pas
                if (!IsDiacriticsSensitive)
                    pressedLetter = Convert.ToChar(Utilities.RemoveDiacritics(Char.ToString(pressedLetter)));

                pressedLetter = Char.ToLower(pressedLetter);

                // si retour chariot 
                if (pressedLetter == '\b')
                {
                    if (CompletedWord.Length > 0)
                    {
                        // enlève la dernière lettre
                        CompletedWord = CompletedWord.Remove(CompletedWord.Length - 1);

                        for (int i = uniformGrid_word.Children.Count - 1; i >= 0; i--)
                        {
                            // enlève la couleur de la dernière lettre tapé (le blanc)
                            if (((uniformGrid_word.Children[i] as Viewbox).Child as TextBlock).Foreground == Brushes.White)
                            {
                                ((uniformGrid_word.Children[i] as Viewbox).Child as TextBlock).Foreground = MainWindow.WordColorBeforeTyping;
                                break; // empêche d'enlever les couleurs de toutes les lettres
                            }
                        }

                        IsWordComplet = false;
                    }
                    return true;
                }

                // si autres lettres
                if (!IsWordComplet)
                {
                    // On regarde si la première lettre match
                    if (WordToWrite[CompletedWord.Length] == pressedLetter)
                    {
                        CompletedWord += pressedLetter;
                        // Change la couleur de la lettre
                        ((uniformGrid_word.Children[CompletedWord.Length - 1] as Viewbox).Child as TextBlock).Foreground = Brushes.White;
                    }
                    // il s'est trompé on supprime l'avancement
                    else
                    {
                        ClearWord();

                        return false;
                    }

                    if (WordToWrite.Length == CompletedWord.Length)
                    {
                        // mot écrit
                        this.IsWordComplet = true;

                        if(!TypeSpone.Properties.Settings.Default.HaveToPressEnter)
                        {
                            // on valide le mot
                            WordCompleted(this);
                            IsDestroying = true;

                            // clear word
                            if (clearWordIfFinished)
                                ClearWord();

                            return true;
                        }
                    }

                    // Le mot fût compléter
                    return true;

                }
                else if ((pressedLetter == '\r' || key == Key.Space) && !IsDestroying && TypeSpone.Properties.Settings.Default.HaveToPressEnter) // je précise ici key car la touche windows a aussi pour valeur ' '
                {
                    // on valide le mot
                    WordCompleted(this);
                    IsDestroying = true;

                    // clear word
                    if (clearWordIfFinished)
                        ClearWord();

                    return true;
                }

                return false;
            }
            return false;
        }

        private void ClearWord()
        {
            
            for (int i = 0; i < uniformGrid_word.Children.Count; i++)
            {
                ((uniformGrid_word.Children[i] as Viewbox).Child as TextBlock).Foreground = MainWindow.WordColorBeforeTyping;
            }

            this.IsWordComplet = false;
            CompletedWord = string.Empty;
            
        }
    }
}
