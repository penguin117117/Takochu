using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Takochu.io;
using Takochu.util;

namespace Takochu.smg
{
    public class Game
    {
        private const string Only_SMG2_File = "/ObjectData/ProductMapObjDataTable.arc";
        public FilesystemBase Filesystem { get; private set; }

        public Game(FilesystemBase filesystem)
        {
            Filesystem = filesystem;
            SetGameVer();
        }

        private void SetGameVer() 
        {
            if (Filesystem.DoesFileExist(Only_SMG2_File))
                GameUtil.SetGame(GameUtil.Game.SMG2);
            else
                GameUtil.SetGame(GameUtil.Game.SMG1);
        }

        public void Close()
        {
            Filesystem.Close();
        }

        public bool DoesFileExist(string file)
        {
            return Filesystem.DoesFileExist(file);
        }

        public bool HasScenario(string galaxy)
        {
            // this solution works for both games
            return Filesystem.DoesFileExist($"/StageData/{galaxy}/{galaxy}Scenario.arc");
        }

        public List<string> GetGalaxies()
        {
            // this solution works for both games
            List<string> stageDataDirs = Filesystem.GetDirectories("/StageData");
            return stageDataDirs.FindAll(galaxyName => HasScenario(galaxyName));
        }

        public GalaxyScenario OpenGalaxy(string galaxy)
        {
            if (!HasScenario(galaxy))
            {
                throw new Exception("Game::OpenGalaxy() -- Requested name is not a Galaxy.");
            }

            return new GalaxyScenario(this, galaxy);
        }

        
    }
}
