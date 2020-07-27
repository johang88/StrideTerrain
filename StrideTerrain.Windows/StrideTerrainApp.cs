using Stride.Engine;

namespace StrideTerrain
{
    class StrideTerrainApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
