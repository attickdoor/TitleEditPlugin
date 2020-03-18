using Dalamud.Game.Command;
using Dalamud.Plugin;
using System.Numerics;
using ImGuiNET;

namespace TitleEditPlugin
{
    internal class MOAssistPlugin : IDalamudPlugin
    {
        public string Name => "Title Edit Plugin";

        public TitleEditConfiguration Configuration;

        private DalamudPluginInterface pluginInterface;
        private TitleEdit titleEdit;

        private string[] ExpacNames = { "A Realm Reborn", "Heavensward", "Stormblood", "Shadowbringers", "Random" };
        private int ExpacNum;
        private bool isImguiTitleEditOpen = false;
        

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.pluginInterface.CommandManager.AddHandler("/ptitle", new CommandInfo(OnCommandDebugMouseover)
            {
                HelpMessage = "Open a window to set the title screen version.",
                ShowInHelp = true
            });

            Configuration = pluginInterface.GetPluginConfig() as TitleEditConfiguration ?? new TitleEditConfiguration();

            titleEdit = new TitleEdit(pluginInterface.TargetModuleScanner, pluginInterface.ClientState, Configuration);

            titleEdit.Enable();
            SetNewConfig();

            this.pluginInterface.UiBuilder.OnBuildUi += UiBuilder_OnBuildUi;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += (sender, args) => isImguiTitleEditOpen = true;
        }

        private void UiBuilder_OnBuildUi()
        {
            if (!isImguiTitleEditOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(600, 250));

            ImGui.Begin("Title Editing", ref isImguiTitleEditOpen,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar);

            ImGui.Text("This window allows you to change what title screen plays when you start the game.");
            ImGui.Separator();

            ImGui.BeginChild("scrolling", new Vector2(0, 150), true, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 3));

            ImGui.Combo("Title screen expac to use", ref ExpacNum, ExpacNames, ExpacNames.Length);

            ImGui.PopStyleVar();

            ImGui.EndChild();

            ImGui.Separator();

            if (ImGui.Button("Save and Close"))
            { 
                UpdateConfig();
                pluginInterface.SavePluginConfig(Configuration);

                SetNewConfig();
                isImguiTitleEditOpen = false;
            }

            ImGui.Spacing();
            ImGui.End();
        }

        private void UpdateConfig()
        {
            Configuration.ExpacNum = (byte)ExpacNum;
        }

        private void SetNewConfig()
        {
            titleEdit.SetExpac(Configuration.ExpacNum);
            ExpacNum = Configuration.ExpacNum;
        }

        public void Dispose()
        {
            titleEdit.Dispose();

            pluginInterface.CommandManager.RemoveHandler("/ptitle");

            pluginInterface.Dispose();
        }

        private void OnCommandDebugMouseover(string command, string arguments)
        {
            isImguiTitleEditOpen = true;
        }
    }
}