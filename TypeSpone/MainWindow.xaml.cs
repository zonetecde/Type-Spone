using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        Timer GameTimer_Duration = new Timer();
        DateTime GameDuration = new DateTime();

        private List<UserControl_WriteBox> UC_WordsToType = new List<UserControl_WriteBox>();
        private List<UserControl_WriteBox> WordsToDestroy = new List<UserControl_WriteBox>();

        // SETTINGS_MOT_PARALLELE
        internal static List<Func<bool>> ActionUCtoDeleteProgress { get; set; } = new List<Func<bool>>(); // contient la liste des actions ClearWord() à executé si aucun mot complété
        internal static bool OneWordHasBeenCompleted { get; set; } = false;

        // Game
        internal Difficulte Difficulte { get; private set; }
        internal static bool Difficulte_Accent { get; private set; } = false;
        internal static bool Difficulte_Majuscule { get; private set; } = false;
        private int NbMotEchoué { get; set; } = 0;
        private int MotRéussi { get; set; } = 0;
        private int NbMotCorrect { get; set; } = 0;
        private int NbCharacterEcris { get; set; } = 0;
        internal static int NbFautesFrappe { get; set; } = 0;
        private double Score { get; set; } = 0;


        // Word Color Before Typing
        internal static Brush WordColorBeforeTyping_GAME;
        internal static Brush WordColorBeforeTyping_MENU;

        //
        internal readonly Random Rdn = new Random();
        internal readonly Color COLOR_ANIMATION_CORRECT;
        internal readonly Color COLOR_END_ANIMATION;
        internal readonly Color COLOR_GAME_OVER;

        // Taille mot
        private int TAILLE_MOT = 50;

        public MainWindow()
        {
            InitializeComponent();

            Words = InitWords();

            BrushConverter brushConverter = new BrushConverter();
            WordColorBeforeTyping_MENU = (Brush)brushConverter.ConvertFromString("#FF525853")!;
            WordColorBeforeTyping_GAME = (Brush)brushConverter.ConvertFromString("#7FD99D2D")!;

            // couleurs du fond du jeu 
            COLOR_ANIMATION_CORRECT = (Color)ColorConverter.ConvertFromString("#66FD6D6D");
            COLOR_END_ANIMATION = (Color)ColorConverter.ConvertFromString("#66FF0000");
            COLOR_GAME_OVER = (Color)ColorConverter.ConvertFromString("#6602004C");

            Grid_MenuPrincipal.Visibility = Visibility.Visible;
            Grid_Game.Visibility = Visibility.Hidden;
            Grid_Options.Visibility = Visibility.Hidden;
            Grid_Difficulte.Visibility = Visibility.Hidden;
            Grid_Difficulte_Difficulte.Visibility = Visibility.Hidden;
            Grid_Difficulte_Retour.Visibility = Visibility.Hidden;
            Grid_PartieTermineeContent.Visibility = Visibility.Hidden;
            Grid_PartieTerminee.Visibility = Visibility.Hidden;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Type Effect sur le titre dans le menu principal 
            Animation.AppearDisepear(700, Run_TypeEffectMainTitle);

            // Settings
            TAILLE_MOT = Properties.Settings.Default.TailleMot;

            // Ajoute les UC_WriteBox pour taper "Jouer", "Option" et "Quitter" 
            AddUcToGrid("Jouer", (uc) => {  Grid_MenuPrincipal.Visibility = Visibility.Hidden; ShowGridAndResetWriteBox(Grid_Difficulte);  ShowGridAndResetWriteBox(Grid_Difficulte_Difficulte); ShowGridAndResetWriteBox(Grid_Difficulte_Retour); }, 4, Grid_MenuPrincipal, WordColorBeforeTyping_MENU);
            AddUcToGrid("Options", Menu_Interaction_Options, 5, Grid_MenuPrincipal, WordColorBeforeTyping_MENU);
            AddUcToGrid("Quitter", Menu_Interaction_Quitter, 6, Grid_MenuPrincipal, WordColorBeforeTyping_MENU);

            // Pour les options
            AddUcToGrid("Retour", Option_Interaction_Retour, 10, Grid_Options, WordColorBeforeTyping_MENU);

            // pour les difficultés
            AddUcToGrid("   Facile", ((uc) => { Difficulte = Difficulte.FACILE; Difficulte_Interaction_Play(uc); }), 2, Grid_Difficulte_Difficulte, WordColorBeforeTyping_MENU);
            AddUcToGrid("    Moyen", ((uc) => { Difficulte = Difficulte.MOYEN; Difficulte_Interaction_Play(uc); }), 4, Grid_Difficulte_Difficulte, WordColorBeforeTyping_MENU);
            AddUcToGrid("Difficile", ((uc) => { Difficulte = Difficulte.DIFFICILE; Difficulte_Interaction_Play(uc); }), 6, Grid_Difficulte_Difficulte, WordColorBeforeTyping_MENU);

            // => {} = Change la couleur de l'option (vert = sélectionner) et toggle Difficulte_Accent.
            AddUcToGrid("Accent   ", ((uc) => { Difficulte_Accent = !Difficulte_Accent; if (Difficulte_Accent == true) uc.ChangeForegroundColor(Brushes.Green); else uc.ChangeForegroundColor(WordColorBeforeTyping_MENU); }), 2, Grid_Difficulte_Options, WordColorBeforeTyping_MENU);  
            AddUcToGrid("Majuscule", ((uc) => { Difficulte_Majuscule = !Difficulte_Majuscule; if (Difficulte_Majuscule == true) uc.ChangeForegroundColor(Brushes.Green); else uc.ChangeForegroundColor(WordColorBeforeTyping_MENU); }), 4, Grid_Difficulte_Options, WordColorBeforeTyping_MENU);  
            
            AddUcToGrid("Retour", ((uc) => { Grid_Difficulte.Visibility = Visibility.Hidden; ShowGridAndResetWriteBox(Grid_MenuPrincipal); Grid_Difficulte_Retour.Visibility = Visibility.Hidden; Grid_Difficulte_Difficulte.Visibility = Visibility.Hidden; ; }), 1, Grid_Difficulte_Retour, WordColorBeforeTyping_MENU);
            
            // Partie terminé grid
            AddUcToGrid("Retour", ((uc) => { Grid_PartieTermineeContent.Visibility = Visibility.Hidden; Grid_PartieTerminee.Visibility = Visibility.Hidden;Grid_Game.Visibility = Visibility.Hidden; Grid_MenuPrincipal.Visibility = Visibility.Visible; }), 5, Grid_PartieTermineeContent, Brushes.Black);
        }

        /// <summary>
        /// Affiche la grid en supprimant l'avancement des writebox dedans
        /// </summary>
        /// <param name="grid"></param>
        private void ShowGridAndResetWriteBox(Grid grid)
        {
            grid.Visibility = Visibility.Visible;
            // Reset pour tous les UC writeBox de cette grid
            for (int i = 0; i < grid.Children.Count; i++)
                if (grid.Children[i] is UserControl_WriteBox)
                {
                    (grid.Children[i] as UserControl_WriteBox).ClearWord();
                }
        }

        private void Option_Interaction_Retour(UserControl_WriteBox obj)
        {
            Grid_Options.Visibility = Visibility.Hidden;
            ShowGridAndResetWriteBox(Grid_MenuPrincipal);
        }

        /// <summary>
        /// Ajoute un userControl d'interaction pour taper un choix
        /// </summary>
        /// <param name="word">Le mot à taper</param>
        /// <param name="action">L'event que ça engendre</param>
        /// <param name="row">La row sur lequel mettre l'option</param>
        private void AddUcToGrid(string word, Action<UserControl_WriteBox> action, int row, Grid grid, Brush customColor)
        {
            UserControl_WriteBox userControl_WriteBox = new UserControl_WriteBox(word, action, customColor);
            userControl_WriteBox.Margin = new Thickness(0,5,0,5);
            grid.Children.Add(userControl_WriteBox);
            Grid.SetColumn(userControl_WriteBox, 1);
            Grid.SetRow(userControl_WriteBox, row);
        }

        #region game

        private void Difficulte_Interaction_Play(UserControl_WriteBox uc_trigger)
        {
            // car on a tapé une difficulté
            PlayAudio(fromUri: "TypeSpone.resources.correct.wav");

            // RESET VARIABLE
            NbFautesFrappe = 0;
            NbMotCorrect = 0;
            NbMotEchoué = 0;
            MotRéussi = 0;
            Score = 0;
            NbCharacterEcris = 0;
            WordsToDestroy.Clear();
            Canvas_game.Children.Clear();
            OneWordHasBeenCompleted = false;
            ActionUCtoDeleteProgress.Clear();
            textBlock_GameTimer.Text = "00:00";
            TextBlock_scr.Text = "0 scr";
            Run_MotCorrect.Text = "0/7";
            Run_MotEchoue.Text = "0/3";

            Grid_MenuPrincipal.Visibility = Visibility.Hidden;
            Grid_Game.Visibility = Visibility.Visible;
            GameTimer_WordFall = new Timer();
            GameTimer_WordAdder = new Timer();
            GameTimer_WordDestroy = new Timer();
            GameTimer_Duration = new Timer();

            UC_WordsToType = new List<UserControl_WriteBox>();

            // Ajoute le premier mot
            AddRandomWordToType();

            // Un mot toutes les 3-6 secondes
            int minInterval = Difficulte == Difficulte.MOYEN ? 3000 : Difficulte == Difficulte.FACILE ? 4000 : 2000;
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
            GameTimer_WordFall.Elapsed +=  (sender, e) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    foreach (var uc in UC_WordsToType)
                    {
                        // est-ce que le mot a franchi la ligne d'arrivé ?
                        // Oui
                        if (Canvas.GetTop(uc) > Canvas_game.Height - (uc.Height / 2) + 10 && !uc.IsInDeadLine && !uc.IsWordComplet)
                        {
                            uc.IsInDeadLine = true;

                            // son echec
                            PlayAudio(fromUri: "TypeSpone.resources.word_deadLine.wav");

                            // animation couleur noir en fond
                            await BackgroundColorAnimation(Colors.Black);

                            // animation fade 
                            uc.ChangeForegroundColor(Brushes.Red);

                            WordsToDestroy.Add(uc);

                            NbMotEchoué++;
                            if (NbMotEchoué >= 3)
                            {
                                GameOver();
                            }
                            Run_MotEchoue.Text = NbMotEchoué + "/3";
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
                            UC_WordsToType.Remove(uc);
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
            
            // Timer compte le temps du jeu
            GameTimer_Duration.Interval = 1000;
            GameDuration = new DateTime(0);
            GameTimer_Duration.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    GameDuration = GameDuration.AddSeconds(1);
                    textBlock_GameTimer.Text = GameDuration.ToString("mm:ss");

                    Score = Math.Round(((double)NbCharacterEcris / ((double)GameDuration.Minute * (double)60 + (double)GameDuration.Second)) * (double)100, 1);
                    TextBlock_scr.Text = Score.ToString().Replace(',', '.').PadLeft(5, ' ') + " scr";
                });
                
            };

            GameTimer_WordAdder.Start();
            GameTimer_WordFall.Start();
            GameTimer_WordDestroy.Start();
            GameTimer_Duration.Start();
        }

        /// <summary>
        /// Game over
        /// </summary>
        private async void GameOver()
        {
            Grid_PartieTerminee.Visibility = Visibility.Visible;
            Grid_PartieTermineeContent.Visibility = Visibility.Visible;
            GameTimer_Duration.Stop();
            GameTimer_WordAdder.Stop();
            GameTimer_WordDestroy.Stop();
            GameTimer_WordFall.Stop();

            await BackgroundColorAnimation(COLOR_GAME_OVER, true);

            // animation resultat
            Timer anim_resultat = new Timer(50);
            double temp_score = 0.0;
            DateTime temps_temp = new DateTime();
            temps_temp = temps_temp.AddSeconds(1);
            anim_resultat.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    bool oneChanged = false;
                    if (Convert.ToInt16(Run_PartieTerminee_Faute.Text) != NbFautesFrappe)
                    {
                        Run_PartieTerminee_Faute.Text = (Convert.ToInt16(Run_PartieTerminee_Faute.Text) + 1).ToString().PadLeft(2, '0');
                        oneChanged = true;
                        PlayAudio(fromUri: "TypeSpone.resources.pop_sound.wav");
                    }
                    if (Convert.ToInt16(Run_PartieTerminee_MotEcrit.Text) != NbMotCorrect)
                    {
                        Run_PartieTerminee_MotEcrit.Text = (Convert.ToInt16(Run_PartieTerminee_MotEcrit.Text) + 1).ToString().PadLeft(2, '0');
                        oneChanged = true;
                        PlayAudio(fromUri: "TypeSpone.resources.pop_sound.wav");
                    }
                    if (temps_temp.Minute != Convert.ToInt16(textBlock_GameTimer.Text.Split(':')[0]) || temps_temp.Second != Convert.ToInt16(textBlock_GameTimer.Text.Split(':')[1]))
                    {
                        temps_temp = temps_temp.AddSeconds(1);
                        Run_PartieTerminee_Temps.Text = temps_temp.ToString("mm:ss");
                        oneChanged = true;
                        PlayAudio(fromUri: "TypeSpone.resources.pop_sound.wav");
                    }
                    if (temp_score < Score)
                    {
                        temp_score += 9;
                        temp_score = Math.Round(temp_score, 1);
                        Run_PartieTerminee_Score.Text = temp_score.ToString();
                        oneChanged = true;
                        PlayAudio(fromUri: "TypeSpone.resources.pop_sound.wav");
                    }

                    if (temp_score > Score)
                        Run_PartieTerminee_Score.Text = Score.ToString();


                    if (!oneChanged)
                        anim_resultat.Stop();
                });
            };
            anim_resultat.Start();

            // regarde si nouveau meilleur score (à partir de 1 min)
            if (Properties.Settings.Default.BestScore < Score && GameDuration.Minute >= 1)
            {
                TextBlock_nouveauRecord.Visibility = Visibility.Visible;
                PlayAudio(fromUri: "TypeSpone.resources.game_end_win.wav");
                Properties.Settings.Default.BestScore = Score;
                Properties.Settings.Default.Save();

            }
            else
            {
                TextBlock_nouveauRecord.Visibility = Visibility.Hidden;
                PlayAudio(fromUri: "TypeSpone.resources.game_end_win.wav");
            }
        }

        /// <summary>
        /// Change le fond du canvas game pour une animation en dégradé de la couleur
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private async Task BackgroundColorAnimation(Color c, bool backToNormalColor = true)
        {
            // Si on veut pas revenir sur la couleur normal cela veut dire on est sur l'écran de gameOver.
            Duration d = new Duration(backToNormalColor == true ? TimeSpan.FromSeconds(0.05) : TimeSpan.FromSeconds(2));

            ColorAnimation ca = new ColorAnimation(c, d);
            GameCanvas_color.BeginAnimation(GradientStop.ColorProperty, ca);
            GameCanvas_color2.BeginAnimation(GradientStop.ColorProperty, ca);

            if (backToNormalColor)
            {
                await PutTaskDelay();
                ColorAnimation ca2 = new ColorAnimation(COLOR_END_ANIMATION, new Duration(TimeSpan.FromSeconds(0.3)));
                GameCanvas_color.BeginAnimation(GradientStop.ColorProperty, ca2);
                GameCanvas_color2.BeginAnimation(GradientStop.ColorProperty, ca2);
            }
        }

        private int SwitchCanvasPart = 0; // 0 = 1/3, 1 = 2/3, 2 = 3/3 pour pas que les uc se chevauche

        /// <summary>
        /// Ajoute un mot au gameBoard
        /// </summary>
        private void AddRandomWordToType(bool speedSlow = false)
        {
            string rdnWord = Words[Rdn.Next(Words.Length)];
            var uc = new UserControl_WriteBox(rdnWord, WordTyped, WordColorBeforeTyping_GAME, canBeDestroyed: true, isDiacriticsSensitive: Difficulte_Accent);

            uc.Height = TAILLE_MOT;
            uc.Width = uc.WordToWrite.Length * TAILLE_MOT;

            Canvas_game.Children.Add(uc);

            int minB = 0;
            int maxB = Convert.ToInt16(Canvas_game.Width) / 3;
            if (SwitchCanvasPart > 0)
            {
                minB = SwitchCanvasPart == 1 ? Convert.ToInt16(Canvas_game.Width) / 3 : Convert.ToInt16((Canvas_game.Width / 3) * 2);
                maxB = SwitchCanvasPart == 1 ? Convert.ToInt16((Canvas_game.Width / 3) * 2) : Convert.ToInt16(Canvas_game.Width) - (TAILLE_MOT * rdnWord.Length);
                minB += TAILLE_MOT;
            }

            SwitchCanvasPart++;

            if (SwitchCanvasPart == 3)
                SwitchCanvasPart = 0;

            if (TAILLE_MOT + minB > maxB)
                minB = TAILLE_MOT;

            Canvas.SetLeft(uc, Rdn.Next(TAILLE_MOT + minB, maxB)); // - TAILLE_MOT * .Length pour pas qu'un mot sorte du champ de vision
            Canvas.SetTop(uc, - TAILLE_MOT * Rdn.Next(1,4)); // caché (va apparaître en descendant)

            if (rdnWord.Length <= 3)
                uc.Tag = 8;
            else if (rdnWord.Length <= 5)
                uc.Tag = 7;
            else if (rdnWord.Length <= 7)
                uc.Tag = 5;
            else if (rdnWord.Length <= 9)
                uc.Tag = 3;
            else if (rdnWord.Length <= 13)
                uc.Tag = 2;
            else 
                uc.Tag = 1;

            // si il y a des accents on ajoute du temps
            if (rdnWord.Contains("é") || rdnWord.Contains("ê") || rdnWord.Contains("ô") || rdnWord.Contains("ç") || rdnWord.Contains("â") || rdnWord.Contains("î") || rdnWord.Contains("è") || rdnWord.Contains("-") && Difficulte_Accent)
                uc.Tag = Convert.ToInt16(uc.Tag) - 3;

            // si il y a déjà 1 uc on baisse le temps aussi
            if(UC_WordsToType.Count >= 1)
                uc.Tag = Convert.ToInt16(uc.Tag) - 3;

            if(speedSlow)
                uc.Tag = Convert.ToInt16(uc.Tag) - 3;

            if (Convert.ToInt16(uc.Tag) <= 0)
                uc.Tag = 1;

            UC_WordsToType.Add(uc);
        }

        private async Task PutTaskDelay()
        {
            await Task.Delay(50);
        }

        private async void WordTyped(UserControl_WriteBox obj)
        {
            PlayAudio(fromUri: "TypeSpone.resources.correct.wav");
            WordsToDestroy.Add(obj);
            NbCharacterEcris += obj.WordToWrite.Length;

            // 1/4 d'avoir un mot qui spawn en plus
            int bMax = Difficulte == Difficulte.MOYEN ? 20 : Difficulte == Difficulte.FACILE ? 100 : 20;

            if (Canvas_game.Children.Count == 3 && Difficulte == Difficulte.MOYEN)
                bMax = 50;
            else if (Canvas_game.Children.Count == 5 && Difficulte == Difficulte.MOYEN)
                bMax = 80;

            if (Rdn.Next(0,100) > bMax || Canvas_game.Children.Count == 1 && Canvas_game.Children.Count < 4) // 1 car c'est lui (obj), < 6 car sinon ça fait trop de mot à écrire
                AddRandomWordToType(false);

            // animation couleur verte
            await BackgroundColorAnimation(COLOR_ANIMATION_CORRECT);

            // Ajoute +1 au résultat correct
            MotRéussi++;
            NbMotCorrect++;

            if (MotRéussi >= 7)
            {
                MotRéussi = 0;
                if(NbMotEchoué > 0)
                    NbMotEchoué--;

                Run_MotEchoue.Text = NbMotEchoué + "/3";
            }
            Run_MotCorrect.Text = MotRéussi + "/7";
        }

        #endregion

        private void Menu_Interaction_Options(UserControl_WriteBox uc_trigger)
        {
            PlayAudio(fromUri: "TypeSpone.resources.correct.wav");

            // Affiche les options
            Grid_MenuPrincipal.Visibility = Visibility.Hidden;
            ShowGridAndResetWriteBox(Grid_Options);

            checkBox_pressEnterToValidate.IsChecked = Properties.Settings.Default.HaveToPressEnter;
            checkBox_taperMotParallele.IsChecked = Properties.Settings.Default.TaperMotParallele;
            textBox_tailleMot.Text = Properties.Settings.Default.TailleMot.ToString();

            Regex _regex = new Regex("[^0-9.-]+"); // regex que des nombres

            textBox_tailleMot.PreviewTextInput += (sender, e) =>
            {
                e.Handled = _regex.IsMatch(e.Text);
            };
            textBox_tailleMot.TextChanged += (sender, e) =>
            {
                if (!String.IsNullOrEmpty(textBox_tailleMot.Text) && Convert.ToInt32(textBox_tailleMot.Text) != 0)

                    Properties.Settings.Default.TailleMot = Convert.ToInt32(textBox_tailleMot.Text);
                else
                    Properties.Settings.Default.TailleMot = 50;

                Properties.Settings.Default.Save();
                TAILLE_MOT = Properties.Settings.Default.TailleMot;
            };

            checkBox_pressEnterToValidate.Checked += (sender, e) =>        
            {
                checkBox_taperMotParallele.IsChecked = false;
                Properties.Settings.Default.HaveToPressEnter = true;
                Properties.Settings.Default.TaperMotParallele = false;
                Properties.Settings.Default.Save();
            };
            checkBox_pressEnterToValidate.Unchecked += (sender, e) =>
            {
                Properties.Settings.Default.HaveToPressEnter = false;

                Properties.Settings.Default.Save();
            };
            checkBox_taperMotParallele.Checked += (sender, e) =>
            {
                Properties.Settings.Default.TaperMotParallele = true;
                checkBox_pressEnterToValidate.IsChecked = false;
                Properties.Settings.Default.HaveToPressEnter = false;
                Properties.Settings.Default.Save();
            };
            checkBox_taperMotParallele.Unchecked += (sender, e) =>
            {
                Properties.Settings.Default.TaperMotParallele = false;
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
            if(e.Key == Key.Escape)
                GC.Collect();

            if (e.Key != Key.Oem6 && e.Key != Key.LWin && e.Key != Key.LeftShift && e.Key != Key.RightShift && e.Key != Key.Capital && e.Key != Key.F11) // Oem6 est la key permettant de faire l'accent circonflexe
            {
                // si on quitte l'animation gif de mot parallèle 
                if (e.Key == Key.Escape)
                    if (Grid_MotParelleleHelp.Visibility == Visibility.Visible)
                        Grid_MotParelleleHelp.Visibility = Visibility.Hidden;


                // Reset les 2 variables pour voir si un mot fut complété
                if (Properties.Settings.Default.TaperMotParallele)
                {
                    ActionUCtoDeleteProgress.Clear();
                    OneWordHasBeenCompleted = false;
                }

                // Check individuellement les grids pour savoir sur quel writeBox on doit ajouter la lettre
                List<bool?> isThereAnyCompleted = new List<bool?>();
                if (Grid_Game.Visibility == Visibility.Hidden || Grid_PartieTerminee.Visibility == Visibility.Visible)
                    isThereAnyCompleted =
                        InteractWithUserContrl_KeyDown(new List<Panel>()
                        {
                            Grid_PartieTermineeContent,
                            Grid_MenuPrincipal,
                            Grid_Options,
                            Grid_Difficulte,
                            Grid_Difficulte_Retour,
                            Grid_Difficulte_Difficulte,
                            Grid_Difficulte_Options,
                        }, e);
                else
                    // Si on est en jeu
                    for (int i = 0; i < UC_WordsToType.Count; i++)
                    {
                        isThereAnyCompleted.Add(UC_WordsToType[i].KeyDown(e.Key, false));
                    }



                // Aucun mot complété : son d'erreur
                // si il y a aucun mot qui n'a été complété 
                if (!isThereAnyCompleted.Any(x => x == true) && !isThereAnyCompleted.Any(x => !x.HasValue))
                {
                    if (Grid_Options.Visibility == Visibility.Hidden || !textBox_tailleMot.IsFocused) // on veut pas avoir de bruit d'erreur si on modifie une txtBox                                                                                                           // si on se trompe on supprime tout l'avancement du mot
                    {

                        PlayAudio(fromUri: "TypeSpone.resources.wrong.wav");
                    }

                    // Settings : option mot parallèle
                    bool pass = false; // pour éviter qu'on ajoute 2x une erreur

                    if (Properties.Settings.Default.TaperMotParallele)
                        // Aucun mot complété; on supprime tout les avancements
                        if (!OneWordHasBeenCompleted)
                            foreach (var action in ActionUCtoDeleteProgress)
                            {
                                if (action() && !pass) // quelque chose fut supprimé
                                {
                                    NbFautesFrappe++;
                                    pass = true;
                                }
                            }
                }
                // supprime l'avancement de tous les autres mots qui n'ont pas été écrit volontairement
                if (isThereAnyCompleted.Any(x => !x.HasValue))
                {
                    for (int i = 0; i < UC_WordsToType.Count; i++)
                    {
                        if (!UC_WordsToType[i].WasWritingIt && !UC_WordsToType[i].IsDestroying)
                            UC_WordsToType[i].ClearWord();
                    }
                }
            }
                    
            // Toggle plein écran
            if(e.Key == Key.F11)
            {
                if (this.WindowStyle == WindowStyle.SingleBorderWindow)
                {
                    this.Visibility = Visibility.Collapsed;
                    this.Topmost = true;
                    this.WindowStyle = WindowStyle.None;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.Visibility = Visibility.Visible;
          
                }
                else
                { 
                    this.Topmost = false;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.ResizeMode = ResizeMode.CanResize;
                }
            }

            // Bruit de clavier mécanique
            PlayAudio(e.Key, "press");

        }

        /// <summary>
        /// 
        /// Check individuellement les grids de grids pour savoir laquelle est visible
        /// et donc laquelle on doit interagir avec KeyDown
        /// </summary>
        /// <param name="grids"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private List<bool?> InteractWithUserContrl_KeyDown(List<Panel> grids, KeyEventArgs e)
        {
            List<bool?> isThereAnyCompleted = new List<bool?>();
            foreach (Panel grid in grids)
                if (grid.Visibility == Visibility.Visible)
                {                
                    // KeyDown pour tous les UC writeBox de cette grid
                    for (int i = 0; i < grid.Children.Count; i++)
                        if (grid.Children[i] is UserControl_WriteBox)
                            isThereAnyCompleted.Add((grid.Children[i] as UserControl_WriteBox).KeyDown(e.Key, true));
                    
                    if(grid as Grid != Grid_Difficulte && grid as Grid != Grid_Difficulte_Retour && grid as Grid != Grid_Difficulte_Difficulte) // car ces grids sont tous unis
                        break;
                }
            return isThereAnyCompleted;
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
            // En fonction de si str est null ou pas : on prend la resource de fromUrl
            Stream str = String.IsNullOrEmpty(fromUri) ? getStreamFromKey(e, state) : System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(fromUri)!;

            WaveFileReader wfr = new WaveFileReader(str);
            Wave16ToFloatProvider wc = new Wave16ToFloatProvider(wfr);
            DirectSoundOut wfo = new DirectSoundOut();
            if(fromUri.Contains("pop"))
                wc.Volume = 0.2f;
            wfo.Init(wc);
            wfo.Play();
            wfo.PlaybackStopped += (sender, e) =>
            {
                wfr.Dispose();
                wfo.Dispose();
                str.Dispose();

            };

        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Grid_MotParelleleHelp.Visibility = Visibility.Visible;
            }
        }

        private void Grid_MotParelleleHelp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Grid_MotParelleleHelp.Visibility = Visibility.Hidden;
            }

        }
    }

    internal enum Difficulte
    {
        FACILE,
        MOYEN,
        DIFFICILE
    }
}
