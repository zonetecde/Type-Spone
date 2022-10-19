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
        public UserControl_WriteBox(string wordToWrite, Action<UserControl_WriteBox> wordCompleted, Brush color, bool isDiacriticsSensitive = false, bool canBeDestroyed = false)
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
                textBlock.Foreground = color;
                viewbox.Child = textBlock;
                viewbox.Margin = new Thickness(2, 0, 2, 0);
                uniformGrid_word.Children.Add(viewbox);
            }

            // enlève les accents si IsDiacriticsSensitive = false
            NbEspaceAvantMot = wordToWrite.Length - wordToWrite.TrimStart().Length;

            WordToWrite = (!isDiacriticsSensitive ? Utilities.RemoveDiacritics(wordToWrite) : wordToWrite).Trim(); // .Trim() car des espaces ont peut être été ajouté pour avoir des mots de la même taille (ex : majuscule/accent)
            WordToWrite = MainWindow.Difficulte_Majuscule ? WordToWrite : WordToWrite.ToLower();

            WordCompleted = wordCompleted;
            Color = color;
            IsDiacriticsSensitive = isDiacriticsSensitive;
            CanBeDestroyed = canBeDestroyed;
        }

        private int NbEspaceAvantMot { get; }
        public string WordToWrite { get; }
        public Action<UserControl_WriteBox> WordCompleted { get; }
        public Brush Color { get; }
        public bool IsDiacriticsSensitive { get; }
        public bool IsDestroying { get; set; }
        public bool IsWordComplet { get; private set; } = false;
        public bool IsInDeadLine { get; internal set; } = false;
        public bool CanBeDestroyed { get; private set; }
        public bool WasWritingIt { get; internal set; } = false; // = true si 3 lettres SUCCESSIF ont écrit écrite pour ce mot
        private int TempCounterSuccessiveLetter { get; set; } = 0;

        private string CompletedWord = String.Empty;

        /// <summary>
        /// Une lettre est appuyé, on regarde si on peut compléter le mot
        /// </summary>
        /// <param name="key"></param>
        internal bool? KeyDown(Key key, bool clearWordIfFinished)
        {
            if (!IsInDeadLine)
            {
                char pressedLetter = Utilities.GetCharFromKey(key);

                // si on garde l'accent ou pas
                if (!MainWindow.Difficulte_Accent)
                    pressedLetter = Convert.ToChar(Utilities.RemoveDiacritics(Char.ToString(pressedLetter)));

                if(!MainWindow.Difficulte_Majuscule)
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
                                // Si la dernière lettre est verte alors on est dans le cadre d'une option; on remet du vert
                                if(((uniformGrid_word.Children[uniformGrid_word.Children.Count - 1] as Viewbox).Child as TextBlock).Foreground == Color)
                                    ((uniformGrid_word.Children[i] as Viewbox).Child as TextBlock).Foreground = Color;
                                else
                                    ((uniformGrid_word.Children[i] as Viewbox).Child as TextBlock).Foreground = Brushes.Green;

                                break; // empêche d'enlever les couleurs de toutes les lettres
                            }
                        }

                        IsWordComplet = false;
                    }
                    return true;
                }

                // si on écrit ê le char est ' ', donc
                if (pressedLetter == ' ')
                    pressedLetter = IsDiacriticsSensitive ? 'ê' : 'e';

                // si autres lettres
                if (!IsWordComplet)
                {
                    // On regarde si la lettre match
                    if (WordToWrite[CompletedWord.Length ] == pressedLetter)
                    {
                        // si 3 lettres successifs sont écrites pour ce mot
                        TempCounterSuccessiveLetter++;
                        if (TempCounterSuccessiveLetter >= 3)
                            WasWritingIt = true;

                        CompletedWord += pressedLetter;
                        // Change la couleur de la lettre
                        // + NbEspaceAvantMot car des fois je rajoute des espaces pour que ça soit à la même taille qu'une autre writebox
                        ((uniformGrid_word.Children[CompletedWord.Length - 1 + NbEspaceAvantMot] as Viewbox).Child as TextBlock).Foreground = Brushes.White;
                        // informe qu'un mot a été complété
                        if (Properties.Settings.Default.TaperMotParallele)
                            MainWindow.OneWordHasBeenCompleted = true;
                    }
                    // il s'est trompé on supprime l'avancement 
                    else
                    {
                        TempCounterSuccessiveLetter = 0;


                        // Si on a le paramètre "écrire d'autres mots en parallèle" en ON on ne supprime pas tant
                        // que l'on ne sait pas si un autre mot a été complété 
                        if (!Properties.Settings.Default.TaperMotParallele)
                            ClearWord();
                        else
                            MainWindow.ActionUCtoDeleteProgress.Add(ClearWord);

                        return false;
                    }

                    if (WordToWrite.Length == CompletedWord.Length)
                    {
                        // mot écrit
                        this.IsWordComplet = true;

                        if(!TypeSpone.Properties.Settings.Default.HaveToPressEnter )
                        {
                            // on valide le mot
                            IsDestroying = true;

                            // clear word
                            if (clearWordIfFinished)
                                ClearWord();

                            WordCompleted(this);

                            // supprime l'avancement de tous les autres mots qui ont <= de 3 lettres blanches
                            return null;
                        }
                    }

                    // Le mot fût compléter
                    return true;

                }
                else if ((pressedLetter == '\r' || key == Key.Space) && (!IsDestroying || !CanBeDestroyed) && TypeSpone.Properties.Settings.Default.HaveToPressEnter) // je précise ici key car la touche windows a aussi pour valeur ' '
                {
                    // on valide le mot
                    if(CanBeDestroyed)
                        IsDestroying = true;

                    // clear word
                    if (clearWordIfFinished)
                        ClearWord();

                    WordCompleted(this);

                    return true;
                }
                else if(Properties.Settings.Default.HaveToPressEnter) // Si on doit normallement appuyé sur enter pour valider et que le mot est remplis, mais que l'on fait pas ça alors on clear ; mauvaise touche
                {
                    MainWindow.NbFautesFrappe++;
                    ClearWord();
                }

                return false;
            }
            return false;
        }

        /// <summary>
        /// Change la couleur du texte
        /// </summary>
        /// <param name="color"></param>
        internal void ChangeForegroundColor(Brush color)
        {
            for (int i = 0; i < uniformGrid_word.Children.Count; i++)
            {
                ((uniformGrid_word.Children[i] as Viewbox).Child as TextBlock).Foreground = color;
            }
        }

        internal bool ClearWord()
        {
            // si c'est du vert on est dans le cadre des paramètres de difficulté, donc on laisse du vert
            if (((uniformGrid_word.Children[uniformGrid_word.Children.Count - 1] as Viewbox).Child as TextBlock).Foreground == Brushes.Green)
                ChangeForegroundColor(Brushes.Green);
            else
                ChangeForegroundColor(Color);

            this.IsWordComplet = false;

            if (CompletedWord.Length == 0) // on a supprimé quelque chose ou pas?
                return false;

            CompletedWord = string.Empty;
            return true;
        }
    }
}
