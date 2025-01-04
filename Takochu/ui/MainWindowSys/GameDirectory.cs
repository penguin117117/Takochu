using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using Takochu.smg;
using Takochu.util;
using Takochu.io;
using Takochu.smg.msg;
using Takochu.smg.img;
using System.Windows.Forms;

namespace Takochu.ui.MainWindowSys
{
    public class GameDirectory
    {
        private const string Default_GameDir = "\"\"";

        public string GamePath
        {
            get => Properties.Settings.Default.GamePath;
            private set => Properties.Settings.Default.GamePath = value;
        }

        public void OpenDirectory(bool reSetup = false) 
        {
            bool isDefaultGameDir    = GamePath == Default_GameDir;
            bool notFoundUserGameDir = !Directory.Exists(GamePath);

            if (isDefaultGameDir || notFoundUserGameDir)
            {
                Translate.GetMessageBox.Show(MessageBoxText.InitialPathSettings, MessageBoxCaption.Info);

                if (BrowseSetGameDirectory() == false)
                {
                    return;
                }
            }

            OpenGalaxyArcFiles(reSetup);
        }

        /// <summary>
        /// ゲームディレクトリを初期化します。
        /// </summary>
        public void SetDefaultPath()
        {
            Properties.Settings.Default.GamePath = Default_GameDir;
            Properties.Settings.Default.Save();
        }

        private void OpenGalaxyArcFiles(bool reSetup = false)
        {

            var extFileSys = new ExternalFilesystem(Properties.Settings.Default.GamePath);
            Program.sGame = new Game(extFileSys);

            if (reSetup)
                LightData.Close();
            LightData.Initialize();




            if (GameUtil.IsSMG2())
            {

                if (reSetup)
                    StageBgmInfoArcFile.Close();

                StageBgmInfoArcFile.Initialize();


            }

            if (reSetup)
                NameHolder.Close();


            NameHolder.Initialize();



            ImageHolder.Initialize();

        }

        public bool BrowseSetGameDirectory()
        {
            var SetPath = Properties.Settings.Default.GamePath;

            if (!Directory.Exists(SetPath))
                SetPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            CommonOpenFileDialog cofd = new CommonOpenFileDialog
            {
                InitialDirectory = SetPath,
                IsFolderPicker = true
            };
            if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = cofd.FileName;
                if (Directory.Exists($"{path}/StageData") && Directory.Exists($"{path}/ObjectData"))
                {
                    Properties.Settings.Default.GamePath = path;
                    Properties.Settings.Default.Save();

                    Program.sGame = new Game(new ExternalFilesystem(path));

                    Translate.GetMessageBox.Show(MessageBoxText.FolderPathCorrectly, MessageBoxCaption.Info);
                    return true;
                }
                else
                {
                    Translate.GetMessageBox.Show(MessageBoxText.InvalidGameFolder, MessageBoxCaption.Error);
                    return false;
                }
            }
            return false;
        }
    }
}
