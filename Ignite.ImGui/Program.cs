using ClickableTransparentOverlay;
using ImGuiNET;
using System.Numerics;

namespace Ignite.UI
{
    internal class Program : Overlay
    {
        static void Main ( string[] args )
        {
            Console.WriteLine("Hello, World!");
            Program program = new();
            program.Start().Wait();
        }

        protected override void Render ()
        {
            ImGui.Begin("Ignite");
            ImGui.SetWindowSize("Ignite", new Vector2(1080, 720));
            ImGui.End();
        }
    }
}
