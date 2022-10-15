using System;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace TypeSpone
{
    internal static class Animation
    {
        /// <summary>
        /// Animation apparition disparition
        /// </summary>
        /// <param name="interval">Interval en ms</param>
        /// <param name="run">Object</param>
        internal static void AppearDisepear(int interval, Run run)
        {
            string textBase = run.Text;
            Timer t_anim = new Timer(interval);
            t_anim.Elapsed += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    run.Text = run.Text.Equals(textBase) ? " " : textBase;
                });
            };
            t_anim.Start();
        }
    }
}