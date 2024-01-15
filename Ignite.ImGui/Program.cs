using ClickableTransparentOverlay;
using Ignite.Systems;
using ImGuiNET;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Ignite.UI
{
    internal class Program : Overlay
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        bool isOpen = true;
        bool isPercistentWorldTabOpen = true;

        World world;


        static void Main(string[] args)
        {
            IntPtr handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);


            Console.WriteLine("Starting ImGui ...");

            Program program = new();

            program.world = new World(Array.Empty<ISystem>());

            Node player = program.world.AddNode("Player");
            Node renderer = program.world.AddNode("Renderer");
            Node controller = program.world.AddNode("Controller");

            controller
                .AddComponent<Move>()
                .AddComponent<Jump>();

            player
                .AddChild(renderer)
                .AddChild(controller);

            program.world.Start();

            program.Start().Wait();
            program.Position = new System.Drawing.Point(0, 0);
            program.Size = new System.Drawing.Size(1920, 1080);
        }

        protected override Task PostInitialized()
        {
            ImGuiStylePtr style = ImGui.GetStyle();

            Vector4 borderColor = new(90f / 255f, 92f / 255f, 144f / 255f, 1);
            Vector4 bgColor = new(21 / 255f, 21 / 255f, 33 / 255f, 1);
            Vector4 titleBgColor = new(177f / 255f, 178f / 255f, 255f / 255f, 1);
            Vector4 textColor = new(248f / 255f, 248f / 255f, 248f / 255f, 1);

            style.Alpha = 1;
            style.Colors[(int)ImGuiCol.TitleBgActive] = titleBgColor;
            style.Colors[(int)ImGuiCol.TitleBg] = titleBgColor;
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = titleBgColor;

            style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0, 0, 0, 0.2f);
            style.Colors[(int)ImGuiCol.ButtonActive] = titleBgColor;
            style.Colors[(int)ImGuiCol.Button] = titleBgColor;

            style.Colors[(int)ImGuiCol.MenuBarBg] = bgColor * 1.5f;

            style.Colors[(int)ImGuiCol.Text] = textColor;
            style.Colors[(int)ImGuiCol.TextSelectedBg] = borderColor;

            style.Colors[(int)ImGuiCol.Border] = borderColor;
            style.Colors[(int)ImGuiCol.Separator] = borderColor;

            style.Colors[(int)ImGuiCol.WindowBg] = bgColor;

            style.WindowMinSize = new Vector2(1080, 720);


            style.FrameRounding = 4;
            style.WindowRounding = 4;
            style.TabRounding = 4;
            style.ChildRounding = 4;
            style.GrabRounding = 4;
            style.ScrollbarRounding = 4;
            style.PopupRounding = 4;

            return base.PostInitialized();
        }

        protected override void Render()
        {
            ImGui.SetNextWindowSize(new Vector2(1080, 720));
            ImGui.Begin("Ignite", ref isOpen, ImGuiWindowFlags.MenuBar);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Exit", "Escape")) { isOpen = false; Close(); Console.WriteLine("Close."); }
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }

            if (ImGui.BeginTabBar("Tabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.FittingPolicyScroll | ImGuiTabBarFlags.FittingPolicyResizeDown))
            {
                if (ImGui.BeginTabItem("Percistent World", ref isPercistentWorldTabOpen,
                    ImGuiTabItemFlags.NoCloseWithMiddleMouseButton | ImGuiTabItemFlags.SetSelected))
                {
                    ShowNodeHierarchy(world.Root, 0);

                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        private void ShowNodeHierarchy(Node root, int depth)
        {
            string indents = string.Empty;
            for (int i = 0; i < depth; i++)
            {
                indents += "\t";
            }

            ImGui.Text($"{indents}{root.Name} | ID: {root.Id}");

            if (!root.Children.IsDefaultOrEmpty)
            {
                foreach (var node in root.Children)
                {
                    ShowNodeHierarchy(node, depth + 1);
                }
            }

            if (root.Components.Count != 0)
            {
                ImGui.Text($"Components : {root.Components.Count}");
                foreach (var (id, component) in root.Components)
                {
                    ImGui.Text($"{indents}\t{component.GetType()} | ID: {id}");

                }
            }
        }
    }
}
