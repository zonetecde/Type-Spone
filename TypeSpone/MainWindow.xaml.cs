using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Variables
        private string[] Words; // liste des mots du dictionnaire français
        Timer GameTimer_WordAdder = new Timer();
        Timer GameTimer_WordFall = new Timer();
        Timer GameTimer_WordDestroy = new Timer();
        private List<UserControl_WriteBox> WordsToType = new List<UserControl_WriteBox>();
        private List<UserControl_WriteBox> WordsToDestroy = new List<UserControl_WriteBox>();

        // Word Color Before Typing
        internal static Brush WordColorBeforeTyping;

        //
        Random Rdn = new Random();

        public MainWindow()
        {
            InitializeComponent();

            Words = InitWords();

            BrushConverter brushConverter = new BrushConverter();
            WordColorBeforeTyping = (Brush)brushConverter.ConvertFromString("#FF525853")!;

            Grid_MenuPrincipal.Visibility = Visibility.Visible;
            Grid_Game.Visibility = Visibility.Hidden;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Type Effect sur le titre dans le menu principal 
            Animation.AppearDisepear(700, Run_TypeEffectMainTitle);

            // Ajoute les UC_WriteBox pour taper "Jouer", "Option" et "Quitter" 
            AddUcToGrid("Jouer", Menu_Interaction_Play, 4, Grid_MenuPrincipal);
            AddUcToGrid("Options", Menu_Interaction_Options, 5, Grid_MenuPrincipal);
            AddUcToGrid("Quitter", Menu_Interaction_Quitter, 6, Grid_MenuPrincipal);
            AddUcToGrid("Retour", Option_Interaction_Retour, 8, Grid_Options);

        }

        private void Option_Interaction_Retour(UserControl_WriteBox obj)
        {
            Grid_Options.Visibility = Visibility.Hidden;
            Grid_MenuPrincipal.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Ajoute un userControl d'interaction pour taper un choix
        /// </summary>
        /// <param name="word">Le mot à taper</param>
        /// <param name="action">L'event que ça engendre</param>
        /// <param name="row">La row sur lequel mettre l'option</param>
        private void AddUcToGrid(string word, Action<UserControl_WriteBox> action, int row, Grid grid)
        {
            UserControl_WriteBox userControl_WriteBox = new UserControl_WriteBox(word, action);
            userControl_WriteBox.Margin = new Thickness(0,5,0,5);
            grid.Children.Add(userControl_WriteBox);
            Grid.SetColumn(userControl_WriteBox, 1);
            Grid.SetRow(userControl_WriteBox, row);
        }

        #region game

        private void Menu_Interaction_Play(UserControl_WriteBox uc_trigger)
        {
            PlayAudio(fromUri: "TypeSpone.resources.correct.wav");

            Grid_MenuPrincipal.Visibility = Visibility.Hidden;
            Grid_Game.Visibility = Visibility.Visible;

            WordsToType = new List<UserControl_WriteBox>();

            // Ajoute le premier mot
            AddRandomWordToType();

            // Un mot toutes les 3-6 secondes
            int minInterval = 3000;
            int maxInterval = 5000;
            GameTimer_WordAdder.Interval = Rdn.Next(minInterval, maxInterval);

            GameTimer_WordAdder.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    minInterval -= 50;
                    maxInterval -= 10;
                    if (minInterval <= 1500)
                    {
                        minInterval = 1500;
                    }
                    if(maxInterval <= 2000)
                        maxInterval = 2000;


                    GameTimer_WordAdder.Interval = Rdn.Next(minInterval, maxInterval);

                    AddRandomWordToType();
                });
            };

            // Timer word fall down
            GameTimer_WordFall.Interval = 15;
            GameTimer_WordFall.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    foreach (var uc in WordsToType)
                    {
                        // est-ce que le mot a franchi la ligne d'arrivé ?
                        // Oui
                        if (Canvas.GetTop(uc) > Canvas_game.Height - (uc.Height / 2) + 10 && !uc.IsInDeadLine)
                        {
                            // animation fade 
                            foreach(Viewbox vb in uc.uniformGrid_word.Children)
                            {
                                (vb.Child as TextBlock).Foreground = Brushes.Red;
                            }
                            WordsToDestroy.Add(uc);
                            uc.IsInDeadLine = true;
                        }
                        // Non, ou la ligne l'a déjà franchi une première fois
                        else
                        {
                            Canvas.SetTop(uc, Canvas.GetTop(uc) + Convert.ToInt32(uc.Tag));
                        }
                    }
                });

            };

            // Animation opacity pour les mots écrits/qui ont franchit la ligne
            GameTimer_WordDestroy.Interval = 35;

            GameTimer_WordDestroy.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    for (int i = 0; i < WordsToDestroy.Count; i++)
                    {
                        UserControl_WriteBox? uc = WordsToDestroy[i];

                        if (uc.uniformGrid_word.Opacity <= 0.1)
                        // delete
                        {
                            Canvas_game.Children.Remove(uc);
                            WordsToType.Remove(uc);
                            WordsToDestroy.Remove(uc);
                            i--;
                        }

                        else
                        {
                            // si c'est dans la deadline l'animation doit être plus lente
                            if(uc.IsInDeadLine)
                                uc.uniformGrid_word.Opacity -= 0.06;
                            else
                                uc.uniformGrid_word.Opacity -= 0.1;
                        }

                    }
                });
                
            };

            GameTimer_WordAdder.Start();
            GameTimer_WordFall.Start();
            GameTimer_WordDestroy.Start();
        }

        /// <summary>
        /// Ajoute un mot au gameBoard
        /// </summary>
        private void AddRandomWordToType()
        {
            var uc = new UserControl_WriteBox(Words[Rdn.Next(Words.Length)], WordTyped);
            uc.Height = 50;
            uc.Width = uc.WordToWrite.Length * 50;

            Canvas_game.Children.Add(uc);

            Canvas.SetLeft(uc, Rdn.Next(50, Convert.ToInt32(Canvas_game.Width) - 375)); // - 300 pour pas qu'un mot sorte du champ de vision
            Canvas.SetTop(uc, -100); // caché (va apparaître en descendant)

            if (uc.WordToWrite.Length <= 3)
                uc.Tag = 8;
            else if (uc.WordToWrite.Length <= 5)
                uc.Tag = 7;
            else if (uc.WordToWrite.Length <= 7)
                uc.Tag = 5;
            else if (uc.WordToWrite.Length <= 9)
                uc.Tag = 3;
            else if (uc.WordToWrite.Length <= 13)
                uc.Tag = 2;
            else 
                uc.Tag = 1;

            WordsToType.Add(uc);
        }

        private void WordTyped(UserControl_WriteBox obj)
        {
            PlayAudio(fromUri: "TypeSpone.resources.correct.wav");
            WordsToDestroy.Add(obj);
        }

        #endregion

        private void Menu_Interaction_Options(UserControl_WriteBox uc_trigger)
        {
            PlayAudio(fromUri: "TypeSpone.resources.correct.wav");

            // Affiche les options
            Grid_MenuPrincipal.Visibility = Visibility.Hidden;
            Grid_Options.Visibility = Visibility.Visible;

            checkBox_pressEnterToValidate.IsChecked = Properties.Settings.Default.HaveToPressEnter;
            checkBox_pressEnterToValidate.Checked += (sender, e) =>
            {
                Properties.Settings.Default.HaveToPressEnter = true;
                Properties.Settings.Default.Save();
            };
            checkBox_pressEnterToValidate.Unchecked += (sender, e) =>
            {
                Properties.Settings.Default.HaveToPressEnter = false;
                Properties.Settings.Default.Save();
            };
        }

        private void Menu_Interaction_Quitter(UserControl_WriteBox uc_trigger)
        {
            PlayAudio(fromUri: "TypeSpone.resources.correct.wav");
            this.Close();
        }

        /// <summary>
        /// Récupère les mots dans le fichier resources/words.txt
        /// </summary>
        private string[] InitWords()
        {
            // Récupère les mots dans un string
            string str_words = new StreamReader(
                            System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("TypeSpone.resources.words.txt")!
                        ).ReadToEnd();

            // Convertis le str en str[]
            return str_words.Split(new string[] { "\n" }, StringSplitOptions.None);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            List<bool> isThereAnyCompleted = new List<bool>();

            // Si on est sur le menu principal
            if (Grid_MenuPrincipal.Visibility == Visibility.Visible)
            {
                // KeyDown pour "Jouer", "Option", "Quitter"
                for (int i = 0; i < Grid_MenuPrincipal.Children.Count; i++)
                {
                    if (Grid_MenuPrincipal.Children[i] is UserControl_WriteBox)
                        isThereAnyCompleted.Add((Grid_MenuPrincipal.Children[i] as UserControl_WriteBox).KeyDown(e.Key, true));
                }

            }
            // Si on est en jeu
            else if(Grid_Game.Visibility == Visibility.Visible)
            {
                for (int i = 0; i < Canvas_game.Children.Count; i++)
                {
                    if (Canvas_game.Children[i] is UserControl_WriteBox)
                        isThereAnyCompleted.Add((Canvas_game.Children[i] as UserControl_WriteBox).KeyDown(e.Key, false));
                }
            }            
            // Si on est dans les options
            else if(Grid_Options.Visibility == Visibility.Visible)
            {
                for (int i = 0; i < Grid_Options.Children.Count; i++)
                {
                    if (Grid_Options.Children[i] is UserControl_WriteBox)
                        isThereAnyCompleted.Add((Grid_Options.Children[i] as UserControl_WriteBox).KeyDown(e.Key, true));
                }
            }

            // Aucun mot complété : son d'erreur
            // si il y a aucun mot qui n'a été complété 
            if (!isThereAnyCompleted.Any(x => x == true))
            {
                // si on se trompe on supprime tout l'avancement du mot
                PlayAudio(fromUri: "TypeSpone.resources.wrong.wav");
            }

            // Bruit de clavier mécanique
            PlayAudio(e.Key, "press");
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            PlayAudio(e.Key, "release");
        }

        /// <summary>
        /// Get l'audio correspondant à la key depuis les resources
        /// </summary>
        /// <param name="e"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private static Stream getStreamFromKey(Key e, string state)
        {
            if (e == Key.Enter)
                return System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("TypeSpone.resources.key1_enter_" + state + ".wav")!;
            else if (e == Key.Space)
                return System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("TypeSpone.resources.key1_space_" + state + ".wav")!;
            else if (e == Key.Back)
                return System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("TypeSpone.resources.key1_return_" + state + ".wav")!;
            else
                return System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("TypeSpone.resources.key1_" + state + ".wav")!;
        }

        /// <summary>
        /// Joue un audio
        /// </summary>
        /// <param name="e"></param>
        /// <param name="state"></param>
        private static void PlayAudio(Key e = Key.A, string state = "", string fromUri = "")
        {
            // Bruit de clavier mécanique si on veut le bruit d'une touche
            Stream str = String.IsNullOrEmpty(fromUri) ? getStreamFromKey(e, state) : null!;

            // En fonction de si str est null ou pas : on prend la resource de fromUrl
            var reader = str != null ? new WaveFileReader(str) : new WaveFileReader(System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(fromUri));
            var waveOut = new WaveOut();
            waveOut.Init(reader);
            waveOut.Play();
            waveOut.PlaybackStopped += (sender, e) =>
            {
                waveOut.Dispose();
            };
        }

    }
}
