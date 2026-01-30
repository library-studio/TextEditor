using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    public static class AppUtility
    {
        public static string GetBinDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void LoadState(MarcControl control)
        {
            try
            {
                var path = GetStateFileName();
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    control.UiStateJson = content;
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        public static void SaveState(MarcControl control)
        {
            var path = GetStateFileName();
            File.WriteAllText(path, control.UiStateJson);
        }

        static string GetMarcFileName()
        {
            return Path.Combine(GetBinDirectory(), "marc.txt");
        }

        static string GetStateFileName()
        {
            return Path.Combine(GetBinDirectory(), "state.txt");
        }

        public static void LoadMarc(MarcControl control)
        {
            //return;
            try
            {
                var path = GetMarcFileName();
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    if (string.IsNullOrEmpty(content) == false)
                    {
                        control.Content = content;
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        public static void SaveMarc(MarcControl control)
        {
            //return;
            var path = GetMarcFileName();
            File.WriteAllText(path, control.Content);
        }


    }
}
